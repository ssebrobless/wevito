using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class SpriteRepairBatchRunnerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-16T12:00:00Z");

    [Fact]
    public async Task BackupBeforeApply()
    {
        var fixture = BatchFixture.Create();
        fixture.WriteRuntime("idle_00.png", "before");
        var runner = fixture.CreateRunner(candidateBytes: "after", proofRunner: _ => true);

        var result = await runner.RunAsync(fixture.Request());

        Assert.True(result.Succeeded);
        var backupPath = Path.Combine(result.BackupFolder, "idle_00.png");
        Assert.True(File.Exists(backupPath));
        Assert.Equal("before", File.ReadAllText(backupPath));
        Assert.Equal("after", File.ReadAllText(fixture.RuntimeFrame("idle_00.png")));
    }

    [Fact]
    public async Task BackupAndStagingStayOutsideRuntimeTree()
    {
        var fixture = BatchFixture.Create();
        fixture.WriteRuntime("idle_00.png", "before");
        var runner = fixture.CreateRunner(candidateBytes: "after", proofRunner: _ => true);

        var result = await runner.RunAsync(fixture.Request());

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(Path.Combine("sprites_runtime", ".backup"), result.BackupFolder, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(Path.Combine(fixture.Root, "sprites_runtime", ".backup")));
        Assert.False(Directory.Exists(Path.Combine(fixture.Root, "sprites_runtime", ".staging")));
    }

    [Fact]
    public async Task PythonRepairWritesUnderCandidatesFolder()
    {
        var fixture = BatchFixture.Create();
        fixture.WriteRuntime("idle_00.png", "before");
        var commandRunner = new CandidateWritingCommandRunner("after");
        var runner = fixture.CreateRunner(commandRunner: commandRunner, proofRunner: _ => true);

        var result = await runner.RunAsync(fixture.Request());

        Assert.True(result.Succeeded);
        Assert.Contains(Path.Combine("sprites_authored", ".candidates"), result.CandidateFolder, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(Path.GetFullPath(Path.Combine(fixture.Root, "sprites_authored", ".candidates")), result.CandidateFolder, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--out-dir", commandRunner.LastArguments);
    }

    [Fact]
    public async Task DryRunApplyAcceptsCandidatesFolder()
    {
        var fixture = BatchFixture.Create();
        fixture.WriteRuntime("idle_00.png", "before");
        var runner = fixture.CreateRunner(candidateBytes: "after", proofRunner: _ => true);

        var result = await runner.RunAsync(fixture.Request());

        Assert.True(result.Succeeded);
        Assert.True(File.Exists(Path.Combine(fixture.ArtifactRoot, "dry-run", "dry-run-apply.json")));
    }

    [Fact]
    public async Task ApplyMovesFilesIntoRuntime()
    {
        var fixture = BatchFixture.Create();
        fixture.WriteRuntime("idle_00.png", "before");
        var runner = fixture.CreateRunner(candidateBytes: "after", proofRunner: _ => true);

        var result = await runner.RunAsync(fixture.Request());

        Assert.True(result.Succeeded);
        Assert.Equal("after", File.ReadAllText(fixture.RuntimeFrame("idle_00.png")));
    }

    [Fact]
    public async Task PostProofRunsAfterApply()
    {
        var fixture = BatchFixture.Create();
        fixture.WriteRuntime("idle_00.png", "before");
        var proofRanAfterApply = false;
        var runner = fixture.CreateRunner(
            candidateBytes: "after",
            proofRunner: manifest =>
            {
                proofRanAfterApply = File.ReadAllText(manifest.Changes[0].RuntimePath) == "after";
                return true;
            });

        var result = await runner.RunAsync(fixture.Request());

        Assert.True(result.Succeeded);
        Assert.True(proofRanAfterApply);
    }

    [Fact]
    public async Task RollbackRestoresOriginalsOnFailure()
    {
        var fixture = BatchFixture.Create();
        fixture.WriteRuntime("idle_00.png", "before");
        var runner = fixture.CreateRunner(candidateBytes: "after", proofRunner: _ => false);

        var result = await runner.RunAsync(fixture.Request());

        Assert.False(result.Succeeded);
        Assert.True(result.RolledBack);
        Assert.Equal("before", File.ReadAllText(fixture.RuntimeFrame("idle_00.png")));
    }

    [Fact]
    public async Task EvidencePacketHasCorrectHonestyFlags()
    {
        var fixture = BatchFixture.Create();
        fixture.WriteRuntime("idle_00.png", "before");
        var ledgerPath = Path.Combine(fixture.Root, "audit.sqlite");
        var ledger = new AuditLedgerService(ledgerPath);
        var runner = fixture.CreateRunner(candidateBytes: "after", proofRunner: _ => true, auditLedgerService: ledger);

        var result = await runner.RunAsync(fixture.Request());
        var rows = ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1));

        Assert.True(result.Succeeded);
        var row = Assert.Single(rows);
        Assert.Equal(SpriteRepairBatchRunner.BatchPacketKind, row.PacketKind);
        Assert.True(row.DidMutate);
        Assert.False(row.DidUseNetwork);
        Assert.False(row.DidUseHostedAi);
        Assert.False(row.DidUseLocalModel);
    }

    [Fact]
    public async Task RespectsKillSwitch()
    {
        var fixture = BatchFixture.Create();
        fixture.WriteRuntime("idle_00.png", "before");
        var commandRunner = new CandidateWritingCommandRunner("after");
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });
        var runner = fixture.CreateRunner(commandRunner: commandRunner, proofRunner: _ => true, killSwitchService: killSwitch);

        var result = await runner.RunAsync(fixture.Request());

        Assert.False(result.Succeeded);
        Assert.Equal("Blocked", result.Status);
        Assert.False(commandRunner.WasCalled);
        Assert.Equal("before", File.ReadAllText(fixture.RuntimeFrame("idle_00.png")));
    }

    [Fact]
    public async Task RefusesIfCandidateFolderOutsideAuthoredCandidates()
    {
        var fixture = BatchFixture.Create();
        fixture.WriteRuntime("idle_00.png", "before");
        var commandRunner = new CandidateWritingCommandRunner("after");
        var runner = fixture.CreateRunner(commandRunner: commandRunner, proofRunner: _ => true);
        var outside = Path.Combine(Path.GetTempPath(), "wevito-outside-candidates", Guid.NewGuid().ToString("N"));

        var result = await runner.RunAsync(fixture.Request(candidateOverride: outside));

        Assert.False(result.Succeeded);
        Assert.Contains("sprites_authored/.candidates", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(commandRunner.WasCalled);
        Assert.Equal("before", File.ReadAllText(fixture.RuntimeFrame("idle_00.png")));
    }

    [Fact]
    public async Task RefusesIfRuntimeWriteOutsideSpritesRuntimeRoot()
    {
        var fixture = BatchFixture.Create(row: Row("..\\outside", "baby", "female"));
        var commandRunner = new CandidateWritingCommandRunner("after");
        var runner = fixture.CreateRunner(commandRunner: commandRunner, proofRunner: _ => true);

        var result = await runner.RunAsync(fixture.Request());

        Assert.False(result.Succeeded);
        Assert.Contains("outside sprites_runtime", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(commandRunner.WasCalled);
    }

    [Fact]
    public void PlainLanguageExplainerCoversSpriteRepairBatchKinds()
    {
        var explainer = new PlainLanguageExplainer();

        Assert.Contains("sprite repair", explainer.ExplainPacketKind(SpriteRepairBatchRunner.BatchPacketKind), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rolled back", explainer.ExplainPacketKind(SpriteRepairBatchRunner.RolledBackPacketKind), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class BatchFixture
    {
        private BatchFixture(string root, SpriteRepairQueueRow row)
        {
            Root = root;
            Row = row;
            ArtifactRoot = Path.Combine(root, "vnext", "artifacts", "batch");
            Directory.CreateDirectory(Path.Combine(root, "tools"));
            Directory.CreateDirectory(Path.Combine(root, "sprites_authored", ".candidates"));
            Directory.CreateDirectory(RuntimeRowFolder);
            File.WriteAllText(Path.Combine(root, "wevito.godot"), "");
            File.WriteAllText(Path.Combine(root, "tools", "fake_repair.py"), "# fake repair");
        }

        public string Root { get; }
        public SpriteRepairQueueRow Row { get; }
        public string ArtifactRoot { get; }

        private string RuntimeRowFolder => Path.Combine(Root, "sprites_runtime", Row.SpeciesId, Row.LifeStage, Row.Gender, "blue");

        public static BatchFixture Create(SpriteRepairQueueRow? row = null)
        {
            var root = Path.Combine(Path.GetTempPath(), "wevito-sprite-repair-batch-tests", Guid.NewGuid().ToString("N"));
            return new BatchFixture(root, row ?? Row("goose", "baby", "female"));
        }

        public SpriteRepairBatchRunner CreateRunner(
            string? candidateBytes = null,
            Func<SpriteWorkflowApplyManifest, bool>? proofRunner = null,
            ICommandRunner? commandRunner = null,
            AuditLedgerService? auditLedgerService = null,
            KillSwitchService? killSwitchService = null)
        {
            return new SpriteRepairBatchRunner(
                commandRunner ?? new CandidateWritingCommandRunner(candidateBytes ?? "after"),
                postApplyProof: new SpriteWorkflowPostApplyProof(proofRunner ?? (_ => true)),
                auditLedgerService: auditLedgerService,
                killSwitchService: killSwitchService);
        }

        public SpriteRepairBatchRequest Request(string? candidateOverride = null)
        {
            return new SpriteRepairBatchRequest(
                Root,
                Row,
                Row.Issues[0],
                ArtifactRoot,
                Now,
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "test-batch",
                candidateOverride);
        }

        public void WriteRuntime(string fileName, string contents)
        {
            Directory.CreateDirectory(RuntimeRowFolder);
            File.WriteAllText(RuntimeFrame(fileName), contents);
        }

        public string RuntimeFrame(string fileName) => Path.Combine(RuntimeRowFolder, fileName);
    }

    private sealed class CandidateWritingCommandRunner : ICommandRunner
    {
        private readonly string _candidateBytes;

        public CandidateWritingCommandRunner(string candidateBytes)
        {
            _candidateBytes = candidateBytes;
        }

        public bool WasCalled { get; private set; }
        public IReadOnlyList<string> LastArguments { get; private set; } = [];

        public Task<ProofExecutionResult> RunAsync(ProofExecutionRequest request, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            LastArguments = request.Command.Arguments;
            var outDirIndex = request.Command.Arguments.ToList().IndexOf("--out-dir");
            Assert.True(outDirIndex >= 0);
            var outDir = request.Command.Arguments[outDirIndex + 1];
            Directory.CreateDirectory(outDir);
            File.WriteAllText(Path.Combine(outDir, "idle_00.png"), _candidateBytes);

            var started = request.RequestedAtUtc;
            return Task.FromResult(new ProofExecutionResult(
                request.TaskCardId,
                request.CommandId,
                ProofExecutionResultStatus.Succeeded,
                0,
                Path.Combine(request.ArtifactRoot, "stdout.txt"),
                Path.Combine(request.ArtifactRoot, "stderr.txt"),
                Path.Combine(request.ArtifactRoot, "merged.log"),
                Path.Combine(request.ArtifactRoot, "manifest.json"),
                MutationDetected: false,
                "fake repair wrote candidate",
                started,
                started.AddMilliseconds(1)));
        }
    }

    private static SpriteRepairQueueRow Row(string species, string lifeStage, string gender)
    {
        return new SpriteRepairQueueRow(
            $"{species}_{lifeStage}_{gender}",
            species,
            lifeStage,
            gender,
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
                    ["missing_frames"],
                    ["test"],
                    "tools/fake_repair.py",
                    "test repair",
                    null,
                    null)
            ]);
    }
}
