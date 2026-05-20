using System.Reflection;
using Wevito.VNext.Core.SelfImprovement.Eval;

namespace Wevito.VNext.Tests;

public sealed class EvalCoverageHealthUIBindingTests
{
    [Fact]
    public void Snapshot_DoesNotCarryCaseIdsOrCaseContents()
    {
        var propertyNames = typeof(EvalCoverageHealthSnapshot)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain(propertyNames, name => name.Contains("CaseId", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, name => name.Contains("Content", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, name => name.Contains("Prompt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("HeldOutCount", propertyNames);
        Assert.Contains("InDistributionCount", propertyNames);
    }

    [Fact]
    public void EvalCoverageExpander_HasNoInteractiveControlsAndNoCaseIdBindings()
    {
        var xaml = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "vnext", "src", "Wevito.VNext.Shell", "ToolPopupWindow.xaml"));
        var start = xaml.IndexOf("AutomationId=\"EvalCoverageHealthExpander\"", StringComparison.Ordinal);
        Assert.True(start >= 0, "EvalCoverageHealthExpander was not found.");
        var end = xaml.IndexOf("AutomationId=\"LocalRuntimeReadinessExpander\"", start, StringComparison.Ordinal);
        Assert.True(end > start, "Eval coverage expander should appear before local runtime readiness.");
        var section = xaml[start..end];

        Assert.DoesNotContain("<Button", section, StringComparison.Ordinal);
        Assert.DoesNotContain("<ToggleSwitch", section, StringComparison.Ordinal);
        Assert.DoesNotContain("<CheckBox", section, StringComparison.Ordinal);
        Assert.DoesNotContain("Click=", section, StringComparison.Ordinal);
        Assert.DoesNotContain("CaseId", section, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret-case", section, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "wevito.godot"))
                || Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
