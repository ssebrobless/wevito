using System.Globalization;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Shell;

internal static class HabitatLoadoutResolver
{
    private static readonly IReadOnlyDictionary<string, string[]> FoodPools = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["omnivore-scavenger"] = ["snack_bowl", "grain_mix", "sliced_apple", "bread_crumbs", "nut_pile", "berry_cluster"],
        ["herbivore-grazer"] = ["hay_bundle", "leafy_greens", "grain_bundle", "clover_bunch", "flower_petals", "root_slice"],
        ["birds-seed"] = ["seed_pile", "mixed_grain", "sunflower_seeds", "hanging_feeder_mix", "shiny_reward", "berry_cluster"],
        ["predator-reptile"] = ["reptile_tray", "protein_bowl", "worm_pile", "bug_cup", "fish", "mouse_prey"],
    };

    private static readonly string[] MammalWaterPool = ["water_bowl", "shallow_water_dish", "treat_cup"];
    private static readonly string[] BirdWaterPool = ["shallow_water_dish", "hanging_feeder", "seed_tray"];
    private static readonly string[] WetlandWaterPool = ["pond_dish", "shallow_water_dish", "water_bowl"];

    private static readonly string[] BurrowToyPoolA = ["tunnel_hide", "rope_toy", "chew_toy", "digging_tray", "leaf_pile"];
    private static readonly string[] BurrowToyPoolB = ["blanket_mat", "crate_hideout", "log_shelter", "moss_bed", "stump_perch"];
    private static readonly string[] ArborealToyPoolA = ["branch_perch", "bell_toy", "mirror_trinket", "rope_toy", "leaf_pile"];
    private static readonly string[] ArborealToyPoolB = ["nest_bed", "stump_perch", "blanket_mat", "crate_hideout", "hay_bed"];
    private static readonly string[] WetlandToyPoolA = ["leaf_pile", "bell_toy", "mirror_trinket", "branch_perch"];
    private static readonly string[] WetlandToyPoolB = ["moss_bed", "hay_bed", "rock_basking_spot", "blanket_mat"];
    private static readonly string[] ReptileToyPoolA = ["branch_perch", "leaf_pile", "mirror_trinket"];
    private static readonly string[] ReptileToyPoolB = ["rock_basking_spot", "moss_bed", "blanket_mat", "log_shelter"];

    private static readonly string[] GroomingPool = ["grooming_brush", "towel", "bandage_roll"];
    private static readonly string[] BathPool = ["soap_bottle", "towel"];
    private static readonly string[] MedicinePool = ["medicine_dropper", "pill_bottle", "bandage_roll"];
    private static readonly string[] DoctorPool = ["thermometer", "first_aid_kit"];
    private static readonly string[] EmergencyDoctorPool = ["first_aid_kit", "thermometer", "syringe"];
    private static readonly HashSet<string> RestAnchorAssets = new(StringComparer.OrdinalIgnoreCase)
    {
        "blanket_mat",
        "crate_hideout",
        "hay_bed",
        "log_shelter",
        "moss_bed",
        "nest_bed",
        "rock_basking_spot",
        "tunnel_hide"
    };

    private static readonly HashSet<string> StageSafeRestAssets = new(StringComparer.OrdinalIgnoreCase)
    {
        "hay_bed",
        "moss_bed",
        "nest_bed",
        "rock_basking_spot"
    };

    private static readonly HashSet<string> PersistentWaterAssets = new(StringComparer.OrdinalIgnoreCase)
    {
        "water_bowl",
        "shallow_water_dish",
        "pond_dish",
        "hanging_feeder",
        "seed_tray"
    };

    private static readonly HashSet<string> PersistentHabitatAccentAssets = new(StringComparer.OrdinalIgnoreCase)
    {
        "branch_perch",
        "bell_toy",
        "mirror_trinket",
        "rope_toy",
        "leaf_pile",
        "moss_bed",
        "nest_bed",
        "rock_basking_spot"
    };

    private static readonly IReadOnlyDictionary<string, string[]> PreferredRestAssets = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["rat"] = ["moss_bed", "hay_bed", "nest_bed"],
        ["crow"] = ["nest_bed", "moss_bed", "hay_bed"],
        ["fox"] = ["moss_bed", "hay_bed", "nest_bed"],
        ["snake"] = ["rock_basking_spot", "moss_bed", "hay_bed"],
        ["deer"] = ["hay_bed", "moss_bed", "nest_bed"],
        ["frog"] = ["moss_bed", "rock_basking_spot", "hay_bed"],
        ["pigeon"] = ["nest_bed", "moss_bed", "hay_bed"],
        ["raccoon"] = ["moss_bed", "hay_bed", "nest_bed"],
        ["squirrel"] = ["nest_bed", "moss_bed", "hay_bed"],
        ["goose"] = ["hay_bed", "moss_bed", "nest_bed"]
    };

    private static readonly IReadOnlyDictionary<string, string[]> PreferredAccentAssets = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["rat"] = ["leaf_pile", "rope_toy", "bell_toy"],
        ["crow"] = ["branch_perch", "mirror_trinket", "bell_toy"],
        ["fox"] = ["leaf_pile", "rope_toy", "bell_toy"],
        ["snake"] = ["rock_basking_spot", "leaf_pile", "mirror_trinket"],
        ["deer"] = ["leaf_pile", "branch_perch", "bell_toy"],
        ["frog"] = ["leaf_pile", "rock_basking_spot", "bell_toy"],
        ["pigeon"] = ["branch_perch", "mirror_trinket", "bell_toy"],
        ["raccoon"] = ["rope_toy", "mirror_trinket", "leaf_pile"],
        ["squirrel"] = ["branch_perch", "rope_toy", "leaf_pile"],
        ["goose"] = ["leaf_pile", "branch_perch", "bell_toy"]
    };

    public static HabitatLoadout Resolve(CompanionState state, GameContent content)
    {
        var livingPets = state.ActivePets.Where(pet => !pet.IsDead).ToList();
        if (livingPets.Count == 0)
        {
            return new HabitatLoadout(
                [],
                new Dictionary<string, HabitatDisplayItem>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, IReadOnlyList<HabitatDisplayItem>>(StringComparer.OrdinalIgnoreCase),
                []);
        }

        var focusPet = livingPets[0];
        var species = content.Species.First(species => string.Equals(species.Id, focusPet.SpeciesId, StringComparison.OrdinalIgnoreCase));
        var needSnapshot = BuildNeedSnapshot(livingPets);
        var actionMap = new Dictionary<string, HabitatDisplayItem>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<HabitatDisplayItem>();
        var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddItem(HabitatDisplayItem item)
        {
            item = EnrichForVisualMapping(item, content.ItemVisualMappings ?? []);
            if (!added.Add($"{item.CategoryFolder}/{item.AssetId}"))
            {
                return;
            }

            ordered.Add(item);
            if (!string.IsNullOrWhiteSpace(item.ActionId) && !actionMap.ContainsKey(item.ActionId))
            {
                actionMap[item.ActionId] = item;
            }
        }

        foreach (var item in PickFoodItems(species, focusPet, needSnapshot["hunger"]))
        {
            AddItem(item);
        }

        AddItem(PickWaterItem(species, focusPet, needSnapshot["thirst"]));

        foreach (var item in PickPlayItems(species, focusPet, needSnapshot["affection"], needSnapshot["fitness"]))
        {
            AddItem(item);
        }

        foreach (var item in PickRestItems(species, focusPet, needSnapshot["energy"]))
        {
            AddItem(item);
        }

        foreach (var item in PickCareItems(focusPet, needSnapshot["cleanliness"]))
        {
            AddItem(item);
        }

        var recommended = ordered
            .OrderByDescending(item => item.IsUrgent)
            .ThenBy(item => PurposeRank(item.Purpose))
            .ThenBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();

        foreach (var item in recommended)
        {
            if (!string.IsNullOrWhiteSpace(item.ActionId) && !actionMap.ContainsKey(item.ActionId))
            {
                actionMap[item.ActionId] = item;
            }
        }

        var actionOptions = ordered
            .Where(item => !string.IsNullOrWhiteSpace(item.ActionId))
            .GroupBy(item => item.ActionId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<HabitatDisplayItem>)group
                    .OrderByDescending(item => item.IsUrgent)
                    .ThenBy(item => PurposeRank(item.Purpose))
                    .ThenBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        var dynamicProps = BuildManifestStageProps(species, focusPet, needSnapshot, content)
            ?? BuildDynamicStageProps(species.Id, ordered);
        return new HabitatLoadout(recommended, actionMap, actionOptions, dynamicProps);
    }

    public static string BuildActionFeedback(string actionId, ActionDefinition actionDefinition, HabitatLoadout loadout)
    {
        if (!loadout.ActionItems.TryGetValue(actionId, out var item) &&
            !(string.Equals(actionId, "home", StringComparison.OrdinalIgnoreCase) && loadout.ActionItems.TryGetValue("rest", out item)))
        {
            return string.IsNullOrWhiteSpace(actionDefinition.FeedbackText)
                ? actionDefinition.EffectSummary
                : actionDefinition.FeedbackText;
        }

        return actionId switch
        {
            "feed" => $"Set out {item.Label.ToLowerInvariant()} for the group.",
            "water" => $"Refreshed the {item.Label.ToLowerInvariant()}.",
            "rest" => $"Settled everyone into the {item.Label.ToLowerInvariant()} for a rest.",
            "play" => $"Brought out the {item.Label.ToLowerInvariant()} to liven the habitat up.",
            "groom" => $"Worked through a careful grooming pass with the {item.Label.ToLowerInvariant()}.",
            "bath" => $"Set up bath time with the {item.Label.ToLowerInvariant()}.",
            "medicine" => $"Applied care with the {item.Label.ToLowerInvariant()}.",
            "doctor" => $"Ran a closer health check using the {item.Label.ToLowerInvariant()}.",
            "home" => $"Called everyone back toward the {item.Label.ToLowerInvariant()}.",
            _ => string.IsNullOrWhiteSpace(actionDefinition.FeedbackText)
                ? actionDefinition.EffectSummary
                : actionDefinition.FeedbackText
        };
    }

    private static Dictionary<string, double> BuildNeedSnapshot(IReadOnlyList<PetActor> pets)
    {
        if (pets.Count == 0)
        {
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                ["hunger"] = 0,
                ["thirst"] = 0,
                ["energy"] = 0,
                ["cleanliness"] = 0,
                ["affection"] = 0,
                ["fitness"] = 0
            };
        }

        return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["hunger"] = pets.Average(pet => pet.Hunger),
            ["thirst"] = pets.Average(pet => pet.Thirst),
            ["energy"] = pets.Average(pet => pet.Energy),
            ["cleanliness"] = pets.Average(pet => pet.Cleanliness),
            ["affection"] = pets.Average(pet => pet.Affection),
            ["fitness"] = pets.Average(pet => pet.Fitness)
        };
    }

    private static IReadOnlyList<HabitatDisplayItem> PickFoodItems(SpeciesDefinition species, PetActor pet, double hunger)
    {
        var items = new List<HabitatDisplayItem>();
        if (FoodPools.TryGetValue(species.PrimaryFoodGroupId, out var primaryFoodPool))
        {
            items.AddRange(PickMany(ResolveFoodFolder(species.PrimaryFoodGroupId), species.Id, pet, "primary-food", "Primary Food", primaryFoodPool, hunger < 78 ? 2 : 1, "feed", hunger < 78));
        }

        if (FoodPools.TryGetValue(species.SecondaryFoodGroupId, out var secondaryFoodPool))
        {
            items.AddRange(PickMany(ResolveFoodFolder(species.SecondaryFoodGroupId), species.Id, pet, "secondary-food", "Alt Food", secondaryFoodPool, hunger < 60 ? 2 : 1, "feed"));
        }

        return items;
    }

    private static HabitatDisplayItem PickWaterItem(SpeciesDefinition species, PetActor pet, double thirst)
    {
        var waterPool = ResolveWaterPool(species.Id);
        return PickOne("containers", species.Id, pet, "water", "Fresh Water", waterPool, "water", thirst < 78);
    }

    private static IReadOnlyList<HabitatDisplayItem> PickPlayItems(SpeciesDefinition species, PetActor pet, double affection, double fitness)
    {
        var count = affection < 72 || fitness < 68 ? 2 : 1;
        var (poolA, poolB) = ResolveToyPools(species.Id);
        var items = new List<HabitatDisplayItem>();
        items.AddRange(PickMany("toys_a", species.Id, pet, "toy-a", "Play", poolA, count, "play", affection < 72 || fitness < 68));
        items.AddRange(PickMany("toys_b", species.Id, pet, "toy-b", "Comfort", poolB, 1, "play"));
        return items;
    }

    private static IReadOnlyList<HabitatDisplayItem> PickRestItems(SpeciesDefinition species, PetActor pet, double energy)
    {
        var (_, poolB) = ResolveToyPools(species.Id);
        return PickMany("toys_b", species.Id, pet, "rest", "Rest Spot", poolB, energy < 64 ? 2 : 1, "rest", energy < 64);
    }

    private static IReadOnlyList<HabitatDisplayItem> PickCareItems(PetActor pet, double cleanliness)
    {
        var items = new List<HabitatDisplayItem>();
        items.AddRange(PickMany("care", pet.SpeciesId, pet, "groom", "Grooming", GroomingPool, cleanliness < 72 ? 2 : 1, "groom", cleanliness < 72));
        items.AddRange(PickMany("care", pet.SpeciesId, pet, "bath", "Bath", BathPool, cleanliness < 58 ? 2 : 1, "bath", cleanliness < 58));

        var activeConditions = (pet.ActiveConditions ?? []).ToList();
        var urgentConditionCount = activeConditions.Count(condition => !condition.IsInnate && condition.Severity >= 2);
        var activeNonInnateConditionCount = activeConditions.Count(condition => !condition.IsInnate);
        var needsMedicine = pet.Health < 74 || activeNonInnateConditionCount > 0;
        var needsDoctor = pet.Health < 58 || urgentConditionCount > 0;

        if (needsMedicine)
        {
            items.AddRange(PickMany("care", pet.SpeciesId, pet, "medicine", "Medicine", MedicinePool, 1, "medicine", pet.Health < 64 || activeNonInnateConditionCount > 0));
        }

        if (needsDoctor)
        {
            var doctorPool = urgentConditionCount > 0 || pet.Health < 44 ? EmergencyDoctorPool : DoctorPool;
            items.AddRange(PickMany("care", pet.SpeciesId, pet, "doctor", "Doctor", doctorPool, 1, "doctor", true));
        }

        return items;
    }

    private static IReadOnlyList<StagePropSpec> BuildDynamicStageProps(string speciesId, IReadOnlyList<HabitatDisplayItem> recommendedItems)
    {
        var stageProps = new List<StagePropSpec>();
        var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddProp(HabitatDisplayItem item, double left, double top, double width, double height, double opacity = 0.96)
        {
            var key = $"{item.CategoryFolder}/{item.AssetId}";
            if (!added.Add(key))
            {
                return;
            }

            stageProps.Add(new StagePropSpec(item.CategoryFolder, item.AssetId, left, top, width, height, opacity));
        }

        var restCandidates = recommendedItems.Where(item => StageSafeRestAssets.Contains(item.AssetId)).ToList();
        var restProp = PickPreferredItem(restCandidates, StageSafeRestAssets, PreferredRestAssets, speciesId);
        if (restProp is not null)
        {
            AddProp(restProp, left: 254, top: 100, width: 72, height: 46, opacity: 0.98);
        }

        var waterProp = recommendedItems.FirstOrDefault(item => PersistentWaterAssets.Contains(item.AssetId));
        if (waterProp is not null)
        {
            var waterIsHanging = string.Equals(waterProp.AssetId, "hanging_feeder", StringComparison.OrdinalIgnoreCase);
            AddProp(
                waterProp,
                left: waterIsHanging ? 282 : 26,
                top: waterIsHanging ? 14 : 118,
                width: waterIsHanging ? 38 : 44,
                height: waterIsHanging ? 60 : 30,
                opacity: 0.94);
        }

        var accentProp = PickPreferredItem(
            recommendedItems.Where(item => !PersistentWaterAssets.Contains(item.AssetId)).ToList(),
            PersistentHabitatAccentAssets,
            PreferredAccentAssets,
            speciesId);
        if (accentProp is not null)
        {
            var elevated = accentProp.AssetId is "branch_perch" or "mirror_trinket" or "bell_toy";
            AddProp(
                accentProp,
                left: elevated ? 188 : 88,
                top: elevated ? 12 : 102,
                width: elevated ? 76 : 58,
                height: elevated ? 42 : 40,
                opacity: 0.92);
        }

        return stageProps;
    }

    private static IReadOnlyList<StagePropSpec>? BuildManifestStageProps(
        SpeciesDefinition species,
        PetActor focusPet,
        IReadOnlyDictionary<string, double> needSnapshot,
        GameContent content)
    {
        var manifestLoadouts = content.HabitatLoadouts;
        if (manifestLoadouts is null || manifestLoadouts.Count == 0)
        {
            TraceLog.Write("habitat-loadout", $"manifest-missing species={species.Id}; using hardcoded fallback");
            return null;
        }

        var loadout = manifestLoadouts.FirstOrDefault(candidate =>
            string.Equals(candidate.SpeciesId, species.Id, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(candidate.EnvironmentId, species.DefaultEnvironmentId, StringComparison.OrdinalIgnoreCase));
        if (loadout is null)
        {
            TraceLog.Write("habitat-loadout", $"species-missing species={species.Id} environment={species.DefaultEnvironmentId}; using hardcoded fallback");
            return null;
        }

        var visualMappings = content.ItemVisualMappings ?? [];
        var props = new List<StagePropSpec>();
        foreach (var slot in loadout.Slots
                     .Where(slot => ShouldShowManifestSlot(slot, focusPet, needSnapshot))
                     .OrderBy(slot => slot.PriorityTier))
        {
            if (!TryResolveCategoryFolder(slot.AssetId, visualMappings, out var categoryFolder))
            {
                TraceLog.Write("habitat-loadout", $"asset-unmapped species={species.Id} asset={slot.AssetId}; using hardcoded fallback");
                return null;
            }

            props.Add(new StagePropSpec(
                categoryFolder,
                slot.AssetId,
                slot.DefaultRect.Left,
                slot.DefaultRect.Top,
                slot.DefaultRect.Width,
                slot.DefaultRect.Height,
                ResolveManifestOpacity(slot),
                slot.DepthBand,
                slot.OcclusionMode,
                slot.ContactShadowMode,
                slot.SlotId));
        }

        return props;
    }

    private static bool ShouldShowManifestSlot(
        HabitatObjectSlot slot,
        PetActor focusPet,
        IReadOnlyDictionary<string, double> needSnapshot)
    {
        if (!string.Equals(slot.SlotId, "interaction", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var recentActionId = focusPet.LastActionAtUtc is not null &&
                             DateTimeOffset.UtcNow - focusPet.LastActionAtUtc.Value < TimeSpan.FromMinutes(12)
            ? focusPet.LastActionId
            : ResolveActionId(focusPet.CurrentActionVisualIntent);

        if (string.Equals(recentActionId, "play", StringComparison.OrdinalIgnoreCase) &&
            slot.AssetId is "ball" or "bell_toy" or "branch_perch" or "leaf_pile")
        {
            return true;
        }

        if (string.Equals(recentActionId, "feed", StringComparison.OrdinalIgnoreCase) &&
            slot.AssetId is "snack_bowl" or "seed_tray" or "bug_treat")
        {
            return true;
        }

        if (string.Equals(recentActionId, "water", StringComparison.OrdinalIgnoreCase) &&
            slot.AssetId is "pond_dish" or "shallow_water_dish" or "water_bowl")
        {
            return true;
        }

        return slot.AssetId switch
        {
            "ball" => GetNeed(needSnapshot, "affection") < 72 || GetNeed(needSnapshot, "fitness") < 68,
            "snack_bowl" or "seed_tray" or "bug_treat" => GetNeed(needSnapshot, "hunger") < 78,
            "pond_dish" or "shallow_water_dish" or "water_bowl" => GetNeed(needSnapshot, "thirst") < 78,
            _ => false
        };
    }

    private static double GetNeed(IReadOnlyDictionary<string, double> needSnapshot, string key)
    {
        return needSnapshot.TryGetValue(key, out var value) ? value : 0;
    }

    private static string ResolveActionId(ActionVisualIntent? intent)
    {
        if (intent is null)
        {
            return string.Empty;
        }

        if (intent.Overlay == PropOverlayKind.Ball ||
            intent.Family is AnimationFamily.PlayBall or AnimationFamily.HoldBall or AnimationFamily.PickupBall or AnimationFamily.DropBall or AnimationFamily.CarryBallWalk or AnimationFamily.CarryBallRun)
        {
            return "play";
        }

        return intent.Family switch
        {
            AnimationFamily.Drink => "water",
            AnimationFamily.Eat => "feed",
            AnimationFamily.Bathe => "bath",
            _ => string.Empty
        };
    }

    private static bool TryResolveCategoryFolder(
        string assetId,
        IReadOnlyList<ItemVisualMapping> visualMappings,
        out string categoryFolder)
    {
        var mapping = visualMappings.FirstOrDefault(candidate =>
        {
            var parts = candidate.VisualAssetId.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 3 && string.Equals(parts[2], assetId, StringComparison.OrdinalIgnoreCase);
        });

        if (mapping is null)
        {
            categoryFolder = string.Empty;
            return false;
        }

        var visualParts = mapping.VisualAssetId.Split('/', StringSplitOptions.RemoveEmptyEntries);
        categoryFolder = visualParts[1];
        return true;
    }

    internal static HabitatDisplayItem EnrichForVisualMapping(
        HabitatDisplayItem item,
        IReadOnlyList<ItemVisualMapping> visualMappings)
    {
        var mapping = TryFindVisualMapping(item.CategoryFolder, item.AssetId, visualMappings);
        if (mapping is null)
        {
            return item;
        }

        return item with
        {
            Label = string.IsNullOrWhiteSpace(mapping.DisplayName) ? item.Label : mapping.DisplayName,
            IsSmallIconSafe = mapping.SmallIconSafe,
            VisualMappingId = mapping.Id
        };
    }

    private static ItemVisualMapping? TryFindVisualMapping(
        string categoryFolder,
        string assetId,
        IReadOnlyList<ItemVisualMapping> visualMappings)
    {
        var expectedVisualAssetId = $"items/{categoryFolder}/{assetId}";
        return visualMappings.FirstOrDefault(candidate =>
            string.Equals(candidate.VisualAssetId, expectedVisualAssetId, StringComparison.OrdinalIgnoreCase));
    }

    private static double ResolveManifestOpacity(HabitatObjectSlot slot)
    {
        return slot.DepthBand switch
        {
            DepthBand.FarProp => 0.9,
            DepthBand.NearOccluder => 0.94,
            _ => slot.SlotId switch
            {
                "primary" => 0.98,
                "interaction" => 0.94,
                _ => 0.92
            }
        };
    }

    private static HabitatDisplayItem? PickPreferredItem(
        IReadOnlyList<HabitatDisplayItem> items,
        ISet<string> allowedAssets,
        IReadOnlyDictionary<string, string[]> preferenceMap,
        string speciesId)
    {
        if (preferenceMap.TryGetValue(speciesId, out var preferredOrder))
        {
            foreach (var assetId in preferredOrder)
            {
                var match = items.FirstOrDefault(item => string.Equals(item.AssetId, assetId, StringComparison.OrdinalIgnoreCase));
                if (match is not null && allowedAssets.Contains(match.AssetId))
                {
                    return match;
                }
            }
        }

        return items.FirstOrDefault(item => allowedAssets.Contains(item.AssetId));
    }

    private static (double Width, double Height) ResolveStageDimensions(string categoryFolder)
    {
        return categoryFolder switch
        {
            "care" => (36, 34),
            "containers" => (38, 30),
            "toys_a" => (44, 34),
            "toys_b" => (54, 40),
            "food_herbivore" => (38, 30),
            "food_predator" => (38, 28),
            "food_birds" => (34, 26),
            "food_omnivore" => (36, 26),
            _ => (36, 28)
        };
    }

    private static HabitatDisplayItem PickOne(
        string categoryFolder,
        string speciesId,
        PetActor pet,
        string salt,
        string purpose,
        string[] assetIds,
        string actionId,
        bool urgent = false)
    {
        return PickMany(categoryFolder, speciesId, pet, salt, purpose, assetIds, 1, actionId, urgent)[0];
    }

    private static IReadOnlyList<HabitatDisplayItem> PickMany(
        string categoryFolder,
        string speciesId,
        PetActor pet,
        string salt,
        string purpose,
        string[] assetIds,
        int count,
        string actionId,
        bool urgent = false)
    {
        if (assetIds.Length == 0 || count <= 0)
        {
            return [];
        }

        var baseIndex = Math.Abs(HashCode.Combine(speciesId, pet.AgeStage, pet.Gender, pet.ColorVariant, salt));
        var picked = new List<HabitatDisplayItem>();
        for (var i = 0; i < Math.Min(count, assetIds.Length); i++)
        {
            var assetId = assetIds[(baseIndex + i * 3) % assetIds.Length];
            picked.Add(new HabitatDisplayItem(
                $"{categoryFolder}:{assetId}",
                ToDisplayLabel(assetId),
                categoryFolder,
                assetId,
                purpose,
                BuildPreferenceHint(pet, actionId, purpose, assetId, urgent, i),
                actionId,
                urgent));
        }

        return picked;
    }

    private static string[] ResolveWaterPool(string speciesId)
    {
        return speciesId switch
        {
            "crow" or "pigeon" or "goose" => BirdWaterPool,
            "frog" or "snake" => WetlandWaterPool,
            _ => MammalWaterPool
        };
    }

    private static (string[] PoolA, string[] PoolB) ResolveToyPools(string speciesId)
    {
        return speciesId switch
        {
            "crow" or "pigeon" or "squirrel" => (ArborealToyPoolA, ArborealToyPoolB),
            "frog" or "goose" => (WetlandToyPoolA, WetlandToyPoolB),
            "snake" => (ReptileToyPoolA, ReptileToyPoolB),
            _ => (BurrowToyPoolA, BurrowToyPoolB)
        };
    }

    private static int PurposeRank(string purpose)
    {
        return purpose switch
        {
            "Doctor" => 0,
            "Medicine" => 1,
            "Bath" => 2,
            "Grooming" => 3,
            "Fresh Water" => 4,
            "Primary Food" => 5,
            "Alt Food" => 6,
            "Rest Spot" => 7,
            "Play" => 8,
            "Comfort" => 9,
            _ => 10
        };
    }

    private static string ToDisplayLabel(string assetId)
    {
        var words = assetId
            .Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word));
        return string.Join(' ', words);
    }

    private static string ResolveFoodFolder(string foodGroupId)
    {
        return foodGroupId switch
        {
            "omnivore-scavenger" => "food_omnivore",
            "herbivore-grazer" => "food_herbivore",
            "birds-seed" => "food_birds",
            "predator-reptile" => "food_predator",
            _ => "food_omnivore"
        };
    }

    private static string BuildPreferenceHint(
        PetActor pet,
        string actionId,
        string purpose,
        string assetId,
        bool urgent,
        int index)
    {
        var personality = pet.Personality;
        var habits = pet.HabitProfile;

        return actionId switch
        {
            "feed" when urgent => "Needed",
            "feed" when string.Equals(purpose, "Primary Food", StringComparison.OrdinalIgnoreCase) &&
                         personality is not null &&
                         personality.FoodLove >= 25 => "Favorite",
            "feed" when string.Equals(purpose, "Alt Food", StringComparison.OrdinalIgnoreCase) && index > 0 => "Backup",
            "water" when urgent => "Needed",
            "water" => "Fresh",
            "play" when personality is not null && personality.Playfulness >= 25 &&
                         assetId is "rope_toy" or "bell_toy" or "leaf_pile" or "mirror_trinket" => "Favorite",
            "play" when personality is not null && personality.CuddleNeed >= 25 &&
                         string.Equals(purpose, "Comfort", StringComparison.OrdinalIgnoreCase) => "Cozy",
            "play" when habits is not null && habits.Stress >= 55 => "Calming",
            "rest" when personality is not null && personality.CuddleNeed >= 25 => "Cozy",
            "rest" when habits is not null && habits.Stress >= 55 => "Settling",
            "groom" when personality is not null && personality.CleanlinessPreference >= 25 => "Preferred",
            "bath" when personality is not null && personality.CleanlinessPreference >= 25 => "Preferred",
            "medicine" or "doctor" when urgent => "Needed",
            "medicine" or "doctor" => "Prepared",
            _ => string.Empty
        };
    }
}
