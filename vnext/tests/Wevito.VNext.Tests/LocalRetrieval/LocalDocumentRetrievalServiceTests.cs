using Wevito.VNext.Core;
using Wevito.VNext.Core.LocalRetrieval;
using Wevito.VNext.Core.Settings;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests.LocalRetrieval;

public sealed class LocalDocumentRetrievalServiceTests
{
    [Fact]
    public async Task LocalDocumentRetrieval_BuildIndex_CreatesIndexDbUnderExpectedDir()
    {
        var root = CreateTempRoot();
        File.WriteAllText(Path.Combine(root, "notes.md"), "goose likes fresh water");
        var indexPath = Path.Combine(root, "local-doc-index", "index.db");
        var service = CreateService(root, indexPath, enabled: true);

        var result = await service.BuildIndexAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(File.Exists(indexPath));
        Assert.EndsWith(Path.Combine("local-doc-index", "index.db"), indexPath);
    }

    [Fact]
    public async Task LocalDocumentRetrieval_BuildIndex_IsIncrementalOnUnchangedFiles()
    {
        var root = CreateTempRoot();
        File.WriteAllText(Path.Combine(root, "notes.txt"), "frog jumps");
        var service = CreateService(root, Path.Combine(root, "index.db"), enabled: true);

        var first = await service.BuildIndexAsync(CancellationToken.None);
        var second = await service.BuildIndexAsync(CancellationToken.None);

        Assert.Equal(1, first.IndexedFiles);
        Assert.Equal(0, second.IndexedFiles);
        Assert.True(second.SkippedFiles >= 1);
    }

    [Fact]
    public async Task LocalDocumentRetrieval_BuildIndex_SkipsFilesAboveSizeCap()
    {
        var root = CreateTempRoot();
        File.WriteAllText(Path.Combine(root, "big.md"), new string('x', 64));
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var service = CreateService(root, Path.Combine(root, "index.db"), enabled: true, maxBytes: 8, ledger: ledger);

        var result = await service.BuildIndexAsync(CancellationToken.None);

        Assert.Equal(0, result.IndexedFiles);
        Assert.Equal(1, result.SkippedFiles);
        Assert.Contains("big.md", result.SkippedPaths);
        Assert.Contains(ledger.Snapshot(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(5)),
            row => row.PacketKind == LocalDocumentRetrievalService.IndexRefusedPacketKind && row.Summary.Contains("reason=size_cap", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LocalDocumentRetrieval_BuildIndex_RefusesPathsOutsideRoot()
    {
        var root = CreateTempRoot();
        var outside = Path.Combine(Path.GetTempPath(), "wevito-local-doc-outside", Guid.NewGuid().ToString("N"), "outside.md");
        Directory.CreateDirectory(Path.GetDirectoryName(outside)!);
        File.WriteAllText(outside, "outside");
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var service = CreateService(
            root,
            Path.Combine(root, "index.db"),
            enabled: true,
            ledger: ledger,
            fileEnumerator: _ => [outside]);

        var result = await service.BuildIndexAsync(CancellationToken.None);

        Assert.Equal(0, result.IndexedFiles);
        Assert.Equal(1, result.SkippedFiles);
        Assert.Contains(ledger.Snapshot(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(5)),
            row => row.PacketKind == LocalDocumentRetrievalService.IndexRefusedPacketKind && row.Summary.Contains("reason=outside_root", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LocalDocumentRetrieval_Query_RanksByBm25_AscendingIsMostRelevant()
    {
        var root = CreateTempRoot();
        File.WriteAllText(Path.Combine(root, "strong.md"), "goose goose goose");
        File.WriteAllText(Path.Combine(root, "weak.md"), "goose");
        var service = CreateService(root, Path.Combine(root, "index.db"), enabled: true);
        await service.BuildIndexAsync(CancellationToken.None);

        var results = await service.QueryAsync("goose", 10, CancellationToken.None);

        Assert.True(results.Count >= 2);
        Assert.Equal("strong.md", results[0].RelativePath);
        Assert.True(results[0].Score <= results[1].Score);
    }

    [Fact]
    public async Task LocalDocumentRetrieval_Query_RejectsUnbalancedQuotes()
    {
        var root = CreateTempRoot();
        File.WriteAllText(Path.Combine(root, "notes.md"), "snake");
        var service = CreateService(root, Path.Combine(root, "index.db"), enabled: true);
        await service.BuildIndexAsync(CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(() => service.QueryAsync("snake \"bad", 5, CancellationToken.None));
    }

    [Fact]
    public async Task LocalDocumentRetrieval_Query_NeverInterpolatesUserInputIntoSql()
    {
        var root = CreateTempRoot();
        File.WriteAllText(Path.Combine(root, "notes.md"), "needle");
        var index = new LocalDocumentIndex(Path.Combine(root, "index.db"));
        var service = CreateService(root, index.IndexPath, enabled: true, index: index);
        await service.BuildIndexAsync(CancellationToken.None);
        var userInput = "needle'; DROP TABLE files; --";

        await service.QueryAsync(userInput, 5, CancellationToken.None);

        Assert.Equal(LocalDocumentIndex.CanonicalQuerySql, index.LastCommandText);
        Assert.DoesNotContain(userInput, index.LastCommandText, StringComparison.Ordinal);
        Assert.Equal($"\"{userInput}\"", index.LastMatchParameter);
    }

    [Fact]
    public async Task LocalDocumentRetrieval_RespectsKillSwitch()
    {
        var root = CreateTempRoot();
        File.WriteAllText(Path.Combine(root, "notes.md"), "crow");
        var settings = Settings(root, enabled: true);
        settings[KillSwitchService.KillSwitchSetting] = bool.TrueString;
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var kill = new KillSwitchService(() => settings, ledger);
        var service = new LocalDocumentRetrievalService(() => settings, ledger, kill, new LocalDocumentIndex(Path.Combine(root, "index.db")));

        var build = await service.BuildIndexAsync(CancellationToken.None);
        var query = await service.QueryAsync("crow", 5, CancellationToken.None);

        Assert.False(build.Success);
        Assert.Equal("killswitch", build.Reason);
        Assert.Empty(query);
    }

    [Fact]
    public async Task LocalDocumentRetrieval_RespectsCapabilityFlag()
    {
        var root = CreateTempRoot();
        File.WriteAllText(Path.Combine(root, "notes.md"), "rat");
        var indexPath = Path.Combine(root, "index.db");
        var service = CreateService(root, indexPath, enabled: false);

        var build = await service.BuildIndexAsync(CancellationToken.None);
        var query = await service.QueryAsync("rat", 5, CancellationToken.None);

        Assert.False(build.Success);
        Assert.Equal("capability_disabled", build.Reason);
        Assert.Empty(query);
        Assert.False(File.Exists(indexPath));
    }

    [Fact]
    public void LocalDocumentRetrieval_IndexFile_IsNotCoLocatedWithAuditLedgerOrWevitoDb()
    {
        var indexPath = SettingKeys.DefaultLocalDocumentIndexPath();
        var auditPath = new AuditLedgerService().DatabasePath;
        var appDbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WevitoVNext",
            "wevito-vnext.db");

        Assert.Contains(Path.Combine("WevitoVNext", "local-doc-index"), indexPath);
        Assert.NotEqual(Path.GetDirectoryName(auditPath), Path.GetDirectoryName(indexPath));
        Assert.NotEqual(Path.GetDirectoryName(appDbPath), Path.GetDirectoryName(indexPath));
    }

    [Fact]
    public void PlainLanguageExplainer_KnowsLocalDocPackets()
    {
        var explainer = new PlainLanguageExplainer();

        Assert.Contains(LocalDocumentRetrievalService.IndexBuiltPacketKind, PlainLanguageExplainer.KnownPacketKinds);
        Assert.Contains(LocalDocumentRetrievalService.QueryCompletedPacketKind, PlainLanguageExplainer.KnownPacketKinds);
        Assert.Contains(LocalDocumentRetrievalService.IndexRefusedPacketKind, PlainLanguageExplainer.KnownPacketKinds);
        Assert.Equal("Built the local document search index.", explainer.ExplainPacketKind(LocalDocumentRetrievalService.IndexBuiltPacketKind));
    }

    [Fact]
    public void Settings_LocalDocumentRetrieval_DefaultsOffWhenHydrating()
    {
        var hydrated = ShellCoordinator.ApplyDefaultSettings(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        Assert.Equal(bool.FalseString, hydrated[SettingKeys.LocalDocumentRetrievalEnabled]);
        Assert.Equal(SettingKeys.DefaultLocalDocumentRetrievalRoot(), hydrated[SettingKeys.LocalDocumentRetrievalRoot]);
        Assert.Equal(SettingKeys.LocalDocumentRetrievalDefaultMaxFileBytes, hydrated[SettingKeys.LocalDocumentRetrievalMaxFileBytes]);
    }

    private static LocalDocumentRetrievalService CreateService(
        string root,
        string indexPath,
        bool enabled,
        long maxBytes = 4194304,
        AuditLedgerService? ledger = null,
        LocalDocumentIndex? index = null,
        Func<string, IEnumerable<string>>? fileEnumerator = null)
    {
        var settings = Settings(root, enabled, maxBytes);
        var kill = new KillSwitchService(() => settings, ledger);
        return new LocalDocumentRetrievalService(
            () => settings,
            ledger,
            kill,
            index ?? new LocalDocumentIndex(indexPath),
            fileEnumerator);
    }

    private static Dictionary<string, string> Settings(string root, bool enabled, long maxBytes = 4194304)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [SettingKeys.LocalDocumentRetrievalEnabled] = enabled.ToString(),
            [SettingKeys.LocalDocumentRetrievalRoot] = root,
            [SettingKeys.LocalDocumentRetrievalMaxFileBytes] = maxBytes.ToString(),
            [KillSwitchService.KillSwitchSetting] = bool.FalseString
        };
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-local-doc-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
