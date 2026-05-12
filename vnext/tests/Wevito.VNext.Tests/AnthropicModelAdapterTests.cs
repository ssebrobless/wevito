using System.Net;
using System.Net.Http;
using System.Text;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Tests;

public sealed class AnthropicModelAdapterTests
{
    [Fact]
    public async Task SuggestAsync_BlocksWithoutApprovalBeforeReadingCredentials()
    {
        var credentialStore = new RecordingCredentialStore("secret");
        var adapter = new AnthropicModelAdapter(new HttpClient(new StaticHttpHandler()), credentialStore);

        var result = await adapter.SuggestAsync(BuildRequest(approved: false));

        Assert.False(result.DidCallProvider);
        Assert.Contains("approval", result.BlockReason, StringComparison.OrdinalIgnoreCase);
        Assert.False(credentialStore.WasRead);
        Assert.True(File.Exists(result.AuditLogPath));
    }

    [Fact]
    public async Task SuggestAsync_BlocksWhenCredentialManagerHasNoKey()
    {
        var credentialStore = new RecordingCredentialStore(null);
        var adapter = new AnthropicModelAdapter(new HttpClient(new StaticHttpHandler()), credentialStore);

        var result = await adapter.SuggestAsync(BuildRequest(approved: true));

        Assert.False(result.DidCallProvider);
        Assert.Contains("API key", result.BlockReason, StringComparison.OrdinalIgnoreCase);
        Assert.True(credentialStore.WasRead);
        Assert.True(File.Exists(result.AuditLogPath));
    }

    [Fact]
    public async Task SuggestAsync_CallsProviderAfterApprovalAndCredential()
    {
        var handler = new StaticHttpHandler("""{"content":[{"type":"text","text":"Use the sprite audit findings first."}]}""");
        var adapter = new AnthropicModelAdapter(new HttpClient(handler), new RecordingCredentialStore("secret"));

        var result = await adapter.SuggestAsync(BuildRequest(approved: true));

        Assert.True(result.DidCallProvider);
        Assert.Equal("Use the sprite audit findings first.", result.Summary);
        Assert.True(handler.WasCalled);
        Assert.True(File.Exists(result.AuditLogPath));
    }

    private static ModelRequest BuildRequest(bool approved)
    {
        var root = Path.Combine(Path.GetTempPath(), "wevito-model-adapter-tests", Guid.NewGuid().ToString("N"));
        return new ModelRequest(
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            "Scout",
            PetHelperRole.ResearchHelper,
            "localDocs",
            "summarize the local docs",
            "localDocs preview ready",
            ApprovedForModelCall: approved,
            ArtifactRoot: root,
            RequestedAtUtc: DateTimeOffset.Parse("2026-05-07T12:00:00Z"));
    }

    private sealed class RecordingCredentialStore(string? apiKey) : IModelCredentialStore
    {
        public bool WasRead { get; private set; }

        public Task<string?> ReadApiKeyAsync(string provider, CancellationToken cancellationToken = default)
        {
            WasRead = true;
            return Task.FromResult(apiKey);
        }
    }

    private sealed class StaticHttpHandler(string responseBody = "{}") : HttpMessageHandler
    {
        public bool WasCalled { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
