using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record ModelRequest(
    Guid PetId,
    string PetName,
    PetHelperRole HelperRole,
    string ToolFamily,
    string UserTask,
    string ToolSummary,
    IReadOnlyList<string>? TrustedContext = null,
    IReadOnlyList<string>? UntrustedContext = null,
    bool ApprovedForModelCall = false,
    string ArtifactRoot = "",
    DateTimeOffset RequestedAtUtc = default);

public sealed record ModelResponse(
    string Provider,
    string Model,
    string Summary,
    bool DidCallProvider,
    string BlockReason = "",
    string AuditLogPath = "",
    TimeSpan Latency = default);

public interface IModelAdapter
{
    Task<ModelResponse> SuggestAsync(ModelRequest request, CancellationToken cancellationToken = default);
}

public interface IModelCredentialStore
{
    Task<string?> ReadApiKeyAsync(string provider, CancellationToken cancellationToken = default);
}
