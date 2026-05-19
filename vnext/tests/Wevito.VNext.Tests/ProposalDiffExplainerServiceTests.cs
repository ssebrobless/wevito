using System.Text.Json;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Eval;

namespace Wevito.VNext.Tests;

public sealed class ProposalDiffExplainerServiceTests
{
    [Fact]
    public void Explain_HappyPath_ReturnsStructuredProposalFields()
    {
        var fixture = CreateFixture();
        var operationId = "apply-proposal-diff-001";
        var taskCardId = Guid.NewGuid();
        var artifacts = WriteArtifacts(fixture.ArtifactRoot, operationId);
        new AuditLedgerService(fixture.DatabasePath).Record(new EvidencePacket(
            Guid.NewGuid(),
            SelfImprovementPacketKinds.ApplyAwaitingApproval,
            taskCardId,
            fixture.Start,
            false,
            false,
            false,
            false,
            artifacts.AwaitingPath,
            $"Supervised self-improvement proposal is awaiting explicit user approval for operation {operationId}.",
            "WaitingForApproval"));

        var explanation = new ProposalDiffExplainerService(fixture.DatabasePath).Explain(operationId);

        Assert.False(explanation.IsBlocked);
        Assert.Equal(operationId, explanation.OperationId);
        Assert.Equal(["sprites_runtime/snake/baby/female/blue/walk_00.png"], explanation.SourcePaths);
        Assert.Equal(["spriteAudit", "spriteWorkflowV2"], explanation.Tools);
        Assert.Equal(0, explanation.DryRunMutationCount);
        Assert.Equal("Passed", explanation.EvalGateStatuses["Build"]);
        Assert.Equal("NotApplicable", explanation.EvalGateStatuses["Held-out eval"]);
        Assert.Equal(new string('a', 64), explanation.ScopeHash);
        Assert.Equal(new string('b', 64), explanation.ManifestHash);
    }

    [Fact]
    public void Explain_KillSwitchActive_BlocksBeforeSql()
    {
        var fixture = CreateFixture();
        var sqlCount = 0;
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        };
        var explanation = new ProposalDiffExplainerService(
            fixture.DatabasePath,
            new KillSwitchService(() => settings),
            _ => sqlCount++).Explain("operation");

        Assert.True(explanation.IsBlocked);
        Assert.Equal("kill_switch=true", explanation.BlockReason);
        Assert.Equal(0, sqlCount);
    }

    [Fact]
    public void Explain_EmptyOperationId_Blocks()
    {
        var fixture = CreateFixture();
        var explanation = new ProposalDiffExplainerService(fixture.DatabasePath).Explain(" ");

        Assert.True(explanation.IsBlocked);
        Assert.Equal("missing_input", explanation.BlockReason);
    }

    [Fact]
    public void Explain_OutOfArtifactsPath_Blocks()
    {
        var fixture = CreateFixture();
        var operationId = "apply-outside-artifact";
        var outside = Path.Combine(fixture.Root, "outside.json");
        File.WriteAllText(outside, "{}");
        new AuditLedgerService(fixture.DatabasePath).Record(new EvidencePacket(
            Guid.NewGuid(),
            SelfImprovementPacketKinds.ApplyAwaitingApproval,
            Guid.NewGuid(),
            fixture.Start,
            false,
            false,
            false,
            false,
            outside,
            $"awaiting operation {operationId}",
            "WaitingForApproval"));

        var explanation = new ProposalDiffExplainerService(fixture.DatabasePath).Explain(operationId);

        Assert.True(explanation.IsBlocked);
        Assert.Equal("artifact_path_outside_allowed_root", explanation.BlockReason);
    }

    [Fact]
    public void Explain_MissingAwaitingApprovalRow_Blocks()
    {
        var fixture = CreateFixture();
        new AuditLedgerService(fixture.DatabasePath).Record(new EvidencePacket(
            Guid.NewGuid(),
            SelfImprovementPacketKinds.ProposalDrafted,
            Guid.NewGuid(),
            fixture.Start,
            false,
            false,
            false,
            false,
            "",
            "not awaiting approval",
            "Drafted"));

        var explanation = new ProposalDiffExplainerService(fixture.DatabasePath).Explain("missing-operation");

        Assert.True(explanation.IsBlocked);
        Assert.Equal("apply_awaiting_approval_row_not_found", explanation.BlockReason);
    }

    [Fact]
    public void Source_IsReadOnlyAndDoesNotDependOnHeldOutStoresOrAuditWrites()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "ProposalDiffExplainerService.cs"));

        Assert.Contains("Mode = SqliteOpenMode.ReadOnly", source, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".Record(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IHeldOutEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HeldOutEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IInDistributionEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InDistributionEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WebRequest", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Type_DoesNotTakeHeldOutOrInDistributionStores()
    {
        var forbidden = new[]
        {
            typeof(IHeldOutEvalStore),
            typeof(HeldOutEvalStore),
            typeof(IInDistributionEvalStore),
            typeof(InDistributionEvalStore)
        };
        var constructorTypes = typeof(ProposalDiffExplainerService)
            .GetConstructors()
            .SelectMany(ctor => ctor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.DoesNotContain(constructorTypes, type => forbidden.Contains(type));
    }

    private static ProposalDiffArtifacts WriteArtifacts(string artifactRoot, string operationId)
    {
        var root = Path.Combine(artifactRoot, "proposal-diff-tests", operationId);
        Directory.CreateDirectory(root);
        var proposalPath = Path.Combine(root, "proposal.json");
        var dryRunPath = Path.Combine(root, "dry-run.json");
        var evalPath = Path.Combine(root, "eval.json");
        var awaitingPath = Path.Combine(root, "apply-awaiting-approval.json");
        File.WriteAllText(proposalPath, JsonSerializer.Serialize(new
        {
            sourcePaths = new[] { "sprites_runtime/snake/baby/female/blue/walk_00.png" },
            tools = new[] { "spriteAudit", "spriteWorkflowV2" },
            didMutate = false
        }));
        File.WriteAllText(dryRunPath, JsonSerializer.Serialize(new
        {
            mutations = 0,
            didMutate = false
        }));
        File.WriteAllText(evalPath, JsonSerializer.Serialize(new
        {
            results = new Dictionary<string, object>
            {
                ["Build"] = new { status = "Passed", reason = "" },
                ["Held-out eval"] = new { status = "NotApplicable", reason = "not_wired_in_v1" }
            },
            didMutate = false
        }));
        File.WriteAllText(awaitingPath, JsonSerializer.Serialize(new
        {
            operationId,
            proposalPath,
            dryRunPath,
            evalPath,
            scopeHash = new string('a', 64),
            experimentManifestHash = new string('b', 64),
            didMutate = false
        }));
        return new ProposalDiffArtifacts(awaitingPath);
    }

    private static ProposalDiffFixture CreateFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-proposal-diff-tests", Guid.NewGuid().ToString("N"));
        var artifactRoot = Path.Combine(root, "vnext", "artifacts");
        Directory.CreateDirectory(artifactRoot);
        return new ProposalDiffFixture(
            root,
            artifactRoot,
            Path.Combine(root, "audit", "ledger.sqlite"),
            new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "wevito.godot")) ||
                Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }

    private sealed record ProposalDiffFixture(string Root, string ArtifactRoot, string DatabasePath, DateTimeOffset Start);

    private sealed record ProposalDiffArtifacts(string AwaitingPath);
}
