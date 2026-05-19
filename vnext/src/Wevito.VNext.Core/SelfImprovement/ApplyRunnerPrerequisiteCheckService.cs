using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Eval;
using Wevito.VNext.Core.SelfImprovement.Experiments;
using Wevito.VNext.Core.SelfImprovement.Judge;
using Wevito.VNext.Core.SelfImprovement.Replay;

namespace Wevito.VNext.Core.SelfImprovement;

public sealed class ApplyRunnerPrerequisiteCheckService
{
    public const string EnabledSetting = "apply_runner_prerequisite_check_enabled";
    public const string SnapshotWindowDaysSetting = "apply_runner_prereq_snapshot_window_days";
    public const string ReplayWindowDaysSetting = "apply_runner_prereq_replay_window_days";

    private const int CheckCount = 10;
    private static readonly string[] LegitimatelyTrueEnabledFlags =
    [
        EnabledSetting,
        EvalGateRunner.EnabledSetting,
        HeuristicJudgeService.EnabledSetting
    ];

    private readonly string _artifactRoot;
    private readonly string _databasePath;
    private readonly AuditLedgerService _ledger;
    private readonly IHeldOutEvalStore _heldOut;
    private readonly IInDistributionEvalStore _inDistribution;
    private readonly KillSwitchService? _killSwitch;
    private readonly Func<IReadOnlyDictionary<string, string>> _settingsProvider;
    private readonly Action<string>? _commandObserver;

    public ApplyRunnerPrerequisiteCheckService(
        string artifactRoot,
        string databasePath,
        AuditLedgerService ledger,
        IHeldOutEvalStore heldOut,
        IInDistributionEvalStore inDist,
        KillSwitchService? killSwitch = null,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null,
        Action<string>? commandObserver = null)
    {
        _artifactRoot = Path.GetFullPath(artifactRoot);
        _databasePath = Path.GetFullPath(databasePath);
        _ledger = ledger;
        _heldOut = heldOut;
        _inDistribution = inDist;
        _killSwitch = killSwitch;
        _settingsProvider = settingsProvider ?? (() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        _commandObserver = commandObserver;
    }

    public ApplyRunnerPrerequisiteCheckResult Check(string operationId, DateTimeOffset nowUtc)
    {
        var normalizedOperationId = string.IsNullOrWhiteSpace(operationId) ? "" : operationId.Trim();
        if (_killSwitch?.IsActive() == true)
        {
            return new ApplyRunnerPrerequisiteCheckResult(normalizedOperationId, BuildKillSwitchEntries(), false, nowUtc);
        }

        var settings = _settingsProvider();
        if (!IsTrue(settings, EnabledSetting))
        {
            return new ApplyRunnerPrerequisiteCheckResult(
                normalizedOperationId,
                [new PrerequisiteEntry(EnabledSetting, false, "false")],
                false,
                nowUtc);
        }

        var entries = BuildEntries(normalizedOperationId, nowUtc, settings);
        var result = new ApplyRunnerPrerequisiteCheckResult(normalizedOperationId, entries, entries.All(entry => entry.Passed), nowUtc);
        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            SelfImprovementPacketKinds.ApplyPrerequisiteCheck,
            null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: JsonSerializer.Serialize(new
            {
                operation_id = normalizedOperationId,
                checks_total = CheckCount,
                checks_passed = entries.Count(entry => entry.Passed),
                all_passed = result.AllPassed
            }, JsonDefaults.Options),
            Status: result.AllPassed ? "Completed" : "Refused"));
        return result;
    }

    private IReadOnlyList<PrerequisiteEntry> BuildEntries(
        string operationId,
        DateTimeOffset nowUtc,
        IReadOnlyDictionary<string, string> settings)
    {
        var latestAwaiting = ReadLatestAwaitingApproval(operationId);
        return
        [
            CheckKillSwitchCallable(settings),
            CheckFlag(EvalGateRunner.EnabledSetting, settings, "EvalGateRunner v1 enabled"),
            CheckJudge(operationId),
            CheckSnapshot(operationId, nowUtc, settings),
            CheckHeldOutCount(),
            CheckInDistributionCount(),
            CheckScopeHash(latestAwaiting),
            CheckReplay(operationId, nowUtc, settings),
            CheckCapabilityDefaults(settings),
            CheckApplyRunnerNotImplemented()
        ];
    }

    private static PrerequisiteEntry CheckKillSwitchCallable(IReadOnlyDictionary<string, string> settings)
    {
        return new PrerequisiteEntry("KillSwitch armed", !KillSwitchService.IsActive(settings), $"kill_switch={KillSwitchService.IsActive(settings)}");
    }

    private static PrerequisiteEntry CheckFlag(string flag, IReadOnlyDictionary<string, string> settings, string name)
    {
        var passed = IsTrue(settings, flag);
        return new PrerequisiteEntry(name, passed, $"{flag}={passed.ToString().ToLowerInvariant()}");
    }

    private PrerequisiteEntry CheckJudge(string operationId)
    {
        var row = ReadLatestRow(SelfImprovementPacketKinds.JudgeCritique, operationId);
        if (row is null)
        {
            return new PrerequisiteEntry("Heuristic judge enabled", false, "no JudgeCritique packet for operation");
        }

        try
        {
            using var document = JsonDocument.Parse(row.Summary);
            var root = document.RootElement;
            var evaluated = root.TryGetProperty("rules_evaluated", out var evaluatedElement) ? evaluatedElement.GetInt32() : -1;
            var passed = root.TryGetProperty("rules_passed", out var passedElement) ? passedElement.GetInt32() : -2;
            var ok = evaluated > 0 && passed == evaluated;
            return new PrerequisiteEntry("Heuristic judge enabled", ok, $"rules_passed={passed}; rules_evaluated={evaluated}");
        }
        catch (JsonException)
        {
            return new PrerequisiteEntry("Heuristic judge enabled", false, "JudgeCritique summary JSON invalid");
        }
    }

    private PrerequisiteEntry CheckSnapshot(string operationId, DateTimeOffset nowUtc, IReadOnlyDictionary<string, string> settings)
    {
        var windowDays = ReadWindowDays(settings, SnapshotWindowDaysSetting);
        var snapshot = FindLatestJson(operationId, "snapshot*.json");
        if (snapshot is null)
        {
            return new PrerequisiteEntry("Snapshot signed and verified recently", false, "no snapshot JSON found");
        }

        var modified = File.GetLastWriteTimeUtc(snapshot);
        if (DateTimeOffset.FromFileTime(modified.ToFileTimeUtc()) < nowUtc.AddDays(-windowDays))
        {
            return new PrerequisiteEntry("Snapshot signed and verified recently", false, $"snapshot older than {windowDays} days");
        }

        return VerifySnapshot(snapshot)
            ? new PrerequisiteEntry("Snapshot signed and verified recently", true, "snapshot signature matches")
            : new PrerequisiteEntry("Snapshot signed and verified recently", false, "snapshot signature mismatch");
    }

    private PrerequisiteEntry CheckHeldOutCount()
    {
        var count = _heldOut.ListCaseIds().Count;
        return new PrerequisiteEntry("Held-out store contains >= 1 case", count >= 1, $"count={count}");
    }

    private PrerequisiteEntry CheckInDistributionCount()
    {
        var count = _inDistribution.ListCaseIds().Count;
        return new PrerequisiteEntry("In-distribution store contains >= 1 case", count >= 1, $"count={count}");
    }

    private PrerequisiteEntry CheckScopeHash(AwaitingApprovalRow? awaiting)
    {
        if (awaiting is null || string.IsNullOrWhiteSpace(awaiting.ArtifactPath) || !File.Exists(awaiting.ArtifactPath))
        {
            return new PrerequisiteEntry("Scope hash matches latest awaiting-approval artifact", false, "awaiting approval artifact missing");
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(awaiting.ArtifactPath));
            var root = document.RootElement;
            var operationId = GetString(root, "operationId", "operation_id");
            var scopeHash = GetString(root, "scopeHash", "scope_hash");
            var proposalPath = GetString(root, "proposalPath", "proposal_path");
            var dryRunPath = GetString(root, "dryRunPath", "dry_run_path");
            var evalPath = GetString(root, "evalPath", "eval_path");
            var recomputed = ComputeScopeHash(operationId, proposalPath, dryRunPath, evalPath);
            var passed = recomputed.Equals(scopeHash, StringComparison.Ordinal);
            return new PrerequisiteEntry("Scope hash matches latest awaiting-approval artifact", passed, passed ? "scope hash matches" : "scope hash mismatch");
        }
        catch (JsonException)
        {
            return new PrerequisiteEntry("Scope hash matches latest awaiting-approval artifact", false, "awaiting approval JSON invalid");
        }
    }

    private PrerequisiteEntry CheckReplay(string operationId, DateTimeOffset nowUtc, IReadOnlyDictionary<string, string> settings)
    {
        var replay = new ReplayResultStore(_artifactRoot, _killSwitch ?? new KillSwitchService(() => settings)).GetLatest(operationId);
        if (replay is null)
        {
            return new PrerequisiteEntry("Replay run within window", false, "no replay result");
        }

        var windowDays = ReadWindowDays(settings, ReplayWindowDaysSetting);
        var recent = replay.ReplayedAtUtc >= nowUtc.AddDays(-windowDays);
        var identical = replay.ResultKind.Equals("Identical", StringComparison.OrdinalIgnoreCase);
        return new PrerequisiteEntry("Replay run within window", recent && identical, $"result={replay.ResultKind}; replayed_at_utc={replay.ReplayedAtUtc:O}");
    }

    private static PrerequisiteEntry CheckCapabilityDefaults(IReadOnlyDictionary<string, string> settings)
    {
        var violations = CapabilityFlagInventory.Entries
            .Where(entry => entry.Name.EndsWith("_enabled", StringComparison.OrdinalIgnoreCase))
            .Where(entry => string.Equals(entry.DefaultValue, bool.FalseString, StringComparison.OrdinalIgnoreCase))
            .Where(entry => IsTrue(settings, entry.Name) && !LegitimatelyTrueEnabledFlags.Contains(entry.Name, StringComparer.OrdinalIgnoreCase))
            .Select(entry => entry.Name)
            .ToArray();
        return new PrerequisiteEntry(
            "Capability default-off audit",
            violations.Length == 0,
            violations.Length == 0 ? "no unexpected enabled flags" : $"unexpected enabled flags: {string.Join(", ", violations)}");
    }

    private static PrerequisiteEntry CheckApplyRunnerNotImplemented()
    {
        var reasonMatches = SupervisedImprovementLoop.ApplyRunnerNotImplementedReason.Equals("apply_runner_not_implemented_in_v0", StringComparison.Ordinal);
        var markerExists = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(SafeGetTypes)
            .Any(type => string.Equals(type.Name, "IApplyRunner", StringComparison.Ordinal));
        var passed = reasonMatches && !markerExists;
        return new PrerequisiteEntry(
            "Apply runner declared not implemented",
            passed,
            passed ? "apply runner not implemented in v0" : $"reason_matches={reasonMatches}; apply_runner_type_exists={markerExists}");
    }

    private AwaitingApprovalRow? ReadLatestAwaitingApproval(string operationId)
    {
        var row = ReadLatestRow(SelfImprovementPacketKinds.ApplyAwaitingApproval, operationId);
        return row is null ? null : new AwaitingApprovalRow(row.TaskCardId, row.ArtifactPath);
    }

    private AuditLedgerRow? ReadLatestRow(string packetKind, string operationId)
    {
        if (string.IsNullOrWhiteSpace(operationId) || !File.Exists(_databasePath))
        {
            return null;
        }

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, packet_id, packet_kind, task_card_id, created_at_utc,
                   did_use_network, did_use_hosted_ai, did_use_local_model,
                   did_mutate, artifact_path, summary, status, error
            FROM audit_ledger
            WHERE packet_kind = $packet_kind
              AND summary LIKE $operation_id
            ORDER BY created_at_utc DESC, id DESC
            LIMIT 1;
            """;
        _commandObserver?.Invoke(command.CommandText);
        command.Parameters.AddWithValue("$packet_kind", packetKind);
        command.Parameters.AddWithValue("$operation_id", $"%{operationId}%");
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var taskCard = Convert.ToString(reader["task_card_id"], CultureInfo.InvariantCulture);
        return new AuditLedgerRow(
            Convert.ToInt64(reader["id"], CultureInfo.InvariantCulture),
            Guid.Parse(Convert.ToString(reader["packet_id"], CultureInfo.InvariantCulture) ?? Guid.Empty.ToString()),
            Convert.ToString(reader["packet_kind"], CultureInfo.InvariantCulture) ?? "",
            Guid.TryParse(taskCard, out var taskCardId) ? taskCardId : null,
            DateTimeOffset.Parse(Convert.ToString(reader["created_at_utc"], CultureInfo.InvariantCulture) ?? DateTimeOffset.MinValue.ToString("O"), CultureInfo.InvariantCulture),
            Convert.ToInt32(reader["did_use_network"], CultureInfo.InvariantCulture) != 0,
            Convert.ToInt32(reader["did_use_hosted_ai"], CultureInfo.InvariantCulture) != 0,
            Convert.ToInt32(reader["did_use_local_model"], CultureInfo.InvariantCulture) != 0,
            Convert.ToInt32(reader["did_mutate"], CultureInfo.InvariantCulture) != 0,
            Convert.ToString(reader["artifact_path"], CultureInfo.InvariantCulture) ?? "",
            Convert.ToString(reader["summary"], CultureInfo.InvariantCulture) ?? "",
            Convert.ToString(reader["status"], CultureInfo.InvariantCulture) ?? "",
            Convert.ToString(reader["error"], CultureInfo.InvariantCulture) ?? "");
    }

    private string? FindLatestJson(string operationId, string pattern)
    {
        if (string.IsNullOrWhiteSpace(operationId) || !Directory.Exists(_artifactRoot))
        {
            return null;
        }

        return Directory.EnumerateFiles(_artifactRoot, pattern, SearchOption.AllDirectories)
            .Where(path => Path.GetFullPath(path).StartsWith(_artifactRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains(operationId, StringComparison.Ordinal))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static bool VerifySnapshot(string path)
    {
        try
        {
            var text = File.ReadAllText(path, Encoding.UTF8);
            using var document = JsonDocument.Parse(text);
            if (!document.RootElement.TryGetProperty("snapshot_sha256", out var signatureElement))
            {
                return false;
            }

            var signature = signatureElement.GetString() ?? "";
            if (signature.Length != 64)
            {
                return false;
            }

            var unsigned = text.Replace($"\"snapshot_sha256\": \"{signature}\"", "\"snapshot_sha256\": \"\"", StringComparison.Ordinal);
            return Sha256(Encoding.UTF8.GetBytes(unsigned)).Equals(signature, StringComparison.Ordinal);
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

    private static string ComputeScopeHash(string operationId, string proposalPath, string dryRunPath, string evalPath)
    {
        var descriptor = SpriteRepairBatchProposalDescriptor.Descriptor;
        var packetKindsTouched = new[]
        {
            SelfImprovementPacketKinds.ProposalDrafted,
            SelfImprovementPacketKinds.DryRunCompleted,
            SelfImprovementPacketKinds.EvalCompleted,
            SelfImprovementPacketKinds.ApplyAwaitingApproval
        };
        var manifestHash = ExperimentManifestHash.Compute(
            descriptor,
            SpriteRepairBatchProposalDescriptor.MutationPosture,
            packetKindsTouched);

        return ScopeHash.Compute(new ScopeHashInputs(
            AutonomousScopeService.SpriteRepairBatchProposalScopeId,
            operationId,
            FileSha256(proposalPath),
            FileSha256(dryRunPath),
            FileSha256(evalPath),
            descriptor.ManifestVersion,
            packetKindsTouched,
            manifestHash));
    }

    private static string FileSha256(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return "";
        }

        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static IReadOnlyList<PrerequisiteEntry> BuildKillSwitchEntries()
    {
        return Enumerable.Range(0, CheckCount)
            .Select(index => new PrerequisiteEntry($"kill_switch_blocked_{index + 1}", false, "kill_switch=true"))
            .ToArray();
    }

    private static int ReadWindowDays(IReadOnlyDictionary<string, string> settings, string key)
    {
        return settings.TryGetValue(key, out var value) &&
               int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) &&
               parsed > 0
            ? parsed
            : 7;
    }

    private static bool IsTrue(IReadOnlyDictionary<string, string> settings, string key)
    {
        return settings.TryGetValue(key, out var value) &&
               bool.TryParse(value, out var parsed) &&
               parsed;
    }

    private static string GetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String)
            {
                return property.GetString() ?? "";
            }

            foreach (var candidate in element.EnumerateObject())
            {
                if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase) &&
                    candidate.Value.ValueKind == JsonValueKind.String)
                {
                    return candidate.Value.GetString() ?? "";
                }
            }
        }

        return "";
    }

    private static string Sha256(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null).Cast<Type>();
        }
    }

    private sealed record AwaitingApprovalRow(Guid? TaskCardId, string ArtifactPath);
}
