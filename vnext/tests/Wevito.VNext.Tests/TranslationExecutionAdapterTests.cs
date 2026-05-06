using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class TranslationExecutionAdapterTests
{
    [Fact]
    public async Task ExecuteAsync_CallsDeepLClientAndWritesResultArtifacts()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-143000-translate-execute");
        var handler = new CapturingHandler("""
            {
              "translations": [
                {
                  "detected_source_language": "EN",
                  "text": "Hola ganso"
                }
              ],
              "billed_characters": 11
            }
            """);
        var adapter = new TranslationExecutionAdapter(
            authKey => new DeepLTranslationClient(new HttpClient(handler), authKey, new Uri("https://api-free.deepl.com/v2/translate")),
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["DEEPL_API_KEY"] = "test-key:fx"
            });

        var result = await adapter.ExecuteAsync(BuildRequest(artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.Completed, result.Status);
        Assert.False(result.DidMutate);
        Assert.True(handler.WasCalled);
        Assert.Contains("DeepL-Auth-Key", handler.AuthorizationHeader);
        Assert.Contains("\"target_lang\":\"ES\"", handler.RequestBody);
        Assert.DoesNotContain("test-key", File.ReadAllText(Path.Combine(artifactRoot, "run-summary.md")));
        Assert.Equal("Hola ganso", File.ReadAllText(Path.Combine(artifactRoot, "translated-text.txt")));

        var report = JsonSerializer.Deserialize<TranslationExecutionReport>(
            File.ReadAllText(Path.Combine(artifactRoot, "translation-execution-report.json")),
            JsonDefaults.Options);
        Assert.NotNull(report);
        Assert.True(report.DidCallProvider);
        Assert.False(report.DidMutate);
        Assert.Equal("Hola ganso", report.TranslatedText);
        Assert.Equal("ES", report.TargetLanguage);
        Assert.Equal(11, report.BilledCharacters);
    }

    [Fact]
    public async Task ExecuteAsync_BlocksWithoutCredentials()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-143000-translate-execute");
        var adapter = new TranslationExecutionAdapter(
            environment: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase));

        var result = await adapter.ExecuteAsync(BuildRequest(artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.False(result.DidMutate);
        Assert.Empty(result.WrittenPaths ?? []);
        Assert.Contains("credentials are missing", result.BlockReason);
    }

    [Fact]
    public async Task ExecuteAsync_BlocksPreviewMode()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-143000-translate-execute");
        var adapter = new TranslationExecutionAdapter(
            environment: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["DEEPL_API_KEY"] = "test-key:fx"
            });

        var result = await adapter.ExecuteAsync(BuildRequest(artifactRoot, TaskAdapterRunMode.DryRunPreview));

        Assert.Equal(TaskAdapterResultStatus.Blocked, result.Status);
        Assert.False(result.DidMutate);
        Assert.Contains("execute mode", result.BlockReason);
    }

    private static TaskAdapterRequest BuildRequest(
        string artifactRoot,
        TaskAdapterRunMode runMode = TaskAdapterRunMode.Execute)
    {
        var intent = new TaskIntent(
            Guid.Parse("c4000000-0000-0000-0000-000000000001"),
            "Nix, translate \"Hello goose\" to Spanish",
            TaskIntentTargetMode.ExplicitPetName,
            TargetPetNameSnapshot: "Nix",
            TaskKind: TaskKind.TranslateText,
            RequestedToolFamily: "translateText");
        var policy = new ToolPolicy(
            "translate-text-network-approval",
            "translateText",
            ToolAccessMode.Network,
            ToolRiskLevel.Medium,
            ApprovalRequirement.BeforeExecution);

        return new TaskAdapterRequest(
            Guid.Parse("d4000000-0000-0000-0000-000000000001"),
            intent,
            policy,
            runMode,
            artifactRoot);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-translation-execution-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        public CapturingHandler(string responseBody)
        {
            _responseBody = responseBody;
        }

        public bool WasCalled { get; private set; }

        public string AuthorizationHeader { get; private set; } = "";

        public string RequestBody { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            AuthorizationHeader = request.Headers.Authorization?.ToString() ?? "";
            RequestBody = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
