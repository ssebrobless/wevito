using System.Text.Json;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Tools;

namespace Wevito.VNext.Tests;

public sealed class ToolCatalogTests
{
    [Fact]
    public void ToolFamiliesMirrorRegistryDescriptors()
    {
        var catalogFamilies = ToolCatalog.ToolFamilies
            .Select(entry => entry.ToolFamily)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var registryFamilies = ToolRegistry.BuildDefaultDescriptors(new AgentToolDispatcher())
            .Select(entry => entry.ToolFamily)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(catalogFamilies, registryFamilies);
    }

    [Fact]
    public void DescriptionsStayPlainAndReviewable()
    {
        foreach (var tab in ToolCatalog.TopLevelTabs)
        {
            Assert.InRange(tab.Description.Length, 1, 120);
        }

        foreach (var tool in ToolCatalog.ToolFamilies)
        {
            Assert.InRange(tool.Description.Length, 1, 120);
        }
    }

    [Fact]
    public void TopTabsKeepAllExistingCommandRoutesReachable()
    {
        var toolIds = ToolCatalog.TopLevelTabs
            .Select(tab => tab.ToolId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var expected in new[] { "actions", "helpers", "basket", "settings", "evidence", "autonomous-scopes", "local-docs", "activity", "benchmarks", "creative-lab" })
        {
            Assert.Contains(expected, toolIds);
        }

        Assert.Contains(ToolCatalog.TopLevelTabs, tab => tab.ToolId == "activity" && tab.IsAdvanced);
        Assert.Contains(ToolCatalog.TopLevelTabs, tab => tab.ToolId == "benchmarks" && tab.IsAdvanced);
        Assert.Contains(ToolCatalog.TopLevelTabs, tab => tab.ToolId == "creative-lab" && tab.IsAdvanced);
    }

    [Fact]
    public void InventoryArtifactMatchesCatalogTabsAndFamilies()
    {
        var path = FindRepoFile("vnext", "artifacts", "c-phase-140-tool-hub", "inventory.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        var inventoryTabs = document.RootElement.GetProperty("tabs")
            .EnumerateArray()
            .Select(item => item.GetProperty("toolId").GetString() ?? "")
            .ToArray();
        var catalogTabs = ToolCatalog.TopLevelTabs
            .Select(tab => tab.ToolId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.All(inventoryTabs, tab => Assert.Contains(tab, catalogTabs));

        var inventoryFamilies = document.RootElement.GetProperty("toolFamilies")
            .EnumerateArray()
            .Select(item => item.GetProperty("toolFamily").GetString() ?? "")
            .ToArray();
        var catalogFamilies = ToolCatalog.ToolFamilies
            .Select(entry => entry.ToolFamily)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.All(inventoryFamilies, family => Assert.Contains(family, catalogFamilies));
    }

    [Fact]
    public void ToolHubLayoutPacketHasPlainLanguage()
    {
        var explainer = new PlainLanguageExplainer();

        var text = explainer.ExplainPacketKind(ToolCatalog.LayoutChangedPacketKind);

        Assert.Contains(ToolCatalog.LayoutChangedPacketKind, PlainLanguageExplainer.KnownPacketKinds);
        Assert.Contains("Tool Hub", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Unknown", text, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoFile(params string[] relativeParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {string.Join(Path.DirectorySeparatorChar, relativeParts)} from {AppContext.BaseDirectory}.");
    }
}
