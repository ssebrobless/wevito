using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Win32;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Shell;

public partial class SpriteWorkflowV2Window : Window
{
    internal const int QueueDisplayLimit = 120;

    private readonly SpriteWorkflowManifestReader _manifestReader = new();
    private readonly SpriteWorkflowContactSheetGenerator _contactSheetGenerator = new();
    private readonly SpriteWorkflowCandidateImporter _candidateImporter = new();
    private readonly SpriteWorkflowDryRunApplyService _dryRunApplyService = new();
    private readonly SpriteWorkflowApplyService _applyService = new();
    private readonly SpriteWorkflowRollbackService _rollbackService = new();
    private SpriteWorkflowManifestSnapshot? _snapshot;
    private SpriteWorkflowQueueRow? _selectedRow;
    private SpriteWorkflowCandidateImportManifest? _lastCandidateImport;
    private SpriteWorkflowDryRunApplyManifest? _lastDryRunManifest;
    private SpriteWorkflowApplyManifest? _lastApplyManifest;
    private string _repoRoot = "";

    public SpriteWorkflowV2Window()
    {
        InitializeComponent();
        Loaded += (_, _) => WindowPlacementHelper.FitInsideWorkArea(this);
    }

    public void LoadProject(string repoRoot)
    {
        _repoRoot = Path.GetFullPath(repoRoot);
        _snapshot = _manifestReader.Read(_repoRoot);
        ApplyFilter();
        StatusText.Text = $"{_snapshot.Rows.Count} rows loaded | runtime read-only";
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

    private void ImportCandidateButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedRow is null)
        {
            DryRunPreviewTextBox.Text = "Select a row before importing candidate frames.";
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Select one candidate PNG from the candidate folder",
            Filter = "PNG frames (*.png)|*.png",
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var sourceFolder = Path.GetDirectoryName(dialog.FileName) ?? "";
        var result = _candidateImporter.Import(new SpriteWorkflowCandidateImportRequest(
            _repoRoot,
            _selectedRow.Key,
            sourceFolder,
            DateTimeOffset.UtcNow));
        DryRunPreviewTextBox.Text = result.Message;
        if (!result.Succeeded || result.Manifest is null)
        {
            return;
        }

        _lastCandidateImport = result.Manifest;
        DryRunApplyButton.IsEnabled = true;
        CandidateStripImage.Source = GenerateCandidateSheetImage(_selectedRow, result.Manifest);
        CandidatePlaceholderText.Text = "";
        ProofLaneText.Text = "Candidate imported. Next gate: dry-run apply must prove exact one-row mutation scope.";
        ProvenanceTextBox.Text = BuildProvenance(_selectedRow) +
                                 Environment.NewLine +
                                 $"[Candidate import]{Environment.NewLine}folder: {result.CandidateFolder}{Environment.NewLine}manifest: {result.ManifestPath}";
    }

    private void DryRunApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedRow is null || _lastCandidateImport is null)
        {
            DryRunPreviewTextBox.Text = "Import a candidate before planning a dry-run apply.";
            return;
        }

        var artifactRoot = Path.Combine(
            _repoRoot,
            "vnext",
            "artifacts",
            "sprite-workflow-v2-dryruns",
            $"{_selectedRow.RowId.Replace('/', '-')}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}");
        var result = _dryRunApplyService.Plan(new SpriteWorkflowDryRunApplyRequest(
            _repoRoot,
            _selectedRow.Key,
            _lastCandidateImport.CandidateFolder,
            artifactRoot,
            DateTimeOffset.UtcNow));
        _lastDryRunManifest = result.Manifest;
        DryRunPreviewTextBox.Text = result.Succeeded && result.Manifest is not null
            ? BuildDryRunPreview(result.Manifest, result.ManifestPath)
            : result.Message;
        ProofLaneText.Text = result.Succeeded && result.Manifest is not null && HasExactMutationScope(_selectedRow, result.Manifest)
            ? "Dry-run passed. Apply remains locked until the exact row id is typed."
            : "Dry-run did not prove exact mutation scope. Apply remains disabled.";
        RefreshApplyButtons();
    }

    private void ApplyConfirmationTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshApplyButtons();
    }

    private void ApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_lastDryRunManifest is null)
        {
            DryRunPreviewTextBox.Text = "Run a dry-run apply before applying.";
            return;
        }

        var result = _applyService.Apply(new SpriteWorkflowApplyRequest(_lastDryRunManifest, DateTimeOffset.UtcNow));
        DryRunPreviewTextBox.Text = result.Message + (result.Succeeded ? $"{Environment.NewLine}apply log: {result.ApplyLogPath}" : "");
        _lastApplyManifest = result.Manifest;
        RollbackButton.IsEnabled = result.Succeeded;
        ProofLaneText.Text = result.Succeeded
            ? "Applied one exact row with backup. Rollback is available."
            : "Apply failed. Runtime should remain protected by the service guards.";
        RefreshApplyButtons();
    }

    private void RollbackButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_lastApplyManifest is null)
        {
            DryRunPreviewTextBox.Text = "No apply manifest is available for rollback.";
            return;
        }

        var result = _rollbackService.Rollback(new SpriteWorkflowRollbackRequest(_lastApplyManifest, DateTimeOffset.UtcNow));
        DryRunPreviewTextBox.Text = result.Message + (result.Succeeded ? $"{Environment.NewLine}rollback log: {result.RollbackLogPath}" : "");
        if (result.Succeeded)
        {
            RollbackButton.IsEnabled = false;
            ProofLaneText.Text = "Rollback succeeded. Runtime row restored from backup.";
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
            .Take(QueueDisplayLimit)
            .Select(row => new SpriteWorkflowQueueRowViewModel(row))
            .ToList();
        QueueListBox.ItemsSource = rows;
        QueueSummaryText.Text = string.IsNullOrWhiteSpace(filter)
            ? $"Showing {rows.Count} of {_snapshot.Rows.Count} rows. Filter to narrow the queue."
            : $"Showing {rows.Count} filtered rows. Queue display is capped at {QueueDisplayLimit}.";
        if (rows.Count > 0 && QueueListBox.SelectedIndex < 0)
        {
            QueueListBox.SelectedIndex = 0;
        }
    }

    private void RenderRow(SpriteWorkflowQueueRow row)
    {
        _selectedRow = row;
        _lastCandidateImport = null;
        _lastDryRunManifest = null;
        _lastApplyManifest = null;
        SelectedRowHeaderText.Text = row.RowId;
        SelectedRowSubheaderText.Text = "Candidate import writes only to sprites_authored/.candidates. Dry-run apply writes a plan only.";
        SelectedTargetSummaryText.Text = BuildTargetSummary(row);
        FindingsTextBox.Text = string.Join(Environment.NewLine, row.Findings.Select(finding => "- " + finding));
        ProvenanceTextBox.Text = BuildProvenance(row);
        DryRunPreviewTextBox.Text = "Report only / no mutation yet. Import a candidate to begin the gated apply path.";
        DryRunApplyButton.IsEnabled = false;
        ApplyButton.IsEnabled = false;
        RollbackButton.IsEnabled = false;
        ApplyConfirmationTextBox.Text = "";
        CandidateStripImage.Source = null;
        CandidatePlaceholderText.Text = "No candidate imported yet";
        ProofLaneText.Text = "Report only / no mutation yet. Import candidate, dry-run, type exact row id, then apply one exact row.";

        SourceStripImage.Source = GenerateSheetImage(row, SpriteWorkflowRootKind.AuthoredVerified) ??
                                  GenerateSheetImage(row, SpriteWorkflowRootKind.Authored);
        RuntimeStripImage.Source = GenerateSheetImage(row, SpriteWorkflowRootKind.Runtime);
    }

    private void RefreshApplyButtons()
    {
        ApplyButton.IsEnabled = CanEnableApply(
            ApplyConfirmationTextBox.Text,
            _selectedRow,
            _lastCandidateImport,
            _lastDryRunManifest,
            _lastApplyManifest);
    }

    public static bool IsApplyConfirmationMatch(string typedText, string rowId)
    {
        return string.Equals(typedText.Trim(), rowId, StringComparison.Ordinal);
    }

    public static string BuildTargetSummary(SpriteWorkflowQueueRow row)
    {
        var expectedFrames = row.Evidence
            .Where(item => item.RootKind is SpriteWorkflowRootKind.Runtime or SpriteWorkflowRootKind.AuthoredVerified or SpriteWorkflowRootKind.Authored)
            .Select(item => item.Frames.Count)
            .DefaultIfEmpty(0)
            .Max();
        var status = row.Findings.Count == 0 ? "clean" : $"{row.Findings.Count} finding(s)";
        return $"{row.Key.Species} | {FormatAge(row.Key.AgeStage)} | {FormatGender(row.Key.Gender)} | {row.Key.ColorVariant} | {row.Key.Family} | {expectedFrames} expected frame(s) | {status}";
    }

    public static bool CanEnableApply(
        string typedText,
        SpriteWorkflowQueueRow? selectedRow,
        SpriteWorkflowCandidateImportManifest? candidateImport,
        SpriteWorkflowDryRunApplyManifest? dryRun,
        SpriteWorkflowApplyManifest? apply)
    {
        return selectedRow is not null &&
               candidateImport is not null &&
               dryRun is not null &&
               apply is null &&
               Equals(candidateImport.Target, selectedRow.Key) &&
               IsApplyConfirmationMatch(typedText, selectedRow.RowId) &&
               HasExactMutationScope(selectedRow, dryRun);
    }

    public static bool HasExactMutationScope(SpriteWorkflowQueueRow? selectedRow, SpriteWorkflowDryRunApplyManifest? dryRun)
    {
        if (selectedRow is null || dryRun is null || dryRun.Changes.Count == 0 || !dryRun.WouldMutateRuntime)
        {
            return false;
        }

        if (!Equals(selectedRow.Key, dryRun.Target))
        {
            return false;
        }

        var runtimeRowFolder = Path.GetFullPath(dryRun.RuntimeRowFolder);
        return dryRun.Changes.All(change =>
            change.WouldOverwriteRuntime &&
            !string.IsNullOrWhiteSpace(change.CurrentRuntimeBlake3) &&
            !string.IsNullOrWhiteSpace(change.CandidateBlake3) &&
            change.FrameId.StartsWith(selectedRow.Key.Family + "_", StringComparison.OrdinalIgnoreCase) &&
            IsPathUnderRoot(change.RuntimePath, runtimeRowFolder) &&
            IsPathUnderRoot(change.BackupPath, Path.GetFullPath(dryRun.PlannedBackupFolder)));
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

    private BitmapImage? GenerateCandidateSheetImage(SpriteWorkflowQueueRow selectedRow, SpriteWorkflowCandidateImportManifest manifest)
    {
        var candidateRow = selectedRow with
        {
            Evidence =
            [
                new SpriteWorkflowRowEvidence(
                    SpriteWorkflowRootKind.Candidate,
                    manifest.CandidateFolder,
                    manifest.ImportedFrames)
            ]
        };
        var outputPath = Path.Combine(
            _repoRoot,
            "vnext",
            "artifacts",
            "sprite-workflow-v2-cache",
            $"{selectedRow.RowId.Replace('/', '-')}-candidate-{manifest.ImportedAtUtc:yyyyMMdd-HHmmss}.png");
        var result = _contactSheetGenerator.Generate(new SpriteWorkflowContactSheetRequest(candidateRow, SpriteWorkflowRootKind.Candidate, outputPath));
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

    private static string BuildDryRunPreview(SpriteWorkflowDryRunApplyManifest manifest, string manifestPath)
    {
        var lines = new List<string>
        {
            $"manifest: {manifestPath}",
            $"candidate: {manifest.CandidateFolder}",
            $"runtime: {manifest.RuntimeRowFolder}",
            $"planned backup: {manifest.PlannedBackupFolder}",
            $"would mutate runtime: {manifest.WouldMutateRuntime}",
            "",
            "planned changes:"
        };
        lines.AddRange(manifest.Changes.Select(change =>
            $"- {change.FrameId}: overwrite={change.WouldOverwriteRuntime} runtime={Path.GetFileName(change.RuntimePath)} current={ShortHash(change.CurrentRuntimeBlake3)} candidate={ShortHash(change.CandidateBlake3)}"));
        return string.Join(Environment.NewLine, lines);
    }

    private static string ShortHash(string hash)
    {
        return string.IsNullOrWhiteSpace(hash) ? "missing" : hash[..Math.Min(10, hash.Length)];
    }

    private static string FormatAge(PetAgeStage ageStage)
    {
        return ageStage.ToString().ToLowerInvariant();
    }

    private static string FormatGender(PetGender gender)
    {
        return gender.ToString().ToLowerInvariant();
    }

    private static bool IsPathUnderRoot(string path, string root)
    {
        var fullPath = Path.GetFullPath(path);
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
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
