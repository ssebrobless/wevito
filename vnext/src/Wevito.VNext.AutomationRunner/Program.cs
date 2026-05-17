using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

var options = RunnerOptions.Parse(args);
if (options.SpriteRepairBatch)
{
    return await RunSpriteRepairBatchAsync(options);
}

if (!options.Sweep)
{
    Console.Error.WriteLine("Usage: Wevito.VNext.AutomationRunner --sweep --out <artifact-dir> [--repo-root <path>]");
    Console.Error.WriteLine("   or: Wevito.VNext.AutomationRunner --sprite-repair-batch --queue <repair_queue.json> --row-id <row> --out <artifact-dir> [--repo-root <path>]");
    return 2;
}

var repoRoot = ResolveRepoRoot(options.RepoRoot);
var outputRoot = Path.GetFullPath(options.OutputRoot ?? Path.Combine(repoRoot, "vnext", "artifacts", "c-phase-127-matrix-sweep"));
Directory.CreateDirectory(outputRoot);
Directory.CreateDirectory(Path.Combine(outputRoot, "cells"));

var species = FilterSpecies(LoadSpecies(repoRoot), options);
var optionalAnimations = LoadOptionalAnimations(repoRoot);
var animationFamilies = new[]
{
    "idle", "walk", "eat", "drink", "sleep", "groom", "bathe", "play", "sad", "happy"
}.Concat(optionalAnimations).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

var client = new DevPipeClient();
await client.WaitForPipeAsync(TimeSpan.FromSeconds(30));
await EnsureSuccess(client.SendAsync(VisualQaCommandTypes.ResetSaveSandbox, new VisualQaResetSaveSandboxRequest("empty_three_slots")), "reset save sandbox");

var rows = new List<VisualQaMatrixCell>();
foreach (var speciesEntry in species)
{
    foreach (var lifeStage in speciesEntry.LifeStages)
    foreach (var gender in speciesEntry.Genders)
    foreach (var color in speciesEntry.Colors)
    {
        var spawn = await client.SendAsync(DevControlCommandTypes.SpawnOrReplacePet, new DevControlSpawnOrReplacePetRequest(
            0,
            speciesEntry.Id,
            lifeStage,
            gender,
            color,
            ReplaceIfOccupied: true));

        await EnsureSuccess(Task.FromResult(spawn), $"spawn {speciesEntry.Id}/{lifeStage}/{gender}/{color}");
        var petId = spawn.Snapshot.Slots[0].PetId;
        var observations = new List<VisualQaAnimationObservation>();

        foreach (var animation in animationFamilies)
        {
            observations.Add(await ObserveAnimationAsync(client, repoRoot, outputRoot, speciesEntry.Id, lifeStage, gender, color, animation, petId));
        }

        var cellTags = observations.SelectMany(observation => observation.Tags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var notes = cellTags.Count == 0
            ? "No opaque-pixel heuristic findings."
            : $"Heuristic findings: {string.Join(", ", cellTags)}.";

        if (cellTags.Count > 0)
        {
            var tagResponse = await client.SendAsync(VisualQaCommandTypes.TagIssue, new VisualQaIssueTagRequest(
                0,
                petId,
                cellTags,
                $"{speciesEntry.Id}/{lifeStage}/{gender}/{color}: {notes}",
                AttachCurrentScreenshot: false));
            await EnsureSuccess(Task.FromResult(tagResponse), $"tag visual QA issue {speciesEntry.Id}/{lifeStage}/{gender}/{color}");
        }

        rows.Add(new VisualQaMatrixCell(speciesEntry.Id, lifeStage, gender, color, observations, cellTags, notes));
    }
}

var manifest = new VisualQaMatrixManifest(
    DateTimeOffset.UtcNow,
    repoRoot,
    species.Sum(item => item.LifeStages.Count * item.Genders.Count * item.Colors.Count),
    species.Select(item => item.Id).ToList(),
    species.SelectMany(item => item.LifeStages).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
    species.SelectMany(item => item.Genders).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
    species.SelectMany(item => item.Colors).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
    animationFamilies,
    rows);

var manifestPath = Path.Combine(outputRoot, "visual_qa_manifest.json");
await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, JsonDefaults.Options), Encoding.UTF8);
await File.WriteAllTextAsync(Path.Combine(outputRoot, "visual_qa_manifest.md"), BuildMarkdown(manifest), Encoding.UTF8);

if (manifest.Rows.Count != manifest.ExpectedCellCount)
{
    Console.Error.WriteLine($"Expected {manifest.ExpectedCellCount} rows but wrote {manifest.Rows.Count}.");
    return 3;
}

if (!manifest.Rows.Any(row => row.Tags.Count > 0) && !options.AllowClean)
{
    Console.Error.WriteLine("Matrix sweep produced no tagged cells; heuristics may be too lax.");
    return 4;
}

Console.WriteLine($"Wrote {manifest.Rows.Count} matrix rows to {manifestPath}");
return 0;

static async Task<int> RunSpriteRepairBatchAsync(RunnerOptions options)
{
    if (string.IsNullOrWhiteSpace(options.QueuePath) ||
        string.IsNullOrWhiteSpace(options.RowId) ||
        string.IsNullOrWhiteSpace(options.OutputRoot))
    {
        Console.Error.WriteLine("--sprite-repair-batch requires --queue, --row-id, and --out.");
        return 2;
    }

    var repoRoot = ResolveRepoRoot(options.RepoRoot);
    var outputRoot = Path.GetFullPath(options.OutputRoot);
    Directory.CreateDirectory(outputRoot);

    var queuePath = Path.GetFullPath(options.QueuePath);
    var queue = new SpriteRepairQueueReader().Load(queuePath, repoRoot);
    var row = queue.Rows.FirstOrDefault(candidate => string.Equals(candidate.RowId, options.RowId, StringComparison.OrdinalIgnoreCase));
    if (row is null)
    {
        Console.Error.WriteLine($"Queue row not found: {options.RowId}");
        return 3;
    }

    var runner = new SpriteRepairBatchRunner(auditLedgerService: new AuditLedgerService());
    var requestedAtUtc = DateTimeOffset.UtcNow;
    var results = new List<SpriteRepairBatchResult>();
    foreach (var issue in row.Issues)
    {
        var batchId = $"c-phase-130-001-{row.RowId}-{issue.ColorVariant}-{issue.AnimationFamily}";
        var issueArtifactRoot = Path.Combine(outputRoot, batchId);
        var result = await runner.RunAsync(new SpriteRepairBatchRequest(
            repoRoot,
            row,
            issue,
            issueArtifactRoot,
            requestedAtUtc,
            Guid.NewGuid(),
            batchId));
        results.Add(result);
        if (!result.Succeeded)
        {
            await WriteBatchSummaryAsync(outputRoot, row, results, requestedAtUtc);
            Console.Error.WriteLine($"Batch stopped on {issue.ColorVariant}/{issue.AnimationFamily}: {result.Message}");
            return 4;
        }
    }

    await WriteBatchSummaryAsync(outputRoot, row, results, requestedAtUtc);
    Console.WriteLine($"Completed {results.Count} repair issue(s) for {row.RowId}");
    return 0;
}

static async Task WriteBatchSummaryAsync(string outputRoot, SpriteRepairQueueRow row, IReadOnlyList<SpriteRepairBatchResult> results, DateTimeOffset requestedAtUtc)
{
    var summary = new
    {
        schemaVersion = "1.0",
        phase = "C-PHASE 130.001",
        requestedAtUtc,
        row = row.RowId,
        issueCount = results.Count,
        succeeded = results.All(result => result.Succeeded),
        didUseNetwork = false,
        didUseHostedAi = false,
        didUseLocalModel = false,
        didMutate = results.Any(result => result.Succeeded),
        results = results.Select(result => new
        {
            result.Status,
            result.Message,
            result.ArtifactPath,
            result.CandidateFolder,
            result.BackupFolder,
            result.RolledBack,
            result.PreHashes,
            result.PostHashes
        }).ToList()
    };
    await File.WriteAllTextAsync(
        Path.Combine(outputRoot, "sprite_repair_batch_summary.json"),
        JsonSerializer.Serialize(summary, JsonDefaults.Options),
        Encoding.UTF8);
}

static async Task<VisualQaAnimationObservation> ObserveAnimationAsync(
    DevPipeClient client,
    string repoRoot,
    string outputRoot,
    string speciesId,
    string lifeStage,
    string gender,
    string color,
    string animation,
    Guid? petId)
{
    var warnings = new List<string>();
    var tags = new List<string>();
    var forceSucceeded = false;
    if (TryMapToAnimationState(animation, out var forceAnimation))
    {
        var force = await client.SendAsync(VisualQaCommandTypes.ForceAnimation, new VisualQaForceAnimationRequest(
            0,
            petId,
            forceAnimation,
            FrameIndex: null,
            PlaybackSpeed: 1,
            Loop: true));
        forceSucceeded = force.Success;
        if (!force.Success)
        {
            warnings.Add(force.Message);
        }
    }
    else
    {
        warnings.Add("Animation family is source-only for this runner because it is optional/propped rather than a PetAnimationState.");
    }

    var framePaths = FindRuntimeFrames(repoRoot, speciesId, lifeStage, gender, color, animation);
    var assetSourceSucceeded = framePaths.Count > 0;
    if (!assetSourceSucceeded)
    {
        if (IsRequiredRuntimeAnimation(animation))
        {
            tags.Add("missing_frames");
        }

        warnings.Add($"No runtime frames found for {animation}.");
    }

    if (framePaths.Count > 0)
    {
        var firstFrame = framePaths[0];
        var capturePath = Path.Combine(outputRoot, "cells", $"{speciesId}_{lifeStage}_{gender}_{color}_{animation}.png");
        File.Copy(firstFrame, capturePath, overwrite: true);
        tags.AddRange(AnalyzeFrames(repoRoot, speciesId, lifeStage, gender, color, animation, framePaths));
        return new VisualQaAnimationObservation(animation, forceSucceeded, assetSourceSucceeded, firstFrame, framePaths.Count, capturePath, tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), warnings);
    }

    return new VisualQaAnimationObservation(animation, forceSucceeded, assetSourceSucceeded, null, 0, null, tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), warnings);
}

static IReadOnlyList<string> AnalyzeFrames(string repoRoot, string speciesId, string lifeStage, string gender, string color, string animation, IReadOnlyList<string> framePaths)
{
    var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var framePath in framePaths)
    {
        var analysis = PngAnalyzer.Analyze(framePath);
        if (analysis.OpaquePixelCount < 8)
        {
            tags.Add("blank");
        }

        if (analysis.IsSolidWhite)
        {
            tags.Add("solid_white");
        }
        else if (analysis.IsSolidBlack)
        {
            tags.Add("solid_black");
        }
        else if (analysis.IsSolidColor)
        {
            tags.Add("solid_color");
        }

        if (analysis.EdgeTouch)
        {
            tags.Add("crop_detected");
        }

        if (analysis.OpaqueCornerCount >= 2)
        {
            tags.Add("box_background");
        }
    }

    if (!string.Equals(animation, "idle", StringComparison.OrdinalIgnoreCase))
    {
        var idlePath = Path.Combine(repoRoot, "sprites_runtime", speciesId, lifeStage, gender, color, "idle_00.png");
        if (File.Exists(idlePath) && framePaths.Count > 0)
        {
            var idleHash = Sha256(idlePath);
            if (framePaths.All(path => string.Equals(Sha256(path), idleHash, StringComparison.OrdinalIgnoreCase)))
            {
                tags.Add("identical_to_idle");
            }
        }
    }

    return tags.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToList();
}

static IReadOnlyList<string> FindRuntimeFrames(string repoRoot, string speciesId, string lifeStage, string gender, string color, string animation)
{
    var folder = Path.Combine(repoRoot, "sprites_runtime", speciesId, lifeStage, gender, color);
    var runtimeAnimation = ResolveRuntimeAnimationId(animation);
    return Directory.Exists(folder)
        ? Directory.GetFiles(folder, $"{runtimeAnimation}_*.png").OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList()
        : [];
}

static IReadOnlyList<SpeciesAxis> FilterSpecies(IReadOnlyList<SpeciesAxis> species, RunnerOptions options)
{
    var filtered = species
        .Where(item => Matches(options.SpeciesId, item.Id))
        .Select(item => item with
        {
            LifeStages = item.LifeStages.Where(stage => Matches(options.LifeStage, stage)).ToList(),
            Genders = item.Genders.Where(gender => Matches(options.Gender, gender)).ToList(),
            Colors = item.Colors.Where(color => Matches(options.Color, color)).ToList()
        })
        .Where(item => item.LifeStages.Count > 0 && item.Genders.Count > 0 && item.Colors.Count > 0)
        .ToList();

    if (filtered.Count == 0)
    {
        throw new InvalidOperationException("No species rows matched the requested sweep filters.");
    }

    return filtered;
}

static bool Matches(string? filter, string value)
{
    return string.IsNullOrWhiteSpace(filter) || string.Equals(filter, value, StringComparison.OrdinalIgnoreCase);
}

static string ResolveRuntimeAnimationId(string animation)
{
    return string.Equals(animation, "play", StringComparison.OrdinalIgnoreCase) ? "happy" : animation;
}

static bool IsRequiredRuntimeAnimation(string animation)
{
    return animation is "idle" or "walk" or "eat" or "drink" or "sleep" or "groom" or "bathe" or "sad" or "happy";
}

static bool TryMapToAnimationState(string animation, out string state)
{
    state = animation switch
    {
        "idle" => "Idle",
        "walk" => "Walk",
        "eat" => "Eat",
        "drink" => "Drink",
        "sleep" => "Sleep",
        "groom" => "Groom",
        "bathe" => "Bathe",
        "sad" => "Sad",
        "happy" => "Happy",
        "play" => "Happy",
        _ => ""
    };

    return state.Length > 0;
}

static string Sha256(string path)
{
    using var stream = File.OpenRead(path);
    return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
}

static async Task EnsureSuccess(Task<DevControlResponseEnvelope> task, string context)
{
    var response = await task;
    if (!response.Success)
    {
        throw new InvalidOperationException($"{context} failed: {response.Message}");
    }
}

static IReadOnlyList<SpeciesAxis> LoadSpecies(string repoRoot)
{
    using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(repoRoot, "vnext", "content", "species.json")));
    return doc.RootElement.EnumerateArray()
        .Select(item => new SpeciesAxis(
            item.GetProperty("id").GetString() ?? throw new InvalidDataException("Species id missing."),
            ReadStringArray(item, "supportedAgeStages").Where(stage => !string.Equals(stage, "senior", StringComparison.OrdinalIgnoreCase)).ToList(),
            ReadStringArray(item, "supportedGenders"),
            ReadStringArray(item, "supportedColors").Where(color => !string.Equals(color, "green", StringComparison.OrdinalIgnoreCase)).ToList()))
        .ToList();
}

static IReadOnlyList<string> LoadOptionalAnimations(string repoRoot)
{
    var path = Path.Combine(repoRoot, "vnext", "content", "optional_animation_families.json");
    if (!File.Exists(path))
    {
        return [];
    }

    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    return doc.RootElement.EnumerateArray()
        .Select(item => item.GetProperty("id").GetString())
        .Where(id => !string.IsNullOrWhiteSpace(id))
        .Select(id => id!)
        .ToList();
}

static IReadOnlyList<string> ReadStringArray(JsonElement item, string propertyName)
{
    return item.TryGetProperty(propertyName, out var property)
        ? property.EnumerateArray().Select(value => value.GetString() ?? "").Where(value => value.Length > 0).ToList()
        : [];
}

static string ResolveRepoRoot(string? explicitRoot)
{
    if (!string.IsNullOrWhiteSpace(explicitRoot))
    {
        return Path.GetFullPath(explicitRoot);
    }

    var current = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "vnext", "content", "species.json")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate Wevito repo root.");
}

static string BuildMarkdown(VisualQaMatrixManifest manifest)
{
    var taggedRows = manifest.Rows.Count(row => row.Tags.Count > 0);
    var tagCounts = manifest.Rows
        .SelectMany(row => row.Tags)
        .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(group => group.Count())
        .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
        .Select(group => $"- `{group.Key}`: {group.Count()}");

    return string.Join(Environment.NewLine, [
        "# C-PHASE 127 Visual QA Matrix Sweep",
        "",
        $"- generated_at: `{manifest.GeneratedAtUtc:O}`",
        $"- expected_cells: `{manifest.ExpectedCellCount}`",
        $"- rows: `{manifest.Rows.Count}`",
        $"- tagged_rows: `{taggedRows}`",
        "",
        "## Tag Counts",
        "",
        .. tagCounts,
        ""
    ]);
}

internal sealed record RunnerOptions(
    bool Sweep,
    bool SpriteRepairBatch,
    string? OutputRoot,
    string? RepoRoot,
    string? QueuePath,
    string? RowId,
    string? SpeciesId,
    string? LifeStage,
    string? Gender,
    string? Color,
    bool AllowClean)
{
    public static RunnerOptions Parse(string[] args)
    {
        var sweep = args.Contains("--sweep", StringComparer.OrdinalIgnoreCase);
        var spriteRepairBatch = args.Contains("--sprite-repair-batch", StringComparer.OrdinalIgnoreCase);
        string? outputRoot = null;
        string? repoRoot = null;
        string? queuePath = null;
        string? rowId = null;
        string? speciesId = null;
        string? lifeStage = null;
        string? gender = null;
        string? color = null;
        var allowClean = args.Contains("--allow-clean", StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index++)
        {
            if (string.Equals(args[index], "--out", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                outputRoot = args[index + 1];
            }
            else if (string.Equals(args[index], "--repo-root", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                repoRoot = args[index + 1];
            }
            else if (string.Equals(args[index], "--queue", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                queuePath = args[index + 1];
            }
            else if (string.Equals(args[index], "--row-id", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                rowId = args[index + 1];
            }
            else if (string.Equals(args[index], "--species", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                speciesId = args[index + 1];
            }
            else if (string.Equals(args[index], "--age", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                lifeStage = args[index + 1];
            }
            else if (string.Equals(args[index], "--gender", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                gender = args[index + 1];
            }
            else if (string.Equals(args[index], "--color", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                color = args[index + 1];
            }
        }

        return new RunnerOptions(sweep, spriteRepairBatch, outputRoot, repoRoot, queuePath, rowId, speciesId, lifeStage, gender, color, allowClean);
    }
}

internal sealed record SpeciesAxis(string Id, IReadOnlyList<string> LifeStages, IReadOnlyList<string> Genders, IReadOnlyList<string> Colors);

internal sealed class DevPipeClient
{
    private const string PipeName = "wevito-vnext-dev-control";
    private const string PipeNameEnvironmentVariable = "WEVITO_DEV_CONTROL_PIPE";

    public async Task WaitForPipeAsync(TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        Exception? last = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                var response = await SendAsync(DevControlCommandTypes.GetSnapshot, new DevControlGetSnapshotRequest());
                if (response.Success)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                last = ex;
                await Task.Delay(500);
            }
        }

        throw new TimeoutException($"Timed out waiting for dev-control pipe. Last error: {last?.Message ?? "none"}");
    }

    public async Task<DevControlResponseEnvelope> SendAsync<TPayload>(string commandType, TPayload payload)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var pipe = new NamedPipeClientStream(".", ResolvePipeName(), PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(timeout.Token);

        await using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, leaveOpen: true);

        await writer.WriteLineAsync(DevControlPipeMessage.SerializeCommand(commandType, payload));
        var line = await reader.ReadLineAsync(timeout.Token);
        if (string.IsNullOrWhiteSpace(line))
        {
            throw new InvalidOperationException("Dev-control returned an empty response.");
        }

        return DevControlPipeMessage.DeserializeResponse(line);
    }

    private static string ResolvePipeName()
    {
        var configured = Environment.GetEnvironmentVariable(PipeNameEnvironmentVariable);
        return string.IsNullOrWhiteSpace(configured) ? PipeName : configured.Trim();
    }
}

internal sealed record PngAnalysis(
    int Width,
    int Height,
    int OpaquePixelCount,
    bool EdgeTouch,
    int OpaqueCornerCount,
    bool IsSolidColor,
    bool IsSolidWhite,
    bool IsSolidBlack);

internal static class PngAnalyzer
{
    public static PngAnalysis Analyze(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var source = decoder.Frames[0];
        var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        var width = converted.PixelWidth;
        var height = converted.PixelHeight;
        var stride = width * 4;
        var pixels = new byte[stride * height];
        converted.CopyPixels(pixels, stride, 0);

        var opaque = new List<(byte B, byte G, byte R, int X, int Y)>();
        var edgeTouch = false;
        var cornerCount = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var offset = y * stride + x * 4;
                var alpha = pixels[offset + 3];
                if (alpha <= 32)
                {
                    continue;
                }

                var pixel = (pixels[offset], pixels[offset + 1], pixels[offset + 2], x, y);
                opaque.Add(pixel);
                edgeTouch |= x <= 1 || y <= 1 || x >= width - 2 || y >= height - 2;
                if ((x <= 1 || x >= width - 2) && (y <= 1 || y >= height - 2))
                {
                    cornerCount++;
                }
            }
        }

        if (opaque.Count == 0)
        {
            return new PngAnalysis(width, height, 0, false, 0, false, false, false);
        }

        var centroidB = opaque.Average(pixel => pixel.B);
        var centroidG = opaque.Average(pixel => pixel.G);
        var centroidR = opaque.Average(pixel => pixel.R);
        var solid = opaque.All(pixel => Distance(pixel.R, pixel.G, pixel.B, centroidR, centroidG, centroidB) <= 8);
        var solidWhite = solid && Distance(255, 255, 255, centroidR, centroidG, centroidB) <= 16;
        var solidBlack = solid && Distance(0, 0, 0, centroidR, centroidG, centroidB) <= 16;

        return new PngAnalysis(width, height, opaque.Count, edgeTouch, cornerCount, solid, solidWhite, solidBlack);
    }

    private static double Distance(double r, double g, double b, double targetR, double targetG, double targetB)
    {
        var dr = r - targetR;
        var dg = g - targetG;
        var db = b - targetB;
        return Math.Sqrt((dr * dr) + (dg * dg) + (db * db));
    }
}
