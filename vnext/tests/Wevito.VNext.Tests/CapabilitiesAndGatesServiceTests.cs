using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;

namespace Wevito.VNext.Tests;

public sealed class CapabilitiesAndGatesServiceTests
{
    [Fact]
    public void Snapshot_OrderMatchesCapabilityFlagInventory()
    {
        var service = new CapabilitiesAndGatesService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        var snapshot = service.Snapshot(DateTimeOffset.UnixEpoch);

        Assert.Equal(
            CapabilityFlagInventory.Entries.Select(entry => entry.Name),
            snapshot.Entries.Select(entry => entry.Name));
    }

    [Fact]
    public void Snapshot_ResolvesOnOffAndUnsetStates()
    {
        var first = CapabilityFlagInventory.Entries[0].Name;
        var second = CapabilityFlagInventory.Entries[1].Name;
        var third = CapabilityFlagInventory.Entries[2].Name;
        var service = new CapabilitiesAndGatesService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [first] = bool.TrueString,
            [second] = bool.FalseString,
            [third] = "maybe"
        });

        var snapshot = service.Snapshot(DateTimeOffset.UnixEpoch);

        Assert.Equal("on", snapshot.Entries.Single(entry => entry.Name == first).State);
        Assert.Equal("off", snapshot.Entries.Single(entry => entry.Name == second).State);
        Assert.Equal("unset", snapshot.Entries.Single(entry => entry.Name == third).State);
        Assert.Equal(1, snapshot.OnCount);
        Assert.Equal(1, snapshot.OffCount);
        Assert.Equal(CapabilityFlagInventory.Entries.Count - 2, snapshot.UnsetCount);
    }

    [Fact]
    public void Snapshot_KillSwitchSuffixAppearsOnEveryEntry()
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        };
        var service = new CapabilitiesAndGatesService(() => settings, new KillSwitchService(() => settings));

        var snapshot = service.Snapshot(DateTimeOffset.UnixEpoch);

        Assert.True(snapshot.KillSwitchActive);
        Assert.All(snapshot.Entries, entry => Assert.EndsWith("; kill_switch=true", entry.State, StringComparison.Ordinal));
    }

    [Fact]
    public void Snapshot_KillSwitchActiveIsFalseWhenSettingUnset()
    {
        var service = new CapabilitiesAndGatesService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)));

        var snapshot = service.Snapshot(DateTimeOffset.UnixEpoch);

        Assert.False(snapshot.KillSwitchActive);
    }

    [Fact]
    public void CapabilitiesAndGatesExpander_HasNoInteractiveControls()
    {
        var xaml = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "vnext", "src", "Wevito.VNext.Shell", "ToolPopupWindow.xaml"));
        var start = xaml.IndexOf("Capabilities and gates (read-only)", StringComparison.Ordinal);
        Assert.True(start >= 0, "Capabilities expander must be present.");
        var end = xaml.IndexOf("<UniformGrid", start, StringComparison.Ordinal);
        Assert.True(end > start, "Capabilities expander should end before the Evidence filter grid.");
        var expanderText = xaml[start..end];

        Assert.DoesNotContain("<Button", expanderText, StringComparison.Ordinal);
        Assert.DoesNotContain("<ToggleSwitch", expanderText, StringComparison.Ordinal);
        Assert.DoesNotContain("<CheckBox", expanderText, StringComparison.Ordinal);
        Assert.DoesNotContain("Click=", expanderText, StringComparison.Ordinal);
    }

    [Fact]
    public void CapabilitiesAndGatesService_DoesNotReferenceForbiddenStoresOrModels()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "CapabilitiesAndGatesService.cs"));

        Assert.DoesNotContain("IHeldOutEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IInDistributionEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IModelAdapter", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ILocalScoringProvider", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Record(", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
