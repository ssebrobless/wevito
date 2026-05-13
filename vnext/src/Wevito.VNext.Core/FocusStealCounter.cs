using System.Text.Json;

namespace Wevito.VNext.Core;

public sealed record FocusStealSnapshot(
    int Count,
    DateTimeOffset? LastRecordedAtUtc);

public sealed class FocusStealCounter
{
    private const string SchemaVersion = "1";
    private readonly string _statePath;
    private readonly object _gate = new();

    public FocusStealCounter(string? statePath = null)
    {
        _statePath = string.IsNullOrWhiteSpace(statePath)
            ? ResolveDefaultPath()
            : Path.GetFullPath(statePath);
    }

    public string StatePath => _statePath;

    public FocusStealSnapshot Read()
    {
        lock (_gate)
        {
            var state = ReadState();
            return new FocusStealSnapshot(state.Count, state.LastRecordedAtUtc);
        }
    }

    public FocusStealSnapshot RecordActivation(bool foregroundIsFullscreenOther, DateTimeOffset nowUtc)
    {
        lock (_gate)
        {
            var state = ReadState();
            if (!foregroundIsFullscreenOther)
            {
                EnsureStateFile(state);
                return new FocusStealSnapshot(state.Count, state.LastRecordedAtUtc);
            }

            state = state with
            {
                Count = state.Count + 1,
                LastRecordedAtUtc = nowUtc
            };
            WriteState(state);
            return new FocusStealSnapshot(state.Count, state.LastRecordedAtUtc);
        }
    }

    private void EnsureStateFile(FocusStealState state)
    {
        if (!File.Exists(_statePath))
        {
            WriteState(state);
        }
    }

    private FocusStealState ReadState()
    {
        try
        {
            if (!File.Exists(_statePath))
            {
                return new FocusStealState(SchemaVersion, 0, null);
            }

            var state = JsonSerializer.Deserialize<FocusStealState>(File.ReadAllText(_statePath));
            return state is null || state.SchemaVersion != SchemaVersion
                ? new FocusStealState(SchemaVersion, 0, null)
                : state;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new FocusStealState(SchemaVersion, 0, null);
        }
    }

    private void WriteState(FocusStealState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_statePath) ?? ".");
        File.WriteAllText(_statePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string ResolveDefaultPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wevito",
            "audit",
            "focus-steal.json");
    }

    private sealed record FocusStealState(
        string SchemaVersion,
        int Count,
        DateTimeOffset? LastRecordedAtUtc);
}
