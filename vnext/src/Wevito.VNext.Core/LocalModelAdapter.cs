using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class LocalModelAdapter : IModelAdapter
{
    public const string Provider = "local";
    public const string Model = "deterministic-local-research-scaffold";

    public Task<ModelResponse> SuggestAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        var summary = BuildDeterministicSummary(request);
        return Task.FromResult(new ModelResponse(
            Provider,
            Model,
            summary,
            DidCallProvider: false,
            AuditLogPath: "",
            Latency: TimeSpan.Zero));
    }

    private static string BuildDeterministicSummary(ModelRequest request)
    {
        var trustedCount = request.TrustedContext?.Count ?? 0;
        var untrustedCount = request.UntrustedContext?.Count ?? 0;
        var action = request.ToolFamily switch
        {
            "localResearch" => "Collect local evidence, list source paths, extract claims, and mark uncertainty before recommending next steps.",
            "localDocs" => "Summarize local documents with source paths and avoid unsupported claims.",
            "spriteAudit" => "Use validator findings and contact-sheet evidence before proposing visual repair work.",
            "codeReview" => "Report concrete code risks with file paths and avoid editing without approval.",
            _ => "Prepare a report-only preview and ask for approval before risky execution."
        };

        return $"Local deterministic suggestion for {request.PetName}: {action} Context counts: trusted={trustedCount}, untrusted={untrustedCount}. No hosted model call was made.";
    }
}
