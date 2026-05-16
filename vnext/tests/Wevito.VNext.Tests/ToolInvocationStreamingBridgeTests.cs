using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using CoreToolDescriptor = Wevito.VNext.Core.ToolDescriptor;

namespace Wevito.VNext.Tests;

public sealed class ToolInvocationStreamingBridgeTests
{
    [Fact]
    public async Task PausesAndResumesStream()
    {
        var bridge = new ToolInvocationStreamingBridge(new ToolInvocationService(new ToolRegistry([Descriptor("localDocs")])));

        var events = await CollectAsync(bridge.BridgeAsync(Source([new ChatStreamEvent(ChatStreamEventKind.ToolCallStart, ToolName: "localDocs", ToolCallJson: "{}")])));

        Assert.Equal(ChatStreamEventKind.ToolCallStart, events[0].Kind);
        Assert.Equal(ChatStreamEventKind.ToolCallResult, events[1].Kind);
        Assert.Equal(ChatStreamEventKind.ToolCallEnd, events[2].Kind);
    }

    [Fact]
    public async Task HandlesToolFailure()
    {
        var failing = new CoreToolDescriptor(
            "localDocs",
            "localDocs",
            "desc",
            """{"type":"object"}""",
            """{"type":"object"}""",
            ToolRiskLevel.Low,
            RequiresApproval: false,
            (_, _) => throw new InvalidOperationException("boom"));
        var bridge = new ToolInvocationStreamingBridge(new ToolInvocationService(new ToolRegistry([failing])));

        var events = await CollectAsync(bridge.BridgeAsync(Source([new ChatStreamEvent(ChatStreamEventKind.ToolCallStart, ToolName: "localDocs", ToolCallJson: "{}")])));

        Assert.Contains(events, item => item.Kind == ChatStreamEventKind.ToolCallResult && item.Content.Contains("failed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RespectsCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var bridge = new ToolInvocationStreamingBridge(new ToolInvocationService(new ToolRegistry([Descriptor("localDocs")])));

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in bridge.BridgeAsync(Source([new ChatStreamEvent(ChatStreamEventKind.ToolCallStart, ToolName: "localDocs", ToolCallJson: "{}")]), cts.Token))
            {
            }
        });
    }

    private static CoreToolDescriptor Descriptor(string family)
    {
        return new CoreToolDescriptor(
            family,
            family,
            "desc",
            """{"type":"object"}""",
            """{"type":"object"}""",
            ToolRiskLevel.Low,
            RequiresApproval: false,
            (request, _) => Task.FromResult(new TaskAdapterResult(request.TaskCardId, family, TaskAdapterResultStatus.PreviewReady, DidMutate: false, PreviewSummary: "tool ok")));
    }

    private static async IAsyncEnumerable<ChatStreamEvent> Source(IEnumerable<ChatStreamEvent> events)
    {
        foreach (var item in events)
        {
            yield return item;
            await Task.Yield();
        }
    }

    private static async Task<List<ChatStreamEvent>> CollectAsync(IAsyncEnumerable<ChatStreamEvent> events)
    {
        var result = new List<ChatStreamEvent>();
        await foreach (var item in events)
        {
            result.Add(item);
        }

        return result;
    }
}



