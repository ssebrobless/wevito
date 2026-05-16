using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class CodexPhaseQueueService
{
    public const string PhaseStartedPacketKind = "codex_phase_started";
    public const string PhaseRetriedPacketKind = "codex_phase_retried";
    public const string PhaseBlockedPacketKind = "codex_phase_blocked";
    public const string PhaseCompletedPacketKind = "codex_phase_completed";
    public const string LoopPausedPacketKind = "codex_loop_paused";
    public const string LoopResumedPacketKind = "codex_loop_resumed";

    private readonly string _queuePath;
    private readonly string _statusPath;
    private readonly string _historyPath;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public CodexPhaseQueueService(
        string docsRoot,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        var root = Path.GetFullPath(docsRoot);
        _queuePath = Path.Combine(root, "codex-phase-queue.json");
        _statusPath = Path.Combine(root, "codex-loop-status.json");
        _historyPath = Path.Combine(root, "codex-phase-history.jsonl");
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public string QueuePath => _queuePath;

    public string StatusPath => _statusPath;

    public string HistoryPath => _historyPath;

    public void EnsureInitialized()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_queuePath) ?? ".");
        if (!File.Exists(_queuePath))
        {
            WriteQueue([]);
        }

        if (!File.Exists(_statusPath))
        {
            WriteStatus(CodexLoopStatus.Idle);
        }

        if (!File.Exists(_historyPath))
        {
            File.WriteAllText(_historyPath, "");
        }
    }

    public IReadOnlyList<CodexPhaseQueueEntry> ReadQueue()
    {
        EnsureInitialized();
        return JsonSerializer.Deserialize<IReadOnlyList<CodexPhaseQueueEntry>>(File.ReadAllText(_queuePath), JsonDefaults.Options) ?? [];
    }

    public CodexLoopStatus ReadStatus()
    {
        EnsureInitialized();
        return JsonSerializer.Deserialize<CodexLoopStatus>(File.ReadAllText(_statusPath), JsonDefaults.Options) ?? CodexLoopStatus.Idle;
    }

    public IReadOnlyList<CodexPhaseHistoryRow> ReadHistory()
    {
        EnsureInitialized();
        return File.ReadLines(_historyPath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<CodexPhaseHistoryRow>(line, JsonDefaults.Options))
            .Where(row => row is not null)
            .Cast<CodexPhaseHistoryRow>()
            .ToList();
    }

    public void Enqueue(CodexPhaseQueueEntry entry)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        EnsureNotCompleted(entry.PhaseId);
        var queue = ReadQueue().Where(item => !item.PhaseId.Equals(entry.PhaseId, StringComparison.OrdinalIgnoreCase)).ToList();
        queue.Add(entry);
        WriteQueue(queue);
    }

    public void InjectFront(CodexPhaseQueueEntry entry)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        EnsureNotCompleted(entry.PhaseId);
        var queue = ReadQueue().Where(item => !item.PhaseId.Equals(entry.PhaseId, StringComparison.OrdinalIgnoreCase)).ToList();
        queue.Insert(0, entry);
        WriteQueue(queue);
    }

    public CodexPhaseQueueEntry? DequeueNext(DateTimeOffset nowUtc)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        var queue = ReadQueue().ToList();
        if (queue.Count == 0)
        {
            return null;
        }

        var entry = queue[0] with { Status = "running" };
        queue.RemoveAt(0);
        WriteQueue(queue);
        WriteStatus(new CodexLoopStatus("running", entry.PhaseId, nowUtc, nowUtc, "phase_started", entry.AttemptCount));
        AppendHistory(new CodexPhaseHistoryRow(entry.PhaseId, PhaseStartedPacketKind, nowUtc, "phase started", "Running"));
        return entry;
    }

    public void CompletePhase(string phaseId, DateTimeOffset nowUtc, string summary)
    {
        AppendHistory(new CodexPhaseHistoryRow(phaseId, PhaseCompletedPacketKind, nowUtc, summary, "Completed"));
        WriteStatus(CodexLoopStatus.Idle with { LastReason = "phase_completed", LastHeartbeatUtc = nowUtc });
    }

    public void BlockPhase(string phaseId, DateTimeOffset nowUtc, string reason)
    {
        AppendHistory(new CodexPhaseHistoryRow(phaseId, PhaseBlockedPacketKind, nowUtc, reason, "Blocked"));
        WriteStatus(new CodexLoopStatus("blocked", phaseId, nowUtc, nowUtc, reason));
    }

    public void RetryPhase(CodexPhaseQueueEntry entry, DateTimeOffset nowUtc, string reason)
    {
        AppendHistory(new CodexPhaseHistoryRow(entry.PhaseId, PhaseRetriedPacketKind, nowUtc, reason, "Retry"));
        InjectFront(entry with { AttemptCount = entry.AttemptCount + 1, Status = "pending" });
    }

    public void Pause(DateTimeOffset nowUtc, string reason)
    {
        AppendHistory(new CodexPhaseHistoryRow("", LoopPausedPacketKind, nowUtc, reason, "Paused"));
        WriteStatus(new CodexLoopStatus("paused", "", null, nowUtc, reason));
    }

    public void Resume(DateTimeOffset nowUtc)
    {
        AppendHistory(new CodexPhaseHistoryRow("", LoopResumedPacketKind, nowUtc, "loop resumed", "Idle"));
        WriteStatus(CodexLoopStatus.Idle with { LastReason = "loop_resumed", LastHeartbeatUtc = nowUtc });
    }

    private void EnsureNotCompleted(string phaseId)
    {
        if (ReadHistory().Any(row =>
                row.PhaseId.Equals(phaseId, StringComparison.OrdinalIgnoreCase) &&
                row.EventKind.Equals(PhaseCompletedPacketKind, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Completed phase '{phaseId}' is immutable and cannot be re-queued.");
        }
    }

    private void WriteQueue(IReadOnlyList<CodexPhaseQueueEntry> queue)
    {
        File.WriteAllText(_queuePath, JsonSerializer.Serialize(queue, JsonDefaults.Options));
    }

    private void WriteStatus(CodexLoopStatus status)
    {
        File.WriteAllText(_statusPath, JsonSerializer.Serialize(status, JsonDefaults.Options));
    }

    private void AppendHistory(CodexPhaseHistoryRow row)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_historyPath) ?? ".");
        File.AppendAllText(_historyPath, JsonSerializer.Serialize(row, JsonDefaults.Options) + Environment.NewLine);
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            row.EventKind,
            null,
            row.CreatedAtUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: true,
            _historyPath,
            row.Summary,
            row.Status));
    }
}
