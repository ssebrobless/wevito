using System.Reflection;
using System.Text.RegularExpressions;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Apply;
using Wevito.VNext.Core.SelfImprovement.Invariants;

namespace Wevito.VNext.Tests;

public sealed class PostC191WatchdogObserverProductTruthTests
{
    private const string ObserverFlagName = "apply_v0_invariant_observer_in_activity_service_enabled";
    private const int C191KnownPacketKindsCount = 160;

    [Fact]
    public void ProductTruth_watchdog_scan_and_emit_method_exists_and_is_public()
    {
        var method = typeof(InvariantViolationWatchdog).GetMethod(
            "ScanAndEmit",
            BindingFlags.Public | BindingFlags.Instance,
            [typeof(DateTimeOffset)]);

        Assert.NotNull(method);
        Assert.Equal(typeof(IReadOnlyList<InvariantCheckResult>), method!.ReturnType);
        var parameter = Assert.Single(method.GetParameters());
        Assert.Equal("nowUtc", parameter.Name);
        Assert.Equal(typeof(DateTimeOffset), parameter.ParameterType);
    }

    [Fact]
    public void ProductTruth_watchdog_scan_and_emit_body_references_scan_and_emit_in_source()
    {
        var body = ExtractMethodBody(WatchdogSource(), "public IReadOnlyList<InvariantCheckResult> ScanAndEmit");

        Assert.Contains("Scan(nowUtc)", body, StringComparison.Ordinal);
        Assert.Contains("EmitInvariantCheckFailedPackets(results, nowUtc)", body, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductTruth_apply_v0_invariant_observer_flag_exists_default_false()
    {
        var entry = CapabilityFlagInventory.Entries.Single(entry => entry.Name == ObserverFlagName);

        Assert.Equal(bool.FalseString, entry.DefaultValue);
    }

    [Fact]
    public void ProductTruth_apply_runner_activity_service_ctor_accepts_optional_watchdog()
    {
        var ctor = typeof(ApplyRunnerActivityService)
            .GetConstructors()
            .Single(candidate => candidate.GetParameters().Any(parameter => parameter.Name == "watchdog"));
        var watchdogParameter = ctor.GetParameters().Single(parameter => parameter.Name == "watchdog");

        Assert.Equal(typeof(InvariantViolationWatchdog), watchdogParameter.ParameterType);
        Assert.True(watchdogParameter.HasDefaultValue);
        Assert.Null(watchdogParameter.DefaultValue);
    }

    [Fact]
    public void ProductTruth_apply_runner_activity_service_does_not_call_scan_or_emit_directly_in_source()
    {
        var source = ActivitySource();

        Assert.DoesNotMatch(new Regex(@"_watchdog\s*\??\s*\.\s*Scan\s*\(", RegexOptions.CultureInvariant), source);
        Assert.DoesNotContain("EmitInvariantCheckFailedPackets(", source, StringComparison.Ordinal);
        Assert.Contains("_watchdog.ScanAndEmit(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductTruth_only_emit_invariant_check_failed_packets_caller_in_src_and_tools_is_watchdog_itself()
    {
        var watchdogPath = WatchdogSourcePath();
        var callers = SourceAndToolFiles()
            .Where(path => File.ReadAllText(path).Contains("EmitInvariantCheckFailedPackets(", StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.All(callers, path => Assert.Equal(watchdogPath, path, ignoreCase: true));
    }

    [Fact]
    public void ProductTruth_only_scan_and_emit_callers_in_src_and_tools_are_watchdog_definition_and_activity_service()
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            WatchdogSourcePath(),
            ActivitySourcePath()
        };
        var callers = SourceAndToolFiles()
            .Where(path => File.ReadAllText(path).Contains("ScanAndEmit(", StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.NotEmpty(callers);
        Assert.All(callers, path => Assert.Contains(path, allowed));
    }

    [Fact]
    public void ProductTruth_post_c189_watchdog_product_truth_file_still_has_fifteen_facts()
    {
        Assert.Equal(15, CountFacts(PostC189ProductTruthPath()));
    }

    [Fact]
    public void ProductTruth_post_c189_refined_fact_name_still_present()
    {
        var source = File.ReadAllText(PostC189ProductTruthPath());

        Assert.Contains(
            "ProductTruth_watchdog_only_allowed_external_facade_producer_is_activity_service_under_flag",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ProductTruth_legacy_apply_runner_not_implemented_reason_unchanged()
    {
        var source = File.ReadAllText(RepoPath("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "SupervisedImprovementLoop.cs"));

        Assert.Contains("apply_runner_not_implemented_in_v0", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductTruth_legacy_apply_completed_packet_kind_constant_unchanged()
    {
        var source = File.ReadAllText(RepoPath("vnext", "src", "Wevito.VNext.Core", "Audit", "SelfImprovementPacketKinds.cs"));

        Assert.Contains("self_improvement_apply_completed", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductTruth_known_packet_kinds_count_unchanged_by_c191()
    {
        Assert.Equal(C191KnownPacketKindsCount, PlainLanguageExplainer.KnownPacketKinds.Count);
    }

    [Fact]
    public void ProductTruth_audit_ledger_service_remains_append_only_no_update_delete_drop_sql()
    {
        var source = File.ReadAllText(AuditLedgerPath());
        var forbiddenStatements = Regex.Matches(
            source,
            @"(?im)^\s*(UPDATE\b|DELETE\s+FROM\b|DROP\s+TABLE\b)");

        Assert.Empty(forbiddenStatements);
    }

    [Fact]
    public void ProductTruth_apply_runner_activity_service_observer_wiring_tests_file_present_with_eight_facts()
    {
        var path = RepoPath("vnext", "tests", "Wevito.VNext.Tests", "ApplyRunnerActivityServiceObserverWiringTests.cs");

        Assert.True(File.Exists(path));
        Assert.Equal(8, CountFacts(path));
    }

    [Fact]
    public void ProductTruth_invariant_violation_watchdog_scan_and_emit_v0_tests_file_present_with_three_facts()
    {
        var path = RepoPath("vnext", "tests", "Wevito.VNext.Tests", "InvariantViolationWatchdogScanAndEmitV0Tests.cs");

        Assert.True(File.Exists(path));
        Assert.Equal(3, CountFacts(path));
    }

    [Fact]
    public void ProductTruth_watchdog_and_activity_service_both_reference_killswitch_in_source()
    {
        // KillSwitch is a centralized service queried by consumers; runtime obedience is covered by the C-189/C-191 behavior tests.
        Assert.Contains("KillSwitch", WatchdogSource(), StringComparison.Ordinal);
        Assert.Contains("KillSwitch", ActivitySource(), StringComparison.Ordinal);
    }

    private static string WatchdogSource()
    {
        return File.ReadAllText(WatchdogSourcePath());
    }

    private static string ActivitySource()
    {
        return File.ReadAllText(ActivitySourcePath());
    }

    private static string WatchdogSourcePath()
    {
        return RepoPath("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Invariants", "InvariantViolationWatchdog.cs");
    }

    private static string ActivitySourcePath()
    {
        return RepoPath("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Apply", "ApplyRunnerActivityService.cs");
    }

    private static string PostC189ProductTruthPath()
    {
        return RepoPath("vnext", "tests", "Wevito.VNext.Tests", "PostC189WatchdogProductTruthTests.cs");
    }

    private static string AuditLedgerPath()
    {
        return RepoPath("vnext", "src", "Wevito.VNext.Core", "AuditLedgerService.cs");
    }

    private static int CountFacts(string path)
    {
        return Regex.Matches(File.ReadAllText(path), @"\[Fact\]", RegexOptions.CultureInvariant).Count;
    }

    private static IEnumerable<string> SourceAndToolFiles()
    {
        return Directory.EnumerateFiles(RepoPath("vnext", "src"), "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(RepoPath("vnext", "tools"), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Contains("bin", StringComparer.OrdinalIgnoreCase))
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Contains("obj", StringComparer.OrdinalIgnoreCase));
    }

    private static string ExtractMethodBody(string source, string methodSignaturePrefix)
    {
        var signatureStart = source.IndexOf(methodSignaturePrefix, StringComparison.Ordinal);
        Assert.True(signatureStart >= 0, $"Missing method signature prefix: {methodSignaturePrefix}");
        var openBrace = source.IndexOf('{', signatureStart);
        Assert.True(openBrace >= 0, "Missing method opening brace.");

        var depth = 0;
        for (var index = openBrace; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(openBrace, index - openBrace + 1);
                }
            }
        }

        throw new InvalidOperationException("Could not find method closing brace.");
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
