using Wevito.VNext.Core;
using Wevito.VNext.Core.SelfImprovement;

namespace Wevito.VNext.Tests;

public sealed class ApplyRunnerPrerequisiteHeldOutAccessTests
{
    [Fact]
    public void ServiceCallsOnlyHeldOutListCaseIds()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-prereq-held-out-access", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        _ = ledger.Snapshot(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1));
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [ApplyRunnerPrerequisiteCheckService.EnabledSetting] = bool.TrueString
        };
        var heldOut = RecordingHeldOutStore.WithCases("case-001");
        var service = new ApplyRunnerPrerequisiteCheckService(
            Path.Combine(root, "vnext", "artifacts"),
            ledger.DatabasePath,
            ledger,
            heldOut,
            RecordingInDistributionStore.WithCases("case-001"),
            settingsProvider: () => settings);

        service.Check("operation-held-out-access", DateTimeOffset.UtcNow);

        Assert.Equal(["ListCaseIds"], heldOut.Calls);
    }
}
