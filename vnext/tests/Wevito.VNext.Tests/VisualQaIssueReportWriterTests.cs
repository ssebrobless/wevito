using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Shell;

namespace Wevito.VNext.Tests;

public sealed class VisualQaIssueReportWriterTests
{
    [Fact]
    public void WriteIssue_WritesMarkdownAndJsonUnderTimestampedFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), $"wevito-visual-qa-{Guid.NewGuid():N}");
        var auditPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wevito",
            "audit",
            "visual-qa-issues.jsonl");
        var beforeLength = File.Exists(auditPath) ? new FileInfo(auditPath).Length : 0;
        try
        {
            var writer = new VisualQaIssueReportWriter(root, () => DateTimeOffset.Parse("2026-05-14T12:34:56Z"));
            var snapshot = new DevControlSnapshot(
                [
                    new DevControlPetSlotSnapshot(
                        0,
                        Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        "Crow 1\ncrow | female | blue | baby",
                        "Crow 1",
                        "crow",
                        "female",
                        "blue",
                        "baby",
                        false,
                        false,
                        "Home",
                        "Idle"),
                    DevControlPetSlotSnapshot.Empty(1),
                    DevControlPetSlotSnapshot.Empty(2)
                ],
                DevControlOptions.Empty,
                DateTimeOffset.Parse("2026-05-14T12:34:56Z"));

            var result = writer.WriteIssue(new VisualQaIssueTagRequest(
                0,
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ["cropped", "bad_motion"],
                "Crow head flattens during one walk frame.",
                AttachCurrentScreenshot: false), snapshot);

            Assert.True(File.Exists(Path.Combine(result.PacketPath, "issue.json")));
            Assert.True(File.Exists(Path.Combine(result.PacketPath, "issue.md")));
            Assert.Contains("cropped", File.ReadAllText(Path.Combine(result.PacketPath, "issue.md")));

            var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(result.PacketPath, "issue.json")));
            Assert.Equal("crow", json.RootElement.GetProperty("slot").GetProperty("speciesId").GetString());
            Assert.Equal("Crow head flattens during one walk frame.", json.RootElement.GetProperty("notes").GetString());
            Assert.True(File.Exists(auditPath));
            Assert.True(new FileInfo(auditPath).Length > beforeLength);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
