using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record SelfImprovementReportRequest(
    DateTimeOffset SinceUtc,
    DateTimeOffset UntilUtc,
    string ArtifactRoot,
    DateTimeOffset GeneratedAtUtc);

public sealed record SelfImprovementReportBucket(
    string PacketKind,
    int Count,
    int NetworkCount,
    int HostedAiCount,
    int LocalModelCount,
    int MutationCount);

public sealed record SelfImprovementReportResult(
    bool Succeeded,
    string ArtifactFolder,
    string MarkdownPath,
    string JsonPath,
    int TotalRows,
    int FlaggedRows,
    IReadOnlyList<SelfImprovementReportBucket> Buckets,
    string Message);

public sealed class SelfImprovementReportService
{
    public const string PacketKind = "self_improvement_report";

    private readonly AuditLedgerService _ledger;
    private readonly KillSwitchService? _killSwitchService;

    public SelfImprovementReportService(AuditLedgerService ledger, KillSwitchService? killSwitchService = null)
    {
        _ledger = ledger;
        _killSwitchService = killSwitchService;
    }

    public SelfImprovementReportResult Run(SelfImprovementReportRequest request)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return new SelfImprovementReportResult(false, "", "", "", 0, 0, [], "kill_switch=true");
        }

        var artifactRoot = Path.GetFullPath(request.ArtifactRoot);
        Directory.CreateDirectory(artifactRoot);
        var folder = Path.Combine(artifactRoot, $"{request.GeneratedAtUtc:yyyyMMdd-HHmmss}-self-improvement-report");
        Directory.CreateDirectory(folder);

        var rows = _ledger.Snapshot(request.SinceUtc, request.UntilUtc);
        var buckets = rows
            .GroupBy(row => row.PacketKind, StringComparer.OrdinalIgnoreCase)
            .Select(group => new SelfImprovementReportBucket(
                group.Key,
                group.Count(),
                group.Count(row => row.DidUseNetwork),
                group.Count(row => row.DidUseHostedAi),
                group.Count(row => row.DidUseLocalModel),
                group.Count(row => row.DidMutate)))
            .OrderByDescending(bucket => bucket.Count)
            .ThenBy(bucket => bucket.PacketKind, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var flaggedRows = rows.Count(IsFlagged);
        var datasetRows = rows
            .Where(row => row.PacketKind.Contains("dataset", StringComparison.OrdinalIgnoreCase) ||
                          row.Summary.Contains("dataset", StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();
        var tuningRows = rows
            .Where(row => row.PacketKind.Equals(AuditLedgerService.TuningApplyPacketKind, StringComparison.OrdinalIgnoreCase) ||
                          row.PacketKind.Equals(AuditLedgerService.TuningRollbackPacketKind, StringComparison.OrdinalIgnoreCase) ||
                          row.PacketKind.Equals(AuditLedgerService.GoldenEvalPacketKind, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();

        var jsonPath = Path.Combine(folder, "report.json");
        var markdownPath = Path.Combine(folder, "report.md");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(new
        {
            schemaVersion = "1",
            generatedAtUtc = request.GeneratedAtUtc,
            sinceUtc = request.SinceUtc,
            untilUtc = request.UntilUtc,
            totalRows = rows.Count,
            flaggedRows,
            buckets,
            datasetEvents = datasetRows.Select(SafeRow),
            tuningAndEvalEvents = tuningRows.Select(SafeRow)
        }, JsonDefaults.Options));
        File.WriteAllText(markdownPath, BuildMarkdown(request, rows.Count, flaggedRows, buckets, datasetRows, tuningRows));

        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            PacketKind,
            TaskCardId: null,
            request.GeneratedAtUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            folder,
            $"Self-improvement report wrote {rows.Count} audited row(s) with {flaggedRows} flagged row(s).",
            "Completed"));

        return new SelfImprovementReportResult(
            true,
            folder,
            markdownPath,
            jsonPath,
            rows.Count,
            flaggedRows,
            buckets,
            "Self-improvement report written.");
    }

    private static bool IsFlagged(AuditLedgerRow row)
    {
        return row.DidUseNetwork || row.DidUseHostedAi || row.DidUseLocalModel || row.DidMutate;
    }

    private static object SafeRow(AuditLedgerRow row)
    {
        return new
        {
            row.PacketKind,
            row.CreatedAtUtc,
            row.Status,
            row.ArtifactPath,
            row.DidUseNetwork,
            row.DidUseHostedAi,
            row.DidUseLocalModel,
            row.DidMutate
        };
    }

    private static string BuildMarkdown(
        SelfImprovementReportRequest request,
        int totalRows,
        int flaggedRows,
        IReadOnlyList<SelfImprovementReportBucket> buckets,
        IReadOnlyList<AuditLedgerRow> datasetRows,
        IReadOnlyList<AuditLedgerRow> tuningRows)
    {
        var lines = new List<string>
        {
            "# Self-Improvement Report",
            "",
            $"Window: {request.SinceUtc:O} to {request.UntilUtc:O}",
            $"Audited rows: {totalRows}",
            $"Flagged rows: {flaggedRows}",
            ""
        };

        lines.Add("## Activity By Packet Kind");
        if (buckets.Count == 0)
        {
            lines.Add("- No audited helper activity in this window.");
        }
        else
        {
            lines.AddRange(buckets.Select(bucket => $"- {bucket.PacketKind}: {bucket.Count} row(s), flags network={bucket.NetworkCount}, hosted_ai={bucket.HostedAiCount}, local_model={bucket.LocalModelCount}, mutate={bucket.MutationCount}"));
        }

        lines.Add("");
        lines.Add("## Dataset And Learning Changes");
        if (datasetRows.Count == 0)
        {
            lines.Add("- No dataset promotion events were recorded in this window.");
        }
        else
        {
            lines.AddRange(datasetRows.Select(row => $"- {row.PacketKind} at {row.CreatedAtUtc:O}: artifact {SafeArtifact(row.ArtifactPath)}"));
        }

        lines.Add("");
        lines.Add("## Tuning, Eval, And Rollback Signals");
        if (tuningRows.Count == 0)
        {
            lines.Add("- No tuning, rollback, or golden-eval events were recorded in this window.");
        }
        else
        {
            lines.AddRange(tuningRows.Select(row => $"- {row.PacketKind} at {row.CreatedAtUtc:O}: status {row.Status}, artifact {SafeArtifact(row.ArtifactPath)}"));
        }

        lines.Add("");
        lines.Add("## Notes");
        lines.Add("- This report contains counts, flags, timestamps, and artifact links only.");
        lines.Add("- Rollback proposals are review-only and never execute automatically.");
        return string.Join(Environment.NewLine, lines);
    }

    private static string SafeArtifact(string artifactPath)
    {
        return string.IsNullOrWhiteSpace(artifactPath) ? "not recorded" : artifactPath;
    }
}
