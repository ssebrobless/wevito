using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Eval;

namespace Wevito.VNext.Core.SelfImprovement.Judge;

public sealed class HeuristicJudgeService
{
    public const string EnabledSetting = "heuristic_judge_enabled";

    private static readonly HeuristicJudgeRule DidMutateFalseRule = new(
        "rule_did_mutate_false",
        "Proposal, dry-run, and eval artifacts must all report didMutate=false.");
    private static readonly HeuristicJudgeRule ApplyRunnerNotImplementedRule = new(
        "rule_apply_runner_not_implemented",
        "Awaiting-approval artifact must confirm the apply runner is not implemented in v0.");
    private static readonly HeuristicJudgeRule ScopeHashPresentFormatRule = new(
        "rule_scope_hash_present_format",
        "Scope hash must be a 64-character lowercase hexadecimal string.");
    private static readonly HeuristicJudgeRule ArtifactPathsUnderArtifactsRule = new(
        "rule_artifact_paths_under_artifacts",
        "Proposal, dry-run, and eval artifact paths must stay under vnext/artifacts.");
    private static readonly HeuristicJudgeRule ProposalSourceHashesMatchLiveRule = new(
        "rule_proposal_source_hashes_match_live",
        "Proposal source hashes should match the current live files when those files exist.");
    private static readonly HeuristicJudgeRule EvalListsEveryManifestGateRule = new(
        "rule_eval_lists_every_manifest_gate",
        "Eval artifact results must list every default eval-gate manifest entry.");

    private readonly string _databasePath;
    private readonly AuditLedgerService _ledger;
    private readonly KillSwitchService? _killSwitchService;
    private readonly Func<IReadOnlyDictionary<string, string>> _settingsProvider;
    private readonly Action<string>? _commandObserver;

    public HeuristicJudgeService(
        string databasePath,
        AuditLedgerService ledger,
        KillSwitchService? killSwitchService = null,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null,
        Action<string>? commandObserver = null)
    {
        _databasePath = Path.GetFullPath(databasePath);
        _ledger = ledger;
        _killSwitchService = killSwitchService;
        _settingsProvider = settingsProvider ?? (() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        _commandObserver = commandObserver;
    }

    public IReadOnlyList<HeuristicJudgeFinding> Critique(string operationId, DateTimeOffset nowUtc)
    {
        if (_killSwitchService?.IsActive() == true || !IsEnabled())
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(operationId) || !File.Exists(_databasePath))
        {
            return [];
        }

        var row = ReadLatestAwaitingApprovalRow(operationId);
        if (row is null)
        {
            return [];
        }

        var awaitingPath = Canonicalize(row.ArtifactPath);
        if (string.IsNullOrWhiteSpace(awaitingPath) || !IsUnderVNextArtifacts(awaitingPath) || !File.Exists(awaitingPath))
        {
            return [];
        }

        using var awaiting = JsonDocument.Parse(File.ReadAllText(awaitingPath));
        var proposalPath = GetString(awaiting.RootElement, "proposalPath", "proposal_path");
        var dryRunPath = GetString(awaiting.RootElement, "dryRunPath", "dry_run_path");
        var evalPath = GetString(awaiting.RootElement, "evalPath", "eval_path");
        var findings = new[]
        {
            CheckDidMutateFalse(proposalPath, dryRunPath, evalPath),
            CheckApplyRunnerNotImplemented(awaiting.RootElement),
            CheckScopeHashPresentFormat(awaiting.RootElement),
            CheckArtifactPathsUnderArtifacts(proposalPath, dryRunPath, evalPath),
            CheckProposalSourceHashesMatchLive(proposalPath, awaitingPath),
            CheckEvalListsEveryManifestGate(evalPath)
        };

        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            SelfImprovementPacketKinds.JudgeCritique,
            row.TaskCardId,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: awaitingPath,
            Summary: JsonSerializer.Serialize(new
            {
                rules_evaluated = findings.Length,
                rules_passed = findings.Count(finding => finding.Passed),
                source = "heuristic_judge",
                operation_id = operationId
            }),
            Status: "Completed"));

        return findings;
    }

    private bool IsEnabled()
    {
        var settings = _settingsProvider();
        return settings.TryGetValue(EnabledSetting, out var value) &&
               bool.TryParse(value, out var parsed) &&
               parsed;
    }

    private AwaitingApprovalRow? ReadLatestAwaitingApprovalRow(string operationId)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT task_card_id, artifact_path
            FROM audit_ledger
            WHERE packet_kind = $packet_kind
              AND summary LIKE $operation_id
            ORDER BY created_at_utc DESC, id DESC
            LIMIT 1;
            """;
        _commandObserver?.Invoke(command.CommandText);
        command.Parameters.AddWithValue("$packet_kind", SelfImprovementPacketKinds.ApplyAwaitingApproval);
        command.Parameters.AddWithValue("$operation_id", $"%{operationId}%");

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var taskCardText = Convert.ToString(reader["task_card_id"], CultureInfo.InvariantCulture) ?? "";
        return new AwaitingApprovalRow(
            Guid.TryParse(taskCardText, out var taskCardId) ? taskCardId : null,
            Convert.ToString(reader["artifact_path"], CultureInfo.InvariantCulture) ?? "");
    }

    private static HeuristicJudgeFinding CheckDidMutateFalse(string proposalPath, string dryRunPath, string evalPath)
    {
        var paths = new[] { proposalPath, dryRunPath, evalPath };
        var allFalse = paths.All(path => TryReadJson(path, out var document) && !GetBool(document.RootElement, "didMutate", "did_mutate"));
        return new HeuristicJudgeFinding(
            DidMutateFalseRule,
            allFalse,
            allFalse ? "proposal/dry-run/eval didMutate=false" : "one or more artifacts were missing or didMutate=true");
    }

    private static HeuristicJudgeFinding CheckApplyRunnerNotImplemented(JsonElement awaitingRoot)
    {
        var value = GetString(awaitingRoot, "applyRunner", "apply_runner");
        var passed = value.Equals("not_implemented_in_v0", StringComparison.Ordinal);
        return new HeuristicJudgeFinding(
            ApplyRunnerNotImplementedRule,
            passed,
            passed ? "applyRunner=not_implemented_in_v0" : $"applyRunner={value}");
    }

    private static HeuristicJudgeFinding CheckScopeHashPresentFormat(JsonElement awaitingRoot)
    {
        var value = GetString(awaitingRoot, "scopeHash", "scope_hash");
        var passed = value.Length == 64 && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
        return new HeuristicJudgeFinding(
            ScopeHashPresentFormatRule,
            passed,
            passed ? "scope hash is 64 lowercase hex characters" : "scope hash missing or invalid");
    }

    private static HeuristicJudgeFinding CheckArtifactPathsUnderArtifacts(string proposalPath, string dryRunPath, string evalPath)
    {
        var paths = new[] { proposalPath, dryRunPath, evalPath };
        var passed = paths.All(path => !string.IsNullOrWhiteSpace(path) && IsUnderVNextArtifacts(Canonicalize(path)));
        return new HeuristicJudgeFinding(
            ArtifactPathsUnderArtifactsRule,
            passed,
            passed ? "all input artifacts are under vnext/artifacts" : "one or more input artifacts are outside vnext/artifacts");
    }

    private static HeuristicJudgeFinding CheckProposalSourceHashesMatchLive(string proposalPath, string awaitingPath)
    {
        if (!TryReadJson(proposalPath, out var proposal))
        {
            return new HeuristicJudgeFinding(ProposalSourceHashesMatchLiveRule, false, "proposal JSON missing or invalid");
        }

        if (!proposal.RootElement.TryGetProperty("sourceHashes", out var hashes) ||
            hashes.ValueKind != JsonValueKind.Object)
        {
            return new HeuristicJudgeFinding(ProposalSourceHashesMatchLiveRule, false, "proposal sourceHashes object missing");
        }

        var repoRoot = ResolveRepositoryRootFromArtifact(awaitingPath);
        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            return new HeuristicJudgeFinding(ProposalSourceHashesMatchLiveRule, false, "repo root could not be resolved from artifact path");
        }

        foreach (var property in hashes.EnumerateObject())
        {
            var expected = property.Value.GetString() ?? "";
            if (string.IsNullOrWhiteSpace(expected))
            {
                continue;
            }

            var livePath = Path.GetFullPath(Path.Combine(repoRoot, property.Name.Replace('/', Path.DirectorySeparatorChar)));
            if (File.Exists(livePath) && !ComputeSha256(livePath).Equals(expected, StringComparison.OrdinalIgnoreCase))
            {
                return new HeuristicJudgeFinding(ProposalSourceHashesMatchLiveRule, false, $"source hash mismatch: {property.Name}");
            }
        }

        return new HeuristicJudgeFinding(ProposalSourceHashesMatchLiveRule, true, "source hashes match existing live files");
    }

    private static HeuristicJudgeFinding CheckEvalListsEveryManifestGate(string evalPath)
    {
        if (!TryReadJson(evalPath, out var eval) ||
            !eval.RootElement.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Object)
        {
            return new HeuristicJudgeFinding(EvalListsEveryManifestGateRule, false, "eval results object missing");
        }

        var actual = results.EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        var expected = EvalGateManifest.Default().Gates.ToHashSet(StringComparer.Ordinal);
        var passed = actual.SetEquals(expected);
        return new HeuristicJudgeFinding(
            EvalListsEveryManifestGateRule,
            passed,
            passed ? "eval lists every manifest gate" : "eval gate key set differs from manifest");
    }

    private static bool TryReadJson(string path, out JsonDocument document)
    {
        document = null!;
        var fullPath = Canonicalize(path);
        if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
        {
            return false;
        }

        try
        {
            document = JsonDocument.Parse(File.ReadAllText(fullPath));
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool GetBool(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value))
            {
                return value.ValueKind == JsonValueKind.True ||
                       (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out var parsed) && parsed);
            }
        }

        return false;
    }

    private static string GetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value))
            {
                return value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.ToString();
            }
        }

        return "";
    }

    private static string Canonicalize(string path)
    {
        return string.IsNullOrWhiteSpace(path) ? "" : Path.GetFullPath(path);
    }

    private static bool IsUnderVNextArtifacts(string fullPath)
    {
        var parts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i].Equals("vnext", StringComparison.OrdinalIgnoreCase) &&
                parts[i + 1].Equals("artifacts", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string ResolveRepositoryRootFromArtifact(string artifactPath)
    {
        var fullPath = Canonicalize(artifactPath);
        var parts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Equals("vnext", StringComparison.OrdinalIgnoreCase))
            {
                return string.Join(Path.DirectorySeparatorChar.ToString(), parts.Take(i));
            }
        }

        return "";
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private sealed record AwaitingApprovalRow(Guid? TaskCardId, string ArtifactPath);
}
