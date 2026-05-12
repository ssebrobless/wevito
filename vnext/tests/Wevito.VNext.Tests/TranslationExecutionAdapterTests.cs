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
                  "text": "Hola __WEVITO_GLOSSARY_000__"
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
        Assert.Contains("__WEVITO_GLOSSARY_000__", handler.RequestBody);
        var markdown = File.ReadAllText(Path.Combine(artifactRoot, "run-summary.md"));
        Assert.DoesNotContain("test-key", markdown);
        Assert.Contains("DeepL API via `DEEPL_API_KEY` or `DEEPL_AUTH_KEY`", markdown);
        Assert.Contains("no hidden provider fallback", markdown);
        Assert.Equal("Hola goose", File.ReadAllText(Path.Combine(artifactRoot, "translated-text.txt")));

        var report = JsonSerializer.Deserialize<TranslationExecutionReport>(
            File.ReadAllText(Path.Combine(artifactRoot, "translation-execution-report.json")),
            JsonDefaults.Options);
        Assert.NotNull(report);
        Assert.True(report.DidCallProvider);
        Assert.False(report.DidMutate);
        Assert.Equal("Hola goose", report.TranslatedText);
        Assert.Equal("ES", report.TargetLanguage);
        Assert.Equal(11, report.BilledCharacters);
        Assert.Equal("protected-token-shim", report.GlossaryMode);
        Assert.Contains(report.AppliedGlossaryEntries, entry => entry.Source == "goose");
        Assert.Contains(report.QaWarnings, warning => warning.StartsWith("fallback_used:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_PreservesCodeBlocksAndInlineCodeDuringGlossaryShim()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-143000-translate-execute");
        var handler = new CapturingHandler("""
            {
              "translations": [
                {
                  "detected_source_language": "EN",
                  "text": "Hola __WEVITO_GLOSSARY_000__ with `goose`\n```\ngoose\n```"
                }
              ],
              "billed_characters": 33
            }
            """);
        var adapter = new TranslationExecutionAdapter(
            authKey => new DeepLTranslationClient(new HttpClient(handler), authKey, new Uri("https://api-free.deepl.com/v2/translate")),
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["DEEPL_API_KEY"] = "test-key:fx"
            });

        var result = await adapter.ExecuteAsync(BuildRequest(
            artifactRoot,
            rawText: "Nix, translate \"Hello goose with `goose`\n```\ngoose\n```\" to Spanish"));

        Assert.Equal(TaskAdapterResultStatus.Completed, result.Status);
        Assert.Contains("__WEVITO_GLOSSARY_000__", handler.RequestBody);
        Assert.Contains("\\u0060goose\\u0060", handler.RequestBody);
        Assert.Contains("\\u0060\\u0060\\u0060\\ngoose\\n\\u0060\\u0060\\u0060", handler.RequestBody);
        Assert.Equal("Hola goose with `goose`\n```\ngoose\n```", File.ReadAllText(Path.Combine(artifactRoot, "translated-text.txt")));
    }

    [Fact]
    public async Task ExecuteAsync_EmitsQaWarningsForPlaceholderAndMarkdownDrift()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-143000-translate-execute");
        var handler = new CapturingHandler("""
            {
              "translations": [
                {
                  "detected_source_language": "EN",
                  "text": "Hola goose name"
                }
              ],
              "billed_characters": 22
            }
            """);
        var adapter = new TranslationExecutionAdapter(
            authKey => new DeepLTranslationClient(new HttpClient(handler), authKey, new Uri("https://api-free.deepl.com/v2/translate")),
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["DEEPL_API_KEY"] = "test-key:fx"
            });

        await adapter.ExecuteAsync(BuildRequest(
            artifactRoot,
            rawText: "Nix, translate \"# Hello goose **{{name}}**\" to Spanish"));

        var report = JsonSerializer.Deserialize<TranslationExecutionReport>(
            File.ReadAllText(Path.Combine(artifactRoot, "translation-execution-report.json")),
            JsonDefaults.Options);
        Assert.NotNull(report);
        Assert.Contains(report.QaWarnings, warning => warning.StartsWith("placeholder_drift:", StringComparison.Ordinal));
        Assert.Contains(report.QaWarnings, warning => warning.StartsWith("markdown_drift:", StringComparison.Ordinal));
        Assert.Contains(report.QaWarnings, warning => warning.StartsWith("fallback_used:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_ProKeyCreatesNativeDeepLGlossaryBeforeTranslate()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-143000-translate-execute");
        var handler = new SequencedHandler(
            """
            {
              "glossary_id": "glossary-123",
              "name": "Wevito EN-ES",
              "dictionaries": [
                {
                  "source_lang": "en",
                  "target_lang": "es",
                  "entry_count": 1
                }
              ]
            }
            """,
            """
            {
              "translations": [
                {
                  "detected_source_language": "EN",
                  "text": "Hola goose"
                }
              ],
              "billed_characters": 11
            }
            """);
        var adapter = new TranslationExecutionAdapter(
            authKey => new DeepLTranslationClient(new HttpClient(handler), authKey, new Uri("https://api.deepl.com/v2/translate")),
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["DEEPL_API_KEY"] = "pro-test-key"
            });

        var result = await adapter.ExecuteAsync(BuildRequest(artifactRoot));

        Assert.Equal(TaskAdapterResultStatus.Completed, result.Status);
        Assert.Equal(2, handler.RequestBodies.Count);
        Assert.Contains("/v3/glossaries", handler.RequestUris[0]);
        Assert.Contains("\"entries\":\"goose\\tgoose", handler.RequestBodies[0]);
        Assert.Contains("\"glossary_id\":\"glossary-123\"", handler.RequestBodies[1]);
        Assert.DoesNotContain("__WEVITO_GLOSSARY_", handler.RequestBodies[1]);

        var report = JsonSerializer.Deserialize<TranslationExecutionReport>(
            File.ReadAllText(Path.Combine(artifactRoot, "translation-execution-report.json")),
            JsonDefaults.Options);
        Assert.NotNull(report);
        Assert.Equal("deepl-native-v3", report.GlossaryMode);
        Assert.DoesNotContain(report.QaWarnings, warning => warning.StartsWith("fallback_used:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_FallsBackToShimWhenNativeGlossaryCreateFails()
    {
        var tempRoot = CreateTempRoot();
        var artifactRoot = Path.Combine(tempRoot, "vnext", "artifacts", "pet-tasks", "20260505-143000-translate-execute");
        var handler = new SequencedHandler(
            "bad glossary",
            """
            {
              "translations": [
                {
                  "detected_source_language": "EN",
                  "text": "Hola __WEVITO_GLOSSARY_000__"
                }
              ],
              "billed_characters": 11
            }
            """,
            firstStatusCode: HttpStatusCode.BadRequest);
        var adapter = new TranslationExecutionAdapter(
            authKey => new DeepLTranslationClient(new HttpClient(handler), authKey, new Uri("https://api.deepl.com/v2/translate")),
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["DEEPL_API_KEY"] = "pro-test-key"
            });

        await adapter.ExecuteAsync(BuildRequest(artifactRoot));

        var report = JsonSerializer.Deserialize<TranslationExecutionReport>(
            File.ReadAllText(Path.Combine(artifactRoot, "translation-execution-report.json")),
            JsonDefaults.Options);
        Assert.NotNull(report);
        Assert.Equal("protected-token-shim-after-native-glossary-failure", report.GlossaryMode);
        Assert.Contains(report.QaWarnings, warning => warning.StartsWith("fallback_used:", StringComparison.Ordinal));
        Assert.Equal("Hola goose", report.TranslatedText);
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
        TaskAdapterRunMode runMode = TaskAdapterRunMode.Execute,
        string rawText = "Nix, translate \"Hello goose\" to Spanish")
    {
        var intent = new TaskIntent(
            Guid.Parse("c4000000-0000-0000-0000-000000000001"),
            rawText,
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

    private sealed class SequencedHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode StatusCode, string Body)> _responses;

        public SequencedHandler(
            string firstBody,
            string secondBody,
            HttpStatusCode firstStatusCode = HttpStatusCode.Created)
        {
            _responses = new Queue<(HttpStatusCode StatusCode, string Body)>();
            _responses.Enqueue((firstStatusCode, firstBody));
            _responses.Enqueue((HttpStatusCode.OK, secondBody));
        }

        public List<string> RequestBodies { get; } = [];

        public List<string> RequestUris { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri?.ToString() ?? "");
            RequestBodies.Add(request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken));
            var response = _responses.Dequeue();
            return new HttpResponseMessage(response.StatusCode)
            {
                Content = new StringContent(response.Body, Encoding.UTF8, "application/json")
            };
        }
    }
}
