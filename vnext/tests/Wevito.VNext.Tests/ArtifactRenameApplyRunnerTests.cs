using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Apply;

namespace Wevito.VNext.Tests;

public sealed class ArtifactRenameApplyRunnerTests
{
    private const string OperationId = "op-123";
    private const string ScopeId = "scope-abc";
    private const string ScopeHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private const string Token = "approve-op-123";
    private const string DraftRelativePath = $"{OperationId}/{ScopeId}/sample.draft.json";
    private static readonly string[] ForbiddenExtensions =
    [
        ".cs", ".csproj", ".xaml", ".config", ".exe", ".dll",
        ".png", ".jpg", ".jpeg", ".gif", ".ico", ".wav", ".ogg",
        ".ttf", ".otf", ".yaml", ".yml", ".toml", ".ini",
        ".bat", ".ps1", ".sh", ".py", ".js", ".ts", ".java",
        ".kt", ".swift", ".rs", ".go"
    ];

    [Fact]
    public void Apply_refuses_when_v0_enabled_flag_false_writes_no_packet() => AssertFlagRefusal(ArtifactRenameApplyRunner.EnabledSetting);

    [Fact]
    public void Apply_refuses_when_design_approved_flag_false_writes_no_packet() => AssertFlagRefusal(ArtifactRenameApplyRunner.DesignApprovedSetting);

    [Fact]
    public void Apply_refuses_when_implementation_phase_approved_flag_false_writes_no_packet() => AssertFlagRefusal(ArtifactRenameApplyRunner.ImplementationPhaseApprovedSetting);

    [Fact]
    public void Apply_refuses_when_dry_run_required_flag_false_writes_no_packet() => AssertFlagRefusal(ArtifactRenameApplyRunner.DryRunRequiredSetting);

    [Fact]
    public void Apply_refuses_when_backup_required_flag_false_writes_no_packet() => AssertFlagRefusal(ArtifactRenameApplyRunner.BackupRequiredSetting);

    [Fact]
    public void Apply_refuses_when_post_proof_required_flag_false_writes_no_packet() => AssertFlagRefusal(ArtifactRenameApplyRunner.PostProofRequiredSetting);

    [Fact]
    public void Apply_refuses_when_rollback_required_flag_false_writes_no_packet() => AssertFlagRefusal(ArtifactRenameApplyRunner.RollbackRequiredSetting);

    [Fact]
    public void Extended_artifact_types_flag_defaults_false()
    {
        var entry = CapabilityFlagInventory.Entries.Single(entry => entry.Name == ArtifactRenameApplyRunner.ExtendedArtifactTypesEnabledSetting);

        Assert.Equal(bool.FalseString, entry.DefaultValue);
    }

    [Fact]
    public void Apply_refuses_when_kill_switch_active_writes_no_packet()
    {
        var fixture = Fixture.Create();
        fixture.Settings[KillSwitchService.KillSwitchSetting] = bool.TrueString;

        var result = fixture.Runner.Apply(fixture.Request(), CancellationToken.None);

        var refused = Assert.IsType<ApplyResult.Refused>(result);
        Assert.Equal("kill_switch_active", refused.Reason);
        Assert.Empty(fixture.ApplyRows());
    }

    [Fact]
    public void Apply_refuses_when_prerequisite_check_returns_any_failed_entry()
    {
        var fixture = Fixture.Create(prereqPassed: false);

        var result = fixture.Runner.Apply(fixture.Request(), CancellationToken.None);

        var refused = Assert.IsType<ApplyResult.Refused>(result);
        Assert.StartsWith("prerequisite_check_failed:", refused.Reason, StringComparison.Ordinal);
        Assert.Empty(fixture.ApplyRows());
    }

    [Fact]
    public void Apply_refuses_when_draft_relative_path_contains_double_dot() => AssertInvalidPath($"{OperationId}/{ScopeId}/../sample.draft.json");

    [Fact]
    public void Apply_refuses_when_draft_relative_path_is_absolute() => AssertInvalidPath("/absolute/sample.draft.json");

    [Fact]
    public void Apply_refuses_when_draft_relative_path_contains_backslash() => AssertInvalidPath($@"{OperationId}\{ScopeId}\sample.draft.json");

    [Fact]
    public void Apply_refuses_when_draft_relative_path_has_unsupported_extension() => AssertInvalidPath($"{OperationId}/{ScopeId}/sample.draft.html");

    [Theory]
    [InlineData("txt")]
    [InlineData("md")]
    [InlineData("svg")]
    public void Apply_refuses_non_json_extension_when_extended_flag_false(string extension)
    {
        var fixture = Fixture.Create(extension: extension);

        var result = fixture.Runner.Apply(fixture.Request(), CancellationToken.None);

        Assert.Equal($"flag_{ArtifactRenameApplyRunner.ExtendedArtifactTypesEnabledSetting}_not_true", Assert.IsType<ApplyResult.Refused>(result).Reason);
        Assert.Empty(fixture.ApplyRows());
    }

    [Theory]
    [InlineData("txt")]
    [InlineData("md")]
    [InlineData("svg")]
    public void Apply_accepts_extended_artifact_extension_when_flag_true(string extension)
    {
        var fixture = Fixture.Create(extension: extension);
        fixture.Settings[ArtifactRenameApplyRunner.ExtendedArtifactTypesEnabledSetting] = bool.TrueString;
        var preHash = fixture.Hash(fixture.SourcePath);

        var result = fixture.Runner.Apply(fixture.Request(), CancellationToken.None);

        var succeeded = Assert.IsType<ApplyResult.Succeeded>(result);
        Assert.Equal($"{OperationId}/{ScopeId}/sample.approved.{extension}", succeeded.ApprovedRelativePath);
        Assert.Equal(preHash, succeeded.PostHashSha256);
        Assert.False(File.Exists(fixture.SourcePath));
        Assert.True(File.Exists(fixture.DestinationPath));
        Assert.Equal(preHash, fixture.Hash(fixture.DestinationPath));
    }

    [Theory]
    [MemberData(nameof(ForbiddenExtensionValues))]
    public void Apply_refuses_forbidden_extensions_even_when_extended_flag_true(string extension)
    {
        var fixture = Fixture.Create(writeSource: false);
        fixture.Settings[ArtifactRenameApplyRunner.ExtendedArtifactTypesEnabledSetting] = bool.TrueString;

        var result = fixture.Runner.Apply(fixture.Request(relativePath: $"{OperationId}/{ScopeId}/sample.draft{extension}"), CancellationToken.None);

        Assert.Equal($"forbidden_extension:{extension}", Assert.IsType<ApplyResult.Refused>(result).Reason);
        Assert.Empty(fixture.ApplyRows());
    }

    [Fact]
    public void Apply_refuses_when_scope_id_segment_mismatches_request()
    {
        var fixture = Fixture.Create();
        var result = fixture.Runner.Apply(fixture.Request(scopeId: "wrong-scope"), CancellationToken.None);
        Assert.Equal("scope_id_mismatch", Assert.IsType<ApplyResult.Refused>(result).Reason);
    }

    [Fact]
    public void Apply_refuses_when_operation_id_segment_mismatches_request()
    {
        var fixture = Fixture.Create();
        var result = fixture.Runner.Apply(fixture.Request(operationId: "wrong-op"), CancellationToken.None);
        Assert.Equal("operation_id_mismatch", Assert.IsType<ApplyResult.Refused>(result).Reason);
    }

    [Fact]
    public void Apply_refuses_when_source_file_does_not_exist()
    {
        var fixture = Fixture.Create(writeSource: false);
        var result = fixture.Runner.Apply(fixture.Request(), CancellationToken.None);
        Assert.Equal("source_missing", Assert.IsType<ApplyResult.Refused>(result).Reason);
    }

    [Fact]
    public void Apply_refuses_when_destination_file_already_exists()
    {
        var fixture = Fixture.Create(writeDestination: true);
        var result = fixture.Runner.Apply(fixture.Request(), CancellationToken.None);
        Assert.Equal("destination_already_exists", Assert.IsType<ApplyResult.Refused>(result).Reason);
    }

    [Fact]
    public void Apply_refuses_when_approval_token_mismatches_awaiting_approval_packet()
    {
        var fixture = Fixture.Create(token: "expected-token");
        var result = fixture.Runner.Apply(fixture.Request(token: "wrong-token"), CancellationToken.None);
        Assert.Equal("approval_token_mismatch", Assert.IsType<ApplyResult.Refused>(result).Reason);
    }

    [Fact]
    public void Apply_refuses_when_scope_hash_mismatches_awaiting_approval_packet()
    {
        var fixture = Fixture.Create(scopeHash: new string('1', 64));
        var result = fixture.Runner.Apply(fixture.Request(), CancellationToken.None);
        Assert.Equal("scope_hash_mismatch", Assert.IsType<ApplyResult.Refused>(result).Reason);
    }

    [Fact]
    public void Apply_happy_path_writes_six_packets_in_order()
    {
        var fixture = Fixture.Create();
        var preHash = fixture.Hash(fixture.SourcePath);

        var result = fixture.Runner.Apply(fixture.Request(), CancellationToken.None);

        var succeeded = Assert.IsType<ApplyResult.Succeeded>(result);
        Assert.Equal($"{OperationId}/{ScopeId}/sample.approved.json", succeeded.ApprovedRelativePath);
        Assert.Equal(preHash, succeeded.PostHashSha256);
        Assert.False(File.Exists(fixture.SourcePath));
        Assert.True(File.Exists(fixture.DestinationPath));
        Assert.Equal(preHash, fixture.Hash(fixture.DestinationPath));
        Assert.Equal(
            [
                SelfImprovementPacketKinds.ApplyV0DryRunStarted,
                SelfImprovementPacketKinds.ApplyV0DryRunCompleted,
                SelfImprovementPacketKinds.ApplyV0BackupWritten,
                SelfImprovementPacketKinds.ApplyV0Applied,
                SelfImprovementPacketKinds.ApplyV0PostProofCompleted,
                SelfImprovementPacketKinds.ApplyV0Completed
            ],
            fixture.ApplyRows().Select(row => row.PacketKind).ToArray());
    }

    [Fact]
    public void Apply_backup_sha256_mismatch_rolls_back_emits_no_completed()
    {
        var fixture = Fixture.Create(sha256: path => path.Contains(".backup-", StringComparison.Ordinal) ? new string('f', 64) : Fixture.RealHash(path));

        var result = fixture.Runner.Apply(fixture.Request(), CancellationToken.None);

        Assert.Equal("backup_sha256_mismatch", Assert.IsType<ApplyResult.RolledBack>(result).Reason);
        Assert.True(File.Exists(fixture.SourcePath));
        Assert.False(File.Exists(fixture.DestinationPath));
        Assert.Equal(
            [
                SelfImprovementPacketKinds.ApplyV0DryRunStarted,
                SelfImprovementPacketKinds.ApplyV0DryRunCompleted,
                SelfImprovementPacketKinds.ApplyV0RolledBack
            ],
            fixture.ApplyRows().Select(row => row.PacketKind).ToArray());
    }

    [Fact]
    public void Apply_post_proof_mismatch_rolls_back_destination_back_to_source()
    {
        var firstDestinationHash = true;
        var fixture = Fixture.Create(sha256: path =>
        {
            if (path.EndsWith(".approved.json", StringComparison.Ordinal) && firstDestinationHash)
            {
                firstDestinationHash = false;
                return new string('e', 64);
            }

            return Fixture.RealHash(path);
        });

        var result = fixture.Runner.Apply(fixture.Request(), CancellationToken.None);

        Assert.Equal("post_proof_mismatch", Assert.IsType<ApplyResult.RolledBack>(result).Reason);
        Assert.True(File.Exists(fixture.SourcePath));
        Assert.False(File.Exists(fixture.DestinationPath));
        Assert.Equal(
            [
                SelfImprovementPacketKinds.ApplyV0DryRunStarted,
                SelfImprovementPacketKinds.ApplyV0DryRunCompleted,
                SelfImprovementPacketKinds.ApplyV0BackupWritten,
                SelfImprovementPacketKinds.ApplyV0Applied,
                SelfImprovementPacketKinds.ApplyV0RolledBack
            ],
            fixture.ApplyRows().Select(row => row.PacketKind).ToArray());
    }

    [Fact]
    public void Apply_double_apply_against_same_artifact_returns_destination_already_exists()
    {
        var fixture = Fixture.Create();
        _ = fixture.Runner.Apply(fixture.Request(), CancellationToken.None);
        File.WriteAllText(fixture.SourcePath, "{\"hello\":\"again\"}");
        var count = fixture.ApplyRows().Count;

        var result = fixture.Runner.Apply(fixture.Request(), CancellationToken.None);

        Assert.Equal("destination_already_exists", Assert.IsType<ApplyResult.Refused>(result).Reason);
        Assert.Equal(count, fixture.ApplyRows().Count);
    }

    [Fact]
    public void Apply_kill_switch_activated_between_backup_and_post_proof_rolls_back()
    {
        var fixture = Fixture.Create(sha256: path =>
        {
            var hash = Fixture.RealHash(path);
            if (path.Contains(".backup-", StringComparison.Ordinal))
            {
                fixtureForKill!.Settings[KillSwitchService.KillSwitchSetting] = bool.TrueString;
            }

            return hash;
        }, deferRunner: true);
        fixtureForKill = fixture;
        fixture.BuildRunner();

        var result = fixture.Runner.Apply(fixture.Request(), CancellationToken.None);

        Assert.Equal("kill_switch_activated", Assert.IsType<ApplyResult.RolledBack>(result).Reason);
        Assert.True(File.Exists(fixture.SourcePath));
        Assert.False(File.Exists(fixture.DestinationPath));
        Assert.Equal(
            [
                SelfImprovementPacketKinds.ApplyV0DryRunStarted,
                SelfImprovementPacketKinds.ApplyV0DryRunCompleted,
                SelfImprovementPacketKinds.ApplyV0BackupWritten,
                SelfImprovementPacketKinds.ApplyV0RolledBack
            ],
            fixture.ApplyRows().Select(row => row.PacketKind).ToArray());
    }

    private static Fixture? fixtureForKill;

    [Fact]
    public void Apply_runner_class_source_contains_no_update_or_delete_sql()
    {
        var source = File.ReadAllText(SourcePath());
        Assert.DoesNotContain("UPDATE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Apply_runner_class_source_only_writes_under_artifact_root()
    {
        var source = File.ReadAllText(SourcePath());
        Assert.DoesNotContain("Environment.SpecialFolder.MyDocuments", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Environment.SpecialFolder.Desktop", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Environment.SpecialFolder.System", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Path.GetTempPath()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AppContext.BaseDirectory", source, StringComparison.Ordinal);
    }

    private static void AssertFlagRefusal(string flag)
    {
        var fixture = Fixture.Create();
        fixture.Settings[flag] = bool.FalseString;

        var result = fixture.Runner.Apply(fixture.Request(), CancellationToken.None);

        Assert.Equal($"flag_{flag}_not_true", Assert.IsType<ApplyResult.Refused>(result).Reason);
        Assert.Empty(fixture.ApplyRows());
    }

    private static void AssertInvalidPath(string relativePath)
    {
        var fixture = Fixture.Create();
        var result = fixture.Runner.Apply(fixture.Request(relativePath: relativePath), CancellationToken.None);
        Assert.Equal("invalid_relative_path", Assert.IsType<ApplyResult.Refused>(result).Reason);
        Assert.Empty(fixture.ApplyRows());
    }

    public static IEnumerable<object[]> ForbiddenExtensionValues()
    {
        return ForbiddenExtensions.Select(extension => new object[] { extension });
    }

    private static string SourcePath()
    {
        var root = FindRepositoryRoot();
        return Path.Combine(root, "vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Apply", "ArtifactRenameApplyRunner.cs");
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (Directory.Exists(Path.Combine(current, ".git")) || File.Exists(Path.Combine(current, "vnext", "Wevito.VNext.sln")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName ?? "";
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }

    private sealed class Fixture
    {
        private readonly Func<string, string>? _sha256;
        private readonly bool _prereqPassed;

        private Fixture(Func<string, string>? sha256, bool prereqPassed, string extension)
        {
            _sha256 = sha256;
            _prereqPassed = prereqPassed;
            Extension = extension;
            Root = Path.Combine(Path.GetTempPath(), "wevito-apply-runner-tests", Guid.NewGuid().ToString("N"));
            ArtifactRoot = Path.Combine(Root, "artifacts");
            Ledger = new AuditLedgerService(Path.Combine(Root, "ledger.sqlite"));
            Settings = AllFlagsTrue();
            KillSwitch = new KillSwitchService(() => Settings, Ledger);
            SourcePath = Path.Combine(ArtifactRoot, OperationId, ScopeId, $"sample.draft.{Extension}");
            DestinationPath = Path.Combine(ArtifactRoot, OperationId, ScopeId, $"sample.approved.{Extension}");
        }

        public string Root { get; }

        public string ArtifactRoot { get; }

        public string SourcePath { get; }

        public string DestinationPath { get; }

        public string Extension { get; }

        public AuditLedgerService Ledger { get; }

        public Dictionary<string, string> Settings { get; }

        public KillSwitchService KillSwitch { get; }

        public ArtifactRenameApplyRunner Runner { get; private set; } = null!;

        public static Fixture Create(
            bool writeSource = true,
            bool writeDestination = false,
            bool prereqPassed = true,
            string token = Token,
            string scopeHash = ScopeHash,
            string extension = "json",
            Func<string, string>? sha256 = null,
            bool deferRunner = false)
        {
            var fixture = new Fixture(sha256, prereqPassed, extension);
            Directory.CreateDirectory(Path.GetDirectoryName(fixture.SourcePath)!);
            if (writeSource)
            {
                File.WriteAllText(fixture.SourcePath, "{\"hello\":\"draft\"}");
            }

            if (writeDestination)
            {
                File.WriteAllText(fixture.DestinationPath, "{\"hello\":\"approved\"}");
            }

            fixture.RecordAwaitingApproval(token, scopeHash);
            if (!deferRunner)
            {
                fixture.BuildRunner();
            }

            return fixture;
        }

        public void BuildRunner()
        {
            Runner = new ArtifactRenameApplyRunner(
                Ledger,
                key => Settings.TryGetValue(key, out var value) ? value : null,
                KillSwitch,
                ArtifactRoot,
                operationId => new ApplyRunnerPrerequisiteCheckResult(
                    operationId,
                    _prereqPassed
                        ? [new PrerequisiteEntry("all", true, "ok")]
                        : [new PrerequisiteEntry("broken", false, "nope")],
                    _prereqPassed,
                    DateTimeOffset.Parse("2026-05-20T00:00:00Z")),
                _sha256 ?? RealHash,
                _ => DateTimeOffset.Parse("2026-05-20T00:00:00Z"));
        }

        public ApplyRequest Request(
            string operationId = OperationId,
            string scopeId = ScopeId,
            string scopeHash = ScopeHash,
            string? relativePath = null,
            string token = Token)
        {
            return new ApplyRequest(operationId, scopeId, scopeHash, relativePath ?? $"{OperationId}/{ScopeId}/sample.draft.{Extension}", token);
        }

        public IReadOnlyList<AuditLedgerRow> ApplyRows()
        {
            return Ledger.Snapshot(DateTimeOffset.Parse("2026-05-19T00:00:00Z"), DateTimeOffset.Parse("2026-05-21T00:00:00Z"))
                .Where(row => row.PacketKind.StartsWith("self_improvement_apply_v0_", StringComparison.Ordinal))
                .OrderBy(row => row.Id)
                .ToArray();
        }

        public string Hash(string path) => RealHash(path);

        public static string RealHash(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream)).ToLowerInvariant();
        }

        private void RecordAwaitingApproval(string token, string scopeHash)
        {
            var approvalPath = Path.Combine(ArtifactRoot, OperationId, ScopeId, "apply-awaiting-approval.json");
            Directory.CreateDirectory(Path.GetDirectoryName(approvalPath)!);
            File.WriteAllText(approvalPath, $$"""
                {
                  "operationId": "{{OperationId}}",
                  "scopeId": "{{ScopeId}}",
                  "scopeHash": "{{scopeHash}}",
                  "approvalToken": "{{token}}"
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
                Summary: $$"""{"operation_id":"{{OperationId}}","scope_hash":"{{scopeHash}}","approval_token":"{{token}}"}""",
                Status: "WaitingForApproval"));
        }

        private static Dictionary<string, string> AllFlagsTrue()
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
