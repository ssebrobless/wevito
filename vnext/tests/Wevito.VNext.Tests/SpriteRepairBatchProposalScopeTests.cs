using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Experiments;

namespace Wevito.VNext.Tests;

public sealed class SpriteRepairBatchProposalScopeTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");

    [Fact]
    public void CompositionRoot_RegistersSpriteRepairBatchProposalExperimentKind()
    {
        var registry = ShellCompositionRoot.CreateExperimentRegistry();

        var descriptor = Assert.Single(registry.RegisteredKinds);
        Assert.Equal(SpriteRepairBatchProposalDescriptor.Kind, descriptor.Kind.Value);
        Assert.False(descriptor.EnabledByDefault);
    }

    [Fact]
    public void Scope_DefaultsOffUntilExplicitToggle()
    {
        var root = CreateSpriteRepairRepo();
        var queuePath = WriteSpriteRepairQueue(root, Row(root));
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var service = new AutonomousScopeService(ledger);

        var canRun = service.CanRunScope(
            AutonomousScopeService.KnownScopes.Single(scope => scope.ScopeId == AutonomousScopeService.SpriteRepairBatchProposalScopeId),
            Request(Settings(betaEnabled: true, proposalScopeEnabled: false), root, []),
            out var reason);

        Assert.False(canRun);
        Assert.Equal("autonomous_scope_sprite-repair-batch-proposal_enabled=false", reason);
    }

    [Fact]
    public void Scope_Tick_WritesFourReviewOnlyEvidencePacketsInOrder()
    {
        var root = CreateSpriteRepairRepo();
        var runtimeFrame = Path.Combine(root, "sprites_runtime", "snake", "baby", "female", "blue", "idle_00.png");
        File.WriteAllText(runtimeFrame, "before");
        var beforeHash = Sha256(runtimeFrame);
        var queuePath = WriteSpriteRepairQueue(root, Row(root, sourcePath: runtimeFrame));
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var scope = new SpriteRepairBatchProposalScope(queuePath, ledger);

        var result = scope.TryRun(Request(Settings(betaEnabled: true, proposalScopeEnabled: true), root, []));

        Assert.True(result.Ran);
        Assert.False(result.DidMutate);
        Assert.Equal(beforeHash, Sha256(runtimeFrame));
        var card = Assert.Single(result.TaskCards);
        Assert.Equal(SpriteRepairBatchProposalDescriptor.Kind, card.ToolFamily);
        Assert.False(card.PolicySnapshot?.IsEnabled);
        Assert.Contains("No sprite mutation", card.ResultSummary, StringComparison.OrdinalIgnoreCase);
        var rows = ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)).OrderBy(row => row.Id).ToList();
        Assert.Equal(
            [
                SelfImprovementPacketKinds.ProposalDrafted,
                SelfImprovementPacketKinds.ConstitutionalReviewed,
                SelfImprovementPacketKinds.DryRunCompleted,
                SelfImprovementPacketKinds.EvalCompleted
            ],
            rows.Select(row => row.PacketKind).ToArray());
        Assert.All(rows, row =>
        {
            Assert.False(row.DidMutate);
            Assert.False(row.DidUseNetwork);
            Assert.False(row.DidUseHostedAi);
            Assert.False(row.DidUseLocalModel);
        });
        Assert.DoesNotContain(rows, row => row.PacketKind.StartsWith("self_improvement_apply_", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Scope_EvalPacketMarksApplyOnlyGatesNotApplicable()
    {
        var root = CreateSpriteRepairRepo();
        var runtimeFrame = Path.Combine(root, "sprites_runtime", "snake", "baby", "female", "blue", "idle_00.png");
        File.WriteAllText(runtimeFrame, "before");
        var queuePath = WriteSpriteRepairQueue(root, Row(root, sourcePath: runtimeFrame));
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var scope = new SpriteRepairBatchProposalScope(queuePath, ledger);

        scope.TryRun(Request(Settings(betaEnabled: true, proposalScopeEnabled: true), root, []));

        var evalRow = Assert.Single(ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)), row => row.PacketKind == SelfImprovementPacketKinds.EvalCompleted);
        using var document = JsonDocument.Parse(File.ReadAllText(evalRow.ArtifactPath));
        Assert.Equal("NotApplicable", document.RootElement.GetProperty("results").GetProperty("Backup").GetProperty("status").GetString());
        Assert.Equal("review_only_v0", document.RootElement.GetProperty("results").GetProperty("Post-proof").GetProperty("reason").GetString());
        Assert.Equal("review_only_v0", document.RootElement.GetProperty("results").GetProperty("Rollback").GetProperty("reason").GetString());
    }

    private static AutonomousScopeRunRequest Request(
        IReadOnlyDictionary<string, string> settings,
        string artifactRoot,
        IReadOnlyList<TaskCard> cards)
    {
        return new AutonomousScopeRunRequest(settings, new RuntimeSupervisorStatus(RuntimeSupervisorMode.Active, true, true, false, "active", ""), artifactRoot, Now, cards);
    }

    private static Dictionary<string, string> Settings(bool betaEnabled, bool proposalScopeEnabled)
    {
        return new Dictionary<string, string>
        {
            [AutonomousOperationsConfig.EnabledSetting] = betaEnabled.ToString(),
            [AutonomousOperationsConfig.DailyCapSetting] = "3",
            [AutonomousOperationsConfig.IntervalMinutesSetting] = "10",
            [RuntimeSupervisorService.BackgroundWorkAllowedSetting] = bool.TrueString,
            [AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairBatchProposalScopeId)] = proposalScopeEnabled.ToString()
        };
    }

    private static string CreateSpriteRepairRepo()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-sprite-repair-batch-proposal", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "tools"));
        Directory.CreateDirectory(Path.Combine(root, "sprites_runtime", "snake", "baby", "female", "blue"));
        File.WriteAllText(Path.Combine(root, "wevito.godot"), "");
        File.WriteAllText(Path.Combine(root, "tools", "fake_repair.py"), "# fake repair");
        return root;
    }

    private static string WriteSpriteRepairQueue(string root, SpriteRepairQueueRow row)
    {
        var queuePath = Path.Combine(root, "repair_queue.json");
        var manifest = new SpriteRepairQueueManifest(
            "1.0",
            Now,
            "visual_qa_manifest.json",
            Now.AddMinutes(-30).ToString("O"),
            1,
            1,
            new Dictionary<string, int> { [row.Priority] = 1 },
            new Dictionary<string, int> { ["crop_detected"] = 1 },
            [row]);
        File.WriteAllText(queuePath, JsonSerializer.Serialize(manifest, JsonDefaults.Options));
        return queuePath;
    }

    private static SpriteRepairQueueRow Row(string root, string? sourcePath = null)
    {
        return new SpriteRepairQueueRow(
            "snake_baby_female",
            "snake",
            "baby",
            "female",
            "P1",
            "queued",
            1,
            ["blue"],
            ["idle"],
            ["tools/fake_repair.py"],
            [
                new SpriteRepairQueueIssue(
                    "blue",
                    "idle",
                    "P1",
                    ["crop_detected"],
                    ["test warning"],
                    "tools/fake_repair.py",
                    "test reason",
                    sourcePath ?? Path.Combine(root, "sprites_runtime", "snake", "baby", "female", "blue", "idle_00.png"),
                    null)
            ]);
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream)).ToLowerInvariant();
    }
}
