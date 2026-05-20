using Wevito.VNext.Core.Sandbox;

namespace Wevito.VNext.Tests;

public sealed class MutationScopeGuardTests
{
    [Fact]
    public void ThrowIfOutsideScope_AllowsLegalPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-mutation-scope", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(root, "operation", "scope", "artifact.draft.json");

        MutationScopeGuard.ThrowIfOutsideScope(path, root, "artifact-root");
    }

    [Fact]
    public void ThrowIfOutsideScope_ThrowsOnAbsolutePathOutsideScope()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-mutation-scope", Guid.NewGuid().ToString("N"));
        var outside = Path.Combine(Path.GetTempPath(), "wevito-outside", Guid.NewGuid().ToString("N"), "artifact.draft.json");

        var ex = Assert.Throws<InvalidOperationException>(() => MutationScopeGuard.ThrowIfOutsideScope(outside, root, "artifact-root"));

        Assert.Contains("artifact-root", ex.Message, StringComparison.Ordinal);
        Assert.Contains(outside, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowIfOutsideScope_ThrowsOnRelativePathThatEscapesScope()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-mutation-scope", Guid.NewGuid().ToString("N"));
        var escaping = Path.Combine(root, "..", "outside", "artifact.draft.json");

        Assert.Throws<InvalidOperationException>(() => MutationScopeGuard.ThrowIfOutsideScope(escaping, root, "artifact-root"));
    }

    [Fact]
    public void ThrowIfOutsideScope_AllowsAlternateCaseScopePrefixOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var root = Path.Combine(Path.GetTempPath(), "wevito-mutation-scope", Guid.NewGuid().ToString("N"));
        var alteredCaseRoot = root.ToUpperInvariant();
        var path = Path.Combine(alteredCaseRoot, "operation", "scope", "artifact.draft.json");

        MutationScopeGuard.ThrowIfOutsideScope(path, root, "artifact-root");
    }

    [Theory]
    [InlineData(null, "root", "scope")]
    [InlineData("", "root", "scope")]
    [InlineData("path", null, "scope")]
    [InlineData("path", "", "scope")]
    [InlineData("path", "root", null)]
    [InlineData("path", "root", "")]
    public void ThrowIfOutsideScope_ThrowsOnNullOrEmptyInput(string? intendedPath, string? scopeRoot, string? scopeName)
    {
        Assert.ThrowsAny<ArgumentException>(() => MutationScopeGuard.ThrowIfOutsideScope(intendedPath!, scopeRoot!, scopeName!));
    }
}
