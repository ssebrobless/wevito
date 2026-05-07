using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Shell;

public partial class SpriteWorkflowV2Window : Window
{
    private readonly SpriteWorkflowManifestReader _manifestReader = new();
    private readonly SpriteWorkflowContactSheetGenerator _contactSheetGenerator = new();
    private SpriteWorkflowManifestSnapshot? _snapshot;
    private string _repoRoot = "";

    public SpriteWorkflowV2Window()
    {
        InitializeComponent();
    }

    public void LoadProject(string repoRoot)
    {
        _repoRoot = Path.GetFullPath(repoRoot);
        _snapshot = _manifestReader.Read(_repoRoot);
        ApplyFilter();
        StatusText.Text = $"{_snapshot.Rows.Count} rows loaded | read-only";
    }

    private void FilterTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void QueueListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (QueueListBox.SelectedItem is SpriteWorkflowQueueRowViewModel row)
        {
            RenderRow(row.Row);
        }
    }

    private void ApplyFilter()
    {
        if (_snapshot is null)
        {
            return;
        }

        var filter = FilterTextBox.Text.Trim();
        var rows = _snapshot.Rows
            .Where(row => string.IsNullOrWhiteSpace(filter) ||
                          row.RowId.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .Take(500)
            .Select(row => new SpriteWorkflowQueueRowViewModel(row))
            .ToList();
        QueueListBox.ItemsSource = rows;
        if (rows.Count > 0 && QueueListBox.SelectedIndex < 0)
        {
            QueueListBox.SelectedIndex = 0;
        }
    }

    private void RenderRow(SpriteWorkflowQueueRow row)
    {
        SelectedRowHeaderText.Text = row.RowId;
        SelectedRowSubheaderText.Text = "Read-only evidence view. Runtime/source/candidate/proof mutation is disabled.";
        FindingsTextBox.Text = string.Join(Environment.NewLine, row.Findings.Select(finding => "- " + finding));
        ProvenanceTextBox.Text = BuildProvenance(row);

        SourceStripImage.Source = GenerateSheetImage(row, SpriteWorkflowRootKind.AuthoredVerified) ??
                                  GenerateSheetImage(row, SpriteWorkflowRootKind.Authored);
        RuntimeStripImage.Source = GenerateSheetImage(row, SpriteWorkflowRootKind.Runtime);
    }

    private BitmapImage? GenerateSheetImage(SpriteWorkflowQueueRow row, SpriteWorkflowRootKind rootKind)
    {
        var slug = row.RowId.Replace('/', '-');
        var outputPath = Path.Combine(_repoRoot, "vnext", "artifacts", "sprite-workflow-v2-cache", $"{slug}-{rootKind.ToString().ToLowerInvariant()}.png");
        var result = _contactSheetGenerator.Generate(new SpriteWorkflowContactSheetRequest(row, rootKind, outputPath));
        if (!result.Succeeded || !File.Exists(outputPath))
        {
            return null;
        }

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(outputPath);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static string BuildProvenance(SpriteWorkflowQueueRow row)
    {
        var lines = new List<string>
        {
            $"row: {row.RowId}",
            $"species: {row.Key.Species}",
            $"age: {row.Key.AgeStage}",
            $"gender: {row.Key.Gender}",
            $"color: {row.Key.ColorVariant}",
            $"family: {row.Key.Family}",
            ""
        };

        foreach (var evidence in row.Evidence.OrderBy(item => item.RootKind))
        {
            lines.Add($"[{evidence.RootKind}] {evidence.Frames.Count} frame(s)");
            foreach (var frame in evidence.Frames.Take(12))
            {
                lines.Add($"  {frame.FrameId} {frame.Geometry.Width}x{frame.Geometry.Height} blake3={frame.Blake3}");
            }

            if (evidence.Frames.Count > 12)
            {
                lines.Add($"  ... {evidence.Frames.Count - 12} more");
            }

            lines.Add("");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private sealed class SpriteWorkflowQueueRowViewModel
    {
        public SpriteWorkflowQueueRowViewModel(SpriteWorkflowQueueRow row)
        {
            Row = row;
            DisplayName = $"{row.RowId}  ({row.Evidence.Sum(item => item.Frames.Count)} frames)";
        }

        public SpriteWorkflowQueueRow Row { get; }

        public string DisplayName { get; }
    }
}
