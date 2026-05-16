using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record AudioOutputRequest(
    string SoundId,
    bool UserTriggered,
    bool IsTextToSpeech);

public sealed record AudioOutputDecision(
    bool CanPlay,
    bool IsTtsBanned,
    string Reason);

public sealed class AudioOutputPolicyService
{
    public const string PetSoundEffectsEnabledSetting = "pet_sound_effects_enabled";
    public const string AudioPlayedPacketKind = "audio_played";

    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public AudioOutputPolicyService(
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public AudioOutputDecision Evaluate(
        AudioOutputRequest request,
        IReadOnlyDictionary<string, string>? settings,
        DateTimeOffset nowUtc)
    {
        if (request.IsTextToSpeech)
        {
            return new AudioOutputDecision(false, true, "tts_banned_in_v1");
        }

        if (!GetBool(settings, PetSoundEffectsEnabledSetting, false))
        {
            return new AudioOutputDecision(false, false, "pet_sounds_disabled_by_default");
        }

        if (!request.UserTriggered)
        {
            return new AudioOutputDecision(false, false, "audio_requires_user_trigger");
        }

        Record(nowUtc, $"Played user-triggered pet sound {request.SoundId}.");
        return new AudioOutputDecision(true, false, "user_triggered_sound_allowed");
    }

    public static bool CanUseTextToSpeech() => false;

    private void Record(DateTimeOffset timestamp, string summary)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return;
        }

        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            AudioPlayedPacketKind,
            null,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: summary,
            Status: "Played",
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
