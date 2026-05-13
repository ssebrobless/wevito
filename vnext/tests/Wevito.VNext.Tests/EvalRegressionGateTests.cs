using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class EvalRegressionGateTests
{
    [Fact]
    public void EvalRegression_FlagsWorseMetrics()
    {
        var baseline = new GoldenEvalBaseline("1", 1, 1, 1, 1, 1, 1, "sha", DateTimeOffset.Parse("2026-05-13T12:00:00Z"));
        var current = baseline with { RecallAt1 = 0.50, MeanReciprocalRank = 0.50 };

        Assert.True(EvalRegressionGate.IsRegression(current, baseline));
    }

    [Fact]
    public void EvalRegression_PassesAtBaseline()
    {
        var baseline = new GoldenEvalBaseline("1", 1, 1, 1, 1, 1, 1, "sha", DateTimeOffset.Parse("2026-05-13T12:00:00Z"));

        Assert.False(EvalRegressionGate.IsRegression(baseline, baseline));
    }

    [Fact]
    public void EvalRegression_FailsClosedOnDatasetShaMismatch()
    {
        var root = CreateDataset();
        var baselinePath = Path.Combine(root, "baseline.json");
        var sha = EvalRegressionGate.ComputeDatasetSha(root);
        File.WriteAllText(baselinePath, JsonSerializer.Serialize(new GoldenEvalBaseline("1", 1, 1, 1, 1, 0, 0, sha, DateTimeOffset.Parse("2026-05-13T12:00:00Z"))));
        File.AppendAllText(Path.Combine(root, "documents", "example-001.md"), " changed");

        var result = new EvalRegressionGate().Run(root, Path.Combine(root, "artifacts"), updateBaseline: false, DateTimeOffset.Parse("2026-05-13T12:00:00Z"));

        Assert.False(result.Succeeded);
        Assert.Contains("sha256 mismatch", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EvalRegression_BaselineOverwriteRequiresExplicitFlag()
    {
        var root = CreateDataset();
        var baselinePath = Path.Combine(root, "baseline.json");
        File.WriteAllText(baselinePath, JsonSerializer.Serialize(new GoldenEvalBaseline("1", 0, 0, 0, 0, 0, 0, "stale", DateTimeOffset.Parse("2026-05-13T12:00:00Z"))));

        var blocked = new EvalRegressionGate().Run(root, Path.Combine(Path.GetTempPath(), "wevito-golden-blocked", Guid.NewGuid().ToString("N")), updateBaseline: false, DateTimeOffset.Parse("2026-05-13T12:00:00Z"));
        var updated = new EvalRegressionGate().Run(root, Path.Combine(Path.GetTempPath(), "wevito-golden-updated", Guid.NewGuid().ToString("N")), updateBaseline: true, DateTimeOffset.Parse("2026-05-13T12:00:00Z"));

        Assert.False(blocked.Succeeded);
        Assert.True(updated.Succeeded);
        var parsed = JsonSerializer.Deserialize<GoldenEvalBaseline>(File.ReadAllText(baselinePath), JsonDefaults.Options);
        Assert.Equal(EvalRegressionGate.ComputeDatasetSha(root), parsed?.DatasetSha256);
    }

    [Fact]
    public void EvalRegression_KillSwitchBlocksGate()
    {
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });

        var result = new EvalRegressionGate(killSwitchService: killSwitch).Run(CreateDataset(), Path.Combine(Path.GetTempPath(), "wevito-golden-kill", Guid.NewGuid().ToString("N")), nowUtc: DateTimeOffset.Parse("2026-05-13T12:00:00Z"));

        Assert.False(result.Succeeded);
        Assert.Equal("kill_switch=true", result.Message);
    }

    private static string CreateDataset()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-golden-eval-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "documents"));
        File.WriteAllText(Path.Combine(root, "documents", "example-001.md"), "goose pond water");
        File.WriteAllText(Path.Combine(root, "questions.json"), """
            [
              {"id":"q1","question":"goose pond water","expectedChunkIds":["example-001"],"rubricNote":"fixture"}
            ]
            """);
        File.WriteAllText(Path.Combine(root, "baseline.json"), "{}");
        var sha = EvalRegressionGate.ComputeDatasetSha(root);
        File.WriteAllText(Path.Combine(root, "baseline.json"), JsonSerializer.Serialize(new GoldenEvalBaseline("1", 1, 1, 1, 1, 0, 0, sha, DateTimeOffset.Parse("2026-05-13T12:00:00Z"))));
        return root;
    }
}
