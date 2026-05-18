namespace Wevito.VNext.Core.SelfImprovement.Eval;

public sealed class HeldOutEvalStore : IHeldOutEvalStore
{
    private readonly string _root;
    private readonly KillSwitchService? _killSwitchService;

    public HeldOutEvalStore(string root, KillSwitchService? killSwitchService = null)
    {
        _root = Path.GetFullPath(root);
        _killSwitchService = killSwitchService;
    }

    public IReadOnlyList<string> ListCaseIds()
    {
        if (_killSwitchService?.IsActive() == true || !Directory.Exists(_root))
        {
            return [];
        }

        return Directory.EnumerateFiles(_root, "*.json", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string? ReadCase(string caseId)
    {
        if (_killSwitchService?.IsActive() == true || string.IsNullOrWhiteSpace(caseId))
        {
            return null;
        }

        var path = Path.GetFullPath(Path.Combine(_root, caseId + ".json"));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return File.Exists(path) ? File.ReadAllText(path) : null;
    }
}
