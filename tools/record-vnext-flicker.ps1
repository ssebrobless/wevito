param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$SkipBuild,
    [int]$StartupDelayMs = 2500,
    [int]$RecordSeconds = 12
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$BuildScript = Join-Path $ProjectRoot "tools\build-vnext.ps1"
$PublishedShellExe = Join-Path $ProjectRoot "vnext\artifacts\shell\Wevito.VNext.Shell.exe"
$BuildShellExe = Join-Path $ProjectRoot "vnext\src\Wevito.VNext.Shell\bin\$Configuration\net8.0-windows\Wevito.VNext.Shell.exe"
$Analyzer = Join-Path $ProjectRoot "tools\analyze-vnext-flicker.py"
$OutputRoot = Join-Path $ProjectRoot "vnext\artifacts\flicker"
$RunStamp = Get-Date -Format "yyyyMMdd-HHmmss"
$OutputDir = Join-Path $OutputRoot $RunStamp
$DataDir = Join-Path $OutputDir "data"
$TraceDir = Join-Path $OutputDir "trace"
$AutomationLog = Join-Path $OutputDir "automation.log"

if (-not $SkipBuild) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $BuildScript -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "vNext build failed with exit code $LASTEXITCODE"
    }
}

function Resolve-ShellExe {
    if (Test-Path $BuildShellExe) {
        return $BuildShellExe
    }

    if (Test-Path $PublishedShellExe) {
        return $PublishedShellExe
    }

    throw "Missing shell executable. Checked: $BuildShellExe and $PublishedShellExe"
}

$ShellExe = Resolve-ShellExe

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
New-Item -ItemType Directory -Force -Path $DataDir | Out-Null
New-Item -ItemType Directory -Force -Path $TraceDir | Out-Null

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class WevitoFlickerNative
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
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

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

function Write-Marker {
    param([string]$Message)

    $line = "{0} | automation | {1}" -f (Get-Date).ToString("o"), $Message
    Add-Content -Path $AutomationLog -Value $line -Encoding UTF8
}

function Get-WindowInfo {
    param(
        [int]$ProcessId,
        [string]$Title = "",
        [int]$TimeoutMs = 10000
    )

    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    while ((Get-Date) -lt $deadline) {
        $windows = [WevitoFlickerNative]::GetWindowsForProcess($ProcessId)
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
        $windows = [WevitoFlickerNative]::GetWindowsForProcess($ProcessId)
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

function Get-AutomationElementByName {
    param(
        [IntPtr]$Handle,
        [string]$Name,
        [System.Windows.Automation.ControlType]$ControlType = $null,
        [int]$TimeoutMs = 2500
    )

    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    while ((Get-Date) -lt $deadline) {
        $root = [System.Windows.Automation.AutomationElement]::FromHandle($Handle)
        if ($null -ne $root) {
            $conditions = [System.Collections.Generic.List[System.Windows.Automation.Condition]]::new()
            $conditions.Add([System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, $Name))
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

function Get-AutomationElementById {
    param(
        [IntPtr]$Handle,
        [string]$AutomationId,
        [System.Windows.Automation.ControlType]$ControlType = $null,
        [int]$TimeoutMs = 2500
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

function Invoke-WindowButton {
    param(
        $WindowInfo,
        [string[]]$Names
    )

    foreach ($name in $Names) {
        $button = Get-AutomationElementByName -Handle $WindowInfo.Handle -Name $name -ControlType ([System.Windows.Automation.ControlType]::Button)
        if ($null -eq $button) {
            continue
        }

        $invoke = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $invoke.Invoke()
        Start-Sleep -Milliseconds 800
        return $true
    }

    return $false
}

function Invoke-WindowButtonById {
    param(
        $WindowInfo,
        [string[]]$AutomationIds,
        [string[]]$Names = @()
    )

    foreach ($automationId in $AutomationIds) {
        $button = Get-AutomationElementById -Handle $WindowInfo.Handle -AutomationId $automationId -ControlType ([System.Windows.Automation.ControlType]::Button)
        if ($null -eq $button) {
            continue
        }

        $invoke = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $invoke.Invoke()
        Start-Sleep -Milliseconds 800
        return $true
    }

    if ($Names.Count -gt 0) {
        return Invoke-WindowButton -WindowInfo $WindowInfo -Names $Names
    }

    return $false
}

function Set-WindowForeground {
    param($WindowInfo)

    [void][WevitoFlickerNative]::ShowWindow($WindowInfo.Handle, 5)
    [void][WevitoFlickerNative]::SetForegroundWindow($WindowInfo.Handle)
    Start-Sleep -Milliseconds 500
}

function Invoke-GlobalHotkey {
    param([string]$Keys)

    $shell = New-Object -ComObject WScript.Shell
    $shell.SendKeys($Keys)
    Start-Sleep -Milliseconds 700
}

function Invoke-LeftClickAt {
    param(
        [int]$X,
        [int]$Y
    )

    [void][WevitoFlickerNative]::SetCursorPos($X, $Y)
    Start-Sleep -Milliseconds 100
    [WevitoFlickerNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [WevitoFlickerNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 700
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

function Open-BasketWindow {
    param($HomeWindow, [int]$ProcessId)

    Invoke-GlobalHotkey -Keys "^+o"
    try {
        return Get-ToolWindowInfo -ProcessId $ProcessId -TimeoutMs 1800
    }
    catch {
        Set-WindowForeground -WindowInfo $HomeWindow
        if (-not (Invoke-WindowButtonById -WindowInfo $HomeWindow -AutomationIds @("WebToolsButton") -Names @("TOOLS", "HIDE"))) {
            throw "Failed to toggle webtools bar."
        }

        if (-not (Invoke-WindowButtonById -WindowInfo $HomeWindow -AutomationIds @("LinkBinTabButton") -Names @("LINK BIN", "LINK BIN ACTIVE"))) {
            throw "Failed to open link bin tab."
        }

        return Get-ToolWindowInfo -ProcessId $ProcessId -TimeoutMs 4000
    }
}

function Start-ShellProcess {
    param([string]$ExePath)

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $ExePath
    $psi.WorkingDirectory = Split-Path -Parent $ExePath
    $psi.UseShellExecute = $false
    $psi.Environment["WEVITO_VNEXT_DATA_ROOT"] = $DataDir
    $psi.Environment["WEVITO_VNEXT_TRACE_DIR"] = $TraceDir
    return [System.Diagnostics.Process]::Start($psi)
}

$shellProcess = $null
$notepadProcess = $null
$ffmpegProcess = $null

try {
    Write-Marker "startup"
    $shellProcess = Start-ShellProcess -ExePath $ShellExe
    Start-Sleep -Milliseconds $StartupDelayMs

    $homeWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito Home Panel"
    $roamWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito Roam Band"

    $notepadProcess = Start-Process -FilePath "notepad.exe" -PassThru
    Start-Sleep -Milliseconds 1000
    $notepadWindow = Get-WindowInfo -ProcessId $notepadProcess.Id

    $screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $videoPath = Join-Path $OutputDir "transition-capture.mp4"
    $ffmpegArgs = @(
        "-y",
        "-f", "gdigrab",
        "-framerate", "20",
        "-offset_x", "$($screen.X)",
        "-offset_y", "$($screen.Y)",
        "-video_size", "$($screen.Width)x$($screen.Height)",
        "-draw_mouse", "1",
        "-i", "desktop",
        "-t", "$RecordSeconds",
        "-c:v", "libx264",
        "-preset", "ultrafast",
        "-pix_fmt", "yuv420p",
        $videoPath
    )
    $ffmpegProcess = Start-Process -FilePath "ffmpeg" -ArgumentList $ffmpegArgs -WindowStyle Hidden -PassThru
    Start-Sleep -Milliseconds 900

    Write-Marker "focused-home"
    Set-WindowForeground -WindowInfo $homeWindow
    Start-Sleep -Milliseconds 1000

    Write-Marker "passive-notepad-foreground"
    Set-WindowForeground -WindowInfo $notepadWindow
    Start-Sleep -Milliseconds 1200

    Write-Marker "pinned-hotkey"
    Set-ClipboardWithRetry -Value "https://openai.com/"
    Invoke-GlobalHotkey -Keys "^+p"
    Start-Sleep -Milliseconds 1200

    Write-Marker "basket-capture"
    Invoke-GlobalHotkey -Keys "^+b"
    Start-Sleep -Milliseconds 500

    Write-Marker "basket-open"
    Set-WindowForeground -WindowInfo $homeWindow
    $basketWindow = Open-BasketWindow -HomeWindow $homeWindow -ProcessId $shellProcess.Id
    Start-Sleep -Milliseconds 1500

    Write-Marker "release-pinned"
    Invoke-GlobalHotkey -Keys "^+p"
    Start-Sleep -Milliseconds 1200

    if (-not $ffmpegProcess.WaitForExit($RecordSeconds * 1000 + 5000)) {
        Stop-Process -Id $ffmpegProcess.Id -Force
        throw "ffmpeg recording did not finish in time."
    }

    $regions = [ordered]@{
        regions = [ordered]@{
            home_panel = [ordered]@{ x = $homeWindow.Left; y = $homeWindow.Top; width = $homeWindow.Width; height = $homeWindow.Height }
            roam_band = [ordered]@{ x = $roamWindow.Left; y = $roamWindow.Top; width = $roamWindow.Width; height = $roamWindow.Height }
            basket_popup = [ordered]@{ x = $basketWindow.Left; y = $basketWindow.Top; width = $basketWindow.Width; height = $basketWindow.Height }
        }
    }
    $regionsPath = Join-Path $OutputDir "regions.json"
    $regions | ConvertTo-Json -Depth 5 | Set-Content -Path $regionsPath -Encoding UTF8

    python $Analyzer --video $videoPath --output-dir $OutputDir --regions $regionsPath --automation-log $AutomationLog --fps 12 --top 6
    if ($LASTEXITCODE -ne 0) {
        throw "Flicker analysis failed with exit code $LASTEXITCODE"
    }

    $summary = [ordered]@{
        captured_at = (Get-Date).ToString("s")
        output_dir = $OutputDir
        video = $videoPath
        trace_dir = $TraceDir
        automation_log = $AutomationLog
        analysis = (Join-Path $OutputDir "flicker-summary.json")
        regions = $regions.regions
    }
    $summaryPath = Join-Path $OutputDir "summary.json"
    $summary | ConvertTo-Json -Depth 5 | Set-Content -Path $summaryPath -Encoding UTF8
    Write-Output "summary=$summaryPath"
}
finally {
    if ($null -ne $ffmpegProcess -and -not $ffmpegProcess.HasExited) {
        Stop-Process -Id $ffmpegProcess.Id -Force
    }

    if ($null -ne $notepadProcess -and -not $notepadProcess.HasExited) {
        Stop-Process -Id $notepadProcess.Id -Force
    }

    if ($null -ne $shellProcess -and -not $shellProcess.HasExited) {
        Stop-Process -Id $shellProcess.Id -Force
    }
}
