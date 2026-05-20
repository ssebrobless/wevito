namespace Wevito.VNext.Tests;

public sealed class LocalOllamaReadinessProbeServiceTextScanTests
{
    [Fact]
    public void ProbeSource_UsesOnlyTagsEndpoint()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "vnext",
            "src",
            "Wevito.VNext.Core",
            "SelfImprovement",
            "Readiness",
            "LocalOllamaReadinessProbeService.cs"));

        Assert.Contains("/api/tags", source, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/generate", source, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/chat", source, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/embeddings", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TestsDoNotConstructDefaultScoringHttpClient()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "vnext",
            "tests",
            "Wevito.VNext.Tests",
            "LocalOllamaReadinessProbeServiceTests.cs"));

        Assert.DoesNotContain("new DefaultScoringHttpClient", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Readiness", "LocalOllamaReadinessProbeService.cs");
            if (File.Exists(candidate))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
    }
}
