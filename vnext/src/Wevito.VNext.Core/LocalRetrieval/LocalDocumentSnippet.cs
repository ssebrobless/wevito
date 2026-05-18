namespace Wevito.VNext.Core.LocalRetrieval;

public sealed record LocalDocumentSnippet(
    string RelativePath,
    int LineNumber,
    string SnippetText,
    double Score);

public sealed record LocalDocumentIndexBuildResult(
    bool Success,
    string Status,
    string Reason,
    int IndexedFiles,
    int SkippedFiles,
    int IndexedLines,
    string IndexPath,
    IReadOnlyList<string> SkippedPaths);

public sealed class LocalDocumentSandboxException : InvalidOperationException
{
    public LocalDocumentSandboxException(string message)
        : base(message)
    {
    }
}
