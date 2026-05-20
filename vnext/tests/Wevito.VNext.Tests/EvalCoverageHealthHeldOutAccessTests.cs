using Wevito.VNext.Core.SelfImprovement.Eval;

namespace Wevito.VNext.Tests;

public sealed class EvalCoverageHealthHeldOutAccessTests
{
    [Fact]
    public void Snapshot_ListsCaseIdsButNeverReadsCaseContents()
    {
        var heldOut = new RecordingHeldOutStore(["held-out-secret-001", "held-out-secret-002"]);
        var inDistribution = new RecordingInDistributionStore(["in-dist-secret-001"]);
        var service = new EvalCoverageHealthService(heldOut, inDistribution);

        _ = service.Snapshot(DateTimeOffset.Parse("2026-05-19T12:00:00Z"));

        Assert.Equal(1, heldOut.ListCaseIdsCalls);
        Assert.Equal(0, heldOut.ReadCaseCalls);
        Assert.Equal(1, inDistribution.ListCaseIdsCalls);
        Assert.Equal(0, inDistribution.ReadCaseCalls);
    }

    [Fact]
    public void ServiceSource_DoesNotReferenceReadCase()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "vnext",
            "src",
            "Wevito.VNext.Core",
            "SelfImprovement",
            "Eval",
            "EvalCoverageHealthService.cs"));

        Assert.DoesNotContain("ReadCase", source, StringComparison.Ordinal);
    }

    private sealed class RecordingHeldOutStore(IReadOnlyList<string> ids) : IHeldOutEvalStore
    {
        public int ListCaseIdsCalls { get; private set; }
        public int ReadCaseCalls { get; private set; }

        public IReadOnlyList<string> ListCaseIds()
        {
            ListCaseIdsCalls++;
            return ids;
        }

        public string? ReadCase(string caseId)
        {
            ReadCaseCalls++;
            return "secret";
        }
    }

    private sealed class RecordingInDistributionStore(IReadOnlyList<string> ids) : IInDistributionEvalStore
    {
        public int ListCaseIdsCalls { get; private set; }
        public int ReadCaseCalls { get; private set; }

        public override IReadOnlyList<string> ListCaseIds()
        {
            ListCaseIdsCalls++;
            return ids;
        }

        public override string? ReadCase(string caseId)
        {
            ReadCaseCalls++;
            return "secret";
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "wevito.godot"))
                || Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
