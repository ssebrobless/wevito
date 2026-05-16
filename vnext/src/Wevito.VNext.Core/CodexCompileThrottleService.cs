using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record CodexLoopStatusSnapshot(
    string State,
    string CurrentPhaseId,
    DateTimeOffset? LastHeartbeatUtc);

public sealed record CodexCompileThrottleDecision(
    int ProcessorCount,
    bool IsThrottled,
    string Reason);

public sealed class CodexCompileThrottleService
{
    public const string ActiveProcessorCapSetting = "resource_codex_active_processor_cap";
    public const string CodexCompileThrottledPacketKind = "codex_compile_throttled";
    private static readonly TimeSpan ActiveUserWindow = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan IdleWindow = TimeSpan.FromMinutes(5);

    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<DateTimeOffset> _lastInputReader;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public CodexCompileThrottleService(
        Func<DateTimeOffset>? clock = null,
        Func<DateTimeOffset>? lastInputReader = null,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _lastInputReader = lastInputReader ?? (() => DateTimeOffset.UtcNow);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public CodexCompileThrottleDecision Decide(
        CodexLoopStatusSnapshot status,
        IReadOnlyDictionary<string, string>? settings = null,
        int logicalProcessorCount = 8)
    {
        if (!string.Equals(status.State, "running", StringComparison.OrdinalIgnoreCase))
        {
            return new CodexCompileThrottleDecision(Math.Max(1, logicalProcessorCount), false, "Codex phase loop is not running.");
        }

        var now = _clock();
        var lastInput = _lastInputReader();
        var activeCap = Clamp(ReadInt(settings, ActiveProcessorCapSetting, 2), 1, 4);
        var idleCap = Math.Max(activeCap, Math.Min(6, Math.Max(1, logicalProcessorCount - 2)));
        if (now - lastInput <= ActiveUserWindow)
        {
            var decision = new CodexCompileThrottleDecision(activeCap, true, "User active; Codex compile/test work is capped.");
            Record(status, decision);
            return decision;
        }

        if (now - lastInput >= IdleWindow)
        {
            var decision = new CodexCompileThrottleDecision(idleCap, false, "User idle; Codex compile/test work can use idle cap.");
            Record(status, decision);
            return decision;
        }

        var middle = new CodexCompileThrottleDecision(activeCap, true, "User recently active; keeping conservative cap.");
        Record(status, middle);
        return middle;
    }

    public IReadOnlyDictionary<string, string> BuildEnvironment(
        CodexLoopStatusSnapshot status,
        IReadOnlyDictionary<string, string>? settings = null,
        int logicalProcessorCount = 8)
    {
        var decision = Decide(status, settings, logicalProcessorCount);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["MSBUILDPROCESSORCOUNT"] = decision.ProcessorCount.ToString()
        };
    }

    private void Record(CodexLoopStatusSnapshot status, CodexCompileThrottleDecision decision)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return;
        }

        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            CodexCompileThrottledPacketKind,
            null,
            _clock(),
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: $"phase={status.CurrentPhaseId} msbuild_processor_count={decision.ProcessorCount} reason={decision.Reason}",
            Status: "Completed"));
    }

    private static int ReadInt(IReadOnlyDictionary<string, string>? settings, string key, int defaultValue)
    {
        return settings is not null && settings.TryGetValue(key, out var raw) && int.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Min(max, Math.Max(min, value));
    }
}
