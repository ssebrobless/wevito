using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class PetTaskAdapterPreviewDispatcher
{
    private const string LocalDocsToolFamily = "localDocs";
    private const string LocalResearchToolFamily = "localResearch";
    private const string SpriteAuditToolFamily = "spriteAudit";
    private const string PetStateToolFamily = "petState";
    private const string AssetInventoryToolFamily = "assetInventory";
    private const string CodeReviewToolFamily = "codeReview";
    private const string CodePatchPlanToolFamily = "codePatchPlan";
    private const string BuildProofToolFamily = "buildProof";
    private const string TranslateTextToolFamily = "translateText";
    private const string AudioAssistToolFamily = "audioAssist";
    private const string ScreenCaptureToolFamily = "screenCapture";
    private const string PetMemoryToolFamily = "petMemory";

    private readonly LocalDocsPreviewAdapter _localDocsPreviewAdapter;
    private readonly LocalResearchPreviewAdapter _localResearchPreviewAdapter;
    private readonly SpriteAuditPreviewAdapter _spriteAuditPreviewAdapter;
    private readonly PetStatePreviewAdapter _petStatePreviewAdapter;
    private readonly AssetInventoryPreviewAdapter _assetInventoryPreviewAdapter;
    private readonly CodeReviewPreviewAdapter _codeReviewPreviewAdapter;
    private readonly CodePatchPlanPreviewAdapter _codePatchPlanPreviewAdapter;
    private readonly BuildProofPreviewAdapter _buildProofPreviewAdapter;
    private readonly TranslationPreviewAdapter _translationPreviewAdapter;
    private readonly AudioAssistPreviewAdapter _audioAssistPreviewAdapter;
    private readonly AudioBoostHandoffAdapter _audioBoostHandoffAdapter;
    private readonly ScreenCapturePreviewAdapter _screenCapturePreviewAdapter;
    private readonly PetMemoryPreviewAdapter _petMemoryPreviewAdapter;
    private readonly AuditLedgerService? _auditLedgerService;

    public PetTaskAdapterPreviewDispatcher(
        LocalDocsPreviewAdapter? localDocsPreviewAdapter = null,
        LocalResearchPreviewAdapter? localResearchPreviewAdapter = null,
        SpriteAuditPreviewAdapter? spriteAuditPreviewAdapter = null,
        PetStatePreviewAdapter? petStatePreviewAdapter = null,
        AssetInventoryPreviewAdapter? assetInventoryPreviewAdapter = null,
        CodeReviewPreviewAdapter? codeReviewPreviewAdapter = null,
        CodePatchPlanPreviewAdapter? codePatchPlanPreviewAdapter = null,
        BuildProofPreviewAdapter? buildProofPreviewAdapter = null,
        TranslationPreviewAdapter? translationPreviewAdapter = null,
        AudioAssistPreviewAdapter? audioAssistPreviewAdapter = null,
        AudioBoostHandoffAdapter? audioBoostHandoffAdapter = null,
        ScreenCapturePreviewAdapter? screenCapturePreviewAdapter = null,
        PetMemoryPreviewAdapter? petMemoryPreviewAdapter = null,
        AuditLedgerService? auditLedgerService = null)
    {
        _localDocsPreviewAdapter = localDocsPreviewAdapter ?? new LocalDocsPreviewAdapter();
        _localResearchPreviewAdapter = localResearchPreviewAdapter ?? new LocalResearchPreviewAdapter();
        _spriteAuditPreviewAdapter = spriteAuditPreviewAdapter ?? new SpriteAuditPreviewAdapter();
        _petStatePreviewAdapter = petStatePreviewAdapter ?? new PetStatePreviewAdapter();
        _assetInventoryPreviewAdapter = assetInventoryPreviewAdapter ?? new AssetInventoryPreviewAdapter();
        _codeReviewPreviewAdapter = codeReviewPreviewAdapter ?? new CodeReviewPreviewAdapter();
        _codePatchPlanPreviewAdapter = codePatchPlanPreviewAdapter ?? new CodePatchPlanPreviewAdapter();
        _buildProofPreviewAdapter = buildProofPreviewAdapter ?? new BuildProofPreviewAdapter();
        _translationPreviewAdapter = translationPreviewAdapter ?? new TranslationPreviewAdapter();
        _audioAssistPreviewAdapter = audioAssistPreviewAdapter ?? new AudioAssistPreviewAdapter();
        _audioBoostHandoffAdapter = audioBoostHandoffAdapter ?? new AudioBoostHandoffAdapter();
        _screenCapturePreviewAdapter = screenCapturePreviewAdapter ?? new ScreenCapturePreviewAdapter();
        _petMemoryPreviewAdapter = petMemoryPreviewAdapter ?? new PetMemoryPreviewAdapter();
        _auditLedgerService = auditLedgerService;
    }

    public TaskAdapterResult BuildPreview(
        TaskAdapterRequest request,
        DateTimeOffset? nowUtc = null,
        GameContent? content = null,
        IReadOnlyList<PetActor>? activePets = null,
        CompanionMode mode = CompanionMode.Focused)
    {
        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var policyFamily = request.PolicySnapshot.ToolFamily;
        var intentFamily = request.Intent.RequestedToolFamily;

        if (!string.Equals(policyFamily, intentFamily, StringComparison.OrdinalIgnoreCase))
        {
            return Block(request, ResolveResultFamily(policyFamily, intentFamily), "Task intent and policy must target the same tool family.", timestamp);
        }

        var result = policyFamily switch
        {
            var family when string.Equals(family, LocalDocsToolFamily, StringComparison.OrdinalIgnoreCase) =>
                _localDocsPreviewAdapter.BuildPreview(request, timestamp),
            var family when string.Equals(family, LocalResearchToolFamily, StringComparison.OrdinalIgnoreCase) =>
                _localResearchPreviewAdapter.BuildPreview(request, timestamp),
            var family when string.Equals(family, SpriteAuditToolFamily, StringComparison.OrdinalIgnoreCase) =>
                _spriteAuditPreviewAdapter.BuildReport(request, timestamp),
            var family when string.Equals(family, PetStateToolFamily, StringComparison.OrdinalIgnoreCase) =>
                _petStatePreviewAdapter.BuildReport(request, content, activePets, mode, timestamp),
            var family when string.Equals(family, AssetInventoryToolFamily, StringComparison.OrdinalIgnoreCase) =>
                _assetInventoryPreviewAdapter.BuildReport(request, timestamp),
            var family when string.Equals(family, CodeReviewToolFamily, StringComparison.OrdinalIgnoreCase) =>
                _codeReviewPreviewAdapter.BuildReport(request, timestamp),
            var family when string.Equals(family, CodePatchPlanToolFamily, StringComparison.OrdinalIgnoreCase) =>
                _codePatchPlanPreviewAdapter.BuildPlan(request, timestamp),
            var family when string.Equals(family, BuildProofToolFamily, StringComparison.OrdinalIgnoreCase) =>
                _buildProofPreviewAdapter.BuildPlan(request, timestamp),
            var family when string.Equals(family, TranslateTextToolFamily, StringComparison.OrdinalIgnoreCase) =>
                _translationPreviewAdapter.BuildPreview(request, timestamp),
            var family when string.Equals(family, AudioAssistToolFamily, StringComparison.OrdinalIgnoreCase) =>
                IsBoostHandoffRequest(request.Intent.RawText)
                    ? _audioBoostHandoffAdapter.BuildSetupGuide(request, timestamp)
                    : _audioAssistPreviewAdapter.BuildStatusReport(request, timestamp),
            var family when string.Equals(family, ScreenCaptureToolFamily, StringComparison.OrdinalIgnoreCase) =>
                _screenCapturePreviewAdapter.BuildPreview(request, timestamp),
            var family when string.Equals(family, PetMemoryToolFamily, StringComparison.OrdinalIgnoreCase) =>
                _petMemoryPreviewAdapter.BuildPreview(request, timestamp),
            _ => Block(request, ResolveResultFamily(policyFamily, intentFamily), $"No PET TASKS dry-run preview adapter is registered for tool family '{policyFamily}'.", timestamp)
        };
        RecordAdapterResult(result, timestamp);
        return result;
    }

    private void RecordAdapterResult(TaskAdapterResult result, DateTimeOffset timestamp)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            result.ToolFamily,
            result.TaskCardId,
            timestamp,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: string.Equals(result.ToolFamily, PetMemoryToolFamily, StringComparison.OrdinalIgnoreCase),
            result.DidMutate,
            result.AuditLogPath,
            string.IsNullOrWhiteSpace(result.ResultSummary) ? result.PreviewSummary : result.ResultSummary,
            result.Status.ToString(),
            result.BlockReason));
    }

    private static string ResolveResultFamily(string policyFamily, string intentFamily)
    {
        return !string.IsNullOrWhiteSpace(policyFamily)
            ? policyFamily
            : intentFamily;
    }

    private static bool IsBoostHandoffRequest(string rawText)
    {
        return rawText.Contains("boost", StringComparison.OrdinalIgnoreCase) ||
               rawText.Contains("equalizer", StringComparison.OrdinalIgnoreCase) ||
               rawText.Contains("fxsound", StringComparison.OrdinalIgnoreCase) ||
               rawText.Contains("apo", StringComparison.OrdinalIgnoreCase) ||
               rawText.Contains("louder than", StringComparison.OrdinalIgnoreCase) ||
               rawText.Contains("over 100", StringComparison.OrdinalIgnoreCase);
    }

    private static TaskAdapterResult Block(TaskAdapterRequest request, string toolFamily, string reason, DateTimeOffset timestamp)
    {
        return new TaskAdapterResult(
            request.TaskCardId,
            toolFamily,
            TaskAdapterResultStatus.Blocked,
            DidMutate: false,
            ReadPaths: [],
            WrittenPaths: [],
            BlockReason: reason,
            CompletedAtUtc: timestamp);
    }
}
