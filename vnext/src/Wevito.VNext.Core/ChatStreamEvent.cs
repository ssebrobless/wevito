namespace Wevito.VNext.Core;

public enum ChatStreamEventKind
{
    Token,
    ToolCallStart,
    ToolCallResult,
    ToolCallEnd,
    Complete,
    Cancelled,
    Error
}

public sealed record ChatStreamEvent(
    ChatStreamEventKind Kind,
    string Content = "",
    string ToolName = "",
    string ToolCallJson = "",
    string ToolResultId = "");
