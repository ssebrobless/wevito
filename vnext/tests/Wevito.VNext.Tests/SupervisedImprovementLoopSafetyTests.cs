using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Wevito.VNext.Core.SelfImprovement;
using Wevito.VNext.Core.SelfImprovement.Experiments;

namespace Wevito.VNext.Tests;

public sealed class SupervisedImprovementLoopSafetyTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-18T12:00:00Z");

    [Fact]
    public void TryRun_WhenKillSwitchActive_DoesNotTick()
    {
        var root = TempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var settings = Settings(pilotEnabled: true, betaEnabled: true, proposalScopeEnabled: true);
        settings[KillSwitchService.KillSwitchSetting] = bool.TrueString;
        var loop = new SupervisedImprovementLoop(ledger, killSwitchService: new KillSwitchService(() => settings));

        var result = loop.TryRun(Request(root, settings, [ProposalCard()]));

        Assert.False(result.Ran);
        Assert.Equal("kill_switch=true", result.BlockReason);
        Assert.Empty(ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)));
    }

    [Fact]
    public void TryRun_WhenProposalScopeOff_DoesNotTick()
    {
        var root = TempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var loop = new SupervisedImprovementLoop(ledger);

        var result = loop.TryRun(Request(root, Settings(pilotEnabled: true, betaEnabled: true, proposalScopeEnabled: false), [ProposalCard()]));

        Assert.False(result.Ran);
        Assert.Equal($"{AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairBatchProposalScopeId)}=false", result.BlockReason);
        Assert.Empty(ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)));
    }

    [Fact]
    public void TryRun_WhenAutonomousBetaOff_DoesNotTick()
    {
        var root = TempRoot();
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var loop = new SupervisedImprovementLoop(ledger);

        var result = loop.TryRun(Request(root, Settings(pilotEnabled: true, betaEnabled: false, proposalScopeEnabled: true), [ProposalCard()]));

        Assert.False(result.Ran);
        Assert.Equal($"{AutonomousOperationsConfig.EnabledSetting}=false", result.BlockReason);
        Assert.Empty(ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)));
    }

    [Fact]
    public void LoopDoesNotConstructUserApplyApproval()
    {
        var repoRoot = FindRepositoryRoot();
        var loopPath = Path.Combine(repoRoot, "vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "SupervisedImprovementLoop.cs");
        var text = File.ReadAllText(loopPath);

        Assert.DoesNotContain("new UserApplyApproval(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ToolPopupIsOnlyProductionConstructorCallerForUserApplyApproval()
    {
        var productionRoot = Path.Combine(FindRepositoryRoot(), "vnext", "src");
        var allowed = Path.Combine(productionRoot, "Wevito.VNext.Shell", "ToolPopupWindow.xaml.cs");
        var offenders = Directory
            .EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Equals(allowed, StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("new UserApplyApproval(", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(productionRoot, path))
            .ToArray();

        Assert.Empty(offenders);
    }

    private static SupervisedImprovementLoopRequest Request(string root, IReadOnlyDictionary<string, string> settings, IReadOnlyList<TaskCard> cards)
    {
        return new SupervisedImprovementLoopRequest(settings, ActiveStatus(), Path.Combine(root, "artifacts"), Now, cards);
    }

    private static RuntimeSupervisorStatus ActiveStatus()
    {
        return new RuntimeSupervisorStatus(RuntimeSupervisorMode.Active, true, true, false, "active", "");
    }

    private static Dictionary<string, string> Settings(bool pilotEnabled, bool betaEnabled, bool proposalScopeEnabled)
    {
        return new Dictionary<string, string>
        {
            [SupervisedImprovementLoopSettings.EnabledSetting] = pilotEnabled.ToString(),
            [AutonomousOperationsConfig.EnabledSetting] = betaEnabled.ToString(),
            [AutonomousOperationsConfig.DailyCapSetting] = "3",
            [AutonomousOperationsConfig.IntervalMinutesSetting] = "10",
            [AutonomousScopeService.BuildEnabledSettingKey(AutonomousScopeService.SpriteRepairBatchProposalScopeId)] = proposalScopeEnabled.ToString()
        };
    }

    private static TaskCard ProposalCard()
    {
        var intent = new TaskIntent(
            Guid.NewGuid(),
            "Review self-improvement sprite repair proposal.",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.ReviewSprites,
            RequestedToolFamily: SpriteRepairBatchProposalDescriptor.Kind,
            NeedsApproval: true);
        return new TaskCard(
            Guid.NewGuid(),
            intent,
            TaskCardStatus.Draft,
            ToolFamily: SpriteRepairBatchProposalDescriptor.Kind,
            CreatedAtUtc: Now,
            UpdatedAtUtc: Now,
            ReviewPayload: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["proposal_path"] = "proposal.json"
            });
    }

    private static string TempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-supervised-loop-safety-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "wevito.godot")) ||
                Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
