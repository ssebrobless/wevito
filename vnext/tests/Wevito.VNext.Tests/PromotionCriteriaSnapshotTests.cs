using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class PromotionCriteriaSnapshotTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-13T12:00:00Z");

    [Fact]
    public void Compute_ReturnsKeepSupervisedPreview_WhenWindowIsTooShort()
    {
        var fixture = PromotionFixture.Create();
        fixture.SeedPassingRows(Now);

        var decision = fixture.Compute(Now.AddDays(-3), Now);

        Assert.Equal(PromotionDecisionLabel.KeepSupervisedPreview, decision.Label);
        Assert.Contains("window_too_short", decision.Reasons);
    }

    [Theory]
    [InlineData("hosted_ai")]
    [InlineData("policy")]
    [InlineData("mutation_without_proof")]
    [InlineData("focus_steal")]
    [InlineData("kill_switch_observed")]
    public void Compute_ReturnsPauseForReliabilityWork_OnSafetyFailures(string failure)
    {
        var fixture = PromotionFixture.Create();
        fixture.SeedPassingRows(Now);
        fixture.SeedSafetyFailure(failure, Now);

        var decision = fixture.Compute(Now.AddDays(-7), Now);

        Assert.Equal(PromotionDecisionLabel.PauseForReliabilityWork, decision.Label);
        Assert.Contains(decision.Criteria, criterion => criterion.Class == PromotionCriterionClass.Safety && !criterion.Passed);
    }

    [Theory]
    [InlineData("uptime")]
    [InlineData("citation")]
    [InlineData("golden")]
    [InlineData("self_improvement")]
    public void Compute_ReturnsKeepSupervisedPreview_OnLivenessFailures(string failure)
    {
        var fixture = PromotionFixture.Create();
        fixture.SeedPassingRows(Now, includeSelfImprovement: failure != "self_improvement", includeHeartbeats: failure != "uptime");
        if (failure == "citation")
        {
            fixture.WriteGoldenEval(passed: true, citationCoverage: 0.2);
        }
        else if (failure == "golden")
        {
            fixture.WriteGoldenEval(passed: false, citationCoverage: 0.9);
        }

        var decision = fixture.Compute(Now.AddDays(-7), Now);

        Assert.Equal(PromotionDecisionLabel.KeepSupervisedPreview, decision.Label);
        Assert.Contains(decision.Criteria, criterion => criterion.Class == PromotionCriterionClass.Liveness && !criterion.Passed);
    }

    [Fact]
    public void Compute_ReturnsEnableAutonomousBeta_WhenAllCriteriaPass()
    {
        var fixture = PromotionFixture.Create();
        fixture.SeedPassingRows(Now);

        var decision = fixture.Compute(Now.AddDays(-7), Now);

        Assert.Equal(PromotionDecisionLabel.EnableAutonomousBeta, decision.Label);
        Assert.All(decision.Criteria, criterion => Assert.True(criterion.Passed, criterion.Id));
    }

    [Fact]
    public void Compute_Refuses_WhenKillSwitchIsActive()
    {
        var fixture = PromotionFixture.Create();
        fixture.SeedPassingRows(Now);
        fixture.Settings[KillSwitchService.KillSwitchSetting] = bool.TrueString;

        var decision = fixture.Compute(Now.AddDays(-7), Now);

        Assert.Equal(PromotionDecisionLabel.KeepSupervisedPreview, decision.Label);
        Assert.Contains("kill_switch_active", decision.Reasons);
    }

    [Fact]
    public void Compute_EmitsPromotionCriteriaSnapshotRow()
    {
        var fixture = PromotionFixture.Create();
        fixture.SeedPassingRows(Now);

        var decision = fixture.Compute(Now.AddDays(-7), Now);
        var rows = fixture.Ledger.Snapshot(Now.AddDays(-7), Now.AddMinutes(1));

        Assert.Equal(PromotionDecisionLabel.EnableAutonomousBeta, decision.Label);
        Assert.Contains(rows, row => row.PacketKind == PromotionCriteriaSnapshot.SnapshotPacketKind && row.Status == "Completed");
    }

    [Fact]
    public void SettingsEntry_IsDisabledByDefault()
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Assert.False(PromotionCriteriaSnapshot.CanEnableAutonomousBetaEntry(null, settings));
        Assert.Contains("Disabled", ToolPopupWindow.FormatAutonomousBetaTryHelp(null, settings));
    }

    [Fact]
    public void SettingsEntry_RequiresDecisionDisabledBetaAndKillSwitchOff()
    {
        var decision = PassingDecision();
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [AutonomousOperationsConfig.EnabledSetting] = bool.FalseString,
            [KillSwitchService.KillSwitchSetting] = bool.FalseString
        };

        Assert.True(PromotionCriteriaSnapshot.CanEnableAutonomousBetaEntry(decision, settings));

        settings[AutonomousOperationsConfig.EnabledSetting] = bool.TrueString;
        Assert.False(PromotionCriteriaSnapshot.CanEnableAutonomousBetaEntry(decision, settings));

        settings[AutonomousOperationsConfig.EnabledSetting] = bool.FalseString;
        settings[KillSwitchService.KillSwitchSetting] = bool.TrueString;
        Assert.False(PromotionCriteriaSnapshot.CanEnableAutonomousBetaEntry(decision, settings));

        Assert.False(PromotionCriteriaSnapshot.CanEnableAutonomousBetaEntry(decision with { Label = PromotionDecisionLabel.KeepSupervisedPreview }, settings));
    }

    [Fact]
    public void ConsentRowIsAllowedOnlyForExplicitConfirm()
    {
        var decision = PassingDecision();
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [AutonomousOperationsConfig.EnabledSetting] = bool.FalseString,
            [KillSwitchService.KillSwitchSetting] = bool.FalseString
        };

        Assert.False(ToolPopupWindow.ShouldWriteAutonomousBetaConsent(false, decision, settings));
        Assert.True(ToolPopupWindow.ShouldWriteAutonomousBetaConsent(true, decision, settings));
    }

    [Theory]
    [InlineData("promotion_criteria_snapshot")]
    [InlineData("promotion_decision")]
    [InlineData("runtime_autonomous_beta_user_consent")]
    public void PlainLanguageExplainer_CoversNewPacketKinds(string packetKind)
    {
        var explainer = new PlainLanguageExplainer();

        var text = explainer.ExplainPacketKind(packetKind);

        Assert.DoesNotContain("Unknown", text);
    }

    private static PromotionDecision PassingDecision()
    {
        return new PromotionDecision(
            PromotionDecisionLabel.EnableAutonomousBeta,
            ["all_criteria_passed"],
            [new PromotionCriterion("window_days", true, PromotionCriterionClass.Liveness, "7", ">= 7", "ok")],
            Now,
            "decision=EnableAutonomousBeta, passes=1/1");
    }

    private sealed class PromotionFixture
    {
        private readonly string _root;

        private PromotionFixture(string root, AuditLedgerService ledger)
        {
            _root = root;
            Ledger = ledger;
            GoldenEvalPath = Path.Combine(root, "eval-report.json");
            Settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["pet_model_mode"] = "LocalOnly",
                [AutonomousOperationsConfig.EnabledSetting] = bool.FalseString,
                [KillSwitchService.KillSwitchSetting] = bool.FalseString
            };
            WriteGoldenEval(passed: true, citationCoverage: 0.9);
        }

        public AuditLedgerService Ledger { get; }

        public string GoldenEvalPath { get; }

        public Dictionary<string, string> Settings { get; }

        public static PromotionFixture Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "wevito-promotion-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new PromotionFixture(root, new AuditLedgerService(Path.Combine(root, "ledger.sqlite")));
        }

        public void SeedPassingRows(DateTimeOffset now, bool includeSelfImprovement = true, bool includeHeartbeats = true)
        {
            for (var day = 0; day < 7; day++)
            {
                var at = now.AddDays(-day).AddHours(-2);
                if (includeHeartbeats)
                {
                    Record("runtime_session_heartbeat", at, "heartbeat=true uptime_hours=8", "Completed");
                }

                Record("localDocs", at.AddMinutes(5), "activity", "Completed");
                if (includeSelfImprovement)
                {
                    Record(AuditLedgerService.SelfImprovementReportPacketKind, at.AddMinutes(10), "daily report", "Completed");
                }
            }

            Record("focus_steal_snapshot", now.AddMinutes(-30), "focus_steal=false day_delta=0 total=0", "Completed");
            Record("budget_meter_snapshot", now.AddMinutes(-25), "budget_exceeded=false budget_delta_pct=0", "Completed");
            Record("mutation_apply", now.AddMinutes(-20), "applied reviewed change", "Completed", mutate: true);
            Record("proof_packet", now.AddMinutes(-19), "post-proof passed", "Completed");
        }

        public void SeedSafetyFailure(string failure, DateTimeOffset now)
        {
            switch (failure)
            {
                case "hosted_ai":
                    Record("did_use_hosted_ai", now.AddMinutes(-5), "hosted call", "Completed", hostedAi: true);
                    break;
                case "policy":
                    Record("policy_block", now.AddMinutes(-5), "UnifiedPolicyService blocked read", "Blocked");
                    break;
                case "mutation_without_proof":
                    Record("mutation_apply", now.AddMinutes(-50), "no proof nearby", "Completed", mutate: true);
                    break;
                case "focus_steal":
                    Record("focus_steal_snapshot", now.AddMinutes(-5), "focus_steal=true", "Completed");
                    break;
                case "kill_switch_observed":
                    Record("kill_switch_user_initiated", now.AddMinutes(-5), "duration_hours>1", "Completed");
                    break;
            }
        }

        public void WriteGoldenEval(bool passed, double citationCoverage)
        {
            File.WriteAllText(GoldenEvalPath, JsonSerializer.Serialize(new
            {
                passed,
                citationCoverageRatio = citationCoverage
            }, JsonDefaults.Options));
        }

        public PromotionDecision Compute(DateTimeOffset since, DateTimeOffset until)
        {
            return new PromotionCriteriaSnapshot(Ledger).Compute(new PromotionCriteriaSnapshotRequest(
                since,
                until,
                Settings,
                GoldenEvalPath));
        }

        private void Record(string kind, DateTimeOffset at, string summary, string status, bool hostedAi = false, bool mutate = false)
        {
            Ledger.Record(new EvidencePacket(
                Guid.NewGuid(),
                kind,
                TaskCardId: null,
                at,
                DidUseNetwork: false,
                DidUseHostedAi: hostedAi,
                DidUseLocalModel: false,
                DidMutate: mutate,
                ArtifactPath: "artifact",
                summary,
                status));
        }
    }
}
