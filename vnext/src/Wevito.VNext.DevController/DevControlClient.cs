using System.IO;
using System.IO.Pipes;
using System.Text;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.DevController;

internal sealed class DevControlClient
{
    private const string PipeName = "wevito-vnext-dev-control";

    public Task<DevControlResponseEnvelope> GetSnapshotAsync()
    {
        return SendAsync(DevControlCommandTypes.GetSnapshot, new DevControlGetSnapshotRequest());
    }

    public Task<DevControlResponseEnvelope> DeletePetAsync(DevControlDeletePetRequest request)
    {
        return SendAsync(DevControlCommandTypes.DeletePet, request);
    }

    public Task<DevControlResponseEnvelope> SpawnOrReplacePetAsync(DevControlSpawnOrReplacePetRequest request)
    {
        return SendAsync(DevControlCommandTypes.SpawnOrReplacePet, request);
    }

    public Task<DevControlResponseEnvelope> ApplyActionAsync(DevControlApplyActionRequest request)
    {
        return SendAsync(DevControlCommandTypes.ApplyAction, request);
    }

    public Task<DevControlResponseEnvelope> RoamAsync(DevControlRoamRequest request)
    {
        return SendAsync(DevControlCommandTypes.Roam, request);
    }

    public Task<DevControlResponseEnvelope> ForceAnimationAsync(VisualQaForceAnimationRequest request)
    {
        return SendAsync(VisualQaCommandTypes.ForceAnimation, request);
    }

    public Task<DevControlResponseEnvelope> ClearForcedAnimationAsync(VisualQaClearForcedAnimationRequest request)
    {
        return SendAsync(VisualQaCommandTypes.ClearForcedAnimation, request);
    }

    public Task<DevControlResponseEnvelope> GetAssetSourceAsync(VisualQaGetAssetSourceRequest request)
    {
        return SendAsync(VisualQaCommandTypes.GetAssetSource, request);
    }

    private static async Task<DevControlResponseEnvelope> SendAsync<TPayload>(string commandType, TPayload payload)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(timeout.Token);

        await using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true)
        {
            AutoFlush = true
        };
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, leaveOpen: true);

        await writer.WriteLineAsync(DevControlPipeMessage.SerializeCommand(commandType, payload));
        var line = await reader.ReadLineAsync(timeout.Token);
        if (string.IsNullOrWhiteSpace(line))
        {
            throw new InvalidOperationException("Wevito dev-control returned an empty response.");
        }

        return DevControlPipeMessage.DeserializeResponse(line);
    }
}
