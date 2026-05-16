using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;
using CoreToolDescriptor = Wevito.VNext.Core.ToolDescriptor;

namespace Wevito.VNext.Tests;

public sealed class OllamaToolFormatAdapterTests
{
    [Fact]
    public void GeneratesValidOpenAiFunctionsSchema()
    {
        var tools = OllamaToolFormatAdapter.ToOpenAiTools([Descriptor("localDocs", "summarize_local_docs")]);
        var json = JsonSerializer.Serialize(tools, JsonDefaults.Options);

        using var document = JsonDocument.Parse(json);
        Assert.Equal("function", document.RootElement[0].GetProperty("type").GetString());
        Assert.Equal("summarize_local_docs", document.RootElement[0].GetProperty("function").GetProperty("name").GetString());
        Assert.Equal("object", document.RootElement[0].GetProperty("function").GetProperty("parameters").GetProperty("type").GetString());
    }

    [Fact]
    public void ParsesToolCallResponse()
    {
        var calls = OllamaToolFormatAdapter.ParseToolCalls("""
            {"choices":[{"message":{"tool_calls":[{"id":"call_1","type":"function","function":{"name":"summarize_local_docs","arguments":"{\"query\":\"sprites\"}"}}]}}]}
            """);

        var call = Assert.Single(calls);
        Assert.Equal("call_1", call.Id);
        Assert.Equal("summarize_local_docs", call.Name);
        Assert.Contains("sprites", call.ArgumentsJson);
    }

    private static CoreToolDescriptor Descriptor(string family, string name)
    {
        return new CoreToolDescriptor(
            family,
            name,
            "desc",
            """{"type":"object","properties":{"query":{"type":"string"}}}""",
            """{"type":"object"}""",
            ToolRiskLevel.Low,
            RequiresApproval: false,
            (request, _) => Task.FromResult(new TaskAdapterResult(request.TaskCardId, family, TaskAdapterResultStatus.PreviewReady, DidMutate: false)));
    }
}



