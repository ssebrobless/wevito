using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public enum FirstLaunchBackgroundChoice
{
    JustChat,
    HelpWithSpriteCleanup,
    AddLater,
    SetUpLocalAiRuntime
}

public sealed class FirstLaunchWizardStateService
{
    public const string CompletedSetting = "first_launch_completed";
    public const string BackgroundChoiceSetting = "first_launch_background_choice";
    public const string InlineChoiceDismissedSetting = "first_run_choice_panel_dismissed";
    public const string LocalRuntimeSetupRequestedSetting = "first_run_local_runtime_setup_requested";
    public const string FirstRunChoiceRecordedPacketKind = "first_run_choice_recorded";
    public const string SpriteCleanupHelpCardDraftedPacketKind = "sprite_cleanup_help_card_drafted";
    public const string ExperimentRegistrySeedSetting = "experiment_registry_seed";
    public const string SpriteTemplateCandidateGenerationSeed = "sprite-template-candidate-generation";
    public const string AgentSlotNameSettingPrefix = "agent_slot_";
    public const string AgentSlotNameSettingSuffix = "_name";

    private readonly AiIdentityService _identityService;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public FirstLaunchWizardStateService(
        AiIdentityService? identityService = null,
        AuditLedgerService? auditLedgerService = null,
        KillSwitchService? killSwitchService = null)
    {
        _identityService = identityService ?? new AiIdentityService(auditLedgerService, killSwitchService);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public bool ShouldRun(IReadOnlyDictionary<string, string>? settings)
    {
        return settings is null ||
            !settings.TryGetValue(CompletedSetting, out var completed) ||
            !bool.TryParse(completed, out var parsed) ||
            !parsed;
    }

    public IReadOnlyDictionary<string, string> CompleteIdentityStep(
        IReadOnlyDictionary<string, string>? settings,
        string aiName,
        DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var next = _identityService.SetAiName(settings, aiName, timestamp);
        return CompleteStep(next, 1, "ai_identity", timestamp);
    }

    public IReadOnlyDictionary<string, string> CompleteAgentNamesStep(
        IReadOnlyDictionary<string, string>? settings,
        IReadOnlyList<string> agentNames,
        DateTimeOffset? nowUtc = null)
    {
        if (IsWriteBlocked(settings))
        {
            return Clone(settings);
        }

        var next = Clone(settings);
        for (var index = 0; index < Math.Min(3, agentNames.Count); index++)
        {
            var name = string.IsNullOrWhiteSpace(agentNames[index])
                ? $"Agent {index + 1}"
                : agentNames[index].Trim();
            next[BuildAgentSlotNameSetting(index)] = name;
            Record("agent_slot_renamed", $"Agent slot {index + 1} named {name}.", nowUtc ?? DateTimeOffset.UtcNow);
        }

        return CompleteStep(next, 2, "agent_names", nowUtc ?? DateTimeOffset.UtcNow);
    }

    public IReadOnlyDictionary<string, string> CompleteBackgroundChoiceStep(
        IReadOnlyDictionary<string, string>? settings,
        FirstLaunchBackgroundChoice choice,
        DateTimeOffset? nowUtc = null)
    {
        if (IsWriteBlocked(settings))
        {
            return Clone(settings);
        }

        var next = Clone(settings);
        next[BackgroundChoiceSetting] = choice.ToString();
        if (choice == FirstLaunchBackgroundChoice.HelpWithSpriteCleanup)
        {
            next[ExperimentRegistrySeedSetting] = SpriteTemplateCandidateGenerationSeed;
        }

        return CompleteStep(next, 3, "background_choice", nowUtc ?? DateTimeOffset.UtcNow);
    }

    public FirstLaunchChoiceResult ApplyInlineChoice(
        IReadOnlyDictionary<string, string>? settings,
        IReadOnlyList<TaskCard>? existingCards,
        AgentTaskCardQueueService queueService,
        FirstLaunchBackgroundChoice choice,
        DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        if (IsWriteBlocked(settings))
        {
            return new FirstLaunchChoiceResult(Clone(settings), existingCards ?? [], null, false, "First-run choice was blocked by Stop Everything.");
        }

        var nextSettings = Clone(settings);
        nextSettings[BackgroundChoiceSetting] = choice.ToString();
        nextSettings[InlineChoiceDismissedSetting] = bool.TrueString;
        if (choice == FirstLaunchBackgroundChoice.HelpWithSpriteCleanup)
        {
            nextSettings[ExperimentRegistrySeedSetting] = SpriteTemplateCandidateGenerationSeed;
        }
        else if (choice == FirstLaunchBackgroundChoice.SetUpLocalAiRuntime)
        {
            nextSettings[LocalRuntimeSetupRequestedSetting] = bool.TrueString;
        }

        Record(FirstRunChoiceRecordedPacketKind, $"Recorded first-run choice: {choice}.", timestamp);

        if (choice != FirstLaunchBackgroundChoice.HelpWithSpriteCleanup)
        {
            return new FirstLaunchChoiceResult(nextSettings, existingCards ?? [], null, false, "First-run choice recorded.");
        }

        var card = BuildSpriteCleanupHelpTaskCard(timestamp);
        var cards = queueService.AppendDraft(existingCards, card);
        Record(SpriteCleanupHelpCardDraftedPacketKind, "Drafted a sprite-cleanup help task card from first-run choice.", timestamp);

        return new FirstLaunchChoiceResult(nextSettings, cards, card, true, "Sprite-cleanup help task card drafted.");
    }

    public IReadOnlyDictionary<string, string> CompleteFirstChatStep(
        IReadOnlyDictionary<string, string>? settings,
        DateTimeOffset? nowUtc = null)
    {
        var next = CompleteStep(settings, 4, "first_chat", nowUtc ?? DateTimeOffset.UtcNow);
        return CompleteWizard(next, nowUtc ?? DateTimeOffset.UtcNow);
    }

    public IReadOnlyDictionary<string, string> CompleteWizard(
        IReadOnlyDictionary<string, string>? settings,
        DateTimeOffset? nowUtc = null)
    {
        if (IsWriteBlocked(settings))
        {
            return Clone(settings);
        }

        var next = Clone(settings);
        next[CompletedSetting] = bool.TrueString;
        Record("first_launch_completed", "First-launch wizard completed.", nowUtc ?? DateTimeOffset.UtcNow);
        return next;
    }

    public static string BuildAgentSlotNameSetting(int index)
    {
        return $"{AgentSlotNameSettingPrefix}{index}{AgentSlotNameSettingSuffix}";
    }

    private IReadOnlyDictionary<string, string> CompleteStep(
        IReadOnlyDictionary<string, string>? settings,
        int stepNumber,
        string label,
        DateTimeOffset timestamp)
    {
        if (IsWriteBlocked(settings))
        {
            return Clone(settings);
        }

        var next = Clone(settings);
        next[$"first_launch_step_{stepNumber}_completed"] = bool.TrueString;
        Record("first_launch_step_completed", $"First-launch step {stepNumber} completed: {label}.", timestamp);
        return next;
    }

    private bool IsWriteBlocked(IReadOnlyDictionary<string, string>? settings)
    {
        return _killSwitchService?.IsActive() == true || KillSwitchService.IsActive(settings);
    }

    private static Dictionary<string, string> Clone(IReadOnlyDictionary<string, string>? settings)
    {
        return settings is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(settings, StringComparer.OrdinalIgnoreCase);
    }

    private void Record(string kind, string summary, DateTimeOffset timestamp)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            kind,
            null,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: summary,
            Status: "Completed"));
    }

    private static TaskCard BuildSpriteCleanupHelpTaskCard(DateTimeOffset timestamp)
    {
        var intent = new TaskIntent(
            Guid.NewGuid(),
            "Help with sprite cleanup",
            TaskIntentTargetMode.RouteToBestHelper,
            TaskKind: TaskKind.ReviewSprites,
            RequestedToolFamily: "spriteAudit",
            RiskLevel: ToolRiskLevel.Low,
            NeedsApproval: true,
            ExpectedOutput: "Read-only sprite audit; produce repair queue draft.",
            CreatedAtUtc: timestamp);
        var policy = new ToolPolicy(
            "sprite-audit-readonly",
            "spriteAudit",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None);

        return new TaskCard(
            intent.Id,
            intent,
            TaskCardStatus.Draft,
            ToolFamily: "spriteAudit",
            PolicySnapshot: policy,
            Timeline: ["drafted_from_first_run_choice"],
            ResultSummary: "Drafted from first-run sprite cleanup choice. Preview is report-only.",
            CreatedAtUtc: timestamp,
            UpdatedAtUtc: timestamp);
    }
}

public sealed record FirstLaunchChoiceResult(
    IReadOnlyDictionary<string, string> Settings,
    IReadOnlyList<TaskCard> TaskCards,
    TaskCard? DraftedCard,
    bool DraftedSpriteCleanupCard,
    string Message);
