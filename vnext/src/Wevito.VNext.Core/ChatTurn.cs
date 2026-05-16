namespace Wevito.VNext.Core;

public sealed record ChatTurn(
    Guid SessionId,
    Guid TurnId,
    string Role,
    string Content,
    string? ToolCallJson,
    string? ToolResultId,
    DateTimeOffset CreatedAtUtc,
    string ModelId,
    int TokensUsed);
