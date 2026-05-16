namespace Wevito.VNext.Core;

public sealed record ChatSessionSummary(
    Guid SessionId,
    string Title,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int TurnCount);
