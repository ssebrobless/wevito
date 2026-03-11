using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class PetSimulationEngine
{
    private readonly Random _random = new(7);

    private static readonly IReadOnlyList<string> DefaultColors = ["red", "orange", "yellow", "blue", "indigo", "violet"];
    private static readonly IReadOnlyList<PetAgeStage> DefaultAges = [PetAgeStage.Baby, PetAgeStage.Teen, PetAgeStage.Adult];
    private static readonly IReadOnlyList<PetGender> DefaultGenders = [PetGender.Female, PetGender.Male];

    public IReadOnlyList<PetActor> CreateDefaultPets(GameContent content)
    {
        var selected = content.Species.Take(3).ToArray();
        var pets = new List<PetActor>();

        for (var i = 0; i < selected.Length; i++)
        {
            var species = selected[i];
            var age = (species.SupportedAgeStages ?? DefaultAges)[Math.Min(i, (species.SupportedAgeStages ?? DefaultAges).Count - 1)];
            var gender = (species.SupportedGenders ?? DefaultGenders)[i % (species.SupportedGenders ?? DefaultGenders).Count];
            var colors = species.SupportedColors ?? DefaultColors;
            var color = colors[i % colors.Count];
            var speed = species.BaseSpeed * GetAgeSpeedFactor(age) * GetGenderSpeedFactor(gender);
            pets.Add(new PetActor(
                Guid.NewGuid(),
                $"{species.DisplayName} {i + 1}",
                species.Id,
                species.AccentColor,
                age,
                gender,
                color,
                0,
                0,
                0,
                0,
                0,
                0,
                speed,
                PetBehaviorState.Home,
                DateTimeOffset.UtcNow.AddSeconds(1 + i * 0.25),
                PetFacingDirection.Right,
                PetAnimationState.Idle,
                DateTimeOffset.UtcNow,
                null,
                null,
                DateTimeOffset.UtcNow,
                84 - i * 4,
                80 - i * 3,
                76 - i * 5,
                79 - i * 2,
                74 + i * 4,
                72 + i * 3,
                88 - i * 2,
                [PetStatusType.Comforted],
                species.DefaultEnvironmentId));
        }

        return pets;
    }

    public IReadOnlyList<PetActor> ApplyLayout(
        IReadOnlyList<PetActor> pets,
        double homeLeft,
        double homeTop,
        double homeWidth,
        double homeHeight)
    {
        var count = Math.Max(1, pets.Count);
        var slotWidth = homeWidth / count;
        var floorY = homeTop + homeHeight - 24;
        var updated = new List<PetActor>(pets.Count);

        for (var i = 0; i < pets.Count; i++)
        {
            var pet = pets[i];
            var homeX = homeLeft + slotWidth * i + slotWidth / 2;
            updated.Add(pet with
            {
                HomeX = homeX,
                HomeY = floorY,
                CurrentX = pet.BehaviorState == PetBehaviorState.Home ? homeX : pet.CurrentX,
                CurrentY = pet.BehaviorState == PetBehaviorState.Home ? floorY : pet.CurrentY,
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
            var lifecycle = AdvanceLifecycle(pet, now);
            var vitals = TickVitals(lifecycle, mode, now, deltaSeconds);
            var moved = mode is CompanionMode.Focused or CompanionMode.Pinned
                ? MoveTowardHome(vitals, now, deltaSeconds)
                : MoveWithinRoamBand(vitals, roamBandBounds, now, deltaSeconds);
            updated.Add(RefreshPresentationState(moved, mode, now));
        }

        return updated;
    }

    public IReadOnlyList<PetActor> ApplyAction(string actionId, IReadOnlyList<PetActor> pets, DateTimeOffset now)
    {
        return pets.Select(pet => ApplyAction(actionId, pet, now)).ToList();
    }

    public IReadOnlyDictionary<string, double> BuildAverageNeedSnapshot(IReadOnlyList<PetActor> pets)
    {
        if (pets.Count == 0)
        {
            return new Dictionary<string, double>
            {
                ["hunger"] = 0,
                ["thirst"] = 0,
                ["energy"] = 0,
                ["cleanliness"] = 0,
                ["affection"] = 0,
                ["comfort"] = 0,
                ["health"] = 0
            };
        }

        return new Dictionary<string, double>
        {
            ["hunger"] = pets.Average(pet => pet.Hunger),
            ["thirst"] = pets.Average(pet => pet.Thirst),
            ["energy"] = pets.Average(pet => pet.Energy),
            ["cleanliness"] = pets.Average(pet => pet.Cleanliness),
            ["affection"] = pets.Average(pet => pet.Affection),
            ["comfort"] = pets.Average(pet => pet.Comfort),
            ["health"] = pets.Average(pet => pet.Health)
        };
    }

    public IReadOnlyList<PetStatusType> BuildAggregateStatuses(IReadOnlyList<PetActor> pets)
    {
        return pets
            .SelectMany(pet => pet.ActiveStatuses ?? [])
            .Distinct()
            .OrderBy(status => status.ToString(), StringComparer.Ordinal)
            .ToList();
    }

    public bool IsActionEnabled(string actionId, IReadOnlyList<PetActor> pets)
    {
        return actionId switch
        {
            "feed" => pets.Any(pet => pet.Hunger < 95),
            "water" => pets.Any(pet => pet.Thirst < 95),
            "rest" => pets.Any(pet => pet.Energy < 96 || pet.BehaviorState != PetBehaviorState.Home),
            "play" => pets.Any(pet => pet.Affection < 95 || pet.Comfort < 95),
            "groom" => pets.Any(pet => pet.Cleanliness < 95),
            "bath" => pets.Any(pet => pet.Cleanliness < 88),
            "medicine" => pets.Any(pet => pet.Health < 95 || HasStatus(pet, PetStatusType.Sick)),
            "doctor" => pets.Any(pet => pet.Health < 85 || HasStatus(pet, PetStatusType.Sick)),
            "home" => pets.Any(pet => pet.BehaviorState != PetBehaviorState.Home),
            _ => true
        };
    }

    private PetActor TickVitals(PetActor pet, CompanionMode mode, DateTimeOffset now, double deltaSeconds)
    {
        var passiveFactor = mode == CompanionMode.Passive ? 1.18 : 0.88;
        var hunger = Clamp(pet.Hunger - deltaSeconds * 1.8 * passiveFactor);
        var thirst = Clamp(pet.Thirst - deltaSeconds * 2.1 * passiveFactor);
        var energyDecay = mode == CompanionMode.Passive ? 1.25 : 0.65;
        var energy = Clamp(pet.Energy - deltaSeconds * energyDecay);
        var cleanliness = Clamp(pet.Cleanliness - deltaSeconds * 0.65 * passiveFactor);
        var affection = Clamp(pet.Affection - deltaSeconds * 0.45);
        var comfortBaseDelta = mode == CompanionMode.Passive ? -0.3 : 0.22;
        var comfort = Clamp(pet.Comfort + deltaSeconds * comfortBaseDelta);

        var healthDelta = 0.0;
        if (hunger < 22)
        {
            healthDelta -= 1.2 * deltaSeconds;
        }

        if (thirst < 20)
        {
            healthDelta -= 1.6 * deltaSeconds;
        }

        if (cleanliness < 24)
        {
            healthDelta -= 0.9 * deltaSeconds;
        }

        if (energy < 18)
        {
            healthDelta -= 0.8 * deltaSeconds;
        }

        if (comfort > 70 && hunger > 55 && thirst > 55)
        {
            healthDelta += 0.35 * deltaSeconds;
        }

        var health = Clamp(pet.Health + healthDelta);
        return pet with
        {
            Hunger = hunger,
            Thirst = thirst,
            Energy = energy,
            Cleanliness = cleanliness,
            Affection = affection,
            Comfort = comfort,
            Health = health
        };
    }

    private PetActor ApplyAction(string actionId, PetActor pet, DateTimeOffset now)
    {
        var updated = actionId switch
        {
            "feed" => pet with
            {
                Hunger = Clamp(pet.Hunger + 34),
                Comfort = Clamp(pet.Comfort + 6),
                Affection = Clamp(pet.Affection + 3)
            },
            "water" => pet with
            {
                Thirst = Clamp(pet.Thirst + 38),
                Comfort = Clamp(pet.Comfort + 4)
            },
            "rest" => pet with
            {
                Energy = Clamp(pet.Energy + 28),
                Comfort = Clamp(pet.Comfort + 10),
                TargetX = pet.HomeX,
                TargetY = pet.HomeY,
                BehaviorState = PetBehaviorState.Recalling
            },
            "play" => pet with
            {
                Affection = Clamp(pet.Affection + 18),
                Comfort = Clamp(pet.Comfort + 12),
                Energy = Clamp(pet.Energy - 6)
            },
            "groom" => pet with
            {
                Cleanliness = Clamp(pet.Cleanliness + 18),
                Affection = Clamp(pet.Affection + 6)
            },
            "bath" => pet with
            {
                Cleanliness = Clamp(pet.Cleanliness + 34),
                Comfort = Clamp(pet.Comfort + 2)
            },
            "medicine" => pet with
            {
                Health = Clamp(pet.Health + 14),
                Comfort = Clamp(pet.Comfort - 2)
            },
            "doctor" => pet with
            {
                Health = Clamp(pet.Health + 24),
                Comfort = Clamp(pet.Comfort - 4)
            },
            "home" => pet with
            {
                Comfort = Clamp(pet.Comfort + 8),
                TargetX = pet.HomeX,
                TargetY = pet.HomeY,
                BehaviorState = PetBehaviorState.Recalling
            },
            _ => pet
        };

        var animation = actionId switch
        {
            "feed" or "water" => PetAnimationState.Eat,
            "rest" or "home" => PetAnimationState.Sleep,
            "play" or "groom" => PetAnimationState.Happy,
            "bath" => PetAnimationState.Bathe,
            "medicine" or "doctor" => PetAnimationState.Sick,
            _ => updated.CurrentAnimationState
        };

        updated = updated with
        {
            OverrideAnimationState = animation,
            OverrideAnimationEndsAtUtc = now.AddSeconds(2.4),
            AnimationStartedAtUtc = now
        };

        return RefreshPresentationState(updated, CompanionMode.Focused, now);
    }

    private PetActor MoveTowardHome(PetActor pet, DateTimeOffset now, double deltaSeconds)
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
                TargetY = roamBandBounds.Bottom - 28,
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
        if (HasStatus(pet, PetStatusType.Sick))
        {
            return PetAnimationState.Sick;
        }

        if (mode is CompanionMode.Focused or CompanionMode.Pinned)
        {
            if (pet.Energy < 24)
            {
                return PetAnimationState.Sleep;
            }

            if (pet.Comfort > 76 && pet.Affection > 76)
            {
                return PetAnimationState.Happy;
            }

            if (pet.Hunger < 28 || pet.Thirst < 28 || pet.Affection < 24)
            {
                return PetAnimationState.Sad;
            }

            return PetAnimationState.Idle;
        }

        return PetAnimationState.Walk;
    }

    private static IReadOnlyList<PetStatusType> BuildStatuses(PetActor pet)
    {
        var statuses = new List<PetStatusType>();
        if (pet.Hunger < 35)
        {
            statuses.Add(PetStatusType.Hungry);
        }

        if (pet.Thirst < 35)
        {
            statuses.Add(PetStatusType.Thirsty);
        }

        if (pet.Energy < 30)
        {
            statuses.Add(PetStatusType.Sleepy);
        }

        if (pet.Health < 52)
        {
            statuses.Add(PetStatusType.Sick);
        }

        if (pet.Cleanliness < 38)
        {
            statuses.Add(PetStatusType.Dirty);
        }

        if (pet.Affection < 34)
        {
            statuses.Add(PetStatusType.Lonely);
        }

        if (pet.Comfort > 76)
        {
            statuses.Add(PetStatusType.Comforted);
        }

        if (pet.Hunger > 70 && pet.Thirst > 68 && pet.Affection > 68 && pet.Health > 72)
        {
            statuses.Add(PetStatusType.Happy);
        }

        return statuses;
    }

    private static bool HasStatus(PetActor pet, PetStatusType status)
    {
        return pet.ActiveStatuses?.Contains(status) == true;
    }

    private static double GetAgeSpeedFactor(PetAgeStage ageStage)
    {
        return ageStage switch
        {
            PetAgeStage.Baby => 0.82,
            PetAgeStage.Teen => 1.05,
            _ => 1.0
        };
    }

    private static double GetGenderSpeedFactor(PetGender gender)
    {
        return gender == PetGender.Male ? 1.04 : 0.98;
    }

    private static double Clamp(double value)
    {
        return Math.Clamp(value, 0, 100);
    }

    private static PetActor AdvanceLifecycle(PetActor pet, DateTimeOffset now)
    {
        var stageStartedAtUtc = pet.AgeStageStartedAtUtc == default ? now : pet.AgeStageStartedAtUtc;
        var elapsed = now - stageStartedAtUtc;

        return pet.AgeStage switch
        {
            PetAgeStage.Baby when elapsed >= TimeSpan.FromMinutes(20) => pet with
            {
                AgeStage = PetAgeStage.Teen,
                Speed = pet.Speed / GetAgeSpeedFactor(PetAgeStage.Baby) * GetAgeSpeedFactor(PetAgeStage.Teen),
                AgeStageStartedAtUtc = now
            },
            PetAgeStage.Teen when elapsed >= TimeSpan.FromMinutes(35) => pet with
            {
                AgeStage = PetAgeStage.Adult,
                Speed = pet.Speed / GetAgeSpeedFactor(PetAgeStage.Teen) * GetAgeSpeedFactor(PetAgeStage.Adult),
                AgeStageStartedAtUtc = now
            },
            _ => pet with { AgeStageStartedAtUtc = stageStartedAtUtc }
        };
    }
}
