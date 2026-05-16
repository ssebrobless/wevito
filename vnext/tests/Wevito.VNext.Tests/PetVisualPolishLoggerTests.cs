using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using Microsoft.Data.Sqlite;

namespace Wevito.VNext.Tests;

public sealed class PetVisualPolishLoggerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

    [Fact]
    public void RateLimitsToOncePerMinute()
    {
        using var temp = new TempLedger();
        var service = new PetVisualPolishLogger(temp.Ledger);

        var first = service.RecordMicroBehavior(PetMicroBehavior.Blink, Now);
        var second = service.RecordMicroBehavior(PetMicroBehavior.LookAround, Now.AddSeconds(30));

        Assert.True(first);
        Assert.False(second);
        Assert.Single(temp.Ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)));
        Assert.Equal(1, service.PendingEventCount);
    }

    [Fact]
    public void AggregatesEventCountsBeforeFlush()
    {
        using var temp = new TempLedger();
        var service = new PetVisualPolishLogger(temp.Ledger);
        service.RecordMicroBehavior(PetMicroBehavior.Blink, Now);
        service.RecordMicroBehavior(PetMicroBehavior.TailWag, Now.AddSeconds(10));
        service.RecordParticleEffect("happy", Now.AddSeconds(20));

        var flushed = service.RecordParticleEffect("walk", Now.AddSeconds(61));

        Assert.True(flushed);
        var rows = temp.Ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(2));
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, row =>
            row.Summary.Contains("TailWag:1", StringComparison.OrdinalIgnoreCase) &&
            row.Summary.Contains("happy:1", StringComparison.OrdinalIgnoreCase) &&
            row.Summary.Contains("walk:1", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, service.PendingEventCount);
    }

    [Fact]
    public void RespectsKillSwitch()
    {
        using var temp = new TempLedger();
        var killSwitch = new KillSwitchService(
            () => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [KillSwitchService.KillSwitchSetting] = bool.TrueString
            },
            temp.Ledger);
        var service = new PetVisualPolishLogger(temp.Ledger, killSwitch);

        var recorded = service.RecordMicroBehavior(PetMicroBehavior.Blink, Now);

        Assert.False(recorded);
        Assert.Empty(temp.Ledger.Snapshot(Now.AddMinutes(-1), Now.AddMinutes(1)));
        Assert.Equal(0, service.PendingEventCount);
    }

    private sealed class TempLedger : IDisposable
    {
        private readonly string _directory;

        public TempLedger()
        {
            _directory = Path.Combine(Path.GetTempPath(), "wevito-pet-visual-polish-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            Ledger = new AuditLedgerService(Path.Combine(_directory, "ledger.sqlite"));
        }

        public AuditLedgerService Ledger { get; }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (Directory.Exists(_directory))
                    {
                        Directory.Delete(_directory, recursive: true);
                    }

                    return;
                }
                catch (IOException) when (attempt < 2)
                {
                    Thread.Sleep(50);
                }
            }
        }
    }
}
