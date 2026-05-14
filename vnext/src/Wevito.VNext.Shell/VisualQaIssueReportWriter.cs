using System.Text.Json;
using System.IO;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

internal sealed record VisualQaIssueReportResult(string PacketPath, string MarkdownPath, string JsonPath);

internal sealed class VisualQaIssueReportWriter
{
    private readonly string _root;
    private readonly Func<DateTimeOffset> _nowProvider;

    public VisualQaIssueReportWriter(string root, Func<DateTimeOffset>? nowProvider = null)
    {
        _root = root;
        _nowProvider = nowProvider ?? (() => DateTimeOffset.UtcNow);
    }

    public VisualQaIssueReportResult WriteIssue(VisualQaIssueTagRequest request, DevControlSnapshot snapshot)
    {
        var now = _nowProvider();
        var slug = BuildSlug(request, snapshot);
        var packetPath = Path.Combine(_root, $"{now:yyyyMMdd-HHmmss}-{slug}");
        Directory.CreateDirectory(packetPath);

        var slot = snapshot.Slots.FirstOrDefault(candidate => candidate.SlotIndex == request.SlotIndex)
            ?? DevControlPetSlotSnapshot.Empty(request.SlotIndex);
        var payload = new
        {
            generatedAtUtc = now,
            request.SlotIndex,
            request.ExpectedPetId,
            request.Tags,
            request.Notes,
            request.AttachCurrentScreenshot,
            slot,
            snapshot.CapturedAtUtc
        };

        var jsonPath = Path.Combine(packetPath, "issue.json");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(payload, JsonDefaults.Options), System.Text.Encoding.UTF8);

        var markdownPath = Path.Combine(packetPath, "issue.md");
        File.WriteAllText(markdownPath, BuildMarkdown(now, request, slot), System.Text.Encoding.UTF8);

        return new VisualQaIssueReportResult(packetPath, markdownPath, jsonPath);
    }

    private static string BuildMarkdown(DateTimeOffset now, VisualQaIssueTagRequest request, DevControlPetSlotSnapshot slot)
    {
        var tags = request.Tags.Count == 0 ? "none" : string.Join(", ", request.Tags.Select(tag => $"`{tag}`"));
        return string.Join(Environment.NewLine, [
            "# Visual QA Issue",
            "",
            $"- generated_at: `{now:O}`",
            $"- slot: `{request.SlotIndex + 1}`",
            $"- pet: `{slot.Name ?? "N/A"}`",
            $"- species: `{slot.SpeciesId ?? "N/A"}`",
            $"- life_stage: `{slot.LifeStage ?? "N/A"}`",
            $"- gender: `{slot.Gender ?? "N/A"}`",
            $"- color: `{slot.ColorVariant ?? "N/A"}`",
            $"- animation: `{slot.AnimationState ?? "N/A"}`",
            $"- tags: {tags}",
            "",
            "## Notes",
            "",
            string.IsNullOrWhiteSpace(request.Notes) ? "_No notes provided._" : request.Notes.Trim(),
            ""
        ]);
    }

    private static string BuildSlug(VisualQaIssueTagRequest request, DevControlSnapshot snapshot)
    {
        var slot = snapshot.Slots.FirstOrDefault(candidate => candidate.SlotIndex == request.SlotIndex);
        var species = Sanitize(slot?.SpeciesId ?? "empty");
        var tags = request.Tags.Count == 0 ? "untagged" : Sanitize(string.Join("-", request.Tags.Take(3)));
        return $"slot-{request.SlotIndex + 1}-{species}-{tags}";
    }

    private static string Sanitize(string value)
    {
        var chars = value.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
        return new string(chars).Trim('-');
    }
}
