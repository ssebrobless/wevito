using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class ToolRegistry
{
    public const string DisabledToolsSetting = "tool_registry_disabled_tools";
    public const string ToolEnabledSettingPrefix = "tool_registry_tool_";

    private readonly List<ToolDescriptor> _descriptors;
    private readonly Func<IReadOnlyDictionary<string, string>> _settingsProvider;
    private readonly AuditLedgerService? _auditLedgerService;

    public ToolRegistry(
        IEnumerable<ToolDescriptor> descriptors,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null,
        AuditLedgerService? auditLedgerService = null,
        DateTimeOffset? nowUtc = null)
    {
        _settingsProvider = settingsProvider ?? (() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        _auditLedgerService = auditLedgerService;
        _descriptors = descriptors
            .GroupBy(descriptor => descriptor.ToolFamily, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(descriptor => descriptor.ToolFamily, StringComparer.OrdinalIgnoreCase)
            .ToList();
        Record("tool_registry_loaded", $"Loaded {_descriptors.Count} AI-callable tool descriptors.", nowUtc ?? DateTimeOffset.UtcNow);
    }

    public IReadOnlyList<ToolDescriptor> Descriptors => _descriptors;

    public IReadOnlyList<ToolDescriptor> LlmVisibleDescriptors =>
        _descriptors.Where(descriptor => !IsDisabled(descriptor.ToolFamily)).ToList();

    public ToolDescriptor? Find(string toolFamily)
    {
        return _descriptors.FirstOrDefault(descriptor =>
            string.Equals(descriptor.ToolFamily, toolFamily, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(descriptor.Name, toolFamily, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<ToolPolicy> BuildPolicies()
    {
        return _descriptors
            .Select(descriptor => new ToolPolicy(
                $"tool-registry:{descriptor.ToolFamily}",
                descriptor.ToolFamily,
                ToolAccessMode.ReadOnly,
                descriptor.RiskLevel,
                descriptor.RequiresApproval ? ApprovalRequirement.BeforeExecution : ApprovalRequirement.None,
                IsEnabled: !IsDisabled(descriptor.ToolFamily),
                BlockReason: IsDisabled(descriptor.ToolFamily) ? "Tool is disabled in the tool registry settings." : ""))
            .ToList();
    }

    public void SetToolEnabled(string toolFamily, bool enabled, DateTimeOffset? nowUtc = null)
    {
        var settings = new Dictionary<string, string>(_settingsProvider(), StringComparer.OrdinalIgnoreCase);
        var disabled = ParseDisabled(settings).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (enabled)
        {
            disabled.Remove(toolFamily);
        }
        else
        {
            disabled.Add(toolFamily);
        }

        settings[DisabledToolsSetting] = string.Join(",", disabled.OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
        Record("tool_registry_setting", $"{toolFamily} enabled={enabled}", nowUtc ?? DateTimeOffset.UtcNow);
    }

    public bool IsDisabled(string toolFamily)
    {
        var settings = _settingsProvider();
        if (settings.TryGetValue(BuildToolEnabledSettingKey(toolFamily), out var raw) &&
            bool.TryParse(raw, out var enabled))
        {
            return !enabled;
        }

        return ParseDisabled(settings).Contains(toolFamily, StringComparer.OrdinalIgnoreCase);
    }

    public static string BuildToolEnabledSettingKey(string toolFamily)
    {
        return $"{ToolEnabledSettingPrefix}{toolFamily}_enabled";
    }

    public static ToolRegistry CreateDefault(
        AgentToolDispatcher? dispatcher = null,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null,
        AuditLedgerService? auditLedgerService = null,
        string? toolDefinitionsPath = null,
        DateTimeOffset? nowUtc = null)
    {
        dispatcher ??= new AgentToolDispatcher(auditLedgerService: auditLedgerService);
        var declaredFamilies = LoadDeclaredToolFamilies(toolDefinitionsPath);
        var candidates = BuildDefaultDescriptors(dispatcher);
        var undeclared = declaredFamilies.Count == 0
            ? new List<string>()
            : candidates
            .Where(descriptor => !declaredFamilies.Contains(descriptor.ToolFamily))
            .Select(descriptor => descriptor.ToolFamily)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (undeclared.Count > 0)
        {
            throw new InvalidOperationException($"Tool registry descriptors must be declared in tool_definitions.json first: {string.Join(", ", undeclared)}");
        }

        return new ToolRegistry(candidates, settingsProvider, auditLedgerService, nowUtc);
    }

    public static IReadOnlyList<ToolDescriptor> BuildDefaultDescriptors(AgentToolDispatcher dispatcher)
    {
        return
        [
            Preview("localDocs", "summarize_local_docs", "Summarize approved local Wevito documents.", ToolRiskLevel.Low, false, dispatcher),
            Preview("localResearch", "local_research_packet", "Prepare an offline/local research evidence packet.", ToolRiskLevel.Low, false, dispatcher),
            Preview("spriteAudit", "sprite_audit_report", "Audit sprite/runtime assets and write a report-only packet.", ToolRiskLevel.Low, false, dispatcher),
            Preview("petState", "get_pet_state", "Read current pet state without changing pets.", ToolRiskLevel.Low, false, dispatcher),
            Preview("assetInventory", "asset_inventory_report", "Inventory local Wevito assets.", ToolRiskLevel.Low, false, dispatcher),
            Preview("codeReview", "code_review_report", "Review local code and prepare a report.", ToolRiskLevel.Low, false, dispatcher),
            Preview("codePatchPlan", "code_patch_plan", "Plan a code change without editing files.", ToolRiskLevel.Low, false, dispatcher),
            Preview("buildProof", "build_proof_plan", "Prepare build proof commands; execution remains approval-gated.", ToolRiskLevel.Medium, true, dispatcher),
            Preview("translateText", "translate_text", "Translate user-provided text locally or through approved deterministic paths.", ToolRiskLevel.Low, false, dispatcher),
            Preview("audioAssist", "audio_assist_report", "Prepare audio/volume guidance without changing system audio.", ToolRiskLevel.Low, false, dispatcher),
            Preview("screenCapture", "screen_capture_preview", "Prepare a screen-capture preview; capture execution remains approval-gated.", ToolRiskLevel.Medium, true, dispatcher),
            Preview("petMemory", "retrieve_pet_memory", "Retrieve reviewed local pet memory rows.", ToolRiskLevel.Low, false, dispatcher),
            BuiltIn("retrieve_from_memory", "retrieve_from_memory", "Retrieve matching local memory rows for explicit chat-context deep dives."),
            BuiltIn("bookmark_for_benchmark", "bookmark_for_benchmark", "Bookmark a chat message for a later benchmark review."),
            BuiltIn("pin_message", "pin_message", "Pin a chat message into the local context budget.")
        ];
    }

    private static ToolDescriptor Preview(
        string toolFamily,
        string name,
        string description,
        ToolRiskLevel risk,
        bool requiresApproval,
        AgentToolDispatcher dispatcher)
    {
        return new ToolDescriptor(
            toolFamily,
            name,
            description,
            DefaultArgumentSchema,
            DefaultReturnSchema,
            risk,
            requiresApproval,
            (request, _) => Task.FromResult(dispatcher.BuildPreview(request)));
    }

    private static ToolDescriptor BuiltIn(string toolFamily, string name, string description)
    {
        return new ToolDescriptor(
            toolFamily,
            name,
            description,
            DefaultArgumentSchema,
            DefaultReturnSchema,
            ToolRiskLevel.Low,
            RequiresApproval: false,
            (request, _) => Task.FromResult(new TaskAdapterResult(
                request.TaskCardId,
                request.PolicySnapshot.ToolFamily,
                TaskAdapterResultStatus.PreviewReady,
                DidMutate: false,
                PreviewSummary: $"{description} This phase registers the tool surface only; durable storage lands in its owning phase.",
                CompletedAtUtc: DateTimeOffset.UtcNow)));
    }

    private static IReadOnlySet<string> LoadDeclaredToolFamilies(string? toolDefinitionsPath)
    {
        var path = string.IsNullOrWhiteSpace(toolDefinitionsPath)
            ? Path.Combine(AppContext.BaseDirectory, "vnext", "content", "tool_definitions.json")
            : toolDefinitionsPath;
        if (!File.Exists(path))
        {
            path = Path.Combine(Environment.CurrentDirectory, "vnext", "content", "tool_definitions.json");
        }

        if (!File.Exists(path))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var families = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return families;
        }

        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.TryGetProperty("toolFamily", out var family) && family.ValueKind == JsonValueKind.String)
            {
                families.Add(family.GetString() ?? "");
            }
            else if (item.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
            {
                families.Add(id.GetString() ?? "");
            }
        }

        return families;
    }

    private static IReadOnlyList<string> ParseDisabled(IReadOnlyDictionary<string, string> settings)
    {
        return settings.TryGetValue(DisabledToolsSetting, out var raw)
            ? raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];
    }

    private void Record(string kind, string summary, DateTimeOffset nowUtc)
    {
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            kind,
            null,
            nowUtc,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: false,
            ArtifactPath: "",
            Summary: summary,
            Status: "Completed"));
    }

    private const string DefaultArgumentSchema = """
        {"type":"object","properties":{"task_text":{"type":"string"},"query":{"type":"string"},"target":{"type":"string"}},"additionalProperties":true}
        """;

    private const string DefaultReturnSchema = """
        {"type":"object","properties":{"status":{"type":"string"},"summary":{"type":"string"},"artifact_path":{"type":"string"}},"required":["status","summary"],"additionalProperties":true}
        """;
}
