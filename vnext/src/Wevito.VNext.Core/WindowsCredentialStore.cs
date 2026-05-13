namespace Wevito.VNext.Core;

public interface IWebCredentialStore
{
    Task<string?> ReadSecretAsync(string targetName, CancellationToken cancellationToken = default);
}

public sealed class WindowsCredentialStore : IWebCredentialStore
{
    public Task<string?> ReadSecretAsync(string targetName, CancellationToken cancellationToken = default)
    {
        // Real Credential Manager read is intentionally deferred behind explicit
        // user approval. C-PHASE 70 wires the target-name contract only.
        return Task.FromResult<string?>(null);
    }

    public static string BuildWebSearchTargetName(string backend)
    {
        return $"Wevito/web-search/{backend}";
    }
}
