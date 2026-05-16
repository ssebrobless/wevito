using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AutonomousOperationsLoopTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-12T12:00:00Z");

    [Theory]
    [InlineData(false, RuntimeSupervisorMode.Active, true, false)]
    [InlineData(true, RuntimeSupervisorMode.Quiet, false, false)]
    [InlineData(true, RuntimeSupervisorMode.PetOnly, false, false)]
    [InlineData(true, RuntimeSupervisorMode.Active, false, false)]
    [InlineData(true, RuntimeSupervisorMode.Active, true, true)]
    public void TryRunIteration_DormantWhenAnyGateFails(bool enabled, RuntimeSupervisorMode mode, bool backgroundAllowed, bool killSwitch)
    {
        var harness = BuildHarness(seedPassingRows: true);
        var settings = Settings(enabled);
        if (killSwitch)
        {
            settings[KillSwitchService.KillSwitchSetting] = bool.TrueString;
        }

        var result = harness.Loop.TryRunIteration(Request(settings, mode, harness.ArtifactRoot, backgroundAllowed));

        Assert.False(result.Ran);
        Assert.False(result.DidMutate);
        Assert.True(string.IsNullOrWhiteSpace(result.ArtifactFolder));
    }

    [Fact]
    public void TryRunIteration_DormantWhenBetaDecisionDoesNotEnable()
    {
        var harness = BuildHarness(seedPassingRows: false);

        var result = harness.Loop.TryRunIteration(Request(Settings(enabled: true), RuntimeSupervisorMode.Active, harness.ArtifactRoot));

        Assert.False(result.Ran);
        Assert.Contains("no ledger history", result.BlockReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryRunIteration_NeverExecutesMutationDirectly()
    {
        var harness = BuildHarness(seedPassingRows: true);

        var result = harness.Loop.TryRunIteration(Request(Settings(enabled: true), RuntimeSupervisorMode.Active, harness.ArtifactRoot));

        Assert.True(result.Ran, result.BlockReason);
        Assert.False(result.DidMutate);
        Assert.True(File.Exists(Path.Combine(result.ArtifactFolder, "autonomous-operations.json")));
        Assert.True(File.Exists(Path.Combine(result.ArtifactFolder, "run-summary.md")));
        Assert.Contains("Mutation apply: false", File.ReadAllText(Path.Combine(result.ArtifactFolder, "run-summary.md")));

        var rows = harness.Ledger.Snapshot(Now.AddHours(-1), Now.AddMinutes(1));
        var row = Assert.Single(rows, ledgerRow => ledgerRow.PacketKind == AutonomousOperationsLoop.PacketKind);
        Assert.False(row.DidMutate);
        Assert.False(row.DidUseNetwork);
        Assert.False(row.DidUseHostedAi);
        Assert.Contains("mutation_apply=false", row.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryRunIteration_EnforcesDailyCap()
    {
        var harness = BuildHarness(seedPassingRows: true);
        harness.Ledger.Record(Packet(AutonomousOperationsLoop.PacketKind, Now.AddMinutes(-15), "Autonomous operations beta completed one proposal-only iteration; mutation_apply=false.", "Completed"));

        var settings = Settings(enabled: true);
        settings[AutonomousOperationsConfig.DailyCapSetting] = "1";
        var result = harness.Loop.TryRunIteration(Request(settings, RuntimeSupervisorMode.Active, harness.ArtifactRoot));

        Assert.False(result.Ran);
        Assert.Contains("daily cap", result.BlockReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryRunIteration_KillSwitchHaltsBeforeIteration()
    {
        var harness = BuildHarness(seedPassingRows: true, killSwitchActive: true);

        var result = harness.Loop.TryRunIteration(Request(Settings(enabled: true), RuntimeSupervisorMode.Active, harness.ArtifactRoot));

        Assert.False(result.Ran);
        Assert.Equal("kill_switch=true", result.BlockReason);
    }

    [Fact]
    public void TryRunIteration_BlocksDuringGodotPetInteractionWindow()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-autonomous-loop-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        SeedPassingRows(ledger);
        var interaction = new UserInteractingWithPetState(ledger);
        interaction.EnterFromGodotPetInput(Now, "pointer_down");
        var loop = new AutonomousOperationsLoop(
            new AutonomousBetaDecisionService(ledger),
            ledger,
            userInteractingWithPetState: interaction);

        var result = loop.TryRunIteration(Request(Settings(enabled: true), RuntimeSupervisorMode.Active, Path.Combine(root, "artifacts")));

        Assert.False(result.Ran);
        Assert.Equal("user_interacting_with_pet=true", result.BlockReason);
    }

    private static Harness BuildHarness(bool seedPassingRows, bool killSwitchActive = false)
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-autonomous-loop-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        if (seedPassingRows)
        {
            SeedPassingRows(ledger);
        }

        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>
        {
            [KillSwitchService.KillSwitchSetting] = killSwitchActive.ToString()
        });
        var decisionService = new AutonomousBetaDecisionService(ledger);
        return new Harness(
            new AutonomousOperationsLoop(decisionService, ledger, killSwitch),
            ledger,
            Path.Combine(root, "artifacts"));
    }

    private static Dictionary<string, string> Settings(bool enabled)
    {
        return new Dictionary<string, string>
        {
            [AutonomousOperationsConfig.EnabledSetting] = enabled.ToString(),
            [AutonomousOperationsConfig.DailyCapSetting] = "3",
            [AutonomousOperationsConfig.IntervalMinutesSetting] = "10",
            [RuntimeSupervisorService.BackgroundWorkAllowedSetting] = bool.TrueString
        };
    }

    private static AutonomousOperationsRequest Request(
        IReadOnlyDictionary<string, string> settings,
        RuntimeSupervisorMode mode,
        string artifactRoot,
        bool backgroundAllowed = true)
    {
        return new AutonomousOperationsRequest(
            settings,
            new RuntimeSupervisorStatus(mode, backgroundAllowed, true, false, "status", ""),
            artifactRoot,
            Now);
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

    private static EvidencePacket Packet(
        string kind,
        DateTimeOffset createdAt,
        string summary,
        string status,
        bool mutate = false)
    {
        return new EvidencePacket(
            Guid.NewGuid(),
            kind,
            null,
            createdAt,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: mutate,
            ArtifactPath: "artifact",
            summary,
            status);
    }

    private sealed record Harness(AutonomousOperationsLoop Loop, AuditLedgerService Ledger, string ArtifactRoot);
}
