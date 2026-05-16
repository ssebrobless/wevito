using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record CursorReactivityRequest(
    string PetId,
    double PetX,
    double PetY,
    double CursorX,
    double CursorY,
    DateTimeOffset NowUtc);

public sealed record CursorReactivityDecision(
    bool Triggered,
    double Distance,
    string Reason);

public sealed class CursorReactivityService
{
    public const string EnabledSetting = "pet_cursor_reactivity_enabled";
    public const string CursorReactivityFiredPacketKind = "pet_cursor_reactivity_fired";
    public const double DefaultProximityPixels = 200;
    public static readonly TimeSpan DefaultCooldown = TimeSpan.FromSeconds(10);

    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly Dictionary<string, DateTimeOffset> _lastTriggeredByPet = new(StringComparer.OrdinalIgnoreCase);

    public CursorReactivityService(
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public CursorReactivityDecision Evaluate(
        CursorReactivityRequest request,
        IReadOnlyDictionary<string, string>? settings = null,
        double proximityPixels = DefaultProximityPixels,
        TimeSpan? cooldown = null)
    {
        var distance = Math.Sqrt(Math.Pow(request.PetX - request.CursorX, 2) + Math.Pow(request.PetY - request.CursorY, 2));
        if (!GetBool(settings, EnabledSetting, true))
        {
            return new CursorReactivityDecision(false, distance, "cursor_reactivity_disabled");
        }

        if (distance > proximityPixels)
        {
            return new CursorReactivityDecision(false, distance, "cursor_outside_proximity");
        }

        var activeCooldown = cooldown ?? DefaultCooldown;
        if (_lastTriggeredByPet.TryGetValue(request.PetId, out var lastTriggered) &&
            request.NowUtc - lastTriggered < activeCooldown)
        {
            return new CursorReactivityDecision(false, distance, "cursor_reactivity_rate_limited");
        }

        if (_killSwitchService?.IsActive() != true)
        {
            _lastTriggeredByPet[request.PetId] = request.NowUtc;
            Record(request.NowUtc, $"Pet {request.PetId} reacted to cursor proximity.");
        }

        return new CursorReactivityDecision(true, distance, "cursor_reactivity_triggered");
    }

    private void Record(DateTimeOffset timestamp, string summary)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            CursorReactivityFiredPacketKind,
            null,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: summary,
            Status: "Fired",
            Error: ""));
    }

    private static bool GetBool(IReadOnlyDictionary<string, string>? settings, string key, bool defaultValue)
    {
        return settings is not null &&
               settings.TryGetValue(key, out var raw) &&
               bool.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }
}
