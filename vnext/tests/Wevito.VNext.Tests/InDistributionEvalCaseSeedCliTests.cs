using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Wevito.VNext.Core.SelfImprovement.Eval;

namespace Wevito.VNext.Tests;

public sealed class InDistributionEvalCaseSeedCliTests
{
    [Fact]
    public void Add_ValidCase_WritesExpectedJson()
    {
        var fixture = CreateFixture();
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = InvokeCli([
            "add",
            "--domain", "sprite-repair",
            "--prompt-file", fixture.PromptFile,
            "--expected-file", fixture.ExpectedFile,
            "--root", fixture.Root,
            "--id", "case-001",
            "--notes", "seeded from test"
        ], output, error);

        Assert.Equal(0, exitCode);
        Assert.Equal("case-001.json", output.ToString().Trim());
        var path = Path.Combine(fixture.Root, "case-001.json");
        Assert.True(File.Exists(path));
        var evalCase = JsonSerializer.Deserialize<InDistributionEvalCase>(File.ReadAllText(path));
        Assert.NotNull(evalCase);
        Assert.Equal(InDistributionEvalCase.CaseKindDiscriminator, evalCase!.CaseKind);
        Assert.Equal("case-001", evalCase.Id);
        Assert.Equal("sprite-repair", evalCase.Domain);
        Assert.Equal(Sha256(File.ReadAllBytes(fixture.PromptFile)), evalCase.PromptSha256);
        Assert.Equal(Sha256(File.ReadAllBytes(fixture.ExpectedFile)), evalCase.ExpectedKindSha256);
    }

    [Fact]
    public void Add_DuplicateId_RefusesWithoutOverwrite()
    {
        var fixture = CreateFixture();
        var args = new[]
        {
            "add",
            "--domain", "sprite-repair",
            "--prompt-file", fixture.PromptFile,
            "--expected-file", fixture.ExpectedFile,
            "--root", fixture.Root,
            "--id", "case-duplicate"
        };

        Assert.Equal(0, InvokeCli(args, new StringWriter(), new StringWriter()));
        var path = Path.Combine(fixture.Root, "case-duplicate.json");
        var before = File.ReadAllText(path);

        var secondExit = InvokeCli(args, new StringWriter(), new StringWriter());

        Assert.NotEqual(0, secondExit);
        Assert.Equal(before, File.ReadAllText(path));
    }

    [Fact]
    public void Add_OutputRootEscape_Refuses()
    {
        var fixture = CreateFixture();

        var exitCode = InvokeCli([
            "add",
            "--domain", "sprite-repair",
            "--prompt-file", fixture.PromptFile,
            "--expected-file", fixture.ExpectedFile,
            "--root", fixture.Root,
            "--id", "..\\escape"
        ], new StringWriter(), new StringWriter());

        Assert.NotEqual(0, exitCode);
        Assert.False(File.Exists(Path.Combine(Directory.GetParent(fixture.Root)!.FullName, "escape.json")));
    }

    [Theory]
    [InlineData("--id", "Bad_ID")]
    [InlineData("--domain", "BadDomain")]
    public void Add_InvalidIdOrDomain_Refuses(string option, string value)
    {
        var fixture = CreateFixture();
        var args = new List<string>
        {
            "add",
            "--domain", "sprite-repair",
            "--prompt-file", fixture.PromptFile,
            "--expected-file", fixture.ExpectedFile,
            "--root", fixture.Root,
            "--id", "case-valid"
        };
        var index = args.IndexOf(option);
        if (index >= 0)
        {
            args[index + 1] = value;
        }
        else
        {
            args.Add(option);
            args.Add(value);
        }

        var exitCode = InvokeCli(args.ToArray(), new StringWriter(), new StringWriter());

        Assert.NotEqual(0, exitCode);
        Assert.Empty(Directory.EnumerateFiles(fixture.Root, "*.json"));
    }

    [Fact]
    public void CliAssembly_DoesNotReferenceRuntimeInDistributionStoreTypes()
    {
        var assembly = LoadCliAssembly();
        var referencedTypes = assembly.GetTypes()
            .SelectMany(type => type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            .SelectMany(ctor => ctor.GetParameters().Select(parameter => parameter.ParameterType))
            .Concat(assembly.GetTypes().SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)).Select(method => method.ReturnType))
            .ToArray();

        Assert.DoesNotContain(referencedTypes, type => type == typeof(IInDistributionEvalStore) || type == typeof(InDistributionEvalStore));
    }

    [Fact]
    public void CliSource_DoesNotImportNetworkClientsOrRuntimeStores()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "vnext", "tools", "Wevito.Tools.InDistributionSeed", "Program.cs"));

        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WebRequest", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IInDistributionEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InDistributionEvalStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AuditLedger", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.Data.Sqlite", source, StringComparison.Ordinal);
    }

    private static int InvokeCli(string[] args, TextWriter output, TextWriter error)
    {
        var programType = LoadCliAssembly().GetType("Wevito.Tools.InDistributionSeed.Program", throwOnError: true)!;
        var run = programType.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!;
        return (int)run.Invoke(null, [args, output, error])!;
    }

    private static Assembly LoadCliAssembly()
    {
        var project = Path.GetFullPath(Path.Combine(FindRepositoryRoot(), "vnext", "tools", "Wevito.Tools.InDistributionSeed", "Wevito.Tools.InDistributionSeed.csproj"));
        var result = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { "build", project, "--nologo" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });
        Assert.NotNull(result);
        result!.WaitForExit();
        Assert.Equal(0, result.ExitCode);
        var assemblyPath = Path.Combine(Path.GetDirectoryName(project)!, "bin", "Debug", "net8.0", "Wevito.Tools.InDistributionSeed.dll");
        return Assembly.LoadFrom(assemblyPath);
    }

    private static CliFixture CreateFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-in-distribution-seed-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var prompt = Path.Combine(root, "prompt.txt");
        var expected = Path.Combine(root, "expected.txt");
        File.WriteAllText(prompt, "repair snake silhouette");
        File.WriteAllText(expected, "sprite-repair");
        return new CliFixture(root, prompt, expected);
    }

    private static string Sha256(byte[] bytes)
    {
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "wevito.godot")) ||
                Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }

    private sealed record CliFixture(string Root, string PromptFile, string ExpectedFile);
}
