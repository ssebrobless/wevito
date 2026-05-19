using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Experiments;
using Wevito.VNext.Core.SelfImprovement.Replay;

namespace Wevito.VNext.Tests;

public sealed class ReplayHarnessTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");

    [Fact]
    public void CapturedTick_ReplaysIdentically()
    {
        var fixture = CreateFixture();
        var capture = Capture(fixture.Scope, fixture.Root, "seed-001");
        var harness = new ReplayHarness(fixture.Scope);

        var result = harness.Replay(capture);

        var identical = Assert.IsType<ReplayComparisonResult.Identical>(result);
        Assert.Equal(capture.Packets.Count, identical.PacketCount);
    }

    [Fact]
    public void Replay_DivergesWhenNonRedactedFieldChanges()
    {
        var fixture = CreateFixture();
        var capture = Capture(fixture.Scope, fixture.Root, "seed-002");
        var mutatedPackets = capture.Packets
            .Select((packet, index) => index == 0 ? packet with { Summary = "changed summary" } : packet)
            .ToArray();
        var mutatedCapture = capture with { Packets = mutatedPackets };
        var harness = new ReplayHarness(fixture.Scope);

        var result = harness.Replay(mutatedCapture);

        var diverged = Assert.IsType<ReplayComparisonResult.Diverged>(result);
        Assert.NotEmpty(diverged.Diffs);
        Assert.Contains("changed summary", diverged.Diffs[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Replay_KillSwitchActive_ReturnsNotApplicable()
    {
        var fixture = CreateFixture();
        var capture = Capture(fixture.Scope, fixture.Root, "seed-003");
        var settings = new Dictionary<string, string> { [KillSwitchService.KillSwitchSetting] = bool.TrueString };
        var harness = new ReplayHarness(fixture.Scope, new KillSwitchService(() => settings));

        var result = harness.Replay(capture);

        var notApplicable = Assert.IsType<ReplayComparisonResult.NotApplicable>(result);
        Assert.Equal("kill_switch=true", notApplicable.Reason);
    }

    [Fact]
    public void Replay_WritesZeroPacketsToProductionLedger()
    {
        var fixture = CreateFixture();
        var capture = Capture(fixture.Scope, fixture.Root, "seed-004");
        var harness = new ReplayHarness(fixture.Scope);

        harness.Replay(capture);

        Assert.Empty(fixture.Ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)));
    }

    private static ReplayCapture Capture(SpriteRepairBatchProposalScope scope, string artifactRoot, string seed)
    {
        var packets = new List<EvidencePacket>();
        var result = scope.TryRun(Request(artifactRoot), seed, packets.Add);
        Assert.True(result.Ran);
        Assert.Equal(
            [
                SelfImprovementPacketKinds.ProposalDrafted,
                SelfImprovementPacketKinds.ConstitutionalReviewed,
                SelfImprovementPacketKinds.DryRunCompleted,
                SelfImprovementPacketKinds.EvalCompleted
            ],
            packets.Select(packet => packet.PacketKind).ToArray());
        return new ReplayCapture(scope.Descriptor.ScopeId, $"operation-{seed}", seed, Now, packets);
    }

    private static ReplayFixture CreateFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-replay-harness-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "tools"));
        Directory.CreateDirectory(Path.Combine(root, "sprites_runtime", "snake", "baby", "female", "blue"));
        File.WriteAllText(Path.Combine(root, "wevito.godot"), "");
        File.WriteAllText(Path.Combine(root, "tools", "fake_repair.py"), "# fake repair");
        File.WriteAllText(Path.Combine(root, "sprites_runtime", "snake", "baby", "female", "blue", "idle_00.png"), "before");
        var queuePath = WriteSpriteRepairQueue(root, Row(root));
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        return new ReplayFixture(root, ledger, new SpriteRepairBatchProposalScope(queuePath, ledger));
    }

    private static AutonomousScopeRunRequest Request(string artifactRoot)
    {
        return new AutonomousScopeRunRequest(
            Settings(),
            new RuntimeSupervisorStatus(RuntimeSupervisorMode.Active, true, true, false, "active", ""),
            artifactRoot,
            Now,
            []);
    }

    private static Dictionary<string, string> Settings()
    {
        return new Dictionary<string, string>
        {
            [AutonomousOperationsConfig.EnabledSetting] = bool.TrueString,
            [AutonomousOperationsConfig.DailyCapSetting] = "3",
            [AutonomousOperationsConfig.IntervalMinutesSetting] = "10",
            [RuntimeSupervisorService.BackgroundWorkAllowedSetting] = bool.TrueString,
            [AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairBatchProposalScopeId)] = bool.TrueString
        };
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

    private static SpriteRepairQueueRow Row(string root)
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
                    Path.Combine(root, "sprites_runtime", "snake", "baby", "female", "blue", "idle_00.png"),
                    null)
            ]);
    }

    private sealed record ReplayFixture(string Root, AuditLedgerService Ledger, SpriteRepairBatchProposalScope Scope);
}
