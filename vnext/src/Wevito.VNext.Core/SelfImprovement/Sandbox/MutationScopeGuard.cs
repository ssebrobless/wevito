namespace Wevito.VNext.Core.Sandbox;

public static class MutationScopeGuard
{
    public static void ThrowIfOutsideScope(string intendedPath, string scopeRoot, string scopeName)
    {
        if (string.IsNullOrWhiteSpace(intendedPath))
        {
            throw new ArgumentException("Mutation path must not be empty.", nameof(intendedPath));
        }

        if (string.IsNullOrWhiteSpace(scopeRoot))
        {
            throw new ArgumentException("Mutation scope root must not be empty.", nameof(scopeRoot));
        }

        if (string.IsNullOrWhiteSpace(scopeName))
        {
            throw new ArgumentException("Mutation scope name must not be empty.", nameof(scopeName));
        }

        var fullPath = Path.GetFullPath(intendedPath);
        var fullRoot = Path.GetFullPath(scopeRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException($"Mutation path '{intendedPath}' resolved outside scope '{scopeName}'.");
    }
}
