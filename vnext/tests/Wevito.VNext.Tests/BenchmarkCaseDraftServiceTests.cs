using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class BenchmarkCaseDraftServiceTests
{
    [Fact]
    public void DraftsHonorKindAxis()
    {
        using var temp = BenchmarkCurationTempWorkspace.Create();
        var service = new BenchmarkCaseDraftService();

        var result = service.Draft(new BenchmarkDraftCaseRequest(
            temp.DraftRoot,
            temp.ApprovedRoot,
            "tool-use",
            "run sprite audit",
            "dispatch spriteAudit",
            ExpectedToolFamily: "spriteAudit",
            CreatedAtUtc: DateTimeOffset.Parse("2026-05-15T12:00:00Z")));

        Assert.True(result.Succeeded);
        Assert.Contains($"{Path.DirectorySeparatorChar}tool-use{Path.DirectorySeparatorChar}", result.DraftPath);
        Assert.Equal("tool-use", result.Case?.Axis);
        Assert.True(File.Exists(result.DraftPath));
    }

    [Fact]
    public void NeverOverwritesApprovedCases()
    {
        using var temp = BenchmarkCurationTempWorkspace.Create();
        var service = new BenchmarkCaseDraftService();
        var request = new BenchmarkDraftCaseRequest(
            temp.DraftRoot,
            temp.ApprovedRoot,
            "chat",
            "hello",
            "hello back",
            CreatedAtUtc: DateTimeOffset.Parse("2026-05-15T12:00:00Z"));
        var first = service.Draft(request);
        Assert.True(first.Succeeded);
        var approvedRoot = Path.Combine(temp.ApprovedRoot, "chat");
        Directory.CreateDirectory(approvedRoot);
        var approvedPath = Path.Combine(approvedRoot, Path.GetFileName(first.DraftPath));
        File.Copy(first.DraftPath, approvedPath);

        var second = service.Draft(request);

        Assert.False(second.Succeeded);
        Assert.Contains("Approved benchmark case already exists", second.Message);
        Assert.Equal(File.ReadAllText(first.DraftPath), File.ReadAllText(approvedPath));
    }
}

internal sealed class BenchmarkCurationTempWorkspace : IDisposable
{
    private BenchmarkCurationTempWorkspace(string root)
    {
        Root = root;
        DraftRoot = Path.Combine(root, "draft");
        ApprovedRoot = Path.Combine(root, "approved");
        DatabasePath = Path.Combine(root, "benchmark-curation.sqlite");
        Directory.CreateDirectory(DraftRoot);
        Directory.CreateDirectory(ApprovedRoot);
    }

    public string Root { get; }

    public string DraftRoot { get; }

    public string ApprovedRoot { get; }

    public string DatabasePath { get; }

    public static BenchmarkCurationTempWorkspace Create() => new(Path.Combine(Path.GetTempPath(), $"wevito-benchmark-curation-{Guid.NewGuid():N}"));

    public string WriteDraft(BenchmarkCase testCase)
    {
        var axisRoot = Path.Combine(DraftRoot, testCase.Axis);
        Directory.CreateDirectory(axisRoot);
        var path = Path.Combine(axisRoot, $"{testCase.Id}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(testCase, JsonDefaults.Options));
        return path;
    }

    public void Dispose()
    {
        if (!Directory.Exists(Root))
        {
            return;
        }

        try
        {
            foreach (var path in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }

            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // SQLite can hold a short-lived file handle on Windows.
        }
    }
}
