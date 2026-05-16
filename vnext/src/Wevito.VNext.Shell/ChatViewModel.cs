using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Wevito.VNext.Core;

namespace Wevito.VNext.Shell;

public sealed class ChatViewModel : INotifyPropertyChanged
{
    private string _statusText = "Local chat ready.";
    private string _searchText = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = [];

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetField(ref _searchText, value);
    }

    public void RenderTurns(IEnumerable<ChatTurn> turns)
    {
        Messages.Clear();
        foreach (var turn in turns)
        {
            Messages.Add(ChatMessageViewModel.FromTurn(turn));
        }
    }

    public void RenderSearchResults(IEnumerable<ChatTurn> turns)
    {
        RenderTurns(turns);
        StatusText = Messages.Count == 0 ? "No matching chat turns." : $"Showing {Messages.Count} search hit(s).";
    }

    public ChatMessageViewModel AppendUser(string text)
    {
        var message = new ChatMessageViewModel("user", text, false, "", "", "");
        Messages.Add(message);
        return message;
    }

    public ChatMessageViewModel AppendAssistantPlaceholder()
    {
        var message = new ChatMessageViewModel("assistant", "", false, "", "", "");
        Messages.Add(message);
        return message;
    }

    public void AppendToolEvent(ChatStreamEvent streamEvent)
    {
        Messages.Add(new ChatMessageViewModel(
            "tool",
            streamEvent.Kind == ChatStreamEventKind.ToolCallStart ? $"using {streamEvent.ToolName}..." : streamEvent.Content,
            true,
            streamEvent.ToolName,
            streamEvent.ToolCallJson,
            streamEvent.ToolResultId));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class ChatMessageViewModel : INotifyPropertyChanged
{
    private string _content;

    public ChatMessageViewModel(string role, string content, bool isToolCall, string toolName, string toolCallJson, string toolResultId)
    {
        Role = role;
        _content = content;
        IsToolCall = isToolCall;
        ToolName = toolName;
        ToolCallJson = toolCallJson;
        ToolResultId = toolResultId;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Role { get; }

    public string Content
    {
        get => _content;
        set
        {
            if (_content == value)
            {
                return;
            }

            _content = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
        }
    }

    public bool IsToolCall { get; }

    public string ToolName { get; }

    public string ToolCallJson { get; }

    public string ToolResultId { get; }

    public string Header => IsToolCall
        ? $"Tool: {ToolName}"
        : Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
            ? "Wevito"
            : Role;

    public bool CanBookmarkForBenchmark => Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) && !IsToolCall;

    public Visibility BenchmarkBookmarkVisibility => CanBookmarkForBenchmark ? Visibility.Visible : Visibility.Collapsed;

    public Visibility PinVisibility => IsToolCall ? Visibility.Collapsed : Visibility.Visible;

    public static ChatMessageViewModel FromTurn(ChatTurn turn)
    {
        return new ChatMessageViewModel(
            turn.Role,
            turn.Content,
            turn.Role.Equals("tool", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(turn.ToolCallJson),
            ExtractToolName(turn.ToolCallJson),
            turn.ToolCallJson ?? "",
            turn.ToolResultId ?? "");
    }

    private static string ExtractToolName(string? toolJson)
    {
        if (string.IsNullOrWhiteSpace(toolJson))
        {
            return "tool";
        }

        return toolJson.Contains("localDocs", StringComparison.OrdinalIgnoreCase) ? "localDocs" : "tool";
    }
}
