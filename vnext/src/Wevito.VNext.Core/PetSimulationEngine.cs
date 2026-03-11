using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class PetSimulationEngine
{
    private readonly Random _random = new();

    public IReadOnlyList<PetActor> CreateDefaultPets(GameContent content)
    {
        var selected = content.Species.Take(3).ToArray();
        var pets = new List<PetActor>();

        for (var i = 0; i < selected.Length; i++)
        {
            var species = selected[i];
            pets.Add(new PetActor(
                Guid.NewGuid(),
                $"{species.DisplayName} {i + 1}",
                species.Id,
                species.AccentColor,
                0,
                0,
                0,
                0,
                0,
                0,
                species.BaseSpeed,
                PetBehaviorState.Home,
                DateTimeOffset.UtcNow));
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
        var floorY = homeTop + homeHeight - 18;
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
                CurrentY = pet.BehaviorState == PetBehaviorState.Home ? floorY : pet.CurrentY
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
            updated.Add(UpdatePet(pet, mode, roamBandBounds, now, deltaSeconds));
        }

        return updated;
    }

    private PetActor UpdatePet(
        PetActor pet,
        CompanionMode mode,
        RectInt roamBandBounds,
        DateTimeOffset now,
        double deltaSeconds)
    {
        if (mode is CompanionMode.Focused or CompanionMode.Pinned)
        {
            return MoveTowardHome(pet, now, deltaSeconds);
        }

        return MoveWithinRoamBand(pet, roamBandBounds, now, deltaSeconds);
    }

    private PetActor MoveTowardHome(PetActor pet, DateTimeOffset now, double deltaSeconds)
    {
        var dx = pet.HomeX - pet.CurrentX;
        var dy = pet.HomeY - pet.CurrentY;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance <= 1.0)
        {
            return pet with
            {
                CurrentX = pet.HomeX,
                CurrentY = pet.HomeY,
                TargetX = pet.HomeX,
                TargetY = pet.HomeY,
                BehaviorState = PetBehaviorState.Home,
                NextDecisionAtUtc = now.AddSeconds(1.5)
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
            BehaviorState = PetBehaviorState.Recalling
        };
    }

    private PetActor MoveWithinRoamBand(PetActor pet, RectInt roamBandBounds, DateTimeOffset now, double deltaSeconds)
    {
        var petWithTarget = pet;
        if (pet.BehaviorState != PetBehaviorState.Roaming ||
            (Math.Abs(pet.CurrentX - pet.TargetX) < 2.0 && now >= pet.NextDecisionAtUtc))
        {
            var minX = roamBandBounds.X + 24;
            var maxX = roamBandBounds.Right - 24;
            var nextX = _random.Next(minX, Math.Max(minX + 1, maxX));
            petWithTarget = pet with
            {
                TargetX = nextX,
                TargetY = roamBandBounds.Bottom - 16,
                BehaviorState = PetBehaviorState.Roaming,
                NextDecisionAtUtc = now.AddSeconds(_random.NextDouble() * 1.2 + 0.6)
            };
        }

        var dx = petWithTarget.TargetX - petWithTarget.CurrentX;
        var dy = petWithTarget.TargetY - petWithTarget.CurrentY;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance <= 1.0)
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
            CurrentY = petWithTarget.CurrentY + dy / distance * step
        };
    }
}
