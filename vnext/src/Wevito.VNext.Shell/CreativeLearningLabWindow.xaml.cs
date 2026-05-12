using System.Windows;
using System.IO;
using Wevito.VNext.Core;

namespace Wevito.VNext.Shell;

public partial class CreativeLearningLabWindow : Window
{
    private readonly LearningLabArtifactIndexer _indexer = new();
    private readonly LearningLabBundleService _bundleService = new();
    private LearningLabLabelStore? _labelStore;
    private LearningLabArtifactIndex? _index;
    private IReadOnlyDictionary<string, LearningLabLabelRecord> _labels = new Dictionary<string, LearningLabLabelRecord>(StringComparer.OrdinalIgnoreCase);
    private string _repoRoot = "";

    public CreativeLearningLabWindow()
    {
        InitializeComponent();
        LabelComboBox.ItemsSource = LearningLabLabelStore.AllowedLabels;
        LabelComboBox.SelectedItem = "defer";
    }

    public void LoadProject(string repoRoot)
    {
        _repoRoot = Path.GetFullPath(repoRoot);
        _labelStore = new LearningLabLabelStore();
        RefreshIndex();
    }

    private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshIndex();
    }

    private async void RefreshIndex()
    {
        if (string.IsNullOrWhiteSpace(_repoRoot))
        {
            return;
        }

        try
        {
            _index = _indexer.Index(new LearningLabArtifactIndexRequest(_repoRoot));
            _labels = _labelStore is null
                ? new Dictionary<string, LearningLabLabelRecord>(StringComparer.OrdinalIgnoreCase)
                : await _labelStore.ListLatestAsync();
            var rows = _index.Artifacts
                .Select(artifact => new LearningLabArtifactViewModel(artifact, ResolveLabel(artifact), ResolveNotes(artifact)))
                .ToList();
            var labeledCount = rows.Count(row => row.Label != "unlabeled");
            var bundle = _bundleService.Evaluate(new LearningLabBundleRequest(
                _index,
                _labels,
                IntendedUse: BundleIntendedUseTextBox.Text.Trim(),
                RollbackPathKnown: true));

            RawMetricText.Text = _index.Metrics.Raw.ToString();
            CleanedMetricText.Text = _index.Metrics.Cleaned.ToString();
            LabeledMetricText.Text = labeledCount.ToString();
            BundledMetricText.Text = bundle.IsReady ? "1" : "0";
            EvalMetricText.Text = "0";
            IndexedEvidenceText.Text = $"{_index.Metrics.MarkdownFiles} markdown files, {_index.Metrics.JsonFiles} JSON files indexed.";
            BundleGateText.Text = BuildBundleGateText(bundle);
            QueueHintText.Text = _index.Artifacts.Count == 0
                ? "No visual-review or animation-run markdown/JSON artifacts found yet."
                : $"{_index.Artifacts.Count} read-only artifacts indexed from visual-review and animation-runs.";
            ArtifactListView.ItemsSource = rows.Take(500).ToList();
            ExportReviewedBundleButton.IsEnabled = bundle.IsReady;
            StatusText.Text = $"Reviewed index refreshed {DateTimeOffset.Now:t}. Nothing trains automatically.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Learning Lab refresh failed without mutating files: {ex.Message}";
        }
    }

    private void ArtifactListView_OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ArtifactListView.SelectedItem is not LearningLabArtifactViewModel row)
        {
            SelectedArtifactText.Text = "Select an artifact to label it.";
            return;
        }

        SelectedArtifactText.Text = $"{row.FileName}\n{row.RelativePath}";
        LabelComboBox.SelectedItem = row.Label == "unlabeled" ? "defer" : row.Label;
        NotesTextBox.Text = row.Notes;
    }

    private async void SaveLabelButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_labelStore is null || ArtifactListView.SelectedItem is not LearningLabArtifactViewModel row)
        {
            StatusText.Text = "Select an artifact before saving a label.";
            return;
        }

        var label = (LabelComboBox.SelectedItem as string) ?? "defer";
        var reviewer = string.IsNullOrWhiteSpace(ReviewerTextBox.Text) ? "code-side" : ReviewerTextBox.Text.Trim();
        await _labelStore.SaveAsync(new LearningLabLabelInput(
            row.AbsolutePath,
            label,
            reviewer,
            NotesTextBox.Text.Trim(),
            DateTimeOffset.UtcNow));
        StatusText.Text = $"Saved '{label}' label for {row.FileName}. No training/export was performed.";
        RefreshIndex();
    }

    private void ExportReviewedBundleButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_index is null)
        {
            StatusText.Text = "Refresh the reviewed-example index before exporting.";
            return;
        }

        var outputRoot = Path.Combine(_repoRoot, "vnext", "artifacts", "creative-learning-lab");
        var result = _bundleService.ExportReviewedBundle(new LearningLabReviewedBundleExportRequest(
            new LearningLabBundleRequest(
                _index,
                _labels,
                BundleIntendedUseTextBox.Text.Trim(),
                RollbackPathKnown: true),
            outputRoot,
            DateTimeOffset.UtcNow));

        BundleGateText.Text = BuildBundleGateText(result.Gate);
        StatusText.Text = result.Succeeded
            ? $"Exported reviewed bundle: {result.BundleFolder}. No training or binary asset copy occurred."
            : $"Reviewed bundle export blocked: {result.Message}";
    }

    private string ResolveLabel(LearningLabArtifactRecord artifact)
    {
        return _labels.TryGetValue(Path.GetFullPath(artifact.AbsolutePath), out var label)
            ? label.Label
            : "unlabeled";
    }

    private string ResolveNotes(LearningLabArtifactRecord artifact)
    {
        return _labels.TryGetValue(Path.GetFullPath(artifact.AbsolutePath), out var label)
            ? label.Notes
            : "";
    }

    private static string BuildBundleGateText(LearningLabBundleGateResult bundle)
    {
        return $"Accepted: {bundle.AcceptedCount} | Rejected: {bundle.RejectedCount} | Blocked: {bundle.BlockedCount} | Waiting: {bundle.WaitingCount}\n" +
               string.Join("\n", bundle.Reasons.Select(reason => "- " + reason));
    }

    private sealed class LearningLabArtifactViewModel
    {
        public LearningLabArtifactViewModel(LearningLabArtifactRecord artifact, string label, string notes)
        {
            Artifact = artifact;
            Label = label;
            Notes = notes;
        }

        public LearningLabArtifactRecord Artifact { get; }

        public string Label { get; }

        public string Notes { get; set; }

        public string ArtifactKind => Artifact.ArtifactKind;

        public string Status => Artifact.Status;

        public string Target => Artifact.Target;

        public string FileName => Artifact.FileName;

        public string RelativePath => Artifact.RelativePath;

        public string AbsolutePath => Artifact.AbsolutePath;
    }
}
