using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core.SelfImprovement.Experiments;

namespace Wevito.VNext.Core.SelfImprovement.Replay;

public sealed class ReplayHarness
{
    private readonly SpriteRepairBatchProposalScope _scope;
    private readonly KillSwitchService? _killSwitch;

    public ReplayHarness(SpriteRepairBatchProposalScope scope, KillSwitchService? killSwitch = null)
    {
        _scope = scope;
        _killSwitch = killSwitch;
    }

    public ReplayComparisonResult Replay(ReplayCapture capture)
    {
        if (_killSwitch?.IsActive() == true)
        {
            return new ReplayComparisonResult.NotApplicable("kill_switch=true");
        }

        if (!capture.ScopeId.Equals(_scope.Descriptor.ScopeId, StringComparison.OrdinalIgnoreCase))
        {
            return new ReplayComparisonResult.NotApplicable("scope_mismatch");
        }

        if (string.IsNullOrWhiteSpace(capture.Seed))
        {
            return new ReplayComparisonResult.NotApplicable("seed_missing");
        }

        var artifactRoot = ResolveArtifactRoot(capture.Packets);
        if (string.IsNullOrWhiteSpace(artifactRoot))
        {
            return new ReplayComparisonResult.NotApplicable("artifact_root_missing");
        }

        var packets = new List<EvidencePacket>();
        var request = new AutonomousScopeRunRequest(
            Settings(),
            new RuntimeSupervisorStatus(RuntimeSupervisorMode.Active, true, true, false, "active", ""),
            artifactRoot,
            capture.OriginalAtUtc,
            ExistingTaskCards: []);

        _scope.TryRun(request, capture.Seed, packets.Add);
        return Compare(capture.Packets, packets);
    }

    public void WriteResult(string path, ReplayComparisonResult result, ReplayCapture capture, DateTimeOffset replayedAtUtc)
    {
        if (_killSwitch?.IsActive() == true)
        {
            throw new InvalidOperationException("kill_switch=true");
        }

        var fullPath = Path.GetFullPath(path);
        if (!IsUnderVNextArtifacts(fullPath))
        {
            throw new InvalidOperationException("replay_result_outside_vnext_artifacts");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");
        File.WriteAllText(fullPath, JsonSerializer.Serialize(ToSummary(result, capture, replayedAtUtc), JsonDefaults.Options));
    }

    private static ReplayResultSummary ToSummary(ReplayComparisonResult result, ReplayCapture capture, DateTimeOffset replayedAtUtc)
    {
        return result switch
        {
            ReplayComparisonResult.Identical => new ReplayResultSummary(
                capture.OperationId,
                "Identical",
                0,
                replayedAtUtc,
                []),
            ReplayComparisonResult.Diverged diverged => new ReplayResultSummary(
                capture.OperationId,
                "Diverged",
                diverged.Diffs.Count,
                replayedAtUtc,
                diverged.Diffs.Take(10).ToArray()),
            ReplayComparisonResult.NotApplicable notApplicable => new ReplayResultSummary(
                capture.OperationId,
                "NotApplicable",
                0,
                replayedAtUtc,
                [notApplicable.Reason]),
            _ => new ReplayResultSummary(capture.OperationId, "NotApplicable", 0, replayedAtUtc, ["unknown_result"])
        };
    }

    private static ReplayComparisonResult Compare(IReadOnlyList<EvidencePacket> expected, IReadOnlyList<EvidencePacket> actual)
    {
        var diffs = new List<string>();
        if (expected.Count != actual.Count)
        {
            diffs.Add($"packet_count expected={expected.Count} actual={actual.Count}");
        }

        var count = Math.Min(expected.Count, actual.Count);
        for (var index = 0; index < count; index++)
        {
            var left = Redact(expected[index]);
            var right = Redact(actual[index]);
            if (!left.Equals(right, StringComparison.Ordinal))
            {
                diffs.Add($"packet[{index}] expected={left} actual={right}");
            }
        }

        return diffs.Count == 0
            ? new ReplayComparisonResult.Identical(actual.Count)
            : new ReplayComparisonResult.Diverged(diffs);
    }

    private static string Redact(EvidencePacket packet)
    {
        return JsonSerializer.Serialize(new
        {
            packet.PacketKind,
            packet.DidUseNetwork,
            packet.DidUseHostedAi,
            packet.DidUseLocalModel,
            packet.DidMutate,
            packet.ArtifactPath,
            packet.Summary,
            packet.Status,
            packet.Error
        }, JsonDefaults.Options);
    }

    private static IReadOnlyDictionary<string, string> Settings()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [AutonomousOperationsConfig.EnabledSetting] = bool.TrueString,
            [AutonomousOperationsConfig.DailyCapSetting] = "3",
            [AutonomousOperationsConfig.IntervalMinutesSetting] = "10",
            [RuntimeSupervisorService.BackgroundWorkAllowedSetting] = bool.TrueString,
            [AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairBatchProposalScopeId)] = bool.TrueString
        };
    }

    private static string ResolveArtifactRoot(IReadOnlyList<EvidencePacket> packets)
    {
        var artifactPath = packets
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

    private static bool IsUnderVNextArtifacts(string fullPath)
    {
        var parts = fullPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
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
}
