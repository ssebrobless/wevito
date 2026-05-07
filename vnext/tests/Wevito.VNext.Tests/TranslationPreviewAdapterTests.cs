using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class TranslationPreviewAdapterTests
{
    private readonly TranslationPreviewAdapter _adapter = new();

    [Fact]
    public void BuildPreview_WritesProviderStatusWithoutCallingProvider()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-142000-translate-text");

        var result = _adapter.BuildPreview(BuildRequest(artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.PreviewReady, result.Status);
        Assert.False(result.DidMutate);
        Assert.Empty(result.ReadPaths ?? []);
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("translation-preview-report.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.WrittenPaths ?? [], path => path.EndsWith("run-summary.md", StringComparison.OrdinalIgnoreCase));

        var report = JsonSerializer.Deserialize<TranslationPreviewReport>(
            File.ReadAllText(Path.Combine(artifactRoot, "translation-preview-report.json")),
            JsonDefaults.Options);
        Assert.NotNull(report);
        Assert.Equal("Hello goose", report.RequestedText);
        Assert.Equal("Spanish", report.TargetLanguage);
        Assert.False(report.DidCallProvider);
        Assert.False(report.DidMutate);
        Assert.Contains(report.ApplicableGlossaryEntries, entry => entry.Source == "goose" && entry.Target == "goose");
        Assert.Contains(report.SafetyNotes, note => note.Contains("No text was sent", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildPreview_MarkdownSurfacesApplicableGlossaryEntries()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-142000-translate-text");

        _adapter.BuildPreview(BuildRequest(artifactRoot));

        var markdown = File.ReadAllText(Path.Combine(artifactRoot, "run-summary.md"));
        Assert.Contains("## Applicable Glossary Entries", markdown);
        Assert.Contains("`goose` -> `goose`", markdown);
    }

    [Fact]
    public void BuildPreview_BlocksExecuteMode()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-142000-translate-text");

        var result = _adapter.BuildPreview(BuildRequest(artifactRoot, TaskAdapterRunMode.Execute));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains("dry-run report", result.BlockReason);
    }

    [Fact]
    public void ProviderRouter_PrefersConfiguredDeepL()
    {
        var router = new TranslationProviderRouter();
        var statuses = router.GetProviderStatuses(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["DEEPL_API_KEY"] = "present"
        });

        var preferred = router.SelectPreferredProvider(statuses);

        Assert.Equal(TranslationProviderKind.DeepL, preferred.Provider);
        Assert.Equal(TranslationProviderAvailability.Configured, preferred.Availability);
    }

    private static TaskAdapterRequest BuildRequest(
        string artifactRoot,
        TaskAdapterRunMode runMode = TaskAdapterRunMode.DryRunPreview)
    {
        var intent = new TaskIntent(
            Guid.Parse("c3000000-0000-0000-0000-000000000001"),
            "Nix, translate \"Hello goose\" to Spanish",
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetNameSnapshot: "Nix",
            TaskKind: TaskKind.TranslateText,
            RequestedToolFamily: "translateText");
        var policy = new ToolPolicy(
            "translate-text-readonly",
            "translateText",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None);

        return new TaskAdapterRequest(
            Guid.Parse("d3000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            runMode,
            artifactRoot);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-translation-preview-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
