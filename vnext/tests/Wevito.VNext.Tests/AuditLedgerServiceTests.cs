using Microsoft.Data.Sqlite;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AuditLedgerServiceTests
{
    [Fact]
    public void Record_AppendsRowsWithRequiredFlags()
    {
        var ledger = new AuditLedgerService(Path.Combine(CreateTempRoot(), "ledger.sqlite"));
        var packetId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var id = ledger.Record(new EvidencePacket(
            packetId,
            "localDocs",
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            DateTimeOffset.Parse("2026-05-12T12:00:00Z"),
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "vnext/artifacts/pet-tasks/example",
            Summary: "Preview ready",
            Status: "PreviewReady"));

        var row = Assert.Single(ledger.Snapshot(
            DateTimeOffset.Parse("2026-05-12T00:00:00Z"),
            DateTimeOffset.Parse("2026-05-13T00:00:00Z")));
        Assert.True(id > 0);
        Assert.Equal(packetId, row.PacketId);
        Assert.False(row.DidUseNetwork);
        Assert.False(row.DidUseHostedAi);
        Assert.False(row.DidUseLocalModel);
        Assert.False(row.DidMutate);
    }

    [Fact]
    public void LedgerRejectsUpdatesAndDeletes()
    {
        var path = Path.Combine(CreateTempRoot(), "ledger.sqlite");
        var ledger = new AuditLedgerService(path);
        ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            "scheduler_proposal",
            null,
            DateTimeOffset.Parse("2026-05-12T12:00:00Z"),
            false,
            false,
            false,
            false,
            "artifact",
            "summary",
            "Draft"));

        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var update = connection.CreateCommand();
        update.CommandText = "UPDATE audit_ledger SET status = 'Changed' WHERE id = 1;";
        Assert.Throws<SqliteException>(() => update.ExecuteNonQuery());
        using var delete = connection.CreateCommand();
        delete.CommandText = "DELETE FROM audit_ledger WHERE id = 1;";
        Assert.Throws<SqliteException>(() => delete.ExecuteNonQuery());
    }

    [Fact]
    public void DispatcherWritesOneLedgerRowPerPreview()
    {
        var root = CreateTempRoot();
        var docsRoot = Path.Combine(root, "docs");
        Directory.CreateDirectory(docsRoot);
        File.WriteAllText(Path.Combine(docsRoot, "plan.md"), "local docs plan");
        var ledger = new AuditLedgerService(Path.Combine(root, "audit", "ledger.sqlite"));
        var dispatcher = new AgentToolDispatcher(auditLedgerService: ledger);

        var result = dispatcher.BuildPreview(BuildRequest("localDocs", TaskKind.SummarizeDocs, docsRoot, [docsRoot]));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        var row = Assert.Single(ledger.Snapshot(DateTimeOffset.MinValue, DateTimeOffset.MaxValue));
        Assert.Equal("localDocs", row.PacketKind);
        Assert.Equal(result.TaskCardId, row.TaskCardId);
        Assert.False(row.DidUseNetwork);
        Assert.False(row.DidUseHostedAi);
        Assert.False(row.DidMutate);
    }

    private static TaskAdapterRequest BuildRequest(
        string toolFamily,
        TaskKind taskKind,
        string approvedRoot,
        IReadOnlyList<string> targets)
    {
        var intent = new TaskIntent(
            Guid.Parse("80000000-0000-0000-0000-000000000001"),
            "run a preview",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: taskKind,
            RequestedToolFamily: toolFamily,
            TargetPathsOrAssets: targets);
        var policy = new ToolPolicy(
            toolFamily + "-readonly",
            toolFamily,
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None,
            ApprovedRootPaths: [approvedRoot]);

        return new TaskAdapterRequest(
            Guid.Parse("90000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            ArtifactRoot: Path.Combine(approvedRoot, "vnext", "artifacts", "pet-tasks", "20260512-120000-preview"));
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-audit-ledger-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
