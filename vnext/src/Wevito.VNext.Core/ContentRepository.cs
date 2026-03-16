using System.Text.Json;
using Wevito.VNext.Contracts;

namespace Wevito.VNext.Core;

public sealed class ContentRepository
{
    private readonly string _contentRoot;

    public ContentRepository(string contentRoot)
    {
        _contentRoot = contentRoot;
    }

    public async Task<GameContent> LoadAsync(CancellationToken cancellationToken = default)
    {
        var species = await LoadArrayAsync<SpeciesDefinition>("species.json", cancellationToken);
        var actions = await LoadArrayAsync<ActionDefinition>("actions.json", cancellationToken);
        var environments = await LoadArrayAsync<EnvironmentDefinition>("environments.json", cancellationToken);
        var tools = await LoadArrayAsync<ToolDefinition>("tool_definitions.json", cancellationToken);
        var needs = await LoadArrayAsync<NeedDefinition>("needs.json", cancellationToken);
        var statuses = await LoadArrayAsync<StatusDefinition>("statuses.json", cancellationToken);
        var items = await LoadArrayAsync<ItemDefinition>("items.json", cancellationToken);
        var conditions = await LoadArrayAsync<ConditionDefinition>("conditions.json", cancellationToken);
        return new GameContent(species, actions, environments, tools, needs, statuses, items, conditions);
    }

    private async Task<IReadOnlyList<TItem>> LoadArrayAsync<TItem>(string fileName, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_contentRoot, fileName);
        await using var stream = File.OpenRead(path);
        var items = await JsonSerializer.DeserializeAsync<List<TItem>>(stream, JsonDefaults.Options, cancellationToken);
        return items ?? [];
    }
}
