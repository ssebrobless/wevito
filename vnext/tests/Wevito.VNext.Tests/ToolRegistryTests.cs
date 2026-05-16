using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using CoreToolDescriptor = Wevito.VNext.Core.ToolDescriptor;

namespace Wevito.VNext.Tests;

public sealed class ToolRegistryTests
{
    [Fact]
    public void LoadsAllExistingPreviewAdapters()
    {
        var registry = ToolRegistry.CreateDefault(toolDefinitionsPath: Path.Combine(Environment.CurrentDirectory, "vnext", "content", "tool_definitions.json"));

        var families = registry.Descriptors.Select(tool => tool.ToolFamily).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var expected in new[] { "localDocs", "localResearch", "spriteAudit", "petState", "assetInventory", "codeReview", "codePatchPlan", "buildProof", "translateText", "audioAssist", "screenCapture", "petMemory", "bookmark_for_benchmark", "pin_message" })
        {
            Assert.Contains(expected, families);
        }
    }

    [Fact]
    public void UserDisabledToolsHiddenFromLlm()
    {
        var registry = new ToolRegistry(
            [Descriptor("localDocs"), Descriptor("spriteAudit")],
            settingsProvider: () => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ToolRegistry.DisabledToolsSetting] = "spriteAudit"
            });

        Assert.Contains(registry.LlmVisibleDescriptors, tool => tool.ToolFamily == "localDocs");
        Assert.DoesNotContain(registry.LlmVisibleDescriptors, tool => tool.ToolFamily == "spriteAudit");
    }

    [Fact]
    public void RegistryRefusesUndeclaredContentTools()
    {
        var path = Path.Combine(Path.GetTempPath(), "wevito-tool-registry-tests", Guid.NewGuid().ToString("N"), "tool_definitions.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new[] { new { toolFamily = "localDocs" } }));

        var error = Assert.Throws<InvalidOperationException>(() => ToolRegistry.CreateDefault(toolDefinitionsPath: path));

        Assert.Contains("spriteAudit", error.Message);
    }

    private static CoreToolDescriptor Descriptor(string family)
    {
        return new CoreToolDescriptor(
            family,
            family,
            $"{family} descriptor",
            """{"type":"object"}""",
            """{"type":"object"}""",
            ToolRiskLevel.Low,
            RequiresApproval: false,
            (request, _) => Task.FromResult(new TaskAdapterResult(request.TaskCardId, family, TaskAdapterResultStatus.PreviewReady, DidMutate: false, PreviewSummary: "ok")));
    }
}



