using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Apply;

namespace Wevito.VNext.Tests;

public sealed class ApplyRunnerActivityServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-19T12:00:00Z");

    [Fact]
    public void ReadRecent_ReturnsEmpty_WhenLedgerHasNoApplyV0Rows()
    {
        var path = CreateDatabasePath();
        var ledger = new AuditLedgerService(path);
        ledger.Record(Packet("petState", Now, "operation_id=ignored", didMutate: false));

        var service = new ApplyRunnerActivityService(path, KillSwitch(false));

        Assert.Empty(service.ReadRecent(20));
    }

    [Fact]
    public void ReadRecent_ReturnsOneGroupPerOperation_WithPacketsInTimestampOrder()
    {
        var path = CreateDatabasePath();
        var ledger = new AuditLedgerService(path);
        ledger.Record(Packet(SelfImprovementPacketKinds.ApplyV0Completed, Now.AddMinutes(2), "operation_id=op-b scope_id=scope-b scope_hash=bbbb post_sha256=post-b", didMutate: true));
        ledger.Record(Packet(SelfImprovementPacketKinds.ApplyV0DryRunStarted, Now, "operation_id=op-a scope_id=scope-a scope_hash=aaaa source=op-a/scope-a/sample.draft.json pre_sha256=pre-a", didMutate: false));
        ledger.Record(Packet(SelfImprovementPacketKinds.ApplyV0Completed, Now.AddMinutes(1), "operation_id=op-a scope_id=scope-a scope_hash=aaaa approved_path=op-a/scope-a/sample.approved.json post_sha256=post-a", didMutate: true));

        var service = new ApplyRunnerActivityService(path, KillSwitch(false));

        var entries = service.ReadRecent(20);

        Assert.Equal(2, entries.Count);
        Assert.Equal("op-b", entries[0].OperationId);
        Assert.Equal("op-a", entries[1].OperationId);
        Assert.Equal(
            [SelfImprovementPacketKinds.ApplyV0DryRunStarted, SelfImprovementPacketKinds.ApplyV0Completed],
            entries[1].Packets.Select(packet => packet.PacketKind).ToArray());
        Assert.Equal("scope-a", entries[1].ScopeId);
        Assert.Equal("aaaa", entries[1].ScopeHash);
    }

    [Fact]
    public void ReadRecent_MarksSucceeded_WhenGroupEndsWithCompleted()
    {
        var entry = SingleEntry(SelfImprovementPacketKinds.ApplyV0Completed);

        Assert.Equal(ApplyRunnerActivityDisposition.Succeeded, entry.Disposition);
    }

    [Fact]
    public void ReadRecent_MarksRolledBack_WhenGroupEndsWithRolledBack()
    {
        var entry = SingleEntry(SelfImprovementPacketKinds.ApplyV0RolledBack);

        Assert.Equal(ApplyRunnerActivityDisposition.RolledBack, entry.Disposition);
    }

    [Fact]
    public void ReadRecent_MarksInProgress_WhenGroupEndsWithNonTerminalPacket()
    {
        var entry = SingleEntry(SelfImprovementPacketKinds.ApplyV0Applied);

        Assert.Equal(ApplyRunnerActivityDisposition.InProgress, entry.Disposition);
    }

    [Fact]
    public void ReadRecent_ReturnsEmpty_WhenKillSwitchActive()
    {
        var path = CreateDatabasePath();
        var ledger = new AuditLedgerService(path);
        ledger.Record(Packet(SelfImprovementPacketKinds.ApplyV0Completed, Now, "operation_id=op-a scope_id=scope-a", didMutate: true));
        var service = new ApplyRunnerActivityService(path, KillSwitch(true));

        Assert.Empty(service.ReadRecent(20));
    }

    [Fact]
    public void Source_DoesNotContainWriteSql()
    {
        var text = File.ReadAllText(SourcePath());

        Assert.DoesNotContain("INSERT", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Source_UsesReadOnlySqliteConnection()
    {
        var text = File.ReadAllText(SourcePath());

        Assert.Contains("Mode = SqliteOpenMode.ReadOnly", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ToolPopupExpander_IsReadOnlyAndDoesNotExposeMutatingControls()
    {
        var xaml = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "Wevito.VNext.Shell",
            "ToolPopupWindow.xaml")));
        var start = xaml.IndexOf("Apply-runner activity (read-only)", StringComparison.Ordinal);
        Assert.True(start >= 0, "Apply-runner activity expander must exist.");
        var end = xaml.IndexOf("<Expander Header=\"Local runtime readiness", start, StringComparison.Ordinal);
        Assert.True(end > start, "Apply-runner activity expander should sit before local runtime readiness.");
        var segment = xaml[start..end];

        Assert.DoesNotContain("<Button", segment, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ToggleSwitch", segment, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<CheckBox", segment, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MenuItem", segment, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToolPopupCodeBehind_DoesNotInvokeRenameRunnerApply()
    {
        var text = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "Wevito.VNext.Shell",
            "ToolPopupWindow.xaml.cs")));

        Assert.DoesNotContain("ArtifactRenameApplyRunner", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ArtifactRenameApplyRunner.Apply", text, StringComparison.Ordinal);
    }

    private static ApplyRunnerActivityEntry SingleEntry(string terminalPacketKind)
    {
        var path = CreateDatabasePath();
        var ledger = new AuditLedgerService(path);
        ledger.Record(Packet(SelfImprovementPacketKinds.ApplyV0DryRunStarted, Now, "operation_id=op-a scope_id=scope-a scope_hash=hash-a", didMutate: false));
        ledger.Record(Packet(terminalPacketKind, Now.AddMinutes(1), "operation_id=op-a scope_id=scope-a scope_hash=hash-a", didMutate: true));
        var service = new ApplyRunnerActivityService(path, KillSwitch(false));
        return Assert.Single(service.ReadRecent(20));
    }

    private static EvidencePacket Packet(string kind, DateTimeOffset createdAt, string summary, bool didMutate)
    {
        return new EvidencePacket(
            Guid.NewGuid(),
            kind,
            null,
            createdAt,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: didMutate,
            ArtifactPath: "",
            Summary: summary,
            Status: "Completed");
    }

    private static KillSwitchService KillSwitch(bool active)
    {
        return new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = active.ToString()
        });
    }

    private static string CreateDatabasePath()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-apply-runner-activity-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return Path.Combine(root, "ledger.sqlite");
    }

    private static string SourcePath()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "Wevito.VNext.Core",
            "SelfImprovement",
            "Apply",
            "ApplyRunnerActivityService.cs"));
    }
}
