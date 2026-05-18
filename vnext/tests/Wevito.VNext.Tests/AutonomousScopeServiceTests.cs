using System.Threading;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AutonomousScopeServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");

    [Fact]
    public void AutonomousScope_DefaultsOffUntilExplicitScopeToggle()
    {
        var root = TempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var service = new AutonomousScopeService(ledger);
        var request = Request(Settings(betaEnabled: true), root, []);

        var canRun = service.CanRunScope(AutonomousScopeService.KnownScopes[0], request, out var reason);

        Assert.False(canRun);
        Assert.Equal("autonomous_scope_sprite-repair-triage_enabled=false", reason);
    }

    [Fact]
    public void SpriteRepairTriageScope_DraftsReviewOnlyCardsForP0P1Rows()
    {
        var root = TempRoot();
        var queuePath = Path.Combine(root, "repair_queue.json");
        File.WriteAllText(queuePath, """
            {
              "rows": [
                { "rowId": "snake_baby_female", "speciesId": "snake", "lifeStage": "baby", "gender": "female", "priority": "P1" },
                { "rowId": "crow_adult_male", "speciesId": "crow", "lifeStage": "adult", "gender": "male", "priority": "P2" }
              ]
            }
            """);
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var scope = new SpriteRepairTriageScope(queuePath, ledger);

        var result = scope.TryRun(Request(Settings(betaEnabled: true, spriteScopeEnabled: true), root, []));

        Assert.True(result.Ran);
        var card = Assert.Single(result.TaskCards);
        Assert.Equal(TaskCardStatus.Draft, card.Status);
        Assert.Equal(SpriteRepairTriageScope.ToolFamily, card.ToolFamily);
        Assert.False(card.PolicySnapshot?.IsEnabled);
        Assert.True(card.Intent.NeedsApproval);
        Assert.False(result.DidMutate);
        var rows = ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1));
        Assert.Contains(rows, row => row.PacketKind == SpriteRepairTriageScope.PacketKind && !row.DidMutate);
    }

    [Fact]
    public void AuditLedgerCleanupScope_MovesOnlyOldArchivedJsonlWithoutDeletingOrEditing()
    {
        var root = TempRoot();
        var auditRoot = Path.Combine(root, "audit");
        Directory.CreateDirectory(auditRoot);
        var oldArchived = Path.Combine(auditRoot, "20260101-archived.jsonl");
        var liveLedger = Path.Combine(auditRoot, "ledger.jsonl");
        File.WriteAllText(oldArchived, "{\"packet\":\"old\"}");
        File.WriteAllText(liveLedger, "{\"packet\":\"live\"}");
        File.SetLastWriteTimeUtc(oldArchived, Now.AddDays(-45).UtcDateTime);
        File.SetLastWriteTimeUtc(liveLedger, Now.AddDays(-45).UtcDateTime);
        var originalHash = Sha256(oldArchived);
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var scope = new AuditLedgerCleanupScope(auditRoot, ledger);

        var result = scope.TryRun(Request(Settings(betaEnabled: true, cleanupScopeEnabled: true), root, []));

        Assert.True(result.Ran);
        Assert.True(result.DidMutate);
        Assert.False(File.Exists(oldArchived));
        Assert.True(File.Exists(liveLedger));
        var archivedPath = Path.Combine(auditRoot, "archive", "20260101-archived.jsonl");
        Assert.True(File.Exists(archivedPath));
        Assert.Equal(originalHash, Sha256(archivedPath));
        Assert.Equal("{\"packet\":\"live\"}", File.ReadAllText(liveLedger));
        var rows = ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1));
        Assert.Contains(rows, row => row.PacketKind == AuditLedgerCleanupScope.PacketKind && row.DidMutate);
    }

    [Fact]
    public async Task SpriteRepairTriageScope_Preview_DoesNotDraftCards()
    {
        var root = TempRoot();
        var queuePath = Path.Combine(root, "repair_queue.json");
        File.WriteAllText(queuePath, """
            {
              "rows": [
                { "rowId": "snake_baby_female", "speciesId": "snake", "lifeStage": "baby", "gender": "female", "priority": "P1" }
              ]
            }
            """);
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var scope = new SpriteRepairTriageScope(queuePath, ledger);

        var preview = await scope.DescribePlannedActionsAsync(CancellationToken.None);

        Assert.Equal(AutonomousScopeService.SpriteRepairTriageScopeId, preview.ScopeId);
        Assert.Equal(1, preview.ActionCount);
        Assert.False(preview.EvidenceFlags.DidMutate);
        Assert.Single(preview.PlannedItems);
        Assert.Empty(ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)));
    }

    [Fact]
    public async Task AuditLedgerCleanupScope_Preview_DoesNotMoveFiles()
    {
        var root = TempRoot();
        var auditRoot = Path.Combine(root, "audit");
        Directory.CreateDirectory(auditRoot);
        var oldArchived = Path.Combine(auditRoot, "20260101-archived.jsonl");
        File.WriteAllText(oldArchived, "{\"packet\":\"old\"}");
        File.SetLastWriteTimeUtc(oldArchived, DateTimeOffset.UtcNow.AddDays(-45).UtcDateTime);
        var originalHash = Sha256(oldArchived);
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var scope = new AuditLedgerCleanupScope(auditRoot, ledger);

        var preview = await scope.DescribePlannedActionsAsync(CancellationToken.None);

        Assert.Equal(1, preview.ActionCount);
        var item = Assert.Single(preview.PlannedItems);
        Assert.Equal(oldArchived, item.SourcePath);
        Assert.Equal(originalHash, item.Sha256);
        Assert.True(File.Exists(oldArchived));
        Assert.False(Directory.Exists(Path.Combine(auditRoot, "archive")));
        Assert.Empty(ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)));
    }

    [Fact]
    public async Task AutonomousScopeService_Preview_RespectsKillSwitch()
    {
        var root = TempRoot();
        var queuePath = Path.Combine(root, "repair_queue.json");
        File.WriteAllText(queuePath, """{ "rows": [ { "rowId": "snake", "priority": "P1" } ] }""");
        var settings = new Dictionary<string, string> { [KillSwitchService.KillSwitchSetting] = bool.TrueString };
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var service = new AutonomousScopeService(ledger, new KillSwitchService(() => settings, ledger));

        var preview = await service.PreviewAsync(
            AutonomousScopeService.SpriteRepairTriageScopeId,
            [new SpriteRepairTriageScope(queuePath, ledger)]);

        Assert.Equal("kill_switch=true", preview.BlockReason);
        Assert.Equal(0, preview.ActionCount);
        Assert.Empty(ledger.Snapshot(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1)));
    }

    [Fact]
    public async Task AutonomousScopeService_Preview_DoesNotChangePersistedFlags()
    {
        var root = TempRoot();
        var queuePath = Path.Combine(root, "repair_queue.json");
        File.WriteAllText(queuePath, """{ "rows": [ { "rowId": "snake", "priority": "P1" } ] }""");
        var settings = Settings(betaEnabled: false, spriteScopeEnabled: false, cleanupScopeEnabled: false);
        var before = settings.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var service = new AutonomousScopeService(ledger);

        var preview = await service.PreviewAsync(
            AutonomousScopeService.SpriteRepairTriageScopeId,
            [new SpriteRepairTriageScope(queuePath, ledger)]);

        Assert.Equal(1, preview.ActionCount);
        Assert.Equal(before, settings);
        var rows = ledger.Snapshot(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.Contains(rows, row => row.PacketKind == AutonomousScopeService.PreviewPacketKind && !row.DidMutate);
    }

    [Fact]
    public void AutonomousOperationsLoop_RunsEnabledSpriteRepairScopeAfterBetaGate()
    {
        var root = TempRoot();
        var queuePath = Path.Combine(root, "repair_queue.json");
        File.WriteAllText(queuePath, """
            { "rows": [
              { "rowId": "frog_baby_female", "speciesId": "frog", "lifeStage": "baby", "gender": "female", "priority": "P1" }
            ] }
            """);
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        SeedPassingRows(ledger);
        var scopeService = new AutonomousScopeService(ledger);
        var registry = new AutonomousScopeRegistry(scopeService, [new SpriteRepairTriageScope(queuePath, ledger)]);
        var loop = new AutonomousOperationsLoop(new AutonomousBetaDecisionService(ledger), ledger, scopeRegistry: registry);

        var settings = Settings(betaEnabled: true, spriteScopeEnabled: true);
        var result = loop.TryRunIteration(new AutonomousOperationsRequest(
            settings,
            ActiveStatus(),
            Path.Combine(root, "artifacts"),
            Now,
            []));

        Assert.True(result.Ran, result.BlockReason);
        Assert.False(result.DidMutate);
        Assert.Single(result.TaskCards ?? []);
        var rows = ledger.Snapshot(Now.AddHours(-1), Now.AddMinutes(1));
        Assert.Contains(rows, row => row.PacketKind == AutonomousScopeService.TickPacketKind);
        Assert.Contains(rows, row => row.PacketKind == SpriteRepairTriageScope.PacketKind);
    }

    [Theory]
    [InlineData(AutonomousScopeService.EnabledChangedPacketKind)]
    [InlineData(AutonomousScopeService.PreviewPacketKind)]
    [InlineData(AutonomousScopeService.TickPacketKind)]
    [InlineData(SpriteRepairTriageScope.PacketKind)]
    [InlineData(AuditLedgerCleanupScope.PacketKind)]
    public void PlainLanguageExplainer_CoversAutonomousScopePacketKinds(string packetKind)
    {
        var unknown = new List<string>();
        var explainer = new PlainLanguageExplainer(unknown.Add);

        var label = explainer.ExplainPacketKind(packetKind);

        Assert.DoesNotContain(packetKind, unknown);
        Assert.False(label.StartsWith("Unknown", StringComparison.OrdinalIgnoreCase), label);
        Assert.Contains(packetKind, PlainLanguageExplainer.KnownPacketKinds);
    }

    private static AutonomousScopeRunRequest Request(
        IReadOnlyDictionary<string, string> settings,
        string artifactRoot,
        IReadOnlyList<TaskCard> cards)
    {
        return new AutonomousScopeRunRequest(settings, ActiveStatus(), artifactRoot, Now, cards);
    }

    private static RuntimeSupervisorStatus ActiveStatus()
    {
        return new RuntimeSupervisorStatus(RuntimeSupervisorMode.Active, true, true, false, "active", "");
    }

    private static Dictionary<string, string> Settings(
        bool betaEnabled,
        bool spriteScopeEnabled = false,
        bool cleanupScopeEnabled = false)
    {
        return new Dictionary<string, string>
        {
            [AutonomousOperationsConfig.EnabledSetting] = betaEnabled.ToString(),
            [AutonomousOperationsConfig.DailyCapSetting] = "3",
            [AutonomousOperationsConfig.IntervalMinutesSetting] = "10",
            [RuntimeSupervisorService.BackgroundWorkAllowedSetting] = bool.TrueString,
            [AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairTriageScopeId)] = spriteScopeEnabled.ToString(),
            [AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.AuditLedgerCleanupScopeId)] = cleanupScopeEnabled.ToString()
        };
    }

    private static void SeedPassingRows(AuditLedgerService ledger)
    {
        ledger.Record(Packet("runtime_session_heartbeat", Now.AddHours(-2), "runtime_session uptime_hours=4 uptime_hours>=4 heartbeat=true", "Completed"));
        ledger.Record(Packet("focus_steal_snapshot", Now.AddHours(-1), "focus_steal=false day_delta=0 total=0", "Completed"));
        ledger.Record(Packet("budget_meter_snapshot", Now.AddHours(-1), "budget_exceeded=false used_this_hour=0 max_this_hour=4", "Completed"));
        ledger.Record(Packet("localDocs", Now.AddHours(-1), "preview", "PreviewReady"));
        ledger.Record(Packet("mutation_apply", Now.AddMinutes(-40), "post-proof passed", "Completed", mutate: true));
        ledger.Record(Packet("proof_packet", Now.AddMinutes(-39), "post-proof passed", "Completed"));
    }

    private static EvidencePacket Packet(string kind, DateTimeOffset createdAt, string summary, string status, bool mutate = false)
    {
        return new EvidencePacket(Guid.NewGuid(), kind, null, createdAt, false, false, false, mutate, "artifact", summary, status);
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string TempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-autonomous-scope-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
