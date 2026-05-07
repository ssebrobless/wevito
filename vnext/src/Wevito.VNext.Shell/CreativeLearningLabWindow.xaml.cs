using System.Windows;
using System.IO;
using Wevito.VNext.Core;

namespace Wevito.VNext.Shell;

public partial class CreativeLearningLabWindow : Window
{
    private readonly LearningLabArtifactIndexer _indexer = new();
    private string _repoRoot = "";

    public CreativeLearningLabWindow()
    {
        InitializeComponent();
    }

    public void LoadProject(string repoRoot)
    {
        _repoRoot = Path.GetFullPath(repoRoot);
        RefreshIndex();
    }

    private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshIndex();
    }

    private void RefreshIndex()
    {
        if (string.IsNullOrWhiteSpace(_repoRoot))
        {
            return;
        }

        var index = _indexer.Index(new LearningLabArtifactIndexRequest(_repoRoot));
        RawMetricText.Text = index.Metrics.Raw.ToString();
        CleanedMetricText.Text = index.Metrics.Cleaned.ToString();
        LabeledMetricText.Text = index.Metrics.Labeled.ToString();
        BundledMetricText.Text = index.Metrics.Bundled.ToString();
        EvalMetricText.Text = index.Metrics.Eval.ToString();
        IndexedEvidenceText.Text = $"{index.Metrics.MarkdownFiles} markdown files, {index.Metrics.JsonFiles} JSON files indexed.";
        QueueHintText.Text = index.Artifacts.Count == 0
            ? "No visual-review or animation-run markdown/JSON artifacts found yet."
            : $"{index.Artifacts.Count} read-only artifacts indexed from visual-review and animation-runs.";
        ArtifactListView.ItemsSource = index.Artifacts.Take(500).ToList();
        StatusText.Text = $"Read-only index refreshed {DateTimeOffset.Now:t}. Nothing is labeled, exported, trained, or mutated.";
    }
}
