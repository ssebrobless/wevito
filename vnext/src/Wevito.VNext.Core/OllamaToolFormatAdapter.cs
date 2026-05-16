using System.Text.Json;

namespace Wevito.VNext.Core;

public sealed record OllamaToolCall(string Name, string ArgumentsJson, string Id = "");

public static class OllamaToolFormatAdapter
{
    public static IReadOnlyList<object> ToOpenAiTools(IEnumerable<ToolDescriptor> descriptors)
    {
        return descriptors.Select(descriptor => new
        {
            type = "function",
            function = new
            {
                name = descriptor.Name,
                description = descriptor.Description,
                parameters = JsonSerializer.Deserialize<JsonElement>(descriptor.ArgumentSchema)
            }
        }).Cast<object>().ToList();
    }

    public static IReadOnlyList<OllamaToolCall> ParseToolCalls(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        if (!document.RootElement.TryGetProperty("choices", out var choices) ||
            choices.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var calls = new List<OllamaToolCall>();
        foreach (var choice in choices.EnumerateArray())
        {
            if (!choice.TryGetProperty("message", out var message) ||
                !message.TryGetProperty("tool_calls", out var toolCalls) ||
                toolCalls.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var call in toolCalls.EnumerateArray())
            {
                var id = call.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? "" : "";
                if (!call.TryGetProperty("function", out var function))
                {
                    continue;
                }

                var name = function.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? "" : "";
                var arguments = function.TryGetProperty("arguments", out var argsElement) ? argsElement.GetString() ?? "{}" : "{}";
                if (!string.IsNullOrWhiteSpace(name))
                {
                    calls.Add(new OllamaToolCall(name, string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments, id));
                }
            }
        }

        return calls;
    }
}

