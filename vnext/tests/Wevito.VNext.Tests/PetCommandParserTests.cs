using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class PetCommandParserTests
{
    private readonly PetCommandParser _parser = new();
    private readonly Guid _beanId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _pipId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private readonly Guid _nixId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void Parse_ExplicitAtName_RoutesToNamedPet()
    {
        var intent = _parser.Parse("@Bean check the sprite audit", Helpers());

        Assert.Equal(TaskIntentTargetMode.ExplicitPetName, intent.TargetMode);
        Assert.Equal(_beanId, intent.TargetPetId);
        Assert.Equal("Bean", intent.TargetPetNameSnapshot);
        Assert.Equal(TaskKind.ReviewSprites, intent.TaskKind);
        Assert.Equal("spriteAudit", intent.RequestedToolFamily);
        Assert.False(intent.NeedsApproval);
    }

    [Fact]
    public void Parse_NaturalNamePrefix_RoutesToNamedPet()
    {
        var intent = _parser.Parse("Pip, summarize the latest docs", Helpers());

        Assert.Equal(TaskIntentTargetMode.ExplicitPetName, intent.TargetMode);
        Assert.Equal(_pipId, intent.TargetPetId);
        Assert.Equal(TaskKind.SummarizeDocs, intent.TaskKind);
        Assert.Equal("localDocs", intent.RequestedToolFamily);
    }

    [Fact]
    public void Parse_NaturalNamePrefixWithoutComma_RoutesToNamedPet()
    {
        var intent = _parser.Parse("Pip summarize the latest docs", Helpers());

        Assert.Equal(TaskIntentTargetMode.ExplicitPetName, intent.TargetMode);
        Assert.Equal(_pipId, intent.TargetPetId);
        Assert.Equal(TaskKind.SummarizeDocs, intent.TaskKind);
        Assert.Equal("localDocs", intent.RequestedToolFamily);
    }

    [Fact]
    public void Parse_DefaultScoutAddress_RoutesToScout()
    {
        var intent = _parser.Parse("Scout summarize the sprite docs", new PetCommandBarService().BuildDefaultHelperProfiles());

        Assert.Equal(TaskIntentTargetMode.ExplicitPetName, intent.TargetMode);
        Assert.Equal(PetCommandBarService.ScoutHelperId, intent.TargetPetId);
        Assert.Equal("Scout", intent.TargetPetNameSnapshot);
        Assert.Equal(TaskKind.SummarizeDocs, intent.TaskKind);
        Assert.Equal("localDocs", intent.RequestedToolFamily);
    }

    [Fact]
    public void Parse_WithoutName_UsesSelectedPetWhenAvailable()
    {
        var intent = _parser.Parse("make a checklist for the visual gate", Helpers(), selectedPetId: _nixId);

        Assert.Equal(TaskIntentTargetMode.SelectedPet, intent.TargetMode);
        Assert.Equal(_nixId, intent.TargetPetId);
        Assert.Equal("Nix", intent.TargetPetNameSnapshot);
        Assert.Equal(TaskKind.CreateChecklistDraft, intent.TaskKind);
    }

    [Fact]
    public void Parse_WithoutName_RoutesToBestHelperByRole()
    {
        var intent = _parser.Parse("review the sprite audit queue", Helpers());

        Assert.Equal(TaskIntentTargetMode.RouteToBestHelper, intent.TargetMode);
        Assert.Equal(_beanId, intent.TargetPetId);
        Assert.Equal("Bean", intent.TargetPetNameSnapshot);
        Assert.Equal(TaskKind.ReviewSprites, intent.TaskKind);
    }

    [Fact]
    public void Parse_BlockedRiskyCommand_BecomesBlockedTaskCard()
    {
        var intent = _parser.Parse("@Nix upload the docs folder", Helpers());
        var card = _parser.CreateDraftTaskCard(intent, Helpers());

        Assert.Equal(ToolRiskLevel.Blocked, intent.RiskLevel);
        Assert.True(intent.NeedsApproval);
        Assert.NotEmpty(intent.RefusalOrClarificationReason);
        Assert.Equal(TaskCardStatus.Blocked, card.Status);
        Assert.Contains(card.Timeline ?? [], entry => entry.StartsWith("blocked:", StringComparison.Ordinal));
    }

    [Fact]
    public void CreateDraftTaskCard_MediumRiskBuildWaitsForApproval()
    {
        var intent = _parser.Parse("Nix, run a build proof", Helpers());
        var card = _parser.CreateDraftTaskCard(intent, Helpers());

        Assert.Equal(TaskKind.BuildProof, intent.TaskKind);
        Assert.Equal(ToolRiskLevel.Medium, intent.RiskLevel);
        Assert.True(intent.NeedsApproval);
        Assert.Equal(TaskCardStatus.WaitingForApproval, card.Status);
        Assert.Equal("Nix", card.AssignedPetNameSnapshot);
    }

    [Fact]
    public void Parse_UnknownPetName_BlocksInsteadOfExecuting()
    {
        var intent = _parser.Parse("@Juniper check the sprite audit", Helpers());
        var card = _parser.CreateDraftTaskCard(intent, Helpers());

        Assert.Equal(TaskIntentTargetMode.ExplicitPetName, intent.TargetMode);
        Assert.Equal("Juniper", intent.TargetPetNameSnapshot);
        Assert.Null(intent.TargetPetId);
        Assert.Equal(ToolRiskLevel.Blocked, intent.RiskLevel);
        Assert.Equal(TaskCardStatus.Blocked, card.Status);
    }

    [Fact]
    public void Parse_SpriteAuditExtractsSimpleRowTarget()
    {
        var intent = _parser.Parse("@Bean review goose baby female blue sprites", Helpers());

        Assert.Equal(TaskKind.ReviewSprites, intent.TaskKind);
        Assert.Equal("spriteAudit", intent.RequestedToolFamily);
        Assert.Contains(Path.Combine("goose", "baby", "female", "blue"), intent.TargetPathsOrAssets ?? [], StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_PetStateCommandRoutesToReadOnlyPetStateAdapter()
    {
        var intent = _parser.Parse("review pet state and wellbeing", Helpers());

        Assert.Equal(TaskIntentTargetMode.RouteToBestHelper, intent.TargetMode);
        Assert.Equal(_pipId, intent.TargetPetId);
        Assert.Equal(TaskKind.ReviewPetState, intent.TaskKind);
        Assert.Equal("petState", intent.RequestedToolFamily);
        Assert.False(intent.NeedsApproval);
    }

    [Fact]
    public void Parse_AssetInventoryCommandRoutesToReadOnlyInventoryAdapter()
    {
        var intent = _parser.Parse("inventory assets for Wevito", Helpers());

        Assert.Equal(TaskIntentTargetMode.RouteToBestHelper, intent.TargetMode);
        Assert.Equal(_beanId, intent.TargetPetId);
        Assert.Equal(TaskKind.InventoryAssets, intent.TaskKind);
        Assert.Equal("assetInventory", intent.RequestedToolFamily);
        Assert.False(intent.NeedsApproval);
    }

    [Fact]
    public void Parse_TranslateCommandRoutesToReadOnlyTranslationPreview()
    {
        var intent = _parser.Parse("translate \"Hello goose\" to Spanish", Helpers());

        Assert.Equal(TaskIntentTargetMode.RouteToBestHelper, intent.TargetMode);
        Assert.Equal(_nixId, intent.TargetPetId);
        Assert.Equal(TaskKind.TranslateText, intent.TaskKind);
        Assert.Equal("translateText", intent.RequestedToolFamily);
        Assert.False(intent.NeedsApproval);
    }

    [Fact]
    public void Parse_AudioCommandRoutesToReadOnlyAudioAssist()
    {
        var intent = _parser.Parse("check audio volume", Helpers());

        Assert.Equal(TaskIntentTargetMode.RouteToBestHelper, intent.TargetMode);
        Assert.Equal(_nixId, intent.TargetPetId);
        Assert.Equal(TaskKind.AudioAssist, intent.TaskKind);
        Assert.Equal("audioAssist", intent.RequestedToolFamily);
        Assert.False(intent.NeedsApproval);
    }

    [Fact]
    public void Parse_ScreenCaptureCommandRoutesToReadOnlyScreenCapturePreview()
    {
        var intent = _parser.Parse("take a screenshot of the Wevito window", Helpers());

        Assert.Equal(TaskIntentTargetMode.RouteToBestHelper, intent.TargetMode);
        Assert.Equal(_nixId, intent.TargetPetId);
        Assert.Equal(TaskKind.ScreenCapture, intent.TaskKind);
        Assert.Equal("screenCapture", intent.RequestedToolFamily);
        Assert.False(intent.NeedsApproval);
    }

    [Fact]
    public void Parse_RegionScreenshotCommandRoutesToScreenCapturePreview()
    {
        var intent = _parser.Parse("screenshot a region", Helpers());

        Assert.Equal(TaskIntentTargetMode.RouteToBestHelper, intent.TargetMode);
        Assert.Equal(_nixId, intent.TargetPetId);
        Assert.Equal(TaskKind.ScreenCapture, intent.TaskKind);
        Assert.Equal("screenCapture", intent.RequestedToolFamily);
        Assert.Contains("selected-region", intent.ExpectedOutput, StringComparison.OrdinalIgnoreCase);
        Assert.False(intent.NeedsApproval);
    }

    [Fact]
    public void Parse_LastRegionScreenshotCommandRoutesToScreenCapturePreview()
    {
        var intent = _parser.Parse("screenshot last region", Helpers());

        Assert.Equal(TaskIntentTargetMode.RouteToBestHelper, intent.TargetMode);
        Assert.Equal(_nixId, intent.TargetPetId);
        Assert.Equal(TaskKind.ScreenCapture, intent.TaskKind);
        Assert.Equal("screenCapture", intent.RequestedToolFamily);
        Assert.Contains("last-region", intent.ExpectedOutput, StringComparison.OrdinalIgnoreCase);
        Assert.False(intent.NeedsApproval);
    }

    [Fact]
    public void Parse_WevitoRecordingCommandRoutesToScreenCapturePreview()
    {
        var intent = _parser.Parse("record the Wevito window for 5 seconds", Helpers());

        Assert.Equal(TaskIntentTargetMode.RouteToBestHelper, intent.TargetMode);
        Assert.Equal(_nixId, intent.TargetPetId);
        Assert.Equal(TaskKind.ScreenCapture, intent.TaskKind);
        Assert.Equal("screenCapture", intent.RequestedToolFamily);
        Assert.False(intent.NeedsApproval);
    }

    [Fact]
    public void Parse_CodeReviewCommandRoutesToReadOnlyCodeReviewAdapter()
    {
        var intent = _parser.Parse("review code in the shell popup", Helpers());

        Assert.Equal(TaskIntentTargetMode.RouteToBestHelper, intent.TargetMode);
        Assert.Equal(_pipId, intent.TargetPetId);
        Assert.Equal(TaskKind.ReviewCode, intent.TaskKind);
        Assert.Equal("codeReview", intent.RequestedToolFamily);
        Assert.False(intent.NeedsApproval);
    }

    [Fact]
    public void Parse_CodePatchPlanCommandRoutesToReadOnlyPatchPlanAdapter()
    {
        var intent = _parser.Parse("plan a code fix for the shell popup", Helpers());

        Assert.Equal(TaskIntentTargetMode.RouteToBestHelper, intent.TargetMode);
        Assert.Equal(_pipId, intent.TargetPetId);
        Assert.Equal(TaskKind.PlanCodePatch, intent.TaskKind);
        Assert.Equal("codePatchPlan", intent.RequestedToolFamily);
        Assert.False(intent.NeedsApproval);
    }

    [Fact]
    public void Parse_LocalDocsExtractsQuotedWindowsPathTarget()
    {
        var intent = _parser.Parse("Nix, summarize docs in \"C:\\Users\\fishe\\Documents\\projects\\wevito\\docs\"", Helpers());

        Assert.Equal(TaskKind.SummarizeDocs, intent.TaskKind);
        Assert.Equal("localDocs", intent.RequestedToolFamily);
        Assert.Contains("C:\\Users\\fishe\\Documents\\projects\\wevito\\docs", intent.TargetPathsOrAssets ?? [], StringComparer.OrdinalIgnoreCase);
    }

    private IReadOnlyList<PetHelperProfile> Helpers()
    {
        return
        [
            new PetHelperProfile(_beanId, "Bean", PetHelperRole.SpriteReviewHelper),
            new PetHelperProfile(_pipId, "Pip", PetHelperRole.ChecklistHelper),
            new PetHelperProfile(_nixId, "Nix", PetHelperRole.ResearchHelper)
        ];
    }
}
