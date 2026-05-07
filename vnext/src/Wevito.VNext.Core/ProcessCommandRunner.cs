using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class ProcessCommandRunner : ICommandRunner
{
    private static readonly string[] ProtectedRoots =
    [
        Path.Combine("vnext", "src"),
        Path.Combine("vnext", "tests"),
        "tools",
        "scripts",
        "sprites_runtime",
        "sprites_shared_runtime",
        "sprites_authored",
        "sprites_authored_runtime",
        Path.Combine("vnext", "content")
    ];

    private static readonly string[] IgnoredDirectoryNames =
    [
        "bin",
        "obj",
        ".git",
        ".vs",
        "artifacts",
        "TestResults"
    ];

    public async Task<ProofExecutionResult> RunAsync(ProofExecutionRequest request, CancellationToken cancellationToken = default)
    {
        var started = DateTimeOffset.UtcNow;
        var artifactRoot = Path.GetFullPath(request.ArtifactRoot);
        Directory.CreateDirectory(artifactRoot);

        var stdoutPath = Path.Combine(artifactRoot, "stdout.txt");
        var stderrPath = Path.Combine(artifactRoot, "stderr.txt");
        var mergedLogPath = Path.Combine(artifactRoot, "merged.log");
        var manifestPath = Path.Combine(artifactRoot, "proof-execution-manifest.json");
        var workingDirectory = ResolveWorkingDirectory(request.Command.WorkingDirectory);

        var preRunHashes = BuildProtectedHashSnapshot(workingDirectory);
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        int? exitCode = null;
        var status = ProofExecutionResultStatus.Failed;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = request.Command.Executable,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var argument in request.Command.Arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            foreach (var pair in request.EnvironmentOverrides)
            {
                startInfo.Environment[pair.Key] = pair.Value;
            }

            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            if (!process.Start())
            {
                status = ProofExecutionResultStatus.Failed;
                stderr.AppendLine("Process could not be started.");
            }
            else
            {
                var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
                var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
                using var timeoutCts = new CancellationTokenSource(request.Command.Timeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                try
                {
                    await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
                    exitCode = process.ExitCode;
                    stdout.Append(await stdoutTask.ConfigureAwait(false));
                    stderr.Append(await stderrTask.ConfigureAwait(false));
                    status = exitCode == 0 ? ProofExecutionResultStatus.Succeeded : ProofExecutionResultStatus.Failed;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    KillProcessTree(process);
                    status = ProofExecutionResultStatus.Cancelled;
                    stderr.AppendLine("Command was cancelled.");
                }
                catch (OperationCanceledException)
                {
                    KillProcessTree(process);
                    status = ProofExecutionResultStatus.TimedOut;
                    stderr.AppendLine($"Command timed out after {request.Command.Timeout}.");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            status = ProofExecutionResultStatus.Cancelled;
            stderr.AppendLine("Command was cancelled before startup completed.");
        }
        catch (Exception exception)
        {
            status = ProofExecutionResultStatus.Failed;
            stderr.AppendLine(exception.ToString());
        }

        var postRunHashes = BuildProtectedHashSnapshot(workingDirectory);
        var mutation = DetectMutation(preRunHashes, postRunHashes);
        if (mutation.MutationDetected)
        {
            status = ProofExecutionResultStatus.MutationDetected;
        }

        var finished = DateTimeOffset.UtcNow;
        await File.WriteAllTextAsync(stdoutPath, stdout.ToString(), CancellationToken.None).ConfigureAwait(false);
        await File.WriteAllTextAsync(stderrPath, stderr.ToString(), CancellationToken.None).ConfigureAwait(false);
        await File.WriteAllTextAsync(
            mergedLogPath,
            BuildMergedLog(request, status, exitCode, stdout.ToString(), stderr.ToString(), mutation),
            CancellationToken.None).ConfigureAwait(false);

        var manifest = new ProofExecutionManifest(
            "1",
            request.TaskCardId,
            "buildProof",
            request.Command,
            artifactRoot,
            stdoutPath,
            stderrPath,
            mergedLogPath,
            request.EnvironmentOverrides,
            preRunHashes,
            postRunHashes,
            mutation.DidMutateCode,
            mutation.DidMutateAssets,
            DidRunAssetPrep: request.Command.Arguments.Any(argument => argument.Contains("asset-prep", StringComparison.OrdinalIgnoreCase) ||
                                                                        argument.Contains("prep-sprites", StringComparison.OrdinalIgnoreCase)),
            mutation.MutationDetected,
            started,
            finished,
            status,
            exitCode);
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, JsonDefaults.Options), CancellationToken.None).ConfigureAwait(false);

        return new ProofExecutionResult(
            request.TaskCardId,
            request.CommandId,
            status,
            exitCode,
            stdoutPath,
            stderrPath,
            mergedLogPath,
            manifestPath,
            mutation.MutationDetected,
            BuildSummary(status, mutation),
            started,
            finished);
    }

    private static string ResolveWorkingDirectory(string workingDirectory)
    {
        if (!string.IsNullOrWhiteSpace(workingDirectory) && !string.Equals(workingDirectory, ".", StringComparison.Ordinal))
        {
            return Path.GetFullPath(workingDirectory);
        }

        return FindRepoRoot(Directory.GetCurrentDirectory()) ??
               FindRepoRoot(AppContext.BaseDirectory) ??
               Path.GetFullPath(".");
    }

    private static string? FindRepoRoot(string startPath)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startPath));
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "vnext")) &&
                File.Exists(Path.Combine(directory.FullName, "vnext", "Wevito.VNext.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static IReadOnlyDictionary<string, string> BuildProtectedHashSnapshot(string repoRoot)
    {
        var hashes = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in ProtectedRoots)
        {
            var fullRoot = Path.Combine(repoRoot, root);
            if (!Directory.Exists(fullRoot))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(fullRoot, "*", SearchOption.AllDirectories))
            {
                if (ShouldIgnoreFile(repoRoot, file))
                {
                    continue;
                }

                var relativePath = Path.GetRelativePath(repoRoot, file).Replace(Path.DirectorySeparatorChar, '/');
                hashes[relativePath] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))).ToLowerInvariant();
            }
        }

        return hashes;
    }

    private static bool ShouldIgnoreFile(string repoRoot, string file)
    {
        var relativeParts = Path.GetRelativePath(repoRoot, file).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return relativeParts.Any(part => IgnoredDirectoryNames.Any(ignored => string.Equals(part, ignored, StringComparison.OrdinalIgnoreCase)));
    }

    private static ProtectedMutationSummary DetectMutation(
        IReadOnlyDictionary<string, string> preRunHashes,
        IReadOnlyDictionary<string, string> postRunHashes)
    {
        var changedPaths = preRunHashes
            .Where(pair => !postRunHashes.TryGetValue(pair.Key, out var after) || !string.Equals(pair.Value, after, StringComparison.OrdinalIgnoreCase))
            .Select(pair => pair.Key)
            .Concat(postRunHashes.Keys.Where(path => !preRunHashes.ContainsKey(path)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var didMutateAssets = changedPaths.Any(path => path.StartsWith("sprites_", StringComparison.OrdinalIgnoreCase));
        var didMutateCode = changedPaths.Any(path => !path.StartsWith("sprites_", StringComparison.OrdinalIgnoreCase));
        return new ProtectedMutationSummary(changedPaths.Count > 0, didMutateCode, didMutateAssets, changedPaths);
    }

    private static void KillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static string BuildMergedLog(
        ProofExecutionRequest request,
        ProofExecutionResultStatus status,
        int? exitCode,
        string stdout,
        string stderr,
        ProtectedMutationSummary mutation)
    {
        var lines = new List<string>
        {
            $"commandId: {request.CommandId}",
            $"command: {request.Command.Executable} {string.Join(" ", request.Command.Arguments)}",
            $"status: {status}",
            $"exitCode: {exitCode?.ToString() ?? "n/a"}",
            $"mutationDetected: {mutation.MutationDetected}",
            ""
        };

        if (mutation.MutationDetected)
        {
            lines.Add("mutatedProtectedPaths:");
            lines.AddRange(mutation.ChangedPaths.Select(path => $"- {path}"));
            lines.Add("");
        }

        lines.Add("## stdout");
        lines.Add(stdout);
        lines.Add("## stderr");
        lines.Add(stderr);
        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildSummary(ProofExecutionResultStatus status, ProtectedMutationSummary mutation)
    {
        if (mutation.MutationDetected)
        {
            return $"Command stopped with protected file mutation detected ({mutation.ChangedPaths.Count} path(s)).";
        }

        return status switch
        {
            ProofExecutionResultStatus.Succeeded => "Command completed successfully with no protected file mutation.",
            ProofExecutionResultStatus.Cancelled => "Command was cancelled; no protected file mutation was detected.",
            ProofExecutionResultStatus.TimedOut => "Command timed out; no protected file mutation was detected.",
            _ => "Command finished without protected file mutation, but did not succeed."
        };
    }

    private sealed record ProtectedMutationSummary(
        bool MutationDetected,
        bool DidMutateCode,
        bool DidMutateAssets,
        IReadOnlyList<string> ChangedPaths);
}
