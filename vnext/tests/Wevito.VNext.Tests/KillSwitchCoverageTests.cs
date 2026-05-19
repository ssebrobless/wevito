using System.Reflection;
using Wevito.VNext.Core;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Tests.Support;

namespace Wevito.VNext.Tests;

public sealed class KillSwitchCoverageTests
{
    [Fact]
    public void SelfImprovementRuntimeTypes_DependOnKillSwitchOrAreAllowListed()
    {
        var selfImprovementTypes = DiscoverSelfImprovementTypes();
        Assert.True(selfImprovementTypes.Length >= 6, "Discovery must not pass vacuously.");

        var allowList = KillSwitchCoverageAllowList.PureDataTypes.ToHashSet(StringComparer.Ordinal);
        var candidates = selfImprovementTypes
            .Where(type => !allowList.Contains(type.FullName ?? type.Name))
            .ToArray();

        Assert.True(candidates.Length >= 6, "At least six non-allow-listed self-improvement runtime types should be checked.");

        var offenders = candidates
            .Where(type => !HasPublicKillSwitchConstructorParameter(type))
            .Select(type => type.FullName ?? type.Name)
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void AllowListEntries_AreDiscoverableAndJustified()
    {
        var discovered = DiscoverSelfImprovementTypes()
            .Select(type => type.FullName ?? type.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missing = KillSwitchCoverageAllowList.PureDataTypes
            .Where(entry => !discovered.Contains(entry))
            .ToArray();
        Assert.Empty(missing);

        var source = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), "vnext", "tests", "Wevito.VNext.Tests", "Support", "KillSwitchCoverageAllowList.cs"));
        var unjustified = source
            .Where(line => line.Contains("typeof(", StringComparison.Ordinal) && !line.Contains("//", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(unjustified);
    }

    [Fact]
    public void DiscoveryIncludesEverySelfImprovementSubNamespace()
    {
        var namespaces = DiscoverSelfImprovementTypes()
            .Select(type => type.Namespace ?? "")
            .Where(ns => ns.StartsWith("Wevito.VNext.Core.SelfImprovement", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Contains("Wevito.VNext.Core.SelfImprovement", namespaces);
        Assert.Contains("Wevito.VNext.Core.SelfImprovement.Eval", namespaces);
        Assert.Contains("Wevito.VNext.Core.SelfImprovement.Experiments", namespaces);
        Assert.Contains("Wevito.VNext.Core.SelfImprovement.Invariants", namespaces);
        Assert.Contains("Wevito.VNext.Core.SelfImprovement.Maturity", namespaces);
    }

    private static Type[] DiscoverSelfImprovementTypes()
    {
        return typeof(SupervisedImprovementLoop).Assembly
            .GetTypes()
            .Where(type => (type.IsPublic || type.IsNestedPublic) &&
                           (type.Namespace ?? "").StartsWith("Wevito.VNext.Core.SelfImprovement", StringComparison.Ordinal))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool HasPublicKillSwitchConstructorParameter(Type type)
    {
        return type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .Any(constructor => constructor.GetParameters().Any(parameter => parameter.ParameterType == typeof(KillSwitchService)));
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
