using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record LocalBrainBenchmarkSmokeCase(
    string Prompt,
    string Provider,
    string Model,
    bool DidCallProvider,
    bool DidFallback,
    bool Passed,
    string Summary,
    string BlockReason);

public sealed record LocalBrainBenchmarkSmokeResult(
    bool Succeeded,
    bool Degraded,
    bool DidUseLocalModel,
    string Status,
    string ArtifactPath,
    IReadOnlyList<LocalBrainBenchmarkSmokeCase> Cases);

public sealed class LocalBrainBenchmarkSmokeTest
{
    public const string PacketKind = "local_brain_smoke_test_completed";

    public static IReadOnlyList<string> DefaultPrompts { get; } =
    [
        "Summarize Wevito's local-only AI safety rules in one sentence.",
        "List two safe next steps for reviewing sprite cleanup evidence.",
        "Explain why hosted AI providers are not the runtime brain.",
        "Describe how a pet helper should ask before mutating files.",
        "Give a short status message for an offline local model runtime."
    ];

    private readonly IModelAdapter _modelAdapter;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public LocalBrainBenchmarkSmokeTest(
        IModelAdapter modelAdapter,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _modelAdapter = modelAdapter;
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public async Task<LocalBrainBenchmarkSmokeResult> RunAsync(
        string artifactRoot,
        DateTimeOffset? nowUtc = null,
        CancellationToken cancellationToken = default)
    {
        var now = nowUtc ?? DateTimeOffset.UtcNow;
        var artifactPath = Path.GetFullPath(Path.Combine(artifactRoot, "local-brain-smoke-test.json"));

        if (_killSwitchService?.IsActive() == true)
        {
            var blocked = new LocalBrainBenchmarkSmokeResult(
                Succeeded: false,
                Degraded: true,
                DidUseLocalModel: false,
                Status: "Blocked",
                artifactPath,
                Cases: []);
            await WriteAndRecordAsync(blocked, now, "kill_switch=true", cancellationToken).ConfigureAwait(false);
            return blocked;
        }

        var cases = new List<LocalBrainBenchmarkSmokeCase>();
        foreach (var prompt in DefaultPrompts)
        {
            var request = new ModelRequest(
                PetId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                PetName: "Local Brain",
                AgentRole: "reasoning",
                ToolFamily: "localBrain",
                UserTask: prompt,
                ToolSummary: "C-PHASE 132 local brain smoke test.",
                ApprovedForModelCall: true,
                ArtifactRoot: artifactRoot,
                RequestedAtUtc: now);
            var response = await _modelAdapter.SuggestAsync(request, cancellationToken).ConfigureAwait(false);
            var hosted = ModelProviderModeService.IsHostedProviderId(response.Provider);
            var passed = !hosted && !string.IsNullOrWhiteSpace(response.Summary) && response.Summary.Trim().Length >= 10;
            cases.Add(new LocalBrainBenchmarkSmokeCase(
                prompt,
                response.Provider,
                response.Model,
                response.DidCallProvider,
                DidFallback: !response.DidCallProvider,
                passed,
                response.Summary,
                hosted ? "hosted_provider_refused" : response.BlockReason));
        }

        var containsHosted = cases.Any(candidate => ModelProviderModeService.IsHostedProviderId(candidate.Provider));
        var didUseLocalModel = cases.Any(candidate => candidate.DidCallProvider && !ModelProviderModeService.IsHostedProviderId(candidate.Provider));
        var succeeded = !containsHosted && cases.All(candidate => candidate.Passed);
        var degraded = !didUseLocalModel;
        var result = new LocalBrainBenchmarkSmokeResult(
            succeeded,
            degraded,
            didUseLocalModel,
            containsHosted ? "Blocked" : degraded ? "Degraded" : "Completed",
            artifactPath,
            cases);
        await WriteAndRecordAsync(result, now, containsHosted ? "hosted_provider_refused" : "", cancellationToken).ConfigureAwait(false);
        return result;
    }

    private async Task WriteAndRecordAsync(LocalBrainBenchmarkSmokeResult result, DateTimeOffset nowUtc, string error, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(result.ArtifactPath) ?? ".");
        await File.WriteAllTextAsync(result.ArtifactPath, JsonSerializer.Serialize(result, JsonDefaults.Options), cancellationToken).ConfigureAwait(false);
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            PacketKind,
            TaskCardId: null,
            CreatedAtUtc: nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: result.DidUseLocalModel,
            DidMutate: false,
            ArtifactPath: result.ArtifactPath,
            Summary: result.Degraded
                ? "Completed the local brain smoke test in deterministic fallback mode."
                : "Completed the local brain smoke test against the local model runtime.",
            Status: result.Status,
            Error: error));
    }
}
