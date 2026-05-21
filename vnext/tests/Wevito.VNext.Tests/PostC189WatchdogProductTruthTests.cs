using System.Reflection;
using System.Text.RegularExpressions;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Invariants;

namespace Wevito.VNext.Tests;

public sealed class PostC189WatchdogProductTruthTests
{
    private const string WatchdogTypeName = "Wevito.VNext.Core.SelfImprovement.Invariants.InvariantViolationWatchdog";
    private const string C189PacketKindValue = "self_improvement_apply_v0_invariant_check_failed";
    private const string C189FlagName = "apply_v0_invariant_check_emit_enabled";
    private const string C189PlainLanguageSentence = "Wevito recorded a self-improvement apply-v0 sequence invariant violation found by the watchdog.";
    private const string AuditLedgerModePin = "append-only-record-plus-readonly-sqlite";
    private const string KillSwitchModePin = "accepts-KillSwitchService";

    [Fact]
    public void ProductTruth_invariant_watchdog_type_exists_in_invariants_namespace()
    {
        var type = Type.GetType($"{WatchdogTypeName}, Wevito.VNext.Core", throwOnError: true);

        Assert.Equal("Wevito.VNext.Core.SelfImprovement.Invariants", type!.Namespace);
        Assert.Equal(typeof(InvariantViolationWatchdog), type);
    }

    [Fact]
    public void ProductTruth_new_c189_packet_kind_string_value_unchanged()
    {
        var field = typeof(SelfImprovementPacketKinds).GetField(
            nameof(SelfImprovementPacketKinds.ApplyV0InvariantCheckFailed),
            BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(field);
        Assert.Equal(C189PacketKindValue, field!.GetRawConstantValue());
        Assert.Equal(C189PacketKindValue, SelfImprovementPacketKinds.ApplyV0InvariantCheckFailed);
    }

    [Fact]
    public void ProductTruth_new_c189_packet_kind_registered_in_plain_language_explainer()
    {
        Assert.Contains(SelfImprovementPacketKinds.ApplyV0InvariantCheckFailed, PlainLanguageExplainer.KnownPacketKinds);
    }

    [Fact]
    public void ProductTruth_new_c189_packet_kind_has_nonempty_plain_language_sentence()
    {
        var sentence = new PlainLanguageExplainer().ExplainPacketKind(SelfImprovementPacketKinds.ApplyV0InvariantCheckFailed).Trim();

        Assert.Equal(C189PlainLanguageSentence, sentence);
        Assert.NotEmpty(sentence);
        Assert.Contains(sentence[^1], ['.', '!', '?']);
    }

    [Fact]
    public void ProductTruth_new_c189_capability_flag_default_false()
    {
        var entry = CapabilityFlagInventory.Entries.Single(entry => entry.Name == C189FlagName);

        Assert.Equal(bool.FalseString, entry.DefaultValue);
    }

    [Fact]
    public void ProductTruth_new_c189_capability_flag_not_flipped_by_any_producer()
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            RepoPath("vnext", "src", "Wevito.VNext.Core", "Audit", "CapabilityFlagInventory.cs"),
            WatchdogSourcePath()
        };
        var forbiddenPatterns = new[]
        {
            "= true",
            "= \"True\"",
            "bool.TrueString",
            ".Set(",
            ".Enable(",
            "Override(",
            "[..]=true"
        };

        foreach (var path in SourceFiles().Where(path => !allowed.Contains(path)))
        {
            var source = File.ReadAllText(path);
            foreach (Match match in Regex.Matches(source, Regex.Escape(C189FlagName), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                var start = Math.Max(0, match.Index - 200);
                var length = Math.Min(source.Length - start, match.Length + 400);
                var window = source.Substring(start, length);
                foreach (var pattern in forbiddenPatterns)
                {
                    Assert.DoesNotContain(pattern, window, StringComparison.OrdinalIgnoreCase);
                }

                Assert.DoesNotMatch(new Regex(@"TryAdd\s*\([^)]*true", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), window);
            }
        }
    }

    [Fact]
    public void ProductTruth_supervised_loop_apply_runner_not_implemented_reason_unchanged()
    {
        var field = typeof(SupervisedImprovementLoop).GetField(
            nameof(SupervisedImprovementLoop.ApplyRunnerNotImplementedReason),
            BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(field);
        Assert.Equal("apply_runner_not_implemented_in_v0", field!.GetRawConstantValue());
        Assert.Equal("apply_runner_not_implemented_in_v0", SupervisedImprovementLoop.ApplyRunnerNotImplementedReason);
    }

    [Fact]
    public void ProductTruth_legacy_apply_completed_constant_unchanged()
    {
        Assert.Equal("self_improvement_apply_completed", SelfImprovementPacketKinds.ApplyCompleted);
    }

    [Fact]
    public void ProductTruth_watchdog_source_has_no_mutating_sql()
    {
        var source = WatchdogSource();

        Assert.DoesNotContain("INSERT INTO", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE ", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ALTER TABLE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TRUNCATE", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductTruth_watchdog_source_has_no_network_imports()
    {
        var source = WatchdogSource();

        Assert.DoesNotContain("using System.Net.Http", source, StringComparison.Ordinal);
        Assert.DoesNotContain("using System.Net.Sockets", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductTruth_watchdog_source_has_no_held_out_or_in_distribution_eval_references()
    {
        var source = WatchdogSource();

        Assert.DoesNotContain("Held" + "Out", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("In" + "Distribution", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Eval" + "Store", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductTruth_watchdog_source_has_no_ui_pet_or_sprite_references()
    {
        var source = WatchdogSource();

        Assert.DoesNotContain("Wevito.VNext.Shell", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Wevito.VNext.UI", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Wevito.VNext.Pet", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Wevito.VNext.Sprite", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductTruth_watchdog_only_allowed_external_facade_producer_is_activity_service_under_flag()
    {
        var watchdogPath = WatchdogSourcePath();
        var sourceFiles = SourceFiles().ToArray();
        var externalEmitCallers = sourceFiles
            .Where(path => !path.Equals(watchdogPath, StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("EmitInvariantCheckFailedPackets(", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(externalEmitCallers);

        var activityPath = RepoPath("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Apply", "ApplyRunnerActivityService.cs");
        var externalFacadeCallers = sourceFiles
            .Where(path => !path.Equals(watchdogPath, StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => Regex.Matches(File.ReadAllText(path), @"\.ScanAndEmit\s*\(", RegexOptions.CultureInvariant)
                .Select(match => new { Path = path, Match = match, Source = File.ReadAllText(path) }))
            .ToArray();

        var caller = Assert.Single(externalFacadeCallers);
        Assert.Equal(activityPath, caller.Path, ignoreCase: true);

        var methodStart = caller.Source.LastIndexOf("public IReadOnlyList<ApplyRunnerActivityEntry> ReadRecent", caller.Match.Index, StringComparison.Ordinal);
        var guardStart = caller.Source.LastIndexOf("IsTrue(_settingsProvider(), ObserverEnabledSetting)", caller.Match.Index, StringComparison.Ordinal);
        Assert.True(methodStart >= 0, "The allowed facade call must be inside ReadRecent.");
        Assert.True(guardStart > methodStart, "The allowed facade call must be guarded by the observer flag.");
        Assert.True(caller.Match.Index - guardStart <= 200, "The observer flag guard must be near the facade call.");

        var externalDirectScanCallers = sourceFiles
            .Where(path => !path.Equals(watchdogPath, StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith(Path.Combine("Wevito.VNext.Shell", "ShellCoordinator.cs"), StringComparison.OrdinalIgnoreCase))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\.Scan\s*\(", RegexOptions.CultureInvariant))
            .ToArray();
        Assert.Empty(externalDirectScanCallers);
    }

    [Fact]
    public void ProductTruth_watchdog_audit_ledger_usage_pinned_to_append_only_or_absent()
    {
        const string expectedMode = AuditLedgerModePin;
        var source = WatchdogSource();

        Assert.Equal("append-only-record-plus-readonly-sqlite", expectedMode);
        Assert.Contains("AuditLedgerService?", source, StringComparison.Ordinal);
        Assert.Contains("_auditLedgerService.Record(new EvidencePacket(", source, StringComparison.Ordinal);
        Assert.Contains("Mode = SqliteOpenMode.ReadOnly", source, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT INTO", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE ", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductTruth_watchdog_kill_switch_behavior_pinned()
    {
        const string expectedMode = KillSwitchModePin;
        var source = WatchdogSource();

        Assert.Equal("accepts-KillSwitchService", expectedMode);
        Assert.Contains("KillSwitchService?", source, StringComparison.Ordinal);
        Assert.Contains("_killSwitchService?.IsActive() == true", source, StringComparison.Ordinal);
    }

    private static string WatchdogSource()
    {
        return File.ReadAllText(WatchdogSourcePath());
    }

    private static string WatchdogSourcePath()
    {
        return RepoPath("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Invariants", "InvariantViolationWatchdog.cs");
    }

    private static IEnumerable<string> SourceFiles()
    {
        return Directory.EnumerateFiles(RepoPath("vnext", "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Contains("bin", StringComparer.OrdinalIgnoreCase))
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Contains("obj", StringComparer.OrdinalIgnoreCase));
    }

    private static string RepoPath(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate {string.Join('/', parts)} from test output directory.");
    }
}
