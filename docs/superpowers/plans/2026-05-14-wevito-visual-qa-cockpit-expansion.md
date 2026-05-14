# Wevito Visual QA Cockpit Expansion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expand the dev-only Wevito controller into a Visual QA Cockpit that can force pet states, inspect animations frame-by-frame, render visual debug overlays, batch-capture every pet/action combination, and record reviewed evidence for future local learning.

**Architecture:** Build on `docs/superpowers/plans/2026-05-14-wevito-dev-control-viewer.md`. The Wevito Shell remains the only state owner and exposes dev-only named-pipe commands; the Dev Controller displays snapshots and sends explicit commands. All audit artifacts are written as timestamped evidence packets under `vnext/artifacts/visual-qa-cockpit/`.

**Tech Stack:** .NET 8, WPF, existing Wevito VNext Shell, JSON named-pipe contracts, existing sprite/runtime content, existing tests under `vnext/tests/Wevito.VNext.Tests/`.

---

## Scope Map

```text
Base dependency
|
+-- Dev Control Viewer
|   +-- slot snapshots
|   +-- spawn/delete/replace
|   +-- selected-pet actions
|   `-- selected-pet roam
|
`-- Visual QA Cockpit Expansion
    +-- Animation Inspector
    +-- Visual Debug Overlays
    +-- Batch Matrix Runner
    +-- Screenshot/GIF Evidence Capture
    +-- Issue Tagging
    +-- Save Sandbox
    +-- Asset Source Inspector
    `-- Soak Observer
```

## Files

### Create

- `vnext/src/Wevito.VNext.Contracts/VisualQaContracts.cs`
- `vnext/src/Wevito.VNext.Shell/VisualQaCommandHandler.cs`
- `vnext/src/Wevito.VNext.Shell/VisualQaOverlayState.cs`
- `vnext/src/Wevito.VNext.Shell/VisualQaBatchRunner.cs`
- `vnext/src/Wevito.VNext.Shell/VisualQaCaptureService.cs`
- `vnext/src/Wevito.VNext.Shell/VisualQaIssueReportWriter.cs`
- `vnext/src/Wevito.VNext.DevController/ViewModels/AnimationInspectorViewModel.cs`
- `vnext/src/Wevito.VNext.DevController/ViewModels/BatchMatrixRunnerViewModel.cs`
- `vnext/src/Wevito.VNext.DevController/ViewModels/IssueTaggingViewModel.cs`
- `vnext/tests/Wevito.VNext.Tests/VisualQaContractsTests.cs`
- `vnext/tests/Wevito.VNext.Tests/VisualQaCommandHandlerTests.cs`
- `vnext/tests/Wevito.VNext.Tests/VisualQaBatchRunnerTests.cs`
- `vnext/tests/Wevito.VNext.Tests/VisualQaEvidenceWriterTests.cs`
- `docs/WEVITO_VISUAL_QA_COCKPIT_2026-05-14.md`

### Modify

- `vnext/src/Wevito.VNext.Contracts/DevControlContracts.cs`
- `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- `vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml`
- `vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml.cs`
- `vnext/src/Wevito.VNext.Shell/SpriteAssetService.cs`
- `vnext/src/Wevito.VNext.DevController/MainWindow.xaml`
- `vnext/src/Wevito.VNext.DevController/MainWindow.xaml.cs`
- `vnext/src/Wevito.VNext.DevController/DevControlClient.cs`
- `vnext/src/Wevito.VNext.DevController/DevControllerViewModel.cs`
- `vnext/tests/Wevito.VNext.Tests/SpriteAssetServiceTests.cs`
- `tools/build-vnext.ps1`

## Phase A: Visual QA Contracts

### Task A1: Add animation, overlay, capture, and issue contracts

**Files:**
- Create: `vnext/src/Wevito.VNext.Contracts/VisualQaContracts.cs`
- Test: `vnext/tests/Wevito.VNext.Tests/VisualQaContractsTests.cs`

- [ ] **Step 1: Write contract serialization tests**

```csharp
[Fact]
public void VisualQaCommandRoundTrips()
{
    var command = new VisualQaForceAnimationRequest(
        SlotIndex: 0,
        ExpectedPetId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
        AnimationFamily: "walk",
        FrameIndex: 2,
        PlaybackSpeed: 0.25,
        Loop: true);

    var json = JsonSerializer.Serialize(command, JsonDefaults.Options);
    var roundTrip = JsonSerializer.Deserialize<VisualQaForceAnimationRequest>(json, JsonDefaults.Options);

    Assert.Equal(command, roundTrip);
}

[Fact]
public void IssueTagRequestKeepsEvidenceScopeExplicit()
{
    var request = new VisualQaIssueTagRequest(
        SlotIndex: 1,
        ExpectedPetId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Tags: new[] { "cropped", "white_box" },
        Notes: "Crow head is flattened on frame 3.",
        AttachCurrentScreenshot: true);

    Assert.Equal(1, request.SlotIndex);
    Assert.Contains("cropped", request.Tags);
    Assert.True(request.AttachCurrentScreenshot);
}
```

- [ ] **Step 2: Add contract records**

```csharp
namespace Wevito.VNext.Contracts;

public static class VisualQaCommandTypes
{
    public const string ForceAnimation = "VisualQa.ForceAnimation";
    public const string ClearForcedAnimation = "VisualQa.ClearForcedAnimation";
    public const string SetOverlayOptions = "VisualQa.SetOverlayOptions";
    public const string RunBatchMatrix = "VisualQa.RunBatchMatrix";
    public const string CaptureEvidence = "VisualQa.CaptureEvidence";
    public const string TagIssue = "VisualQa.TagIssue";
    public const string ResetSaveSandbox = "VisualQa.ResetSaveSandbox";
    public const string GetAssetSource = "VisualQa.GetAssetSource";
    public const string GetSoakObserver = "VisualQa.GetSoakObserver";
}

public sealed record VisualQaForceAnimationRequest(
    int SlotIndex,
    Guid? ExpectedPetId,
    string AnimationFamily,
    int? FrameIndex,
    double PlaybackSpeed,
    bool Loop);

public sealed record VisualQaOverlayOptions(
    bool ShowSpriteBounds,
    bool ShowAlphaBounds,
    bool ShowAnchorPoints,
    bool ShowGroundLine,
    bool ShowTaskbarLine,
    bool ShowEnvironmentZones,
    bool ShowAssetSourceLabels);

public sealed record VisualQaRunBatchMatrixRequest(
    IReadOnlyList<string> SpeciesIds,
    IReadOnlyList<string> LifeStages,
    IReadOnlyList<string> Genders,
    IReadOnlyList<string> ColorVariants,
    IReadOnlyList<string> AnimationFamilies,
    bool CaptureContactSheets,
    bool CaptureScreenshots,
    bool IncludeActions);

public sealed record VisualQaCaptureEvidenceRequest(
    string CaptureKind,
    int? SlotIndex,
    Guid? ExpectedPetId,
    string Label);

public sealed record VisualQaIssueTagRequest(
    int SlotIndex,
    Guid? ExpectedPetId,
    IReadOnlyList<string> Tags,
    string Notes,
    bool AttachCurrentScreenshot);

public sealed record VisualQaAssetSourceSnapshot(
    int SlotIndex,
    Guid? PetId,
    string? SpeciesId,
    string? LifeStage,
    string? Gender,
    string? ColorVariant,
    string? AnimationFamily,
    string SourceKind,
    string? SourcePath,
    bool IsFallback,
    IReadOnlyList<string> Warnings);
```

- [ ] **Step 3: Run focused contract tests**

Run:

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "VisualQaContracts"
```

Expected: tests pass.

- [ ] **Step 4: Commit**

```powershell
git add .\vnext\src\Wevito.VNext.Contracts\VisualQaContracts.cs .\vnext\tests\Wevito.VNext.Tests\VisualQaContractsTests.cs
git commit -m "C-PHASE QA: add visual QA cockpit contracts"
```

## Phase B: Animation Inspector

### Task B1: Force animation family, frame index, speed, and loop mode

**Files:**
- Create: `vnext/src/Wevito.VNext.Shell/VisualQaCommandHandler.cs`
- Modify: `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- Create: `vnext/src/Wevito.VNext.DevController/ViewModels/AnimationInspectorViewModel.cs`
- Modify: `vnext/src/Wevito.VNext.DevController/MainWindow.xaml`
- Test: `vnext/tests/Wevito.VNext.Tests/VisualQaCommandHandlerTests.cs`

- [ ] **Step 1: Write tests for forced animation behavior**

```csharp
[Fact]
public async Task ForceAnimation_AppliesOnlySelectedPet()
{
    var harness = VisualQaShellHarness.CreateWithThreePets();
    var selected = harness.Pets[1];

    var response = await harness.Handler.ForceAnimationAsync(new VisualQaForceAnimationRequest(
        SlotIndex: 1,
        ExpectedPetId: selected.Id,
        AnimationFamily: "walk",
        FrameIndex: 2,
        PlaybackSpeed: 0.25,
        Loop: true));

    Assert.True(response.Success);
    Assert.Equal("walk", harness.GetForcedAnimation(selected.Id).AnimationFamily);
    Assert.Null(harness.GetForcedAnimation(harness.Pets[0].Id));
    Assert.Null(harness.GetForcedAnimation(harness.Pets[2].Id));
}

[Fact]
public async Task ForceAnimation_RejectsExpectedPetMismatch()
{
    var harness = VisualQaShellHarness.CreateWithThreePets();

    var response = await harness.Handler.ForceAnimationAsync(new VisualQaForceAnimationRequest(
        SlotIndex: 0,
        ExpectedPetId: Guid.Parse("99999999-9999-9999-9999-999999999999"),
        AnimationFamily: "idle",
        FrameIndex: 0,
        PlaybackSpeed: 1,
        Loop: true));

    Assert.False(response.Success);
    Assert.Contains("changed", response.Message, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 2: Add forced animation state**

```csharp
internal sealed record VisualQaForcedAnimation(
    string AnimationFamily,
    int? FrameIndex,
    double PlaybackSpeed,
    bool Loop,
    DateTimeOffset StartedAtUtc);
```

Store in ShellCoordinator or a focused helper:

```csharp
private readonly Dictionary<Guid, VisualQaForcedAnimation> _visualQaForcedAnimations = new();
```

- [ ] **Step 3: Apply forced animation during render snapshot creation**

When a pet has a forced animation:

```csharp
presentation.AnimationState = forced.AnimationFamily;
presentation.ForcedFrameIndex = forced.FrameIndex;
presentation.AnimationPlaybackSpeed = forced.PlaybackSpeed;
```

If the existing presentation model does not expose these fields, add explicit dev-only fields to the presentation model and keep normal gameplay paths unchanged.

- [ ] **Step 4: Add controller controls**

In `MainWindow.xaml`, add an `Animation Inspector` panel:

```xml
<GroupBox Header="Animation Inspector">
  <StackPanel>
    <ComboBox ItemsSource="{Binding AnimationFamilies}" SelectedItem="{Binding SelectedAnimationFamily}" />
    <Slider Minimum="0" Maximum="12" Value="{Binding SelectedFrameIndex}" />
    <ComboBox ItemsSource="{Binding PlaybackSpeeds}" SelectedItem="{Binding SelectedPlaybackSpeed}" />
    <CheckBox Content="Loop" IsChecked="{Binding LoopForcedAnimation}" />
    <Button Content="Force Animation" Command="{Binding ForceAnimationCommand}" />
    <Button Content="Clear" Command="{Binding ClearForcedAnimationCommand}" />
  </StackPanel>
</GroupBox>
```

- [ ] **Step 5: Run focused tests**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "VisualQaCommandHandler"
```

Expected: tests pass.

- [ ] **Step 6: Commit**

```powershell
git add .\vnext\src .\vnext\tests .\vnext\src\Wevito.VNext.DevController
git commit -m "C-PHASE QA: add visual animation inspector"
```

## Phase C: Visual Debug Overlays

### Task C1: Draw bounds, anchors, ground/taskbar lines, and zones

**Files:**
- Create: `vnext/src/Wevito.VNext.Shell/VisualQaOverlayState.cs`
- Modify: `vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml`
- Modify: `vnext/src/Wevito.VNext.Shell/HomePanelWindow.xaml.cs`
- Modify: `vnext/src/Wevito.VNext.DevController/MainWindow.xaml`
- Test: `vnext/tests/Wevito.VNext.Tests/VisualQaCommandHandlerTests.cs`

- [ ] **Step 1: Write overlay state tests**

```csharp
[Fact]
public async Task SetOverlayOptions_UpdatesShellOverlayState()
{
    var harness = VisualQaShellHarness.CreateWithThreePets();

    var options = new VisualQaOverlayOptions(
        ShowSpriteBounds: true,
        ShowAlphaBounds: true,
        ShowAnchorPoints: true,
        ShowGroundLine: true,
        ShowTaskbarLine: true,
        ShowEnvironmentZones: false,
        ShowAssetSourceLabels: true);

    var response = await harness.Handler.SetOverlayOptionsAsync(options);

    Assert.True(response.Success);
    Assert.True(harness.OverlayState.ShowSpriteBounds);
    Assert.True(harness.OverlayState.ShowTaskbarLine);
    Assert.False(harness.OverlayState.ShowEnvironmentZones);
}
```

- [ ] **Step 2: Add overlay state model**

```csharp
internal sealed class VisualQaOverlayState
{
    public bool ShowSpriteBounds { get; set; }
    public bool ShowAlphaBounds { get; set; }
    public bool ShowAnchorPoints { get; set; }
    public bool ShowGroundLine { get; set; }
    public bool ShowTaskbarLine { get; set; }
    public bool ShowEnvironmentZones { get; set; }
    public bool ShowAssetSourceLabels { get; set; }

    public void Apply(VisualQaOverlayOptions options)
    {
        ShowSpriteBounds = options.ShowSpriteBounds;
        ShowAlphaBounds = options.ShowAlphaBounds;
        ShowAnchorPoints = options.ShowAnchorPoints;
        ShowGroundLine = options.ShowGroundLine;
        ShowTaskbarLine = options.ShowTaskbarLine;
        ShowEnvironmentZones = options.ShowEnvironmentZones;
        ShowAssetSourceLabels = options.ShowAssetSourceLabels;
    }
}
```

- [ ] **Step 3: Render WPF overlay shapes**

Use transparent WPF shapes in `HomePanelWindow`:

```text
Red outline    = sprite render bounds
Yellow outline = non-transparent alpha bounds
Cyan dot       = anchor / feet contact
Green line     = home-stage ground line
Pink line      = taskbar/passive roam baseline
Blue outline   = interaction/environment zone
```

Implementation rule: overlays must not affect pet layout measurement. Place them on a top-level `Canvas` with `IsHitTestVisible="False"`.

- [ ] **Step 4: Add controller checkboxes**

Add checkboxes for each overlay option and send `VisualQa.SetOverlayOptions` on change.

- [ ] **Step 5: Run focused tests and a manual overlay launch**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "VisualQa"
```

Manual: launch Shell + Dev Controller, enable all overlays, confirm pets still move and click behavior is unchanged.

- [ ] **Step 6: Commit**

```powershell
git add .\vnext\src .\vnext\tests
git commit -m "C-PHASE QA: add visual debug overlays"
```

## Phase D: Batch Matrix Runner

### Task D1: Cycle through species, life stages, genders, colors, and animation families

**Files:**
- Create: `vnext/src/Wevito.VNext.Shell/VisualQaBatchRunner.cs`
- Create: `vnext/src/Wevito.VNext.DevController/ViewModels/BatchMatrixRunnerViewModel.cs`
- Modify: `vnext/src/Wevito.VNext.DevController/MainWindow.xaml`
- Test: `vnext/tests/Wevito.VNext.Tests/VisualQaBatchRunnerTests.cs`

- [ ] **Step 1: Write batch runner scope tests**

```csharp
[Fact]
public void BuildMatrix_SkipsUnavailableRuntimeCombinations()
{
    var catalog = VisualQaTestCatalog.WithAvailableRuntimeRows(new[]
    {
        "goose/baby/female/red/idle",
        "goose/baby/female/red/walk",
        "goose/baby/female/blue/idle"
    });

    var request = new VisualQaRunBatchMatrixRequest(
        SpeciesIds: new[] { "goose" },
        LifeStages: new[] { "baby" },
        Genders: new[] { "female" },
        ColorVariants: new[] { "red", "green", "blue" },
        AnimationFamilies: new[] { "idle", "walk" },
        CaptureContactSheets: true,
        CaptureScreenshots: true,
        IncludeActions: false);

    var rows = VisualQaBatchRunner.BuildMatrix(catalog, request);

    Assert.Contains(rows, row => row.ColorVariant == "red" && row.AnimationFamily == "idle");
    Assert.Contains(rows, row => row.ColorVariant == "red" && row.AnimationFamily == "walk");
    Assert.Contains(rows, row => row.ColorVariant == "blue" && row.AnimationFamily == "idle");
    Assert.DoesNotContain(rows, row => row.ColorVariant == "green");
}
```

- [ ] **Step 2: Implement matrix builder**

The matrix builder returns deterministic rows sorted by:

```text
species -> life stage -> gender -> color -> animation family
```

It must reject missing runtime rows and record skipped combinations in the report.

- [ ] **Step 3: Add runner execution**

For each matrix row:

1. Spawn or replace selected slot with requested pet.
2. Force requested animation.
3. Wait one render tick.
4. Capture screenshot/contact-sheet item.
5. Append result row with source paths and warnings.

- [ ] **Step 4: Add controller batch UI**

Include a compact panel:

```text
Batch Matrix
[x] Current species only
[x] Current age/gender/color only
[ ] All available runtime combinations
[x] Capture screenshots
[x] Capture contact sheet
Run Batch
```

Default must be current selected slot only, not all combinations.

- [ ] **Step 5: Run tests**

```powershell
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --filter "VisualQaBatchRunner"
```

- [ ] **Step 6: Commit**

```powershell
git add .\vnext\src .\vnext\tests
git commit -m "C-PHASE QA: add visual QA batch matrix runner"
```

## Phase E: Evidence Capture and Issue Tagging

### Task E1: Write timestamped visual evidence packets

**Files:**
- Create: `vnext/src/Wevito.VNext.Shell/VisualQaCaptureService.cs`
- Create: `vnext/src/Wevito.VNext.Shell/VisualQaIssueReportWriter.cs`
- Create: `vnext/src/Wevito.VNext.DevController/ViewModels/IssueTaggingViewModel.cs`
- Test: `vnext/tests/Wevito.VNext.Tests/VisualQaEvidenceWriterTests.cs`

- [ ] **Step 1: Write evidence packet tests**

```csharp
[Fact]
public void IssueReportWriter_WritesJsonAndMarkdown()
{
    using var temp = TestTempDirectory.Create();
    var writer = new VisualQaIssueReportWriter(temp.Path);

    var packet = writer.WriteIssue(new VisualQaIssueTagRequest(
        SlotIndex: 0,
        ExpectedPetId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
        Tags: new[] { "cropped", "bad_motion" },
        Notes: "Head crop appears during walk frame.",
        AttachCurrentScreenshot: false), VisualQaSnapshotFixtures.CrowBaby());

    Assert.True(File.Exists(Path.Combine(packet.PacketPath, "issue.json")));
    Assert.True(File.Exists(Path.Combine(packet.PacketPath, "issue.md")));
}
```

- [ ] **Step 2: Implement artifact folder convention**

All visual QA artifacts go under:

```text
vnext/artifacts/visual-qa-cockpit/yyyyMMdd-HHmmss-<slug>/
```

Each packet contains:

```text
manifest.json
issue.json
issue.md
screenshot.png      optional
contact-sheet.png   optional
```

- [ ] **Step 3: Add issue tags**

Initial tag list:

```text
white_box
cropped
blurry
wrong_scale
static_animation
bad_motion
wrong_baseline
duplicate_sprite
bad_opacity
missing_frame
wrong_asset_source
needs_redraw
```

- [ ] **Step 4: Wire controller issue panel**

Panel:

```text
Issue Tags
[ ] white box  [ ] cropped  [ ] blurry  [ ] wrong scale
[ ] static     [ ] bad motion [ ] wrong baseline
Notes: [........................................]
[Capture + Tag Issue]
```

- [ ] **Step 5: Commit**

```powershell
git add .\vnext\src .\vnext\tests
git commit -m "C-PHASE QA: add visual QA evidence capture and issue tags"
```

## Phase F: Save Sandbox and New-User Flow

### Task F1: Add dev-only save reset and controlled seed modes

**Files:**
- Modify: `vnext/src/Wevito.VNext.Shell/ShellCoordinator.cs`
- Modify: `vnext/src/Wevito.VNext.DevController/MainWindow.xaml`
- Test: `vnext/tests/Wevito.VNext.Tests/VisualQaCommandHandlerTests.cs`

- [ ] **Step 1: Write save sandbox tests**

```csharp
[Fact]
public async Task ResetSaveSandbox_ClearsPetsAndShowsEggChoice()
{
    var harness = VisualQaShellHarness.CreateWithThreePets();

    var response = await harness.Handler.ResetSaveSandboxAsync(new VisualQaResetSaveSandboxRequest(
        Mode: "fresh_start_egg_choice"));

    Assert.True(response.Success);
    Assert.Empty(harness.State.ActivePets);
    Assert.True(harness.State.ShouldShowEggChoice);
}
```

- [ ] **Step 2: Add reset modes**

Modes:

```text
fresh_start_egg_choice
empty_three_slots
three_random_alive_pets
three_specific_test_pets
```

Rules:

- Dev-only.
- Requires confirmation from controller.
- Writes normal Wevito save through existing persistence path.
- Does not delete sprite assets or artifact folders.

- [ ] **Step 3: Add controller buttons**

Buttons:

```text
Reset to Egg Choice
Empty Save
Seed 3 Test Pets
```

Each button must show a confirmation dialog before sending command.

- [ ] **Step 4: Commit**

```powershell
git add .\vnext\src .\vnext\tests
git commit -m "C-PHASE QA: add dev save sandbox controls"
```

## Phase G: Asset Source Inspector

### Task G1: Show where the visible pet frame came from

**Files:**
- Modify: `vnext/src/Wevito.VNext.Shell/SpriteAssetService.cs`
- Modify: `vnext/src/Wevito.VNext.Shell/VisualQaCommandHandler.cs`
- Test: `vnext/tests/Wevito.VNext.Tests/SpriteAssetServiceTests.cs`

- [ ] **Step 1: Write asset-source tests**

```csharp
[Fact]
public void ResolveFrame_ReturnsRuntimeSourceWhenRuntimeFrameExists()
{
    var service = SpriteAssetServiceTestHarness.Create();

    var source = service.ResolveFrameSource(
        speciesId: "goose",
        lifeStage: "baby",
        gender: "female",
        colorVariant: "blue",
        animationFamily: "walk",
        frameIndex: 0);

    Assert.Equal("runtime", source.SourceKind);
    Assert.False(source.IsFallback);
    Assert.EndsWith("sprites_runtime\\goose\\baby\\female\\blue\\walk_00.png", source.SourcePath);
}
```

- [ ] **Step 2: Add source metadata return type**

```csharp
public sealed record SpriteFrameSourceInfo(
    string SourceKind,
    string? SourcePath,
    bool IsFallback,
    IReadOnlyList<string> Warnings);
```

- [ ] **Step 3: Display in controller**

For the selected slot, show:

```text
Asset source: runtime
Frame: sprites_runtime/goose/baby/female/blue/walk_00.png
Warnings: none
```

- [ ] **Step 4: Commit**

```powershell
git add .\vnext\src .\vnext\tests
git commit -m "C-PHASE QA: add runtime asset source inspector"
```

## Phase H: Soak Observer Panel

### Task H1: Make autonomous observation visible without enabling mutation

**Files:**
- Modify: `vnext/src/Wevito.VNext.Shell/VisualQaCommandHandler.cs`
- Modify: `vnext/src/Wevito.VNext.DevController/MainWindow.xaml`
- Test: `vnext/tests/Wevito.VNext.Tests/VisualQaCommandHandlerTests.cs`

- [ ] **Step 1: Write observer tests**

```csharp
[Fact]
public async Task SoakObserver_ReturnsReadOnlyStatus()
{
    var harness = VisualQaShellHarness.CreateWithSoakStatus(day: 2, flaggedRows: 3);

    var response = await harness.Handler.GetSoakObserverAsync();

    Assert.True(response.Success);
    Assert.Contains("day 2", response.Message, StringComparison.OrdinalIgnoreCase);
    Assert.Equal(3, response.Snapshot.SoakStatus.FlaggedRows);
}
```

- [ ] **Step 2: Add read-only observer fields**

Expose:

```text
soak active/inactive
day number
latest heartbeat time
previews created
approvals created
mutations performed
flagged visual rows
latest artifact path
```

- [ ] **Step 3: Add controller panel**

Panel:

```text
Soak Observer
Status: active, day 1/7
Previews: 0
Approvals: 0
Mutations: 0
Flagged rows: 0
Latest evidence: [Open Folder]
```

This panel is read-only. It must not enable autonomous mutation.

- [ ] **Step 4: Commit**

```powershell
git add .\vnext\src .\vnext\tests
git commit -m "C-PHASE QA: add read-only soak observer panel"
```

## Phase I: Final Integration and Validation

### Task I1: Build, publish, and manually prove the cockpit

**Files:**
- Modify: `tools/build-vnext.ps1`
- Create: `docs/WEVITO_VISUAL_QA_COCKPIT_2026-05-14.md`

- [ ] **Step 1: Ensure build publishes both apps**

Expected outputs:

```text
vnext/artifacts/shell/Wevito.VNext.Shell.exe
vnext/artifacts/dev-controller/Wevito.VNext.DevController.exe
```

- [ ] **Step 2: Run full validation**

```powershell
dotnet build .\vnext\Wevito.VNext.sln
dotnet test .\vnext\tests\Wevito.VNext.Tests\Wevito.VNext.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-vnext.ps1 -Configuration Debug -SkipAssetPrep -SkipTests
```

Expected:

```text
Build succeeded.
All tests pass.
Build script completes without asset prep.
```

- [ ] **Step 3: Manual proof**

```powershell
Start-Process .\vnext\artifacts\shell\Wevito.VNext.Shell.exe
Start-Process .\vnext\artifacts\dev-controller\Wevito.VNext.DevController.exe
```

Manual proof checklist:

```text
[ ] Controller connects to running Shell.
[ ] Three pet slots match visible Wevito pets.
[ ] Empty slots show N/A.
[ ] Spawn/replace works with confirmation.
[ ] Delete removes pet without ghost.
[ ] Force animation shows selected animation only.
[ ] Frame scrubber pauses on a specific frame.
[ ] Overlay bounds appear without blocking clicks.
[ ] Batch current-pet capture writes artifact folder.
[ ] Issue tagging writes JSON and markdown.
[ ] Save reset returns to egg choice.
[ ] Asset source inspector shows runtime/fallback source.
[ ] Soak observer is read-only.
```

- [ ] **Step 4: Write report**

Create `docs/WEVITO_VISUAL_QA_COCKPIT_2026-05-14.md` with:

```text
Goal
Implemented tools
Safety boundaries
Validation commands
Manual proof results
Known limitations
Next recommended visual QA pass
```

- [ ] **Step 5: Commit**

```powershell
git add .\tools\build-vnext.ps1 .\docs\WEVITO_VISUAL_QA_COCKPIT_2026-05-14.md
git commit -m "C-PHASE QA: document visual QA cockpit proof"
```

## Stop Conditions

Stop and ask if any of these happen:

- The base dev-control pipe is not implemented yet.
- The controller would need direct save-file or database writes.
- A feature requires mutating sprite PNGs as part of inspection.
- GIF/video capture requires a new external binary dependency.
- Overlay rendering changes normal pet layout.
- Batch runner cannot be limited to current selection by default.
- Asset source inspector shows fallback art for many runtime combinations; this should become a separate sprite-source recovery phase.

## Recommended Build Order

```text
1. Build base Dev Control Viewer.
2. Add Animation Inspector.
3. Add Visual Debug Overlays.
4. Add Asset Source Inspector.
5. Add Evidence Capture + Issue Tags.
6. Add Batch Matrix Runner.
7. Add Save Sandbox.
8. Add Soak Observer.
9. Publish and run manual visual proof.
```

## Why This Order

Animation inspection and overlays give immediate value for the bugs currently visible: low-FPS-looking animation, bad crop, wrong baseline, invisible pets, and stale/fallback art. Batch mode becomes safer after the inspector and source overlay exist, because the batch results can explain what was actually rendered. Save sandbox comes after the core inspection tools so fresh-start testing does not distract from frame-level sprite QA.

