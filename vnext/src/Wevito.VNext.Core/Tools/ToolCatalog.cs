using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core.Tools;

public static class ToolCatalog
{
    public const string AdvancedToolsVisibleSetting = "tool_hub_advanced_visible";
    public const string LayoutAnnouncementSetting = "tool_hub_layout_v1_announced";
    public const string LayoutChangedPacketKind = "tool_hub_layout_changed";

    public static IReadOnlyList<ToolTabDescriptor> TopLevelTabs { get; } =
    [
        new("pets", "Pets", "actions", "ToolTabPetsButton", false, "Care, feed, play, and inspect the selected pets."),
        new("tasks", "Tasks", "helpers", "ToolTabTasksButton", false, "Ask Wevito helpers for report-only task cards."),
        new("tools", "Tools", "basket", "ToolTabToolsButton", false, "Manage saved links and local tool shortcuts."),
        new("local-ai", "Local AI", "settings", "ToolTabLocalAiButton", false, "Review local brain status, settings, and safety gates."),
        new("autonomy", "Autonomy", "autonomous-scopes", "ToolTabAutonomyButton", false, "Preview and control approved autonomous scopes."),
        new("local-docs", "Local Docs", "local-docs", "ToolTabLocalDocsButton", false, "Search approved local documents when retrieval is enabled."),
        new("activity", "Activity", "activity", "ToolTabActivityButton", true, "Inspect recent audit-ledger activity and helper evidence."),
        new("benchmarks", "Benchmarks", "benchmarks", "ToolTabBenchmarksButton", true, "Curate and run reviewed local benchmark cases."),
        new("creative-lab", "Creative Lab", "creative-lab", "ToolTabCreativeLabButton", true, "Review learning examples without training or promotion.")
    ];

    public static IReadOnlyList<ToolFamilyCatalogEntry> ToolFamilies { get; } =
    [
        Preview("localDocs", "summarize_local_docs", "Summarize approved local Wevito documents.", ToolRiskLevel.Low),
        Preview("localResearch", "local_research_packet", "Prepare an offline/local research evidence packet.", ToolRiskLevel.Low),
        Preview("spriteAudit", "sprite_audit_report", "Audit sprite/runtime assets and write a report-only packet.", ToolRiskLevel.Low),
        Preview("petState", "get_pet_state", "Read current pet state without changing pets.", ToolRiskLevel.Low),
        Preview("assetInventory", "asset_inventory_report", "Inventory local Wevito assets.", ToolRiskLevel.Low),
        Preview("codeReview", "code_review_report", "Review local code and prepare a report.", ToolRiskLevel.Low),
        Preview("codePatchPlan", "code_patch_plan", "Plan a code change without editing files.", ToolRiskLevel.Low),
        Preview("buildProof", "build_proof_plan", "Prepare build proof commands; execution remains approval-gated.", ToolRiskLevel.Medium, requiresApproval: true),
        Preview("translateText", "translate_text", "Translate user-provided text through approved local or deterministic paths.", ToolRiskLevel.Low),
        Preview("audioAssist", "audio_assist_report", "Prepare audio/volume guidance without changing system audio.", ToolRiskLevel.Low),
        Preview("screenCapture", "screen_capture_preview", "Prepare a screen-capture preview; execution remains approval-gated.", ToolRiskLevel.Medium, requiresApproval: true),
        Preview("petMemory", "retrieve_pet_memory", "Retrieve reviewed local pet memory rows.", ToolRiskLevel.Low),
        BuiltIn("retrieve_from_memory", "retrieve_from_memory", "Retrieve matching local memory rows for explicit chat-context deep dives."),
        BuiltIn("bookmark_for_benchmark", "bookmark_for_benchmark", "Bookmark a chat message for a later benchmark review."),
        BuiltIn("pin_message", "pin_message", "Pin a chat message into the local context budget.")
    ];

    public static ToolFamilyCatalogEntry? FindFamily(string toolFamily)
    {
        return ToolFamilies.FirstOrDefault(entry =>
            string.Equals(entry.ToolFamily, toolFamily, StringComparison.OrdinalIgnoreCase));
    }

    public static ToolTabDescriptor? FindTabByToolId(string toolId)
    {
        return TopLevelTabs.FirstOrDefault(entry =>
            string.Equals(entry.ToolId, toolId, StringComparison.OrdinalIgnoreCase));
    }

    private static ToolFamilyCatalogEntry Preview(
        string toolFamily,
        string name,
        string description,
        ToolRiskLevel riskLevel,
        bool requiresApproval = false)
    {
        return new ToolFamilyCatalogEntry(toolFamily, name, description, riskLevel, requiresApproval, IsBuiltIn: false);
    }

    private static ToolFamilyCatalogEntry BuiltIn(string toolFamily, string name, string description)
    {
        return new ToolFamilyCatalogEntry(toolFamily, name, description, ToolRiskLevel.Low, RequiresApproval: false, IsBuiltIn: true);
    }
}

public sealed record ToolTabDescriptor(
    string Id,
    string DisplayName,
    string ToolId,
    string AutomationId,
    bool IsAdvanced,
    string Description);

public sealed record ToolFamilyCatalogEntry(
    string ToolFamily,
    string Name,
    string Description,
    ToolRiskLevel RiskLevel,
    bool RequiresApproval,
    bool IsBuiltIn);
