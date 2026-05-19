using System.Text.Json;

namespace Wevito.VNext.Core.SelfImprovement.Eval;

public sealed class InDistributionEvalStore : IInDistributionEvalStore
{
    private readonly string _root;
    private readonly KillSwitchService? _killSwitchService;

    public InDistributionEvalStore(string root, KillSwitchService? killSwitchService = null)
    {
        _root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        _killSwitchService = killSwitchService;
    }

    public override IReadOnlyList<string> ListCaseIds()
    {
        if (_killSwitchService?.IsActive() == true || !Directory.Exists(_root))
        {
            return [];
        }

        return Directory.EnumerateFiles(_root, "*.json", SearchOption.TopDirectoryOnly)
            .Where(HasInDistributionDiscriminator)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public override string? ReadCase(string caseId)
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

        if (!File.Exists(path) || !HasInDistributionDiscriminator(path))
        {
            return null;
        }

        return File.ReadAllText(path);
    }

    private static bool HasInDistributionDiscriminator(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.TryGetProperty("case_kind", out var caseKind) &&
                   caseKind.ValueKind == JsonValueKind.String &&
                   string.Equals(caseKind.GetString(), InDistributionEvalCase.CaseKindDiscriminator, StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
