using System.Security.Cryptography;

namespace Wevito.VNext.Core;

public sealed class LocalToolAccessPolicy
{
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly string[] DeniedSegments = [".git", "secrets", "credentials"];
    private readonly string _repoRoot;
    private readonly IReadOnlyList<string> _defaultAllowedRoots;
    private readonly IReadOnlyList<string> _absoluteDeniedRoots;

    public LocalToolAccessPolicy(string? repoRoot = null, IReadOnlyList<string>? allowedRoots = null)
    {
        _repoRoot = EnsureTrailingSeparator(Path.GetFullPath(repoRoot ?? Directory.GetCurrentDirectory()));
        _defaultAllowedRoots = (allowedRoots is { Count: > 0 } ? allowedRoots : BuildDefaultAllowedRoots(_repoRoot))
            .Select(root => NormalizeRoot(root, _repoRoot))
            .Distinct(PathComparer)
            .ToList();
        _absoluteDeniedRoots = BuildAbsoluteDeniedRoots()
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(EnsureTrailingSeparator)
            .Distinct(PathComparer)
            .ToList();
    }

    public string RepoRoot => _repoRoot;
    public IReadOnlyList<string> DefaultAllowedRoots => _defaultAllowedRoots;

    public PolicyDecision EvaluateRead(string path, IReadOnlyList<string>? approvedRoots = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Block(PolicyDecisionScope.FileRead, path, "Read path is required.");
        }

        if (ContainsParentTraversal(path))
        {
            return Block(PolicyDecisionScope.FileRead, path, "Path contains parent traversal ('..') and is blocked.");
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path, _repoRoot);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return Block(PolicyDecisionScope.FileRead, path, $"Path could not be normalized: {ex.Message}");
        }

        var deniedReason = FindDenyReason(fullPath);
        if (!string.IsNullOrWhiteSpace(deniedReason))
        {
            return Block(PolicyDecisionScope.FileRead, fullPath, deniedReason, fullPath);
        }

        if (HasReparsePointInExistingChain(fullPath))
        {
            return Block(PolicyDecisionScope.FileRead, fullPath, "Path crosses a symlink or reparse point and is blocked.", fullPath);
        }

        var roots = ResolveAllowedRoots(approvedRoots);
        if (!roots.Any(root => IsInsideRoot(fullPath, root)))
        {
            return Block(PolicyDecisionScope.FileRead, fullPath, "Path is outside approved local file roots.", fullPath);
        }

        return new PolicyDecision(
            PolicyDecisionScope.FileRead,
            fullPath,
            Wevito.VNext.Contracts.ToolPolicyDecisionStatus.Allowed,
            Wevito.VNext.Contracts.ToolRiskLevel.Low,
            Wevito.VNext.Contracts.ApprovalRequirement.None,
            "Path is inside approved local file roots and outside denylisted areas.",
            fullPath);
    }

    public PolicyDecision EvaluateToolScript(
        string scriptPath,
        IReadOnlyDictionary<string, string>? settings,
        IReadOnlyDictionary<string, string>? allowedScriptSha256 = null)
    {
        if (settings is null ||
            !settings.TryGetValue("local_tool_exec_enabled", out var enabledRaw) ||
            !bool.TryParse(enabledRaw, out var enabled) ||
            !enabled)
        {
            return Block(PolicyDecisionScope.LocalToolExecution, scriptPath, "local_tool_exec_enabled=false");
        }

        var readDecision = EvaluateRead(scriptPath, [Path.Combine(_repoRoot, "tools")]);
        if (readDecision.IsBlocked)
        {
            return readDecision with
            {
                Scope = PolicyDecisionScope.LocalToolExecution,
                Reason = $"Script is not inside the approved tools root: {readDecision.Reason}"
            };
        }

        var fullPath = readDecision.NormalizedPath ?? Path.GetFullPath(scriptPath, _repoRoot);
        if (!File.Exists(fullPath))
        {
            return Block(PolicyDecisionScope.LocalToolExecution, fullPath, "Script file does not exist.", fullPath);
        }

        if (!Path.GetExtension(fullPath).Equals(".ps1", StringComparison.OrdinalIgnoreCase))
        {
            return Block(PolicyDecisionScope.LocalToolExecution, fullPath, "Only PowerShell scripts under tools/ may be previewed in C-PHASE 71.", fullPath);
        }

        var actualHash = ComputeSha256(fullPath);
        var expectedHash = ResolveExpectedHash(fullPath, settings, allowedScriptSha256);
        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            return Block(PolicyDecisionScope.LocalToolExecution, fullPath, "No sha256 allowlist entry exists for this script.", fullPath);
        }

        if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            return Block(PolicyDecisionScope.LocalToolExecution, fullPath, "Script sha256 does not match the allowlist.", fullPath);
        }

        return new PolicyDecision(
            PolicyDecisionScope.LocalToolExecution,
            fullPath,
            Wevito.VNext.Contracts.ToolPolicyDecisionStatus.ApprovalRequired,
            Wevito.VNext.Contracts.ToolRiskLevel.High,
            Wevito.VNext.Contracts.ApprovalRequirement.BeforeExecution,
            "Script is allowlisted by path and sha256. C-PHASE 71 only permits dry-run preview, not execution.",
            fullPath);
    }

    private IReadOnlyList<string> ResolveAllowedRoots(IReadOnlyList<string>? approvedRoots)
    {
        return (approvedRoots is { Count: > 0 } ? approvedRoots : _defaultAllowedRoots)
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(root => NormalizeRoot(root, _repoRoot))
            .Distinct(PathComparer)
            .ToList();
    }

    private string FindDenyReason(string fullPath)
    {
        var segments = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (segments.Any(segment => DeniedSegments.Contains(segment, StringComparer.OrdinalIgnoreCase)))
        {
            return "Path contains a denylisted segment (.git, secrets, or credentials).";
        }

        if (Path.GetFileName(fullPath).StartsWith(".env", StringComparison.OrdinalIgnoreCase))
        {
            return "Path targets a denylisted environment file.";
        }

        if (_absoluteDeniedRoots.Any(root => IsInsideRoot(fullPath, root)))
        {
            return "Path is inside a denylisted user credential or Microsoft application data root.";
        }

        return "";
    }

    private static IReadOnlyList<string> BuildDefaultAllowedRoots(string repoRoot)
    {
        return
        [
            Path.Combine(repoRoot, "vnext"),
            Path.Combine(repoRoot, "docs"),
            Path.Combine(repoRoot, "tools"),
            Path.Combine(repoRoot, "vnext", "artifacts")
        ];
    }

    private static IReadOnlyList<string> BuildAbsoluteDeniedRoots()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return
        [
            string.IsNullOrWhiteSpace(userProfile) ? "" : Path.Combine(userProfile, ".ssh"),
            string.IsNullOrWhiteSpace(localAppData) ? "" : Path.Combine(localAppData, "Microsoft"),
            string.IsNullOrWhiteSpace(appData) ? "" : Path.Combine(appData, "Microsoft")
        ];
    }

    private static bool ContainsParentTraversal(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(part => part == "..");
    }

    private static bool HasReparsePointInExistingChain(string fullPath)
    {
        var candidate = File.Exists(fullPath)
            ? Path.GetDirectoryName(fullPath)
            : Directory.Exists(fullPath)
                ? fullPath
                : FindExistingAncestor(fullPath);

        while (!string.IsNullOrWhiteSpace(candidate))
        {
            try
            {
                var attributes = File.GetAttributes(candidate);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    return true;
                }
            }
            catch
            {
                return true;
            }

            candidate = Path.GetDirectoryName(candidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        return false;
    }

    private static string? FindExistingAncestor(string fullPath)
    {
        var candidate = Path.GetDirectoryName(fullPath);
        while (!string.IsNullOrWhiteSpace(candidate))
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.GetDirectoryName(candidate);
        }

        return null;
    }

    private static string ResolveExpectedHash(
        string fullPath,
        IReadOnlyDictionary<string, string> settings,
        IReadOnlyDictionary<string, string>? allowedScriptSha256)
    {
        var fileName = Path.GetFileName(fullPath);
        if (allowedScriptSha256 is not null)
        {
            if (allowedScriptSha256.TryGetValue(fullPath, out var byPath))
            {
                return byPath;
            }

            if (allowedScriptSha256.TryGetValue(fileName, out var byName))
            {
                return byName;
            }
        }

        return settings.TryGetValue($"local_tool_exec_sha256:{fileName}", out var scoped)
            ? scoped
            : settings.TryGetValue("local_tool_exec_allowed_sha256", out var generic)
                ? generic
                : "";
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static bool IsInsideRoot(string path, string root)
    {
        var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return normalizedPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
               normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRoot(string root, string repoRoot)
    {
        return EnsureTrailingSeparator(Path.IsPathFullyQualified(root)
            ? Path.GetFullPath(root)
            : Path.GetFullPath(root, repoRoot));
    }

    private static string EnsureTrailingSeparator(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return fullPath.EndsWith(Path.DirectorySeparatorChar) || fullPath.EndsWith(Path.AltDirectorySeparatorChar)
            ? fullPath
            : fullPath + Path.DirectorySeparatorChar;
    }

    private static PolicyDecision Block(PolicyDecisionScope scope, string subject, string reason, string? normalizedPath = null)
    {
        return new PolicyDecision(
            scope,
            subject,
            Wevito.VNext.Contracts.ToolPolicyDecisionStatus.Blocked,
            Wevito.VNext.Contracts.ToolRiskLevel.Blocked,
            Wevito.VNext.Contracts.ApprovalRequirement.HandOffRequired,
            reason,
            normalizedPath);
    }
}
