using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed record BenchmarkDraftCaseRequest(
    string DraftRoot,
    string ApprovedRoot,
    string Axis,
    string Prompt,
    string ExpectedText,
    string ActualText = "",
    string ExpectedToolFamily = "",
    string ActualToolFamily = "",
    IReadOnlyList<string>? ExpectedChunkIds = null,
    IReadOnlyList<string>? RetrievedChunkIds = null,
    IReadOnlyList<string>? RequiredJsonFields = null,
    string JsonPayload = "",
    bool MustBlock = false,
    bool DidTriggerAction = false,
    DateTimeOffset? CreatedAtUtc = null);

public sealed record BenchmarkDraftCaseResult(
    bool Succeeded,
    string DraftPath,
    BenchmarkCase? Case,
    string Message);

public sealed record BenchmarkApprovedCaseResult(
    bool Succeeded,
    string ApprovedPath,
    BenchmarkCase? Case,
    string Message);

public sealed class BenchmarkCaseDraftService
{
    public const string CaseDraftedPacketKind = "benchmark_case_drafted";
    public const string BookmarkedFromChatPacketKind = "benchmark_case_bookmarked_from_chat";

    private readonly AuditLedgerService? _auditLedgerService;
    private readonly KillSwitchService? _killSwitchService;

    public BenchmarkCaseDraftService(AuditLedgerService? auditLedgerService = null, KillSwitchService? killSwitchService = null)
    {
        _auditLedgerService = auditLedgerService;
        _killSwitchService = killSwitchService;
    }

    public BenchmarkDraftCaseResult Draft(BenchmarkDraftCaseRequest request, bool fromChatBookmark = false)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return new BenchmarkDraftCaseResult(false, "", null, "kill_switch=true");
        }

        var axis = NormalizeAxis(request.Axis);
        var draftAxisRoot = Path.Combine(Path.GetFullPath(request.DraftRoot), axis);
        var approvedAxisRoot = Path.Combine(Path.GetFullPath(request.ApprovedRoot), axis);
        Directory.CreateDirectory(draftAxisRoot);
        Directory.CreateDirectory(approvedAxisRoot);
        var createdAt = request.CreatedAtUtc ?? DateTimeOffset.UtcNow;
        var id = BuildStableId(axis, request.Prompt, createdAt);
        var approvedPath = Path.Combine(approvedAxisRoot, $"{id}.json");
        if (File.Exists(approvedPath))
        {
            return new BenchmarkDraftCaseResult(false, "", null, "Approved benchmark case already exists; draft refused to avoid overwrite.");
        }

        var draftPath = Path.Combine(draftAxisRoot, $"{id}.json");
        if (File.Exists(draftPath))
        {
            draftPath = Path.Combine(draftAxisRoot, $"{id}-{Guid.NewGuid():N}.json");
        }

        var testCase = new BenchmarkCase(
            id,
            axis,
            request.Prompt,
            request.ExpectedText,
            request.ActualText,
            request.ExpectedToolFamily,
            request.ActualToolFamily,
            request.ExpectedChunkIds,
            request.RetrievedChunkIds,
            request.RequiredJsonFields,
            request.JsonPayload,
            request.MustBlock,
            request.DidTriggerAction);
        File.WriteAllText(draftPath, JsonSerializer.Serialize(testCase, JsonDefaults.Options));
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            fromChatBookmark ? BookmarkedFromChatPacketKind : CaseDraftedPacketKind,
            null,
            createdAt,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: true,
            draftPath,
            $"Drafted {axis} benchmark case {id}.",
            "Drafted"));
        return new BenchmarkDraftCaseResult(true, draftPath, testCase, "Draft benchmark case created.");
    }

    public BenchmarkDraftCaseResult BookmarkFromChat(string draftRoot, string approvedRoot, string assistantText, DateTimeOffset? nowUtc = null)
    {
        return Draft(new BenchmarkDraftCaseRequest(
            draftRoot,
            approvedRoot,
            "chat",
            Prompt: "bookmark-from-chat",
            ExpectedText: assistantText,
            ActualText: assistantText,
            CreatedAtUtc: nowUtc), fromChatBookmark: true);
    }

    public BenchmarkApprovedCaseResult CreateAdversarialApprovedCase(
        string approvedRoot,
        string hostilePrompt,
        string expectedBehavior,
        DateTimeOffset? nowUtc = null)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return new BenchmarkApprovedCaseResult(false, "", null, "kill_switch=true");
        }

        if (string.IsNullOrWhiteSpace(hostilePrompt))
        {
            return new BenchmarkApprovedCaseResult(false, "", null, "Hostile prompt is required.");
        }

        if (string.IsNullOrWhiteSpace(expectedBehavior))
        {
            return new BenchmarkApprovedCaseResult(false, "", null, "Expected Wevito behavior is required.");
        }

        var axis = "safety";
        var approvedAxisRoot = Path.Combine(Path.GetFullPath(approvedRoot), axis);
        Directory.CreateDirectory(approvedAxisRoot);
        var createdAt = nowUtc ?? DateTimeOffset.UtcNow;
        var id = BuildStableId(axis, hostilePrompt, createdAt);
        var approvedPath = Path.Combine(approvedAxisRoot, $"{id}.json");
        if (File.Exists(approvedPath))
        {
            return new BenchmarkApprovedCaseResult(false, "", null, "Approved benchmark case already exists; refusing to overwrite.");
        }

        var testCase = new BenchmarkCase(
            id,
            axis,
            hostilePrompt.Trim(),
            ExpectedText: expectedBehavior.Trim(),
            MustBlock: true,
            DidTriggerAction: false);
        File.WriteAllText(approvedPath, JsonSerializer.Serialize(testCase, JsonDefaults.Options));
        File.SetAttributes(approvedPath, File.GetAttributes(approvedPath) | FileAttributes.ReadOnly);
        _auditLedgerService?.Record(new EvidencePacket(
            Guid.NewGuid(),
            BenchmarkCaseCurationStore.CaseApprovedPacketKind,
            null,
            createdAt,
            DidUseNetwork: false,
            DidUseHostedAi: false,
            DidUseLocalModel: false,
            DidMutate: true,
            approvedPath,
            $"Approved human-authored adversarial benchmark case {id}.",
            "Approved"));
        return new BenchmarkApprovedCaseResult(true, approvedPath, testCase, "Approved adversarial benchmark case created.");
    }

    private static string NormalizeAxis(string axis)
    {
        return string.IsNullOrWhiteSpace(axis) ? "chat" : axis.Trim().ToLowerInvariant();
    }

    private static string BuildStableId(string axis, string prompt, DateTimeOffset createdAt)
    {
        var input = $"{axis}|{prompt}|{createdAt:O}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant()[..10];
        return $"{axis}-{createdAt:yyyyMMddHHmmss}-{hash}";
    }
}
