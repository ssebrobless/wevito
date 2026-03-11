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
$OutputRoot = Join-Path $ProjectRoot "vnext\artifacts\probes"
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

Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class WevitoProbeNative
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
    public static extern IntPtr GetForegroundWindow();

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

function Get-WindowInfo {
    param(
        [int]$ProcessId,
        [string]$Title = "",
        [int]$TimeoutMs = 10000
    )

    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    while ((Get-Date) -lt $deadline) {
        $windows = [WevitoProbeNative]::GetWindowsForProcess($ProcessId)
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

function Set-WindowForeground {
    param($WindowInfo)

    [void][WevitoProbeNative]::ShowWindow($WindowInfo.Handle, 5)
    [void][WevitoProbeNative]::SetForegroundWindow($WindowInfo.Handle)
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

    [void][WevitoProbeNative]::SetCursorPos($X, $Y)
    Start-Sleep -Milliseconds 100
    [WevitoProbeNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [WevitoProbeNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 700
}

function Set-ClipboardWithRetry {
    param(
        [string]$Value,
        [int]$Attempts = 10
    )

    for ($i = 0; $i -lt $Attempts; $i++) {
        try {
            Set-Clipboard -Value $Value
            return
        }
        catch {
            Start-Sleep -Milliseconds 250
        }
    }

    throw "Failed to set clipboard after $Attempts attempts."
}

function Get-ForegroundProcessId {
    $handle = [WevitoProbeNative]::GetForegroundWindow()
    if ($handle -eq [IntPtr]::Zero) {
        return 0
    }

    $windowPid = [uint32]0
    [void][WevitoProbeNative]::GetWindowThreadProcessId($handle, [ref]$windowPid)
    return [int]$windowPid
}

function Wait-ForTraceText {
    param(
        [string]$TracePath,
        [string]$Pattern,
        [int]$TimeoutMs = 4000
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

$shellProcess = $null
$notepadProcess = $null

try {
    $shellProcess = Start-ShellProcess -ExePath $ShellExe
    Start-Sleep -Milliseconds $StartupDelayMs

    $homeWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito Home Panel"
    $notepadProcess = Start-Process -FilePath "notepad.exe" -PassThru
    Start-Sleep -Milliseconds 1000
    $notepadWindow = Get-WindowInfo -ProcessId $notepadProcess.Id
    Set-WindowForeground -WindowInfo $notepadWindow

    $foregroundAfterNotepad = Get-ForegroundProcessId

    Invoke-GlobalHotkey -Keys "^+p"
    $foregroundAfterPin = Get-ForegroundProcessId

    Set-ClipboardWithRetry -Value "https://openai.com/pinned-probe"
    $overlayActionX = [int]($homeWindow.Left + $homeWindow.Width - 194)
    $overlayActionY = [int]($homeWindow.Top + 22)
    Invoke-LeftClickAt -X $overlayActionX -Y $overlayActionY
    $foregroundAfterClick = Get-ForegroundProcessId
    $shellTracePath = Join-Path $TraceDir "Wevito.VNext.Shell.trace.log"
    $captureTriggered = Wait-ForTraceText -TracePath $shellTracePath -Pattern "ui-command \| .*capture-clipboard"
    $basketUpdated = Wait-ForTraceText -TracePath $shellTracePath -Pattern "basket \| .*count=1"

    Invoke-GlobalHotkey -Keys "^+p"
    $foregroundAfterRelease = Get-ForegroundProcessId

    $result = [ordered]@{
        captured_at = (Get-Date).ToString("s")
        output_dir = $OutputDir
        home_window = [ordered]@{
            left = $homeWindow.Left
            top = $homeWindow.Top
            width = $homeWindow.Width
            height = $homeWindow.Height
        }
        foreground_after_notepad = $foregroundAfterNotepad
        foreground_after_pin = $foregroundAfterPin
        foreground_after_overlay_click = $foregroundAfterClick
        foreground_after_release = $foregroundAfterRelease
        notepad_pid = $notepadProcess.Id
        shell_pid = $shellProcess.Id
        pin_did_not_steal_focus = ($foregroundAfterPin -eq $notepadProcess.Id)
        overlay_click_did_not_steal_focus = ($foregroundAfterClick -eq $notepadProcess.Id)
        release_did_not_steal_focus = ($foregroundAfterRelease -eq $notepadProcess.Id)
        overlay_capture_triggered = $captureTriggered
        basket_updated = $basketUpdated
    }

    $summaryPath = Join-Path $OutputDir "summary.json"
    $result | ConvertTo-Json -Depth 5 | Set-Content -Path $summaryPath -Encoding UTF8
    Write-Output "summary=$summaryPath"

    if (-not $result.pin_did_not_steal_focus) {
        throw "Pinned HUD stole focus."
    }
    if (-not $result.overlay_click_did_not_steal_focus) {
        throw "Pinned overlay click stole focus."
    }
    if (-not $result.release_did_not_steal_focus) {
        throw "Release stole focus."
    }
    if (-not $result.overlay_capture_triggered -or -not $result.basket_updated) {
        throw "Pinned overlay click did not trigger the expected overlay interaction."
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
