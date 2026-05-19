using System.Text.Json;
using Wevito.VNext.Core;
using Wevito.VNext.Core.SelfImprovement.Eval;

namespace Wevito.VNext.Tests;

public sealed class InDistributionEvalStoreTests
{
    [Fact]
    public void ListCaseIds_ReturnsEmptyForEmptyRoot()
    {
        var root = CreateRoot();
        var store = new InDistributionEvalStore(root);

        Assert.Empty(store.ListCaseIds());
    }

    [Fact]
    public void ListCaseIds_SkipsFilesWithoutInDistributionCaseKind()
    {
        var root = CreateRoot();
        File.WriteAllText(Path.Combine(root, "held-out-shaped.json"), "{\"id\":\"held-out-shaped\"}");
        File.WriteAllText(Path.Combine(root, "in-distribution.json"), ValidCaseJson("in-distribution"));
        var store = new InDistributionEvalStore(root);

        var ids = store.ListCaseIds();

        Assert.Equal(["in-distribution"], ids);
    }

    [Fact]
    public void KillSwitch_BlocksListingAndReading()
    {
        var root = CreateRoot();
        File.WriteAllText(Path.Combine(root, "case-001.json"), ValidCaseJson("case-001"));
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });
        var store = new InDistributionEvalStore(root, killSwitch);

        Assert.Empty(store.ListCaseIds());
        Assert.Null(store.ReadCase("case-001"));
    }

    [Fact]
    public void ReadCase_BlocksPathTraversal()
    {
        var root = CreateRoot();
        var store = new InDistributionEvalStore(root);

        Assert.Null(store.ReadCase("..\\outside"));
    }

    [Fact]
    public void ListCaseIds_OnlySurfacesJsonFiles()
    {
        var root = CreateRoot();
        File.WriteAllText(Path.Combine(root, "case-json.json"), ValidCaseJson("case-json"));
        File.WriteAllText(Path.Combine(root, "case-text.txt"), ValidCaseJson("case-text"));
        var store = new InDistributionEvalStore(root);

        Assert.Equal(["case-json"], store.ListCaseIds());
        Assert.Null(store.ReadCase("case-text"));
    }

    [Fact]
    public void ReadCase_ReturnsNullForHeldOutFormatFile()
    {
        var root = CreateRoot();
        File.WriteAllText(Path.Combine(root, "held-out-shaped.json"), "{\"id\":\"held-out-shaped\"}");
        var store = new InDistributionEvalStore(root);

        Assert.Null(store.ReadCase("held-out-shaped"));
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-in-distribution-eval", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static string ValidCaseJson(string id)
    {
        var evalCase = new InDistributionEvalCase(
            id,
            "sprite-repair",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            DateTimeOffset.Parse("2026-05-19T12:00:00Z"),
            "test-only visible case");

        return JsonSerializer.Serialize(evalCase);
    }
}
