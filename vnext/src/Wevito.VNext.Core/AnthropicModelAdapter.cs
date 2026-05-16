using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class AnthropicModelAdapter : IModelAdapter
{
    private const string Provider = "anthropic";
    private const string DefaultModel = "claude-3-5-sonnet-latest";
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private readonly HttpClient _httpClient;
    private readonly IModelCredentialStore _credentialStore;
    private readonly string _model;

    public AnthropicModelAdapter(
        HttpClient? httpClient = null,
        IModelCredentialStore? credentialStore = null,
        string model = DefaultModel)
    {
        _httpClient = httpClient ?? new HttpClient();
        _credentialStore = credentialStore ?? new WindowsCredentialManagerModelCredentialStore();
        _model = string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
    }

    public async Task<ModelResponse> SuggestAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var auditPath = ResolveAuditPath(request);

        if (!request.ApprovedForModelCall)
        {
            return await BlockAsync(request, auditPath, stopwatch, "Model call requires explicit first-call/user approval.", cancellationToken);
        }

        var apiKey = await _credentialStore.ReadApiKeyAsync(Provider, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return await BlockAsync(request, auditPath, stopwatch, "Missing Anthropic API key in Windows Credential Manager.", cancellationToken);
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        httpRequest.Headers.Add("x-api-key", apiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(BuildPayload(request)), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            var reason = $"Anthropic request failed with HTTP {(int)response.StatusCode}.";
            await WriteAuditAsync(request, auditPath, "blocked", stopwatch.Elapsed, reason, cancellationToken).ConfigureAwait(false);
            return new ModelResponse(Provider, _model, "", DidCallProvider: true, reason, auditPath, stopwatch.Elapsed);
        }

        var summary = ExtractSummary(body);
        await WriteAuditAsync(request, auditPath, "called", stopwatch.Elapsed, "", cancellationToken).ConfigureAwait(false);
        return new ModelResponse(Provider, _model, summary, DidCallProvider: true, AuditLogPath: auditPath, Latency: stopwatch.Elapsed);
    }

    private object BuildPayload(ModelRequest request)
    {
        var untrusted = request.UntrustedContext is { Count: > 0 }
            ? string.Join(Environment.NewLine, request.UntrustedContext.Select(PetModelSummaryService.WrapUntrusted))
            : "None.";
        var trusted = request.TrustedContext is { Count: > 0 }
            ? string.Join(Environment.NewLine, request.TrustedContext)
            : "None.";

        return new
        {
            model = _model,
            max_tokens = 300,
            system = "You are a read-only Wevito helper pet. Suggest next steps from the tool output. Do not request mutation, credentials, or hidden data.",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = $"""
                    Helper: {request.PetName} ({request.AgentRole})
                    Tool family: {request.ToolFamily}
                    User task: {request.UserTask}

                    Trusted context:
                    {trusted}

                    Untrusted context:
                    {untrusted}

                    Tool summary:
                    {request.ToolSummary}
                    """
                }
            }
        };
    }

    private static string ExtractSummary(string body)
    {
        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("content", out var content) ||
            content.ValueKind != JsonValueKind.Array)
        {
            return "";
        }

        foreach (var item in content.EnumerateArray())
        {
            if (item.TryGetProperty("text", out var text) &&
                text.ValueKind == JsonValueKind.String)
            {
                return text.GetString() ?? "";
            }
        }

        return "";
    }

    private static async Task<ModelResponse> BlockAsync(
        ModelRequest request,
        string auditPath,
        Stopwatch stopwatch,
        string reason,
        CancellationToken cancellationToken)
    {
        stopwatch.Stop();
        await WriteAuditAsync(request, auditPath, "blocked", stopwatch.Elapsed, reason, cancellationToken).ConfigureAwait(false);
        return new ModelResponse(Provider, DefaultModel, "", DidCallProvider: false, reason, auditPath, stopwatch.Elapsed);
    }

    private static string ResolveAuditPath(ModelRequest request)
    {
        var artifactRoot = string.IsNullOrWhiteSpace(request.ArtifactRoot)
            ? Path.Combine("vnext", "artifacts", "pet-tasks", BuildAuditSlug(request))
            : request.ArtifactRoot;
        return Path.GetFullPath(Path.Combine(artifactRoot, "model-call.json"));
    }

    private static string BuildAuditSlug(ModelRequest request)
    {
        var timestamp = (request.RequestedAtUtc == default ? DateTimeOffset.UtcNow : request.RequestedAtUtc).ToString("yyyyMMdd-HHmmss");
        var pet = Slugify(request.PetName);
        var tool = Slugify(request.ToolFamily);
        return $"{timestamp}-{pet}-{tool}";
    }

    private static string Slugify(string value)
    {
        var chars = value
            .Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_')
            .Select(char.ToLowerInvariant)
            .ToArray();
        return chars.Length == 0 ? "model-call" : new string(chars);
    }

    private static async Task WriteAuditAsync(
        ModelRequest request,
        string auditPath,
        string decision,
        TimeSpan latency,
        string blockReason,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(auditPath) ?? ".");
        var audit = new
        {
            schemaVersion = "1",
            provider = Provider,
            model = DefaultModel,
            petId = request.PetId,
            petName = request.PetName,
            helperRole = request.AgentRole,
            toolFamily = request.ToolFamily,
            argsHash = ComputeArgsHash(request),
            decision,
            blockReason,
            latencyMs = (long)latency.TotalMilliseconds,
            generatedAtUtc = DateTimeOffset.UtcNow
        };
        await File.WriteAllTextAsync(auditPath, JsonSerializer.Serialize(audit, JsonDefaults.Options), cancellationToken).ConfigureAwait(false);
    }

    private static string ComputeArgsHash(ModelRequest request)
    {
        var material = JsonSerializer.Serialize(new
        {
            request.PetId,
            request.PetName,
            helperRole = request.AgentRole,
            request.ToolFamily,
            request.UserTask,
            request.ToolSummary,
            request.TrustedContext,
            request.UntrustedContext
        }, JsonDefaults.Options);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
    }
}

public sealed class WindowsCredentialManagerModelCredentialStore : IModelCredentialStore
{
    public Task<string?> ReadApiKeyAsync(string provider, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult<string?>(null);
        }

        var targetName = $"Wevito/{provider}/api-key";
        if (!CredRead(targetName, CredentialType.Generic, 0, out var credentialPointer))
        {
            return Task.FromResult<string?>(null);
        }

        try
        {
            var credential = Marshal.PtrToStructure<NativeCredential>(credentialPointer);
            if (credential.CredentialBlobSize == 0 || credential.CredentialBlob == IntPtr.Zero)
            {
                return Task.FromResult<string?>(null);
            }

            var bytes = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);
            return Task.FromResult<string?>(Encoding.Unicode.GetString(bytes).TrimEnd('\0'));
        }
        finally
        {
            CredFree(credentialPointer);
        }
    }

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, CredentialType type, int reservedFlag, out IntPtr credentialPointer);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern void CredFree(IntPtr credentialPointer);

    private enum CredentialType : uint
    {
        Generic = 1
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private readonly struct NativeCredential
    {
        public readonly uint Flags;
        public readonly uint Type;
        public readonly IntPtr TargetName;
        public readonly IntPtr Comment;
        public readonly System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public readonly uint CredentialBlobSize;
        public readonly IntPtr CredentialBlob;
        public readonly uint Persist;
        public readonly uint AttributeCount;
        public readonly IntPtr Attributes;
        public readonly IntPtr TargetAlias;
        public readonly IntPtr UserName;
    }
}
