using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record ToolInvocationResult(
    string ToolFamily,
    TaskAdapterResultStatus Status,
    string ResultJson,
    string Summary,
    string AuditLogPath = "",
    string Error = "")
{
    public bool IsSuccess => Status is TaskAdapterResultStatus.PreviewReady or TaskAdapterResultStatus.Completed;
}

