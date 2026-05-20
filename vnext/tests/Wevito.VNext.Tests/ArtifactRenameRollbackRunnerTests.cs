using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Apply;

namespace Wevito.VNext.Tests;

public sealed class ArtifactRenameRollbackRunnerTests
{
    private const string OperationId = "op-rollback";
    private const string ScopeId = "scope-abc";
    private const string ScopeHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
    private const string Token = "approve-rollback";
    private const string ApprovedRelativePath = $"{OperationId}/{ScopeId}/sample.approved.json";
    private static readonly string[] ForbiddenExtensions =
    [
        ".cs", ".csproj", ".xaml", ".config", ".exe", ".dll",
        ".png", ".jpg", ".jpeg", ".gif", ".ico", ".wav", ".ogg",
        ".ttf", ".otf", ".yaml", ".yml", ".toml", ".ini",
        ".bat", ".ps1", ".sh", ".py", ".js", ".ts", ".java",
        ".kt", ".swift", ".rs", ".go"
    ];

    [Fact]
    public void ExplicitRollback_refuses_when_enabled_flag_false() => AssertFlagRefusal(ArtifactRenameRollbackRunner.ExplicitRollbackEnabledSetting);

    [Fact]
    public void ExplicitRollback_refuses_when_design_approved_flag_false() => AssertFlagRefusal(ArtifactRenameRollbackRunner.ExplicitRollbackDesignApprovedSetting);

    [Fact]
    public void ExplicitRollback_refuses_when_apply_design_approved_flag_false() => AssertFlagRefusal(ArtifactRenameApplyRunner.DesignApprovedSetting);

    [Fact]
    public void ExplicitRollback_refuses_when_apply_implementation_phase_approved_flag_false() => AssertFlagRefusal(ArtifactRenameApplyRunner.ImplementationPhaseApprovedSetting);

    [Fact]
    public void ExplicitRollback_refuses_when_kill_switch_active_writes_no_packet()
    {
        var fixture = Fixture.Create();
        fixture.Settings[KillSwitchService.KillSwitchSetting] = bool.TrueString;

        var result = fixture.Runner.ExplicitRollback(fixture.Request(), CancellationToken.None);

        Assert.Equal("kill_switch_active", Assert.IsType<RollbackResult.Refused>(result).Reason);
        Assert.Empty(fixture.RollbackRows());
    }

    [Fact]
    public void ExplicitRollback_refuses_when_prerequisite_check_returns_any_failed_entry()
    {
        var fixture = Fixture.Create(prereqPassed: false);

        var result = fixture.Runner.ExplicitRollback(fixture.Request(), CancellationToken.None);

        Assert.StartsWith("prerequisite_check_failed:", Assert.IsType<RollbackResult.Refused>(result).Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void ExplicitRollback_refuses_when_relative_path_contains_double_dot() => AssertInvalidPath($"{OperationId}/{ScopeId}/../sample.approved.json");

    [Fact]
    public void ExplicitRollback_refuses_when_relative_path_is_absolute() => AssertInvalidPath("/absolute/sample.approved.json");

    [Fact]
    public void ExplicitRollback_refuses_when_relative_path_contains_backslash() => AssertInvalidPath($@"{OperationId}\{ScopeId}\sample.approved.json");

    [Fact]
    public void ExplicitRollback_refuses_when_relative_path_does_not_end_with_approved_json() => AssertInvalidPath($"{OperationId}/{ScopeId}/sample.draft.json");

    [Fact]
    public void ExplicitRollback_refuses_when_relative_path_has_unsupported_extension() => AssertInvalidPath($"{OperationId}/{ScopeId}/sample.approved.html");

    [Theory]
    [InlineData("txt")]
    [InlineData("md")]
    [InlineData("svg")]
    public void ExplicitRollback_refuses_non_json_extension_when_extended_flag_false(string extension)
    {
        var fixture = Fixture.Create(extension: extension);

        var result = fixture.Runner.ExplicitRollback(fixture.Request(), CancellationToken.None);

        Assert.Equal($"flag_{ArtifactRenameApplyRunner.ExtendedArtifactTypesEnabledSetting}_not_true", Assert.IsType<RollbackResult.Refused>(result).Reason);
        Assert.Empty(fixture.RollbackRows());
    }

    [Theory]
    [InlineData("txt")]
    [InlineData("md")]
    [InlineData("svg")]
    public void ExplicitRollback_accepts_extended_artifact_extension_when_flag_true(string extension)
    {
        var fixture = Fixture.Create(extension: extension);
        fixture.Settings[ArtifactRenameApplyRunner.ExtendedArtifactTypesEnabledSetting] = bool.TrueString;
        var preHash = fixture.Hash(fixture.SourcePath);

        var result = fixture.Runner.ExplicitRollback(fixture.Request(), CancellationToken.None);

        var succeeded = Assert.IsType<RollbackResult.Succeeded>(result);
        Assert.Equal($"{OperationId}/{ScopeId}/sample.draft.{extension}", succeeded.DraftRelativePath);
        Assert.Equal(preHash, succeeded.PostHashSha256);
        Assert.False(File.Exists(fixture.SourcePath));
        Assert.True(File.Exists(fixture.DestinationPath));
        Assert.Equal(preHash, fixture.Hash(fixture.DestinationPath));
    }

    [Theory]
    [MemberData(nameof(ForbiddenExtensionValues))]
    public void ExplicitRollback_refuses_forbidden_extensions_even_when_extended_flag_true(string extension)
    {
        var fixture = Fixture.Create(writeSource: false);
        fixture.Settings[ArtifactRenameApplyRunner.ExtendedArtifactTypesEnabledSetting] = bool.TrueString;

        var result = fixture.Runner.ExplicitRollback(fixture.Request(relativePath: $"{OperationId}/{ScopeId}/sample.approved{extension}"), CancellationToken.None);

        Assert.Equal($"forbidden_extension:{extension}", Assert.IsType<RollbackResult.Refused>(result).Reason);
        Assert.Empty(fixture.RollbackRows());
    }

    [Fact]
    public void ExplicitRollback_refuses_when_scope_id_segment_mismatches_request()
    {
        var fixture = Fixture.Create();
        var result = fixture.Runner.ExplicitRollback(fixture.Request(scopeId: "wrong-scope"), CancellationToken.None);
        Assert.Equal("scope_id_mismatch", Assert.IsType<RollbackResult.Refused>(result).Reason);
    }

    [Fact]
    public void ExplicitRollback_refuses_when_operation_id_segment_mismatches_request()
    {
        var fixture = Fixture.Create();
        var result = fixture.Runner.ExplicitRollback(fixture.Request(operationId: "wrong-op"), CancellationToken.None);
        Assert.Equal("operation_id_mismatch", Assert.IsType<RollbackResult.Refused>(result).Reason);
    }

    [Fact]
    public void ExplicitRollback_refuses_when_source_file_does_not_exist()
    {
        var fixture = Fixture.Create(writeSource: false);
        var result = fixture.Runner.ExplicitRollback(fixture.Request(), CancellationToken.None);
        Assert.Equal("source_missing", Assert.IsType<RollbackResult.Refused>(result).Reason);
    }

    [Fact]
    public void ExplicitRollback_refuses_when_destination_file_already_exists()
    {
        var fixture = Fixture.Create(writeDestination: true);
        var result = fixture.Runner.ExplicitRollback(fixture.Request(), CancellationToken.None);
        Assert.Equal("destination_already_exists", Assert.IsType<RollbackResult.Refused>(result).Reason);
    }

    [Fact]
    public void ExplicitRollback_refuses_when_approval_token_mismatches_awaiting_approval_packet()
    {
        var fixture = Fixture.Create(token: "expected-token");
        var result = fixture.Runner.ExplicitRollback(fixture.Request(token: "wrong-token"), CancellationToken.None);
        Assert.Equal("approval_token_mismatch", Assert.IsType<RollbackResult.Refused>(result).Reason);
    }

    [Fact]
    public void ExplicitRollback_refuses_when_scope_hash_mismatches_awaiting_approval_packet()
    {
        var fixture = Fixture.Create(scopeHash: new string('1', 64));
        var result = fixture.Runner.ExplicitRollback(fixture.Request(), CancellationToken.None);
        Assert.Equal("scope_hash_mismatch", Assert.IsType<RollbackResult.Refused>(result).Reason);
    }

    [Fact]
    public void ExplicitRollback_happy_path_writes_started_and_completed_packets()
    {
        var fixture = Fixture.Create();
        var preHash = fixture.Hash(fixture.SourcePath);

        var result = fixture.Runner.ExplicitRollback(fixture.Request(), CancellationToken.None);

        var succeeded = Assert.IsType<RollbackResult.Succeeded>(result);
        Assert.Equal($"{OperationId}/{ScopeId}/sample.draft.json", succeeded.DraftRelativePath);
        Assert.Equal(preHash, succeeded.PostHashSha256);
        Assert.False(File.Exists(fixture.SourcePath));
        Assert.True(File.Exists(fixture.DestinationPath));
        Assert.Equal(preHash, fixture.Hash(fixture.DestinationPath));
        Assert.Equal(
            [
                SelfImprovementPacketKinds.ApplyV0ExplicitRollbackStarted,
                SelfImprovementPacketKinds.ApplyV0ExplicitRollbackCompleted
            ],
            fixture.RollbackRows().Select(row => row.PacketKind).ToArray());
    }

    [Fact]
    public void ExplicitRollback_double_rollback_refuses_destination_already_exists()
    {
        var fixture = Fixture.Create();
        _ = fixture.Runner.ExplicitRollback(fixture.Request(), CancellationToken.None);
        File.WriteAllText(fixture.SourcePath, "{\"hello\":\"approved-again\"}");

        var result = fixture.Runner.ExplicitRollback(fixture.Request(), CancellationToken.None);

        Assert.Equal("destination_already_exists", Assert.IsType<RollbackResult.Refused>(result).Reason);
    }

    [Fact]
    public void ExplicitRollback_kill_switch_activated_after_started_refuses()
    {
        var fixture = Fixture.Create(sha256: path =>
        {
            if (path.EndsWith(".approved.json", StringComparison.Ordinal))
            {
                fixtureForKill!.Settings[KillSwitchService.KillSwitchSetting] = bool.TrueString;
            }

            return Fixture.RealHash(path);
        }, deferRunner: true);
        fixtureForKill = fixture;
        fixture.BuildRunner();

        var result = fixture.Runner.ExplicitRollback(fixture.Request(), CancellationToken.None);

        Assert.Equal("kill_switch_active", Assert.IsType<RollbackResult.Refused>(result).Reason);
        Assert.True(File.Exists(fixture.SourcePath));
        Assert.False(File.Exists(fixture.DestinationPath));
    }

    private static Fixture? fixtureForKill;

    [Fact]
    public void ExplicitRollback_runner_class_source_contains_no_update_or_delete_sql()
    {
        var source = File.ReadAllText(SourcePath());
        Assert.DoesNotContain("UPDATE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExplicitRollback_runner_class_source_never_calls_apply_runner_apply()
    {
        var source = File.ReadAllText(SourcePath());
        Assert.DoesNotContain("ArtifactRenameApplyRunner.Apply", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Apply(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ExplicitRollback_runner_class_source_only_writes_under_artifact_root()
    {
        var source = File.ReadAllText(SourcePath());
        Assert.DoesNotContain("Environment.SpecialFolder.MyDocuments", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Environment.SpecialFolder.Desktop", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Environment.SpecialFolder.System", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Path.GetTempPath()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AppContext.BaseDirectory", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Rollback_evidence_expander_is_read_only_and_does_not_expose_mutating_controls()
    {
        var xaml = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "vnext", "src", "Wevito.VNext.Shell", "ToolPopupWindow.xaml"));
        var start = xaml.IndexOf("Rollback evidence (read-only)", StringComparison.Ordinal);
        Assert.True(start >= 0, "Rollback evidence expander must exist.");
        var end = xaml.IndexOf("<Expander Header=\"Local runtime readiness", start, StringComparison.Ordinal);
        Assert.True(end > start, "Rollback evidence expander should sit before local runtime readiness.");
        var section = xaml[start..end];

        Assert.DoesNotContain("<Button", section, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ToggleSwitch", section, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<CheckBox", section, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MenuItem", section, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExplicitRollback(", File.ReadAllText(Path.Combine(FindRepositoryRoot(), "vnext", "src", "Wevito.VNext.Shell", "ToolPopupWindow.xaml.cs")), StringComparison.Ordinal);
    }

    private static void AssertFlagRefusal(string flag)
    {
        var fixture = Fixture.Create();
        fixture.Settings[flag] = bool.FalseString;

        var result = fixture.Runner.ExplicitRollback(fixture.Request(), CancellationToken.None);

        Assert.Equal($"flag_{flag}_not_true", Assert.IsType<RollbackResult.Refused>(result).Reason);
    }

    private static void AssertInvalidPath(string relativePath)
    {
        var fixture = Fixture.Create();
        var result = fixture.Runner.ExplicitRollback(fixture.Request(relativePath: relativePath), CancellationToken.None);
        Assert.Equal("invalid_relative_path", Assert.IsType<RollbackResult.Refused>(result).Reason);
    }

    public static IEnumerable<object[]> ForbiddenExtensionValues()
    {
        return ForbiddenExtensions.Select(extension => new object[] { extension });
    }

    private static string SourcePath()
    {
        var root = FindRepositoryRoot();
        return Path.Combine(root, "vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Apply", "ArtifactRenameRollbackRunner.cs");
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "vnext", "Wevito.VNext.sln")))
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
            Root = Path.Combine(Path.GetTempPath(), "wevito-rollback-runner-tests", Guid.NewGuid().ToString("N"));
            ArtifactRoot = Path.Combine(Root, "artifacts");
            Ledger = new AuditLedgerService(Path.Combine(Root, "ledger.sqlite"));
            Settings = AllFlagsTrue();
            KillSwitch = new KillSwitchService(() => Settings, Ledger);
            SourcePath = Path.Combine(ArtifactRoot, OperationId, ScopeId, $"sample.approved.{Extension}");
            DestinationPath = Path.Combine(ArtifactRoot, OperationId, ScopeId, $"sample.draft.{Extension}");
        }

        public string Root { get; }

        public string ArtifactRoot { get; }

        public string SourcePath { get; }

        public string DestinationPath { get; }

        public string Extension { get; }

        public AuditLedgerService Ledger { get; }

        public Dictionary<string, string> Settings { get; }

        public KillSwitchService KillSwitch { get; }

        public ArtifactRenameRollbackRunner Runner { get; private set; } = null!;

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
                File.WriteAllText(fixture.SourcePath, "{\"hello\":\"approved\"}");
            }

            if (writeDestination)
            {
                File.WriteAllText(fixture.DestinationPath, "{\"hello\":\"draft\"}");
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
            Runner = new ArtifactRenameRollbackRunner(
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

        public RollbackRequest Request(
            string operationId = OperationId,
            string scopeId = ScopeId,
            string scopeHash = ScopeHash,
            string? relativePath = null,
            string token = Token)
        {
            return new RollbackRequest(operationId, scopeId, scopeHash, relativePath ?? $"{OperationId}/{ScopeId}/sample.approved.{Extension}", token);
        }

        public IReadOnlyList<AuditLedgerRow> RollbackRows()
        {
            return Ledger.Snapshot(DateTimeOffset.Parse("2026-05-19T00:00:00Z"), DateTimeOffset.Parse("2026-05-21T00:00:00Z"))
                .Where(row => row.PacketKind.Contains("explicit_rollback", StringComparison.Ordinal))
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
                [ArtifactRenameRollbackRunner.ExplicitRollbackEnabledSetting] = bool.TrueString,
                [ArtifactRenameRollbackRunner.ExplicitRollbackDesignApprovedSetting] = bool.TrueString,
                [ArtifactRenameApplyRunner.DesignApprovedSetting] = bool.TrueString,
                [ArtifactRenameApplyRunner.ImplementationPhaseApprovedSetting] = bool.TrueString
            };
        }
    }
}
