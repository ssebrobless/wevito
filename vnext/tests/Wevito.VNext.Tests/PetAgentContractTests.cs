using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Tests;

public sealed class PetAgentContractTests
{
    [Fact]
    public void ActiveHelperRoster_RepresentsThreeNamedHelpersWithoutGrantingToolAccess()
    {
        var helpers = new[]
        {
            new PetHelperProfile(Guid.NewGuid(), "Bean", PetHelperRole.SpriteReviewHelper),
            new PetHelperProfile(Guid.NewGuid(), "Pip", PetHelperRole.ChecklistHelper),
            new PetHelperProfile(Guid.NewGuid(), "Nix", PetHelperRole.ResearchHelper)
        };

        var roster = new ActiveHelperRoster(helpers);

        Assert.Equal(PetAgentContractLimits.MaxActiveHelpers, roster.Helpers.Count);
        Assert.All(roster.Helpers, helper => Assert.Null(helper.AllowedToolFamilies));
        Assert.Contains(roster.Helpers, helper => helper.PetNameSnapshot == "Bean");
    }

    [Fact]
    public void TaskIntent_RoundTripsWithCamelCaseStringEnums()
    {
        var intent = new TaskIntent(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Bean, check the sprite audit",
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetNameSnapshot: "Bean",
            TaskKind: TaskKind.ReviewSprites,
            RequestedToolFamily: "spriteAudit",
            TargetPathsOrAssets: new[] { "sprites_runtime/goose/baby/female/blue" },
            RiskLevel: ToolRiskLevel.Low,
            ExpectedOutput: "No-mutation audit summary");

        var json = JsonSerializer.Serialize(intent, JsonDefaults.Options);
        var roundTrip = JsonSerializer.Deserialize<TaskIntent>(json, JsonDefaults.Options);

        Assert.Contains("\"targetMode\":\"explicitPetName\"", json);
        Assert.Contains("\"taskKind\":\"reviewSprites\"", json);
        Assert.NotNull(roundTrip);
        Assert.Equal("Bean", roundTrip.TargetPetNameSnapshot);
        Assert.Equal(TaskKind.ReviewSprites, roundTrip.TaskKind);
        Assert.False(roundTrip.NeedsApproval);
    }

    [Fact]
    public void TaskCard_KeepsPetRoutingSeparateFromToolPolicy()
    {
        var petId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var intent = new TaskIntent(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "@Nix summarize the latest docs",
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetId: petId,
            TargetPetNameSnapshot: "Nix",
            TaskKind: TaskKind.SummarizeDocs,
            RequestedToolFamily: "localDocs",
            RiskLevel: ToolRiskLevel.Low);
        var policy = new ToolPolicy(
            "local-docs-readonly",
            "localDocs",
            ToolAccessMode.ReadOnly,
            ToolRiskLevel.Low,
            ApprovalRequirement.None,
            ApprovedRootPaths: new[] { @"C:\Users\fishe\Documents\projects\wevito\docs" });

        var card = new TaskCard(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            intent,
            TaskCardStatus.Draft,
            AssignedPetId: petId,
            AssignedPetNameSnapshot: "Nix",
            ToolFamily: "localDocs",
            PolicySnapshot: policy);

        Assert.Equal("Nix", card.AssignedPetNameSnapshot);
        Assert.Equal(ToolAccessMode.ReadOnly, card.PolicySnapshot?.AccessMode);
        Assert.Equal(ApprovalRequirement.None, card.PolicySnapshot?.ApprovalRequirement);
        Assert.Equal(PetHelperRole.ResearchHelper, PetHelperRole.ResearchHelper);
    }

    [Fact]
    public void PetCommandBarState_RoundTripsQueuedTaskCards()
    {
        var petId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var intent = new TaskIntent(
            Guid.Parse("77777777-7777-7777-7777-777777777777"),
            "Bean, review goose sprites",
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetId: petId,
            TargetPetNameSnapshot: "Bean",
            TaskKind: TaskKind.ReviewSprites,
            RequestedToolFamily: "spriteAudit");
        var card = new TaskCard(
            intent.Id,
            intent,
            TaskCardStatus.Draft,
            AssignedPetId: petId,
            AssignedPetNameSnapshot: "Bean",
            ToolFamily: "spriteAudit");
        var state = new PetCommandBarState(
            [new PetHelperProfile(petId, "Bean", PetHelperRole.SpriteReviewHelper)],
            LastIntent: intent,
            LastTaskCard: card,
            StatusMessage: "1 saved",
            QueuedTaskCards: [card],
            WellbeingSnapshots:
            [
                new PetWellbeingSnapshot(
                    petId,
                    "Bean",
                    "goose",
                    PetAgeStage.Baby,
                    PetGender.Female,
                    "blue",
                    PetWellbeingUrgency.Watch,
                    PetDriveFamily.SocialConnection,
                    PetEmotionChannel.Attachment,
                    "Bean should be watched for affection.",
                    new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["affection"] = 34
                    },
                    ["playful", "social"],
                    [],
                    [])
            ]);

        var json = JsonSerializer.Serialize(state, JsonDefaults.Options);
        var roundTrip = JsonSerializer.Deserialize<PetCommandBarState>(json, JsonDefaults.Options);

        Assert.Contains("\"queuedTaskCards\"", json);
        Assert.Contains("\"wellbeingSnapshots\"", json);
        Assert.NotNull(roundTrip);
        Assert.Single(roundTrip.QueuedTaskCards ?? []);
        Assert.Equal(TaskCardStatus.Draft, roundTrip.QueuedTaskCards![0].Status);
        Assert.Single(roundTrip.WellbeingSnapshots ?? []);
        Assert.Equal(PetDriveFamily.SocialConnection, roundTrip.WellbeingSnapshots![0].DominantDrive);
    }

    [Fact]
    public void LearningFeedback_DefaultsToNotApprovedForDataset()
    {
        var feedback = new LearningFeedback(
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            TaskCardId: null,
            PetNameSnapshot: "Pip",
            LearningFeedbackLabel.Accepted,
            "Helpful summary, but keep future reports shorter.");

        Assert.False(feedback.ApprovedForDataset);
    }

    [Fact]
    public void TaskAdapterResult_RecordsNoMutationPreview()
    {
        var request = new TaskAdapterRequest(
            Guid.Parse("88888888-8888-8888-8888-888888888888"),
            new TaskIntent(
                Guid.Parse("99999999-9999-9999-9999-999999999999"),
                "Bean, summarize local docs",
                TaskIntentTargetMode.ExplicitPetName,
                TaskKind: TaskKind.SummarizeDocs,
                RequestedToolFamily: "localDocs"),
            new ToolPolicy(
                "local-docs-readonly",
                "localDocs",
                ToolAccessMode.ReadOnly,
                ToolRiskLevel.Low,
                ApprovalRequirement.None,
                ApprovedRootPaths: new[] { @"C:\Users\fishe\Documents\projects\wevito\docs" }));
        var result = new TaskAdapterResult(
            request.TaskCardId,
            "localDocs",
            TaskAdapterResultStatus.PreviewReady,
            DidMutate: false,
            ReadPaths: new[] { @"C:\Users\fishe\Documents\projects\wevito\docs\VNEXT_IMPLEMENTATION_CHECKLIST.md" },
            WrittenPaths: [],
            PreviewSummary: "Would read one docs file.");

        var json = JsonSerializer.Serialize(result, JsonDefaults.Options);
        var roundTrip = JsonSerializer.Deserialize<TaskAdapterResult>(json, JsonDefaults.Options);

        Assert.Contains("\"status\":\"previewReady\"", json);
        Assert.Contains("\"didMutate\":false", json);
        Assert.NotNull(roundTrip);
        Assert.False(roundTrip.DidMutate);
        Assert.Equal(TaskAdapterResultStatus.PreviewReady, roundTrip.Status);
    }

    [Fact]
    public void CaptureManifest_RoundTripsWithPolicyFieldsAndNoMutation()
    {
        var requestId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var manifest = new CaptureManifest(
            "wevito.capture.v1",
            requestId,
            CapturePreset.WevitoWindow,
            CaptureTargetKind.WevitoWindow,
            CaptureOutputKind.ScreenshotPng,
            CapturePrivacyLevel.WevitoOnly,
            @"C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\pet-tasks\capture",
            @"C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\pet-tasks\capture\screenshot.png",
            @"C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\pet-tasks\capture\manifest.json",
            @"C:\Users\fishe\Documents\projects\wevito\vnext\artifacts\pet-tasks\capture\run-summary.md",
            IncludeOverlayMetadata: true);
        var result = new CaptureResult(
            requestId,
            TaskAdapterResultStatus.PreviewReady,
            DidCapture: false,
            DidMutate: false,
            Manifest: manifest,
            WrittenPaths: [],
            Summary: "Would capture the Wevito window.");

        var json = JsonSerializer.Serialize(result, JsonDefaults.Options);
        var roundTrip = JsonSerializer.Deserialize<CaptureResult>(json, JsonDefaults.Options);

        Assert.Contains("\"preset\":\"wevitoWindow\"", json);
        Assert.Contains("\"privacyLevel\":\"wevitoOnly\"", json);
        Assert.Contains("\"didMutate\":false", json);
        Assert.NotNull(roundTrip);
        Assert.False(roundTrip.DidCapture);
        Assert.False(roundTrip.DidMutate);
        Assert.Equal(CaptureTargetKind.WevitoWindow, roundTrip.Manifest?.TargetKind);
    }

    [Fact]
    public void CodePatchPlanReport_RoundTripsWithSafetyGatesAndNoMutation()
    {
        var report = new CodePatchPlanReport(
            "1",
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "codePatchPlan",
            [@"C:\Users\fishe\Documents\projects\wevito\vnext\src"],
            [
                new CodePatchPlanFileCandidate(
                    @"C:\Users\fishe\Documents\projects\wevito\vnext\src\Sample.cs",
                    "Sample.cs",
                    "C#",
                    10,
                    200)
            ],
            "Pip, plan a code patch",
            "Prepare a reversible patch.",
            [new CodePatchPlanStep(1, "Inspect", "Read first, then edit only after approval.", ToolRiskLevel.Low)],
            ["dotnet build .\\vnext\\Wevito.VNext.sln"],
            ["Use git diff before rollback."],
            ["No runtime/source PNG mutation."],
            DidMutate: false,
            DateTimeOffset.Parse("2026-05-05T14:00:00Z"));

        var json = JsonSerializer.Serialize(report, JsonDefaults.Options);
        var roundTrip = JsonSerializer.Deserialize<CodePatchPlanReport>(json, JsonDefaults.Options);

        Assert.Contains("\"toolFamily\":\"codePatchPlan\"", json);
        Assert.Contains("\"didMutate\":false", json);
        Assert.NotNull(roundTrip);
        Assert.Equal("codePatchPlan", roundTrip.ToolFamily);
        Assert.False(roundTrip.DidMutate);
        Assert.Single(roundTrip.SafetyGates);
    }

    [Fact]
    public void BuildProofPlanReport_RoundTripsWithoutRunningCommands()
    {
        var report = new BuildProofPlanReport(
            "1",
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "buildProof",
            [
                new BuildProofCommandPlan(
                    1,
                    "powershell -NoProfile -ExecutionPolicy Bypass -File .\\tools\\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests",
                    "Safe publish without asset prep.",
                    ToolRiskLevel.Medium,
                    RequiresApproval: true,
                    MustSkipAssetPrep: true)
            ],
            ["vnext\\artifacts\\shell\\Wevito.VNext.Shell.exe"],
            ["This adapter must not execute commands."],
            ["Any proof path requires asset prep."],
            DidRunCommands: false,
            DidMutate: false,
            DateTimeOffset.Parse("2026-05-05T14:10:00Z"));

        var json = JsonSerializer.Serialize(report, JsonDefaults.Options);
        var roundTrip = JsonSerializer.Deserialize<BuildProofPlanReport>(json, JsonDefaults.Options);

        Assert.Contains("\"toolFamily\":\"buildProof\"", json);
        Assert.Contains("\"didRunCommands\":false", json);
        Assert.NotNull(roundTrip);
        Assert.False(roundTrip.DidRunCommands);
        Assert.False(roundTrip.DidMutate);
        Assert.True(roundTrip.Commands[0].MustSkipAssetPrep);
    }

    [Fact]
    public void TranslationPreviewReport_RoundTripsWithoutProviderCall()
    {
        var report = new TranslationPreviewReport(
            "1",
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            "translateText",
            "Hello goose",
            "auto",
            "Spanish",
            "DeepL",
            11,
            [
                new TranslationProviderStatus(
                    TranslationProviderKind.DeepL,
                    TranslationProviderAvailability.MissingCredentials,
                    SupportsGlossary: true,
                    SupportsSelfHosted: false,
                    "Missing key.")
            ],
            [
                new TranslationGlossaryEntry("goose", "goose", CaseSensitive: false, "Canonical species name.")
            ],
            ["No provider was called."],
            DidCallProvider: false,
            DidMutate: false,
            DateTimeOffset.Parse("2026-05-05T14:20:00Z"));

        var json = JsonSerializer.Serialize(report, JsonDefaults.Options);
        var roundTrip = JsonSerializer.Deserialize<TranslationPreviewReport>(json, JsonDefaults.Options);

        Assert.Contains("\"toolFamily\":\"translateText\"", json);
        Assert.Contains("\"didCallProvider\":false", json);
        Assert.NotNull(roundTrip);
        Assert.False(roundTrip.DidCallProvider);
        Assert.False(roundTrip.DidMutate);
        Assert.Equal("Spanish", roundTrip.TargetLanguage);
        Assert.Equal("goose", roundTrip.ApplicableGlossaryEntries[0].Target);
    }

    [Fact]
    public void TranslationExecutionReport_RoundTripsWithoutLeakingCredentials()
    {
        var report = new TranslationExecutionReport(
            "1",
            Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            "translateText",
            TranslationProviderKind.DeepL,
            "Hello goose",
            "Hola ganso",
            "auto",
            "ES",
            "EN",
            11,
            11,
            "protected-token-shim",
            [
                new TranslationGlossaryEntry("goose", "goose", CaseSensitive: false, "Canonical species name.")
            ],
            ["fallback_used: protected-token-shim."],
            ["API key was not written to artifacts."],
            DidCallProvider: true,
            DidMutate: false,
            DateTimeOffset.Parse("2026-05-05T14:30:00Z"));

        var json = JsonSerializer.Serialize(report, JsonDefaults.Options);
        var roundTrip = JsonSerializer.Deserialize<TranslationExecutionReport>(json, JsonDefaults.Options);

        Assert.Contains("\"provider\":\"deepL\"", json);
        Assert.Contains("\"didCallProvider\":true", json);
        Assert.DoesNotContain("test-key", json, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(roundTrip);
        Assert.True(roundTrip.DidCallProvider);
        Assert.False(roundTrip.DidMutate);
        Assert.Equal("Hola ganso", roundTrip.TranslatedText);
        Assert.Equal("protected-token-shim", roundTrip.GlossaryMode);
        Assert.Contains(roundTrip.QaWarnings, warning => warning.StartsWith("fallback_used:", StringComparison.Ordinal));
    }

    [Fact]
    public void AudioAssistStatusReport_RoundTripsWithoutAudioChanges()
    {
        var report = new AudioAssistStatusReport(
            "1",
            Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            "audioAssist",
            [
                new AudioAssistCapability(
                    AudioAssistActionKind.InspectVolume,
                    AudioAssistCapabilityStatus.NotImplemented,
                    ToolRiskLevel.Low,
                    ApprovalRequirement.None,
                    "Read-only status planned.")
            ],
            "No audio changes.",
            ["No audio settings were changed."],
            DidInspectSystemAudio: false,
            DidChangeAudio: false,
            DidMutate: false,
            DateTimeOffset.Parse("2026-05-05T14:50:00Z"));

        var json = JsonSerializer.Serialize(report, JsonDefaults.Options);
        var roundTrip = JsonSerializer.Deserialize<AudioAssistStatusReport>(json, JsonDefaults.Options);

        Assert.Contains("\"toolFamily\":\"audioAssist\"", json);
        Assert.Contains("\"didChangeAudio\":false", json);
        Assert.NotNull(roundTrip);
        Assert.False(roundTrip.DidInspectSystemAudio);
        Assert.False(roundTrip.DidChangeAudio);
        Assert.False(roundTrip.DidMutate);
    }
}
