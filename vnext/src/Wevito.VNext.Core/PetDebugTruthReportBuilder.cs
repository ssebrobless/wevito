using System.Text;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class PetDebugTruthReportBuilder
{
    private readonly PetSimulationEngine _simulationEngine;
    private readonly PetWellbeingInterpreter _wellbeingInterpreter;

    public PetDebugTruthReportBuilder(
        PetSimulationEngine? simulationEngine = null,
        PetWellbeingInterpreter? wellbeingInterpreter = null)
    {
        _simulationEngine = simulationEngine ?? new PetSimulationEngine();
        _wellbeingInterpreter = wellbeingInterpreter ?? new PetWellbeingInterpreter();
    }

    public PetDebugTruthReport Build(GameContent content, IReadOnlyList<PetActor> pets, CompanionMode mode, DateTimeOffset timestamp)
    {
        var snapshots = _wellbeingInterpreter.BuildSnapshots(pets);
        var snapshotsByPetId = snapshots.ToDictionary(snapshot => snapshot.PetId);
        var petEntries = pets
            .Select(pet =>
            {
                var snapshot = snapshotsByPetId[pet.Id];
                return new PetDebugTruthPetEntry(
                    pet.Id,
                    pet.Name,
                    pet.SpeciesId,
                    pet.AgeStage,
                    pet.Gender,
                    pet.ColorVariant,
                    pet.BehaviorState,
                    pet.CurrentAnimationState,
                    ResolveExpectedAnimationHint(pet, mode),
                    snapshot.Urgency,
                    snapshot.DominantDrive,
                    snapshot.DominantEmotion,
                    snapshot.Summary,
                    snapshot.Statuses,
                    snapshot.PersonalityDescriptors,
                    snapshot.ActiveConditionIds);
            })
            .ToList();
        var actionEntries = content.Actions
            .Where(action => action.IsPrimaryAction)
            .Select(action =>
            {
                var enabled = _simulationEngine.IsActionEnabled(action.Id, pets);
                return new PetDebugTruthActionEntry(
                    action.Id,
                    action.DisplayName,
                    enabled,
                    ExplainActionReadiness(action.Id, pets, enabled));
            })
            .ToList();
        var findings = BuildFindings(petEntries, actionEntries);

        return new PetDebugTruthReport(timestamp, mode, petEntries, actionEntries, findings);
    }

    public string ToMarkdown(PetDebugTruthReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Pet Debug Truth Report");
        builder.AppendLine();
        builder.AppendLine($"Generated UTC: {report.GeneratedAtUtc:O}");
        builder.AppendLine($"Mode: {report.Mode}");
        builder.AppendLine();
        builder.AppendLine("## Pets");
        builder.AppendLine();
        builder.AppendLine("| Pet | State | Visible | Expected | Wellbeing | Drive | Emotion | Summary |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var pet in report.Pets)
        {
            builder.AppendLine($"| {pet.PetName} | {pet.BehaviorState} | {pet.VisibleAnimation} | {pet.ExpectedAnimationHint} | {pet.Urgency} | {pet.DominantDrive} | {pet.DominantEmotion} | {EscapeMarkdown(pet.Summary)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Actions");
        builder.AppendLine();
        builder.AppendLine("| Action | Enabled | Reason |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var action in report.Actions)
        {
            builder.AppendLine($"| {action.DisplayName} | {action.IsEnabled} | {EscapeMarkdown(action.Reason)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Findings");
        builder.AppendLine();
        if (report.Findings.Count == 0)
        {
            builder.AppendLine("- No debug-truth findings.");
        }
        else
        {
            foreach (var finding in report.Findings)
            {
                builder.AppendLine($"- {finding}");
            }
        }

        return builder.ToString();
    }

    private static IReadOnlyList<string> BuildFindings(
        IReadOnlyList<PetDebugTruthPetEntry> pets,
        IReadOnlyList<PetDebugTruthActionEntry> actions)
    {
        var findings = new List<string>();
        if (pets.Count == 0)
        {
            findings.Add("No active pets are available for simulation/debug truth.");
            return findings;
        }

        foreach (var pet in pets)
        {
            if (pet.Urgency is PetWellbeingUrgency.Critical or PetWellbeingUrgency.NeedsCare)
            {
                findings.Add($"{pet.PetName} is {pet.Urgency}; dominant drive is {pet.DominantDrive}.");
            }

            if (pet.VisibleAnimation != pet.ExpectedAnimationHint && pet.ExpectedAnimationHint is PetAnimationState.Sick or PetAnimationState.Sleep or PetAnimationState.Walk)
            {
                findings.Add($"{pet.PetName} visible animation is {pet.VisibleAnimation}, expected hint is {pet.ExpectedAnimationHint}.");
            }
        }

        if (actions.All(action => !action.IsEnabled))
        {
            findings.Add("No primary care actions are currently enabled.");
        }

        return findings;
    }

    private static PetAnimationState ResolveExpectedAnimationHint(PetActor pet, CompanionMode mode)
    {
        if (pet.ActiveStatuses?.Contains(PetStatusType.Sick) == true)
        {
            return PetAnimationState.Sick;
        }

        if (mode is CompanionMode.Focused or CompanionMode.Pinned && pet.Energy < 24)
        {
            return PetAnimationState.Sleep;
        }

        return pet.BehaviorState == PetBehaviorState.Roaming
            ? PetAnimationState.Walk
            : PetAnimationState.Idle;
    }

    private static string ExplainActionReadiness(string actionId, IReadOnlyList<PetActor> pets, bool enabled)
    {
        if (pets.Count == 0)
        {
            return "No active pets.";
        }

        return actionId switch
        {
            "feed" => enabled ? "A pet is hungry or has a nutrition-related condition." : "All pets have high hunger values and no malnutrition condition.",
            "water" => enabled ? "A pet can use water." : "All pets have high thirst values.",
            "rest" => enabled ? "A pet is tired, away from home, or has exhaustion." : "All pets are rested at home.",
            "play" => enabled ? "A pet can benefit from affection, comfort, or fitness play." : "All pets are socially and physically satisfied.",
            "groom" => enabled ? "A pet can benefit from grooming or a related condition treatment." : "All pets are clean enough for grooming to be optional.",
            "bath" => enabled ? "A pet is dirty enough or has a bath-treatable condition." : "All pets are clean enough for bath to be optional.",
            "medicine" => enabled ? "A pet has low health, sickness, or a medicine-treatable condition." : "No pet currently needs medicine.",
            "doctor" => enabled ? "A pet has low health or an active condition." : "No pet currently needs doctor care.",
            "home" => enabled ? "A pet is away from home." : "All pets are already home.",
            _ => enabled ? "Action is currently available." : "Action is currently unavailable."
        };
    }

    private static string EscapeMarkdown(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }
}
