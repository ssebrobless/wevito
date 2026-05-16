using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class AiIdentityService
{
    public const string AiIdentityNameSetting = "ai_identity_name";
    public const string DefaultAiName = "Wevito";

    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public AiIdentityService(AuditLedgerService? auditLedgerService = null, KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public string GetAiName(IReadOnlyDictionary<string, string>? settings = null)
    {
        return settings is not null &&
            settings.TryGetValue(AiIdentityNameSetting, out var value) &&
            !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : DefaultAiName;
    }

    public IReadOnlyDictionary<string, string> SetAiName(
        IReadOnlyDictionary<string, string>? settings,
        string? name,
        DateTimeOffset? nowUtc = null)
    {
        if (_killSwitchService?.IsActive() == true || KillSwitchService.IsActive(settings))
        {
            return Clone(settings);
        }

        var normalized = NormalizeName(name);
        var next = Clone(settings);
        next[AiIdentityNameSetting] = normalized;
        Record("ai_identity_set", $"AI identity name set to {normalized}.", nowUtc ?? DateTimeOffset.UtcNow);
        return next;
    }

    private static string NormalizeName(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? DefaultAiName : trimmed;
    }

    private static Dictionary<string, string> Clone(IReadOnlyDictionary<string, string>? settings)
    {
        return settings is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(settings, StringComparer.OrdinalIgnoreCase);
    }

    private void Record(string kind, string summary, DateTimeOffset timestamp)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            kind,
            null,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: summary,
            Status: "Completed"));
    }
}
