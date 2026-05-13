using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class OnnxPhiLocalModelAdapter : IModelAdapter
{
    public const string Provider = "onnx-phi";
    public const string DefaultModelFolder = "vnext/content/local-models/generation/phi-3-mini-4k-int4";

    private readonly string _modelFolder;
    private readonly Func<IReadOnlyDictionary<string, string>> _settingsProvider;
    private readonly LocalModelAdapter _fallbackAdapter;
    private readonly KillSwitchService? _killSwitchService;

    public OnnxPhiLocalModelAdapter(
        string? modelFolder = null,
        Func<IReadOnlyDictionary<string, string>>? settingsProvider = null,
        LocalModelAdapter? fallbackAdapter = null,
        KillSwitchService? killSwitchService = null)
    {
        _modelFolder = string.IsNullOrWhiteSpace(modelFolder)
            ? Path.GetFullPath(DefaultModelFolder)
            : Path.GetFullPath(modelFolder);
        _settingsProvider = settingsProvider ?? (() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        _fallbackAdapter = fallbackAdapter ?? new LocalModelAdapter(killSwitchService);
        _killSwitchService = killSwitchService;
    }

    public bool IsFeatureEnabled => ReadBool(_settingsProvider(), ModelProviderModeService.InProcessLocalRuntimeEnabledSetting, false);

    public bool HasWeights => Directory.Exists(_modelFolder) &&
                              (File.Exists(Path.Combine(_modelFolder, "model.onnx")) ||
                               File.Exists(Path.Combine(_modelFolder, "model.onnx.data")) ||
                               Directory.EnumerateFiles(_modelFolder, "*.onnx", SearchOption.TopDirectoryOnly).Any());

    public async Task<ModelResponse> SuggestAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        if (_killSwitchService?.IsActive() == true)
        {
            return new ModelResponse(Provider, "phi-3-mini-4k-int4", "", DidCallProvider: false, BlockReason: "kill_switch=true");
        }

        if (!IsFeatureEnabled)
        {
            return await FallbackAsync(request, "local_runtime_inproc_enabled=false", cancellationToken).ConfigureAwait(false);
        }

        if (!HasWeights)
        {
            return await FallbackAsync(request, "ONNX Phi weights are missing.", cancellationToken).ConfigureAwait(false);
        }

        // C-PHASE 79 deliberately exposes the route and safety seam only. Actual GenAI inference remains a later opt-in.
        return await FallbackAsync(request, "ONNX Phi route is available but inference is deferred to a later approved phase.", cancellationToken).ConfigureAwait(false);
    }

    private async Task<ModelResponse> FallbackAsync(ModelRequest request, string reason, CancellationToken cancellationToken)
    {
        var fallback = await _fallbackAdapter.SuggestAsync(request, cancellationToken).ConfigureAwait(false);
        return fallback with
        {
            Provider = Provider,
            Model = "phi-3-mini-4k-int4+deterministic-fallback",
            BlockReason = reason
        };
    }

    private static bool ReadBool(IReadOnlyDictionary<string, string> settings, string key, bool defaultValue)
    {
        return settings.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed)
            ? parsed
            : defaultValue;
    }
}
