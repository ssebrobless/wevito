using System.Text.Json;
using Blake3;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record SpriteRepairBatchRequest(
    string RepoRoot,
    SpriteRepairQueueRow Row,
    SpriteRepairQueueIssue Issue,
    string ArtifactRoot,
    DateTimeOffset RequestedAtUtc,
    Guid? TaskCardId = null,
    string? BatchId = null,
    string? CandidateFolderOverride = null);

public sealed record SpriteRepairBatchResult(
    bool Succeeded,
    bool RolledBack,
    string Status,
    string Message,
    string ArtifactPath,
    string CandidateFolder,
    string BackupFolder,
    IReadOnlyDictionary<string, string> PreHashes,
    IReadOnlyDictionary<string, string> PostHashes);

public sealed record SpriteRepairBatchPlan(
    string RowId,
    string SpeciesId,
    string LifeStage,
    string Gender,
    string ColorVariant,
    string AnimationFamily,
    string RuntimeTargetDirectory,
    string CandidateOutputDirectory,
    string RepairToolPath,
    bool RepairToolExists,
    string CommandLine,
    IReadOnlyList<string> FlaggedFrameRelativePaths,
    IReadOnlyList<string> WouldWriteRelativePaths,
    string Summary);

public sealed class SpriteRepairBatchRunner
{
    public const string BatchPacketKind = "sprite_repair_batch";
    public const string RolledBackPacketKind = "sprite_repair_batch_rolled_back";

    private readonly ICommandRunner _commandRunner;
    private readonly SpriteWorkflowDryRunApplyService _dryRunApplyService;
    private readonly SpriteWorkflowApplyService _applyService;
    private readonly SpriteWorkflowPostApplyProof _postApplyProof;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public SpriteRepairBatchRunner(
        ICommandRunner? commandRunner = null,
        SpriteWorkflowDryRunApplyService? dryRunApplyService = null,
        SpriteWorkflowApplyService? applyService = null,
        SpriteWorkflowPostApplyProof? postApplyProof = null,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _commandRunner = commandRunner ?? new ProcessCommandRunner();
        _dryRunApplyService = dryRunApplyService ?? new SpriteWorkflowDryRunApplyService();
        _applyService = applyService ?? new SpriteWorkflowApplyService();
        _postApplyProof = postApplyProof ?? new SpriteWorkflowPostApplyProof();
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public async Task<SpriteRepairBatchResult> RunAsync(SpriteRepairBatchRequest request, CancellationToken cancellationToken = default)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return Fail(request, "Blocked", "kill_switch=true", rolledBack: false);
        }

        var repoRoot = Path.GetFullPath(request.RepoRoot);
        var artifactRoot = Path.GetFullPath(request.ArtifactRoot);
        Directory.CreateDirectory(artifactRoot);

        var target = BuildTarget(request.Row, request.Issue);
        var runtimeRowFolder = SpriteWorkflowDryRunApplyService.ResolveRuntimeRowFolder(repoRoot, target);
        var runtimeRoot = Path.Combine(repoRoot, "sprites_runtime");
        if (!IsPathUnderRoot(runtimeRowFolder, runtimeRoot))
        {
            return Fail(request, "Failed", "Runtime write target is outside sprites_runtime.", rolledBack: false);
        }

        var batchId = BuildBatchId(request);
        var candidateFolder = ResolveCandidateFolder(repoRoot, batchId, request.CandidateFolderOverride);
        var authoredRoot = Path.Combine(repoRoot, "sprites_authored");
        if (!IsPathUnderRoot(candidateFolder, authoredRoot) ||
            !candidateFolder.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => string.Equals(part, ".candidates", StringComparison.OrdinalIgnoreCase)))
        {
            return Fail(request, "Failed", "Candidate folder must stay under sprites_authored/.candidates.", rolledBack: false);
        }

        var repairToolPath = ResolveToolPath(repoRoot, request.Issue.RepairTool);
        if (!File.Exists(repairToolPath))
        {
            return Fail(request, "Failed", $"Repair tool does not exist: {repairToolPath}", rolledBack: false);
        }

        Directory.CreateDirectory(candidateFolder);
        var preHashes = HashRuntimeFamily(runtimeRowFolder, request.Issue.AnimationFamily);
        var command = BuildRepairCommand(repoRoot, repairToolPath, request.Row, request.Issue, candidateFolder);
        var commandResult = await _commandRunner.RunAsync(new ProofExecutionRequest(
            request.TaskCardId ?? Guid.NewGuid(),
            command.CommandId,
            command,
            Path.Combine(artifactRoot, "repair-command"),
            new Dictionary<string, string>(),
            request.RequestedAtUtc), cancellationToken).ConfigureAwait(false);

        if (commandResult.Status is not (ProofExecutionResultStatus.Succeeded or ProofExecutionResultStatus.MutationDetected))
        {
            return RecordAndFail(request, artifactRoot, candidateFolder, preHashes, "Failed", $"Repair command failed: {commandResult.Status}", rolledBack: false, didMutate: false);
        }

        var dryRun = _dryRunApplyService.Plan(new SpriteWorkflowDryRunApplyRequest(
            repoRoot,
            target,
            candidateFolder,
            Path.Combine(artifactRoot, "dry-run"),
            request.RequestedAtUtc));
        if (!dryRun.Succeeded || dryRun.Manifest is null)
        {
            return RecordAndFail(request, artifactRoot, candidateFolder, preHashes, "Failed", dryRun.Message, rolledBack: false, didMutate: false);
        }

        var apply = _applyService.Apply(new SpriteWorkflowApplyRequest(dryRun.Manifest, request.RequestedAtUtc));
        if (!apply.Succeeded || apply.Manifest is null)
        {
            return RecordAndFail(request, artifactRoot, candidateFolder, preHashes, "Failed", apply.Message, rolledBack: false, didMutate: false);
        }

        var proof = _postApplyProof.VerifyOrRollback(apply.Manifest, request.RequestedAtUtc);
        var postHashes = HashChanges(apply.Manifest.Changes);
        var success = proof.Succeeded && !proof.RolledBack;
        var packetKind = proof.RolledBack ? RolledBackPacketKind : BatchPacketKind;
        var status = success ? "Completed" : "Failed";
        var message = proof.Message;
        var artifactPath = WriteBatchManifest(artifactRoot, request, target, candidateFolder, apply.Manifest.BackupFolder, preHashes, postHashes, success, proof.RolledBack, message);
        RecordPacket(request, packetKind, artifactPath, status, message, didMutate: true);

        return new SpriteRepairBatchResult(
            success,
            proof.RolledBack,
            status,
            message,
            artifactPath,
            candidateFolder,
            apply.Manifest.BackupFolder,
            preHashes,
            postHashes);
    }

    public SpriteRepairBatchPlan BuildPlanForReview(SpriteRepairBatchRequest request)
    {
        var repoRoot = Path.GetFullPath(request.RepoRoot);
        var target = BuildTarget(request.Row, request.Issue);
        var runtimeRowFolder = SpriteWorkflowDryRunApplyService.ResolveRuntimeRowFolder(repoRoot, target);
        var batchId = BuildBatchId(request);
        var candidateFolder = ResolveCandidateFolder(repoRoot, batchId, request.CandidateFolderOverride);
        var repairToolPath = ResolveToolPath(repoRoot, request.Issue.RepairTool);
        var command = BuildRepairCommand(repoRoot, repairToolPath, request.Row, request.Issue, candidateFolder);
        var flaggedFrames = ResolveFlaggedFrameRelativePaths(repoRoot, runtimeRowFolder, request.Issue);
        var wouldWrite = ResolveWouldWriteRelativePaths(repoRoot, runtimeRowFolder, request.Issue, flaggedFrames);
        var summary = $"Would run {request.Issue.RepairTool} for {request.Row.SpeciesId}/{request.Row.LifeStage}/{request.Row.Gender}/{request.Issue.ColorVariant}/{request.Issue.AnimationFamily}; would write {wouldWrite.Count} runtime frame path(s) after candidate review.";

        return new SpriteRepairBatchPlan(
            request.Row.RowId,
            request.Row.SpeciesId,
            request.Row.LifeStage,
            request.Row.Gender,
            request.Issue.ColorVariant,
            request.Issue.AnimationFamily,
            runtimeRowFolder,
            candidateFolder,
            repairToolPath,
            File.Exists(repairToolPath),
            FormatCommandLine(command),
            flaggedFrames,
            wouldWrite,
            summary);
    }

    private static SpriteRowKey BuildTarget(SpriteRepairQueueRow row, SpriteRepairQueueIssue issue)
    {
        if (!Enum.TryParse<PetAgeStage>(row.LifeStage, ignoreCase: true, out var ageStage) ||
            ageStage == PetAgeStage.Senior)
        {
            throw new InvalidOperationException($"Unsupported repair age stage: {row.LifeStage}");
        }

        if (!Enum.TryParse<PetGender>(row.Gender, ignoreCase: true, out var gender))
        {
            throw new InvalidOperationException($"Unsupported repair gender: {row.Gender}");
        }

        return new SpriteRowKey(row.SpeciesId, ageStage, gender, issue.ColorVariant, issue.AnimationFamily);
    }

    private static ProofExecutionCommand BuildRepairCommand(
        string repoRoot,
        string repairToolPath,
        SpriteRepairQueueRow row,
        SpriteRepairQueueIssue issue,
        string candidateFolder)
    {
        List<string> arguments =
        [
            repairToolPath,
            "--repo-root",
            repoRoot,
            "--row-id",
            row.RowId,
            "--species",
            row.SpeciesId,
            "--age",
            row.LifeStage,
            "--gender",
            row.Gender,
            "--color",
            issue.ColorVariant,
            "--animation",
            issue.AnimationFamily,
            "--out-dir",
            candidateFolder
        ];

        if (!string.IsNullOrWhiteSpace(issue.SourcePath))
        {
            arguments.Add("--source-path");
            arguments.Add(issue.SourcePath);
        }

        return new ProofExecutionCommand(
            "sprite-repair-batch",
            "python",
            arguments,
            repoRoot,
            TimeSpan.FromMinutes(5),
            MustSkipAssetPrep: true);
    }

    private static string ResolveToolPath(string repoRoot, string repairTool)
    {
        return Path.GetFullPath(Path.Combine(repoRoot, repairTool.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static IReadOnlyList<string> ResolveFlaggedFrameRelativePaths(string repoRoot, string runtimeRowFolder, SpriteRepairQueueIssue issue)
    {
        var explicitPaths = new[] { issue.SourcePath, issue.CapturePath }
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => ToRepoRelativePath(repoRoot, ResolvePath(repoRoot, path!)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (explicitPaths.Count > 0)
        {
            return explicitPaths;
        }

        if (!Directory.Exists(runtimeRowFolder))
        {
            return [];
        }

        return Directory.EnumerateFiles(runtimeRowFolder, $"{issue.AnimationFamily}_*.png", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => ToRepoRelativePath(repoRoot, path))
            .ToArray();
    }

    private static IReadOnlyList<string> ResolveWouldWriteRelativePaths(
        string repoRoot,
        string runtimeRowFolder,
        SpriteRepairQueueIssue issue,
        IReadOnlyList<string> flaggedFrames)
    {
        if (flaggedFrames.Count > 0)
        {
            return flaggedFrames
                .Select(path => path.Replace('\\', '/'))
                .ToArray();
        }

        return [ToRepoRelativePath(repoRoot, Path.Combine(runtimeRowFolder, $"{issue.AnimationFamily}_00.png"))];
    }

    private static string FormatCommandLine(ProofExecutionCommand command)
    {
        return string.Join(" ", new[] { command.Executable }.Concat(command.Arguments).Select(QuoteCommandPart));
    }

    private static string QuoteCommandPart(string part)
    {
        return part.Any(char.IsWhiteSpace) ? $"\"{part.Replace("\"", "\\\"")}\"" : part;
    }

    private static string ToRepoRelativePath(string repoRoot, string path)
    {
        return Path.GetRelativePath(repoRoot, Path.GetFullPath(path)).Replace('\\', '/');
    }

    private static string ResolvePath(string repoRoot, string path)
    {
        return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(repoRoot, path));
    }

    private static string ResolveCandidateFolder(string repoRoot, string batchId, string? overridePath)
    {
        return Path.GetFullPath(overridePath ?? Path.Combine(repoRoot, "sprites_authored", ".candidates", batchId));
    }

    private static string BuildBatchId(SpriteRepairBatchRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.BatchId))
        {
            return request.BatchId;
        }

        return $"{request.Row.RowId}-{request.Issue.ColorVariant}-{request.Issue.AnimationFamily}-{request.RequestedAtUtc:yyyyMMdd-HHmmss}";
    }

    private static IReadOnlyDictionary<string, string> HashRuntimeFamily(string runtimeRowFolder, string animationFamily)
    {
        if (!Directory.Exists(runtimeRowFolder))
        {
            return new Dictionary<string, string>();
        }

        return Directory.EnumerateFiles(runtimeRowFolder, $"{animationFamily}_*.png", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(path => path, path => ComputeBlake3(path), StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, string> HashChanges(IReadOnlyList<SpriteWorkflowDryRunChange> changes)
    {
        return changes
            .Where(change => File.Exists(change.RuntimePath))
            .ToDictionary(change => change.RuntimePath, change => ComputeBlake3(change.RuntimePath), StringComparer.OrdinalIgnoreCase);
    }

    private static string ComputeBlake3(string absolutePath)
    {
        var hash = Hasher.Hash(File.ReadAllBytes(absolutePath));
        return Convert.ToHexString(hash.AsSpan()).ToLowerInvariant();
    }

    private static bool IsPathUnderRoot(string path, string root)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private SpriteRepairBatchResult RecordAndFail(
        SpriteRepairBatchRequest request,
        string artifactRoot,
        string candidateFolder,
        IReadOnlyDictionary<string, string> preHashes,
        string status,
        string message,
        bool rolledBack,
        bool didMutate)
    {
        var artifactPath = WriteBatchManifest(artifactRoot, request, null, candidateFolder, "", preHashes, new Dictionary<string, string>(), success: false, rolledBack, message);
        RecordPacket(request, rolledBack ? RolledBackPacketKind : BatchPacketKind, artifactPath, status, message, didMutate);
        return new SpriteRepairBatchResult(false, rolledBack, status, message, artifactPath, candidateFolder, "", preHashes, new Dictionary<string, string>());
    }

    private SpriteRepairBatchResult Fail(SpriteRepairBatchRequest request, string status, string message, bool rolledBack)
    {
        return new SpriteRepairBatchResult(false, rolledBack, status, message, "", "", "", new Dictionary<string, string>(), new Dictionary<string, string>());
    }

    private static string WriteBatchManifest(
        string artifactRoot,
        SpriteRepairBatchRequest request,
        SpriteRowKey? target,
        string candidateFolder,
        string backupFolder,
        IReadOnlyDictionary<string, string> preHashes,
        IReadOnlyDictionary<string, string> postHashes,
        bool success,
        bool rolledBack,
        string message)
    {
        Directory.CreateDirectory(artifactRoot);
        var path = Path.Combine(artifactRoot, "sprite-repair-batch.json");
        var payload = new
        {
            schemaVersion = "1",
            generatedAtUtc = request.RequestedAtUtc,
            rowId = request.Row.RowId,
            issue = request.Issue,
            target,
            candidateFolder,
            backupFolder,
            success,
            rolledBack,
            message,
            preHashes,
            postHashes
        };
        File.WriteAllText(path, JsonSerializer.Serialize(payload, JsonDefaults.Options));
        return path;
    }

    private void RecordPacket(SpriteRepairBatchRequest request, string packetKind, string artifactPath, string status, string message, bool didMutate)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            packetKind,
            request.TaskCardId,
            request.RequestedAtUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            didMutate,
            artifactPath,
            message,
            status,
            status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ? "" : message));
    }
}
