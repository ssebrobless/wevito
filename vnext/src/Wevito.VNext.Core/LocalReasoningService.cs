using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class LocalReasoningService
{
    private readonly IModelAdapter _modelAdapter;
    private readonly CitationEnforcer _citationEnforcer;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly PromptConfig? _promptConfig;

    public LocalReasoningService(
        IModelAdapter? modelAdapter = null,
        CitationEnforcer? citationEnforcer = null,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null,
        PromptConfig? promptConfig = null)
    {
        _modelAdapter = modelAdapter ?? new LocalModelAdapter(killSwitchService);
        _citationEnforcer = citationEnforcer ?? new CitationEnforcer();
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
        _promptConfig = promptConfig;
    }

    public async Task<LocalReasoningResult> ReasonAsync(LocalReasoningRequest request, CancellationToken cancellationToken = default)
    {
        var timestamp = request.RequestedAtUtc == default ? DateTimeOffset.UtcNow : request.RequestedAtUtc;
        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        Directory.CreateDirectory(artifactRoot);

        if (_killSwitchService?.IsActive() == true)
        {
            return Block(request, timestamp, artifactRoot, "kill_switch=true");
        }

        if (request.Retrieved.Chunks.Count == 0)
        {
            var emptyPacket = WritePacket(request, timestamp, artifactRoot, prompt: "", response: "No local evidence was retrieved.", modelId: "none", citationCoverage: 0, didUseLocalModel: false);
            Record("local_reasoning", "No local evidence was retrieved; reasoning degraded safely.", "PreviewReady", timestamp, request.TaskCardId, emptyPacket.PacketPath);
            return new LocalReasoningResult(true, "No local evidence was retrieved. Add approved local documents before asking Wevito to synthesize claims.", 0, emptyPacket.Packet, emptyPacket.PacketPath);
        }

        var prompt = BuildPrompt(request);
        var response = await _modelAdapter.SuggestAsync(new ModelRequest(
            request.TaskCardId,
            "Local Wevito",
            request.HelperRole,
            request.ToolFamily,
            request.Question,
            prompt,
            TrustedContext: BuildTrustedContext(request, prompt),
            UntrustedContext: (request.UntrustedContext ?? []).Select(PetModelSummaryService.WrapUntrusted).ToList(),
            ApprovedForModelCall: true,
            ArtifactRoot: artifactRoot,
            RequestedAtUtc: timestamp), cancellationToken).ConfigureAwait(false);

        if (ModelProviderModeService.IsHostedProviderId(response.Provider))
        {
            return Block(request, timestamp, artifactRoot, "Hosted adapter response was refused by LocalReasoningService.");
        }

        var raw = string.IsNullOrWhiteSpace(response.Summary)
            ? BuildDeterministicCitedSummary(request)
            : response.Summary;
        var enforced = _citationEnforcer.Enforce(raw, request.Retrieved.Chunks);
        var packetResult = WritePacket(
            request,
            timestamp,
            artifactRoot,
            prompt,
            enforced.Text,
            $"{response.Provider}/{response.Model}",
            enforced.CitationCoverageRatio,
            response.DidCallProvider);
        Record("local_reasoning", $"Local reasoning completed with citation coverage {enforced.CitationCoverageRatio:0.00}.", "Completed", timestamp, request.TaskCardId, packetResult.PacketPath);
        return new LocalReasoningResult(true, enforced.Text, enforced.CitationCoverageRatio, packetResult.Packet, packetResult.PacketPath);
    }

    private LocalReasoningResult Block(LocalReasoningRequest request, DateTimeOffset timestamp, string artifactRoot, string reason)
    {
        var packetResult = WritePacket(request, timestamp, artifactRoot, prompt: "", response: "", modelId: "blocked", citationCoverage: 0, didUseLocalModel: false);
        Record("local_reasoning", reason, "Blocked", timestamp, request.TaskCardId, packetResult.PacketPath, reason);
        return new LocalReasoningResult(false, "", 0, packetResult.Packet, packetResult.PacketPath, reason);
    }

    private string BuildPrompt(LocalReasoningRequest request)
    {
        var rolePrompt = ResolveRolePrompt(request.HelperRole);
        var chunks = request.Retrieved.Chunks
            .Select((chunk, index) => $"[{index + 1}] {chunk.Text}")
            .ToList();
        var trusted = request.TrustedContext is { Count: > 0 }
            ? string.Join(Environment.NewLine, request.TrustedContext)
            : "None.";
        var untrusted = request.UntrustedContext is { Count: > 0 }
            ? string.Join(Environment.NewLine, request.UntrustedContext.Select(PetModelSummaryService.WrapUntrusted))
            : "None.";
        return $"""
            {rolePrompt}

            Tool family: {request.ToolFamily}
            Question: {request.Question}

            Local evidence chunks:
            {string.Join(Environment.NewLine, chunks)}

            Trusted context:
            {trusted}

            Untrusted context:
            {untrusted}

            Answer with claims grounded in the numbered local chunks. Every claim sentence must cite a chunk like [1].
            Do not use hosted AI, web content, hidden file reads, mutation, or uncited claims.
            """;
    }

    private string ResolveRolePrompt(PetHelperRole role)
    {
        var key = role switch
        {
            PetHelperRole.SpriteReviewHelper => "Scout",
            PetHelperRole.ChecklistHelper => "Inspector",
            PetHelperRole.ResearchHelper => "Builder",
            _ => "Scout"
        };
        if (_promptConfig?.Templates.TryGetValue(key, out var template) == true)
        {
            return template;
        }

        return key switch
        {
            "Inspector" => "You are Inspector, Wevito's local code and checklist helper. Be precise and cite local chunks.",
            "Builder" => "You are Builder, Wevito's local research helper. Synthesize only the provided local chunks.",
            _ => "You are Scout, Wevito's local evidence helper. Find grounded claims and cite local chunks."
        };
    }

    private static IReadOnlyList<string> BuildTrustedContext(LocalReasoningRequest request, string prompt)
    {
        return (request.TrustedContext ?? [])
            .Concat(request.Retrieved.Chunks.Select((chunk, index) => $"[{index + 1}] {chunk.ChunkId} {chunk.Path}"))
            .Append(prompt)
            .ToList();
    }

    private static string BuildDeterministicCitedSummary(LocalReasoningRequest request)
    {
        var first = request.Retrieved.Chunks.First();
        return $"Local evidence is available from {Path.GetFileName(first.Path)} [{1}].";
    }

    private (LocalReasoningEvidencePacket Packet, string PacketPath) WritePacket(
        LocalReasoningRequest request,
        DateTimeOffset timestamp,
        string artifactRoot,
        string prompt,
        string response,
        string modelId,
        double citationCoverage,
        bool didUseLocalModel)
    {
        var packetPath = Path.Combine(artifactRoot, "local-reasoning-packet.json");
        var packet = new LocalReasoningEvidencePacket(
            "1",
            request.TaskCardId,
            request.HelperRole.ToString(),
            request.ToolFamily,
            request.Question,
            request.Retrieved.Chunks.Select(chunk => chunk.ChunkId).ToList(),
            Sha256(prompt),
            Sha256(response),
            modelId,
            citationCoverage,
            didUseLocalModel,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidMutate: false,
            packetPath,
            timestamp);
        File.WriteAllText(packetPath, JsonSerializer.Serialize(packet, JsonDefaults.Options));
        return (packet, packetPath);
    }

    private void Record(string kind, string summary, string status, DateTimeOffset timestamp, Guid taskCardId, string artifactPath, string error = "")
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            kind,
            taskCardId,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: artifactPath,
            Summary: summary,
            Status: status,
            Error: error));
    }

    private static string ResolveArtifactRoot(string artifactRoot, DateTimeOffset timestamp)
    {
        return string.IsNullOrWhiteSpace(artifactRoot)
            ? Path.GetFullPath(Path.Combine("vnext", "artifacts", "pet-tasks", $"{timestamp:yyyyMMdd-HHmmss}-local-reasoning"))
            : Path.GetFullPath(artifactRoot);
    }

    private static string Sha256(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? ""))).ToLowerInvariant();
    }
}
