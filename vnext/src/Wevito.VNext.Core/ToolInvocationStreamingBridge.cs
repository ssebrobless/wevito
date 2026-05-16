using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Wevito.VNext.Core;

public sealed class ToolInvocationStreamingBridge
{
    private readonly ToolInvocationService _invocationService;

    public ToolInvocationStreamingBridge(ToolInvocationService invocationService)
    {
        _invocationService = invocationService;
    }

    public async IAsyncEnumerable<ChatStreamEvent> BridgeAsync(
        IAsyncEnumerable<ChatStreamEvent> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item.Kind != ChatStreamEventKind.ToolCallStart || string.IsNullOrWhiteSpace(item.ToolName))
            {
                yield return item;
                continue;
            }

            yield return item;
            ToolInvocationResult result;
            try
            {
                using var args = JsonDocument.Parse(string.IsNullOrWhiteSpace(item.ToolCallJson) ? "{}" : item.ToolCallJson);
                result = await _invocationService.InvokeAsync(item.ToolName, args.RootElement, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result = new ToolInvocationResult(item.ToolName, Wevito.VNext.Contracts.TaskAdapterResultStatus.Failed, "{}", $"Tool failed safely: {ex.GetType().Name}.", Error: ex.Message);
            }

            var resultId = string.IsNullOrWhiteSpace(item.ToolResultId) ? Guid.NewGuid().ToString("N") : item.ToolResultId;
            yield return new ChatStreamEvent(ChatStreamEventKind.ToolCallResult, result.Summary, item.ToolName, item.ToolCallJson, resultId);
            yield return new ChatStreamEvent(ChatStreamEventKind.ToolCallEnd, ToolName: item.ToolName, ToolCallJson: item.ToolCallJson, ToolResultId: resultId);
        }
    }
}

