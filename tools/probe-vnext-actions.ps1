param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$SkipBuild,
    [int]$StartupDelayMs = 2500
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$BuildScript = Join-Path $ProjectRoot "tools\build-vnext.ps1"
$ShellExe = Join-Path $ProjectRoot "vnext\artifacts\shell\Wevito.VNext.Shell.exe"
$OutputRoot = Join-Path $ProjectRoot "vnext\artifacts\action-probes"
$RunStamp = Get-Date -Format "yyyyMMdd-HHmmss"
$OutputDir = Join-Path $OutputRoot $RunStamp
$DataDir = Join-Path $OutputDir "data"
$TraceDir = Join-Path $OutputDir "trace"

if (-not $SkipBuild) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $BuildScript -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "vNext build failed with exit code $LASTEXITCODE"
    }
}

if (-not (Test-Path $ShellExe)) {
    throw "Missing published shell executable: $ShellExe"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
New-Item -ItemType Directory -Force -Path $DataDir | Out-Null
New-Item -ItemType Directory -Force -Path $TraceDir | Out-Null

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class WevitoActionProbeNative
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

function Start-ShellProcess {
    param(
        [string]$ExePath,
        [string]$DataRoot,
        [string]$TraceRoot
    )

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $ExePath
    $psi.WorkingDirectory = Split-Path -Parent $ExePath
    $psi.UseShellExecute = $false
    $psi.Environment["WEVITO_VNEXT_DATA_ROOT"] = $DataRoot
    $psi.Environment["WEVITO_VNEXT_TRACE_DIR"] = $TraceRoot
    return [System.Diagnostics.Process]::Start($psi)
}

function Get-WindowInfo {
    param(
        [int]$ProcessId,
        [string]$Title = "",
        [int]$TimeoutMs = 10000
    )

    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    while ((Get-Date) -lt $deadline) {
        $windows = [WevitoActionProbeNative]::GetWindowsForProcess($ProcessId)
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
        [int]$TimeoutMs = 5000
    )

    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    while ((Get-Date) -lt $deadline) {
        $windows = [WevitoActionProbeNative]::GetWindowsForProcess($ProcessId)
        $match = $windows |
            Where-Object { $_.Title -ne "Wevito Home Panel" -and $_.Title -ne "Wevito Roam Band" } |
            Select-Object -First 1
        if ($null -ne $match) {
            return $match
        }

        Start-Sleep -Milliseconds 200
    }

    throw "Timed out waiting for tool popup window for process $ProcessId."
}

function Set-WindowForeground {
    param($WindowInfo)

    [void][WevitoActionProbeNative]::ShowWindow($WindowInfo.Handle, 5)
    [void][WevitoActionProbeNative]::SetForegroundWindow($WindowInfo.Handle)
    Start-Sleep -Milliseconds 500
}

function Get-AutomationElement {
    param(
        [IntPtr]$Handle,
        [string]$Name = "",
        [string]$AutomationId = "",
        [System.Windows.Automation.ControlType]$ControlType = $null,
        [int]$TimeoutMs = 2500
    )

    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    while ((Get-Date) -lt $deadline) {
        $root = [System.Windows.Automation.AutomationElement]::FromHandle($Handle)
        if ($null -ne $root) {
            $conditions = [System.Collections.Generic.List[System.Windows.Automation.Condition]]::new()
            if (-not [string]::IsNullOrWhiteSpace($AutomationId)) {
                $conditions.Add([System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $AutomationId))
            }
            elseif (-not [string]::IsNullOrWhiteSpace($Name)) {
                $conditions.Add([System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, $Name))
            }
            else {
                throw "Name or AutomationId is required."
            }

            if ($null -ne $ControlType) {
                $conditions.Add([System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty, $ControlType))
            }

            $condition = if ($conditions.Count -eq 1) {
                $conditions[0]
            }
            else {
                [System.Windows.Automation.AndCondition]::new($conditions.ToArray())
            }

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
        [string[]]$Names = @(),
        [string[]]$AutomationIds = @(),
        [switch]$AllowDisabled
    )

    foreach ($automationId in $AutomationIds) {
        $button = Get-AutomationElement -Handle $WindowInfo.Handle -AutomationId $automationId -ControlType ([System.Windows.Automation.ControlType]::Button)
        if ($null -eq $button) {
            continue
        }

        if (-not $AllowDisabled -and -not $button.Current.IsEnabled) {
            continue
        }

        $invoke = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $invoke.Invoke()
        Start-Sleep -Milliseconds 900
        return $automationId
    }

    foreach ($name in $Names) {
        $button = Get-AutomationElement -Handle $WindowInfo.Handle -Name $name -ControlType ([System.Windows.Automation.ControlType]::Button)
        if ($null -eq $button) {
            continue
        }

        if (-not $AllowDisabled -and -not $button.Current.IsEnabled) {
            continue
        }

        $invoke = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $invoke.Invoke()
        Start-Sleep -Milliseconds 900
        return $name
    }

    return $null
}

function Toggle-CheckBoxByName {
    param(
        $WindowInfo,
        [string]$Name
    )

    $checkbox = Get-AutomationElement -Handle $WindowInfo.Handle -Name $Name -ControlType ([System.Windows.Automation.ControlType]::CheckBox)
    if ($null -eq $checkbox) {
        throw "Missing checkbox '$Name'."
    }

    $toggle = $checkbox.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    $toggle.Toggle()
    Start-Sleep -Milliseconds 600
}

function Select-ListItemByName {
    param(
        $WindowInfo,
        [string]$Name
    )

    $item = Get-AutomationElement -Handle $WindowInfo.Handle -Name $Name -ControlType ([System.Windows.Automation.ControlType]::ListItem)
    if ($null -eq $item) {
        throw "Missing list item '$Name'."
    }

    $selection = $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    $selection.Select()
    Start-Sleep -Milliseconds 500
}

function Select-FirstListItem {
    param($WindowInfo)

    $list = Get-AutomationElement -Handle $WindowInfo.Handle -AutomationId "BasketList" -ControlType ([System.Windows.Automation.ControlType]::List)
    if ($null -eq $list) {
        throw "Missing basket list."
    }

    $item = $list.FindFirst(
        [System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::ListItem))

    if ($null -eq $item) {
        throw "Missing basket list item."
    }

    $selection = $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    $selection.Select()
    Start-Sleep -Milliseconds 500
}

function Wait-ForTraceText {
    param(
        [string]$TracePath,
        [string]$Pattern,
        [int]$TimeoutMs = 6000
    )

    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $TracePath) {
            $match = Select-String -Path $TracePath -Pattern $Pattern -SimpleMatch:$false -ErrorAction SilentlyContinue
            if ($null -ne $match) {
                return $true
            }
        }

        Start-Sleep -Milliseconds 200
    }

    return $false
}

function Set-ClipboardWithRetry {
    param([string]$Value)

    for ($i = 0; $i -lt 10; $i++) {
        try {
            Set-Clipboard -Value $Value
            return
        }
        catch {
            Start-Sleep -Milliseconds 250
        }
    }

    throw "Failed to set clipboard."
}

function Get-BrowserSnapshot {
    $names = @("msedge", "chrome", "firefox", "brave", "opera")
    Get-Process -Name $names -ErrorAction SilentlyContinue | Select-Object Id, ProcessName
}

function Stop-NewBrowsers {
    param($BeforeSnapshot)

    $beforeIds = @{}
    foreach ($process in $BeforeSnapshot) {
        $beforeIds[$process.Id] = $true
    }

    $after = Get-BrowserSnapshot
    foreach ($process in $after) {
        if (-not $beforeIds.ContainsKey($process.Id)) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }
    }
}

function New-SessionPaths {
    param([string]$Name)

    $sessionRoot = Join-Path $OutputDir $Name
    $sessionData = Join-Path $sessionRoot "data"
    $sessionTrace = Join-Path $sessionRoot "trace"
    New-Item -ItemType Directory -Force -Path $sessionRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $sessionData | Out-Null
    New-Item -ItemType Directory -Force -Path $sessionTrace | Out-Null

    return [ordered]@{
        root = $sessionRoot
        data = $sessionData
        trace = $sessionTrace
    }
}

function Invoke-ActionSession {
    param(
        [string]$ActionId,
        [string]$ButtonId,
        [switch]$RequireRecall
    )

    $paths = New-SessionPaths -Name ("action-" + $ActionId)
    $shellProcess = $null
    $notepadProcess = $null

    try {
        $shellProcess = Start-ShellProcess -ExePath $ShellExe -DataRoot $paths.data -TraceRoot $paths.trace
        Start-Sleep -Milliseconds $StartupDelayMs

        $homeWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito Home Panel"
        Set-WindowForeground -WindowInfo $homeWindow

        if ($RequireRecall) {
            $notepadProcess = Start-Process -FilePath "notepad.exe" -PassThru
            Start-Sleep -Milliseconds 1200
            $notepadWindow = Get-WindowInfo -ProcessId $notepadProcess.Id
            Set-WindowForeground -WindowInfo $notepadWindow
            Start-Sleep -Milliseconds 2800
            [void][WevitoActionProbeNative]::ShowWindow($homeWindow.Handle, 5)
            [void][WevitoActionProbeNative]::SetForegroundWindow($homeWindow.Handle)
            Start-Sleep -Milliseconds 120
        }

        $invoked = Invoke-Button -WindowInfo $homeWindow -AutomationIds @($ButtonId) -Names @($ActionId)
        if ($null -eq $invoked) {
            throw "Failed to invoke action button '$ActionId'."
        }

        $shellTracePath = Join-Path $paths.trace "Wevito.VNext.Shell.trace.log"
        $tracePattern = "action \| .*" + [regex]::Escape($ActionId)
        $traceMatched = Wait-ForTraceText -TracePath $shellTracePath -Pattern $tracePattern
        $shellAlive = -not $shellProcess.HasExited

        return [ordered]@{
            action = $ActionId
            output_dir = $paths.root
            invoked = $invoked
            trace_matched = $traceMatched
            shell_alive = $shellAlive
        }
    }
    finally {
        if ($null -ne $notepadProcess -and -not $notepadProcess.HasExited) {
            Stop-Process -Id $notepadProcess.Id -Force
        }

        if ($null -ne $shellProcess -and -not $shellProcess.HasExited) {
            Stop-Process -Id $shellProcess.Id -Force
        }
    }
}

function Invoke-ToolSession {
    $paths = New-SessionPaths -Name "tools"
    $shellProcess = $null

    try {
        $shellProcess = Start-ShellProcess -ExePath $ShellExe -DataRoot $paths.data -TraceRoot $paths.trace
        Start-Sleep -Milliseconds $StartupDelayMs

        $homeWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito Home Panel"
        Set-WindowForeground -WindowInfo $homeWindow

        $shellTracePath = Join-Path $paths.trace "Wevito.VNext.Shell.trace.log"
        $brokerTracePath = Join-Path $paths.trace "Wevito.VNext.Broker.trace.log"

        $saveInvoked = Invoke-Button -WindowInfo $homeWindow -AutomationIds @("SaveButton") -Names @("SAVE")
        if ($null -eq $saveInvoked) {
            throw "Failed to invoke save button."
        }

        $settingsInvoked = Invoke-Button -WindowInfo $homeWindow -AutomationIds @("SettingsButton") -Names @("SET", "DONE")
        if ($null -eq $settingsInvoked) {
            throw "Failed to open settings popup."
        }

        $toolWindow = Get-ToolWindowInfo -ProcessId $shellProcess.Id
        Toggle-CheckBoxByName -WindowInfo $toolWindow -Name "Compact focused HUD"
        Toggle-CheckBoxByName -WindowInfo $toolWindow -Name "Show pet names in the habitat"
        Toggle-CheckBoxByName -WindowInfo $toolWindow -Name "Show status summary text"

        $closeInvoked = Invoke-Button -WindowInfo $toolWindow -AutomationIds @("ToolCloseButton") -Names @("Close")
        if ($null -eq $closeInvoked) {
            throw "Failed to close settings popup."
        }

        $basketUrl = "https://example.com/actions"
        Set-ClipboardWithRetry -Value $basketUrl

        $captureInvoked = Invoke-Button -WindowInfo $homeWindow -AutomationIds @("CaptureButton") -Names @("CAP")
        if ($null -eq $captureInvoked) {
            throw "Failed to invoke capture button."
        }

        if (-not (Wait-ForTraceText -TracePath $shellTracePath -Pattern "basket \| .*count=1")) {
            throw "Basket capture did not appear in shell trace."
        }

        $basketInvoked = Invoke-Button -WindowInfo $homeWindow -AutomationIds @("BasketButton") -Names @("BIN", "HIDE")
        if ($null -eq $basketInvoked) {
            throw "Failed to open basket popup."
        }

        $toolWindow = Get-ToolWindowInfo -ProcessId $shellProcess.Id
        Select-FirstListItem -WindowInfo $toolWindow

        Set-ClipboardWithRetry -Value "wevito-sentinel"
        $copyInvoked = Invoke-Button -WindowInfo $toolWindow -AutomationIds @("CopyButton") -Names @("Copy")
        if ($null -eq $copyInvoked) {
            throw "Failed to invoke basket copy."
        }
        Start-Sleep -Milliseconds 600
        $clipboardAfterCopy = Get-Clipboard -Raw

        $browserSnapshot = Get-BrowserSnapshot
        $openInvoked = Invoke-Button -WindowInfo $toolWindow -AutomationIds @("OpenButton") -Names @("Open")
        if ($null -eq $openInvoked) {
            throw "Failed to invoke basket open."
        }
        Start-Sleep -Milliseconds 1200
        Stop-NewBrowsers -BeforeSnapshot $browserSnapshot

        $deleteInvoked = Invoke-Button -WindowInfo $toolWindow -AutomationIds @("DeleteButton") -Names @("Delete")
        if ($null -eq $deleteInvoked) {
            throw "Failed to invoke basket delete."
        }

        $closeInvoked = Invoke-Button -WindowInfo $toolWindow -AutomationIds @("ToolCloseButton") -Names @("Close")
        if ($null -eq $closeInvoked) {
            throw "Failed to close basket popup."
        }

        return [ordered]@{
            output_dir = $paths.root
            settings_traces = [ordered]@{
                compact_hud = Wait-ForTraceText -TracePath $shellTracePath -Pattern "settings \| .*compact_hud="
                show_pet_names = Wait-ForTraceText -TracePath $shellTracePath -Pattern "settings \| .*show_pet_names="
                show_status_summary = Wait-ForTraceText -TracePath $shellTracePath -Pattern "settings \| .*show_status_summary="
            }
            basket_traces = [ordered]@{
                copied = Wait-ForTraceText -TracePath $shellTracePath -Pattern "basket \| .*copy id="
                opened = Wait-ForTraceText -TracePath $shellTracePath -Pattern "basket \| .*open id="
                deleted = Wait-ForTraceText -TracePath $shellTracePath -Pattern "basket \| .*delete id="
                broker_open_url = Wait-ForTraceText -TracePath $brokerTracePath -Pattern "shell-command \| .*OpenUrl"
            }
            save_trace = Wait-ForTraceText -TracePath $shellTracePath -Pattern "persistence \| .*saved pets="
            clipboard_after_copy = $clipboardAfterCopy
            shell_alive = -not $shellProcess.HasExited
        }
    }
    finally {
        if ($null -ne $shellProcess -and -not $shellProcess.HasExited) {
            Stop-Process -Id $shellProcess.Id -Force
        }
    }
}

$summary = [ordered]@{}
$actionResults = @()

$actionSpecs = @(
    [ordered]@{ id = "doctor"; button = "DoctorButton"; require_recall = $false },
    [ordered]@{ id = "medicine"; button = "MedicineButton"; require_recall = $false },
    [ordered]@{ id = "bath"; button = "BathButton"; require_recall = $false },
    [ordered]@{ id = "groom"; button = "GroomButton"; require_recall = $false },
    [ordered]@{ id = "feed"; button = "FeedButton"; require_recall = $false },
    [ordered]@{ id = "water"; button = "WaterButton"; require_recall = $false },
    [ordered]@{ id = "play"; button = "PlayButton"; require_recall = $false },
    [ordered]@{ id = "rest"; button = "RestButton"; require_recall = $false },
    [ordered]@{ id = "home"; button = "HomeButton"; require_recall = $true }
)

foreach ($spec in $actionSpecs) {
    $actionResults += Invoke-ActionSession -ActionId $spec.id -ButtonId $spec.button -RequireRecall:$spec.require_recall
}

$toolResult = Invoke-ToolSession

$summary = [ordered]@{
    captured_at = (Get-Date).ToString("s")
    output_dir = $OutputDir
    actions = $actionResults
    tools = $toolResult
}

$summaryPath = Join-Path $OutputDir "summary.json"
$summary | ConvertTo-Json -Depth 6 | Set-Content -Path $summaryPath -Encoding UTF8
Write-Output "summary=$summaryPath"

if ($actionResults | Where-Object { -not $_.trace_matched -or -not $_.shell_alive }) {
    throw "One or more action sessions failed validation."
}
if ($toolResult.settings_traces.Values -contains $false) {
    throw "One or more settings trace validations failed."
}
if ($toolResult.basket_traces.Values -contains $false) {
    throw "One or more basket trace validations failed."
}
if (-not $toolResult.save_trace) {
    throw "Save trace validation failed."
}
if (-not $toolResult.shell_alive) {
    throw "Shell exited during tool probe."
}
if ($toolResult.clipboard_after_copy -ne "https://example.com/actions") {
    throw "Basket copy did not restore the expected URL to the clipboard."
}
