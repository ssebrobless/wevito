using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Wevito.VNext.Core.SelfImprovement;

public static class ScopeHash
{
    public static string Compute(ScopeHashInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        var canonical = new
        {
            inputs.ScopeId,
            inputs.OperationId,
            inputs.ProposalSha256,
            inputs.DryRunSha256,
            inputs.EvalSha256,
            inputs.ExperimentManifestVersion,
            inputs.ExperimentManifestHash,
            PacketKindsTouched = (inputs.PacketKindsTouched ?? [])
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
