using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class GameModeDetectorServiceTests
{
    [Fact]
    public void DetectsGameModeFromRegistry()
    {
        var service = new GameModeDetectorService(registryReader: () => 1);

        var result = service.Evaluate();

        Assert.True(result.IsGameModeActive);
        Assert.Equal("registry", result.Source);
    }

    [Fact]
    public void SignalsCoexistenceTriggerOnDetect()
    {
        var service = new CoexistenceTriggerService();

        var result = service.Evaluate(
            new Dictionary<string, string>(),
            null,
            new CoexistenceResourceSnapshot(GameModeActive: true),
            DateTimeOffset.Parse("2026-05-15T12:00:00Z"));

        Assert.True(result.IsQuieting);
        Assert.Contains("game_mode", result.ActiveTriggers);
    }
}
