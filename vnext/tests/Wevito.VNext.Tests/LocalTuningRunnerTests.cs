using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LocalTuningRunnerTests
{
    [Fact]
    public void Run_DryRunNeverWritesOutsideAllowlist()
    {
        var root = CreateTempRoot();
        var runner = new LocalTuningRunner();

        var result = runner.Run(Request(root, dryRun: true));

        Assert.True(result.Succeeded, result.Message);
        Assert.False(result.DidMutate);
        Assert.True(File.Exists(Path.Combine(result.ArtifactFolder, "dry-run.json")));
        Assert.False(File.Exists(Path.Combine(root, "vnext", "content", "local-ai", "prompt-config.json")));
    }

    [Fact]
    public void Run_EvalRegressionTriggersRollback()
    {
        var root = CreateTempRoot();
        var target = Path.Combine(root, "vnext", "content", "local-ai", "prompt-config.json");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllText(target, "{\"before\":true}");
        var beforeHash = PromptConfigStore.Sha256(target);
        var runner = new LocalTuningRunner();

        var result = runner.Run(Request(
            root,
            dryRun: false,
            baseline: new LearningEvalMetrics(1, 1, 1, 1, 1),
            candidate: new LearningEvalMetrics(0.2, 1, 0.2, 1, 1)));

        Assert.True(result.Succeeded, result.Message);
        Assert.True(result.RolledBack);
        Assert.True(result.DidMutate);
        Assert.Equal(beforeHash, PromptConfigStore.Sha256(target));
        Assert.Equal(beforeHash, result.PostSha256);
    }

    [Fact]
    public void Run_RollbackIsByteExact()
    {
        var root = CreateTempRoot();
        var target = Path.Combine(root, "vnext", "content", "local-ai", "prompt-config.json");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var original = """
            {
              "exact": "bytes",
              "spacing": true
            }
            """;
        File.WriteAllText(target, original);
        var runner = new LocalTuningRunner();

        var result = runner.Run(Request(
            root,
            dryRun: false,
            baseline: new LearningEvalMetrics(1, 1, 1, 1, 1),
            candidate: new LearningEvalMetrics(0, 0, 0, 1, 1)));

        Assert.True(result.RolledBack);
        Assert.Equal(original, File.ReadAllText(target));
        Assert.Equal(result.PreSha256, result.PostSha256);
    }

    [Fact]
    public void Run_LoraStageNeverExecutes()
    {
        var root = CreateTempRoot();
        var runner = new LocalTuningRunner();

        var ex = Assert.Throws<InvalidOperationException>(() => runner.Run(Request(root, dryRun: false, stage: LocalTrainingStage.LoraPilot)));

        Assert.Contains("LoRA", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static LocalTuningRunRequest Request(
        string root,
        bool dryRun,
        LocalTrainingStage stage = LocalTrainingStage.PromptConfig,
        LearningEvalMetrics? baseline = null,
        LearningEvalMetrics? candidate = null)
    {
        return new LocalTuningRunRequest(
            root,
            Path.Combine(root, "vnext", "content"),
            Path.Combine(root, "vnext", "artifacts", "tuning"),
            "v0001-20260512-120000",
            stage,
            dryRun,
            baseline ?? new LearningEvalMetrics(1, 1, 1, 1, 1),
            candidate ?? new LearningEvalMetrics(1, 1, 1, 1, 1),
            DateTimeOffset.Parse("2026-05-12T12:00:00Z"));
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-local-tuning-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "vnext", "content"));
        Directory.CreateDirectory(Path.Combine(root, "vnext", "artifacts"));
        return root;
    }
}
