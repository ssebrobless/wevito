using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public enum HelperPermissionMode
{
    Plan,
    Preview,
    Execute
}

public sealed record HelperToolCapability(
    PetHelperRole HelperRole,
    string HelperName,
    string ToolFamily,
    bool ReadsUntrustedExternal,
    bool ReadsPrivateData,
    bool SendsNetwork);

public sealed record HelperAllowlistDecision(
    bool IsAllowed,
    string Reason,
    HelperToolCapability? Capability = null);

public sealed class HelperAllowlistEvaluator
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
    private readonly IReadOnlyList<HelperToolCapability> _capabilities;
    private readonly IReadOnlySet<string> _globalDeny;
    private readonly IReadOnlyDictionary<string, IReadOnlySet<string>> _helperDeny;
    private readonly UnifiedPolicyService _unifiedPolicyService;

    public HelperAllowlistEvaluator(
        IReadOnlyList<HelperToolCapability>? capabilities = null,
        IReadOnlySet<string>? globalDeny = null,
        IReadOnlyDictionary<string, IReadOnlySet<string>>? helperDeny = null,
        UnifiedPolicyService? unifiedPolicyService = null)
    {
        _capabilities = capabilities ?? BuildDefaultCapabilityMatrix();
        _globalDeny = globalDeny ?? new HashSet<string>(Comparer);
        _helperDeny = helperDeny ?? new Dictionary<string, IReadOnlySet<string>>(Comparer);
        _unifiedPolicyService = unifiedPolicyService ?? new UnifiedPolicyService();
    }

    public HelperAllowlistDecision Evaluate(PetHelperProfile helper, string toolFamily, HelperPermissionMode mode = HelperPermissionMode.Preview)
    {
        if (mode == HelperPermissionMode.Execute)
        {
            return new HelperAllowlistDecision(false, "Model summaries are read-only in the pilot and cannot run in execute mode.");
        }

        if (_globalDeny.Contains(toolFamily))
        {
            return new HelperAllowlistDecision(false, $"Tool family '{toolFamily}' is globally denied for model summaries.");
        }

        if (_helperDeny.TryGetValue(helper.PetNameSnapshot, out var denied) && denied.Contains(toolFamily))
        {
            return new HelperAllowlistDecision(false, $"Tool family '{toolFamily}' is denied for helper '{helper.PetNameSnapshot}'.");
        }

        if (helper.AllowedToolFamilies is { Count: > 0 } &&
            !helper.AllowedToolFamilies.Contains(toolFamily, Comparer))
        {
            return new HelperAllowlistDecision(false, $"Helper '{helper.PetNameSnapshot}' does not allow tool family '{toolFamily}'.");
        }

        var capability = _capabilities.FirstOrDefault(candidate =>
            string.Equals(candidate.ToolFamily, toolFamily, StringComparison.OrdinalIgnoreCase) &&
            (candidate.HelperRole == helper.Role ||
             string.Equals(candidate.HelperName, helper.PetNameSnapshot, StringComparison.OrdinalIgnoreCase)));

        if (capability is null)
        {
            return new HelperAllowlistDecision(false, $"No read-only model capability is declared for {helper.Role} and tool family '{toolFamily}'.");
        }

        if (HasLethalTrifecta(capability))
        {
            return new HelperAllowlistDecision(false, $"Capability '{capability.HelperName}/{capability.ToolFamily}' combines untrusted input, private data, and network send.");
        }

        return _unifiedPolicyService.EvaluateHelperCapability(
            new HelperAllowlistDecision(true, "Allowed by read-only pet model capability.", capability));
    }

    public IReadOnlyList<HelperToolCapability> FindLethalTrifectaViolations()
    {
        return _capabilities.Where(HasLethalTrifecta).ToList();
    }

    public static IReadOnlyList<HelperToolCapability> BuildDefaultCapabilityMatrix()
    {
        return
        [
            new HelperToolCapability(PetHelperRole.ResearchHelper, "Scout", "localDocs", ReadsUntrustedExternal: true, ReadsPrivateData: false, SendsNetwork: true),
            new HelperToolCapability(PetHelperRole.ResearchHelper, "Scout", "codeReview", ReadsUntrustedExternal: true, ReadsPrivateData: false, SendsNetwork: true),
            new HelperToolCapability(PetHelperRole.SpriteReviewHelper, "Inspector", "spriteAudit", ReadsUntrustedExternal: false, ReadsPrivateData: true, SendsNetwork: true),
            new HelperToolCapability(PetHelperRole.SpriteReviewHelper, "Inspector", "petState", ReadsUntrustedExternal: false, ReadsPrivateData: true, SendsNetwork: true),
            new HelperToolCapability(PetHelperRole.ChecklistHelper, "Builder", "codePatchPlan", ReadsUntrustedExternal: false, ReadsPrivateData: true, SendsNetwork: true),
            new HelperToolCapability(PetHelperRole.ChecklistHelper, "Builder", "codeReview", ReadsUntrustedExternal: false, ReadsPrivateData: true, SendsNetwork: true)
        ];
    }

    private static bool HasLethalTrifecta(HelperToolCapability capability)
    {
        return capability.ReadsUntrustedExternal && capability.ReadsPrivateData && capability.SendsNetwork;
    }
}
