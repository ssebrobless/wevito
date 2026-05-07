using System.Net;
using System.Net.Http;
using System.Text;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class LibreTranslateClientTests
{
    [Fact]
    public async Task GetLanguagesAsync_ReadsLocalSidecarLanguages()
    {
        var handler = new CapturingHandler("""
            [
              { "code": "en", "name": "English" },
              { "code": "es", "name": "Spanish" }
            ]
            """);
        var client = new LibreTranslateClient(new HttpClient(handler), new Uri("http://localhost:5000/"));

        var languages = await client.GetLanguagesAsync();

        Assert.Equal("http://localhost:5000/languages", handler.RequestUri);
        Assert.Equal(2, languages.Count);
        Assert.Contains(languages, language => language.Code == "es" && language.Name == "Spanish");
    }

    [Fact]
    public async Task TranslateAsync_PostsTextToLocalSidecar()
    {
        var handler = new CapturingHandler("""
            {
              "translatedText": "Hola goose"
            }
            """);
        var client = new LibreTranslateClient(new HttpClient(handler), new Uri("http://localhost:5000"));

        var response = await client.TranslateAsync("Hello goose", "en", "es");

        Assert.Equal("http://localhost:5000/translate", handler.RequestUri);
        Assert.Contains("\"q\":\"Hello goose\"", handler.RequestBody);
        Assert.Contains("\"source\":\"en\"", handler.RequestBody);
        Assert.Contains("\"target\":\"es\"", handler.RequestBody);
        Assert.Equal("Hola goose", response.TranslatedText);
    }

    [Fact]
    public async Task SetupGuideWriter_WritesConsentAndInstallNotes()
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-libretranslate-guide-tests", Guid.NewGuid().ToString("N"), "vnext", "artifacts", "pet-tasks", "20260507-libretranslate-setup");

        var path = await LibreTranslateSetupGuideWriter.WriteAsync(root, DateTimeOffset.Parse("2026-05-07T00:00:00Z"));

        var markdown = File.ReadAllText(path);
        Assert.Contains("LibreTranslate Local Sidecar Setup", markdown);
        Assert.Contains("https://github.com/LibreTranslate/LibreTranslate", markdown);
        Assert.Contains("text stays on your machine", markdown, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        public CapturingHandler(string responseBody)
        {
            _responseBody = responseBody;
        }

        public string RequestUri { get; private set; } = "";

        public string RequestBody { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString() ?? "";
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
