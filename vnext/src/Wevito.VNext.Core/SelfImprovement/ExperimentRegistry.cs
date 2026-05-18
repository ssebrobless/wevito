namespace Wevito.VNext.Core.SelfImprovement;

public sealed class ExperimentRegistry
{
    private readonly List<ExperimentDescriptor> _registeredKinds = [];

    private ExperimentRegistry()
    {
    }

    public IReadOnlyList<ExperimentDescriptor> RegisteredKinds => _registeredKinds;

    public static ExperimentRegistry Empty()
    {
        return new ExperimentRegistry();
    }

    public static ExperimentRegistry ForCompositionRoot(params ExperimentDescriptor[] descriptors)
    {
        var registry = new ExperimentRegistry();
        foreach (var descriptor in descriptors)
        {
            registry.Register(descriptor);
        }

        return registry;
    }

    public static ExperimentRegistry ForTests(params ExperimentDescriptor[] descriptors)
    {
        var registry = new ExperimentRegistry();
        foreach (var descriptor in descriptors)
        {
            registry.Register(descriptor);
        }

        return registry;
    }

    private void Register(ExperimentDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor.Kind.Value))
        {
            throw new ArgumentException("Experiment kind is required.", nameof(descriptor));
        }

        if (_registeredKinds.Any(existing => string.Equals(existing.Kind.Value, descriptor.Kind.Value, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Experiment kind already registered: {descriptor.Kind.Value}");
        }

        _registeredKinds.Add(descriptor);
    }
}
