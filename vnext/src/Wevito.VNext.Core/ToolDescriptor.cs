using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public delegate Task<TaskAdapterResult> ToolPreviewAdapterDelegate(TaskAdapterRequest request, CancellationToken cancellationToken);

public sealed record ToolDescriptor(
    string ToolFamily,
    string Name,
    string Description,
    string ArgumentSchema,
    string ReturnSchema,
    ToolRiskLevel RiskLevel,
    bool RequiresApproval,
    ToolPreviewAdapterDelegate Adapter);

