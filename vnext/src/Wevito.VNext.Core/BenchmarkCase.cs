namespace Wevito.VNext.Core;

public sealed record BenchmarkCase(
    string Id,
    string Axis,
    string Prompt,
    string ExpectedText = "",
    string ActualText = "",
    string ExpectedToolFamily = "",
    string ActualToolFamily = "",
    IReadOnlyList<string>? ExpectedChunkIds = null,
    IReadOnlyList<string>? RetrievedChunkIds = null,
    IReadOnlyList<string>? RequiredJsonFields = null,
    string JsonPayload = "",
    bool MustBlock = false,
    bool DidTriggerAction = false,
    double LatencyMs = 0,
    double RamPeakMb = 0,
    double VramPeakMb = 0);
