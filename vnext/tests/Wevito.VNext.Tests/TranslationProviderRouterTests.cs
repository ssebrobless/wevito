using System.Net.Http;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class TranslationProviderRouterTests
{
    [Fact]
    public async Task GetProviderStatusesAsync_ProbesLibreTranslateLanguages()
    {
        var router = new TranslationProviderRouter(_ => new FakeLibreTranslateClient());

        var statuses = await router.GetProviderStatusesAsync(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["LIBRETRANSLATE_URL"] = "http://localhost:5000",
            ["WEVITO_TRANSLATION_PROVIDER"] = "LibreTranslate"
        });

        var libre = statuses.Single(status => status.Provider == TranslationProviderKind.LibreTranslate);
        Assert.Equal(TranslationProviderAvailability.Available, libre.Availability);
        Assert.True(libre.IsUserSelected);
        Assert.True(libre.SupportsSelfHosted);
        Assert.Contains("2 language", libre.Detail);
        Assert.Contains("Text stays on your machine", libre.ConsentSummary);
    }

    [Fact]
    public void SelectPreferredProvider_PrefersUserSelectedAvailableProvider()
    {
        var router = new TranslationProviderRouter();
        var statuses = new[]
        {
            new TranslationProviderStatus(
                TranslationProviderKind.DeepL,
                TranslationProviderAvailability.Configured,
                SupportsGlossary: true,
                SupportsSelfHosted: false,
                "DeepL ready."),
            new TranslationProviderStatus(
                TranslationProviderKind.LibreTranslate,
                TranslationProviderAvailability.Available,
                SupportsGlossary: false,
                SupportsSelfHosted: true,
                "LibreTranslate ready.",
                IsUserSelected: true)
        };

        var selected = router.SelectPreferredProvider(statuses);

        Assert.Equal(TranslationProviderKind.LibreTranslate, selected.Provider);
    }

    [Fact]
    public async Task GetProviderStatusesAsync_ReportsLibreEndpointProbeFailure()
    {
        var router = new TranslationProviderRouter(_ => new ThrowingLibreTranslateClient());

        var statuses = await router.GetProviderStatusesAsync(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["LIBRETRANSLATE_URL"] = "http://localhost:5000"
        });

        var libre = statuses.Single(status => status.Provider == TranslationProviderKind.LibreTranslate);
        Assert.Equal(TranslationProviderAvailability.MissingEndpoint, libre.Availability);
        Assert.Contains("did not respond", libre.Detail);
    }

    private sealed class FakeLibreTranslateClient : LibreTranslateClient
    {
        public FakeLibreTranslateClient()
            : base(new HttpClient(new FakeHandler()), new Uri("http://localhost:5000/"))
        {
        }
    }

    private sealed class ThrowingLibreTranslateClient : LibreTranslateClient
    {
        public ThrowingLibreTranslateClient()
            : base(new HttpClient(new ThrowingHandler()), new Uri("http://localhost:5000/"))
        {
        }
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    [
                      { "code": "en", "name": "English" },
                      { "code": "es", "name": "Spanish" }
                    ]
                    """)
            });
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("offline")
            });
        }
    }
}
