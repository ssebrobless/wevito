using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Eval;

namespace Wevito.VNext.Tests;

public sealed class HeldOutEvalStoreVisibilityTests
{
    [Fact]
    public void HeldOutStore_IsNotReferencedByForbiddenSurfaces()
    {
        var forbiddenTypes = new[]
        {
            typeof(ToolRegistry),
            typeof(EvidenceSummaryService),
            typeof(AutonomousScopeService)
        };

        foreach (var type in forbiddenTypes)
        {
            var signatures = type.GetConstructors()
                .SelectMany(ctor => ctor.GetParameters().Select(parameter => parameter.ParameterType))
                .Concat(type.GetMethods().Select(method => method.ReturnType))
                .Concat(type.GetMethods().SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType)));

            Assert.DoesNotContain(signatures, signature => signature == typeof(IHeldOutEvalStore) || signature == typeof(HeldOutEvalStore));
        }
    }

    [Fact]
    public void HeldOutStore_KillSwitchPreventsListingAndReading()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-held-out-eval", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "secret-case.json"), "{\"case\":\"held-out\"}");
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });

        var store = new HeldOutEvalStore(root, killSwitch);

        Assert.Empty(store.ListCaseIds());
        Assert.Null(store.ReadCase("secret-case"));
    }

    [Fact]
    public void HeldOutStore_BlocksPathTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-held-out-eval", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var store = new HeldOutEvalStore(root);

        Assert.Null(store.ReadCase("..\\outside"));
    }
}
