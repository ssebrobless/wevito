using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement;

namespace Wevito.VNext.Tests;

public sealed class ScopeHashTests
{
    [Fact]
    public void Compute_SameInputs_ReturnsSameLowercaseHexHash()
    {
        var first = ScopeHash.Compute(Inputs());
        var second = ScopeHash.Compute(Inputs());

        Assert.Equal(first, second);
        Assert.Matches("^[0-9a-f]{64}$", first);
    }

    [Fact]
    public void Compute_FieldChange_ChangesHash()
    {
        var baseline = ScopeHash.Compute(Inputs());
        var changed = ScopeHash.Compute(Inputs() with { EvalSha256 = new string('b', 64) });

        Assert.NotEqual(baseline, changed);
    }

    [Fact]
    public void Compute_ExperimentManifestHashChange_ChangesHash()
    {
        var baseline = ScopeHash.Compute(Inputs());
        var changed = ScopeHash.Compute(Inputs(experimentManifestHash: new string('f', 64)));

        Assert.NotEqual(baseline, changed);
    }

    [Fact]
    public void Compute_EmptyExperimentManifestHashDiffersFromNonEmpty()
    {
        var empty = ScopeHash.Compute(Inputs(experimentManifestHash: ""));
        var nonEmpty = ScopeHash.Compute(Inputs(experimentManifestHash: new string('e', 64)));

        Assert.NotEqual(empty, nonEmpty);
    }

    [Fact]
    public void Compute_PacketKindsAreSortedBeforeHashing()
    {
        var sorted = ScopeHash.Compute(Inputs(packetKinds:
        [
            SelfImprovementPacketKinds.ProposalDrafted,
            SelfImprovementPacketKinds.DryRunCompleted,
            SelfImprovementPacketKinds.EvalCompleted
        ]));
        var unsorted = ScopeHash.Compute(Inputs(packetKinds:
        [
            SelfImprovementPacketKinds.EvalCompleted,
            SelfImprovementPacketKinds.ProposalDrafted,
            SelfImprovementPacketKinds.DryRunCompleted
        ]));

        Assert.Equal(sorted, unsorted);
    }

    private static ScopeHashInputs Inputs(IReadOnlyList<string>? packetKinds = null, string experimentManifestHash = "manifest-hash-001")
    {
        // MIGRATED in C-PHASE 163: manifest hash folded into scope hash.
        return new ScopeHashInputs(
            "sprite-repair-batch-proposal",
            "apply-candidate-001",
            new string('1', 64),
            new string('2', 64),
            new string('3', 64),
            "1",
            packetKinds ??
            [
                SelfImprovementPacketKinds.ProposalDrafted,
                SelfImprovementPacketKinds.DryRunCompleted,
                SelfImprovementPacketKinds.EvalCompleted
            ],
            experimentManifestHash);
    }
}
