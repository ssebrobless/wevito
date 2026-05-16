using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AiIdentityServiceTests
{
    [Fact]
    public void DefaultsToWevito()
    {
        var service = new AiIdentityService();

        Assert.Equal("Wevito", service.GetAiName(new Dictionary<string, string>()));
    }

    [Fact]
    public async Task PersistsCustomNameAcrossRestart()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-ai-identity-tests", Guid.NewGuid().ToString("N"));
        var repository = new AppRepository(Path.Combine(root, "state.db"));
        await repository.InitializeAsync();
        var service = new AiIdentityService();
        var settings = service.SetAiName(new Dictionary<string, string>(), "Milo");
        var state = new CompanionState(
            CompanionMode.Focused,
            IsPinned: false,
            ActiveEnvironmentId: "pond",
            ActiveTool: new ToolSession("helpers", true),
            ActivePets: [],
            BasketItems: [],
            SettingsSnapshot: settings,
            TaskCards: []);

        await repository.SaveAsync(state);

        var loaded = await repository.LoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal("Milo", service.GetAiName(loaded.SettingsSnapshot));
    }

    [Fact]
    public void EmitsPacketOnNameChange()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-ai-identity-tests", Guid.NewGuid().ToString("N"));
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var service = new AiIdentityService(ledger);
        var timestamp = DateTimeOffset.Parse("2026-05-15T12:00:00Z");

        service.SetAiName(new Dictionary<string, string>(), "Wisp", timestamp);

        var row = Assert.Single(ledger.Snapshot(timestamp.AddMinutes(-1), timestamp.AddMinutes(1)));
        Assert.Equal("ai_identity_set", row.PacketKind);
        Assert.False(row.DidUseHostedAi);
        Assert.False(row.DidMutate);
    }
}
