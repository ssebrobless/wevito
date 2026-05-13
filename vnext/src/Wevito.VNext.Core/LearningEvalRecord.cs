namespace Wevito.VNext.Core;

public sealed record LearningEvalExample(
    string SourceId,
    string Query,
    string Content);

public sealed record LearningEvalMetrics(
    double RecallAt1,
    double RecallAt3,
    double MeanReciprocalRank,
    double LatencyMsP50,
    double LatencyMsP95);

public sealed record LearningEvalRunRecord(
    string SchemaVersion,
    string DatasetVersion,
    string BaselineDatasetVersion,
    LearningEvalMetrics Metrics,
    LearningEvalMetrics? PriorBaselineMetrics,
    bool Regression,
    bool BaselinePromoted,
    int ExamplesEvaluated,
    DateTimeOffset CreatedAtUtc);

public sealed record LearningEvalResult(
    bool Succeeded,
    string DatasetVersion,
    string ReportPath,
    string SummaryPath,
    string BaselinePath,
    LearningEvalMetrics Metrics,
    bool Regression,
    bool BaselinePromoted,
    string Message);

public sealed record LearningEvalRequest(
    string DatasetRoot,
    string ArtifactRoot,
    string BaselinePath,
    DateTimeOffset CreatedAtUtc,
    string DatasetVersion = "",
    double RegressionTolerance = 0.02);
