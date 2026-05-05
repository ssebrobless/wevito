using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public interface IAudioEndpointStatusReader
{
    AudioEndpointStatus ReadDefaultRenderEndpoint(DateTimeOffset inspectedAtUtc);
}

public interface IAudioEndpointController : IAudioEndpointStatusReader
{
    AudioEndpointStatus SetDefaultRenderVolume(double volumePercent, DateTimeOffset changedAtUtc);

    AudioEndpointStatus SetDefaultRenderMute(bool isMuted, DateTimeOffset changedAtUtc);
}
