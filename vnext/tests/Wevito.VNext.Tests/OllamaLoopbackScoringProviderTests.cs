using System.Reflection;
using System.Text.Json;
using Wevito.VNext.Core;
using Wevito.VNext.Core.Audit;
using Wevito.VNext.Core.SelfImprovement.Scoring;

namespace Wevito.VNext.Tests;

public sealed class OllamaLoopbackScoringProviderTests
{
    [Fact]
    public void Score_BothFlagsOff_RefusesProviderDisabled()
    {
        var provider = Provider(new Dictionary<string, string>(), out var http);

        var result = provider.Score(Request(), CancellationToken.None);

        var refused = Assert.IsType<LocalScoringResult.Refused>(result);
        Assert.Equal("local_scoring_provider_enabled=false", refused.Reason);
        Assert.Empty(http.Calls);
    }

    [Fact]
    public void Score_OllamaFlagOff_RefusesOllamaDisabled()
    {
        var provider = Provider(new Dictionary<string, string>
        {
            [NotConfiguredScoringProvider.EnabledSetting] = bool.TrueString
        }, out var http);

        var result = provider.Score(Request(), CancellationToken.None);

        var refused = Assert.IsType<LocalScoringResult.Refused>(result);
        Assert.Equal("local_scoring_provider_ollama_enabled=false", refused.Reason);
        Assert.Empty(http.Calls);
    }

    [Fact]
    public void Score_KillSwitchActive_RefusesBeforeHttp()
    {
        var settings = EnabledSettings();
        settings[KillSwitchService.KillSwitchSetting] = bool.TrueString;
        var http = new FakeScoringHttpClient("{\"response\":\"0.8\",\"model\":\"qwen2.5\"}");
        var provider = new OllamaLoopbackScoringProvider(http, new KillSwitchService(() => settings), () => settings);

        var result = provider.Score(Request(), CancellationToken.None);

        var refused = Assert.IsType<LocalScoringResult.Refused>(result);
        Assert.Equal("kill_switch=true", refused.Reason);
        Assert.Empty(http.Calls);
    }

    [Fact]
    public void Score_BothFlagsOn_FakeHttpScore_ReturnsScored()
    {
        var settings = EnabledSettings();
        settings[OllamaLoopbackScoringProvider.OllamaModelSetting] = "qwen2.5";
        var provider = Provider(settings, out var http);

        var result = provider.Score(Request(), CancellationToken.None);

        var scored = Assert.IsType<LocalScoringResult.Scored>(result);
        Assert.Equal(0.8, scored.Score);
        Assert.Equal("proposal rubric", scored.Rubric);
        Assert.Equal("qwen2.5", scored.ModelIdentity);
        var call = Assert.Single(http.Calls);
        Assert.Equal("127.0.0.1", call.Uri.Host);
        Assert.Equal("/api/generate", call.Uri.AbsolutePath);
    }

    [Fact]
    public void Score_NonLoopbackHost_RefusesBeforeHttp()
    {
        var settings = EnabledSettings();
        settings[OllamaLoopbackScoringProvider.LoopbackEndpointSetting] = "localhost:11434";
        var provider = Provider(settings, out var http);

        var result = provider.Score(Request(), CancellationToken.None);

        var refused = Assert.IsType<LocalScoringResult.Refused>(result);
        Assert.Equal("non_loopback_endpoint", refused.Reason);
        Assert.Empty(http.Calls);
    }

    [Fact]
    public void Score_PostBodyContainsPromptSha256Only()
    {
        var provider = Provider(EnabledSettings(), out var http);

        provider.Score(Request(), CancellationToken.None);

        var call = Assert.Single(http.Calls);
        using var document = JsonDocument.Parse(call.Body);
        Assert.Equal(Sha(), document.RootElement.GetProperty("prompt").GetString());
        Assert.DoesNotContain("raw prompt text", call.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"Prompt\"", call.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Rubric\"", call.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void CapabilityFlags_DefaultsAreSafe()
    {
        var ollama = Assert.Single(CapabilityFlagInventory.Entries, entry => entry.Name == OllamaLoopbackScoringProvider.OllamaEnabledSetting);
        var endpoint = Assert.Single(CapabilityFlagInventory.Entries, entry => entry.Name == OllamaLoopbackScoringProvider.LoopbackEndpointSetting);
        var model = Assert.Single(CapabilityFlagInventory.Entries, entry => entry.Name == OllamaLoopbackScoringProvider.OllamaModelSetting);

        Assert.Equal(bool.FalseString, ollama.DefaultValue);
        Assert.Equal("", endpoint.DefaultValue);
        Assert.Contains("127.0.0.1:11434", endpoint.PlainLanguage, StringComparison.Ordinal);
        Assert.Equal("", model.DefaultValue);
        Assert.Contains("qwen2.5:7b-instruct-q4_k_m", model.PlainLanguage, StringComparison.Ordinal);
    }

    [Fact]
    public void TestsNeverConstructDefaultScoringHttpClient()
    {
        var testSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "vnext", "tests", "Wevito.VNext.Tests", "OllamaLoopbackScoringProviderTests.cs"));
        var constructionText = "new " + nameof(DefaultScoringHttpClient);
        var networkNamespaceText = "System.Net." + "Http";
        var socketsText = "Sock" + "ets";

        Assert.DoesNotContain(constructionText, testSource, StringComparison.Ordinal);
        Assert.DoesNotContain(networkNamespaceText, testSource, StringComparison.Ordinal);
        Assert.DoesNotContain(socketsText, testSource, StringComparison.Ordinal);
    }

    [Fact]
    public void NoProducerWiresOllamaLoopbackScoringProvider()
    {
        var root = Path.Combine(FindRepositoryRoot(), "vnext", "src", "Wevito.VNext.Core");
        var offenders = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}SelfImprovement{Path.DirectorySeparatorChar}Scoring{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.EndsWith(Path.Combine("Audit", "CapabilityFlagInventory.cs"), StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("OllamaLoopbackScoringProvider", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(FindRepositoryRoot(), path))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void NoNewSelfImprovementPacketKindWasAdded()
    {
        var packetKindNames = typeof(SelfImprovementPacketKinds).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => field.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.DoesNotContain(packetKindNames, name => name.Contains("Scoring", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(packetKindNames, name => name.Contains("Ollama", StringComparison.OrdinalIgnoreCase));
    }

    private static OllamaLoopbackScoringProvider Provider(IReadOnlyDictionary<string, string> settings, out FakeScoringHttpClient http)
    {
        http = new FakeScoringHttpClient("{\"response\":\"0.8\",\"model\":\"qwen2.5\"}");
        return new OllamaLoopbackScoringProvider(http, new KillSwitchService(() => settings), () => settings);
    }

    private static Dictionary<string, string> EnabledSettings()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [NotConfiguredScoringProvider.EnabledSetting] = bool.TrueString,
            [OllamaLoopbackScoringProvider.OllamaEnabledSetting] = bool.TrueString
        };
    }

    private static LocalScoringRequest Request()
    {
        return new LocalScoringRequest(Sha(), "proposal rubric");
    }

    private static string Sha()
    {
        return new string('a', 64);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "wevito.godot")) ||
                Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
