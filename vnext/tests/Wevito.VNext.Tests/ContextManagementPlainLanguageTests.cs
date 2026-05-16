namespace Wevito.VNext.Tests;

using Wevito.VNext.Core;

public sealed class ContextManagementPlainLanguageTests
{
    [Theory]
    [InlineData(ChatContextBudgetService.ContextBudgetPressurePacketKind)]
    [InlineData(RollingSummarizerService.SummarizationRunPacketKind)]
    [InlineData(RetrievalAutomaticInjector.RetrievalTriggeredPacketKind)]
    [InlineData(ToolResultBudgetService.ToolResultTruncatedPacketKind)]
    [InlineData(ChatColdStorageService.ChatSessionArchivedPacketKind)]
    [InlineData(PinnedContextStore.PinnedMessageAddedPacketKind)]
    [InlineData(PinnedContextStore.PinnedMessageRemovedPacketKind)]
    public void CoversAllContextManagementKinds(string packetKind)
    {
        var explainer = new PlainLanguageExplainer();

        Assert.Contains(packetKind, PlainLanguageExplainer.KnownPacketKinds);
        Assert.False(explainer.ExplainPacketKind(packetKind).StartsWith("Unknown", StringComparison.Ordinal));
    }
}
