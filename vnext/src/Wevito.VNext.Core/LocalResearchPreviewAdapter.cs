using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class LocalResearchPreviewAdapter
{
    private const string ToolFamily = "localResearch";
    private readonly ResearchPlannerService _planner;
    private readonly WebResearchConnector? _webResearchConnector;

    public LocalResearchPreviewAdapter(ResearchPlannerService? planner = null, WebResearchConnector? webResearchConnector = null)
    {
        _planner = planner ?? new ResearchPlannerService();
        _webResearchConnector = webResearchConnector;
    }

    public TaskAdapterResult BuildPreview(TaskAdapterRequest request, DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (request.RunMode != TaskAdapterRunMode.DryRunPreview)
        {
            return Block(request, "Local research adapter only supports dry-run preview right now.", timestamp);
        }

        if (!string.Equals(request.PolicySnapshot.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Intent.RequestedToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, "Task intent and policy must both target localResearch.", timestamp);
        }

        if (request.PolicySnapshot.AccessMode != ToolAccessMode.ReadOnly)
        {
            return Block(request, "Local research preview requires a read-only policy.", timestamp);
        }

        var artifactRoot = ResolveArtifactRoot(request.ArtifactRoot, timestamp);
        if (!IsSafePetTaskArtifactRoot(artifactRoot))
        {
            return Block(request, "Local research artifacts must be written under a pet-tasks artifact folder.", timestamp);
        }

        var webFetches = TryFetchWebEvidence(request, timestamp, artifactRoot);
        var packet = _planner.Plan(new ResearchPlannerRequest(
            request.Intent.RawText,
            LocalMemory: request.Intent.TargetPathsOrAssets ?? [],
            LocalDocumentPaths: request.PolicySnapshot.ApprovedRootPaths,
            PriorToolReports: [],
            WebFetches: webFetches,
            AllowNetwork: webFetches.Count > 0,
            AllowHostedAi: false,
            RequestedAtUtc: timestamp));

        Directory.CreateDirectory(artifactRoot);
        var jsonPath = Path.Combine(artifactRoot, "research-evidence-packet.json");
        var markdownPath = Path.Combine(artifactRoot, "run-summary.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(packet, JsonDefaults.Options));
        File.WriteAllText(markdownPath, BuildMarkdown(packet, request));

        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            ReadPaths: packet.SourcesInspected
                .Where(source => source.Kind is ResearchSourceKind.LocalDocument or ResearchSourceKind.ToolReport)
                .Select(source => source.PathOrUri)
                .ToList(),
            WrittenPaths: [jsonPath, markdownPath],
            PreviewSummary: $"Wrote localResearch evidence packet for {packet.SourcesInspected.Count} source record(s). No hosted AI or network fetch was used.",
            ResultSummary: $"localResearch report ready: {markdownPath}",
            AuditLogPath: markdownPath,
            CompletedAtUtc: timestamp);
    }

    private IReadOnlyList<WebFetchRecord> TryFetchWebEvidence(TaskAdapterRequest request, DateTimeOffset timestamp, string artifactRoot)
    {
        if (_webResearchConnector is null || request.PolicySnapshot.AccessMode != ToolAccessMode.Network || request.RunMode != TaskAdapterRunMode.DryRunPreview)
        {
            return [];
        }

        var result = _webResearchConnector.FetchAsync(new WebResearchRequest(
            TaskCardId: request.TaskCardId,
            ApprovedTaskCard: true,
            Query: request.Intent.RawText,
            Backend: "",
            ArtifactRoot: artifactRoot,
            CacheRoot: "",
            Settings: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [WebResearchConnector.WebSearchEnabledSetting] = bool.FalseString,
                [WebResearchConnector.WebBackendSetting] = "offline"
            },
            RuntimeStatus: new RuntimeSupervisorStatus(RuntimeSupervisorMode.Active, true, true, false, "active", ""),
            RequestedAtUtc: timestamp)).GetAwaiter().GetResult();
        return result.Succeeded ? result.Records : [];
    }

    private static string BuildMarkdown(ResearchEvidencePacket packet, TaskAdapterRequest request)
    {
        var lines = new List<string>
        {
            "# PET TASKS Local Research Evidence Packet",
            "",
            $"Generated: {packet.CreatedAtUtc:O}",
            $"TaskCard: `{request.TaskCardId}`",
            $"Question: {packet.Question}",
            "",
            "## Safety",
            "",
            $"- Used hosted AI: {packet.DidUseHostedAi}",
            $"- Used network: {packet.DidUseNetwork}",
            "- Did mutate files: false",
            "",
            "## Synthesis",
            "",
            packet.Synthesis,
            "",
            "## Sources"
        };

        if (packet.SourcesInspected.Count == 0)
        {
            lines.Add("");
            lines.Add("No local sources were provided. The next step should collect local memory, docs, or prior reports before researching externally.");
        }
        else
        {
            lines.Add("");
            lines.AddRange(packet.SourcesInspected.Select(source => $"- `{source.Id}` {source.Kind}: {source.Title} -> {source.PathOrUri}"));
        }

        lines.Add("");
        lines.Add("## Claims");
        lines.Add("");
        if (packet.ClaimsExtracted.Count == 0)
        {
            lines.Add("No claims were extracted yet.");
        }
        else
        {
            lines.AddRange(packet.ClaimsExtracted.Select(claim => $"- confidence {claim.Confidence:0.00}: {claim.Claim} Sources: {string.Join(", ", claim.SourceIds)}. Uncertainty: {claim.Uncertainty}"));
        }

        lines.Add("");
        lines.Add("## Next Recommended Action");
        lines.Add("");
        lines.Add(packet.NextRecommendedAction);
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string ResolveArtifactRoot(string artifactRoot, DateTimeOffset timestamp)
    {
        if (!string.IsNullOrWhiteSpace(artifactRoot))
        {
            return Path.GetFullPath(artifactRoot);
        }

        var slug = timestamp.ToString("yyyyMMdd-HHmmss") + "-local-research";
        return Path.GetFullPath(Path.Combine("vnext", "artifacts", "pet-tasks", slug));
    }

    private static bool IsSafePetTaskArtifactRoot(string artifactRoot)
    {
        var fullPath = Path.GetFullPath(artifactRoot);
        var parts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Length >= 2 &&
               parts.Any(part => string.Equals(part, "pet-tasks", StringComparison.OrdinalIgnoreCase)) &&
               !parts.Any(part => part.StartsWith("candidate-frames", StringComparison.OrdinalIgnoreCase) ||
                                  part.StartsWith("backup-before-", StringComparison.OrdinalIgnoreCase) ||
                                  part.StartsWith("godot-packaged-proof-", StringComparison.OrdinalIgnoreCase));
    }

    private static TaskAdapterResult Block(TaskAdapterRequest request, string reason, DateTimeOffset timestamp)
    {
        return new TaskAdapterResult(
            request.TaskCardId,
            ToolFamily,
            TaskAdapterResultStatus.Blocked,
            DidMutate: false,
            ReadPaths: [],
            WrittenPaths: [],
            BlockReason: reason,
            CompletedAtUtc: timestamp);
    }
}
