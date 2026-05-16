using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wevito.VNext.Core;

namespace Wevito.VNext.Shell;

public partial class ChatPanel : UserControl
{
    private readonly ChatViewModel _viewModel = new();
    private ChatSessionService? _sessionService;
    private ChatStreamingService? _streamingService;
    private CancellationTokenSource? _streamCancellation;
    private Guid _sessionId;

    public ChatPanel()
    {
        InitializeComponent();
        DataContext = _viewModel;
        PreviewKeyDown += ChatPanel_OnPreviewKeyDown;
    }

    public void Configure(ChatSessionService sessionService, ChatHistoryStore historyStore, ChatStreamingService streamingService)
    {
        _sessionService = sessionService;
        _streamingService = streamingService;
        _sessionId = _sessionService.GetCurrentSessionId();
        _viewModel.RenderTurns(_sessionService.GetTurns(_sessionId));
        _viewModel.StatusText = "Local chat ready. Tool calls are preview-only until C-PHASE 109.";
    }

    private async void SendButton_OnClick(object sender, RoutedEventArgs e)
    {
        await SendAsync();
    }

    private void StopButton_OnClick(object sender, RoutedEventArgs e)
    {
        _streamCancellation?.Cancel();
    }

    private void NewChatButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_sessionService is null)
        {
            return;
        }

        _streamCancellation?.Cancel();
        _sessionId = _sessionService.StartNewSession();
        _viewModel.RenderTurns([]);
        _viewModel.StatusText = "Started a new local chat.";
    }

    private void SearchButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_sessionService is null)
        {
            return;
        }

        var query = SearchTextBox.Text ?? "";
        _viewModel.SearchText = query;
        _viewModel.RenderSearchResults(_sessionService.SearchTurns(query, limit: 50));
    }

    private async Task SendAsync()
    {
        if (_streamingService is null || _sessionService is null)
        {
            _viewModel.StatusText = "Chat services are still starting.";
            return;
        }

        var text = InputTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        InputTextBox.Text = "";
        _viewModel.AppendUser(text);
        var assistant = _viewModel.AppendAssistantPlaceholder();
        _streamCancellation?.Cancel();
        _streamCancellation = new CancellationTokenSource();
        SendButton.IsEnabled = false;
        _viewModel.StatusText = "Wevito is thinking locally...";

        try
        {
            await foreach (var streamEvent in _streamingService.StreamAssistantTurnAsync(_sessionId, text, _streamCancellation.Token))
            {
                switch (streamEvent.Kind)
                {
                    case ChatStreamEventKind.Token:
                        assistant.Content += streamEvent.Content;
                        break;
                    case ChatStreamEventKind.ToolCallStart:
                    case ChatStreamEventKind.ToolCallResult:
                    case ChatStreamEventKind.ToolCallEnd:
                        _viewModel.AppendToolEvent(streamEvent);
                        break;
                    case ChatStreamEventKind.Cancelled:
                        _viewModel.StatusText = "Chat turn cancelled.";
                        break;
                    case ChatStreamEventKind.Complete:
                        _viewModel.StatusText = "Chat turn complete.";
                        break;
                    case ChatStreamEventKind.Error:
                        assistant.Content += streamEvent.Content;
                        _viewModel.StatusText = "Chat turn stopped safely.";
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _viewModel.StatusText = "Chat turn cancelled.";
        }
        finally
        {
            SendButton.IsEnabled = true;
            _streamCancellation?.Dispose();
            _streamCancellation = null;
        }
    }

    private void ChatPanel_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _streamCancellation?.Cancel();
            e.Handled = true;
        }
    }
}
