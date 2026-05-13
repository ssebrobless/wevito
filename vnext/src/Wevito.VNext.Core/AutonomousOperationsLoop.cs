using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record AutonomousOperationsRequest(
    IReadOnlyDictionary<string, string> Settings,
    RuntimeSupervisorStatus RuntimeStatus,
    string ArtifactRoot,
    DateTimeOffset RequestedAtUtc);

public sealed record AutonomousOperationsResult(
    bool Ran,
    bool DidMutate,
    string ArtifactFolder,
    string Summary,
    string BlockReason);

public sealed class AutonomousOperationsLoop
{
    public const string PacketKind = "activity_summary";

    private readonly AutonomousBetaDecisionService _decisionService;
    private readonly AuditLedgerService _ledger;
    private readonly KillSwitchService? _killSwitchService;
    private DateTimeOffset _lastIterationAtUtc;

    public AutonomousOperationsLoop(AutonomousBetaDecisionService decisionService, AuditLedgerService ledger, KillSwitchService? killSwitchService = null)
    {
        _decisionService = decisionService;
        _ledger = ledger;
        _killSwitchService = killSwitchService;
    }

    public AutonomousOperationsResult TryRunIteration(AutonomousOperationsRequest request)
    {
        if (_killSwitchService?.IsActive() == true || KillSwitchService.IsActive(request.Settings))
        {
            return Block("kill_switch=true");
        }

        var config = AutonomousOperationsConfig.FromSettings(request.Settings);
        if (!config.Enabled)
        {
            return Block("runtime_autonomous_beta_enabled=false");
        }

        if (request.RuntimeStatus.Mode != RuntimeSupervisorMode.Active)
        {
            return Block("Runtime supervisor must be Active.");
        }

        if (!request.RuntimeStatus.BackgroundWorkAllowed)
        {
            return Block("Runtime supervisor background work must be allowed.");
        }

        if (_lastIterationAtUtc != default && request.RequestedAtUtc - _lastIterationAtUtc < config.TickInterval)
        {
            return Block("Autonomous beta interval has not elapsed.");
        }

        if (CountToday(request.RequestedAtUtc) >= config.DailyIterationCap)
        {
            return Block("Autonomous beta daily cap reached.");
        }

        var decision = _decisionService.Decide(request.RequestedAtUtc);
        if (decision.Decision != AutonomousBetaDecisionLabel.EnableAutonomousBeta)
        {
            return Block(decision.Summary);
        }

        if (_killSwitchService?.IsActive() == true || KillSwitchService.IsActive(request.Settings))
        {
            return Block("kill_switch=true");
        }

        var folder = Path.Combine(request.ArtifactRoot, $"{request.RequestedAtUtc:yyyyMMdd-HHmmss}-autonomous-operations");
        Directory.CreateDirectory(folder);
        var steps = BuildSteps(config);
        var packet = new
        {
            schemaVersion = "1",
            decision,
            steps,
            didMutate = false,
            didApplyMutation = false,
            note = "Autonomous beta iteration is proposal/preview only."
        };
        File.WriteAllText(Path.Combine(folder, "autonomous-operations.json"), JsonSerializer.Serialize(packet, JsonDefaults.Options));
        File.WriteAllText(Path.Combine(folder, "run-summary.md"), BuildSummary(decision, steps));
        _ledger.Record(new EvidencePacket(
            Guid.NewGuid(),
            PacketKind,
            null,
            request.RequestedAtUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: config.LocalModelReasoningEnabled,
            DidMutate: false,
            folder,
            "Autonomous operations beta completed one proposal-only iteration; mutation_apply=false.",
            "Completed"));
        TryWriteSelfImprovementReport(request);
        _lastIterationAtUtc = request.RequestedAtUtc;
        return new AutonomousOperationsResult(true, false, folder, "Autonomous beta iteration completed without mutation.", "");
    }

    private void TryWriteSelfImprovementReport(AutonomousOperationsRequest request)
    {
        try
        {
            var since = request.RequestedAtUtc.AddDays(-1);
            _ = new SelfImprovementReportService(_ledger, _killSwitchService).Run(new SelfImprovementReportRequest(
                since,
                request.RequestedAtUtc,
                request.ArtifactRoot,
                request.RequestedAtUtc));
        }
        catch (Exception)
        {
            // Activity reports are optional evidence packets; never let a report failure make the pet loop noisy.
        }
    }

    private int CountToday(DateTimeOffset nowUtc)
    {
        var since = new DateTimeOffset(nowUtc.UtcDateTime.Date, TimeSpan.Zero);
        return _ledger.Snapshot(since, nowUtc).Count(row => row.PacketKind.Equals(PacketKind, StringComparison.OrdinalIgnoreCase) && row.Summary.Contains("Autonomous operations beta", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> BuildSteps(AutonomousOperationsConfig config)
    {
        var steps = new List<string>
        {
            "propose",
            "local_research"
        };
        steps.Add(config.ApprovedWebFetchEnabled ? "approved_web_fetch_preview" : "web_fetch_skipped_disabled");
        steps.Add(config.ApprovedFileReadEnabled ? "approved_file_read_preview" : "file_read_skipped_disabled");
        steps.Add(config.LocalModelReasoningEnabled ? "local_model_reasoning_preview" : "deterministic_reasoning_fallback");
        steps.Add("mutation_proposal_preview_only");
        return steps;
    }

    private static string BuildSummary(AutonomousBetaDecision decision, IReadOnlyList<string> steps)
    {
        return string.Join(Environment.NewLine, [
            "# Autonomous Operations Beta",
            "",
            $"- Decision: {decision.Decision}",
            "- Did mutate: false",
            "- Mutation apply: false",
            "",
            "## Steps",
            .. steps.Select(step => $"- {step}"),
            "",
            "The loop may propose or preview work only. It cannot apply guarded mutations."
        ]);
    }

    private static AutonomousOperationsResult Block(string reason)
    {
        return new AutonomousOperationsResult(false, false, "", "", reason);
    }
}
