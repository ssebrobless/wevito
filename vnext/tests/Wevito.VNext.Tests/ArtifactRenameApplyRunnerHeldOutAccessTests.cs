using System.Reflection;
using Wevito.VNext.Core.SelfImprovement.Apply;

namespace Wevito.VNext.Tests;

public sealed class ArtifactRenameApplyRunnerHeldOutAccessTests
{
    [Fact]
    public void Apply_runner_exposes_no_held_out_or_in_distribution_eval_dependencies()
    {
        var signatureTypes = typeof(ArtifactRenameApplyRunner)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(ctor => ctor.GetParameters().Select(parameter => parameter.ParameterType))
            .Concat(typeof(ArtifactRenameApplyRunner).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(field => field.FieldType))
            .Select(type => type.FullName ?? type.Name)
            .ToArray();

        Assert.DoesNotContain(signatureTypes, name => name.Contains("HeldOut", StringComparison.Ordinal));
        Assert.DoesNotContain(signatureTypes, name => name.Contains("InDistribution", StringComparison.Ordinal));
        Assert.DoesNotContain(signatureTypes, name => name.Contains("EvalCase", StringComparison.Ordinal));

        var source = File.ReadAllText(SourcePath());
        Assert.DoesNotContain("HeldOut", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InDistribution", source, StringComparison.Ordinal);
        Assert.DoesNotContain("EvalCase", source, StringComparison.Ordinal);
    }

    private static string SourcePath()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            var candidate = Path.Combine(current, "vnext", "src", "Wevito.VNext.Core", "SelfImprovement", "Apply", "ArtifactRenameApplyRunner.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = Directory.GetParent(current)?.FullName ?? "";
        }

        throw new FileNotFoundException("ArtifactRenameApplyRunner.cs not found.");
    }
}
