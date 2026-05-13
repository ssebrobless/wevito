namespace Wevito.VNext.Core;

public sealed record EvidencePacket(
    Guid PacketId,
    string PacketKind,
    Guid? TaskCardId,
    DateTimeOffset CreatedAtUtc,
    bool DidUseNetwork,
    bool DidUseHostedAi,
    bool DidUseLocalModel,
    bool DidMutate,
    string ArtifactPath,
    string Summary,
    string Status,
    string Error = "");

public sealed record AuditLedgerRow(
    long Id,
    Guid PacketId,
    string PacketKind,
    Guid? TaskCardId,
    DateTimeOffset CreatedAtUtc,
    bool DidUseNetwork,
    bool DidUseHostedAi,
    bool DidUseLocalModel,
    bool DidMutate,
    string ArtifactPath,
    string Summary,
    string Status,
    string Error);
