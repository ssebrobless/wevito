namespace Wevito.VNext.Tests;

public sealed class ShellRefusesHostedAdapterAtRuntimeTests
{
    [Fact]
    public void CompositionRootHasNoHostedAdapter()
    {
        var source = File.ReadAllText(ResolveRepoPath("vnext", "src", "Wevito.VNext.Shell", "ShellCoordinator.cs"));

        Assert.DoesNotContain("new AnthropicModelAdapter", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new OpenAi", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("new Gemini", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("new LocalModelProviderRouterAdapter", source, StringComparison.Ordinal);
        Assert.Contains("new OllamaLocalModelAdapter", source, StringComparison.Ordinal);
    }

    private static string ResolveRepoPath(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not resolve repo path.", Path.Combine(parts));
    }
}
