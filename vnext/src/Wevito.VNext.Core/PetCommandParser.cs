using System.Text.RegularExpressions;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class PetCommandParser
{
    private static readonly string[] BlockedActionTerms =
    [
        "delete",
        "send email",
        "send message",
        "dm ",
        "upload",
        "purchase",
        "payment",
        "password",
        "api key",
        "login",
        "install"
    ];
    private static readonly Regex WindowsPathRegex = new(
        "(?:\"(?<path>[A-Za-z]:\\\\[^\"]+)\"|(?<path>[A-Za-z]:\\\\\\S+))",
        RegexOptions.Compiled);
    private static readonly HashSet<string> AgeTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "baby",
        "teen",
        "adult"
    };
    private static readonly HashSet<string> GenderTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "female",
        "male"
    };
    private static readonly HashSet<string> ColorTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "red",
        "orange",
        "yellow",
        "blue",
        "indigo",
        "violet"
    };

    public TaskIntent Parse(
        string rawText,
        IReadOnlyList<PetHelperProfile> helpers,
        Guid? selectedPetId = null,
        DateTimeOffset? nowUtc = null)
    {
        var createdAt = nowUtc ?? DateTimeOffset.UtcNow;
        var trimmed = (rawText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return CreateBlockedIntent(
                trimmed,
                TaskIntentTargetMode.RouteToBestHelper,
                "Please enter a task for one of your helper pets.",
                createdAt);
        }

        var target = ResolveTarget(trimmed, helpers, selectedPetId);
        var body = target.CommandBody.Trim();
        var classification = Classify(body);
        var reason = classification.RefusalReason;

        if (target.TargetMode == TaskIntentTargetMode.ExplicitPetName && target.Helper is null)
        {
            reason = $"No active helper pet named \"{target.PetNameSnapshot}\" was found.";
            classification = classification with
            {
                RiskLevel = ToolRiskLevel.Blocked,
                NeedsApproval = true
            };
        }

        if (target.TargetMode == TaskIntentTargetMode.RouteToBestHelper && target.Helper is null)
        {
            reason = "No active helper pets are available to route this task.";
            classification = classification with
            {
                RiskLevel = ToolRiskLevel.Blocked,
                NeedsApproval = true
            };
        }

        return new TaskIntent(
            Guid.NewGuid(),
            trimmed,
            target.TargetMode,
            TargetPetId: target.Helper?.PetId,
            TargetPetNameSnapshot: target.Helper?.PetNameSnapshot ?? target.PetNameSnapshot,
            TaskKind: classification.TaskKind,
            RequestedToolFamily: classification.ToolFamily,
            TargetPathsOrAssets: classification.TargetPathsOrAssets,
            RiskLevel: classification.RiskLevel,
            NeedsApproval: classification.NeedsApproval,
            ExpectedOutput: classification.ExpectedOutput,
            RefusalOrClarificationReason: reason,
            CreatedAtUtc: createdAt);
    }

    public TaskCard CreateDraftTaskCard(
        TaskIntent intent,
        IReadOnlyList<PetHelperProfile> helpers,
        DateTimeOffset? nowUtc = null)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var helper = FindHelper(intent, helpers);
        var status = intent.RiskLevel == ToolRiskLevel.Blocked ||
            !string.IsNullOrWhiteSpace(intent.RefusalOrClarificationReason)
            ? TaskCardStatus.Blocked
            : intent.NeedsApproval
                ? TaskCardStatus.WaitingForApproval
                : TaskCardStatus.Draft;

        return new TaskCard(
            Guid.NewGuid(),
            intent,
            status,
            AssignedPetId: helper?.PetId,
            AssignedPetNameSnapshot: helper?.PetNameSnapshot ?? intent.TargetPetNameSnapshot,
            ToolFamily: intent.RequestedToolFamily,
            Timeline: [BuildInitialTimeline(status, intent)],
            CreatedAtUtc: timestamp,
            UpdatedAtUtc: timestamp);
    }

    private static TaskIntent CreateBlockedIntent(
        string rawText,
        TaskIntentTargetMode targetMode,
        string reason,
        DateTimeOffset createdAt)
    {
        return new TaskIntent(
            Guid.NewGuid(),
            rawText,
            targetMode,
            RiskLevel: ToolRiskLevel.Blocked,
            NeedsApproval: true,
            RefusalOrClarificationReason: reason,
            CreatedAtUtc: createdAt);
    }

    private static TargetResolution ResolveTarget(
        string rawText,
        IReadOnlyList<PetHelperProfile> helpers,
        Guid? selectedPetId)
    {
        if (rawText.StartsWith('@'))
        {
            var splitIndex = rawText.IndexOf(' ');
            var name = splitIndex > 1 ? rawText[1..splitIndex] : rawText[1..];
            return ResolveExplicitName(
                rawText,
                name,
                splitIndex > 0 ? rawText[(splitIndex + 1)..] : string.Empty,
                helpers);
        }

        foreach (var helper in helpers)
        {
            var commaPrefix = helper.PetNameSnapshot + ",";
            if (rawText.StartsWith(commaPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return new TargetResolution(
                    TaskIntentTargetMode.ExplicitPetName,
                    helper,
                    helper.PetNameSnapshot,
                    rawText[commaPrefix.Length..]);
            }

            var spacePrefix = helper.PetNameSnapshot + " ";
            if (rawText.StartsWith(spacePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return new TargetResolution(
                    TaskIntentTargetMode.ExplicitPetName,
                    helper,
                    helper.PetNameSnapshot,
                    rawText[spacePrefix.Length..]);
            }
        }

        if (selectedPetId is not null)
        {
            var selected = helpers.FirstOrDefault(helper => helper.PetId == selectedPetId.Value);
            if (selected is not null)
            {
                return new TargetResolution(
                    TaskIntentTargetMode.SelectedPet,
                    selected,
                    selected.PetNameSnapshot,
                    rawText);
            }
        }

        var classification = Classify(rawText);
        var best = helpers.FirstOrDefault(helper =>
                (helper.AllowedToolFamilies is { Count: > 0 }
                    ? helper.AllowedToolFamilies
                    : AgentSlotService.BuildAllowedToolFamilies(helper.SlotIndex))
                .Contains(classification.ToolFamily, StringComparer.OrdinalIgnoreCase)) ??
            helpers.FirstOrDefault();

        return new TargetResolution(
            TaskIntentTargetMode.RouteToBestHelper,
            best,
            best?.PetNameSnapshot ?? string.Empty,
            rawText);
    }

    private static TargetResolution ResolveExplicitName(
        string rawText,
        string petName,
        string commandBody,
        IReadOnlyList<PetHelperProfile> helpers)
    {
        var helper = helpers.FirstOrDefault(candidate =>
            string.Equals(candidate.PetNameSnapshot, petName, StringComparison.OrdinalIgnoreCase));
        return new TargetResolution(
            TaskIntentTargetMode.ExplicitPetName,
            helper,
            petName,
            string.IsNullOrWhiteSpace(commandBody) ? rawText : commandBody);
    }

    private static Classification Classify(string commandBody)
    {
        var normalized = commandBody.Trim().ToLowerInvariant();
        if (ContainsAny(normalized, BlockedActionTerms))
        {
            return new Classification(
                TaskKind.ExternalAction,
                "externalAction",
                ToolRiskLevel.Blocked,
                NeedsApproval: true,
                ExpectedOutput: "Blocked task card with safety explanation",
                RefusalReason: "This command needs a human-reviewed policy path before any helper pet can prepare execution.");
        }

        if (normalized.Contains("pet state") ||
            normalized.Contains("wellbeing") ||
            normalized.Contains("debug truth") ||
            normalized.Contains("personality") ||
            normalized.Contains("pet status") ||
            normalized.Contains("pet needs"))
        {
            return new Classification(
                TaskKind.ReviewPetState,
                "petState",
                ToolRiskLevel.Low,
                NeedsApproval: false,
                ExpectedOutput: "No-mutation pet state/debug-truth report");
        }

        if (normalized.Contains("research") ||
            normalized.Contains("look into") ||
            normalized.Contains("investigate") ||
            normalized.Contains("compare sources"))
        {
            return new Classification(
                TaskKind.Research,
                "localResearch",
                ToolRiskLevel.Low,
                NeedsApproval: false,
                ExpectedOutput: "Local-first research plan with source/evidence packet");
        }

        if (normalized.Contains("translate") ||
            normalized.Contains("translation") ||
            normalized.Contains("make this japanese") ||
            normalized.Contains("make this spanish") ||
            normalized.Contains("make this french") ||
            normalized.Contains("turn this into"))
        {
            return new Classification(
                TaskKind.TranslateText,
                "translateText",
                ToolRiskLevel.Low,
                NeedsApproval: false,
                ExpectedOutput: "No-network translation preview report");
        }

        if (normalized.Contains("audio") ||
            normalized.Contains("volume") ||
            normalized.Contains("mute") ||
            normalized.Contains("unmute") ||
            normalized.Contains("boost") ||
            normalized.Contains("equalizer") ||
            normalized.Contains("fxsound") ||
            normalized.Contains("apo") ||
            normalized.Contains("sound boost") ||
            normalized.Contains("volume boost"))
        {
            return new Classification(
                TaskKind.AudioAssist,
                "audioAssist",
                ToolRiskLevel.Low,
                NeedsApproval: false,
                ExpectedOutput: "No-mutation audio assist status report");
        }

        if (normalized.Contains("remember") ||
            normalized.Contains("learn this") ||
            normalized.Contains("save preference") ||
            normalized.Contains("store preference") ||
            normalized.Contains("teach helper") ||
            normalized.Contains("pet memory"))
        {
            return new Classification(
                TaskKind.UpdatePetMemory,
                "petMemory",
                ToolRiskLevel.Medium,
                NeedsApproval: true,
                ExpectedOutput: "Approval-gated pet memory write preview");
        }

        if (normalized.Contains("screenshot") ||
            normalized.Contains("screen shot") ||
            normalized.Contains("screen capture") ||
            normalized.Contains("capture screen") ||
            normalized.Contains("record screen") ||
            normalized.Contains("screen recording") ||
            normalized.Contains("proof clip") ||
            (normalized.Contains("record") && (normalized.Contains("wevito") || normalized.Contains("window"))))
        {
            var target = ScreenCaptureTargetResolver.ResolveTarget(commandBody);
            var targetLabel = target.TargetKind switch
            {
                CaptureTargetKind.LastRegion => "last-region",
                CaptureTargetKind.SelectedRegion => "selected-region",
                _ => "Wevito-window"
            };
            return new Classification(
                TaskKind.ScreenCapture,
                "screenCapture",
                ToolRiskLevel.Low,
                NeedsApproval: false,
                ExpectedOutput: $"No-capture {targetLabel} screen capture preview report");
        }

        if (normalized.Contains("asset inventory") ||
            normalized.Contains("inventory assets") ||
            normalized.Contains("asset count") ||
            normalized.Contains("count assets") ||
            normalized.Contains("list assets"))
        {
            var targets = ExtractLocalPathTargets(commandBody).ToList();
            return new Classification(
                TaskKind.InventoryAssets,
                "assetInventory",
                ToolRiskLevel.Low,
                NeedsApproval: false,
                ExpectedOutput: "No-mutation asset inventory report",
                TargetPathsOrAssets: targets.Count == 0 ? null : targets);
        }

        if (normalized.Contains("summarize") || normalized.Contains("summary") || normalized.Contains("docs"))
        {
            var targets = ExtractLocalPathTargets(commandBody).ToList();
            return new Classification(
                TaskKind.SummarizeDocs,
                "localDocs",
                ToolRiskLevel.Low,
                NeedsApproval: false,
                ExpectedOutput: "Local document summary",
                TargetPathsOrAssets: targets.Count == 0 ? null : targets);
        }

        if (normalized.Contains("code review") ||
            normalized.Contains("review code") ||
            normalized.Contains("review the code") ||
            normalized.Contains("review module") ||
            normalized.Contains("inspect code") ||
            normalized.Contains(".cs") ||
            normalized.Contains(".gd") ||
            normalized.Contains(".ps1"))
        {
            var targets = ExtractLocalPathTargets(commandBody).ToList();
            return new Classification(
                TaskKind.ReviewCode,
                "codeReview",
                ToolRiskLevel.Low,
                NeedsApproval: false,
                ExpectedOutput: "No-mutation code review report",
                TargetPathsOrAssets: targets.Count == 0 ? null : targets);
        }

        if (normalized.Contains("code patch plan") ||
            normalized.Contains("patch plan") ||
            normalized.Contains("plan code patch") ||
            normalized.Contains("plan a code fix") ||
            normalized.Contains("plan code fix") ||
            normalized.Contains("implementation plan for code") ||
            normalized.Contains("plan the fix") && ContainsAny(normalized, [".cs", ".gd", ".ps1", "code", "script", "shell", "vnext", "godot"]))
        {
            var targets = ExtractLocalPathTargets(commandBody).ToList();
            return new Classification(
                TaskKind.PlanCodePatch,
                "codePatchPlan",
                ToolRiskLevel.Low,
                NeedsApproval: false,
                ExpectedOutput: "No-mutation code patch plan report",
                TargetPathsOrAssets: targets.Count == 0 ? null : targets);
        }

        if (normalized.Contains("sprite") || normalized.Contains("audit") || normalized.Contains("review"))
        {
            var targets = ExtractLocalPathTargets(commandBody)
                .Concat(ExtractSpriteRowTargets(commandBody))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return new Classification(
                TaskKind.ReviewSprites,
                "spriteAudit",
                ToolRiskLevel.Low,
                NeedsApproval: false,
                ExpectedOutput: "No-mutation sprite review task card",
                TargetPathsOrAssets: targets.Count == 0 ? null : targets);
        }

        if (normalized.Contains("checklist") || normalized.Contains("plan"))
        {
            return new Classification(
                TaskKind.CreateChecklistDraft,
                "checklist",
                ToolRiskLevel.Low,
                NeedsApproval: false,
                ExpectedOutput: "Draft checklist");
        }

        if (normalized.Contains("build"))
        {
            return new Classification(
                TaskKind.BuildProof,
                "buildProof",
                ToolRiskLevel.Medium,
                NeedsApproval: true,
                ExpectedOutput: "Build/proof task awaiting approval");
        }

        if (normalized.Contains("screenshot") || normalized.Contains("proof"))
        {
            return new Classification(
                TaskKind.CaptureProof,
                "proofCapture",
                ToolRiskLevel.Low,
                NeedsApproval: false,
                ExpectedOutput: "Proof capture task card");
        }

        if (normalized.Contains("open"))
        {
            return new Classification(
                TaskKind.OpenLocalDocument,
                "localDocs",
                ToolRiskLevel.Low,
                NeedsApproval: false,
                ExpectedOutput: "Open local document task card");
        }

        if (normalized.Contains("basket") || normalized.Contains("save link"))
        {
            return new Classification(
                TaskKind.SaveLinkToBasket,
                "basket",
                ToolRiskLevel.Low,
                NeedsApproval: false,
                ExpectedOutput: "Save link to basket task card");
        }

        return new Classification(
            TaskKind.Unknown,
            "draft",
            ToolRiskLevel.Low,
            NeedsApproval: false,
            ExpectedOutput: "Draft task card for user review");
    }

    private static PetHelperProfile? FindHelper(TaskIntent intent, IReadOnlyList<PetHelperProfile> helpers)
    {
        if (intent.TargetPetId is not null)
        {
            var byId = helpers.FirstOrDefault(helper => helper.PetId == intent.TargetPetId.Value);
            if (byId is not null)
            {
                return byId;
            }
        }

        return string.IsNullOrWhiteSpace(intent.TargetPetNameSnapshot)
            ? null
            : helpers.FirstOrDefault(helper =>
                string.Equals(helper.PetNameSnapshot, intent.TargetPetNameSnapshot, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsAny(string text, IReadOnlyList<string> terms)
    {
        return terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> ExtractLocalPathTargets(string commandBody)
    {
        return WindowsPathRegex
            .Matches(commandBody)
            .Select(match => match.Groups["path"].Value.TrimEnd('.', ',', ';'))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> ExtractSpriteRowTargets(string commandBody)
    {
        var tokens = Regex
            .Split(commandBody.ToLowerInvariant(), "[^a-z0-9_]+")
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .ToList();

        for (var index = 1; index < tokens.Count - 2; index++)
        {
            var species = tokens[index - 1];
            var age = tokens[index];
            var gender = tokens[index + 1];
            var color = tokens[index + 2];
            if (AgeTokens.Contains(age) && GenderTokens.Contains(gender) && ColorTokens.Contains(color) && IsLikelySpeciesToken(species))
            {
                yield return Path.Combine(species, age, gender, color);
            }
        }
    }

    private static bool IsLikelySpeciesToken(string token)
    {
        return !string.IsNullOrWhiteSpace(token) &&
               token.Length >= 3 &&
               !AgeTokens.Contains(token) &&
               !GenderTokens.Contains(token) &&
               !ColorTokens.Contains(token) &&
               token is not ("audit" or "review" or "sprite" or "sprites" or "the" or "for");
    }

    private static string BuildInitialTimeline(TaskCardStatus status, TaskIntent intent)
    {
        return status switch
        {
            TaskCardStatus.Blocked => $"blocked: {intent.RefusalOrClarificationReason}",
            TaskCardStatus.WaitingForApproval => "waiting_for_approval: policy approval required before execution",
            _ => "draft: parsed command into task card without executing tools"
        };
    }

    private sealed record TargetResolution(
        TaskIntentTargetMode TargetMode,
        PetHelperProfile? Helper,
        string PetNameSnapshot,
        string CommandBody);

    private sealed record Classification(
        TaskKind TaskKind,
        string ToolFamily,
        ToolRiskLevel RiskLevel,
        bool NeedsApproval,
        string ExpectedOutput,
        IReadOnlyList<string>? TargetPathsOrAssets = null,
        string RefusalReason = "");
}
