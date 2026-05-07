param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$SkipBuild,
    [ValidateSet("", "localDocs", "spriteAudit", "assetInventory", "petState", "codeReview", "codePatchPlan", "buildProof", "translateText", "audioAssist", "screenCapture")]
    [string]$TaskKind = "",
    [string]$TaskText = "review goose baby female blue sprites",
    [string]$ExpectedToolFamily = "spriteAudit",
    [switch]$ApproveBeforePreview,
    [switch]$ExpectExecuteEnabledAfterPreview,
    [string]$ExpectArtifactFileCreated = "",
    [switch]$SkipSpriteHashCheck,
    [string]$LayoutScreenshotPath = "",
    [int]$StartupDelayMs = 2500
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ShellBinRoot = Join-Path $ProjectRoot "vnext\src\Wevito.VNext.Shell\bin\$Configuration"
$BuildShellExeCandidates = @(
    (Join-Path $ShellBinRoot "net8.0-windows10.0.19041.0\Wevito.VNext.Shell.exe"),
    (Join-Path $ShellBinRoot "net8.0-windows\Wevito.VNext.Shell.exe")
)
$BuildShellExe = $BuildShellExeCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
$OutputRoot = Join-Path $ProjectRoot "vnext\artifacts\pet-task-probes"
$RunStamp = "$(Get-Date -Format "yyyyMMdd-HHmmss-fff")-$([Guid]::NewGuid().ToString("N").Substring(0, 8))"
$OutputDir = Join-Path $OutputRoot $RunStamp
$DataDir = Join-Path $OutputDir "data"
$TraceDir = Join-Path $OutputDir "trace"
$TargetRow = Join-Path $ProjectRoot "sprites_runtime\goose\baby\female\blue"
$BuildTargetRow = Join-Path (Split-Path -Parent $BuildShellExe) "sprites_runtime\goose\baby\female\blue"

if (-not [string]::IsNullOrWhiteSpace($TaskKind)) {
    $ExpectedToolFamily = $TaskKind
    $TaskText = switch ($TaskKind) {
        "localDocs" { "summarize the local docs" }
        "spriteAudit" { "review goose baby female blue sprites" }
        "assetInventory" { "inventory assets in sprites_runtime" }
        "petState" { "review pet state" }
        "codeReview" { "review the code in Wevito.VNext.Core" }
        "codePatchPlan" { "plan a code fix in vnext" }
        "buildProof" { "run a build proof" }
        "translateText" { "translate Hello goose to Spanish" }
        "audioAssist" { "boost my PC volume" }
        "screenCapture" { "screenshot the Wevito window" }
        default { $TaskText }
    }
}

if (-not $SkipBuild) {
    dotnet build (Join-Path $ProjectRoot "vnext\src\Wevito.VNext.Shell\Wevito.VNext.Shell.csproj") --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "vNext Shell build failed with exit code $LASTEXITCODE"
    }
}

if ([string]::IsNullOrWhiteSpace($BuildShellExe) -or -not (Test-Path $BuildShellExe)) {
    throw "Missing shell executable: $BuildShellExe"
}

if (-not $SkipSpriteHashCheck -and -not (Test-Path $TargetRow)) {
    throw "Missing PET TASKS probe target row: $TargetRow"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
New-Item -ItemType Directory -Force -Path $DataDir | Out-Null
New-Item -ItemType Directory -Force -Path $TraceDir | Out-Null

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
if (-not [string]::IsNullOrWhiteSpace($LayoutScreenshotPath)) {
    Add-Type -AssemblyName System.Drawing
}

Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class WevitoPetTasksProbeNative
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextW(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLengthW(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int command);

    public sealed class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public int ProcessId { get; set; }
        public string Title { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static List<WindowInfo> GetWindowsForProcess(int processId)
    {
        var windows = new List<WindowInfo>();
        EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
        {
            if (!IsWindowVisible(hWnd))
            {
                return true;
            }

            uint pid;
            GetWindowThreadProcessId(hWnd, out pid);
            if (pid != processId)
            {
                return true;
            }

            var length = GetWindowTextLengthW(hWnd);
            var builder = new StringBuilder(length + 1);
            GetWindowTextW(hWnd, builder, builder.Capacity);
            if (builder.Length == 0)
            {
                return true;
            }

            RECT rect;
            if (!GetWindowRect(hWnd, out rect))
            {
                return true;
            }

            windows.Add(new WindowInfo
            {
                Handle = hWnd,
                ProcessId = processId,
                Title = builder.ToString(),
                Left = rect.Left,
                Top = rect.Top,
                Width = Math.Max(0, rect.Right - rect.Left),
                Height = Math.Max(0, rect.Bottom - rect.Top)
            });

            return true;
        }, IntPtr.Zero);

        return windows;
    }
}
"@

function Get-TargetRowHashes {
    param([string]$RowPath)

    Get-ChildItem -Path $RowPath -Filter "*.png" |
        Sort-Object FullName |
        ForEach-Object {
            [ordered]@{
                path = $_.FullName
                hash = (Get-FileHash -Algorithm SHA256 -Path $_.FullName).Hash.ToLowerInvariant()
            }
        }
}

function Start-ShellProcess {
    param([string]$ScenarioPath = "")

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $BuildShellExe
    $psi.WorkingDirectory = Split-Path -Parent $BuildShellExe
    $psi.UseShellExecute = $false
    $psi.Environment["WEVITO_VNEXT_DATA_ROOT"] = $DataDir
    $psi.Environment["WEVITO_VNEXT_TRACE_DIR"] = $TraceDir
    if (-not [string]::IsNullOrWhiteSpace($ScenarioPath)) {
        $psi.Environment["WEVITO_VNEXT_SCENARIO_PATH"] = $ScenarioPath
    }
    return [System.Diagnostics.Process]::Start($psi)
}

function New-ProbeScenario {
    $scenarioPath = Join-Path $OutputDir "scenario.json"
    $scenario = [ordered]@{
        mode = "Pinned"
        clearBasket = $true
        settingsSnapshot = [ordered]@{
            compact_hud = "False"
            show_pet_names = "False"
            show_status_summary = "True"
            webtools_visible = "False"
        }
    }
    $scenario | ConvertTo-Json -Depth 4 | Set-Content -Path $scenarioPath -Encoding UTF8
    return $scenarioPath
}

function Get-WindowInfo {
    param(
        [int]$ProcessId,
        [string]$Title = "",
        [int]$TimeoutMs = 10000
    )

    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    while ((Get-Date) -lt $deadline) {
        $windows = [WevitoPetTasksProbeNative]::GetWindowsForProcess($ProcessId)
        $match = if ([string]::IsNullOrWhiteSpace($Title)) {
            $windows | Select-Object -First 1
        }
        else {
            $windows | Where-Object { $_.Title -eq $Title } | Select-Object -First 1
        }

        if ($null -ne $match) {
            return $match
        }

        Start-Sleep -Milliseconds 200
    }

    throw "Timed out waiting for window '$Title' for process $ProcessId."
}

function Get-ToolWindowInfo {
    param(
        [int]$ProcessId,
        [string]$Title,
        [int]$TimeoutMs = 7000
    )

    return Get-WindowInfo -ProcessId $ProcessId -Title $Title -TimeoutMs $TimeoutMs
}

function Set-WindowForeground {
    param($WindowInfo)

    $shell = New-Object -ComObject WScript.Shell
    for ($attempt = 0; $attempt -lt 12; $attempt++) {
        [void][WevitoPetTasksProbeNative]::ShowWindow($WindowInfo.Handle, 5)
        [void][WevitoPetTasksProbeNative]::SetForegroundWindow($WindowInfo.Handle)
        try { [void]$shell.AppActivate($WindowInfo.Title) } catch { }
        try { [void]$shell.AppActivate($WindowInfo.ProcessId) } catch { }
        Start-Sleep -Milliseconds 250
    }
}

function Save-WindowScreenshot {
    param(
        $WindowInfo,
        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $parent = Split-Path -Parent $fullPath
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    $bitmap = [System.Drawing.Bitmap]::new($WindowInfo.Width, $WindowInfo.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen(
            $WindowInfo.Left,
            $WindowInfo.Top,
            0,
            0,
            [System.Drawing.Size]::new($WindowInfo.Width, $WindowInfo.Height))
        $bitmap.Save($fullPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Get-AllTargetHashes {
    $rows = @($TargetRow)
    if ((Test-Path $BuildTargetRow) -and -not [string]::Equals($BuildTargetRow, $TargetRow, [System.StringComparison]::OrdinalIgnoreCase)) {
        $rows += $BuildTargetRow
    }

    foreach ($row in $rows) {
        [ordered]@{
            row = $row
            files = @(Get-TargetRowHashes -RowPath $row)
        }
    }
}

function Get-AutomationElement {
    param(
        [IntPtr]$Handle,
        [string]$AutomationId,
        [System.Windows.Automation.ControlType]$ControlType = $null,
        [int]$TimeoutMs = 4000
    )

    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    while ((Get-Date) -lt $deadline) {
        $root = [System.Windows.Automation.AutomationElement]::FromHandle($Handle)
        if ($null -ne $root) {
            $conditions = [System.Collections.Generic.List[System.Windows.Automation.Condition]]::new()
            $conditions.Add([System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $AutomationId))
            if ($null -ne $ControlType) {
                $conditions.Add([System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty, $ControlType))
            }
            $condition = if ($conditions.Count -eq 1) { $conditions[0] } else { [System.Windows.Automation.AndCondition]::new($conditions.ToArray()) }
            $match = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
            if ($null -ne $match) {
                return $match
            }
        }

        Start-Sleep -Milliseconds 150
    }

    return $null
}

function Invoke-Button {
    param(
        $WindowInfo,
        [string]$AutomationId
    )

    $button = Get-AutomationElement -Handle $WindowInfo.Handle -AutomationId $AutomationId -ControlType ([System.Windows.Automation.ControlType]::Button)
    if ($null -eq $button) {
        throw "Missing button '$AutomationId'."
    }
    if (-not $button.Current.IsEnabled) {
        throw "Button '$AutomationId' is disabled."
    }

    $invoke = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $invoke.Invoke()
    Start-Sleep -Milliseconds 900
}

function Set-TextBoxValue {
    param(
        $WindowInfo,
        [string]$AutomationId,
        [string]$Value
    )

    $textBox = Get-AutomationElement -Handle $WindowInfo.Handle -AutomationId $AutomationId -ControlType ([System.Windows.Automation.ControlType]::Edit)
    if ($null -eq $textBox) {
        throw "Missing textbox '$AutomationId'."
    }

    $textBox.SetFocus()
    Start-Sleep -Milliseconds 250
    Set-Clipboard -Value $Value
    $shell = New-Object -ComObject WScript.Shell
    $shell.SendKeys("^a")
    Start-Sleep -Milliseconds 150
    $shell.SendKeys("^v")
    Start-Sleep -Milliseconds 400

    try {
        $pattern = $textBox.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
        if (-not [string]::Equals($pattern.Current.Value, $Value, [System.StringComparison]::Ordinal)) {
            $pattern.SetValue($Value)
        }
    }
    catch {
    }
    Start-Sleep -Milliseconds 500
}

function Wait-ForTraceText {
    param(
        [string]$TracePath,
        [string]$Pattern,
        [int]$TimeoutMs = 9000
    )

    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $TracePath) {
            $match = Select-String -Path $TracePath -Pattern $Pattern -ErrorAction SilentlyContinue
            if ($null -ne $match) {
                return $match | Select-Object -Last 1
            }
        }

        Start-Sleep -Milliseconds 250
    }

    return $null
}

$beforeHashes = if ($SkipSpriteHashCheck) { @() } else { @(Get-AllTargetHashes) }
$shellProcess = $null

try {
    $scenarioPath = New-ProbeScenario
    $shellProcess = Start-ShellProcess -ScenarioPath $scenarioPath
    Start-Sleep -Milliseconds $StartupDelayMs

    $homeWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito Home Panel"
    Set-WindowForeground -WindowInfo $homeWindow

    Invoke-Button -WindowInfo $homeWindow -AutomationId "WebToolsButton"
    Invoke-Button -WindowInfo $homeWindow -AutomationId "HelperTabButton"

    $toolWindow = Get-ToolWindowInfo -ProcessId $shellProcess.Id -Title "Wevito PET TASKS"
    Set-WindowForeground -WindowInfo $toolWindow
    Save-WindowScreenshot -WindowInfo $toolWindow -Path $LayoutScreenshotPath
    $reportOnlyElement = Get-AutomationElement -Handle $toolWindow.Handle -AutomationId "PetTaskReportOnlyBadge" -ControlType ([System.Windows.Automation.ControlType]::Text)
    if ($null -eq $reportOnlyElement -or $reportOnlyElement.Current.Name.IndexOf("REPORT ONLY", [System.StringComparison]::Ordinal) -lt 0) {
        throw "Missing PET TASKS REPORT ONLY badge."
    }

    $wellbeingElement = Get-AutomationElement -Handle $toolWindow.Handle -AutomationId "PetWellbeingSnapshotText" -ControlType ([System.Windows.Automation.ControlType]::Text)
    if ($null -eq $wellbeingElement) {
        throw "Missing PET TASKS wellbeing snapshot text."
    }
    $wellbeingText = $wellbeingElement.Current.Name
    if ([string]::IsNullOrWhiteSpace($wellbeingText) -or -not $wellbeingText.StartsWith("Wellbeing:", [System.StringComparison]::Ordinal)) {
        throw "PET TASKS wellbeing snapshot text was not populated. text='$wellbeingText'"
    }

    $capabilityElement = Get-AutomationElement -Handle $toolWindow.Handle -AutomationId "PetTaskCapabilityText" -ControlType ([System.Windows.Automation.ControlType]::Text)
    if ($null -eq $capabilityElement) {
        throw "Missing PET TASKS capability text."
    }
    $capabilityText = $capabilityElement.Current.Name
    if ([string]::IsNullOrWhiteSpace($capabilityText) -or
        $capabilityText.IndexOf("spriteAudit", [System.StringComparison]::Ordinal) -lt 0 -or
        $capabilityText.IndexOf("assetInventory", [System.StringComparison]::Ordinal) -lt 0 -or
        $capabilityText.IndexOf("codeReview", [System.StringComparison]::Ordinal) -lt 0 -or
        $capabilityText.IndexOf("codePatchPlan", [System.StringComparison]::Ordinal) -lt 0 -or
        $capabilityText.IndexOf("buildProof", [System.StringComparison]::Ordinal) -lt 0 -or
        $capabilityText.IndexOf("translateText", [System.StringComparison]::Ordinal) -lt 0 -or
        $capabilityText.IndexOf("audioAssist", [System.StringComparison]::Ordinal) -lt 0 -or
        $capabilityText.IndexOf("screenCapture", [System.StringComparison]::Ordinal) -lt 0 -or
        $capabilityText.IndexOf("petState", [System.StringComparison]::Ordinal) -lt 0 -or
        $capabilityText.IndexOf("localDocs", [System.StringComparison]::Ordinal) -lt 0 -or
        $capabilityText.IndexOf("Locked:", [System.StringComparison]::Ordinal) -lt 0) {
        throw "PET TASKS capability text did not describe enabled previews and locked actions. text='$capabilityText'"
    }

    $nextActionElement = Get-AutomationElement -Handle $toolWindow.Handle -AutomationId "PetTaskNextActionText" -ControlType ([System.Windows.Automation.ControlType]::Text)
    if ($null -eq $nextActionElement) {
        throw "Missing PET TASKS next-action text."
    }
    $nextActionText = $nextActionElement.Current.Name
    if ([string]::IsNullOrWhiteSpace($nextActionText) -or
        $nextActionText.IndexOf("Next:", [System.StringComparison]::Ordinal) -lt 0) {
        throw "PET TASKS next-action text was not populated. text='$nextActionText'"
    }

    $resultPathElement = Get-AutomationElement -Handle $toolWindow.Handle -AutomationId "PetTaskResultPathText" -ControlType ([System.Windows.Automation.ControlType]::Text)
    if ($null -eq $resultPathElement) {
        throw "Missing PET TASKS result path text."
    }
    $resultPathText = $resultPathElement.Current.Name
    if ([string]::IsNullOrWhiteSpace($resultPathText) -or
        ($resultPathText.IndexOf("Result:", [System.StringComparison]::Ordinal) -lt 0 -and
         $resultPathText.IndexOf("Report:", [System.StringComparison]::Ordinal) -lt 0)) {
        throw "PET TASKS result path text was not populated. text='$resultPathText'"
    }

    foreach ($artifactButtonId in @("PetTaskOpenReportButton", "PetTaskCopyPathButton", "PetTaskOpenFolderButton")) {
        $artifactButton = Get-AutomationElement -Handle $toolWindow.Handle -AutomationId $artifactButtonId -ControlType ([System.Windows.Automation.ControlType]::Button)
        if ($null -eq $artifactButton) {
            throw "Missing PET TASKS artifact button '$artifactButtonId'."
        }
    }

    Set-TextBoxValue -WindowInfo $toolWindow -AutomationId "PetCommandTextBox" -Value $TaskText
    Invoke-Button -WindowInfo $toolWindow -AutomationId "PetCommandSubmitButton"
    if ($ApproveBeforePreview) {
        Invoke-Button -WindowInfo $toolWindow -AutomationId "PetTaskApproveButton"
    }
    Invoke-Button -WindowInfo $toolWindow -AutomationId "PetTaskPreviewButton"

    $postPreviewNextActionElement = Get-AutomationElement -Handle $toolWindow.Handle -AutomationId "PetTaskNextActionText" -ControlType ([System.Windows.Automation.ControlType]::Text)
    if ($null -eq $postPreviewNextActionElement) {
        throw "Missing PET TASKS post-preview next-action text."
    }
    $postPreviewNextActionText = $postPreviewNextActionElement.Current.Name
    if ([string]::IsNullOrWhiteSpace($postPreviewNextActionText) -or
        $postPreviewNextActionText.IndexOf("Next:", [System.StringComparison]::Ordinal) -lt 0) {
        throw "PET TASKS post-preview next-action text was not populated. text='$postPreviewNextActionText'"
    }

    $postPreviewResultPathElement = Get-AutomationElement -Handle $toolWindow.Handle -AutomationId "PetTaskResultPathText" -ControlType ([System.Windows.Automation.ControlType]::Text)
    if ($null -eq $postPreviewResultPathElement) {
        throw "Missing PET TASKS post-preview result path text."
    }
    $postPreviewResultPathText = $postPreviewResultPathElement.Current.Name
    if ([string]::IsNullOrWhiteSpace($postPreviewResultPathText) -or
        ($postPreviewResultPathText.IndexOf("Result:", [System.StringComparison]::Ordinal) -lt 0 -and
         $postPreviewResultPathText.IndexOf("Report:", [System.StringComparison]::Ordinal) -lt 0)) {
        throw "PET TASKS post-preview result path text was not populated. text='$postPreviewResultPathText'"
    }

    $executeEnabledAfterPreview = $null
    $executionAuditPath = ""
    $expectedArtifactPath = ""
    $shellTracePath = Join-Path $TraceDir "Wevito.VNext.Shell.trace.log"
    $escapedFamily = [System.Text.RegularExpressions.Regex]::Escape($ExpectedToolFamily)
    if ($ExpectExecuteEnabledAfterPreview) {
        $executeButton = Get-AutomationElement -Handle $toolWindow.Handle -AutomationId "PetTaskExecuteButton" -ControlType ([System.Windows.Automation.ControlType]::Button)
        if ($null -eq $executeButton) {
            throw "Missing PET TASKS execute button."
        }
        $executeEnabledAfterPreview = [bool]$executeButton.Current.IsEnabled
        if (-not $executeEnabledAfterPreview) {
            throw "PET TASKS execute button was not enabled after preview."
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($ExpectArtifactFileCreated)) {
        Invoke-Button -WindowInfo $toolWindow -AutomationId "PetTaskExecuteButton"
        $executeTrace = Wait-ForTraceText -TracePath $shellTracePath -Pattern "pet-command \| .*execute .*family=$escapedFamily.*status=Completed" -TimeoutMs 60000
        if ($null -eq $executeTrace) {
            throw "PET TASKS execute trace was not observed."
        }

        if ($executeTrace.Line -match "audit=(?<audit>.+)$") {
            $executionAuditPath = $Matches.audit.Trim()
        }
        if ([string]::IsNullOrWhiteSpace($executionAuditPath) -or -not (Test-Path $executionAuditPath)) {
            throw "PET TASKS execution did not produce a readable audit path. trace='$($executeTrace.Line)'"
        }

        $executionArtifactRoot = Split-Path -Parent $executionAuditPath
        $expectedArtifactPath = Join-Path $executionArtifactRoot $ExpectArtifactFileCreated
        if (-not (Test-Path $expectedArtifactPath)) {
            throw "PET TASKS execution did not produce expected artifact '$ExpectArtifactFileCreated'. expected='$expectedArtifactPath'"
        }
    }

    $previewTrace = Wait-ForTraceText -TracePath $shellTracePath -Pattern "pet-command \| .*preview .*family=$escapedFamily.*status=PreviewReady"
    if ($null -eq $previewTrace) {
        throw "PET TASKS preview trace was not observed."
    }

    if (-not $SkipSpriteHashCheck) {
        $afterHashes = @(Get-AllTargetHashes)
        $changed = @()
        for ($i = 0; $i -lt $beforeHashes.Count; $i++) {
            for ($j = 0; $j -lt $beforeHashes[$i].files.Count; $j++) {
                if ($beforeHashes[$i].files[$j].hash -ne $afterHashes[$i].files[$j].hash) {
                    $changed += $beforeHashes[$i].files[$j].path
                }
            }
        }
        if ($changed.Count -gt 0) {
            throw "PET TASKS preview mutated target sprite files: $($changed -join ', ')"
        }
    }

    $auditPath = ""
    if ($previewTrace.Line -match "audit=(?<audit>.+)$") {
        $auditPath = $Matches.audit.Trim()
    }
    if ([string]::IsNullOrWhiteSpace($auditPath) -or -not (Test-Path $auditPath)) {
        throw "PET TASKS preview did not produce a readable audit path. trace='$($previewTrace.Line)'"
    }

    $summary = [ordered]@{
        captured_at = (Get-Date).ToString("s")
        output_dir = $OutputDir
        layout_screenshot_path = if ([string]::IsNullOrWhiteSpace($LayoutScreenshotPath)) { "" } else { [System.IO.Path]::GetFullPath($LayoutScreenshotPath) }
        trace = $shellTracePath
        audit_path = $auditPath
        task_text = $TaskText
        expected_tool_family = $ExpectedToolFamily
        approve_before_preview = [bool]$ApproveBeforePreview
        expect_execute_enabled_after_preview = [bool]$ExpectExecuteEnabledAfterPreview
        execute_enabled_after_preview = $executeEnabledAfterPreview
        expect_artifact_file_created = $ExpectArtifactFileCreated
        execution_audit_path = $executionAuditPath
        expected_artifact_path = $expectedArtifactPath
        wellbeing_text = $wellbeingText
        capability_text = $capabilityText
        next_action_text = $nextActionText
        post_preview_next_action_text = $postPreviewNextActionText
        result_path_text = $resultPathText
        post_preview_result_path_text = $postPreviewResultPathText
        target_rows_checked = @($beforeHashes | ForEach-Object { $_.row })
        target_hash_check_skipped = [bool]$SkipSpriteHashCheck
        target_hashes_unchanged = if ($SkipSpriteHashCheck) { $null } else { $true }
        shell_alive = -not $shellProcess.HasExited
    }
    $summaryPath = Join-Path $OutputDir "summary.json"
    $summary | ConvertTo-Json -Depth 4 | Set-Content -Path $summaryPath -Encoding UTF8
    Write-Output "summary=$summaryPath"
}
catch {
    $failure = [ordered]@{
        captured_at = (Get-Date).ToString("s")
        output_dir = $OutputDir
        error = $_.Exception.Message
        category = $_.CategoryInfo.ToString()
        script_stack = $_.ScriptStackTrace
    }
    $failurePath = Join-Path $OutputDir "failure.json"
    $failure | ConvertTo-Json -Depth 4 | Set-Content -Path $failurePath -Encoding UTF8
    Write-Error "PET TASKS vNext probe failed. failure=$failurePath error=$($_.Exception.Message)"
    exit 1
}
finally {
    if ($null -ne $shellProcess -and -not $shellProcess.HasExited) {
        Stop-Process -Id $shellProcess.Id -Force
    }
}
