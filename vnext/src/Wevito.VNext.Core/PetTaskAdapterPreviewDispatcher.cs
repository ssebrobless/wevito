using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class PetTaskAdapterPreviewDispatcher
{
    private const string LocalDocsToolFamily = "localDocs";
    private const string SpriteAuditToolFamily = "spriteAudit";
    private const string PetStateToolFamily = "petState";
    private const string AssetInventoryToolFamily = "assetInventory";
    private const string CodeReviewToolFamily = "codeReview";
    private const string CodePatchPlanToolFamily = "codePatchPlan";
    private const string BuildProofToolFamily = "buildProof";
    private const string TranslateTextToolFamily = "translateText";
    private const string AudioAssistToolFamily = "audioAssist";
    private const string ScreenCaptureToolFamily = "screenCapture";

    private readonly LocalDocsPreviewAdapter _localDocsPreviewAdapter;
    private readonly SpriteAuditPreviewAdapter _spriteAuditPreviewAdapter;
    private readonly PetStatePreviewAdapter _petStatePreviewAdapter;
    private readonly AssetInventoryPreviewAdapter _assetInventoryPreviewAdapter;
    private readonly CodeReviewPreviewAdapter _codeReviewPreviewAdapter;
    private readonly CodePatchPlanPreviewAdapter _codePatchPlanPreviewAdapter;
    private readonly BuildProofPreviewAdapter _buildProofPreviewAdapter;
    private readonly TranslationPreviewAdapter _translationPreviewAdapter;
    private readonly AudioAssistPreviewAdapter _audioAssistPreviewAdapter;
    private readonly ScreenCapturePreviewAdapter _screenCapturePreviewAdapter;

    public PetTaskAdapterPreviewDispatcher(
        LocalDocsPreviewAdapter? localDocsPreviewAdapter = null,
        SpriteAuditPreviewAdapter? spriteAuditPreviewAdapter = null,
        PetStatePreviewAdapter? petStatePreviewAdapter = null,
        AssetInventoryPreviewAdapter? assetInventoryPreviewAdapter = null,
        CodeReviewPreviewAdapter? codeReviewPreviewAdapter = null,
        CodePatchPlanPreviewAdapter? codePatchPlanPreviewAdapter = null,
        BuildProofPreviewAdapter? buildProofPreviewAdapter = null,
        TranslationPreviewAdapter? translationPreviewAdapter = null,
        AudioAssistPreviewAdapter? audioAssistPreviewAdapter = null,
        ScreenCapturePreviewAdapter? screenCapturePreviewAdapter = null)
    {
        _localDocsPreviewAdapter = localDocsPreviewAdapter ?? new LocalDocsPreviewAdapter();
        _spriteAuditPreviewAdapter = spriteAuditPreviewAdapter ?? new SpriteAuditPreviewAdapter();
        _petStatePreviewAdapter = petStatePreviewAdapter ?? new PetStatePreviewAdapter();
        _assetInventoryPreviewAdapter = assetInventoryPreviewAdapter ?? new AssetInventoryPreviewAdapter();
        _codeReviewPreviewAdapter = codeReviewPreviewAdapter ?? new CodeReviewPreviewAdapter();
        _codePatchPlanPreviewAdapter = codePatchPlanPreviewAdapter ?? new CodePatchPlanPreviewAdapter();
        _buildProofPreviewAdapter = buildProofPreviewAdapter ?? new BuildProofPreviewAdapter();
        _translationPreviewAdapter = translationPreviewAdapter ?? new TranslationPreviewAdapter();
        _audioAssistPreviewAdapter = audioAssistPreviewAdapter ?? new AudioAssistPreviewAdapter();
        _screenCapturePreviewAdapter = screenCapturePreviewAdapter ?? new ScreenCapturePreviewAdapter();
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

        return policyFamily switch
        {
            var family when string.Equals(family, LocalDocsToolFamily, StringComparison.OrdinalIgnoreCase) =>
                _localDocsPreviewAdapter.BuildPreview(request, timestamp),
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
                _audioAssistPreviewAdapter.BuildStatusReport(request, timestamp),
            var family when string.Equals(family, ScreenCaptureToolFamily, StringComparison.OrdinalIgnoreCase) =>
                _screenCapturePreviewAdapter.BuildPreview(request, timestamp),
            _ => Block(request, ResolveResultFamily(policyFamily, intentFamily), $"No PET TASKS dry-run preview adapter is registered for tool family '{policyFamily}'.", timestamp)
        };
    }

    private static string ResolveResultFamily(string policyFamily, string intentFamily)
    {
        return !string.IsNullOrWhiteSpace(policyFamily)
            ? policyFamily
            : intentFamily;
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
