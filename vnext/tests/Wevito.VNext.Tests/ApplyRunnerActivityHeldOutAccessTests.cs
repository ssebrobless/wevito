using Wevito.VNext.Core.SelfImprovement.Apply;
using Wevito.VNext.Core.SelfImprovement.Eval;

namespace Wevito.VNext.Tests;

public sealed class ApplyRunnerActivityHeldOutAccessTests
{
    [Fact]
    public void ActivityTypes_DoNotExposeHeldOutOrInDistributionStores()
    {
        var forbidden = new[]
        {
            typeof(IHeldOutEvalStore),
            typeof(HeldOutEvalStore),
            typeof(IInDistributionEvalStore),
            typeof(InDistributionEvalStore)
        };
        var types = new[]
        {
            typeof(ApplyRunnerActivityService),
            typeof(ApplyRunnerActivityEntry),
            typeof(ApplyRunnerActivityPacket)
        };

        foreach (var type in types)
        {
            var signatures = type.GetConstructors()
                .SelectMany(ctor => ctor.GetParameters().Select(parameter => parameter.ParameterType))
                .Concat(type.GetMethods().Select(method => method.ReturnType))
                .Concat(type.GetMethods().SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType)));

            Assert.DoesNotContain(signatures, signature => forbidden.Contains(signature));
        }
    }

    [Fact]
    public void ActivityServiceSource_DoesNotReferenceHeldOutOrInDistributionNames()
    {
        var text = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "Wevito.VNext.Core",
            "SelfImprovement",
            "Apply",
            "ApplyRunnerActivityService.cs")));

        Assert.DoesNotContain("HeldOut", text, StringComparison.Ordinal);
        Assert.DoesNotContain("InDistribution", text, StringComparison.Ordinal);
    }
}
