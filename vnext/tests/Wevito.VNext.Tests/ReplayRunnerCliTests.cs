using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Experiments;
using Wevito.VNext.Core.SelfImprovement.Replay;

namespace Wevito.VNext.Tests;

public sealed class ReplayRunnerCliTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-19T12:00:00Z");

    [Fact]
    public void Run_IdenticalCapture_WritesResultAndReturnsZero()
    {
        var fixture = CreateFixture("seed-identical");
        var output = new StringWriter();

        var exitCode = InvokeCli(["run", "--captured", fixture.CapturePath], output, new StringWriter());

        Assert.Equal(0, exitCode);
        var resultPath = Path.Combine(Path.GetDirectoryName(fixture.CapturePath)!, "replay-result.json");
        Assert.True(File.Exists(resultPath));
        var summary = JsonSerializer.Deserialize<ReplayResultSummary>(File.ReadAllText(resultPath), JsonDefaults.Options);
        Assert.NotNull(summary);
        Assert.Equal("Identical", summary!.ResultKind);
        Assert.Equal(fixture.OperationId, summary.OperationId);
        Assert.Contains(resultPath, output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Run_DivergedCapture_ReturnsExitCode3()
    {
        var fixture = CreateFixture("seed-diverged", capture => capture with
        {
            Packets = capture.Packets.Select((packet, index) => index == 0 ? packet with { Summary = "changed" } : packet).ToArray()
        });

        var exitCode = InvokeCli(["run", "--captured", fixture.CapturePath], new StringWriter(), new StringWriter());

        Assert.Equal(3, exitCode);
        var summary = JsonSerializer.Deserialize<ReplayResultSummary>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(fixture.CapturePath)!, "replay-result.json")), JsonDefaults.Options);
        Assert.Equal("Diverged", summary!.ResultKind);
        Assert.NotEmpty(summary.FirstTenDiffs);
    }

    [Fact]
    public void Run_NotApplicableCapture_ReturnsExitCode2()
    {
        var fixture = CreateFixture("seed-not-applicable", capture => capture with { Seed = "" });

        var exitCode = InvokeCli(["run", "--captured", fixture.CapturePath], new StringWriter(), new StringWriter());

        Assert.Equal(2, exitCode);
        var summary = JsonSerializer.Deserialize<ReplayResultSummary>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(fixture.CapturePath)!, "replay-result.json")), JsonDefaults.Options);
        Assert.Equal("NotApplicable", summary!.ResultKind);
    }

    [Fact]
    public void Run_MissingCapture_ReturnsExitCode4()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-replay-runner-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var exitCode = InvokeCli(["run", "--captured", Path.Combine(root, "missing.json")], new StringWriter(), new StringWriter());

        Assert.Equal(4, exitCode);
    }

    [Fact]
    public void Run_ResultOutsideVNextArtifacts_Refuses()
    {
        var fixture = CreateFixture("seed-outside");
        var outside = Path.Combine(fixture.Root, "outside", "replay-result.json");

        var exitCode = InvokeCli(["run", "--captured", fixture.CapturePath, "--result", outside], new StringWriter(), new StringWriter());

        Assert.NotEqual(0, exitCode);
        Assert.False(File.Exists(outside));
    }

    [Fact]
    public void Run_WritesNoFileOutsideArtifactsRoot()
    {
        var fixture = CreateFixture("seed-confined");
        var outsideBefore = Directory.EnumerateFiles(fixture.Root, "*", SearchOption.AllDirectories)
            .Where(path => !Path.GetFullPath(path).StartsWith(fixture.ArtifactsRoot, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => Path.GetRelativePath(fixture.Root, path))
            .ToArray();

        var exitCode = InvokeCli(["run", "--captured", fixture.CapturePath], new StringWriter(), new StringWriter());

        Assert.Equal(0, exitCode);
        var outsideAfter = Directory.EnumerateFiles(fixture.Root, "*", SearchOption.AllDirectories)
            .Where(path => !Path.GetFullPath(path).StartsWith(fixture.ArtifactsRoot, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => Path.GetRelativePath(fixture.Root, path))
            .ToArray();
        Assert.Equal(outsideBefore, outsideAfter);
    }

    [Fact]
    public void ReplayRunnerAndStore_DoNotReferenceNetworkOrEvalStores()
    {
        var root = FindRepositoryRoot();
        var files = new[]
        {
            Path.Combine(root, "vnext", "tools", "Wevito.Tools.ReplayRunner", "Program.cs"),
            Path.Combine(root, "vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Replay", "ReplayResultStore.cs"),
            Path.Combine(root, "vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Replay", "ReplayHarness.cs"),
            Path.Combine(root, "vnext", "src", "Wevito.VNext.Shell", "ToolPopupWindow.xaml.cs")
        };

        var source = string.Join(Environment.NewLine, files.Select(File.ReadAllText));
        Assert.DoesNotContain("IHeldOutEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HeldOutEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IInDistributionEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InDistributionEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_ledger.Record", source, StringComparison.Ordinal);
    }

    private static int InvokeCli(string[] args, TextWriter output, TextWriter error)
    {
        var programType = LoadCliAssembly().GetType("Wevito.Tools.ReplayRunner.Program", throwOnError: true)!;
        var run = programType.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!;
        return (int)run.Invoke(null, [args, output, error])!;
    }

    private static Assembly LoadCliAssembly()
    {
        var project = Path.GetFullPath(Path.Combine(FindRepositoryRoot(), "vnext", "tools", "Wevito.Tools.ReplayRunner", "Wevito.Tools.ReplayRunner.csproj"));
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { "build", project, "--nologo" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });
        Assert.NotNull(process);
        process!.WaitForExit();
        Assert.Equal(0, process.ExitCode);
        var assemblyPath = Path.Combine(Path.GetDirectoryName(project)!, "bin", "Debug", "net8.0", "Wevito.Tools.ReplayRunner.dll");
        return Assembly.LoadFrom(assemblyPath);
    }

    private static ReplayRunnerFixture CreateFixture(string seed, Func<ReplayCapture, ReplayCapture>? mutate = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-replay-runner-cli-tests", Guid.NewGuid().ToString("N"));
        var artifactsRoot = Path.Combine(root, "vnext", "artifacts");
        Directory.CreateDirectory(Path.Combine(root, "tools"));
        Directory.CreateDirectory(Path.Combine(root, "sprites_runtime", "snake", "baby", "female", "blue"));
        Directory.CreateDirectory(Path.Combine(artifactsRoot, "captured", seed));
        File.WriteAllText(Path.Combine(root, "wevito.godot"), "");
        File.WriteAllText(Path.Combine(root, "tools", "fake_repair.py"), "# fake repair");
        File.WriteAllText(Path.Combine(root, "sprites_runtime", "snake", "baby", "female", "blue", "idle_00.png"), "before");

        var queuePath = WriteSpriteRepairQueue(root, Row(root));
        var scope = new SpriteRepairBatchProposalScope(queuePath, new AuditLedgerService(Path.Combine(root, "unused-ledger.sqlite")));
        var packets = new List<EvidencePacket>();
        var result = scope.TryRun(Request(artifactsRoot), seed, packets.Add);
        Assert.True(result.Ran);

        var operationId = $"operation-{seed}";
        var capture = new ReplayCapture(scope.Descriptor.ScopeId, operationId, seed, Now, packets);
        capture = mutate?.Invoke(capture) ?? capture;
        var capturePath = Path.Combine(artifactsRoot, "captured", seed, "capture.json");
        File.WriteAllText(capturePath, JsonSerializer.Serialize(capture, JsonDefaults.Options));
        return new ReplayRunnerFixture(root, artifactsRoot, capturePath, operationId);
    }

    private static AutonomousScopeRunRequest Request(string artifactRoot)
    {
        return new AutonomousScopeRunRequest(
            Settings(),
            new RuntimeSupervisorStatus(RuntimeSupervisorMode.Active, true, true, false, "active", ""),
            artifactRoot,
            Now,
            []);
    }

    private static Dictionary<string, string> Settings()
    {
        return new Dictionary<string, string>
        {
            [AutonomousOperationsConfig.EnabledSetting] = bool.TrueString,
            [AutonomousOperationsConfig.DailyCapSetting] = "3",
            [AutonomousOperationsConfig.IntervalMinutesSetting] = "10",
            [RuntimeSupervisorService.BackgroundWorkAllowedSetting] = bool.TrueString,
            [AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairBatchProposalScopeId)] = bool.TrueString
        };
    }

    private static string WriteSpriteRepairQueue(string root, SpriteRepairQueueRow row)
    {
        var queueRoot = Path.Combine(root, "vnext", "artifacts", "c-phase-128-sprite-repair-queue");
        Directory.CreateDirectory(queueRoot);
        var queuePath = Path.Combine(queueRoot, "repair_queue.json");
        var manifest = new SpriteRepairQueueManifest(
            "1.0",
            Now,
            "visual_qa_manifest.json",
            Now.AddMinutes(-30).ToString("O"),
            1,
            1,
            new Dictionary<string, int> { [row.Priority] = 1 },
            new Dictionary<string, int> { ["crop_detected"] = 1 },
            [row]);
        File.WriteAllText(queuePath, JsonSerializer.Serialize(manifest, JsonDefaults.Options));
        return queuePath;
    }

    private static SpriteRepairQueueRow Row(string root)
    {
        return new SpriteRepairQueueRow(
            "snake_baby_female",
            "snake",
            "baby",
            "female",
            "P1",
            "queued",
            1,
            ["blue"],
            ["idle"],
            ["tools/fake_repair.py"],
            [
                new SpriteRepairQueueIssue(
                    "blue",
                    "idle",
                    "P1",
                    ["crop_detected"],
                    ["test warning"],
                    "tools/fake_repair.py",
                    "test reason",
                    Path.Combine(root, "sprites_runtime", "snake", "baby", "female", "blue", "idle_00.png"),
                    null)
            ]);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "wevito.godot")) ||
                Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }

    private sealed record ReplayRunnerFixture(string Root, string ArtifactsRoot, string CapturePath, string OperationId);
}
