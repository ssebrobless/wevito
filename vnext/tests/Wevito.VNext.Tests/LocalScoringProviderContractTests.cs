using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Scoring;

namespace Wevito.VNext.Tests;

public sealed class LocalScoringProviderContractTests
{
    [Fact]
    public void NotConfiguredProvider_ReturnsDefaultDenyRefusal()
    {
        var provider = new NotConfiguredScoringProvider();

        var result = provider.Score(new LocalScoringRequest(Sha(), "score safety"), CancellationToken.None);

        var refused = Assert.IsType<LocalScoringResult.Refused>(result);
        Assert.Equal("local_scoring_provider_not_configured", refused.Reason);
    }

    [Fact]
    public void NotConfiguredProvider_KillSwitchActive_ReturnsKillSwitchRefusal()
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        };
        var provider = new NotConfiguredScoringProvider(new KillSwitchService(() => settings));

        var result = provider.Score(new LocalScoringRequest(Sha(), "score safety"), CancellationToken.None);

        var refused = Assert.IsType<LocalScoringResult.Refused>(result);
        Assert.Equal("kill_switch=true", refused.Reason);
    }

    [Fact]
    public void CompositionRoot_ReturnsNotConfiguredProviderWithoutProducerWiring()
    {
        var provider = ShellCompositionRoot.CreateLocalScoringProvider();

        Assert.IsType<NotConfiguredScoringProvider>(provider);
    }

    [Fact]
    public void CapabilityFlag_DefaultsFalse()
    {
        var entry = Assert.Single(CapabilityFlagInventory.Entries, item => item.Name == NotConfiguredScoringProvider.EnabledSetting);

        Assert.Equal(bool.FalseString, entry.DefaultValue);
        Assert.Contains("Default off", entry.PlainLanguage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LocalScoringRequest_OnlyCarriesPromptSha256AndRubric()
    {
        var properties = typeof(LocalScoringRequest).GetProperties().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(["PromptSha256", "Rubric"], properties);
        Assert.DoesNotContain("Prompt", properties);
    }

    [Fact]
    public void DefaultDenyContractFiles_ContainNoHttpNetworkOrOllamaCode()
    {
        var root = Path.Combine(FindRepositoryRoot(), "vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Scoring");
        var contractFiles = new[]
        {
            "LocalScoringRequest.cs",
            "LocalScoringResult.cs",
            "ILocalScoringProvider.cs",
            "NotConfiguredScoringProvider.cs"
        };
        var source = string.Join(Environment.NewLine, contractFiles.Select(file => File.ReadAllText(Path.Combine(root, file))));

        Assert.DoesNotContain("System.Net.Http", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net.Sockets", source, StringComparison.Ordinal);
        Assert.DoesNotContain("127.0.0.1", source, StringComparison.Ordinal);
        Assert.DoesNotContain("localhost", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Ollama", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NoProducerConsumesLocalScoringProvider()
    {
        var root = Path.Combine(FindRepositoryRoot(), "vnext", "src", "Wevito.VNext.Core");
        var offenders = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}SelfImprovement{Path.DirectorySeparatorChar}Scoring{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith("ShellCompositionRoot.cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith(Path.Combine("Audit", "CapabilityFlagInventory.cs"), StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("ILocalScoringProvider", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(FindRepositoryRoot(), path))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void ScoringSource_DoesNotReferenceEvalStores()
    {
        var root = Path.Combine(FindRepositoryRoot(), "vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Scoring");
        var source = string.Join(Environment.NewLine, Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));

        Assert.DoesNotContain("IHeldOutEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HeldOutEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IInDistributionEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InDistributionEvalStore", source, StringComparison.Ordinal);
    }

    private static string Sha()
    {
        return new string('a', 64);
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
