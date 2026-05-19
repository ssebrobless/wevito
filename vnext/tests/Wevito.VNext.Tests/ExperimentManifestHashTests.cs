using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;

namespace Wevito.VNext.Tests;

public sealed class ExperimentManifestHashTests
{
    [Fact]
    public void Compute_SameInputs_ReturnsStableLowercaseHexHash()
    {
        var first = ExperimentManifestHash.Compute(Descriptor(), "ReviewOnly", PacketKinds());
        var second = ExperimentManifestHash.Compute(Descriptor(), "ReviewOnly", PacketKinds());

        Assert.Equal(first, second);
        Assert.Matches("^[0-9a-f]{64}$", first);
    }

    [Fact]
    public void Compute_FieldChanges_ChangeHash()
    {
        var baseline = ExperimentManifestHash.Compute(Descriptor(), "ReviewOnly", PacketKinds());

        Assert.NotEqual(baseline, ExperimentManifestHash.Compute(Descriptor(kind: "other-kind"), "ReviewOnly", PacketKinds()));
        Assert.NotEqual(baseline, ExperimentManifestHash.Compute(Descriptor(displayName: "Other display"), "ReviewOnly", PacketKinds()));
        Assert.NotEqual(baseline, ExperimentManifestHash.Compute(Descriptor(description: "Other description"), "ReviewOnly", PacketKinds()));
        Assert.NotEqual(baseline, ExperimentManifestHash.Compute(Descriptor(manifestVersion: "2"), "ReviewOnly", PacketKinds()));
        Assert.NotEqual(baseline, ExperimentManifestHash.Compute(Descriptor(), "ApplyCapable", PacketKinds()));
        Assert.NotEqual(baseline, ExperimentManifestHash.Compute(Descriptor(), "ReviewOnly", [SelfImprovementPacketKinds.ProposalDrafted]));
    }

    [Fact]
    public void Compute_PacketKindsAreSortedBeforeHashing()
    {
        var sorted = ExperimentManifestHash.Compute(Descriptor(), "ReviewOnly",
        [
            SelfImprovementPacketKinds.ProposalDrafted,
            SelfImprovementPacketKinds.DryRunCompleted,
            SelfImprovementPacketKinds.EvalCompleted
        ]);
        var unsorted = ExperimentManifestHash.Compute(Descriptor(), "ReviewOnly",
        [
            SelfImprovementPacketKinds.EvalCompleted,
            SelfImprovementPacketKinds.ProposalDrafted,
            SelfImprovementPacketKinds.DryRunCompleted
        ]);

        Assert.Equal(sorted, unsorted);
    }

    private static ExperimentDescriptor Descriptor(
        string kind = "sprite-repair-batch-proposal",
        string displayName = "Sprite-repair batch proposal",
        string description = "Review-only dry-run proposal for a guarded sprite repair batch.",
        string manifestVersion = "1")
    {
        return new ExperimentDescriptor(
            new ExperimentKind(kind),
            displayName,
            description,
            EnabledByDefault: false,
            ManifestVersion: manifestVersion);
    }

    private static IReadOnlyList<string> PacketKinds()
    {
        return
        [
            SelfImprovementPacketKinds.ProposalDrafted,
            SelfImprovementPacketKinds.DryRunCompleted,
            SelfImprovementPacketKinds.EvalCompleted
        ];
    }
}
