using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class FirstRunSpriteCleanupCardTests
{
    [Fact]
    public void SelectingSpriteCleanupHelpAppendsDraftCard()
    {
        var result = ApplySpriteCleanupChoice();

        Assert.True(result.DraftedSpriteCleanupCard);
        Assert.Single(result.TaskCards);
        Assert.Equal(TaskCardStatus.Draft, result.TaskCards[0].Status);
    }

    [Fact]
    public void DraftCardUsesTaskKindReviewSprites()
    {
        var card = ApplySpriteCleanupChoice().DraftedCard;

        Assert.NotNull(card);
        Assert.Equal(TaskKind.ReviewSprites, card!.Intent.TaskKind);
    }

    [Fact]
    public void DraftCardCarriesSpriteAuditToolFamily()
    {
        var card = ApplySpriteCleanupChoice().DraftedCard;

        Assert.NotNull(card);
        Assert.Equal("spriteAudit", card!.ToolFamily);
        Assert.Equal("spriteAudit", card.Intent.RequestedToolFamily);
    }

    [Fact]
    public void DraftCardHasNeedsApprovalTrue()
    {
        var card = ApplySpriteCleanupChoice().DraftedCard;

        Assert.NotNull(card);
        Assert.True(card!.Intent.NeedsApproval);
        Assert.Equal(ToolRiskLevel.Low, card.Intent.RiskLevel);
    }

    [Fact]
    public void AuditPacketRecordedAlongsideCard()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-first-run-card-tests", Guid.NewGuid().ToString("N"));
        var ledger = new AuditLedgerService(Path.Combine(root, "ledger.sqlite"));
        var service = new FirstLaunchWizardStateService(auditLedgerService: ledger);
        var timestamp = DateTimeOffset.Parse("2026-05-16T12:00:00Z");

        service.ApplyInlineChoice(
            new Dictionary<string, string>(),
            [],
            new AgentTaskCardQueueService(),
            FirstLaunchBackgroundChoice.HelpWithSpriteCleanup,
            timestamp);

        var rows = ledger.Snapshot(timestamp.AddMinutes(-1), timestamp.AddMinutes(1));
        Assert.Contains(rows, row => row.PacketKind == FirstLaunchWizardStateService.FirstRunChoiceRecordedPacketKind);
        Assert.Contains(rows, row => row.PacketKind == FirstLaunchWizardStateService.SpriteCleanupHelpCardDraftedPacketKind);
    }

    [Fact]
    public void RespectsKillSwitch()
    {
        var service = new FirstLaunchWizardStateService(killSwitchService: new KillSwitchService());
        var settings = new Dictionary<string, string>
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        };

        var result = service.ApplyInlineChoice(
            settings,
            [],
            new AgentTaskCardQueueService(),
            FirstLaunchBackgroundChoice.HelpWithSpriteCleanup);

        Assert.False(result.DraftedSpriteCleanupCard);
        Assert.Empty(result.TaskCards);
    }

    private static FirstLaunchChoiceResult ApplySpriteCleanupChoice()
    {
        var service = new FirstLaunchWizardStateService();
        return service.ApplyInlineChoice(
            new Dictionary<string, string>(),
            [],
            new AgentTaskCardQueueService(),
            FirstLaunchBackgroundChoice.HelpWithSpriteCleanup,
            DateTimeOffset.Parse("2026-05-16T12:00:00Z"));
    }
}
