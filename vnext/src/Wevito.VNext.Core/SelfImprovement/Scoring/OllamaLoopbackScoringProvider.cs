using System.Globalization;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core.SelfImprovement.Scoring;

public sealed record OllamaLoopbackScoringProvider(
    IScoringHttpClient Http,
    KillSwitchService? KillSwitch,
    Func<IReadOnlyDictionary<string, string>> Settings) : ILocalScoringProvider
{
    public const string OllamaEnabledSetting = "local_scoring_provider_ollama_enabled";
    public const string LoopbackEndpointSetting = "local_scoring_provider_loopback_endpoint";
    public const string OllamaModelSetting = "local_scoring_provider_ollama_model";
    public const string DefaultEndpoint = "127.0.0.1:11434";
    public const string DefaultModel = "qwen2.5:7b-instruct-q4_k_m";

    public override LocalScoringResult Score(LocalScoringRequest request, CancellationToken cancellationToken)
    {
        if (KillSwitch?.IsActive() == true)
        {
            return new LocalScoringResult.Refused("kill_switch=true");
        }

        var settings = Settings();
        if (!IsTrue(settings, NotConfiguredScoringProvider.EnabledSetting))
        {
            return new LocalScoringResult.Refused("local_scoring_provider_enabled=false");
        }

        if (!IsTrue(settings, OllamaEnabledSetting))
        {
            return new LocalScoringResult.Refused("local_scoring_provider_ollama_enabled=false");
        }

        var endpoint = settings.TryGetValue(LoopbackEndpointSetting, out var configuredEndpoint) && !string.IsNullOrWhiteSpace(configuredEndpoint)
            ? configuredEndpoint
            : DefaultEndpoint;
        if (!endpoint.StartsWith("127.0.0.1:", StringComparison.Ordinal))
        {
            return new LocalScoringResult.Refused("non_loopback_endpoint");
        }

        var model = settings.TryGetValue(OllamaModelSetting, out var configuredModel) && !string.IsNullOrWhiteSpace(configuredModel)
            ? configuredModel
            : DefaultModel;
        var uri = new Uri($"http://{endpoint}/api/generate");
        var body = JsonSerializer.Serialize(new
        {
            model,
            prompt = request.PromptSha256,
            stream = false,
            options = new
            {
                temperature = 0
            }
        }, JsonDefaults.Options);

        try
        {
            var responseJson = Http.PostAsync(uri, body, cancellationToken).GetAwaiter().GetResult();
            using var document = JsonDocument.Parse(responseJson);
            var response = document.RootElement.TryGetProperty("response", out var responseElement)
                ? responseElement.GetString()
                : null;
            var responseModel = document.RootElement.TryGetProperty("model", out var modelElement)
                ? modelElement.GetString()
                : model;

            return double.TryParse(response, NumberStyles.Float, CultureInfo.InvariantCulture, out var score)
                ? new LocalScoringResult.Scored(score, request.Rubric, responseModel ?? model)
                : new LocalScoringResult.Refused("invalid_score_response");
        }
        catch (InvalidOperationException exception) when (string.Equals(exception.Message, "non_loopback_endpoint", StringComparison.Ordinal))
        {
            return new LocalScoringResult.Refused("non_loopback_endpoint");
        }
    }

    private static bool IsTrue(IReadOnlyDictionary<string, string> settings, string key)
    {
        return settings.TryGetValue(key, out var value) &&
               string.Equals(value, bool.TrueString, StringComparison.OrdinalIgnoreCase);
    }
}
