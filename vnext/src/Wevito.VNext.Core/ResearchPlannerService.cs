namespace Wevito.VNext.Core;

public sealed record ResearchPlannerRequest(
    string Question,
    IReadOnlyList<string>? LocalMemory = null,
    IReadOnlyList<string>? LocalDocumentPaths = null,
    IReadOnlyList<string>? PriorToolReports = null,
    bool AllowNetwork = false,
    bool AllowHostedAi = false,
    DateTimeOffset RequestedAtUtc = default);

public sealed class ResearchPlannerService
{
    public ResearchEvidencePacket Plan(ResearchPlannerRequest request)
    {
        var timestamp = request.RequestedAtUtc == default ? DateTimeOffset.UtcNow : request.RequestedAtUtc;
        var sources = BuildSources(request).ToList();
        var claims = BuildClaims(request, sources).ToList();
        var networkRequested = request.AllowNetwork;
        var synthesis = BuildSynthesis(request, sources, claims, networkRequested);

        return new ResearchEvidencePacket(
            request.Question.Trim(),
            request.LocalMemory?.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList() ?? [],
            sources,
            claims,
            synthesis,
            BuildNextAction(request, networkRequested),
            DidUseHostedAi: false,
            DidUseNetwork: false,
            timestamp);
    }

    private static IEnumerable<ResearchSourceRecord> BuildSources(ResearchPlannerRequest request)
    {
        var index = 1;
        foreach (var memory in request.LocalMemory ?? [])
        {
            if (string.IsNullOrWhiteSpace(memory))
            {
                continue;
            }

            yield return new ResearchSourceRecord($"memory-{index++}", ResearchSourceKind.LocalMemory, "Local memory", memory.Trim());
        }

        foreach (var path in request.LocalDocumentPaths ?? [])
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            yield return new ResearchSourceRecord($"doc-{index++}", ResearchSourceKind.LocalDocument, Path.GetFileName(path.Trim()), path.Trim());
        }

        foreach (var report in request.PriorToolReports ?? [])
        {
            if (string.IsNullOrWhiteSpace(report))
            {
                continue;
            }

            yield return new ResearchSourceRecord($"report-{index++}", ResearchSourceKind.ToolReport, "Prior tool report", report.Trim());
        }

        if (request.AllowNetwork)
        {
            yield return new ResearchSourceRecord(
                $"web-{index}",
                ResearchSourceKind.WebSourcePlaceholder,
                "Network research placeholder",
                "network research not fetched in local-first scaffold",
                IsNetworkSource: true,
                WasFetched: false);
        }
    }

    private static IEnumerable<ResearchClaimRecord> BuildClaims(ResearchPlannerRequest request, IReadOnlyList<ResearchSourceRecord> sources)
    {
        var sourceIds = sources.Where(source => !source.IsNetworkSource).Select(source => source.Id).ToList();
        if (sourceIds.Count > 0)
        {
            yield return new ResearchClaimRecord(
                "Local evidence is available and should be synthesized before any hosted AI or network research is considered.",
                sourceIds,
                0.82,
                "Claim is based on local source presence, not semantic document understanding yet.");
        }

        if (request.AllowNetwork)
        {
            yield return new ResearchClaimRecord(
                "Network research was requested, but C-PHASE 64 keeps fetching disabled and records only a placeholder.",
                sources.Where(source => source.IsNetworkSource).Select(source => source.Id).ToList(),
                0.95,
                "No web source was fetched in this scaffold.");
        }

        if (request.AllowHostedAi)
        {
            yield return new ResearchClaimRecord(
                "Hosted AI was requested, but local-first mode does not call a provider in C-PHASE 64.",
                [],
                0.95,
                "A later phase may add explicit approval-gated hosted fallback.");
        }
    }

    private static string BuildSynthesis(ResearchPlannerRequest request, IReadOnlyList<ResearchSourceRecord> sources, IReadOnlyList<ResearchClaimRecord> claims, bool networkRequested)
    {
        var question = string.IsNullOrWhiteSpace(request.Question) ? "Unspecified research question" : request.Question.Trim();
        var localCount = sources.Count(source => !source.IsNetworkSource);
        var networkNote = networkRequested
            ? " Network fetching was requested but intentionally not performed."
            : "";
        return $"Research plan for '{question}': inspect {localCount} local source(s), extract claims with source ids, and produce a reviewable report before taking action.{networkNote} Hosted AI was not used.";
    }

    private static string BuildNextAction(ResearchPlannerRequest request, bool networkRequested)
    {
        if (networkRequested)
        {
            return "Ask for explicit approval before enabling any network-backed research adapter; meanwhile continue with local evidence.";
        }

        return "Create a report-only PET TASKS preview using local evidence, then ask the user before executing risky work.";
    }
}
