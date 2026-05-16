using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record PetMemoryRoutingDecision(
    AgentSlotProfile? Helper,
    bool UsedMemory,
    string Reason,
    double Score = 0);

public sealed class PetMemoryRouter
{
    private readonly PetMemoryStore _memoryStore;
    private int _roundRobinIndex;

    public PetMemoryRouter(PetMemoryStore? memoryStore = null)
    {
        _memoryStore = memoryStore ?? new PetMemoryStore();
    }

    public PetMemoryRoutingDecision Route(
        TaskIntent intent,
        IReadOnlyList<AgentSlotProfile> helpers,
        AgentSlotProfile? fallbackHelper = null,
        int topK = 3)
    {
        if (helpers.Count == 0)
        {
            return new PetMemoryRoutingDecision(null, UsedMemory: false, "No helpers are available.");
        }

        if (intent.TargetMode is TaskIntentTargetMode.ExplicitPetName or TaskIntentTargetMode.SelectedPet)
        {
            return new PetMemoryRoutingDecision(fallbackHelper, UsedMemory: false, "Explicit or selected helper routing bypasses memory.");
        }

        AgentSlotProfile? bestHelper = null;
        PetMemorySearchResult? bestResult = null;
        foreach (var helper in helpers)
        {
            var results = _memoryStore.Search(helper.PetId, intent.RawText, intent.RequestedToolFamily, topK);
            var candidate = results.OrderByDescending(result => result.Score).FirstOrDefault();
            if (candidate is null)
            {
                continue;
            }

            if (bestResult is null || candidate.Score > bestResult.Score)
            {
                bestResult = candidate;
                bestHelper = helper;
            }
        }

        if (bestHelper is not null && bestResult is not null && bestResult.Score > 0.15d)
        {
            return new PetMemoryRoutingDecision(
                bestHelper,
                UsedMemory: true,
                $"Memory matched '{bestResult.Example.Label}' for {bestHelper.PetNameSnapshot}.",
                bestResult.Score);
        }

        if (fallbackHelper is not null)
        {
            return new PetMemoryRoutingDecision(fallbackHelper, UsedMemory: false, "No strong memory match; using parser fallback.");
        }

        var helperIndex = Math.Abs(_roundRobinIndex++ % helpers.Count);
        return new PetMemoryRoutingDecision(helpers[helperIndex], UsedMemory: false, "No memory match; using round-robin fallback.");
    }
}
