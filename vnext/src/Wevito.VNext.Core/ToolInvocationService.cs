using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class ToolInvocationService
{
    public const string ToolInvocationStartedPacketKind = "tool_invocation_started";
    public const string ToolInvocationCompletedPacketKind = "tool_invocation_completed";

    private readonly ToolRegistry _registry;
    private readonly UnifiedPolicyService _unifiedPolicyService;
    private readonly ToolResultBudgetService _toolResultBudgetService;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly Func<TaskIntent, IReadOnlyList<ToolPolicy>, ToolPolicyDecision>? _policyEvaluator;

    public ToolInvocationService(
        ToolRegistry registry,
        UnifiedPolicyService? unifiedPolicyService = null,
        ToolResultBudgetService? toolResultBudgetService = null,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null,
        Func<TaskIntent, IReadOnlyList<ToolPolicy>, ToolPolicyDecision>? policyEvaluator = null)
    {
        _registry = registry;
        _unifiedPolicyService = unifiedPolicyService ?? new UnifiedPolicyService(killSwitchService: killSwitchService);
        _toolResultBudgetService = toolResultBudgetService ?? new ToolResultBudgetService(auditLedgerService: auditLedgerService, killSwitchService: killSwitchService);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
        _policyEvaluator = policyEvaluator;
    }

    public async Task<ToolInvocationResult> InvokeAsync(string toolFamily, JsonElement args, CancellationToken cancellationToken = default)
    {
        var timestamp = DateTimeOffset.UtcNow;
        if (_killSwitchService?.IsActive() == true)
        {
            return Block(toolFamily, "kill_switch=true", timestamp);
        }

        var descriptor = _registry.Find(toolFamily);
        if (descriptor is null)
        {
            return Block(toolFamily, $"Tool '{toolFamily}' is not registered.", timestamp);
        }

        var taskCardId = Guid.NewGuid();
        var intent = new TaskIntent(
            Guid.NewGuid(),
            ExtractTaskText(args, descriptor),
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.Unknown,
            RequestedToolFamily: descriptor.ToolFamily,
            RiskLevel: descriptor.RiskLevel,
            NeedsApproval: descriptor.RequiresApproval,
            ExpectedOutput: descriptor.Description,
            CreatedAtUtc: timestamp);

        var policies = _registry.BuildPolicies();
        var decision = _policyEvaluator?.Invoke(intent, policies) ?? _unifiedPolicyService.EvaluateToolPolicy(intent, policies);
        if (decision.Status != ToolPolicyDecisionStatus.Allowed || decision.PolicySnapshot is null)
        {
            return Block(descriptor.ToolFamily, decision.Reason, timestamp, taskCardId);
        }

        Record(ToolInvocationStartedPacketKind, descriptor.ToolFamily, taskCardId, timestamp, $"Started AI tool invocation for {descriptor.ToolFamily}.", "Started");
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = new TaskAdapterRequest(taskCardId, intent, decision.PolicySnapshot, ArtifactRoot: BuildArtifactRoot(descriptor.ToolFamily), RequestedAtUtc: timestamp);
            var adapterResult = await descriptor.Adapter(request, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var rawJson = JsonSerializer.Serialize(new
            {
                adapterResult.Status,
                adapterResult.ToolFamily,
                adapterResult.PreviewSummary,
                adapterResult.ResultSummary,
                adapterResult.AuditLogPath,
                adapterResult.BlockReason
            }, JsonDefaults.Options);
            var budgeted = _toolResultBudgetService.FormatToolResult(descriptor.ToolFamily, rawJson, DateTimeOffset.UtcNow);
            var summary = string.IsNullOrWhiteSpace(adapterResult.ResultSummary)
                ? adapterResult.PreviewSummary
                : adapterResult.ResultSummary;
            Record(ToolInvocationCompletedPacketKind, descriptor.ToolFamily, taskCardId, DateTimeOffset.UtcNow, summary, adapterResult.Status.ToString(), adapterResult.BlockReason);
            return new ToolInvocationResult(descriptor.ToolFamily, adapterResult.Status, budgeted.Truncated, summary, string.IsNullOrWhiteSpace(budgeted.FullPath) ? adapterResult.AuditLogPath : budgeted.FullPath, adapterResult.BlockReason);
        }
        catch (OperationCanceledException)
        {
            Record(ToolInvocationCompletedPacketKind, descriptor.ToolFamily, taskCardId, DateTimeOffset.UtcNow, "Tool invocation cancelled.", "Cancelled", "cancelled=true");
            throw;
        }
        catch (Exception ex)
        {
            var error = $"Tool invocation failed safely: {ex.GetType().Name}.";
            Record(ToolInvocationCompletedPacketKind, descriptor.ToolFamily, taskCardId, DateTimeOffset.UtcNow, error, "Failed", error);
            return new ToolInvocationResult(descriptor.ToolFamily, TaskAdapterResultStatus.Failed, "{}", error, Error: error);
        }
    }

    private ToolInvocationResult Block(string toolFamily, string reason, DateTimeOffset timestamp, Guid? taskCardId = null)
    {
        Record(ToolInvocationCompletedPacketKind, toolFamily, taskCardId, timestamp, reason, "Blocked", reason);
        return new ToolInvocationResult(toolFamily, TaskAdapterResultStatus.Blocked, "{}", reason, Error: reason);
    }

    private void Record(string packetKind, string toolFamily, Guid? taskCardId, DateTimeOffset timestamp, string summary, string status, string error = "")
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            taskCardId,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: BuildArtifactRoot(toolFamily),
            Summary: summary,
            Status: status,
            Error: error));
    }

    private static string ExtractTaskText(JsonElement args, ToolDescriptor descriptor)
    {
        if (args.ValueKind == JsonValueKind.Object)
        {
            foreach (var key in new[] { "task_text", "query", "target", "text" })
            {
                if (args.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString() ?? descriptor.Description;
                }
            }
        }

        return descriptor.Description;
    }

    private static string BuildArtifactRoot(string toolFamily)
    {
        var safeFamily = string.Join("-", (toolFamily ?? "tool").Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return Path.Combine("vnext", "artifacts", "tool-invocations", $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{safeFamily}");
    }
}
