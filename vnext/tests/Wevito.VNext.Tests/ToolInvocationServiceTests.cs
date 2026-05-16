using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using CoreToolDescriptor = Wevito.VNext.Core.ToolDescriptor;

namespace Wevito.VNext.Tests;

public sealed class ToolInvocationServiceTests
{
    [Fact]
    public async Task RoutesToCorrectAdapter()
    {
        var called = false;
        var service = new ToolInvocationService(new ToolRegistry([Descriptor("localDocs", request =>
        {
            called = true;
            return new TaskAdapterResult(request.TaskCardId, "localDocs", TaskAdapterResultStatus.PreviewReady, DidMutate: false, PreviewSummary: "docs ready");
        })]));

        using var args = JsonDocument.Parse("""{"task_text":"summarize"}""");
        var result = await service.InvokeAsync("localDocs", args.RootElement);

        Assert.True(called);
        Assert.True(result.IsSuccess);
        Assert.Contains("docs ready", result.Summary);
    }

    [Fact]
    public async Task ConsultsUnifiedPolicyServiceFirst()
    {
        var order = new List<string>();
        var service = new ToolInvocationService(
            new ToolRegistry([Descriptor("localDocs", _ =>
            {
                order.Add("adapter");
                return new TaskAdapterResult(Guid.NewGuid(), "localDocs", TaskAdapterResultStatus.PreviewReady, DidMutate: false, PreviewSummary: "ok");
            })]),
            policyEvaluator: (intent, policies) =>
            {
                order.Add("policy");
                return new UnifiedPolicyService().EvaluateToolPolicy(intent, policies);
            });

        using var args = JsonDocument.Parse("{}");
        await service.InvokeAsync("localDocs", args.RootElement);

        Assert.Equal(["policy", "adapter"], order);
    }

    [Fact]
    public async Task RefusesWhenKillSwitchActive()
    {
        var killSwitch = new KillSwitchService(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [KillSwitchService.KillSwitchSetting] = bool.TrueString
        });
        var service = new ToolInvocationService(new ToolRegistry([Descriptor("localDocs")]), killSwitchService: killSwitch);

        using var args = JsonDocument.Parse("{}");
        var result = await service.InvokeAsync("localDocs", args.RootElement);

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.Contains("kill_switch", result.Error);
    }

    [Fact]
    public async Task WritesEvidencePacket()
    {
        var db = Path.Combine(Path.GetTempPath(), "wevito-tool-invocation-tests", Guid.NewGuid().ToString("N"), "ledger.sqlite");
        var ledger = new AuditLedgerService(db);
        var service = new ToolInvocationService(new ToolRegistry([Descriptor("localDocs")], auditLedgerService: ledger), auditLedgerService: ledger);

        using var args = JsonDocument.Parse("{}");
        await service.InvokeAsync("localDocs", args.RootElement);

        var rows = ledger.Snapshot(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(5));
        Assert.Contains(rows, row => row.PacketKind == ToolInvocationService.ToolInvocationStartedPacketKind);
        Assert.Contains(rows, row => row.PacketKind == ToolInvocationService.ToolInvocationCompletedPacketKind);
    }

    private static CoreToolDescriptor Descriptor(string family, Func<TaskAdapterRequest, TaskAdapterResult>? adapter = null)
    {
        return new CoreToolDescriptor(
            family,
            family,
            $"{family} descriptor",
            """{"type":"object"}""",
            """{"type":"object"}""",
            ToolRiskLevel.Low,
            RequiresApproval: false,
            (request, _) => Task.FromResult(adapter?.Invoke(request) ?? new TaskAdapterResult(request.TaskCardId, family, TaskAdapterResultStatus.PreviewReady, DidMutate: false, PreviewSummary: "ok")));
    }
}



