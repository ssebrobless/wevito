using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record ToolResultBudgetFormatResult(
    string Truncated,
    string FullPath,
    int TotalTokens,
    int TruncatedTokens);

public sealed class ToolResultBudgetService
{
    public const string ToolResultTruncatedPacketKind = "tool_result_truncated";
    public const int DefaultTokenBudget = 4_000;

    private readonly string _artifactRoot;
    private readonly int _tokenBudget;
    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public ToolResultBudgetService(string? artifactRoot = null, int tokenBudget = DefaultTokenBudget, AuditLedgerService? auditLedgerService = null, KillSwitchService? killSwitchService = null)
    {
        _artifactRoot = Path.GetFullPath(artifactRoot ?? Path.Combine("vnext", "artifacts", "tool-results"));
        _tokenBudget = Math.Max(1, tokenBudget);
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public ToolResultBudgetFormatResult FormatToolResult(string toolFamily, string rawResult, DateTimeOffset? nowUtc = null)
    {
        var tokens = Tokenize(rawResult);
        if (tokens.Length <= _tokenBudget)
        {
            return new ToolResultBudgetFormatResult(rawResult ?? "", "", tokens.Length, tokens.Length);
        }

        if (_killSwitchService?.IsActive() == true)
        {
            return new ToolResultBudgetFormatResult("[truncated; full result not written because kill_switch=true]", "", tokens.Length, 0);
        }

        var timestamp = nowUtc ?? DateTimeOffset.UtcNow;
        var safeFamily = string.Join("-", (toolFamily ?? "tool").Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var folder = Path.Combine(_artifactRoot, $"{timestamp:yyyyMMdd-HHmmss}-{safeFamily}");
        Directory.CreateDirectory(folder);
        var fullPath = Path.Combine(folder, "result.json");
        File.WriteAllText(fullPath, JsonSerializer.Serialize(new
        {
            schemaVersion = "1",
            toolFamily,
            totalTokens = tokens.Length,
            result = rawResult ?? ""
        }, JsonDefaults.Options));

        var clipped = string.Join(" ", tokens.Take(_tokenBudget));
        var marker = $"[truncated; full result at {Path.GetRelativePath(Environment.CurrentDirectory, fullPath)}]";
        var formatted = $"{clipped}{Environment.NewLine}{marker}";
        _auditLedgerService?.Record(new EvidencePacket(Guid.NewGuid(), ToolResultTruncatedPacketKind, null, timestamp, false, false, false, true, folder, $"Truncated {toolFamily} tool result from {tokens.Length} to {_tokenBudget} tokens.", "Completed"));
        return new ToolResultBudgetFormatResult(formatted, fullPath, tokens.Length, _tokenBudget);
    }

    private static string[] Tokenize(string? value)
    {
        return (value ?? "").Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries);
    }
}
