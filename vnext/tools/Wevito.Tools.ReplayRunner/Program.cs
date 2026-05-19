using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Experiments;
using Wevito.VNext.Core.SelfImprovement.Replay;

namespace Wevito.Tools.ReplayRunner;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            return Run(args, Console.Out, Console.Error);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 0 || !args[0].Equals("run", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine("Usage: run --captured <path> [--result <path>]");
            return 1;
        }

        var options = Parse(args.Skip(1).ToArray());
        if (!options.TryGetValue("captured", out var capturedPath) || string.IsNullOrWhiteSpace(capturedPath))
        {
            error.WriteLine("Capture file is required.");
            return 4;
        }

        var canonicalCapture = Path.GetFullPath(capturedPath);
        if (!File.Exists(canonicalCapture))
        {
            error.WriteLine("Capture file does not exist.");
            return 4;
        }

        var resultPath = options.TryGetValue("result", out var requestedResult) && !string.IsNullOrWhiteSpace(requestedResult)
            ? Path.GetFullPath(requestedResult)
            : Path.Combine(Path.GetDirectoryName(canonicalCapture) ?? ".", "replay-result.json");
        if (!IsUnderVNextArtifacts(resultPath))
        {
            error.WriteLine("Replay result path must be under vnext/artifacts.");
            return 1;
        }

        var captureDocument = JsonSerializer.Deserialize<ReplayCaptureDocument>(File.ReadAllText(canonicalCapture), JsonDefaults.Options);
        if (captureDocument is null)
        {
            error.WriteLine("Capture JSON could not be parsed.");
            return 2;
        }

        var capture = new ReplayCapture(
            captureDocument.ScopeId,
            captureDocument.OperationId,
            captureDocument.Seed,
            captureDocument.OriginalAtUtc,
            captureDocument.Packets ?? []);

        var artifactRoot = ResolveArtifactRoot(capture);
        var repoRoot = ResolveRepoRootFromArtifactRoot(artifactRoot);
        var queuePath = Path.Combine(repoRoot, "vnext", "artifacts", "c-phase-128-sprite-repair-queue", "repair_queue.json");
        var ledger = new AuditLedgerService(Path.Combine(Path.GetDirectoryName(resultPath) ?? ".", "replay-runner-unused-ledger.sqlite"));
        var scope = new SpriteRepairBatchProposalScope(queuePath, ledger);
        var harness = new ReplayHarness(scope);
        var result = harness.Replay(capture);

        harness.WriteResult(resultPath, result, capture, DateTimeOffset.UtcNow);
        output.WriteLine(resultPath);

        return result switch
        {
            ReplayComparisonResult.Identical => 0,
            ReplayComparisonResult.NotApplicable => 2,
            ReplayComparisonResult.Diverged => 3,
            _ => 2
        };
    }

    private static Dictionary<string, string> Parse(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index++)
        {
            var key = args[index];
            if (!key.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Invalid argument: {key}");
            }

            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for argument: {key}");
            }

            options[key[2..]] = args[++index];
        }

        return options;
    }

    private static string ResolveArtifactRoot(ReplayCapture capture)
    {
        var artifactPath = capture.Packets
            .Select(packet => packet.ArtifactPath)
            .Select(path => path.Replace('/', Path.DirectorySeparatorChar))
            .FirstOrDefault(path => path.Contains($"{Path.DirectorySeparatorChar}sprite-repair-batch-proposal{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(artifactPath))
        {
            return "";
        }

        var marker = $"{Path.DirectorySeparatorChar}sprite-repair-batch-proposal{Path.DirectorySeparatorChar}";
        var index = artifactPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return index <= 0 ? "" : artifactPath[..index];
    }

    private static string ResolveRepoRootFromArtifactRoot(string artifactRoot)
    {
        var full = Path.GetFullPath(string.IsNullOrWhiteSpace(artifactRoot) ? Directory.GetCurrentDirectory() : artifactRoot);
        var directory = new DirectoryInfo(full);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "wevito.godot")) ||
                Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static bool IsUnderVNextArtifacts(string fullPath)
    {
        var parts = Path.GetFullPath(fullPath).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        for (var index = 0; index < parts.Length - 1; index++)
        {
            if (string.Equals(parts[index], "vnext", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(parts[index + 1], "artifacts", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private sealed record ReplayCaptureDocument(
        string ScopeId,
        string OperationId,
        string Seed,
        DateTimeOffset OriginalAtUtc,
        IReadOnlyList<EvidencePacket>? Packets);
}
