using System.Reflection;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Eval;

namespace Wevito.VNext.Tests;

public sealed class InDistributionVersusHeldOutTypeSeparationTests
{
    [Fact]
    public void StoreTypes_AreDistinctAndDoNotImplementEachOthersInterfaces()
    {
        Assert.NotEqual(typeof(InDistributionEvalStore), typeof(HeldOutEvalStore));
        Assert.False(typeof(IHeldOutEvalStore).IsAssignableFrom(typeof(InDistributionEvalStore)));
        Assert.False(typeof(IInDistributionEvalStore).IsAssignableFrom(typeof(HeldOutEvalStore)));
    }

    [Fact]
    public void SelfImprovementTypes_DoNotMixHeldOutAndInDistributionStoresInConstructors()
    {
        var offenders = typeof(SupervisedImprovementLoop).Assembly
            .GetTypes()
            .Where(type => (type.Namespace ?? "").StartsWith("Wevito.VNext.Core.SelfImprovement", StringComparison.Ordinal))
            .Select(type => new
            {
                Type = type,
                Constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            })
            .Where(entry => entry.Constructors.Any(ConstructorMixesStoreTypes))
            .Select(entry => entry.Type.FullName ?? entry.Type.Name)
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void InDistributionStore_DoesNotListHeldOutFormatFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-in-distribution-separation", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "held-out-case.json"), "{\"id\":\"held-out-case\",\"domain\":\"sprite-repair\"}");
        var store = new InDistributionEvalStore(root);

        Assert.Empty(store.ListCaseIds());
    }

    private static bool ConstructorMixesStoreTypes(ConstructorInfo constructor)
    {
        var parameters = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        var hasHeldOut = parameters.Any(type => type == typeof(IHeldOutEvalStore) || type == typeof(HeldOutEvalStore));
        var hasInDistribution = parameters.Any(type => type == typeof(IInDistributionEvalStore) || type == typeof(InDistributionEvalStore));
        return hasHeldOut && hasInDistribution;
    }
}
