namespace Wevito.VNext.Core;

public sealed class LocalModelProviderRouterAdapter : IModelAdapter
{
    private readonly Func<IReadOnlyDictionary<string, string>> _settingsProvider;
    private readonly ModelProviderModeService _modeService;
    private readonly LocalRuntimeProbeService _probeService;
    private readonly IModelAdapter _ollamaAdapter;
    private readonly IModelAdapter _onnxPhiAdapter;
    private readonly IModelAdapter _deterministicAdapter;
    private readonly Func<bool> _onnxPhiWeightsPresent;

    public LocalModelProviderRouterAdapter(
        Func<IReadOnlyDictionary<string, string>> settingsProvider,
        ModelProviderModeService? modeService = null,
        LocalRuntimeProbeService? probeService = null,
        IModelAdapter? ollamaAdapter = null,
        IModelAdapter? onnxPhiAdapter = null,
        IModelAdapter? deterministicAdapter = null,
        Func<bool>? onnxPhiWeightsPresent = null)
    {
        _settingsProvider = settingsProvider;
        _modeService = modeService ?? new ModelProviderModeService();
        _probeService = probeService ?? new LocalRuntimeProbeService();
        _deterministicAdapter = deterministicAdapter ?? new LocalModelAdapter();
        _ollamaAdapter = ollamaAdapter ?? new OllamaLocalModelAdapter(settingsProvider: settingsProvider);
        _onnxPhiAdapter = onnxPhiAdapter ?? new OnnxPhiLocalModelAdapter(settingsProvider: settingsProvider);
        _onnxPhiWeightsPresent = onnxPhiWeightsPresent ?? (() => false);
    }

    public async Task<ModelResponse> SuggestAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        var settings = _settingsProvider();
        var parsed = _modeService.ReadSettings(settings);
        if (parsed.Mode == ModelProviderMode.Disabled)
        {
            return new ModelResponse("none", "disabled", "", DidCallProvider: false, BlockReason: "Model provider mode is disabled.");
        }

        if (parsed.Mode == ModelProviderMode.ApprovedCloud)
        {
            return new ModelResponse("none", "hosted-blocked", "", DidCallProvider: false, BlockReason: "Hosted providers are blocked in C-PHASE 79.");
        }

        var probe = await _probeService.ProbeAsync(settings, null, request.RequestedAtUtc == default ? DateTimeOffset.UtcNow : request.RequestedAtUtc, cancellationToken).ConfigureAwait(false);
        var route = _modeService.DecideRoute(parsed, probe, _onnxPhiWeightsPresent());
        return route.Route switch
        {
            ModelProviderRoute.Ollama => await _ollamaAdapter.SuggestAsync(request, cancellationToken).ConfigureAwait(false),
            ModelProviderRoute.OnnxPhi => await _onnxPhiAdapter.SuggestAsync(request, cancellationToken).ConfigureAwait(false),
            ModelProviderRoute.DeterministicLocal => await _deterministicAdapter.SuggestAsync(request, cancellationToken).ConfigureAwait(false),
            _ => new ModelResponse(route.ProviderId, route.Route.ToString(), "", DidCallProvider: false, BlockReason: route.Reason)
        };
    }
}
