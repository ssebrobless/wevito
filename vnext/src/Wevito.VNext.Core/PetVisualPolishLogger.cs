using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class PetVisualPolishLogger
{
    public const string PetMicroBehaviorPacketKind = "pet_micro_behavior";
    public const string PetParticleEffectPacketKind = "pet_particle_effect_played";
    public const string AnimationBlendingSetting = "pet_visual_animation_blending";
    public const string PositionInterpolationSetting = "pet_visual_position_interpolation";
    public const string IdleMicroBehaviorsSetting = "pet_visual_idle_micro_behaviors";
    public const string ParticleEffectsSetting = "pet_visual_particle_effects";
    public const string WindowShakeReactionSetting = "pet_visual_window_shake_reaction";
    public static readonly TimeSpan MinimumFlushInterval = TimeSpan.FromMinutes(1);

    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;
    private readonly Dictionary<string, int> _pendingMicroBehaviors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _pendingParticleEffects = new(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset? _lastFlushAtUtc;

    public PetVisualPolishLogger(
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public int PendingEventCount => _pendingMicroBehaviors.Values.Sum() + _pendingParticleEffects.Values.Sum();

    public bool RecordMicroBehavior(PetMicroBehavior behavior, DateTimeOffset nowUtc)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return false;
        }

        Increment(_pendingMicroBehaviors, behavior.ToString());
        return TryFlush(nowUtc);
    }

    public bool RecordParticleEffect(string effectName, DateTimeOffset nowUtc)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return false;
        }

        Increment(_pendingParticleEffects, string.IsNullOrWhiteSpace(effectName) ? "unknown" : effectName.Trim());
        return TryFlush(nowUtc);
    }

    public bool TryFlush(DateTimeOffset nowUtc, bool force = false)
    {
        if (_killSwitchService?.IsActive() == true || PendingEventCount == 0)
        {
            return false;
        }

        if (!force && _lastFlushAtUtc is not null && nowUtc - _lastFlushAtUtc < MinimumFlushInterval)
        {
            return false;
        }

        var microSummary = FormatCounts(_pendingMicroBehaviors);
        var particleSummary = FormatCounts(_pendingParticleEffects);
        var packetKind = _pendingParticleEffects.Count > 0 && _pendingMicroBehaviors.Count == 0
            ? PetParticleEffectPacketKind
            : PetMicroBehaviorPacketKind;
        var summary = $"micro={microSummary}; particles={particleSummary}; total={PendingEventCount}";
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: summary,
            Status: "Completed",
            Error: ""));

        _pendingMicroBehaviors.Clear();
        _pendingParticleEffects.Clear();
        _lastFlushAtUtc = nowUtc;
        return true;
    }

    private static void Increment(Dictionary<string, int> counts, string key)
    {
        counts[key] = counts.TryGetValue(key, out var current) ? current + 1 : 1;
    }

    private static string FormatCounts(Dictionary<string, int> counts)
    {
        return counts.Count == 0
            ? "none"
            : string.Join(",", counts.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase).Select(entry => $"{entry.Key}:{entry.Value}"));
    }
}
