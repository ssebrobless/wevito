using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;

namespace Wevito.VNext.Tests;

public sealed class CapabilityFlagAuditServiceTests
{
    [Fact]
    public void GetRows_UsesDefaultWhenSettingAbsent()
    {
        var service = new CapabilityFlagAuditService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        var rows = service.GetRows();
        var web = Assert.Single(rows, row => row.Name == WebResearchConnector.WebSearchEnabledSetting);

        Assert.Equal(bool.FalseString, web.DefaultValue);
        Assert.Equal(bool.FalseString, web.CurrentValue);
        Assert.True(web.IsDefault);
    }

    [Fact]
    public void GetRows_ShowsOverridesWithoutWriting()
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [WebResearchConnector.WebSearchEnabledSetting] = bool.TrueString
        };
        var service = new CapabilityFlagAuditService(() => settings);

        var rows = service.GetRows();
        var web = Assert.Single(rows, row => row.Name == WebResearchConnector.WebSearchEnabledSetting);

        Assert.Equal(bool.TrueString, web.CurrentValue);
        Assert.False(web.IsDefault);
        Assert.Equal(bool.TrueString, settings[WebResearchConnector.WebSearchEnabledSetting]);
    }

    [Fact]
    public void GetRows_MasksCurrentValuesWhenKillSwitchIsActive()
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString,
            [WebResearchConnector.WebSearchEnabledSetting] = bool.TrueString
        };
        var service = new CapabilityFlagAuditService(() => settings, new KillSwitchService(() => settings));

        var rows = service.GetRows();

        Assert.All(rows, row =>
        {
            Assert.Equal("(masked: kill_switch=true)", row.CurrentValue);
            Assert.False(row.IsDefault);
        });
    }

    [Fact]
    public void ServiceDoesNotDependOnAuditLedgerOrRecordCalls()
    {
        var source = File.ReadAllText(SourcePath("vnext", "src", "Wevito.VNext.Core", "Audit", "CapabilityFlagAuditService.cs"));

        Assert.DoesNotContain("AuditLedgerService", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Record(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PublishSetting", source, StringComparison.Ordinal);
    }

    [Fact]
    public void EvidencePanelCapabilityFlagSectionIsReadOnlyAndHasNoButtons()
    {
        var xaml = File.ReadAllText(SourcePath("vnext", "src", "Wevito.VNext.Shell", "ToolPopupWindow.xaml"));
        var start = xaml.IndexOf("AutomationId=\"CapabilityFlagPanel\"", StringComparison.Ordinal);
        Assert.True(start >= 0, "CapabilityFlagPanel was not found.");
        var end = xaml.IndexOf("AutomationId=\"EvidenceSummaryGrid\"", start, StringComparison.Ordinal);
        Assert.True(end > start, "CapabilityFlagPanel should appear before EvidenceSummaryGrid.");
        var section = xaml[start..end];

        Assert.Contains("IsReadOnly=\"True\"", section, StringComparison.Ordinal);
        Assert.DoesNotContain("<Button", section, StringComparison.OrdinalIgnoreCase);
    }

    private static string SourcePath(params string[] parts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not locate source file: {Path.Combine(parts)}");
    }
}
