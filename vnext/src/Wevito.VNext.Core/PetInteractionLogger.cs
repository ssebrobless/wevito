using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record PetInteractionLogEntry(
    Guid PetId,
    string PetNameSnapshot,
    string ActionId,
    string Source,
    DateTimeOffset OccurredAtUtc);

public sealed class PetInteractionLogger
{
    public const string PacketKind = "pet_interaction_logged";
    public static readonly string DefaultLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Wevito",
        "audit",
        "pet-interactions.jsonl");

    private readonly string _logPath;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public PetInteractionLogger(
        string? logPath = null,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _logPath = Path.GetFullPath(logPath ?? DefaultLogPath);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public string LogPath => _logPath;

    public bool Append(PetInteractionLogEntry entry, Guid? taskCardId = null)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            _killSwitchService.Record(PacketKind, taskCardId, entry.OccurredAtUtc, "Blocked pet interaction logging because kill_switch=true.", "Blocked");
            return false;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_logPath) ?? ".");
        File.AppendAllText(_logPath, JsonSerializer.Serialize(entry, JsonDefaults.Options) + Environment.NewLine);
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            PacketKind,
            taskCardId,
            entry.OccurredAtUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: _logPath,
            Summary: $"{entry.PetNameSnapshot} interaction logged: {entry.ActionId}.",
            Status: "Completed"));
        return true;
    }

    public IReadOnlyList<PetInteractionLogEntry> RecentInteractions(Guid petId, DateTimeOffset sinceUtc, DateTimeOffset untilUtc)
    {
        if (!File.Exists(_logPath))
        {
            return [];
        }

        var entries = new List<PetInteractionLogEntry>();
        foreach (var line in File.ReadLines(_logPath))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var entry = JsonSerializer.Deserialize<PetInteractionLogEntry>(line, JsonDefaults.Options);
                if (entry is not null &&
                    entry.PetId == petId &&
                    entry.OccurredAtUtc >= sinceUtc &&
                    entry.OccurredAtUtc <= untilUtc)
                {
                    entries.Add(entry);
                }
            }
            catch (JsonException)
            {
                // Ignore corrupt historical rows rather than blocking read-only pet state.
            }
        }

        return entries
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .ToList();
    }
}
