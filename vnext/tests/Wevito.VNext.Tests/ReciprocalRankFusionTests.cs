using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class ReciprocalRankFusionTests
{
    [Fact]
    public void Fuse_IsDeterministicForSameInputs()
    {
        var left = new[] { "a", "b", "c" };
        var right = new[] { "c", "a", "d" };

        var first = ReciprocalRankFusion.Fuse(left, right);
        var second = ReciprocalRankFusion.Fuse(left, right);

        Assert.Equal(first, second);
        Assert.True(first["a"] > first["b"]);
        Assert.True(first["c"] > first["d"]);
    }
}
