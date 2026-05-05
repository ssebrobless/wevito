using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Tests;

public sealed class SpriteWorkflowContractTests
{
    [Fact]
    public void SpriteRowManifest_RecordsConcreteFrameScopedTarget()
    {
        var key = new SpriteRowKey("goose", PetAgeStage.Baby, PetGender.Female, "blue", "hold_ball");
        var frames = new[]
        {
            new SpriteFrameManifest("hold_ball_00", "sprites_runtime/goose/baby/female/blue/hold_ball_00.png", "abc123", new SpriteFrameGeometry(60, 59)),
            new SpriteFrameManifest("hold_ball_01", "sprites_runtime/goose/baby/female/blue/hold_ball_01.png", "def456", new SpriteFrameGeometry(60, 59))
        };

        var manifest = new SpriteRowManifest(key, ExpectedFrameCount: 4, frames, IsOptionalFamily: true);

        Assert.Equal("goose", manifest.Key.Species);
        Assert.Equal("hold_ball", manifest.Key.Family);
        Assert.True(manifest.IsOptionalFamily);
        Assert.Equal(4, manifest.ExpectedFrameCount);
        Assert.Equal(2, manifest.Frames.Count);
    }

    [Fact]
    public void AnimationCandidateManifest_CanMarkRuntimeOverlaySeparateFromBodyFrames()
    {
        var key = new SpriteRowKey("goose", PetAgeStage.Baby, PetGender.Female, "blue", "hold_ball");
        var manifest = new AnimationCandidateManifest(
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            key,
            AnimationCandidateState.AcceptedForApplyProbe,
            new[]
            {
                "vnext/artifacts/animation-runs/goose/hold_ball_00.png",
                "vnext/artifacts/animation-runs/goose/hold_ball_01.png"
            },
            new SpriteFrameGeometry(60, 59),
            Provider: "manual-proof",
            VisualProofPath: "vnext/artifacts/animation-runs/goose/qa/preview.gif",
            UsesRuntimePropOverlay: true);

        Assert.True(manifest.UsesRuntimePropOverlay);
        Assert.Equal(60, manifest.RuntimeFrameGeometry.Width);
        Assert.Equal(AnimationCandidateState.AcceptedForApplyProbe, manifest.State);
    }

    [Fact]
    public void ApplyProofManifest_RoundTripsBackupAndProofSurfaceData()
    {
        var key = new SpriteRowKey("goose", PetAgeStage.Baby, PetGender.Female, "blue", "hold_ball");
        var proof = new ApplyProofManifest(
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            key,
            ApplyProofState.DryRunPassed,
            new Dictionary<string, string>
            {
                ["hold_ball_00.png"] = "before-hash"
            },
            new Dictionary<string, string>
            {
                ["hold_ball_00.png"] = "after-hash"
            },
            new[] { SpriteProofSurface.Godot },
            DryRunReportPath: "vnext/artifacts/apply/dry-run.json",
            RollbackProcedurePath: "vnext/artifacts/apply/rollback.ps1");

        var json = JsonSerializer.Serialize(proof, JsonDefaults.Options);
        var roundTrip = JsonSerializer.Deserialize<ApplyProofManifest>(json, JsonDefaults.Options);

        Assert.Contains("\"state\":\"dryRunPassed\"", json);
        Assert.Contains("\"proofSurfaces\":[\"godot\"]", json);
        Assert.NotNull(roundTrip);
        Assert.Equal("before-hash", roundTrip.BackupBeforeApplyHashes["hold_ball_00.png"]);
        Assert.Equal(SpriteProofSurface.Godot, roundTrip.ProofSurfaces[0]);
    }
}
