using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Wevito.VNext.Contracts;
using Wevito.VNext.Core;

namespace Wevito.VNext.Shell;

public sealed class BenchmarkCurationViewModel : INotifyPropertyChanged
{
    private readonly string _draftRoot;
    private readonly string _approvedRoot;
    private readonly BenchmarkCaseDraftService _draftService;
    private readonly BenchmarkCaseCurationStore _curationStore;
    private BenchmarkDraftRow? _selectedDraft;
    private string _editorPrompt = "";
    private string _editorExpectedText = "";
    private string _editorAxis = "chat";
    private string _statusText = "Benchmark curation queue ready.";
    private string _adversarialPrompt = "";
    private string _adversarialExpectedBehavior = "refuse / block / sanitize";

    public BenchmarkCurationViewModel(
        string draftRoot,
        string approvedRoot,
        BenchmarkCaseDraftService draftService,
        BenchmarkCaseCurationStore curationStore)
    {
        _draftRoot = Path.GetFullPath(draftRoot);
        _approvedRoot = Path.GetFullPath(approvedRoot);
        _draftService = draftService;
        _curationStore = curationStore;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<BenchmarkDraftRow> PendingDrafts { get; } = [];

    public BenchmarkDraftRow? SelectedDraft
    {
        get => _selectedDraft;
        set
        {
            if (SetField(ref _selectedDraft, value) && value is not null)
            {
                EditorAxis = value.Case.Axis;
                EditorPrompt = value.Case.Prompt;
                EditorExpectedText = value.Case.ExpectedText;
            }
        }
    }

    public string EditorPrompt
    {
        get => _editorPrompt;
        set => SetField(ref _editorPrompt, value);
    }

    public string EditorExpectedText
    {
        get => _editorExpectedText;
        set => SetField(ref _editorExpectedText, value);
    }

    public string EditorAxis
    {
        get => _editorAxis;
        set => SetField(ref _editorAxis, string.IsNullOrWhiteSpace(value) ? "chat" : value.Trim().ToLowerInvariant());
    }

    public string AdversarialPrompt
    {
        get => _adversarialPrompt;
        set => SetField(ref _adversarialPrompt, value);
    }

    public string AdversarialExpectedBehavior
    {
        get => _adversarialExpectedBehavior;
        set => SetField(ref _adversarialExpectedBehavior, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public void LoadPendingCases()
    {
        Directory.CreateDirectory(_draftRoot);
        Directory.CreateDirectory(_approvedRoot);
        PendingDrafts.Clear();
        foreach (var path in Directory.EnumerateFiles(_draftRoot, "*.json", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var testCase = ReadCase(path);
            if (testCase is null)
            {
                continue;
            }

            PendingDrafts.Add(new BenchmarkDraftRow(path, testCase));
        }

        SelectedDraft = PendingDrafts.FirstOrDefault();
        StatusText = PendingDrafts.Count == 0 ? "No pending benchmark drafts." : $"{PendingDrafts.Count} benchmark draft(s) pending review.";
    }

    public BenchmarkDraftCaseResult BookmarkFromChat(string assistantText, DateTimeOffset? nowUtc = null)
    {
        var result = _draftService.BookmarkFromChat(_draftRoot, _approvedRoot, assistantText, nowUtc);
        if (!result.Succeeded)
        {
            StatusText = result.Message;
            return result;
        }

        _curationStore.AppendPending(result.DraftPath, "chat-bookmark", "Assistant response bookmarked for benchmark review.", nowUtc);
        LoadPendingCases();
        StatusText = "Chat response saved as a benchmark draft.";
        return result;
    }

    public BenchmarkCaseReviewRecord ApproveSelected(string reviewer = "user")
    {
        if (SelectedDraft is null)
        {
            throw new InvalidOperationException("No benchmark draft is selected.");
        }

        ReviseSelected(reviewer, "Final edit before approval.");
        var selectedPath = SelectedDraft.Path;
        var record = _curationStore.Approve(selectedPath, _approvedRoot, reviewer, "Approved from curation UI.");
        LoadPendingCases();
        StatusText = "Benchmark draft approved and moved to approved cases.";
        return record;
    }

    public BenchmarkCaseReviewRecord RejectSelected(string reviewer = "user")
    {
        if (SelectedDraft is null)
        {
            throw new InvalidOperationException("No benchmark draft is selected.");
        }

        var record = _curationStore.Reject(SelectedDraft.Path, reviewer, "Rejected from curation UI.");
        LoadPendingCases();
        StatusText = "Benchmark draft rejected and removed.";
        return record;
    }

    public BenchmarkCaseReviewRecord ReviseSelected(string reviewer = "user", string notes = "Revised from curation UI.")
    {
        if (SelectedDraft is null)
        {
            throw new InvalidOperationException("No benchmark draft is selected.");
        }

        var existing = SelectedDraft.Case;
        var revised = existing with
        {
            Axis = EditorAxis,
            Prompt = EditorPrompt,
            ExpectedText = EditorExpectedText
        };
        var record = _curationStore.Revise(SelectedDraft.Path, revised, reviewer, notes);
        SelectedDraft = new BenchmarkDraftRow(SelectedDraft.Path, revised);
        StatusText = "Benchmark draft revised.";
        return record;
    }

    public BenchmarkApprovedCaseResult AddAdversarialCase(DateTimeOffset? nowUtc = null)
    {
        var result = _draftService.CreateAdversarialApprovedCase(_approvedRoot, AdversarialPrompt, AdversarialExpectedBehavior, nowUtc);
        if (!result.Succeeded)
        {
            StatusText = result.Message;
            return result;
        }

        _curationStore.RecordApproved(result.ApprovedPath, "user", "Human-authored adversarial benchmark case.", nowUtc);
        LoadPendingCases();
        StatusText = "Adversarial benchmark case approved directly from human input.";
        return result;
    }

    private static BenchmarkCase? ReadCase(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<BenchmarkCase>(File.ReadAllText(path), JsonDefaults.Options);
        }
        catch
        {
            return null;
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public sealed record BenchmarkDraftRow(string Path, BenchmarkCase Case)
{
    public string DisplayName => $"{Case.Axis} | {Case.Id}";

    public string Summary => $"{Case.Prompt} -> {Case.ExpectedText}";
}
