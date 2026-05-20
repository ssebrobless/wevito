using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Scoring;

namespace Wevito.VNext.Tests;

public sealed class SupervisedScoringDryRunServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-19T12:00:00Z");

    [Fact]
    public void Run_KillSwitchActive_ReturnsRefusedAndWritesNoPacket()
    {
        var fixture = Fixture.Create(settings: EnabledSettings(), killSwitchActive: true);
        fixture.SeedAwaitingApproval();

        var result = fixture.Service.Run(fixture.OperationId, Now, CancellationToken.None);

        Assert.Equal("Refused", result.ResultKind);
        Assert.Equal("kill_switch=true", result.Reason);
        Assert.Empty(fixture.ScoringRows());
    }

    [Fact]
    public void Run_FlagOff_ReturnsRefusedAndWritesNoPacket()
    {
        var fixture = Fixture.Create(settings: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        fixture.SeedAwaitingApproval();

        var result = fixture.Service.Run(fixture.OperationId, Now, CancellationToken.None);

        Assert.Equal("Refused", result.ResultKind);
        Assert.Equal("supervised_scoring_dry_run_enabled=false", result.Reason);
        Assert.Empty(fixture.ScoringRows());
    }

    [Fact]
    public void Run_NoAwaitingApproval_ReturnsRefusedAndWritesNoPacket()
    {
        var fixture = Fixture.Create(settings: EnabledSettings());

        var result = fixture.Service.Run(fixture.OperationId, Now, CancellationToken.None);

        Assert.Equal("Refused", result.ResultKind);
        Assert.Equal("no_awaiting_approval_packet", result.Reason);
        Assert.Empty(fixture.ScoringRows());
    }

    [Fact]
    public void Run_DefaultProviderRefusesAndWritesOneSafePacket()
    {
        var fixture = Fixture.Create(
            settings: EnabledSettings(),
            providerFactory: killSwitch => new NotConfiguredScoringProvider(killSwitch));
        fixture.SeedAwaitingApproval();

        var result = fixture.Service.Run(fixture.OperationId, Now, CancellationToken.None);

        Assert.Equal("Refused", result.ResultKind);
        Assert.Equal("local_scoring_provider_not_configured", result.Reason);
        var row = Assert.Single(fixture.ScoringRows());
        Assert.False(row.DidUseNetwork);
        Assert.False(row.DidUseHostedAi);
        Assert.False(row.DidUseLocalModel);
        Assert.False(row.DidMutate);
        Assert.Equal("Refused", row.Status);
        AssertSafeSummary(row, fixture.ScopeHash);
    }

    [Fact]
    public void Run_OllamaProviderWithBothFlagsOff_WritesRefusedPacket()
    {
        var settings = EnabledSettings();
        var fixture = Fixture.Create(
            settings: settings,
            providerFactory: killSwitch => new OllamaLoopbackScoringProvider(
                new FakeScoringHttpClient("{\"response\":\"0.8\",\"model\":\"qwen2.5\"}"),
                killSwitch,
                () => settings));
        fixture.SeedAwaitingApproval();

        var result = fixture.Service.Run(fixture.OperationId, Now, CancellationToken.None);

        Assert.Equal("Refused", result.ResultKind);
        Assert.Equal("local_scoring_provider_enabled=false", result.Reason);
        var row = Assert.Single(fixture.ScoringRows());
        Assert.False(row.DidUseNetwork);
        Assert.False(row.DidUseLocalModel);
        AssertSafeSummary(row, fixture.ScopeHash);
    }

    [Fact]
    public void Run_OllamaProviderWithBothFlagsOnAndFakeHttp_WritesScoredPacket()
    {
        var settings = EnabledSettings();
        settings[NotConfiguredScoringProvider.EnabledSetting] = bool.TrueString;
        settings[OllamaLoopbackScoringProvider.OllamaEnabledSetting] = bool.TrueString;
        var fixture = Fixture.Create(
            settings: settings,
            providerFactory: killSwitch => new OllamaLoopbackScoringProvider(
                new FakeScoringHttpClient("{\"response\":\"0.8\",\"model\":\"qwen2.5\"}"),
                killSwitch,
                () => settings));
        fixture.SeedAwaitingApproval();

        var result = fixture.Service.Run(fixture.OperationId, Now, CancellationToken.None);

        Assert.Equal("Scored", result.ResultKind);
        Assert.Equal("scored", result.Reason);
        Assert.Equal("qwen2.5", result.ModelIdentity);
        var row = Assert.Single(fixture.ScoringRows());
        Assert.True(row.DidUseNetwork);
        Assert.True(row.DidUseLocalModel);
        Assert.False(row.DidUseHostedAi);
        Assert.False(row.DidMutate);
        Assert.Equal("Scored", row.Status);
        AssertSafeSummary(row, fixture.ScopeHash);
    }

    [Fact]
    public void Run_BuildsPromptHashFromOperationAndScopeHashOnly()
    {
        var provider = new RecordingScoringProvider(new LocalScoringResult.Refused("recorded"));
        var fixture = Fixture.Create(settings: EnabledSettings(), providerFactory: _ => provider);
        fixture.SeedAwaitingApproval();

        fixture.Service.Run(fixture.OperationId, Now, CancellationToken.None);

        var request = Assert.Single(provider.Requests);
        Assert.Equal(Sha256($"{fixture.OperationId}|{fixture.ScopeHash}"), request.PromptSha256);
        Assert.Equal(SupervisedScoringDryRunService.RubricId, request.Rubric);
    }

    [Fact]
    public void LocalScoringRequest_OnlyExposesPromptSha256AndRubric()
    {
        var properties = typeof(LocalScoringRequest).GetProperties()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["PromptSha256", "Rubric"], properties);
    }

    [Fact]
    public void Source_DoesNotSerializeScorePromptOrRawPrompt()
    {
        var source = File.ReadAllText(SourcePath());

        Assert.DoesNotContain("\"score\":", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"prompt\":", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw_prompt", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IHeldOutEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IInDistributionEvalStore", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PacketKind_IsKnownToPlainLanguageExplainer()
    {
        Assert.Contains(SelfImprovementPacketKinds.ScoringDryRun, PlainLanguageExplainer.KnownPacketKinds);
        Assert.Equal(
            "Dry-run of the supervised self-improvement scoring contract. Carries no raw prompt and never mutates code.",
            new PlainLanguageExplainer().ExplainPacketKind(SelfImprovementPacketKinds.ScoringDryRun));
    }

    [Fact]
    public void CapabilityFlag_DefaultsOff()
    {
        var entry = Assert.Single(CapabilityFlagInventory.Entries, entry => entry.Name == SupervisedScoringDryRunService.EnabledSetting);

        Assert.Equal(bool.FalseString, entry.DefaultValue);
        Assert.Contains("Default off", entry.PlainLanguage, StringComparison.Ordinal);
    }

    private static Dictionary<string, string> EnabledSettings()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [SupervisedScoringDryRunService.EnabledSetting] = bool.TrueString
        };
    }

    private static void AssertSafeSummary(AuditLedgerRow row, string scopeHash)
    {
        Assert.DoesNotContain(scopeHash, row.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain("\"score\"", row.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"prompt\"", row.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw prompt", row.Summary, StringComparison.OrdinalIgnoreCase);
        using var summary = JsonDocument.Parse(row.Summary);
        Assert.Equal(SupervisedScoringDryRunService.RubricId, summary.RootElement.GetProperty("rubric_id").GetString());
    }

    private static string SourcePath()
    {
        return Path.Combine(
            FindRepositoryRoot(),
            "vnext",
            "src",
            "Wevito.VNext.Core",
            "SelfImprovement",
            "Scoring",
            "SupervisedScoringDryRunService.cs");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "wevito.godot")) ||
                Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }

    private static string Sha256(string text)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }

    private sealed class Fixture
    {
        private Fixture(
            string root,
            string operationId,
            string scopeHash,
            AuditLedgerService ledger,
            SupervisedScoringDryRunService service)
        {
            Root = root;
            OperationId = operationId;
            ScopeHash = scopeHash;
            Ledger = ledger;
            Service = service;
        }

        public string Root { get; }
        public string OperationId { get; }
        public string ScopeHash { get; }
        public AuditLedgerService Ledger { get; }
        public SupervisedScoringDryRunService Service { get; }

        public static Fixture Create(
            IReadOnlyDictionary<string, string> settings,
            bool killSwitchActive = false,
            Func<KillSwitchService, ILocalScoringProvider>? providerFactory = null)
        {
            var root = Path.Combine(Path.GetTempPath(), "wevito-scoring-dry-run", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var operationId = $"operation-{Guid.NewGuid():N}";
            var scopeHash = new string('a', 64);
            var databasePath = Path.Combine(root, "ledger.sqlite");
            var ledger = new AuditLedgerService(databasePath);
            _ = ledger.Snapshot(Now.AddHours(-1), Now.AddHours(1));
            var allSettings = new Dictionary<string, string>(settings, StringComparer.OrdinalIgnoreCase)
            {
                [KillSwitchService.KillSwitchSetting] = killSwitchActive.ToString()
            };
            var killSwitch = new KillSwitchService(() => allSettings);
            var provider = providerFactory?.Invoke(killSwitch) ?? new RecordingScoringProvider(new LocalScoringResult.Refused("recorded"));
            var service = new SupervisedScoringDryRunService(databasePath, ledger, provider, killSwitch, () => allSettings);
            return new Fixture(root, operationId, scopeHash, ledger, service);
        }

        public void SeedAwaitingApproval()
        {
            var artifactPath = Path.Combine(Root, "vnext", "artifacts", OperationId, "apply-awaiting-approval.json");
            Directory.CreateDirectory(Path.GetDirectoryName(artifactPath)!);
            File.WriteAllText(artifactPath, JsonSerializer.Serialize(new
            {
                operationId = OperationId,
                scopeHash = ScopeHash,
                applyRunner = "not_implemented_in_v0"
            }, JsonDefaults.Options));

            Ledger.Record(new EvidencePacket(
                Guid.NewGuid(),
                SelfImprovementPacketKinds.ApplyAwaitingApproval,
                Guid.NewGuid(),
                Now.AddMinutes(-1),
                DidUseNetwork: false,
                DidUseHostedAi: false,
                DidUseLocalModel: false,
                DidMutate: false,
                ArtifactPath: artifactPath,
                Summary: $"Awaiting approval for operation {OperationId}.",
                Status: "WaitingForApproval"));
        }

        public IReadOnlyList<AuditLedgerRow> ScoringRows()
        {
            return Ledger.Snapshot(Now.AddHours(-1), Now.AddHours(1))
                .Where(row => row.PacketKind.Equals(SelfImprovementPacketKinds.ScoringDryRun, StringComparison.Ordinal))
                .ToArray();
        }
    }

    private sealed record RecordingScoringProvider(LocalScoringResult Result) : ILocalScoringProvider
    {
        public List<LocalScoringRequest> Requests { get; } = [];

        public override LocalScoringResult Score(LocalScoringRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Result;
        }
    }
}
