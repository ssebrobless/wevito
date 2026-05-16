using System.Collections.Concurrent;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class AgentToolConcurrencyCoordinator
{
    private readonly ConcurrentDictionary<Guid, InFlightToolCall> _inFlight = new();
    private readonly SemaphoreSlim _modelGenerationGate = new(1, 1);

    public IReadOnlyList<InFlightToolCall> Snapshot()
    {
        return _inFlight.Values.OrderBy(call => call.StartedAtUtc).ToList();
    }

    public Task<T> RunToolAsync<T>(
        AgentSlot slot,
        string toolFamily,
        Func<CancellationToken, Task<T>> work,
        Guid? taskCardId = null,
        CancellationToken cancellationToken = default)
    {
        return TrackAsync(slot, toolFamily, IsModelGeneration: false, work, taskCardId, cancellationToken);
    }

    public async Task<T> RunModelGenerationAsync<T>(
        AgentSlot slot,
        string toolFamily,
        Func<CancellationToken, Task<T>> work,
        Guid? taskCardId = null,
        CancellationToken cancellationToken = default)
    {
        await _modelGenerationGate.WaitAsync(cancellationToken);
        try
        {
            return await TrackAsync(slot, toolFamily, IsModelGeneration: true, work, taskCardId, cancellationToken);
        }
        finally
        {
            _modelGenerationGate.Release();
        }
    }

    private async Task<T> TrackAsync<T>(
        AgentSlot slot,
        string toolFamily,
        bool IsModelGeneration,
        Func<CancellationToken, Task<T>> work,
        Guid? taskCardId,
        CancellationToken cancellationToken)
    {
        var call = new InFlightToolCall(
            Guid.NewGuid(),
            slot.Id,
            slot.SlotIndex,
            toolFamily,
            IsModelGeneration,
            DateTimeOffset.UtcNow,
            taskCardId);
        _inFlight[call.Id] = call;
        try
        {
            return await work(cancellationToken);
        }
        finally
        {
            _inFlight.TryRemove(call.Id, out _);
        }
    }
}
