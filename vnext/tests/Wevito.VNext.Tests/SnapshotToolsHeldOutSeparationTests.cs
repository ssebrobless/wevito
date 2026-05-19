namespace Wevito.VNext.Tests;

public sealed class SnapshotToolsHeldOutSeparationTests
{
    [Fact]
    public void SnapshotAndReplayTools_DoNotImportEvalNamespaceOrRuntimeStores()
    {
        var root = FindRepositoryRoot();
        var files = new[]
        {
            Path.Combine(root, "vnext", "tools", "Wevito.Tools.SnapshotExport", "Program.cs"),
            Path.Combine(root, "vnext", "tools", "Wevito.Tools.SnapshotExport", "Wevito.Tools.SnapshotExport.csproj"),
            Path.Combine(root, "vnext", "tools", "Wevito.Tools.SnapshotVerify", "Program.cs"),
            Path.Combine(root, "vnext", "tools", "Wevito.Tools.SnapshotVerify", "Wevito.Tools.SnapshotVerify.csproj"),
            Path.Combine(root, "vnext", "tools", "Wevito.Tools.ReplayRunner", "Program.cs"),
            Path.Combine(root, "vnext", "tools", "Wevito.Tools.ReplayRunner", "Wevito.Tools.ReplayRunner.csproj")
        };

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            Assert.DoesNotContain("using Wevito.VNext.Core.SelfImprovement.Eval", source, StringComparison.Ordinal);
            AssertNoRuntimeStoreTokens(source);
        }
    }

    [Fact]
    public void SeedTools_MayUseCaseTypesButDoNotReferenceRuntimeStores()
    {
        var root = FindRepositoryRoot();
        var files = new[]
        {
            Path.Combine(root, "vnext", "tools", "Wevito.Tools.HeldOutSeed", "Program.cs"),
            Path.Combine(root, "vnext", "tools", "Wevito.Tools.HeldOutSeed", "Wevito.Tools.HeldOutSeed.csproj"),
            Path.Combine(root, "vnext", "tools", "Wevito.Tools.InDistributionSeed", "Program.cs"),
            Path.Combine(root, "vnext", "tools", "Wevito.Tools.InDistributionSeed", "Wevito.Tools.InDistributionSeed.csproj")
        };

        foreach (var file in files)
        {
            AssertNoRuntimeStoreTokens(File.ReadAllText(file));
        }
    }

    private static void AssertNoRuntimeStoreTokens(string source)
    {
        Assert.DoesNotContain("IHeldOutEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HeldOutEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IInDistributionEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InDistributionEvalStore", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "wevito.godot")) ||
                Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
