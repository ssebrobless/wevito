using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class PetModelSummaryService
{
    private readonly IModelAdapter? _modelAdapter;
    private readonly HelperAllowlistEvaluator _allowlistEvaluator;

    public PetModelSummaryService(
        IModelAdapter? modelAdapter = null,
        HelperAllowlistEvaluator? allowlistEvaluator = null)
    {
        _modelAdapter = modelAdapter;
        _allowlistEvaluator = allowlistEvaluator ?? new HelperAllowlistEvaluator();
    }

    public async Task<TaskAdapterResult> AppendIfAllowedAsync(
        TaskAdapterRequest request,
        TaskAdapterResult result,
        PetHelperProfile helper,
        bool approvedForModelCall = false,
        CancellationToken cancellationToken = default)
    {
        if (_modelAdapter is null || result.Status != TaskAdapterResultStatus.PreviewReady || !approvedForModelCall)
        {
            return result;
        }

        var decision = _allowlistEvaluator.Evaluate(helper, result.ToolFamily, HelperPermissionMode.Preview);
        if (!decision.IsAllowed)
        {
            return result;
        }

        var modelRequest = new ModelRequest(
            helper.PetId,
            helper.PetNameSnapshot,
            helper.Role,
            result.ToolFamily,
            request.Intent.RawText,
            BuildToolSummary(result),
            TrustedContext: BuildTrustedContext(request, result),
            UntrustedContext: BuildUntrustedContext(request, result, decision.Capability),
            ApprovedForModelCall: approvedForModelCall,
            ArtifactRoot: request.ArtifactRoot,
            RequestedAtUtc: request.RequestedAtUtc == default ? DateTimeOffset.UtcNow : request.RequestedAtUtc);

        var response = await _modelAdapter.SuggestAsync(modelRequest, cancellationToken).ConfigureAwait(false);
        if (!response.DidCallProvider || string.IsNullOrWhiteSpace(response.Summary))
        {
            return result;
        }

        var preview = string.IsNullOrWhiteSpace(result.PreviewSummary)
            ? $"Model suggestion: {response.Summary}"
            : $"{result.PreviewSummary}{Environment.NewLine}{Environment.NewLine}Model suggestion: {response.Summary}";
        var written = (result.WrittenPaths ?? []).Concat(string.IsNullOrWhiteSpace(response.AuditLogPath) ? [] : [response.AuditLogPath]).ToList();

        return result with
        {
            PreviewSummary = preview,
            WrittenPaths = written,
            AuditLogPath = string.IsNullOrWhiteSpace(response.AuditLogPath) ? result.AuditLogPath : response.AuditLogPath
        };
    }

    public static string WrapUntrusted(string value)
    {
        return $"<untrusted>{Environment.NewLine}{value}{Environment.NewLine}</untrusted>";
    }

    private static string BuildToolSummary(TaskAdapterResult result)
    {
        return string.Join(Environment.NewLine, new[]
        {
            result.PreviewSummary,
            result.ResultSummary,
            string.IsNullOrWhiteSpace(result.BlockReason) ? "" : $"Blocked: {result.BlockReason}"
        }.Where(line => !string.IsNullOrWhiteSpace(line)));
    }

    private static IReadOnlyList<string> BuildTrustedContext(TaskAdapterRequest request, TaskAdapterResult result)
    {
        return
        [
            $"TaskCardId: {request.TaskCardId}",
            $"ToolFamily: {result.ToolFamily}",
            $"RunMode: {request.RunMode}",
            $"DidMutate: {result.DidMutate}"
        ];
    }

    private static IReadOnlyList<string> BuildUntrustedContext(TaskAdapterRequest request, TaskAdapterResult result, HelperToolCapability? capability)
    {
        if (capability is null || !capability.ReadsUntrustedExternal)
        {
            return [];
        }

        var paths = result.ReadPaths is { Count: > 0 }
            ? string.Join(Environment.NewLine, result.ReadPaths.Take(20))
            : "No read paths.";
        return
        [
            $"User task text: {request.Intent.RawText}",
            $"Tool read paths: {paths}"
        ];
    }
}
