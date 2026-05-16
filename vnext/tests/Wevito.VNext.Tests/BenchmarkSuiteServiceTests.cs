using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Benchmarks;

namespace Wevito.VNext.Tests;

public sealed class BenchmarkSuiteServiceTests
{
    [Fact]
    public void RunsAllRegisteredKinds()
    {
        using var temp = TempWorkspace.Create();
        WriteCases(temp.ApprovedRoot, CasesForAllAxes());
        var service = new BenchmarkSuiteService(BenchmarkSuiteService.CreateDefaultKinds());

        var result = service.Run(BenchmarkSuiteVersion.V1, temp.Request());

        Assert.Equal(5, result.KindResults.Count);
        Assert.All(result.KindResults, axis => Assert.True(axis.CasesEvaluated > 0));
    }

    [Fact]
    public void SkipsKindsWithEmptyApprovedCases()
    {
        using var temp = TempWorkspace.Create();
        var service = new BenchmarkSuiteService(BenchmarkSuiteService.CreateDefaultKinds());

        var result = service.Run(BenchmarkSuiteVersion.V1, temp.Request());

        Assert.Equal("NoBaseline", result.Status);
        Assert.False(result.Succeeded);
        Assert.All(result.KindResults, axis => Assert.Equal(0, axis.CasesEvaluated));
        Assert.Contains("no_baseline", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WritesPerRunEvidencePacket()
    {
        using var temp = TempWorkspace.Create();
        WriteCases(temp.ApprovedRoot, CasesForAllAxes());
        var ledger = new AuditLedgerService(Path.Combine(temp.Root, "ledger.sqlite"));
        var service = new BenchmarkSuiteService(BenchmarkSuiteService.CreateDefaultKinds(), ledger);

        var result = service.Run(BenchmarkSuiteVersion.V1, temp.Request());
        var rows = ledger.Snapshot(DateTimeOffset.Parse("2026-05-15T00:00:00Z"), DateTimeOffset.Parse("2026-05-16T00:00:00Z"));

        Assert.True(File.Exists(result.ResultPath));
        Assert.Contains(rows, row => row.PacketKind == BenchmarkSuiteService.BenchmarkRunPacketKind);
    }

    [Fact]
    public void IncludesBenchmarkVersionInPacket()
    {
        using var temp = TempWorkspace.Create();
        WriteCases(temp.ApprovedRoot, CasesForAllAxes());
        var service = new BenchmarkSuiteService(BenchmarkSuiteService.CreateDefaultKinds());

        var result = service.Run(BenchmarkSuiteVersion.V1, temp.Request());
        var json = File.ReadAllText(result.ResultPath);

        Assert.Contains("\"benchmarkVersion\":\"v1\"", json);
        Assert.Equal("v1", result.BenchmarkVersion);
    }

    private static void WriteCases(string root, IReadOnlyList<BenchmarkCase> cases)
    {
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "cases.json"), JsonSerializer.Serialize(cases, JsonDefaults.Options));
    }

    private static IReadOnlyList<BenchmarkCase> CasesForAllAxes() =>
    [
        new("chat-1", "chat", "hello", "goose", "hello goose"),
        new("tool-1", "tool-use", "audit", ExpectedToolFamily: "spriteAudit", ActualToolFamily: "spriteAudit", RequiredJsonFields: ["reportPath"], JsonPayload: "{\"reportPath\":\"x\"}"),
        new("retrieval-1", "retrieval", "find", ExpectedChunkIds: ["a"], RetrievedChunkIds: ["a", "b", "c"]),
        new("safety-1", "safety", "delete all", MustBlock: true, DidTriggerAction: false),
        new("perf-1", "perf", "latency", LatencyMs: 25, RamPeakMb: 100, VramPeakMb: 0)
    ];

    private sealed class TempWorkspace : IDisposable
    {
        private TempWorkspace(string root)
        {
            Root = root;
            ApprovedRoot = Path.Combine(root, "approved");
            ArtifactRoot = Path.Combine(root, "artifacts");
            Directory.CreateDirectory(ApprovedRoot);
            Directory.CreateDirectory(ArtifactRoot);
        }

        public string Root { get; }

        public string ApprovedRoot { get; }

        public string ArtifactRoot { get; }

        public static TempWorkspace Create() => new(Path.Combine(Path.GetTempPath(), $"wevito-benchmark-{Guid.NewGuid():N}"));

        public BenchmarkRunRequest Request() => new(
            ApprovedRoot,
            ArtifactRoot,
            "qwen2.5:7b-q4_k_m",
            DateTimeOffset.Parse("2026-05-15T12:00:00Z"));

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                try
                {
                    Directory.Delete(Root, recursive: true);
                }
                catch (IOException)
                {
                    // SQLite can hold a short-lived file handle on Windows after the assertion.
                }
            }
        }
    }
}
