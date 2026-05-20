namespace Wevito.VNext.Tests;

public sealed class ArtifactRenameRollbackRunnerHeldOutAccessTests
{
    [Fact]
    public void Rollback_runner_source_does_not_reference_eval_stores_or_eval_cases()
    {
        var source = File.ReadAllText(SourcePath());

        Assert.DoesNotContain("IHeldOutEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HeldOutEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HeldOutEvalCase", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IInDistributionEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InDistributionEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InDistributionEvalCase", source, StringComparison.Ordinal);
    }

    private static string SourcePath()
    {
        var root = FindRepositoryRoot();
        return Path.Combine(root, "vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Apply", "ArtifactRenameRollbackRunner.cs");
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "vnext", "Wevito.VNext.sln")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName ?? "";
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
