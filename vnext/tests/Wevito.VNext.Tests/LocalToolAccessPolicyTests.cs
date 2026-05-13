using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LocalToolAccessPolicyTests
{
    [Fact]
    public void EvaluateRead_AllowsPathInsideApprovedRoot()
    {
        var root = CreateTempRoot();
        var docs = Path.Combine(root, "docs");
        Directory.CreateDirectory(docs);
        var file = Path.Combine(docs, "plan.md");
        File.WriteAllText(file, "ok");
        var policy = new LocalToolAccessPolicy(root);

        var decision = policy.EvaluateRead(file, [docs]);

        Assert.Equal(ToolPolicyDecisionStatus.Allowed, decision.Status);
    }

    [Fact]
    public void EvaluateRead_DenylistBeatsAllowlist()
    {
        var root = CreateTempRoot();
        var secrets = Path.Combine(root, "docs", "secrets");
        Directory.CreateDirectory(secrets);
        var file = Path.Combine(secrets, "token.txt");
        File.WriteAllText(file, "nope");
        var policy = new LocalToolAccessPolicy(root);

        var decision = policy.EvaluateRead(file, [Path.Combine(root, "docs")]);

        Assert.Equal(ToolPolicyDecisionStatus.Blocked, decision.Status);
        Assert.Contains("denylisted", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EvaluateRead_BlocksOutsideAllowlistRoot()
    {
        var root = CreateTempRoot();
        var docs = Path.Combine(root, "docs");
        var outside = Path.Combine(root, "outside");
        Directory.CreateDirectory(docs);
        Directory.CreateDirectory(outside);
        var file = Path.Combine(outside, "plan.md");
        File.WriteAllText(file, "nope");
        var policy = new LocalToolAccessPolicy(root);

        var decision = policy.EvaluateRead(file, [docs]);

        Assert.Equal(ToolPolicyDecisionStatus.Blocked, decision.Status);
        Assert.Contains("outside approved", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EvaluateRead_BlocksParentTraversalBeforeNormalization()
    {
        var root = CreateTempRoot();
        var docs = Path.Combine(root, "docs");
        Directory.CreateDirectory(docs);
        var policy = new LocalToolAccessPolicy(root);

        var decision = policy.EvaluateRead(Path.Combine(docs, "..", "docs", "plan.md"), [docs]);

        Assert.Equal(ToolPolicyDecisionStatus.Blocked, decision.Status);
        Assert.Contains("parent traversal", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EvaluateRead_BlocksCaseFoldedTraversalIntoGit()
    {
        var root = CreateTempRoot();
        var git = Path.Combine(root, ".GIT");
        Directory.CreateDirectory(git);
        var file = Path.Combine(git, "config");
        File.WriteAllText(file, "nope");
        var policy = new LocalToolAccessPolicy(root);

        var decision = policy.EvaluateRead(file, [root]);

        Assert.Equal(ToolPolicyDecisionStatus.Blocked, decision.Status);
        Assert.Contains("denylisted", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EvaluateRead_BlocksSymlinkWhenPlatformAllowsCreatingOne()
    {
        var root = CreateTempRoot();
        var docs = Path.Combine(root, "docs");
        var outside = Path.Combine(root, "outside");
        var link = Path.Combine(docs, "linked");
        Directory.CreateDirectory(docs);
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(outside, "secret.md"), "nope");

        try
        {
            Directory.CreateSymbolicLink(link, outside);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        {
            return;
        }

        var policy = new LocalToolAccessPolicy(root);
        var decision = policy.EvaluateRead(Path.Combine(link, "secret.md"), [docs]);

        Assert.Equal(ToolPolicyDecisionStatus.Blocked, decision.Status);
        Assert.Contains("symlink", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-local-tool-access-policy-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
