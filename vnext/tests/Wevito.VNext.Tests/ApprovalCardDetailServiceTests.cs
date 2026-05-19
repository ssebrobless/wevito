using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;

namespace Wevito.VNext.Tests;

public sealed class ApprovalCardDetailServiceTests
{
    [Fact]
    public void BuildFor_ReturnsInputHashesScopeHashAndExpectedChain()
    {
        var fixture = ApprovalFixture.Create();
        var service = new ApprovalCardDetailService(
            fixture.DatabasePath,
            sha256Resolver: path => $"sha:{Path.GetFileName(path)}");

        var detail = service.BuildFor(fixture.TaskCardId);

        Assert.False(detail.Blocked);
        Assert.Equal(fixture.OperationId, detail.OperationId);
        Assert.Equal(AutonomousScopeService.SpriteRepairBatchProposalScopeId, detail.ScopeId);
        Assert.Equal("scope-hash-123", detail.ScopeHash);
        Assert.Equal(ApprovalCardDetail.SafetyCopyText, detail.SafetyCopy);
        Assert.Equal(Path.GetFullPath(fixture.ArtifactPath), detail.ArtifactJsonPath);
        Assert.Collection(
            detail.InputFiles,
            file =>
            {
                Assert.Equal("proposal", file.Role);
                Assert.Equal("sha:proposal.json", file.Sha256);
            },
            file =>
            {
                Assert.Equal("dry_run", file.Role);
                Assert.Equal("sha:dry-run.json", file.Sha256);
            },
            file =>
            {
                Assert.Equal("eval", file.Role);
                Assert.Equal("sha:eval.json", file.Sha256);
            });
        Assert.Equal(ExpectedPacketChain(), detail.ExpectedPacketChain);
    }

    [Fact]
    public void BuildFor_KillSwitchActive_ReturnsBlockedWithoutData()
    {
        var fixture = ApprovalFixture.Create();
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        };
        var service = new ApprovalCardDetailService(
            fixture.DatabasePath,
            new KillSwitchService(() => settings),
            path => throw new InvalidOperationException($"Should not hash {path}."));

        var detail = service.BuildFor(fixture.TaskCardId);

        Assert.True(detail.Blocked);
        Assert.Equal("kill_switch=true", detail.BlockedReason);
        Assert.Empty(detail.InputFiles);
    }

    [Fact]
    public void BuildFor_RejectsArtifactPathOutsideVNextArtifacts()
    {
        var fixture = ApprovalFixture.Create(artifactOutsideAllowedRoot: true);
        var service = new ApprovalCardDetailService(fixture.DatabasePath);

        var detail = service.BuildFor(fixture.TaskCardId);

        Assert.True(detail.Blocked);
        Assert.Equal("artifact_path_outside_allowed_root", detail.BlockedReason);
        Assert.DoesNotContain("outside", detail.ArtifactJsonPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildFor_NonexistentTaskCard_ReturnsBlocked()
    {
        var fixture = ApprovalFixture.Create();
        var service = new ApprovalCardDetailService(fixture.DatabasePath);

        var detail = service.BuildFor(Guid.NewGuid());

        Assert.True(detail.Blocked);
        Assert.Equal("apply_awaiting_approval_row_not_found", detail.BlockedReason);
    }

    [Fact]
    public void BuildFor_UsesReadOnlySqlAndWritesNoPackets()
    {
        var fixture = ApprovalFixture.Create();
        var commands = new List<string>();
        var service = new ApprovalCardDetailService(fixture.DatabasePath, commandObserver: commands.Add);

        _ = service.BuildFor(fixture.TaskCardId);

        Assert.All(commands, command =>
        {
            Assert.DoesNotContain("INSERT", command, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UPDATE", command, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE", command, StringComparison.OrdinalIgnoreCase);
        });
        var source = File.ReadAllText(SourcePath("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "ApprovalCardDetailService.cs"));
        Assert.DoesNotContain(".Record(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IHeldOutEvalStore", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ToolPopupDetailPanelIsReadOnlyAndDoesNotExposeExtraButtons()
    {
        var xaml = File.ReadAllText(SourcePath("vnext", "src", "Wevito.VNext.Shell", "ToolPopupWindow.xaml"));
        var start = xaml.IndexOf("AutomationId=\"SupervisedApplyDetailExpander\"", StringComparison.Ordinal);
        Assert.True(start >= 0, "SupervisedApplyDetailExpander was not found.");
        var end = xaml.IndexOf("SupervisedApplyOperationTextBox", start, StringComparison.Ordinal);
        Assert.True(end > start, "Detail expander should appear before the approval text box.");
        var section = xaml[start..end];

        Assert.DoesNotContain("<Button", section, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConfirmationText", section, StringComparison.Ordinal);
    }

    private static string SourcePath(params string[] parts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not locate source file: {Path.Combine(parts)}");
    }

    private static IReadOnlyList<string> ExpectedPacketChain()
    {
        return
        [
            SelfImprovementPacketKinds.ProposalDrafted,
            SelfImprovementPacketKinds.ConstitutionalReviewed,
            SelfImprovementPacketKinds.DryRunCompleted,
            SelfImprovementPacketKinds.EvalCompleted,
            SelfImprovementPacketKinds.ApplyAwaitingApproval,
            SelfImprovementPacketKinds.ApplyRefused,
            SelfImprovementPacketKinds.ApplyCompleted,
            SelfImprovementPacketKinds.RollbackVerified,
            SelfImprovementPacketKinds.MaturityClockReset
        ];
    }

    private sealed record ApprovalFixture(
        string DatabasePath,
        Guid TaskCardId,
        string OperationId,
        string ArtifactPath)
    {
        public static ApprovalFixture Create(bool artifactOutsideAllowedRoot = false)
        {
            var root = Path.Combine(Path.GetTempPath(), "wevito-approval-card-detail-tests", Guid.NewGuid().ToString("N"));
            var databasePath = Path.Combine(root, "ledger.sqlite");
            var taskCardId = Guid.NewGuid();
            var operationId = $"apply-{Guid.NewGuid():N}";
            var artifactsRoot = Path.Combine(root, "vnext", "artifacts", "supervised-improvement-pilot", operationId);
            var outsideRoot = Path.Combine(root, "outside");
            Directory.CreateDirectory(artifactsRoot);
            Directory.CreateDirectory(outsideRoot);
            var proposalPath = WriteJson(artifactsRoot, "proposal.json");
            var dryRunPath = WriteJson(artifactsRoot, "dry-run.json");
            var evalPath = WriteJson(artifactsRoot, "eval.json");
            var artifactPath = artifactOutsideAllowedRoot
                ? Path.Combine(outsideRoot, "apply-awaiting-approval.json")
                : Path.Combine(artifactsRoot, "apply-awaiting-approval.json");
            File.WriteAllText(artifactPath, JsonSerializer.Serialize(new
            {
                schemaVersion = "1",
                packetKind = SelfImprovementPacketKinds.ApplyAwaitingApproval,
                scopeId = AutonomousScopeService.SpriteRepairBatchProposalScopeId,
                operationId,
                scopeHash = "scope-hash-123",
                proposalPath,
                dryRunPath,
                evalPath
            }, JsonDefaults.Options));

            var ledger = new AuditLedgerService(databasePath);
            ledger.Record(new EvidencePacket(
                Guid.NewGuid(),
                SelfImprovementPacketKinds.ApplyAwaitingApproval,
                taskCardId,
                DateTimeOffset.Parse("2026-05-18T12:00:00Z"),
                DidUseNetwork: false,
                DidUseHostedAi: false,
                DidUseLocalModel: false,
                DidMutate: false,
                ArtifactPath: artifactPath,
                Summary: $"Awaiting approval for {operationId}.",
                Status: "WaitingForApproval"));

            return new ApprovalFixture(databasePath, taskCardId, operationId, artifactPath);
        }

        private static string WriteJson(string root, string fileName)
        {
            var path = Path.Combine(root, fileName);
            File.WriteAllText(path, "{}");
            return path;
        }
    }
}
