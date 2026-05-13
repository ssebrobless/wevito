using System.Security.Cryptography;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record LearningLabPromotionRequest(
    string BundleFolder,
    string DatasetRoot,
    string ArtifactRoot,
    TaskCard ApprovedTaskCard,
    Guid PetId,
    string PetName,
    DateTimeOffset PromotedAtUtc);

public sealed record LearningLabPromotionResult(
    bool Succeeded,
    string DatasetVersion,
    string DatasetFolder,
    string ManifestPath,
    string ExamplesPath,
    string PromotionReportPath,
    int AcceptedCount,
    int ExcludedCount,
    int MemoryRowsWritten,
    string Message);

public sealed class LearningLabPromotionService
{
    public const bool AutomaticTrainingEnabled = false;
    public const bool AutomaticMemoryPromotionEnabled = false;
    public const string ToolFamily = "learningPromotion";

    private readonly PetMemoryStore _memoryStore;
    private readonly PetMemoryWriteGate _writeGate;

    public LearningLabPromotionService(PetMemoryStore? memoryStore = null, PetMemoryWriteGate? writeGate = null)
    {
        _memoryStore = memoryStore ?? new PetMemoryStore();
        _writeGate = writeGate ?? new PetMemoryWriteGate();
    }

    public LearningLabPromotionResult Promote(LearningLabPromotionRequest request)
    {
        var approval = ValidateApproval(request);
        if (!string.IsNullOrWhiteSpace(approval))
        {
            return Block(approval);
        }

        var labelsPath = Path.Combine(request.BundleFolder, "labels.json");
        var sourcesPath = Path.Combine(request.BundleFolder, "sources.json");
        var summaryPath = Path.Combine(request.BundleFolder, "summary.md");
        if (!File.Exists(labelsPath) || !File.Exists(sourcesPath) || !File.Exists(summaryPath))
        {
            return Block("Reviewed bundle must include labels.json, sources.json, and summary.md.");
        }

        var labels = ReadLabels(labelsPath);
        var sources = ReadSources(sourcesPath);
        var accepted = BuildAcceptedRows(labels, sources);
        if (accepted.Count == 0)
        {
            return Block("No accepted examples are available for promotion.");
        }

        var decision = _writeGate.Evaluate(new PetMemoryWriteRequest(
            request.PetId,
            request.PetName,
            ToolFamily,
            $"Promote {accepted.Count} reviewed Learning Lab example(s).",
            "accept",
            ContainsUntrustedContent: true,
            Approved: true));
        if (decision.Status != ToolPolicyDecisionStatus.Allowed)
        {
            return Block(decision.Reason);
        }

        var datasetVersion = BuildNextDatasetVersion(request.DatasetRoot, request.PromotedAtUtc);
        var datasetFolder = Path.Combine(request.DatasetRoot, datasetVersion);
        Directory.CreateDirectory(datasetFolder);

        var examplesPath = Path.Combine(datasetFolder, "examples.jsonl");
        WriteJsonLines(examplesPath, accepted);
        var examplesSha256 = ComputeSha256(examplesPath);
        var reviewer = string.Join(", ", accepted.Select(row => row.Reviewer).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase));
        var manifest = new LearningDatasetManifest(
            "1",
            datasetVersion,
            request.ApprovedTaskCard.Id,
            request.PetId,
            request.PetName,
            accepted.Count,
            labels.Count - accepted.Count,
            [labelsPath, sourcesPath, summaryPath],
            examplesSha256,
            string.IsNullOrWhiteSpace(reviewer) ? "unknown" : reviewer,
            AutomaticTrainingEnabled,
            AutomaticMemoryPromotionEnabled,
            request.PromotedAtUtc);
        var manifestPath = Path.Combine(datasetFolder, "manifest.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonDefaults.Options));

        var memoryRows = 0;
        foreach (var row in accepted)
        {
            _memoryStore.AddExample(
                request.PetId,
                ToolFamily,
                row.Content,
                row.Label,
                request.PromotedAtUtc,
                datasetVersion,
                request.ApprovedTaskCard.Id.ToString());
            memoryRows++;
        }

        var reportFolder = Path.Combine(request.ArtifactRoot, $"{request.PromotedAtUtc:yyyyMMdd-HHmmss}-learning-promotion");
        Directory.CreateDirectory(reportFolder);
        var promotionReportPath = Path.Combine(reportFolder, "promotion-report.json");
        File.WriteAllText(promotionReportPath, JsonSerializer.Serialize(new
        {
            schemaVersion = "1",
            taskCardId = request.ApprovedTaskCard.Id,
            datasetVersion,
            acceptedCount = accepted.Count,
            excludedCount = labels.Count - accepted.Count,
            memoryRowsWritten = memoryRows,
            didTrain = false,
            didCopyBinaryAssets = false,
            didMutate = true,
            datasetManifest = manifestPath,
            examplesPath
        }, JsonDefaults.Options));
        File.WriteAllText(Path.Combine(reportFolder, "run-summary.md"), string.Join(Environment.NewLine, [
            "# Learning Promotion",
            "",
            $"- Dataset: {datasetVersion}",
            $"- Accepted examples: {accepted.Count}",
            $"- Excluded examples: {labels.Count - accepted.Count}",
            $"- Memory rows written: {memoryRows}",
            "- Training started: false",
            "- Binary assets copied: false",
            "",
            "Reviewed examples were promoted into local memory and a versioned dataset only after an approved task card."
        ]));

        return new LearningLabPromotionResult(
            true,
            datasetVersion,
            datasetFolder,
            manifestPath,
            examplesPath,
            promotionReportPath,
            accepted.Count,
            labels.Count - accepted.Count,
            memoryRows,
            "Learning promotion completed without training or binary asset copy.");
    }

    private static string ValidateApproval(LearningLabPromotionRequest request)
    {
        if (request.ApprovedTaskCard.Status != TaskCardStatus.Approved)
        {
            return "Learning promotion requires an approved task card.";
        }

        if (!string.Equals(request.ApprovedTaskCard.ToolFamily, ToolFamily, StringComparison.OrdinalIgnoreCase))
        {
            return "Learning promotion task card must use the learningPromotion tool family.";
        }

        if (request.PetId == Guid.Empty || string.IsNullOrWhiteSpace(request.PetName))
        {
            return "Learning promotion requires a concrete helper pet.";
        }

        return "";
    }

    private static IReadOnlyList<BundleLabelRow> ReadLabels(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("labels")
            .EnumerateArray()
            .Select(item => new BundleLabelRow(
                ReadString(item, "AbsolutePath"),
                ReadString(item, "Label"),
                ReadString(item, "Reviewer"),
                ReadString(item, "Notes")))
            .ToList();
    }

    private static IReadOnlyList<BundleSourceRow> ReadSources(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("sources")
            .EnumerateArray()
            .Select(item => new BundleSourceRow(
                ReadString(item, "RelativePath"),
                ReadString(item, "AbsolutePath"),
                ReadString(item, "ArtifactKind"),
                ReadString(item, "Target")))
            .ToList();
    }

    private static IReadOnlyList<LearningPromotionExampleRow> BuildAcceptedRows(IReadOnlyList<BundleLabelRow> labels, IReadOnlyList<BundleSourceRow> sources)
    {
        var sourceByPath = sources
            .Where(source => !string.IsNullOrWhiteSpace(source.AbsolutePath))
            .ToDictionary(source => Path.GetFullPath(source.AbsolutePath), StringComparer.OrdinalIgnoreCase);

        return labels
            .Where(label => string.Equals(label.Label, "accept", StringComparison.OrdinalIgnoreCase))
            .Select(label =>
            {
                var key = Path.GetFullPath(label.AbsolutePath);
                sourceByPath.TryGetValue(key, out var source);
                var relativePath = source?.RelativePath ?? "";
                var artifactKind = source?.ArtifactKind ?? "reviewed-example";
                var target = source?.Target ?? "unknown";
                var content = $"{artifactKind} | {target} | {relativePath} | {label.Notes}".Trim();
                return new LearningPromotionExampleRow(
                    label.AbsolutePath,
                    relativePath,
                    artifactKind,
                    target,
                    label.Label,
                    label.Reviewer,
                    label.Notes,
                    content);
            })
            .OrderBy(row => row.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void WriteJsonLines(string path, IReadOnlyList<LearningPromotionExampleRow> rows)
    {
        File.WriteAllLines(path, rows.Select(row => JsonSerializer.Serialize(row, JsonDefaults.Options)));
    }

    private static string BuildNextDatasetVersion(string datasetRoot, DateTimeOffset timestamp)
    {
        Directory.CreateDirectory(datasetRoot);
        var next = Directory.EnumerateDirectories(datasetRoot, "v????-*", SearchOption.TopDirectoryOnly).Count() + 1;
        return $"v{next:0000}-{timestamp:yyyyMMdd-HHmmss}";
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string ReadString(JsonElement item, string propertyName)
    {
        if (item.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? "";
        }

        var camel = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
        return item.TryGetProperty(camel, out value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
    }

    private static LearningLabPromotionResult Block(string reason)
    {
        return new LearningLabPromotionResult(false, "", "", "", "", "", 0, 0, 0, reason);
    }

    private sealed record BundleLabelRow(
        string AbsolutePath,
        string Label,
        string Reviewer,
        string Notes);

    private sealed record BundleSourceRow(
        string RelativePath,
        string AbsolutePath,
        string ArtifactKind,
        string Target);
}
