namespace Wevito.VNext.Tests;

public sealed class IdentityRenameSweepTests
{
    [Theory]
    [InlineData("PetTaskCard")]
    [InlineData("HelperPet")]
    [InlineData("PetCommandBar")]
    [InlineData("PetCommandParser")]
    [InlineData("PetTaskAdapterPreviewDispatcher")]
    [InlineData("PetHelperProfile")]
    [InlineData("PetHelperAvailability")]
    public void NoLegacyIdentityReferences(string legacyToken)
    {
        var root = FindRepoRoot();
        var files = Directory.EnumerateFiles(Path.Combine(root, "vnext"), "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith($"{Path.DirectorySeparatorChar}IdentityRenameSweepTests.cs", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var offenders = files
            .Where(path => File.ReadAllText(path).Contains(legacyToken, StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path))
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void PetActorAndPetMemoryStoreKeptAsIs()
    {
        var root = FindRepoRoot();

        Assert.True(File.Exists(Path.Combine(root, "vnext", "src", "Wevito.VNext.Core", "PetMemoryStore.cs")));
        Assert.Contains("record PetActor", File.ReadAllText(Path.Combine(root, "vnext", "src", "Wevito.VNext.Contracts", "Models.cs")));
        Assert.Contains("class PetSimulationEngine", File.ReadAllText(Path.Combine(root, "vnext", "src", "Wevito.VNext.Core", "PetSimulationEngine.cs")));
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current is not null &&
            !Directory.Exists(Path.Combine(current.FullName, ".git")) &&
            !File.Exists(Path.Combine(current.FullName, ".git")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? Environment.CurrentDirectory;
    }
}
