using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.Sandbox;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Apply;
using Wevito.VNext.Tests.Support;

namespace Wevito.VNext.Tests;

public sealed class PostC183ApplyRunnerProductTruthTests
{
    private const string OperationId = "truth-op";
    private const string ScopeId = "truth-scope";
    private const string ScopeHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private const string ApprovalToken = "truth-token";

    private static readonly string[] ApplyV0PacketKinds =
    [
        SelfImprovementPacketKinds.ApplyV0DryRunStarted,
        SelfImprovementPacketKinds.ApplyV0DryRunCompleted,
        SelfImprovementPacketKinds.ApplyV0BackupWritten,
        SelfImprovementPacketKinds.ApplyV0Applied,
        SelfImprovementPacketKinds.ApplyV0PostProofCompleted,
        SelfImprovementPacketKinds.ApplyV0RolledBack,
        SelfImprovementPacketKinds.ApplyV0Completed
    ];

    private static readonly string[] ApplyV0CapabilityFlags =
    [
        ArtifactRenameApplyRunner.DesignApprovedSetting,
        ArtifactRenameApplyRunner.ImplementationPhaseApprovedSetting,
        ArtifactRenameApplyRunner.EnabledSetting,
        ArtifactRenameApplyRunner.DryRunRequiredSetting,
        ArtifactRenameApplyRunner.BackupRequiredSetting,
        ArtifactRenameApplyRunner.PostProofRequiredSetting,
        ArtifactRenameApplyRunner.RollbackRequiredSetting
    ];

    private static readonly string[] ForbiddenExtensions =
    [
        ".cs", ".csproj", ".xaml", ".config", ".exe", ".dll",
        ".png", ".jpg", ".jpeg", ".gif", ".ico", ".wav", ".ogg",
        ".ttf", ".otf", ".yaml", ".yml", ".toml", ".ini",
        ".bat", ".ps1", ".sh", ".py", ".js", ".ts", ".java",
        ".kt", ".swift", ".rs", ".go"
    ];

    [Fact]
    public void ProductTruth_artifact_rename_apply_runner_type_exists_in_apply_namespace()
    {
        Assert.Equal("Wevito.VNext.Core.SelfImprovement.Apply", typeof(ArtifactRenameApplyRunner).Namespace);
    }

    [Fact]
    public void ProductTruth_seven_apply_v0_packet_kinds_are_in_known_packet_kinds()
    {
        foreach (var packetKind in ApplyV0PacketKinds)
        {
            Assert.Contains(packetKind, PlainLanguageExplainer.KnownPacketKinds);
        }
    }

    [Fact]
    public void ProductTruth_seven_apply_v0_capability_flags_default_false()
    {
        foreach (var flag in ApplyV0CapabilityFlags)
        {
            AssertCapabilityFlagDefaultsFalse(flag);
        }
    }

    [Fact]
    public void ProductTruth_supervised_loop_apply_runner_not_implemented_reason_unchanged()
    {
        Assert.Equal("apply_runner_not_implemented_in_v0", SupervisedImprovementLoop.ApplyRunnerNotImplementedReason);
    }

    [Fact]
    public void ProductTruth_legacy_apply_completed_constant_unchanged()
    {
        Assert.Equal("self_improvement_apply_completed", SelfImprovementPacketKinds.ApplyCompleted);
    }

    [Fact]
    public void ProductTruth_runner_type_appears_in_held_out_eval_store_visibility_forbidden_types()
    {
        var source = ReadSource("vnext", "tests", "Wevito.VNext.Tests", "HeldOutEvalStoreVisibilityTests.cs");

        Assert.Contains("typeof(ArtifactRenameApplyRunner)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductTruth_runner_only_writes_under_artifact_root()
    {
        var source = ReadSource("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Apply", "ArtifactRenameApplyRunner.cs");

        Assert.DoesNotContain("Environment.SpecialFolder.MyDocuments", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Environment.SpecialFolder.Desktop", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Environment.SpecialFolder.System", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AppContext.BaseDirectory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Path.GetTempPath()", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductTruth_apply_runner_activity_service_type_exists()
    {
        Assert.Equal("Wevito.VNext.Core.SelfImprovement.Apply", typeof(ApplyRunnerActivityService).Namespace);
    }

    [Fact]
    public void ProductTruth_apply_runner_activity_service_uses_readonly_sqlite()
    {
        var source = ReadSource("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Apply", "ApplyRunnerActivityService.cs");

        Assert.Contains("Mode = SqliteOpenMode.ReadOnly", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductTruth_apply_runner_activity_service_no_insert_update_delete_sql()
    {
        var source = ReadSource("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Apply", "ApplyRunnerActivityService.cs");

        Assert.DoesNotContain("INSERT", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductTruth_artifact_rename_rollback_runner_exists_with_symmetric_path_constraints()
    {
        var source = ReadSource("vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Apply", "ArtifactRenameRollbackRunner.cs");

        Assert.Equal("Wevito.VNext.Core.SelfImprovement.Apply", typeof(ArtifactRenameRollbackRunner).Namespace);
        Assert.Contains(".approved.", source, StringComparison.Ordinal);
        Assert.Contains(".draft.", source, StringComparison.Ordinal);
        Assert.Contains("json|txt|md|svg", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductTruth_three_explicit_rollback_packet_kinds_in_known_kinds()
    {
        Assert.Contains(SelfImprovementPacketKinds.ApplyV0ExplicitRollbackStarted, PlainLanguageExplainer.KnownPacketKinds);
        Assert.Contains(SelfImprovementPacketKinds.ApplyV0ExplicitRollbackCompleted, PlainLanguageExplainer.KnownPacketKinds);
        Assert.Contains(SelfImprovementPacketKinds.ApplyV0ExplicitRollbackRefused, PlainLanguageExplainer.KnownPacketKinds);
    }

    [Fact]
    public void ProductTruth_two_explicit_rollback_capability_flags_default_false()
    {
        AssertCapabilityFlagDefaultsFalse(ArtifactRenameRollbackRunner.ExplicitRollbackEnabledSetting);
        AssertCapabilityFlagDefaultsFalse(ArtifactRenameRollbackRunner.ExplicitRollbackDesignApprovedSetting);
    }

    [Fact]
    public void ProductTruth_mutation_scope_guard_throws_invalid_operation_on_out_of_scope_path()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-product-truth-scope", Guid.NewGuid().ToString("N"), "root");
        var outside = Path.Combine(Path.GetTempPath(), "wevito-product-truth-scope", Guid.NewGuid().ToString("N"), "outside.txt");

        var ex = Assert.Throws<InvalidOperationException>(() => MutationScopeGuard.ThrowIfOutsideScope(outside, root, "artifact-root"));
        Assert.Contains("artifact-root", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductTruth_mutation_allow_list_contains_only_expected_eight_types()
    {
        Assert.Equal(
            [
                "ArtifactRenameApplyRunner",
                "ArtifactRenameRollbackRunner",
                "AuditLedgerService",
                "HeldOutEvalSeedTool",
                "InDistributionEvalSeedTool",
                "ReplayResultStore",
                "SnapshotExportService",
                "SnapshotVerifyTool"
            ],
            MutationAllowList.TypeNames.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ProductTruth_mutation_scope_audit_tests_pass_with_only_known_mutators()
    {
        var source = ReadSource("vnext", "tests", "Wevito.VNext.Tests", "MutationScopeAuditTests.cs");

        Assert.Contains("DiscoverMutatingCallers", source, StringComparison.Ordinal);
        Assert.Contains("nameof(ArtifactRenameApplyRunner)", source, StringComparison.Ordinal);
        Assert.Contains("nameof(ArtifactRenameRollbackRunner)", source, StringComparison.Ordinal);
        Assert.Contains("nameof(AuditLedgerService)", source, StringComparison.Ordinal);
        Assert.Contains(SelfImprovementPacketKinds.MutationScopeAuditEvent, PlainLanguageExplainer.KnownPacketKinds);
        AssertCapabilityFlagDefaultsFalse("mutation_scope_audit_emit_enabled");
    }

    [Fact]
    public void ProductTruth_extended_artifact_types_flag_default_false()
    {
        AssertCapabilityFlagDefaultsFalse(ArtifactRenameApplyRunner.ExtendedArtifactTypesEnabledSetting);
    }

    [Theory]
    [InlineData("txt")]
    [InlineData("md")]
    [InlineData("svg")]
    public void ProductTruth_runner_refuses_non_json_extension_when_flag_false(string extension)
    {
        var fixture = Fixture.Create(extension: extension);

        var result = fixture.Runner.Apply(fixture.Request(), CancellationToken.None);

        var refused = Assert.IsType<ApplyResult.Refused>(result);
        Assert.Equal($"flag_{ArtifactRenameApplyRunner.ExtendedArtifactTypesEnabledSetting}_not_true", refused.Reason);
    }

    [Theory]
    [InlineData("txt")]
    [InlineData("md")]
    [InlineData("svg")]
    public void ProductTruth_runner_accepts_txt_md_svg_when_flag_true(string extension)
    {
        var fixture = Fixture.Create(extension: extension, extendedFlag: true);
        var preHash = fixture.Hash(fixture.SourcePath);

        var result = fixture.Runner.Apply(fixture.Request(), CancellationToken.None);

        var succeeded = Assert.IsType<ApplyResult.Succeeded>(result);
        Assert.Equal($"{OperationId}/{ScopeId}/sample.approved.{extension}", succeeded.ApprovedRelativePath);
        Assert.Equal(preHash, succeeded.PostHashSha256);
        Assert.False(File.Exists(fixture.SourcePath));
        Assert.True(File.Exists(fixture.DestinationPath));
    }

    [Theory]
    [MemberData(nameof(ForbiddenExtensionValues))]
    public void ProductTruth_runner_refuses_each_forbidden_extension_unconditionally(string extension)
    {
        var fixture = Fixture.Create(writeSource: false, extendedFlag: true);

        var result = fixture.Runner.Apply(fixture.Request(relativePath: $"{OperationId}/{ScopeId}/sample.draft{extension}"), CancellationToken.None);

        var refused = Assert.IsType<ApplyResult.Refused>(result);
        Assert.Equal($"forbidden_extension:{extension}", refused.Reason);
    }

    public static IEnumerable<object[]> ForbiddenExtensionValues()
    {
        return ForbiddenExtensions.Select(extension => new object[] { extension });
    }

    private static void AssertCapabilityFlagDefaultsFalse(string name)
    {
        var entry = CapabilityFlagInventory.Entries.Single(entry => entry.Name == name);
        Assert.Equal(bool.FalseString, entry.DefaultValue);
    }

    private static string ReadSource(params string[] parts)
    {
        return File.ReadAllText(RepoPath(parts));
    }

    private static string RepoPath(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate {string.Join('/', parts)} from test output directory.");
    }

    private sealed class Fixture
    {
        private readonly string _extension;

        private Fixture(string extension)
        {
            _extension = extension;
            Root = Path.Combine(Path.GetTempPath(), "wevito-product-truth-apply", Guid.NewGuid().ToString("N"));
            ArtifactRoot = Path.Combine(Root, "artifacts");
            Ledger = new AuditLedgerService(Path.Combine(Root, "ledger.sqlite"));
            Settings = AllApplyFlagsTrue();
            KillSwitch = new KillSwitchService(() => Settings, Ledger);
            SourcePath = Path.Combine(ArtifactRoot, OperationId, ScopeId, $"sample.draft.{extension}");
            DestinationPath = Path.Combine(ArtifactRoot, OperationId, ScopeId, $"sample.approved.{extension}");
            Runner = new ArtifactRenameApplyRunner(
                Ledger,
                key => Settings.TryGetValue(key, out var value) ? value : null,
                KillSwitch,
                ArtifactRoot,
                operationId => new ApplyRunnerPrerequisiteCheckResult(
                    operationId,
                    [new PrerequisiteEntry("all", true, "ok")],
                    true,
                    DateTimeOffset.Parse("2026-05-20T00:00:00Z")),
                Hash,
                _ => DateTimeOffset.Parse("2026-05-20T00:00:00Z"));
        }

        public string Root { get; }

        public string ArtifactRoot { get; }

        public string SourcePath { get; }

        public string DestinationPath { get; }

        public AuditLedgerService Ledger { get; }

        public Dictionary<string, string> Settings { get; }

        public KillSwitchService KillSwitch { get; }

        public ArtifactRenameApplyRunner Runner { get; }

        public static Fixture Create(bool writeSource = true, string extension = "json", bool extendedFlag = false)
        {
            var fixture = new Fixture(extension);
            Directory.CreateDirectory(Path.GetDirectoryName(fixture.SourcePath)!);
            if (writeSource)
            {
                File.WriteAllText(fixture.SourcePath, "artifact");
            }

            if (extendedFlag)
            {
                fixture.Settings[ArtifactRenameApplyRunner.ExtendedArtifactTypesEnabledSetting] = bool.TrueString;
            }

            fixture.RecordAwaitingApproval();
            return fixture;
        }

        public ApplyRequest Request(string? relativePath = null)
        {
            return new ApplyRequest(
                OperationId,
                ScopeId,
                ScopeHash,
                relativePath ?? $"{OperationId}/{ScopeId}/sample.draft.{_extension}",
                ApprovalToken);
        }

        public string Hash(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream)).ToLowerInvariant();
        }

        private void RecordAwaitingApproval()
        {
            var approvalPath = Path.Combine(ArtifactRoot, OperationId, ScopeId, "apply-awaiting-approval.json");
            Directory.CreateDirectory(Path.GetDirectoryName(approvalPath)!);
            File.WriteAllText(approvalPath, $$"""
                {
                  "operationId": "{{OperationId}}",
                  "scopeId": "{{ScopeId}}",
                  "scopeHash": "{{ScopeHash}}",
                  "approvalToken": "{{ApprovalToken}}"
                }
                """);
            Ledger.Record(new EvidencePacket(
                Guid.NewGuid(),
                SelfImprovementPacketKinds.ApplyAwaitingApproval,
                Guid.NewGuid(),
                DateTimeOffset.Parse("2026-05-20T00:00:00Z"),
                DidUseNetwork: false,
                DidUseHostedAi: false,
                DidUseLocalModel: false,
                DidMutate: false,
                ArtifactPath: approvalPath,
                Summary: $$"""{"operation_id":"{{OperationId}}","scope_hash":"{{ScopeHash}}","approval_token":"{{ApprovalToken}}"}""",
                Status: "WaitingForApproval"));
        }

        private static Dictionary<string, string> AllApplyFlagsTrue()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ArtifactRenameApplyRunner.DesignApprovedSetting] = bool.TrueString,
                [ArtifactRenameApplyRunner.ImplementationPhaseApprovedSetting] = bool.TrueString,
                [ArtifactRenameApplyRunner.EnabledSetting] = bool.TrueString,
                [ArtifactRenameApplyRunner.DryRunRequiredSetting] = bool.TrueString,
                [ArtifactRenameApplyRunner.BackupRequiredSetting] = bool.TrueString,
                [ArtifactRenameApplyRunner.PostProofRequiredSetting] = bool.TrueString,
                [ArtifactRenameApplyRunner.RollbackRequiredSetting] = bool.TrueString
            };
        }
    }
}
