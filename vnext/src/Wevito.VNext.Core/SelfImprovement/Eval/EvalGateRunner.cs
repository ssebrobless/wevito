namespace Wevito.VNext.Core.SelfImprovement.Eval;

public sealed class EvalGateRunner
{
    private readonly EvalGateManifest _manifest;
    private readonly IHeldOutEvalStore? _heldOutEvalStore;
    private readonly KillSwitchService? _killSwitchService;

    public EvalGateRunner(
        EvalGateManifest? manifest = null,
        IHeldOutEvalStore? heldOutEvalStore = null,
        KillSwitchService? killSwitchService = null)
    {
        _manifest = manifest ?? EvalGateManifest.Default();
        _heldOutEvalStore = heldOutEvalStore;
        _killSwitchService = killSwitchService;
    }

    public IReadOnlyDictionary<string, EvalGateResult> Preview()
    {
        var reason = _killSwitchService?.IsActive() == true
            ? "kill_switch=true"
            : "no_eval_run_wired_v0";

        // The held-out store is intentionally retained only for future gate-runner use.
        _ = _heldOutEvalStore;

        return _manifest.Gates.ToDictionary(
            gate => gate,
            _ => (EvalGateResult)new EvalGateResult.NotApplicable(reason),
            StringComparer.OrdinalIgnoreCase);
    }
}
