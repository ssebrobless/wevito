using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Wevito.VNext.Core.SelfImprovement;

public sealed class ExperimentManifestHash
{
    public ExperimentManifestHash(KillSwitchService killSwitchService)
    {
        _ = killSwitchService;
    }

    public static string Compute(
        ExperimentDescriptor descriptor,
        string mutationPosture,
        IEnumerable<string> packetKindsTouched)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(packetKindsTouched);

        var canonical = new
        {
            descriptor.Kind.Value,
            descriptor.DisplayName,
            descriptor.Description,
            descriptor.ManifestVersion,
            mutationPosture,
            PacketKindsTouched = packetKindsTouched
                .Order(StringComparer.Ordinal)
                .ToArray()
        };
        var json = JsonSerializer.Serialize(canonical, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = null
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }
}
