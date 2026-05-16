namespace Wevito.VNext.Contracts;

public enum PetHelperAvailability
{
    Available,
    Drafting,
    WaitingForApproval,
    Running,
    Reviewing,
    Blocked,
    Done,
    Failed
}

public enum AgentSlotStatus
{
    Idle,
    Drafting,
    Waiting,
    RunningTool,
    Generating,
    Reviewing,
    Blocked,
    Failed
}

public enum TaskOrigin
{
    UserVisible,
    Background
}

public enum TaskIntentTargetMode
{
    ExplicitPetName,
    SelectedPet,
    RouteToBestHelper
}

public enum TaskKind
{
    Unknown,
    SearchLocalDocs,
    SummarizeDocs,
    CreateChecklistDraft,
    CaptureProof,
    ExportNoMutationReport,
    OpenLocalDocument,
    SaveLinkToBasket,
    ReviewSprites,
    InventoryAssets,
    ReviewCode,
    PlanCodePatch,
    ReviewPetState,
    Research,
    BuildProof,
    TranslateText,
    AudioAssist,
    ScreenCapture,
    UpdatePetMemory,
    ExternalAction
}

public enum ToolRiskLevel
{
    Low,
    Medium,
    High,
    Blocked
}

public enum ToolAccessMode
{
    ReadOnly,
    Write,
    Network,
    Browser,
    ExternalCommunication
}

public enum ApprovalRequirement
{
    None,
    BeforeExecution,
    ActionTime,
    HandOffRequired
}

public enum TaskCardStatus
{
    Draft,
    WaitingForApproval,
    Approved,
    Running,
    Reviewing,
    Done,
    Blocked,
    Failed,
    Cancelled
}

public enum ToolPolicyDecisionStatus
{
    Allowed,
    ApprovalRequired,
    Blocked
}

public enum ReviewedExampleKind
{
    PetCommand,
    TaskOutcome,
    SpriteCandidate,
    VisualReview,
    ToolPolicyDecision
}

public enum LearningFeedbackLabel
{
    Accepted,
    Rejected,
    NeedsRevision,
    PreferA,
    PreferB
}

public enum TaskAdapterRunMode
{
    DryRunPreview,
    Execute
}

public enum TaskAdapterResultStatus
{
    PreviewReady,
    Completed,
    Blocked,
    Failed
}

public enum CapturePreset
{
    WevitoWindow,
    ProofSurface,
    CurrentForegroundWindow,
    SelectedRegion,
    LastRegion,
    FullDesktop,
    ShortRecording
}

public enum CaptureTargetKind
{
    WevitoWindow,
    ProofSurface,
    ForegroundWindow,
    SelectedRegion,
    LastRegion,
    FullDesktop
}

public enum CaptureOutputKind
{
    ScreenshotPng,
    ClipMp4,
    ClipGif
}

public enum CapturePrivacyLevel
{
    WevitoOnly,
    SelectedApp,
    SelectedRegion,
    Desktop,
    ExternalShare
}

public enum TranslationProviderKind
{
    DeepL,
    GoogleCloudTranslation,
    AzureAiTranslator,
    LibreTranslate
}

public enum TranslationProviderAvailability
{
    Available,
    Configured,
    MissingCredentials,
    MissingEndpoint,
    Disabled
}

public enum AudioAssistActionKind
{
    InspectVolume,
    SetVolume,
    Mute,
    Unmute,
    BoostGuide,
    BoostHandoff,
    ExternalEnhancerHandoff
}

public enum AudioBoostDetectionStatus
{
    Installed,
    Partial,
    NotInstalled
}

public enum AudioAssistCapabilityStatus
{
    Available,
    ApprovalRequired,
    Blocked,
    NotImplemented
}

public enum ScreenCaptureActionKind
{
    Screenshot,
    WindowScreenshot,
    RegionScreenshot,
    ScreenRecording,
    GifRecording
}

public enum ScreenCaptureCapabilityStatus
{
    PreviewReady,
    ApprovalRequired,
    Blocked,
    NotImplemented
}

public sealed record PetHelperProfile(
    Guid PetId,
    string PetNameSnapshot,
    int SlotIndex,
    AgentSlotStatus AgentStatus = AgentSlotStatus.Idle,
    PetHelperAvailability Availability = PetHelperAvailability.Available,
    Guid? CurrentTaskCardId = null,
    IReadOnlyList<string>? AllowedToolFamilies = null,
    IReadOnlyDictionary<string, string>? PreferenceSnapshot = null);

public sealed record AgentSlot(
    Guid Id,
    int SlotIndex,
    string Name,
    AgentSlotStatus Status = AgentSlotStatus.Idle,
    Guid? CurrentTaskCardId = null,
    DateTimeOffset LastUsedAtUtc = default,
    Guid? PetId = null,
    string Species = "pet",
    string ToolIcon = "",
    string ActiveToolFamily = "");

public sealed record InFlightToolCall(
    Guid Id,
    Guid AgentSlotId,
    int SlotIndex,
    string ToolFamily,
    bool IsModelGeneration,
    DateTimeOffset StartedAtUtc,
    Guid? TaskCardId = null);

public static class PetAgentContractLimits
{
    public const int MaxActiveHelpers = 3;
}

public sealed record ActiveHelperRoster(
    IReadOnlyList<PetHelperProfile> Helpers,
    DateTimeOffset UpdatedAtUtc = default);

public sealed record TaskIntent(
    Guid Id,
    string RawText,
    TaskIntentTargetMode TargetMode,
    Guid? TargetPetId = null,
    string TargetPetNameSnapshot = "",
    TaskKind TaskKind = TaskKind.Unknown,
    string RequestedToolFamily = "",
    IReadOnlyList<string>? TargetPathsOrAssets = null,
    ToolRiskLevel RiskLevel = ToolRiskLevel.Low,
    bool NeedsApproval = false,
    string ExpectedOutput = "",
    string RefusalOrClarificationReason = "",
    DateTimeOffset CreatedAtUtc = default);

public sealed record ToolPolicy(
    string PolicyId,
    string ToolFamily,
    ToolAccessMode AccessMode,
    ToolRiskLevel RiskLevel,
    ApprovalRequirement ApprovalRequirement,
    bool IsEnabled = true,
    IReadOnlyList<string>? ApprovedRootPaths = null,
    string BlockReason = "");

public sealed record ToolDescriptor(
    string ToolId,
    string DisplayName,
    string ToolFamily,
    ToolAccessMode AccessMode,
    string Description,
    ToolRiskLevel DefaultRiskLevel = ToolRiskLevel.Low,
    ApprovalRequirement DefaultApprovalRequirement = ApprovalRequirement.None);

public sealed record ToolPolicyDecision(
    string ToolFamily,
    ToolPolicyDecisionStatus Status,
    ToolRiskLevel RiskLevel,
    ApprovalRequirement ApprovalRequirement,
    ToolPolicy? PolicySnapshot = null,
    string Reason = "");

public sealed record PetCommandBarState(
    IReadOnlyList<PetHelperProfile> ActiveHelpers,
    string InputText = "",
    TaskIntent? LastIntent = null,
    TaskCard? LastTaskCard = null,
    ToolPolicyDecision? LastPolicyDecision = null,
    string StatusMessage = "",
    DateTimeOffset UpdatedAtUtc = default,
    IReadOnlyList<TaskCard>? QueuedTaskCards = null,
    IReadOnlyList<PetWellbeingSnapshot>? WellbeingSnapshots = null);

public sealed record TaskCard(
    Guid Id,
    TaskIntent Intent,
    TaskCardStatus Status,
    Guid? AssignedPetId = null,
    string AssignedPetNameSnapshot = "",
    string ToolFamily = "",
    ToolPolicy? PolicySnapshot = null,
    IReadOnlyList<string>? Timeline = null,
    string ResultSummary = "",
    string AuditLogPath = "",
    DateTimeOffset CreatedAtUtc = default,
    DateTimeOffset UpdatedAtUtc = default);

public sealed record ReviewedExample(
    Guid Id,
    ReviewedExampleKind Kind,
    LearningFeedbackLabel Label,
    string SourceTaskCardId = "",
    string RawInputSummary = "",
    string CleanedExample = "",
    string ReviewerFeedback = "",
    DateTimeOffset ReviewedAtUtc = default);

public sealed record LearningFeedback(
    Guid Id,
    Guid? TaskCardId,
    string PetNameSnapshot,
    LearningFeedbackLabel Label,
    string Feedback,
    bool ApprovedForDataset = false,
    DateTimeOffset CreatedAtUtc = default);

public sealed record TaskAdapterRequest(
    Guid TaskCardId,
    TaskIntent Intent,
    ToolPolicy PolicySnapshot,
    TaskAdapterRunMode RunMode = TaskAdapterRunMode.DryRunPreview,
    string ArtifactRoot = "",
    DateTimeOffset RequestedAtUtc = default);

public sealed record TaskAdapterResult(
    Guid TaskCardId,
    string ToolFamily,
    TaskAdapterResultStatus Status,
    bool DidMutate,
    IReadOnlyList<string>? ReadPaths = null,
    IReadOnlyList<string>? WrittenPaths = null,
    string PreviewSummary = "",
    string ResultSummary = "",
    string AuditLogPath = "",
    string BlockReason = "",
    DateTimeOffset CompletedAtUtc = default);

public sealed record CaptureRegion(
    int X,
    int Y,
    int Width,
    int Height);

public sealed record CaptureRequest(
    Guid Id,
    CapturePreset Preset,
    CaptureTargetKind TargetKind,
    CaptureOutputKind OutputKind = CaptureOutputKind.ScreenshotPng,
    CapturePrivacyLevel PrivacyLevel = CapturePrivacyLevel.WevitoOnly,
    Guid? TaskCardId = null,
    CaptureRegion? Region = null,
    bool IncludeCursor = false,
    bool IncludeOverlayMetadata = true,
    bool IsRecording = false,
    bool IsExternalShareRequested = false,
    DateTimeOffset CreatedAtUtc = default);

public sealed record CapturePolicyDecision(
    CapturePreset Preset,
    CaptureTargetKind TargetKind,
    ToolPolicyDecisionStatus Status,
    ToolRiskLevel RiskLevel,
    ApprovalRequirement ApprovalRequirement,
    string Reason);

public sealed record CaptureManifest(
    string SchemaVersion,
    Guid RequestId,
    CapturePreset Preset,
    CaptureTargetKind TargetKind,
    CaptureOutputKind OutputKind,
    CapturePrivacyLevel PrivacyLevel,
    string ArtifactRoot,
    string OutputPath,
    string ManifestPath,
    string SummaryPath,
    Guid? TaskCardId = null,
    CaptureRegion? Region = null,
    bool IncludeCursor = false,
    bool IncludeOverlayMetadata = true,
    bool DidUploadOrShare = false,
    DateTimeOffset CapturedAtUtc = default);

public sealed record CaptureResult(
    Guid RequestId,
    TaskAdapterResultStatus Status,
    bool DidCapture,
    bool DidMutate,
    CaptureManifest? Manifest = null,
    IReadOnlyList<string>? WrittenPaths = null,
    IReadOnlyList<string>? Warnings = null,
    string Summary = "",
    DateTimeOffset CompletedAtUtc = default);

public sealed record SpriteAuditFrameFinding(
    string Path,
    string RelativePath,
    string Sha256,
    int Width,
    int Height,
    long ByteLength,
    IReadOnlyList<string>? IssueKinds = null);

public sealed record SpriteAuditReport(
    string SchemaVersion,
    Guid TaskCardId,
    string ToolFamily,
    string TargetRoot,
    IReadOnlyList<string> TargetPaths,
    int PngCount,
    int FindingCount,
    IReadOnlyList<SpriteAuditFrameFinding> Findings,
    bool DidMutate,
    DateTimeOffset GeneratedAtUtc);

public sealed record AssetInventoryRootSummary(
    string RootPath,
    string RootName,
    int DirectoryCount,
    int FileCount,
    int PngCount,
    int ImportSidecarCount,
    int JsonCount,
    long TotalBytes);

public sealed record AssetInventoryFinding(
    string Kind,
    string Path,
    string Detail);

public sealed record AssetInventoryReport(
    string SchemaVersion,
    Guid TaskCardId,
    string ToolFamily,
    IReadOnlyList<AssetInventoryRootSummary> Roots,
    int RuntimeVariantFolderCount,
    int RuntimePngCount,
    int SharedPngCount,
    int FindingCount,
    IReadOnlyList<AssetInventoryFinding> Findings,
    bool DidMutate,
    DateTimeOffset GeneratedAtUtc);

public sealed record CodeReviewFileSummary(
    string Path,
    string RelativePath,
    string Language,
    int LineCount,
    long ByteLength);

public sealed record CodeReviewFinding(
    string Kind,
    string Path,
    int Line,
    string Detail);

public sealed record CodeReviewReport(
    string SchemaVersion,
    Guid TaskCardId,
    string ToolFamily,
    IReadOnlyList<string> TargetPaths,
    int FilesScanned,
    int FindingCount,
    IReadOnlyList<CodeReviewFileSummary> Files,
    IReadOnlyList<CodeReviewFinding> Findings,
    IReadOnlyList<string> SuggestedTests,
    bool DidMutate,
    DateTimeOffset GeneratedAtUtc);

public sealed record CodePatchPlanFileCandidate(
    string Path,
    string RelativePath,
    string Language,
    int LineCount,
    long ByteLength);

public sealed record CodePatchPlanStep(
    int Order,
    string Title,
    string Detail,
    ToolRiskLevel RiskLevel);

public sealed record CodePatchPlanReport(
    string SchemaVersion,
    Guid TaskCardId,
    string ToolFamily,
    IReadOnlyList<string> TargetPaths,
    IReadOnlyList<CodePatchPlanFileCandidate> CandidateFiles,
    string RequestedChange,
    string ProposedScope,
    IReadOnlyList<CodePatchPlanStep> Steps,
    IReadOnlyList<string> ValidationPlan,
    IReadOnlyList<string> RollbackPlan,
    IReadOnlyList<string> SafetyGates,
    bool DidMutate,
    DateTimeOffset GeneratedAtUtc);

public sealed record BuildProofCommandPlan(
    int Order,
    string Command,
    string Purpose,
    ToolRiskLevel RiskLevel,
    bool RequiresApproval,
    bool MustSkipAssetPrep);

public sealed record BuildProofPlanReport(
    string SchemaVersion,
    Guid TaskCardId,
    string ToolFamily,
    IReadOnlyList<BuildProofCommandPlan> Commands,
    IReadOnlyList<string> ProofArtifactsExpected,
    IReadOnlyList<string> SafetyGates,
    IReadOnlyList<string> StopConditions,
    bool DidRunCommands,
    bool DidMutate,
    DateTimeOffset GeneratedAtUtc);

public sealed record TranslationProviderStatus(
    TranslationProviderKind Provider,
    TranslationProviderAvailability Availability,
    bool SupportsGlossary,
    bool SupportsSelfHosted,
    string Detail,
    bool IsDefault = false,
    bool IsUserSelected = false,
    string ConsentSummary = "");

public sealed record TranslationGlossaryEntry(
    string Source,
    string Target,
    bool CaseSensitive,
    string Notes);

public sealed record TranslationPreviewReport(
    string SchemaVersion,
    Guid TaskCardId,
    string ToolFamily,
    string RequestedText,
    string SourceLanguage,
    string TargetLanguage,
    string PreferredProvider,
    int CharacterCount,
    IReadOnlyList<TranslationProviderStatus> Providers,
    IReadOnlyList<TranslationGlossaryEntry> ApplicableGlossaryEntries,
    IReadOnlyList<string> SafetyNotes,
    bool DidCallProvider,
    bool DidMutate,
    DateTimeOffset GeneratedAtUtc);

public sealed record TranslationExecutionReport(
    string SchemaVersion,
    Guid TaskCardId,
    string ToolFamily,
    TranslationProviderKind Provider,
    string RequestedText,
    string TranslatedText,
    string SourceLanguage,
    string TargetLanguage,
    string DetectedSourceLanguage,
    int CharacterCount,
    int? BilledCharacters,
    string GlossaryMode,
    IReadOnlyList<TranslationGlossaryEntry> AppliedGlossaryEntries,
    IReadOnlyList<string> QaWarnings,
    IReadOnlyList<string> SafetyNotes,
    bool DidCallProvider,
    bool DidMutate,
    DateTimeOffset GeneratedAtUtc);

public sealed record AudioAssistCapability(
    AudioAssistActionKind ActionKind,
    AudioAssistCapabilityStatus Status,
    ToolRiskLevel RiskLevel,
    ApprovalRequirement ApprovalRequirement,
    string Detail);

public sealed record AudioEndpointStatus(
    string Source,
    bool IsAvailable,
    double? MasterVolumePercent,
    bool? IsMuted,
    string EndpointId,
    string Detail,
    DateTimeOffset InspectedAtUtc);

public sealed record AudioAssistStatusReport(
    string SchemaVersion,
    Guid TaskCardId,
    string ToolFamily,
    IReadOnlyList<AudioAssistCapability> Capabilities,
    string CurrentStatusSummary,
    IReadOnlyList<string> SafetyNotes,
    bool DidInspectSystemAudio,
    bool DidChangeAudio,
    bool DidMutate,
    DateTimeOffset GeneratedAtUtc,
    AudioEndpointStatus? EndpointStatus = null);

public sealed record AudioAssistExecutionReport(
    string SchemaVersion,
    Guid TaskCardId,
    string ToolFamily,
    AudioAssistActionKind ActionKind,
    double? RequestedVolumePercent,
    AudioEndpointStatus? BeforeStatus,
    AudioEndpointStatus? AfterStatus,
    IReadOnlyList<string> SafetyNotes,
    bool DidChangeAudio,
    bool DidMutateFiles,
    DateTimeOffset GeneratedAtUtc);

public sealed record AudioBoostToolStatus(
    string ToolName,
    AudioBoostDetectionStatus Status,
    IReadOnlyList<string> Evidence,
    string OfficialUrl,
    string Detail);

public sealed record AudioBoostHandoffReport(
    string SchemaVersion,
    Guid TaskCardId,
    string ToolFamily,
    IReadOnlyList<AudioBoostToolStatus> Tools,
    IReadOnlyList<string> SafetyNotes,
    bool DidInstallOrConfigure,
    bool DidMutate,
    DateTimeOffset GeneratedAtUtc);

public sealed record ScreenCaptureCapability(
    ScreenCaptureActionKind ActionKind,
    ScreenCaptureCapabilityStatus Status,
    ToolRiskLevel RiskLevel,
    ApprovalRequirement ApprovalRequirement,
    string Detail);

public sealed record ScreenCapturePreviewReport(
    string SchemaVersion,
    Guid TaskCardId,
    string ToolFamily,
    IReadOnlyList<ScreenCaptureCapability> Capabilities,
    string RequestedCaptureSummary,
    IReadOnlyList<string> SafetyNotes,
    bool DidCaptureScreen,
    bool DidRecordScreen,
    bool DidMutate,
    DateTimeOffset GeneratedAtUtc);

public sealed record LocalDocPreviewEntry(
    string Path,
    string RelativePath,
    string Sha256,
    string Extension,
    long ByteLength,
    string PreviewLine);

public sealed record LocalDocsPreviewReport(
    string SchemaVersion,
    Guid TaskCardId,
    string ToolFamily,
    IReadOnlyList<string> ApprovedRoots,
    IReadOnlyList<string> TargetPaths,
    int DocumentCount,
    IReadOnlyList<LocalDocPreviewEntry> Documents,
    bool DidMutate,
    DateTimeOffset GeneratedAtUtc);
