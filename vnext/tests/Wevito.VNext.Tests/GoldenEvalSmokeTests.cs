using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class GoldenEvalSmokeTests
{
    [Fact]
    public void GoldenEval_RunCheckedInDatasetPasses()
    {
        var artifactRoot = Path.Combine(Path.GetTempPath(), "wevito-golden-smoke", Guid.NewGuid().ToString("N"));
        var repoRoot = FindRepoRoot();
        var result = new LearningEvalService().RunGoldenEval(
            Path.Combine(repoRoot, "vnext", "content", "local-ai", "eval-golden"),
            artifactRoot,
            updateBaseline: false,
            DateTimeOffset.Parse("2026-05-13T12:00:00Z"));

        Assert.True(result.Succeeded, result.Message);
        Assert.False(result.Regression);
        Assert.True(result.Current.RecallAt1 >= 0.98);
        Assert.True(File.Exists(result.ReportPath));
        Assert.True(File.Exists(result.SummaryPath));
    }

    private static string FindRepoRoot()
    {
        var candidate = Directory.GetCurrentDirectory();
        while (!string.IsNullOrWhiteSpace(candidate))
        {
            if (Directory.Exists(Path.Combine(candidate, "vnext")) && Directory.Exists(Path.Combine(candidate, "docs")))
            {
                return candidate;
            }

            candidate = Directory.GetParent(candidate)?.FullName ?? "";
        }

        throw new DirectoryNotFoundException("Could not locate Wevito repo root.");
    }
}
