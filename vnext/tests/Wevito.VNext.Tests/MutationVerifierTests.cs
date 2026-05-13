using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class MutationVerifierTests
{
    [Fact]
    public void BuildPostProofCommands_MatchesFileTypes()
    {
        var root = CreateTempRoot();
        var verifier = new MutationVerifier();

        var commands = verifier.BuildPostProofCommands(root, [
            Path.Combine(root, "vnext", "src", "Wevito.VNext.Core", "Example.cs"),
            Path.Combine(root, "vnext", "tests", "Wevito.VNext.Tests", "ExampleTests.cs"),
            Path.Combine(root, "sprites_runtime", "goose", "baby", "female", "blue", "idle_00.png")
        ]);

        Assert.Contains(commands, command => command.CommandId == "dotnet-build");
        Assert.Contains(commands, command => command.CommandId == "dotnet-test");
        Assert.Contains(commands, command => command.CommandId == "sprite-contract");
        Assert.Contains(commands, command => command.CommandId == "runtime-canvas");
    }

    [Fact]
    public void VerifyHashes_ConfirmsCurrentBytes()
    {
        var root = CreateTempRoot();
        var file = Path.Combine(root, "vnext", "content", "example.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "before");
        var verifier = new MutationVerifier();
        var hash = new GuardedMutationFileHash(file, GuardedMutationService.ComputeSha256(file), Exists: true);

        Assert.True(verifier.VerifyHashes([hash]));
        File.WriteAllText(file, "after");
        Assert.False(verifier.VerifyHashes([hash]));
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-mutation-verifier-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
