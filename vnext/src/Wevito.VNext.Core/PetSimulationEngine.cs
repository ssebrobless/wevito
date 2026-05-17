using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class PetSimulationEngine
{
    private const double BabyToTeenAgeMinutes = 60;
    private const double TeenToAdultAgeMinutes = 240;
    private const double AgingThresholdMinutes = 480;
    private const double DeathToGhostTransitionSeconds = 2.5;
    private const double MemorialLifetimeDays = 1;
    private const double CareDeltaSecondsToMinutes = 1.0 / 60.0;
    private static readonly IReadOnlyDictionary<FetchStage, double> FetchStageDurations = new Dictionary<FetchStage, double>
    {
        [FetchStage.MoveToBall] = 2.4,
        [FetchStage.Pickup] = 0.7,
        [FetchStage.Hold] = 0.45,
        [FetchStage.CarryWalk] = 0.8,
        [FetchStage.CarryRun] = 1.4,
        [FetchStage.Drop] = 0.7,
        [FetchStage.ReturnIdle] = 0.35
    };

    private readonly Random _random = new(7);

    private static readonly IReadOnlyList<string> DefaultColors = ["red", "orange", "yellow", "blue", "indigo", "violet"];
    private static readonly IReadOnlyList<PetAgeStage> DefaultAges = [PetAgeStage.Baby, PetAgeStage.Teen, PetAgeStage.Adult];
    private static readonly IReadOnlyList<PetGender> DefaultGenders = [PetGender.Female, PetGender.Male];
    private static readonly HashSet<string> MedicineTreatableConditions = new(StringComparer.OrdinalIgnoreCase)
    {
        "respiratoryProblems",
        "parasites",
        "dentalProblems",
        "sheddingIssues",
        "skinInfections",
        "viralSusceptibility",
        "exhaustion",
        "injury"
    };

    public IReadOnlyList<PetActor> CreateDefaultPets(GameContent content)
    {
        var speciesPool = content.Species.ToArray();
        var selected = speciesPool
            .OrderBy(species => StarterRosterScore(species.Id))
            .Take(3)
            .ToArray();
        var pets = new List<PetActor>();
        var preferredStarterColors = new[] { "violet", "orange", "yellow" };
        var preferredStarterAges = new[] { PetAgeStage.Adult, PetAgeStage.Teen, PetAgeStage.Adult };
        var preferredStarterGenders = new[] { PetGender.Male, PetGender.Female, PetGender.Male };

        for (var i = 0; i < selected.Length; i++)
        {
            var species = selected[i];
            var supportedAges = species.SupportedAgeStages ?? DefaultAges;
            var supportedGenders = species.SupportedGenders ?? DefaultGenders;
            var colors = species.SupportedColors ?? DefaultColors;
            var age = supportedAges.Contains(preferredStarterAges[i]) ? preferredStarterAges[i] : supportedAges[Math.Min(i, supportedAges.Count - 1)];
            var gender = supportedGenders.Contains(preferredStarterGenders[i]) ? preferredStarterGenders[i] : supportedGenders[i % supportedGenders.Count];
            var preferredColor = preferredStarterColors[i % preferredStarterColors.Length];
            var color = colors.Contains(preferredColor, StringComparer.OrdinalIgnoreCase) ? preferredColor : colors[i % colors.Count];
            pets.Add(CreatePet(
                species,
                age,
                gender,
                color,
                $"{species.DisplayName} {i + 1}",
                DateTimeOffset.UtcNow,
                84 - i * 4,
                80 - i * 3,
                76 - i * 5,
                79 - i * 2,
                74 + i * 4,
                72 + i * 3,
                88 - i * 2,
                70 + i * 4,
                activeStatuses: [PetStatusType.Comforted],
                nextDecisionOffsetSeconds: 1 + i * 0.25));
        }

        return pets;
    }

    public PetActor CreatePet(
        SpeciesDefinition species,
        PetAgeStage ageStage,
        PetGender gender,
        string colorVariant,
        string name,
        DateTimeOffset now,
        double hunger = 84,
        double thirst = 82,
        double energy = 76,
        double cleanliness = 78,
        double affection = 72,
        double comfort = 74,
        double health = 88,
        double fitness = 68,
        IReadOnlyList<PetStatusType>? activeStatuses = null,
        double nextDecisionOffsetSeconds = 1.0)
    {
        var biologicalAgeMinutes = GetStartingBiologicalAgeMinutes(ageStage);
        var personality = SeedPersonality(species.PersonalitySeed, ageStage, gender);
        var habits = SeedHabitProfile(ageStage);
        var conditions = EnsureInnateCondition(species.InnateConditionId, []);
        var speed = CalculatePetSpeedFromState(species.BaseSpeed, ageStage, gender, fitness, biologicalAgeMinutes, conditions);
        return new PetActor(
            Guid.NewGuid(),
            name,
            species.Id,
            species.AccentColor,
            ageStage,
            gender,
            colorVariant,
            0,
            0,
            0,
            0,
            0,
            0,
            species.BaseSpeed,
            speed,
            PetBehaviorState.Home,
            now.AddSeconds(nextDecisionOffsetSeconds),
            PetFacingDirection.Right,
            PetAnimationState.Idle,
            now,
            null,
            null,
            string.Empty,
            null,
            now,
            hunger,
            thirst,
            energy,
            cleanliness,
            affection,
            comfort,
            health,
            fitness,
            biologicalAgeMinutes,
            personality,
            habits,
            conditions,
            activeStatuses ?? [],
            species.DefaultEnvironmentId);
    }

    public PetActor ReconfigurePet(
        PetActor pet,
        SpeciesDefinition species,
        PetAgeStage ageStage,
        PetGender gender,
        string colorVariant,
        DateTimeOffset now)
    {
        var biologicalAgeMinutes = GetStartingBiologicalAgeMinutes(ageStage);
        var fitness = 68;
        var conditions = EnsureInnateCondition(species.InnateConditionId, []);
        return pet with
        {
            SpeciesId = species.Id,
            AccentColor = species.AccentColor,
            AgeStage = ageStage,
            Gender = gender,
            ColorVariant = colorVariant,
            BaseSpeed = species.BaseSpeed,
            Speed = CalculatePetSpeedFromState(species.BaseSpeed, ageStage, gender, fitness, biologicalAgeMinutes, conditions),
            SelectedEnvironmentId = species.DefaultEnvironmentId,
            AgeStageStartedAtUtc = now,
            CurrentAnimationState = PetAnimationState.Idle,
            AnimationStartedAtUtc = now,
            OverrideAnimationState = null,
            OverrideAnimationEndsAtUtc = null,
            LastActionId = string.Empty,
            LastActionAtUtc = null,
            Hunger = 84,
            Thirst = 82,
            Energy = 76,
            Cleanliness = 78,
            Affection = 72,
            Comfort = 74,
            Health = 88,
            Fitness = fitness,
            BiologicalAgeMinutes = biologicalAgeMinutes,
            Personality = SeedPersonality(species.PersonalitySeed, ageStage, gender),
            HabitProfile = SeedHabitProfile(ageStage),
            ActiveConditions = conditions,
            ActiveStatuses = [PetStatusType.Comforted]
        };
    }

    public double CalculatePetSpeed(SpeciesDefinition species, PetAgeStage ageStage, PetGender gender)
    {
        return CalculatePetSpeedFromState(species.BaseSpeed, ageStage, gender, 68, GetStartingBiologicalAgeMinutes(ageStage), []);
    }

    public IReadOnlyList<PetActor> ApplyLayout(
        IReadOnlyList<PetActor> pets,
        double homeLeft,
        double homeTop,
        double homeWidth,
        double homeHeight)
    {
        var livingCount = Math.Max(1, pets.Count(pet => !pet.IsDead));
        var slotWidth = homeWidth / livingCount;
        var floorY = homeTop + homeHeight - 68;
        var updated = new List<PetActor>(pets.Count);
        var livingIndex = 0;

        for (var i = 0; i < pets.Count; i++)
        {
            var pet = pets[i];
            var slotIndex = pet.IsDead ? Math.Min(livingIndex, livingCount - 1) : livingIndex++;
            var homeX = homeLeft + slotWidth * slotIndex + slotWidth / 2;
            updated.Add(pet with
            {
                HomeX = homeX,
                HomeY = floorY,
                CurrentX = !pet.IsDead && pet.BehaviorState == PetBehaviorState.Home ? homeX : pet.CurrentX,
                CurrentY = !pet.IsDead && pet.BehaviorState == PetBehaviorState.Home ? floorY : pet.CurrentY,
                SelectedEnvironmentId = string.IsNullOrWhiteSpace(pet.SelectedEnvironmentId) ? pet.SpeciesId : pet.SelectedEnvironmentId
            });
        }

        return updated;
    }

    public IReadOnlyList<PetActor> Tick(
        IReadOnlyList<PetActor> pets,
        CompanionMode mode,
        RectInt roamBandBounds,
        DateTimeOffset now,
        double deltaSeconds)
    {
        var updated = new List<PetActor>(pets.Count);
        foreach (var pet in pets)
        {
            var normalized = NormalizePet(pet);
            if (normalized.IsDead)
            {
                var dead = RefreshDerivedValues(UpdateDeadLifecycle(normalized, now));
                var roamingGhost = MoveWithinRoamBand(dead, roamBandBounds, now, deltaSeconds);
                updated.Add(RefreshPresentationState(roamingGhost, mode, now));
                continue;
            }

            var vitals = TickVitals(normalized, mode, deltaSeconds);
            var habits = UpdateHabitProfile(vitals, mode, deltaSeconds);
            var conditions = UpdateConditions(habits, deltaSeconds);
            var personality = UpdatePersonality(conditions, deltaSeconds);
            var aged = AdvanceLifecycle(UpdateBiologicalAge(personality, deltaSeconds), now);
            var lifecycle = ApplyDeathIfNeeded(aged, now);
            if (lifecycle.IsDead)
            {
                var dead = RefreshDerivedValues(lifecycle);
                var roamingGhost = MoveWithinRoamBand(dead, roamBandBounds, now, deltaSeconds);
                updated.Add(RefreshPresentationState(roamingGhost, mode, now));
                continue;
            }

            var derived = RefreshDerivedValues(lifecycle);
            var moved = mode is CompanionMode.Focused or CompanionMode.Pinned
                ? MoveTowardHome(derived, now, deltaSeconds)
                : MoveWithinRoamBand(derived, roamBandBounds, now, deltaSeconds);
            updated.Add(RefreshPresentationState(moved, mode, now));
        }

        return updated;
    }

    public IReadOnlyList<PetActor> ApplyAction(string actionId, IReadOnlyList<PetActor> pets, DateTimeOffset now)
    {
        var actionDefinition = BuildImplicitActionDefinition(actionId);
        return ApplyAction(actionDefinition, pets, now);
    }

    public IReadOnlyList<PetActor> ApplyAction(ActionDefinition actionDefinition, IReadOnlyList<PetActor> pets, DateTimeOffset now)
    {
        return pets.Select(pet => ApplyAction(actionDefinition, pet, now)).ToList();
    }

    public PetActor ApplyAutoCare(PetActor pet, DateTimeOffset now)
    {
        var normalized = NormalizePet(pet);
        if (normalized.IsDead)
        {
            return normalized;
        }

        if (normalized.Thirst >= 80)
        {
            return normalized;
        }

        if (normalized.CurrentActionVisualIntent?.Family == AnimationFamily.Drink &&
            normalized.OverrideAnimationEndsAtUtc is not null &&
            normalized.OverrideAnimationEndsAtUtc > now)
        {
            return normalized;
        }

        return ApplyAction(BuildWaterActionDefinition(includeOptionalFamily: true), normalized, now);
    }

    public PetActor StartFetchSequence(PetActor pet, DateTimeOffset now)
    {
        if (pet.IsDead)
        {
            return NormalizePet(pet);
        }

        return SetFetchStage(NormalizePet(pet), FetchStage.MoveToBall, now, new HashSet<AnimationFamily>());
    }

    public PetActor AdvanceFetchSequence(
        PetActor pet,
        DateTimeOffset now,
        IReadOnlySet<AnimationFamily>? availableOptionalFamilies = null)
    {
        var sequence = pet.ActiveFetchSequence;
        if (pet.IsDead)
        {
            return NormalizePet(pet);
        }

        if (sequence is null || sequence.Stage == FetchStage.None)
        {
            return StartFetchSequence(pet, now);
        }

        var maxDuration = FetchStageDurations.TryGetValue(sequence.Stage, out var duration)
            ? duration
            : 0.5;
        if ((now - sequence.StageStartedAtUtc).TotalSeconds < maxDuration)
        {
            return pet;
        }

        var nextStage = GetNextFetchStage(sequence.Stage);
        return SetFetchStage(pet, nextStage, now, availableOptionalFamilies ?? new HashSet<AnimationFamily>());
    }

    public static ActionVisualIntent ResolveFetchStageIntent(
        FetchStage stage,
        IReadOnlySet<AnimationFamily>? availableOptionalFamilies = null)
    {
        var available = availableOptionalFamilies ?? new HashSet<AnimationFamily>();
        return stage switch
        {
            FetchStage.MoveToBall => new ActionVisualIntent(AnimationFamily.Walk, PropOverlayKind.Ball, true),
            FetchStage.Pickup => new ActionVisualIntent(ChooseFetchFamily(AnimationFamily.PickupBall, available, AnimationFamily.PlayBall, AnimationFamily.Happy), PropOverlayKind.Ball),
            FetchStage.Hold => new ActionVisualIntent(ChooseFetchFamily(AnimationFamily.HoldBall, available, AnimationFamily.Happy), PropOverlayKind.Ball, true),
            FetchStage.CarryWalk => new ActionVisualIntent(ChooseFetchFamily(AnimationFamily.CarryBallWalk, available, AnimationFamily.HoldBall, AnimationFamily.Walk), PropOverlayKind.Ball, true),
            FetchStage.CarryRun => new ActionVisualIntent(ChooseFetchFamily(AnimationFamily.CarryBallRun, available, AnimationFamily.CarryBallWalk, AnimationFamily.HoldBall, AnimationFamily.Walk), PropOverlayKind.Ball, true),
            FetchStage.Drop => new ActionVisualIntent(ChooseFetchFamily(AnimationFamily.DropBall, available, AnimationFamily.Happy), PropOverlayKind.Ball),
            FetchStage.ReturnIdle => new ActionVisualIntent(AnimationFamily.Idle),
            _ => new ActionVisualIntent(AnimationFamily.Idle)
        };
    }

    public static ActionVisualIntent ResolveActionVisualIntent(ActionDefinition actionDefinition)
    {
        var family = string.IsNullOrWhiteSpace(actionDefinition.OptionalAnimationFamily)
            ? MapAnimationStateToFamily(actionDefinition.AnimationState)
            : ParseAnimationFamily(actionDefinition.OptionalAnimationFamily);
        var overlay = ParsePropOverlay(actionDefinition.PropOverlay);
        var loopUntilStopped = family is AnimationFamily.HoldBall or AnimationFamily.CarryBallWalk or AnimationFamily.CarryBallRun;
        return new ActionVisualIntent(family, overlay, loopUntilStopped);
    }

    public IReadOnlyDictionary<string, double> BuildAverageNeedSnapshot(IReadOnlyList<PetActor> pets)
    {
        var livingPets = pets.Where(pet => !pet.IsDead).ToList();
        if (livingPets.Count == 0)
        {
            return new Dictionary<string, double>
            {
                ["hunger"] = 0,
                ["thirst"] = 0,
                ["energy"] = 0,
                ["cleanliness"] = 0,
                ["affection"] = 0,
                ["comfort"] = 0,
                ["health"] = 0,
                ["fitness"] = 0
            };
        }

        return new Dictionary<string, double>
        {
            ["hunger"] = livingPets.Average(pet => pet.Hunger),
            ["thirst"] = livingPets.Average(pet => pet.Thirst),
            ["energy"] = livingPets.Average(pet => pet.Energy),
            ["cleanliness"] = livingPets.Average(pet => pet.Cleanliness),
            ["affection"] = livingPets.Average(pet => pet.Affection),
            ["comfort"] = livingPets.Average(pet => pet.Comfort),
            ["health"] = livingPets.Average(pet => pet.Health),
            ["fitness"] = livingPets.Average(pet => pet.Fitness)
        };
    }

    public IReadOnlyList<PetStatusType> BuildAggregateStatuses(IReadOnlyList<PetActor> pets)
    {
        return pets
            .Where(pet => !pet.IsDead)
            .SelectMany(pet => pet.ActiveStatuses ?? [])
            .Distinct()
            .OrderBy(status => status.ToString(), StringComparer.Ordinal)
            .ToList();
    }

    public bool IsActionEnabled(string actionId, IReadOnlyList<PetActor> pets)
    {
        var livingPets = pets.Where(pet => !pet.IsDead).ToList();
        return actionId switch
        {
            "feed" => livingPets.Any(pet => pet.Hunger < 95 || HasCondition(pet, "malnutrition")),
            "water" => livingPets.Any(pet => pet.Thirst < 95),
            "rest" => livingPets.Any(pet => pet.Energy < 96 || pet.BehaviorState != PetBehaviorState.Home || HasCondition(pet, "exhaustion")),
            "play" => livingPets.Any(pet => pet.Affection < 95 || pet.Comfort < 95 || pet.Fitness < 92),
            "groom" => livingPets.Any(pet => pet.Cleanliness < 95 || HasCondition(pet, "dentalProblems") || HasCondition(pet, "dentalOvergrowth") || HasCondition(pet, "parasites")),
            "bath" => livingPets.Any(pet => pet.Cleanliness < 88 || HasCondition(pet, "skinInfections") || HasCondition(pet, "sheddingIssues") || HasCondition(pet, "parasites")),
            "medicine" => livingPets.Any(pet => pet.Health < 95 || HasStatus(pet, PetStatusType.Sick) || CountTreatableConditions(pet) > 0),
            "doctor" => livingPets.Any(pet => pet.Health < 85 || (pet.ActiveConditions?.Count ?? 0) > 0),
            "home" => livingPets.Any(pet => pet.BehaviorState != PetBehaviorState.Home),
            _ => true
        };
    }

    public string DescribePersonality(PetActor pet)
    {
        var personality = pet.Personality ?? new PetPersonalityProfile();
        var descriptors = new List<string>();

        if (personality.Playfulness >= 25)
        {
            descriptors.Add("playful");
        }
        else if (personality.Playfulness <= -25)
        {
            descriptors.Add("reserved");
        }

        if (personality.Cheerfulness >= 25)
        {
            descriptors.Add("bright");
        }
        else if (personality.Cheerfulness <= -25)
        {
            descriptors.Add("moody");
        }

        if (personality.CuddleNeed >= 25 || personality.SocialNeed >= 25)
        {
            descriptors.Add("clingy");
        }
        else if (personality.SocialNeed <= -25)
        {
            descriptors.Add("solitary");
        }

        if (personality.CleanlinessPreference >= 25)
        {
            descriptors.Add("neat");
        }

        if (personality.ActivityLevel >= 25)
        {
            descriptors.Add("active");
        }

        if (personality.Stubbornness >= 25)
        {
            descriptors.Add("headstrong");
        }

        return descriptors.Count == 0 ? "settling in" : string.Join(", ", descriptors.Take(3));
    }

    public string DescribeAging(PetActor pet)
    {
        var rate = CalculateAgingRate(pet);
        var lifePhase = pet.BiologicalAgeMinutes >= AgingThresholdMinutes
            ? "senior"
            : pet.AgeStage.ToString().ToLowerInvariant();
        var pace = rate switch
        {
            < 0.92 => "aging gently",
            > 1.25 => "aging fast",
            _ => "aging steady"
        };
        return $"{lifePhase} - {pace}";
    }

    private PetActor NormalizePet(PetActor pet)
    {
        var habitProfile = pet.HabitProfile ?? SeedHabitProfile(pet.AgeStage);
        var personality = pet.Personality ?? new PetPersonalityProfile();
        var activeConditions = pet.ActiveConditions ?? [];
        var biologicalAgeMinutes = pet.BiologicalAgeMinutes <= 0 && pet.AgeStage != PetAgeStage.Baby
            ? GetStartingBiologicalAgeMinutes(pet.AgeStage)
            : pet.BiologicalAgeMinutes;

        return pet with
        {
            BaseSpeed = pet.BaseSpeed <= 0 ? pet.Speed : pet.BaseSpeed,
            Fitness = pet.Fitness <= 0 ? 68 : pet.Fitness,
            BiologicalAgeMinutes = biologicalAgeMinutes,
            HabitProfile = habitProfile,
            Personality = personality,
            ActiveConditions = activeConditions,
            ActiveStatuses = pet.ActiveStatuses ?? []
        };
    }

    private PetActor TickVitals(PetActor pet, CompanionMode mode, double deltaSeconds)
    {
        var careDelta = deltaSeconds * CareDeltaSecondsToMinutes;
        var personality = pet.Personality ?? new PetPersonalityProfile();
        var elderFactor = pet.BiologicalAgeMinutes >= AgingThresholdMinutes ? 1.12 : 1.0;
        var passiveFactor = mode == CompanionMode.Passive ? 1.18 : 0.88;

        var hungerDecay = 1.8 * passiveFactor * elderFactor * (1.0 + Math.Max(-0.25, -personality.FoodLove / 320.0));
        var thirstDecay = 2.1 * passiveFactor * elderFactor;
        var energyDecay = (mode == CompanionMode.Passive ? 1.25 : 0.65) * (1.0 + Math.Max(0, personality.ActivityLevel) / 260.0 + (elderFactor - 1.0));
        var cleanlinessDecay = 0.65 * passiveFactor * (1.0 + Math.Max(0, -personality.CleanlinessPreference) / 300.0);
        var affectionDecay = 0.45 * (1.0 + Math.Max(0, personality.SocialNeed) / 280.0);
        var comfortBaseDelta = mode == CompanionMode.Passive ? -0.3 : 0.22;
        var comfortDelta = comfortBaseDelta + Math.Max(-0.15, personality.CuddleNeed / 700.0);
        var fitnessDelta = mode == CompanionMode.Passive ? 0.45 : -0.18;

        var hunger = Clamp(pet.Hunger - careDelta * hungerDecay);
        var thirst = Clamp(pet.Thirst - careDelta * thirstDecay);
        var energy = Clamp(pet.Energy - careDelta * energyDecay);
        var cleanliness = Clamp(pet.Cleanliness - careDelta * cleanlinessDecay);
        var affection = Clamp(pet.Affection - careDelta * affectionDecay);
        var comfort = Clamp(pet.Comfort + careDelta * comfortDelta);
        var fitness = Clamp(pet.Fitness + careDelta * fitnessDelta);

        if (energy < 18 || HasCondition(pet, "injury") || HasCondition(pet, "jointPain"))
        {
            fitness = Clamp(fitness - careDelta * 0.55);
        }

        var healthDelta = 0.0;
        if (hunger < 22)
        {
            healthDelta -= 1.2 * careDelta;
        }

        if (thirst < 20)
        {
            healthDelta -= 1.6 * careDelta;
        }

        if (cleanliness < 24)
        {
            healthDelta -= 0.9 * careDelta;
        }

        if (energy < 18)
        {
            healthDelta -= 0.8 * careDelta;
        }

        if (fitness < 20)
        {
            healthDelta -= 0.55 * careDelta;
        }

        if (comfort > 70 && hunger > 55 && thirst > 55)
        {
            healthDelta += 0.35 * careDelta;
        }

        healthDelta -= CalculateConditionHealthPenalty(pet.ActiveConditions ?? []) * careDelta;

        return pet with
        {
            Hunger = hunger,
            Thirst = thirst,
            Energy = energy,
            Cleanliness = cleanliness,
            Affection = affection,
            Comfort = comfort,
            Health = Clamp(pet.Health + healthDelta),
            Fitness = fitness
        };
    }

    private PetActor UpdateHabitProfile(PetActor pet, CompanionMode mode, double deltaSeconds)
    {
        var careDelta = deltaSeconds * CareDeltaSecondsToMinutes;
        var habits = pet.HabitProfile ?? new PetHabitProfile();
        var careAverage = (pet.Hunger + pet.Thirst + pet.Cleanliness + pet.Affection + pet.Comfort + pet.Energy + pet.Health + pet.Fitness) / 8.0;
        var stressTarget = Clamp(100 - careAverage + ((pet.ActiveConditions?.Count ?? 0) * 6));

        return pet with
        {
            HabitProfile = habits with
            {
                Nutrition = Approach(habits.Nutrition, pet.Hunger, 0.18 * careDelta),
                Hydration = Approach(habits.Hydration, pet.Thirst, 0.18 * careDelta),
                Exercise = Approach(habits.Exercise, pet.Fitness + (mode == CompanionMode.Passive ? 6 : 0), 0.16 * careDelta),
                Hygiene = Approach(habits.Hygiene, pet.Cleanliness, 0.16 * careDelta),
                Affection = Approach(habits.Affection, (pet.Affection + pet.Comfort) / 2.0, 0.16 * careDelta),
                Rest = Approach(habits.Rest, pet.Energy, 0.16 * careDelta),
                Medical = Approach(habits.Medical, Math.Min(100, pet.Health + ((pet.ActiveConditions?.Count ?? 0) == 0 ? 10 : -8)), 0.12 * careDelta),
                Stress = Approach(habits.Stress, stressTarget, 0.2 * careDelta)
            }
        };
    }

    private PetActor UpdateConditions(PetActor pet, double deltaSeconds)
    {
        var careDelta = deltaSeconds * CareDeltaSecondsToMinutes;
        var conditions = (pet.ActiveConditions ?? [])
            .ToDictionary(condition => condition.Id, condition => condition, StringComparer.OrdinalIgnoreCase);
        var habits = pet.HabitProfile ?? new PetHabitProfile();
        var lifestylePenalty = Math.Max(0, 50 - (habits.Nutrition + habits.Exercise + habits.Hygiene + habits.Rest) / 4.0);

        UpdateCondition(ref conditions, "obesity", GetLifestyleSeverity(pet.Hunger > 88 && pet.Fitness < 52, pet.Hunger > 95 || (pet.Hunger > 84 && pet.Fitness < 35), pet.Hunger > 98 && pet.Fitness < 22), false);
        UpdateCondition(ref conditions, "malnutrition", GetLifestyleSeverity(pet.Hunger < 26 || habits.Nutrition < 34, pet.Hunger < 18 || habits.Nutrition < 24, pet.Hunger < 10 || habits.Nutrition < 16), false);
        UpdateCondition(ref conditions, "depression", GetLifestyleSeverity((pet.Affection < 28 && pet.Comfort < 30) || habits.Affection < 34, pet.Affection < 18 || habits.Affection < 24, pet.Affection < 10 || habits.Stress > 70), false);
        UpdateCondition(ref conditions, "anxiety", GetLifestyleSeverity((pet.Comfort < 28 || habits.Stress > 56), pet.Comfort < 18 || habits.Stress > 68, pet.Comfort < 10 || habits.Stress > 82), false);
        UpdateCondition(ref conditions, "jointPain", GetLifestyleSeverity(pet.Fitness < 28 || pet.BiologicalAgeMinutes >= AgingThresholdMinutes, pet.Fitness < 18 || (pet.BiologicalAgeMinutes >= AgingThresholdMinutes && lifestylePenalty > 18), pet.Fitness < 10), false);
        UpdateCondition(ref conditions, "exhaustion", GetLifestyleSeverity(pet.Energy < 16, pet.Energy < 10, pet.Energy < 6), false);
        UpdateCondition(ref conditions, "injury", ReduceByTime(conditions, "injury", pet.Energy > 64 && pet.Health > 68 ? careDelta * 0.3 : 0), false);

        foreach (var innateCondition in conditions.Values.Where(condition => condition.IsInnate).ToList())
        {
            var targetSeverity = GetInnateConditionTargetSeverity(pet, innateCondition.Id);
            conditions[innateCondition.Id] = innateCondition with
            {
                Severity = ApproachSeverity(innateCondition.Severity, targetSeverity, careDelta * 0.4)
            };
        }

        return pet with { ActiveConditions = conditions.Values.OrderBy(condition => condition.Id, StringComparer.OrdinalIgnoreCase).ToList() };
    }

    private PetActor UpdatePersonality(PetActor pet, double deltaSeconds)
    {
        var careDelta = deltaSeconds * CareDeltaSecondsToMinutes;
        var habits = pet.HabitProfile ?? new PetHabitProfile();
        var personality = pet.Personality ?? new PetPersonalityProfile();
        var conditionLoad = (pet.ActiveConditions ?? []).Sum(condition => condition.Severity);

        return pet with
        {
            Personality = personality with
            {
                FoodLove = DriftTrait(personality.FoodLove, (55 - habits.Nutrition) * 0.8 + habits.FeedCount * 0.12, careDelta),
                CuddleNeed = DriftTrait(personality.CuddleNeed, (60 - habits.Affection) * 0.9 + (50 - habits.Rest) * 0.2, careDelta),
                CleanlinessPreference = DriftTrait(personality.CleanlinessPreference, (habits.Hygiene - 50) * 0.75, careDelta),
                ActivityLevel = DriftTrait(personality.ActivityLevel, (habits.Exercise - 50) * 0.9, careDelta),
                Cheerfulness = DriftTrait(personality.Cheerfulness, (habits.Affection + habits.Rest + habits.Nutrition + habits.Hydration) / 2.2 - habits.Stress - conditionLoad * 4, careDelta),
                SocialNeed = DriftTrait(personality.SocialNeed, (58 - habits.Affection) * 0.8 + (personality.CuddleNeed / 2.5), careDelta),
                Playfulness = DriftTrait(personality.Playfulness, (habits.Exercise + habits.Affection - 110) * 0.85, careDelta),
                Stubbornness = DriftTrait(personality.Stubbornness, habits.Stress - habits.Medical + conditionLoad * 3, careDelta)
            }
        };
    }

    private PetActor UpdateBiologicalAge(PetActor pet, double deltaSeconds)
    {
        return pet with
        {
            BiologicalAgeMinutes = pet.BiologicalAgeMinutes + (deltaSeconds / 60.0) * CalculateAgingRate(pet)
        };
    }

    private PetActor RefreshDerivedValues(PetActor pet)
    {
        if (pet.IsDead)
        {
            return pet with
            {
                Speed = Math.Max(24, pet.BaseSpeed * 0.42),
                BehaviorState = PetBehaviorState.Roaming,
                ActiveFetchSequence = null
            };
        }

        return pet with
        {
            Speed = CalculatePetSpeedFromState(pet.BaseSpeed <= 0 ? pet.Speed : pet.BaseSpeed, pet.AgeStage, pet.Gender, pet.Fitness, pet.BiologicalAgeMinutes, pet.ActiveConditions)
        };
    }

    private static PetActor ApplyDeathIfNeeded(PetActor pet, DateTimeOffset now)
    {
        if (pet.IsDead || (pet.Health > 0 && pet.Hunger > 0 && pet.Thirst > 0))
        {
            return pet;
        }

        var memorialX = pet.CurrentX == 0 ? pet.HomeX : pet.CurrentX;
        var memorialY = pet.CurrentY == 0 ? pet.HomeY : pet.CurrentY;
        return pet with
        {
            IsDead = true,
            IsGhost = false,
            DiedAtUtc = now,
            MemorialExpiresAtUtc = now.AddDays(MemorialLifetimeDays),
            MemorialObjectId = "memorial_object",
            MemorialX = memorialX,
            MemorialY = memorialY,
            Health = 0,
            Hunger = Math.Max(0, pet.Hunger),
            Thirst = Math.Max(0, pet.Thirst),
            Speed = Math.Max(24, pet.BaseSpeed * 0.42),
            TargetX = memorialX,
            TargetY = memorialY,
            CurrentX = memorialX,
            CurrentY = memorialY,
            BehaviorState = PetBehaviorState.Roaming,
            ActiveFetchSequence = null,
            CurrentActionVisualIntent = null,
            OverrideAnimationState = PetAnimationState.Sad,
            OverrideAnimationEndsAtUtc = now.AddSeconds(DeathToGhostTransitionSeconds),
            AnimationStartedAtUtc = now,
            LastActionId = "death",
            LastActionAtUtc = now
        };
    }

    private static PetActor UpdateDeadLifecycle(PetActor pet, DateTimeOffset now)
    {
        if (!pet.IsDead)
        {
            return pet;
        }

        var diedAt = pet.DiedAtUtc ?? now;
        var shouldGhost = (now - diedAt).TotalSeconds >= DeathToGhostTransitionSeconds;
        var memorialExpired = pet.MemorialExpiresAtUtc is not null && pet.MemorialExpiresAtUtc <= now;
        return pet with
        {
            IsGhost = pet.IsGhost || shouldGhost,
            DiedAtUtc = diedAt,
            MemorialObjectId = memorialExpired ? string.Empty : pet.MemorialObjectId,
            MemorialExpiresAtUtc = memorialExpired ? null : pet.MemorialExpiresAtUtc,
            Speed = Math.Max(24, pet.BaseSpeed * 0.42),
            BehaviorState = PetBehaviorState.Roaming,
            ActiveFetchSequence = null,
            CurrentActionVisualIntent = null
        };
    }

    private PetActor ApplyAction(ActionDefinition actionDefinition, PetActor pet, DateTimeOffset now)
    {
        var actionId = actionDefinition.Id;
        var normalized = NormalizePet(pet);
        if (normalized.IsDead)
        {
            return RefreshPresentationState(normalized, CompanionMode.Focused, now);
        }

        var habits = normalized.HabitProfile ?? new PetHabitProfile();
        var personality = normalized.Personality ?? new PetPersonalityProfile();
        var conditions = (normalized.ActiveConditions ?? []).ToDictionary(condition => condition.Id, condition => condition, StringComparer.OrdinalIgnoreCase);

        var updated = actionId switch
        {
            "feed" => normalized with
            {
                Hunger = Clamp(normalized.Hunger + 34),
                Comfort = Clamp(normalized.Comfort + 6),
                Affection = Clamp(normalized.Affection + 3),
                Health = Clamp(normalized.Health + (HasCondition(normalized, "malnutrition") ? 4 : 1))
            },
            "water" => normalized with
            {
                Thirst = Clamp(normalized.Thirst + 38),
                Comfort = Clamp(normalized.Comfort + 4),
                Health = Clamp(normalized.Health + 2)
            },
            "rest" => normalized with
            {
                Energy = Clamp(normalized.Energy + 28),
                Comfort = Clamp(normalized.Comfort + 10),
                Health = Clamp(normalized.Health + 4),
                TargetX = normalized.HomeX,
                TargetY = normalized.HomeY,
                BehaviorState = PetBehaviorState.Recalling
            },
            "play" => normalized with
            {
                Affection = Clamp(normalized.Affection + 18),
                Comfort = Clamp(normalized.Comfort + 12),
                Fitness = Clamp(normalized.Fitness + 12),
                Energy = Clamp(normalized.Energy - 8),
                Hunger = Clamp(normalized.Hunger - 5)
            },
            "groom" => normalized with
            {
                Cleanliness = Clamp(normalized.Cleanliness + 18),
                Affection = Clamp(normalized.Affection + 6),
                Health = Clamp(normalized.Health + 2)
            },
            "bath" => normalized with
            {
                Cleanliness = Clamp(normalized.Cleanliness + 34),
                Comfort = Clamp(normalized.Comfort + 2),
                Health = Clamp(normalized.Health + 4)
            },
            "medicine" => normalized with
            {
                Health = Clamp(normalized.Health + 14),
                Comfort = Clamp(normalized.Comfort - 2)
            },
            "doctor" => normalized with
            {
                Health = Clamp(normalized.Health + 24),
                Comfort = Clamp(normalized.Comfort - 4)
            },
            "home" => normalized with
            {
                Comfort = Clamp(normalized.Comfort + 8),
                TargetX = normalized.HomeX,
                TargetY = normalized.HomeY,
                BehaviorState = PetBehaviorState.Recalling
            },
            _ => normalized
        };

        habits = actionId switch
        {
            "feed" => habits with { Nutrition = Clamp(habits.Nutrition + 8), Stress = Clamp(habits.Stress - 3), FeedCount = habits.FeedCount + 1 },
            "water" => habits with { Hydration = Clamp(habits.Hydration + 10), Stress = Clamp(habits.Stress - 2), WaterCount = habits.WaterCount + 1 },
            "rest" => habits with { Rest = Clamp(habits.Rest + 10), Stress = Clamp(habits.Stress - 6), RestCount = habits.RestCount + 1 },
            "play" => habits with { Exercise = Clamp(habits.Exercise + 12), Affection = Clamp(habits.Affection + 8), Stress = Clamp(habits.Stress - 4), PlayCount = habits.PlayCount + 1 },
            "groom" => habits with { Hygiene = Clamp(habits.Hygiene + 9), Affection = Clamp(habits.Affection + 3), GroomCount = habits.GroomCount + 1 },
            "bath" => habits with { Hygiene = Clamp(habits.Hygiene + 14), Stress = Clamp(habits.Stress - 2), BathCount = habits.BathCount + 1 },
            "medicine" => habits with { Medical = Clamp(habits.Medical + 9), Stress = Clamp(habits.Stress + 1), MedicineCount = habits.MedicineCount + 1 },
            "doctor" => habits with { Medical = Clamp(habits.Medical + 14), Stress = Clamp(habits.Stress + 2), DoctorCount = habits.DoctorCount + 1 },
            _ => habits
        };

        personality = actionId switch
        {
            "feed" => personality with { FoodLove = ClampTrait(personality.FoodLove + 1.2), Cheerfulness = ClampTrait(personality.Cheerfulness + 0.4) },
            "rest" => personality with { CuddleNeed = ClampTrait(personality.CuddleNeed - 0.6), Stubbornness = ClampTrait(personality.Stubbornness - 0.5) },
            "play" => personality with { Playfulness = ClampTrait(personality.Playfulness + 1.5), ActivityLevel = ClampTrait(personality.ActivityLevel + 1.1), Cheerfulness = ClampTrait(personality.Cheerfulness + 0.9) },
            "groom" or "bath" => personality with { CleanlinessPreference = ClampTrait(personality.CleanlinessPreference + 1.0) },
            "medicine" or "doctor" => personality with { Stubbornness = ClampTrait(personality.Stubbornness - 0.5) },
            _ => personality
        };

        conditions = actionId switch
        {
            "play" => ApplyPlaySideEffects(conditions, normalized),
            "rest" => ReduceSelectedConditions(conditions, ["exhaustion", "anxiety"], 1),
            "groom" => ReduceSelectedConditions(conditions, ["parasites", "dentalProblems", "dentalOvergrowth"], 1),
            "bath" => ReduceSelectedConditions(conditions, ["parasites", "skinInfections", "sheddingIssues"], 1),
            "medicine" => ReduceTreatableConditions(conditions, 1),
            "doctor" => ReduceAllConditions(conditions, 1),
            _ => conditions
        };

        var animation = actionDefinition.AnimationState;
        var visualIntent = ResolveActionVisualIntent(actionDefinition);

        updated = updated with
        {
            HabitProfile = habits,
            Personality = personality,
            ActiveConditions = conditions.Values.OrderBy(condition => condition.Id, StringComparer.OrdinalIgnoreCase).ToList(),
            OverrideAnimationState = animation,
            OverrideAnimationEndsAtUtc = now.AddSeconds(2.4),
            LastActionId = actionId,
            LastActionAtUtc = now,
            AnimationStartedAtUtc = now,
            CurrentActionVisualIntent = visualIntent
        };

        return RefreshPresentationState(RefreshDerivedValues(updated), CompanionMode.Focused, now);
    }

    private static ActionDefinition BuildImplicitActionDefinition(string actionId)
    {
        var animationState = actionId switch
        {
            "feed" => PetAnimationState.Eat,
            "water" => PetAnimationState.Drink,
            "rest" or "home" => PetAnimationState.Sleep,
            "play" => PetAnimationState.Happy,
            "groom" => PetAnimationState.Groom,
            "bath" => PetAnimationState.Bathe,
            "medicine" => PetAnimationState.Sick,
            "doctor" => PetAnimationState.Doctor,
            _ => PetAnimationState.Idle
        };
        return new ActionDefinition(actionId, actionId, string.Empty, AnimationState: animationState);
    }

    private static ActionDefinition BuildWaterActionDefinition(bool includeOptionalFamily)
    {
        return new ActionDefinition(
            "water",
            "Water",
            "Restores thirst.",
            AnimationState: PetAnimationState.Eat,
            OptionalAnimationFamily: includeOptionalFamily ? "drink" : null);
    }

    private static PetActor SetFetchStage(
        PetActor pet,
        FetchStage stage,
        DateTimeOffset now,
        IReadOnlySet<AnimationFamily> availableOptionalFamilies)
    {
        if (stage == FetchStage.None)
        {
            return pet with
            {
                ActiveFetchSequence = null,
                CurrentActionVisualIntent = new ActionVisualIntent(AnimationFamily.Idle),
                OverrideAnimationState = null,
                OverrideAnimationEndsAtUtc = null,
                LastActionId = "fetch_ball",
                LastActionAtUtc = now,
                AnimationStartedAtUtc = now
            };
        }

        var sequenceStartedAt = pet.ActiveFetchSequence?.SequenceStartedAtUtc ?? now;
        var intent = ResolveFetchStageIntent(stage, availableOptionalFamilies);
        return pet with
        {
            ActiveFetchSequence = new FetchSequenceState(stage, now, sequenceStartedAt),
            CurrentActionVisualIntent = intent,
            OverrideAnimationState = MapAnimationFamilyToState(intent.Family),
            OverrideAnimationEndsAtUtc = now.AddSeconds(FetchStageDurations.TryGetValue(stage, out var duration) ? duration : 0.5),
            LastActionId = "fetch_ball",
            LastActionAtUtc = now,
            AnimationStartedAtUtc = now
        };
    }

    private static FetchStage GetNextFetchStage(FetchStage stage)
    {
        return stage switch
        {
            FetchStage.MoveToBall => FetchStage.Pickup,
            FetchStage.Pickup => FetchStage.Hold,
            FetchStage.Hold => FetchStage.CarryWalk,
            FetchStage.CarryWalk => FetchStage.CarryRun,
            FetchStage.CarryRun => FetchStage.Drop,
            FetchStage.Drop => FetchStage.ReturnIdle,
            FetchStage.ReturnIdle => FetchStage.None,
            _ => FetchStage.None
        };
    }

    private static AnimationFamily ChooseFetchFamily(
        AnimationFamily preferred,
        IReadOnlySet<AnimationFamily> available,
        params AnimationFamily[] fallbacks)
    {
        if (available.Count == 0 || available.Contains(preferred))
        {
            return preferred;
        }

        return fallbacks.FirstOrDefault(available.Contains) is { } fallback && !EqualityComparer<AnimationFamily>.Default.Equals(fallback, default)
            ? fallback
            : fallbacks.LastOrDefault(AnimationFamily.Idle);
    }

    private static PetAnimationState MapAnimationFamilyToState(AnimationFamily family)
    {
        return family switch
        {
            AnimationFamily.Walk or AnimationFamily.CarryBallWalk or AnimationFamily.CarryBallRun => PetAnimationState.Walk,
            AnimationFamily.Eat => PetAnimationState.Eat,
            AnimationFamily.Drink => PetAnimationState.Drink,
            AnimationFamily.Sad => PetAnimationState.Sad,
            AnimationFamily.Sleep => PetAnimationState.Sleep,
            AnimationFamily.Sick => PetAnimationState.Sick,
            AnimationFamily.Bathe => PetAnimationState.Bathe,
            AnimationFamily.Happy or AnimationFamily.PlayBall or AnimationFamily.HoldBall or AnimationFamily.PickupBall or AnimationFamily.DropBall => PetAnimationState.Happy,
            _ => PetAnimationState.Idle
        };
    }

    private static AnimationFamily MapAnimationStateToFamily(PetAnimationState animationState)
    {
        return animationState switch
        {
            PetAnimationState.Walk => AnimationFamily.Walk,
            PetAnimationState.Eat => AnimationFamily.Eat,
            PetAnimationState.Drink => AnimationFamily.Drink,
            PetAnimationState.Happy => AnimationFamily.Happy,
            PetAnimationState.Groom => AnimationFamily.Happy,
            PetAnimationState.Sad => AnimationFamily.Sad,
            PetAnimationState.Sleep => AnimationFamily.Sleep,
            PetAnimationState.Sick => AnimationFamily.Sick,
            PetAnimationState.Doctor => AnimationFamily.Sick,
            PetAnimationState.Bathe => AnimationFamily.Bathe,
            _ => AnimationFamily.Idle
        };
    }

    private static AnimationFamily ParseAnimationFamily(string value)
    {
        return NormalizeToken(value) switch
        {
            "idle" => AnimationFamily.Idle,
            "walk" => AnimationFamily.Walk,
            "eat" => AnimationFamily.Eat,
            "happy" => AnimationFamily.Happy,
            "sad" => AnimationFamily.Sad,
            "sleep" => AnimationFamily.Sleep,
            "sick" => AnimationFamily.Sick,
            "bathe" => AnimationFamily.Bathe,
            "drink" => AnimationFamily.Drink,
            "playball" => AnimationFamily.PlayBall,
            "holdball" => AnimationFamily.HoldBall,
            "pickupball" => AnimationFamily.PickupBall,
            "dropball" => AnimationFamily.DropBall,
            "carryballwalk" => AnimationFamily.CarryBallWalk,
            "carryballrun" => AnimationFamily.CarryBallRun,
            _ => AnimationFamily.Idle
        };
    }

    private static PropOverlayKind ParsePropOverlay(string? value)
    {
        return NormalizeToken(value) switch
        {
            "ball" => PropOverlayKind.Ball,
            "waterbowl" => PropOverlayKind.WaterBowl,
            "fooddish" => PropOverlayKind.FoodDish,
            "medicineitem" => PropOverlayKind.MedicineItem,
            "groomingitem" => PropOverlayKind.GroomingItem,
            "bathtowel" => PropOverlayKind.BathTowel,
            _ => PropOverlayKind.None
        };
    }

    private static string NormalizeToken(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("_", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Trim()
                .ToLowerInvariant();
    }

    private static PetActor MoveTowardHome(PetActor pet, DateTimeOffset now, double deltaSeconds)
    {
        var dx = pet.HomeX - pet.CurrentX;
        var dy = pet.HomeY - pet.CurrentY;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance <= 3.0)
        {
            return pet with
            {
                CurrentX = pet.HomeX,
                CurrentY = pet.HomeY,
                TargetX = pet.HomeX,
                TargetY = pet.HomeY,
                BehaviorState = PetBehaviorState.Home,
                NextDecisionAtUtc = now.AddSeconds(1.6)
            };
        }

        var step = Math.Min(distance, pet.Speed * deltaSeconds);
        var nx = pet.CurrentX + dx / distance * step;
        var ny = pet.CurrentY + dy / distance * step;
        return pet with
        {
            CurrentX = nx,
            CurrentY = ny,
            TargetX = pet.HomeX,
            TargetY = pet.HomeY,
            BehaviorState = PetBehaviorState.Recalling,
            FacingDirection = dx < 0 ? PetFacingDirection.Left : PetFacingDirection.Right
        };
    }

    private PetActor MoveWithinRoamBand(PetActor pet, RectInt roamBandBounds, DateTimeOffset now, double deltaSeconds)
    {
        var petWithTarget = pet;
        if (pet.BehaviorState != PetBehaviorState.Roaming ||
            (Math.Abs(pet.CurrentX - pet.TargetX) < 3.0 && now >= pet.NextDecisionAtUtc))
        {
            var minX = roamBandBounds.X + 48;
            var maxX = roamBandBounds.Right - 48;
            var nextX = _random.Next(minX, Math.Max(minX + 1, maxX));
            petWithTarget = pet with
            {
                TargetX = nextX,
                TargetY = roamBandBounds.Bottom - 10,
                BehaviorState = PetBehaviorState.Roaming,
                NextDecisionAtUtc = now.AddSeconds(_random.NextDouble() * 1.5 + 0.7)
            };
        }

        var dx = petWithTarget.TargetX - petWithTarget.CurrentX;
        var dy = petWithTarget.TargetY - petWithTarget.CurrentY;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance <= 2.0)
        {
            return petWithTarget with
            {
                CurrentX = petWithTarget.TargetX,
                CurrentY = petWithTarget.TargetY
            };
        }

        var step = Math.Min(distance, petWithTarget.Speed * deltaSeconds);
        return petWithTarget with
        {
            CurrentX = petWithTarget.CurrentX + dx / distance * step,
            CurrentY = petWithTarget.CurrentY + dy / distance * step,
            FacingDirection = dx < 0 ? PetFacingDirection.Left : PetFacingDirection.Right
        };
    }

    private static PetActor RefreshPresentationState(PetActor pet, CompanionMode mode, DateTimeOffset now)
    {
        pet = UpdateDeadLifecycle(pet, now);
        var overrideAnimation = pet.OverrideAnimationEndsAtUtc is not null && pet.OverrideAnimationEndsAtUtc > now
            ? pet.OverrideAnimationState
            : null;

        var nextAnimation = overrideAnimation ?? ResolveDefaultAnimation(pet, mode);
        var nextStatuses = BuildStatuses(pet);
        return pet with
        {
            CurrentAnimationState = nextAnimation,
            AnimationStartedAtUtc = nextAnimation == pet.CurrentAnimationState ? pet.AnimationStartedAtUtc : now,
            OverrideAnimationState = overrideAnimation,
            OverrideAnimationEndsAtUtc = overrideAnimation is null ? null : pet.OverrideAnimationEndsAtUtc,
            ActiveStatuses = nextStatuses
        };
    }

    private static PetAnimationState ResolveDefaultAnimation(PetActor pet, CompanionMode mode)
    {
        if (pet.IsDead)
        {
            return pet.IsGhost ? PetAnimationState.Idle : PetAnimationState.Sad;
        }

        if (HasStatus(pet, PetStatusType.Sick))
        {
            return PetAnimationState.Sick;
        }

        if (mode is CompanionMode.Focused or CompanionMode.Pinned)
        {
            if (pet.BehaviorState == PetBehaviorState.Recalling && IsMovingTowardTarget(pet))
            {
                return PetAnimationState.Walk;
            }

            if (pet.Energy < 24)
            {
                return PetAnimationState.Sleep;
            }

            if (pet.Comfort > 76 && pet.Affection > 76 && pet.Fitness > 60)
            {
                return PetAnimationState.Happy;
            }

            if (pet.Hunger < 28 || pet.Thirst < 28 || pet.Affection < 24)
            {
                return PetAnimationState.Sad;
            }

            return PetAnimationState.Idle;
        }

        return IsMovingTowardTarget(pet)
            ? PetAnimationState.Walk
            : PetAnimationState.Idle;
    }

    private static bool IsMovingTowardTarget(PetActor pet)
    {
        var dx = pet.TargetX - pet.CurrentX;
        var dy = pet.TargetY - pet.CurrentY;
        return Math.Sqrt(dx * dx + dy * dy) > 2.5;
    }

    private static int StarterRosterScore(string speciesId)
    {
        var daySeed = DateTimeOffset.UtcNow.UtcDateTime.Date.DayOfYear;
        var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(speciesId);
        return HashCode.Combine(daySeed, hash);
    }

    private static IReadOnlyList<PetStatusType> BuildStatuses(PetActor pet)
    {
        var statuses = new List<PetStatusType>();
        if (pet.IsDead)
        {
            statuses.Add(pet.IsGhost ? PetStatusType.Ghost : PetStatusType.Dead);
            return statuses;
        }

        if (pet.Hunger < 35 || HasCondition(pet, "malnutrition"))
        {
            statuses.Add(PetStatusType.Hungry);
        }

        if (pet.Thirst < 35)
        {
            statuses.Add(PetStatusType.Thirsty);
        }

        if (pet.Energy < 30 || HasCondition(pet, "exhaustion"))
        {
            statuses.Add(PetStatusType.Sleepy);
        }

        if (pet.Health < 58 || (pet.ActiveConditions?.Any(condition => condition.Severity >= 2) ?? false))
        {
            statuses.Add(PetStatusType.Sick);
        }

        if (pet.Cleanliness < 38 || HasCondition(pet, "skinInfections") || HasCondition(pet, "parasites"))
        {
            statuses.Add(PetStatusType.Dirty);
        }

        if (pet.Affection < 34 || HasCondition(pet, "anxiety") || HasCondition(pet, "depression"))
        {
            statuses.Add(PetStatusType.Lonely);
        }

        if (pet.Comfort > 76 && (pet.HabitProfile?.Stress ?? 0) < 32)
        {
            statuses.Add(PetStatusType.Comforted);
        }

        if (pet.Hunger > 70 && pet.Thirst > 68 && pet.Affection > 68 && pet.Health > 72 && pet.Fitness > 60)
        {
            statuses.Add(PetStatusType.Happy);
        }

        return statuses;
    }

    private static bool HasStatus(PetActor pet, PetStatusType status)
    {
        return pet.ActiveStatuses?.Contains(status) == true;
    }

    private static bool HasCondition(PetActor pet, string conditionId)
    {
        return pet.ActiveConditions?.Any(condition => string.Equals(condition.Id, conditionId, StringComparison.OrdinalIgnoreCase) && condition.Severity > 0) == true;
    }

    private static int CountTreatableConditions(PetActor pet)
    {
        return (pet.ActiveConditions ?? []).Count(condition => MedicineTreatableConditions.Contains(condition.Id));
    }

    private static PetAgeStage ResolveAgeStage(double biologicalAgeMinutes)
    {
        if (biologicalAgeMinutes < BabyToTeenAgeMinutes)
        {
            return PetAgeStage.Baby;
        }

        if (biologicalAgeMinutes < TeenToAdultAgeMinutes)
        {
            return PetAgeStage.Teen;
        }

        if (biologicalAgeMinutes >= AgingThresholdMinutes)
        {
            return PetAgeStage.Senior;
        }

        return PetAgeStage.Adult;
    }

    private static PetActor AdvanceLifecycle(PetActor pet, DateTimeOffset now)
    {
        var nextAgeStage = ResolveAgeStage(pet.BiologicalAgeMinutes);
        if (nextAgeStage == pet.AgeStage)
        {
            return pet with
            {
                AgeStageStartedAtUtc = pet.AgeStageStartedAtUtc == default ? now : pet.AgeStageStartedAtUtc
            };
        }

        return pet with
        {
            AgeStage = nextAgeStage,
            AgeStageStartedAtUtc = now
        };
    }

    private static double GetAgeSpeedFactor(PetAgeStage ageStage)
    {
        return ageStage switch
        {
            PetAgeStage.Baby => 0.82,
            PetAgeStage.Teen => 1.05,
            PetAgeStage.Senior => 0.9,
            _ => 1.0
        };
    }

    private static double GetGenderSpeedFactor(PetGender gender)
    {
        return gender == PetGender.Male ? 1.04 : 0.98;
    }

    private static double CalculatePetSpeedFromState(
        double baseSpeed,
        PetAgeStage ageStage,
        PetGender gender,
        double fitness,
        double biologicalAgeMinutes,
        IReadOnlyList<PetConditionRecord>? conditions)
    {
        var elderFactor = biologicalAgeMinutes >= AgingThresholdMinutes ? 0.82 : 1.0;
        var fitnessFactor = 0.78 + Clamp(fitness) / 450.0;
        var conditionPenalty = 0.0;
        foreach (var condition in conditions ?? [])
        {
            if (condition.Severity <= 0)
            {
                continue;
            }

            switch (condition.Id)
            {
                case "jointPain":
                case "jointStiffness":
                    conditionPenalty += 0.05 * condition.Severity;
                    break;
                case "exhaustion":
                case "injury":
                    conditionPenalty += 0.04 * condition.Severity;
                    break;
                case "obesity":
                case "footProblems":
                    conditionPenalty += 0.03 * condition.Severity;
                    break;
            }
        }

        return Math.Max(36, baseSpeed * GetAgeSpeedFactor(ageStage) * GetGenderSpeedFactor(gender) * elderFactor * fitnessFactor * Math.Max(0.55, 1.0 - conditionPenalty));
    }

    public double CalculateAgingRate(PetActor pet)
    {
        var habits = pet.HabitProfile ?? new PetHabitProfile();
        var personality = pet.Personality ?? new PetPersonalityProfile();
        var careAverage = (habits.Nutrition + habits.Hydration + habits.Exercise + habits.Hygiene + habits.Affection + habits.Rest + habits.Medical) / 7.0;
        var conditionPressure = (pet.ActiveConditions ?? []).Sum(condition => condition.IsInnate ? 0.05 * condition.Severity : 0.08 * condition.Severity);
        var neglectPressure = Math.Max(0, 58 - careAverage) / 120.0;
        var careBonus = Math.Max(0, careAverage - 72) / 180.0;
        var healthPressure = Math.Max(0, 72 - pet.Health) / 180.0;
        var stressPressure = habits.Stress / 220.0;
        var cheerfulnessBonus = Math.Max(0, personality.Cheerfulness) / 450.0;
        return Math.Clamp(1.0 + conditionPressure + neglectPressure + healthPressure + stressPressure - careBonus - cheerfulnessBonus, 0.72, 1.95);
    }

    private static PetHabitProfile SeedHabitProfile(PetAgeStage ageStage)
    {
        return ageStage switch
        {
            PetAgeStage.Baby => new PetHabitProfile(76, 76, 58, 72, 74, 72, 74, 14),
            PetAgeStage.Teen => new PetHabitProfile(72, 72, 68, 70, 68, 66, 70, 18),
            PetAgeStage.Senior => new PetHabitProfile(68, 68, 54, 70, 72, 78, 66, 24),
            _ => new PetHabitProfile()
        };
    }

    private static PetPersonalityProfile SeedPersonality(PetPersonalityProfile? seed, PetAgeStage ageStage, PetGender gender)
    {
        var personality = seed ?? new PetPersonalityProfile();
        var ageBias = ageStage switch
        {
            PetAgeStage.Baby => new PetPersonalityProfile(4, 8, 0, -6, 4, 8, 6, -6),
            PetAgeStage.Teen => new PetPersonalityProfile(0, 2, 0, 8, 2, 4, 8, 2),
            PetAgeStage.Senior => new PetPersonalityProfile(-2, 8, 3, -8, 2, 8, -4, 2),
            _ => new PetPersonalityProfile()
        };
        var genderBias = gender == PetGender.Male
            ? new PetPersonalityProfile(1, -1, 0, 2, 0, 0, 1, 2)
            : new PetPersonalityProfile(0, 2, 1, -1, 1, 2, 0, -1);

        return new PetPersonalityProfile(
            ClampTrait(personality.FoodLove + ageBias.FoodLove + genderBias.FoodLove),
            ClampTrait(personality.CuddleNeed + ageBias.CuddleNeed + genderBias.CuddleNeed),
            ClampTrait(personality.CleanlinessPreference + ageBias.CleanlinessPreference + genderBias.CleanlinessPreference),
            ClampTrait(personality.ActivityLevel + ageBias.ActivityLevel + genderBias.ActivityLevel),
            ClampTrait(personality.Cheerfulness + ageBias.Cheerfulness + genderBias.Cheerfulness),
            ClampTrait(personality.SocialNeed + ageBias.SocialNeed + genderBias.SocialNeed),
            ClampTrait(personality.Playfulness + ageBias.Playfulness + genderBias.Playfulness),
            ClampTrait(personality.Stubbornness + ageBias.Stubbornness + genderBias.Stubbornness));
    }

    private static IReadOnlyList<PetConditionRecord> EnsureInnateCondition(string innateConditionId, IReadOnlyList<PetConditionRecord> conditions)
    {
        var next = conditions.ToList();
        if (!string.IsNullOrWhiteSpace(innateConditionId) &&
            next.All(condition => !string.Equals(condition.Id, innateConditionId, StringComparison.OrdinalIgnoreCase)))
        {
            next.Add(new PetConditionRecord(innateConditionId, 1, true));
        }

        return next;
    }

    private static double GetStartingBiologicalAgeMinutes(PetAgeStage ageStage)
    {
        return ageStage switch
        {
            PetAgeStage.Baby => 0,
            PetAgeStage.Teen => BabyToTeenAgeMinutes + 12,
            PetAgeStage.Senior => AgingThresholdMinutes + 18,
            _ => TeenToAdultAgeMinutes + 18
        };
    }

    private static double CalculateConditionHealthPenalty(IReadOnlyList<PetConditionRecord> conditions)
    {
        var penalty = 0.0;
        foreach (var condition in conditions)
        {
            penalty += condition.IsInnate ? 0.03 * condition.Severity : 0.05 * condition.Severity;
        }

        return penalty;
    }

    private static double DriftTrait(double currentValue, double targetSignal, double deltaSeconds)
    {
        var target = Math.Clamp(targetSignal, -100, 100);
        return ClampTrait(Approach(currentValue, target, 0.04 * deltaSeconds));
    }

    private static double ClampTrait(double value)
    {
        return Math.Clamp(value, -100, 100);
    }

    private static double Clamp(double value)
    {
        return Math.Clamp(value, 0, 100);
    }

    private static double Approach(double current, double target, double rate)
    {
        return current + (target - current) * Math.Clamp(rate, 0, 1);
    }

    private static int GetLifestyleSeverity(bool mild, bool moderate, bool severe)
    {
        if (severe)
        {
            return 3;
        }

        if (moderate)
        {
            return 2;
        }

        if (mild)
        {
            return 1;
        }

        return 0;
    }

    private static int ReduceByTime(Dictionary<string, PetConditionRecord> conditions, string conditionId, double amount)
    {
        if (!conditions.TryGetValue(conditionId, out var current))
        {
            return 0;
        }

        var severity = current.Severity - (int)Math.Floor(amount);
        if (severity <= 0)
        {
            conditions.Remove(conditionId);
            return 0;
        }

        conditions[conditionId] = current with { Severity = severity };
        return severity;
    }

    private static int ApproachSeverity(int currentSeverity, int targetSeverity, double deltaSeconds)
    {
        if (currentSeverity == targetSeverity)
        {
            return currentSeverity;
        }

        var step = Math.Max(1, (int)Math.Round(deltaSeconds));
        return currentSeverity < targetSeverity
            ? Math.Min(targetSeverity, currentSeverity + step)
            : Math.Max(targetSeverity, currentSeverity - step);
    }

    private static void UpdateCondition(ref Dictionary<string, PetConditionRecord> conditions, string id, int severity, bool isInnate)
    {
        if (severity <= 0)
        {
            conditions.Remove(id);
            return;
        }

        conditions[id] = new PetConditionRecord(id, severity, isInnate);
    }

    private static int GetInnateConditionTargetSeverity(PetActor pet, string conditionId)
    {
        return conditionId switch
        {
            "respiratoryProblems" => GetLifestyleSeverity(pet.Thirst < 46 || pet.Energy < 46, pet.Health < 54, pet.Health < 34),
            "parasites" => GetLifestyleSeverity(pet.Cleanliness < 58, pet.Cleanliness < 38, pet.Cleanliness < 20),
            "dentalProblems" => GetLifestyleSeverity(pet.Hunger < 54, pet.Hunger < 34, pet.Hunger < 20),
            "sheddingIssues" => GetLifestyleSeverity(pet.Cleanliness < 60 || pet.Comfort < 56, pet.Cleanliness < 42 || pet.Comfort < 34, pet.Cleanliness < 24),
            "jointStiffness" => GetLifestyleSeverity(pet.Fitness < 58, pet.Fitness < 38, pet.Fitness < 22),
            "skinInfections" => GetLifestyleSeverity(pet.Cleanliness < 60, pet.Cleanliness < 36, pet.Cleanliness < 18),
            "viralSusceptibility" => GetLifestyleSeverity(pet.Health < 68, pet.Health < 48, pet.Health < 30),
            "dentalOvergrowth" => GetLifestyleSeverity(pet.Hunger < 58, pet.Hunger < 40, pet.Hunger < 24),
            "footProblems" => GetLifestyleSeverity(pet.Fitness < 54 || pet.Cleanliness < 54, pet.Fitness < 34 || pet.Cleanliness < 34, pet.Fitness < 18),
            _ => 1
        };
    }

    private static Dictionary<string, PetConditionRecord> ReduceSelectedConditions(
        Dictionary<string, PetConditionRecord> conditions,
        IReadOnlyList<string> targets,
        int amount)
    {
        foreach (var target in targets)
        {
            if (!conditions.TryGetValue(target, out var current))
            {
                continue;
            }

            var nextSeverity = current.Severity - amount;
            if (nextSeverity <= 0)
            {
                conditions.Remove(target);
            }
            else
            {
                conditions[target] = current with { Severity = nextSeverity };
            }
        }

        return conditions;
    }

    private static Dictionary<string, PetConditionRecord> ReduceTreatableConditions(Dictionary<string, PetConditionRecord> conditions, int amount)
    {
        foreach (var condition in conditions.Values.Where(condition => MedicineTreatableConditions.Contains(condition.Id)).ToList())
        {
            var nextSeverity = condition.Severity - amount;
            if (nextSeverity <= 0)
            {
                conditions.Remove(condition.Id);
            }
            else
            {
                conditions[condition.Id] = condition with { Severity = nextSeverity };
            }
        }

        return conditions;
    }

    private static Dictionary<string, PetConditionRecord> ReduceAllConditions(Dictionary<string, PetConditionRecord> conditions, int amount)
    {
        foreach (var condition in conditions.Values.ToList())
        {
            var floor = condition.IsInnate ? 1 : 0;
            var nextSeverity = Math.Max(floor, condition.Severity - amount);
            if (nextSeverity <= 0)
            {
                conditions.Remove(condition.Id);
            }
            else
            {
                conditions[condition.Id] = condition with { Severity = nextSeverity };
            }
        }

        return conditions;
    }

    private static Dictionary<string, PetConditionRecord> ApplyPlaySideEffects(Dictionary<string, PetConditionRecord> conditions, PetActor pet)
    {
        if (pet.Energy < 24 || pet.Health < 38)
        {
            var nextSeverity = Math.Max(1, conditions.TryGetValue("injury", out var current) ? current.Severity + 1 : 1);
            conditions["injury"] = new PetConditionRecord("injury", Math.Min(3, nextSeverity), false);
        }
        else
        {
            ReduceSelectedConditions(conditions, ["depression", "anxiety", "jointPain"], 1);
        }

        return conditions;
    }
}
