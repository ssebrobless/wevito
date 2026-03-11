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
$OutputRoot = Join-Path $ProjectRoot "vnext\artifacts\screenshots"
$RunStamp = Get-Date -Format "yyyyMMdd-HHmmss"
$OutputDir = Join-Path $OutputRoot $RunStamp

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

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class WevitoVisualNative
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

function Get-WindowInfo {
    param(
        [int]$ProcessId,
        [string]$Title = "",
        [int]$TimeoutMs = 10000
    )

    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    while ((Get-Date) -lt $deadline) {
        $windows = [WevitoVisualNative]::GetWindowsForProcess($ProcessId)
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

    if ([string]::IsNullOrWhiteSpace($Title)) {
        throw "Timed out waiting for any visible window for process $ProcessId."
    }

    throw "Timed out waiting for window '$Title' for process $ProcessId."
}

function Set-WindowForeground {
    param($WindowInfo)

    [void][WevitoVisualNative]::ShowWindow($WindowInfo.Handle, 5)
    [void][WevitoVisualNative]::SetForegroundWindow($WindowInfo.Handle)
    Start-Sleep -Milliseconds 450
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

    [void][WevitoVisualNative]::SetCursorPos($X, $Y)
    Start-Sleep -Milliseconds 100
    [WevitoVisualNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [WevitoVisualNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 600
}

function Save-Image {
    param(
        [string]$Path,
        [int]$X,
        [int]$Y,
        [int]$Width,
        [int]$Height
    )

    $bitmap = New-Object System.Drawing.Bitmap $Width, $Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen($X, $Y, 0, 0, $bitmap.Size)
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Save-WindowCapture {
    param(
        [string]$Name,
        $WindowInfo
    )

    if ($WindowInfo.Width -le 0 -or $WindowInfo.Height -le 0) {
        return $null
    }

    $path = Join-Path $OutputDir "$Name.png"
    Save-Image -Path $path -X $WindowInfo.Left -Y $WindowInfo.Top -Width $WindowInfo.Width -Height $WindowInfo.Height
    return $path
}

function Save-DesktopCapture {
    param([string]$Name)

    $screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $path = Join-Path $OutputDir "$Name.png"
    Save-Image -Path $path -X $screen.X -Y $screen.Y -Width $screen.Width -Height $screen.Height
    return $path
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

function Open-BasketWindow {
    param($HomeWindow, [int]$ProcessId)

    Invoke-GlobalHotkey -Keys "^+o"
    try {
        return Get-WindowInfo -ProcessId $ProcessId -Title "Wevito Basket" -TimeoutMs 1500
    }
    catch {
        $buttonX = [int]($HomeWindow.Left + $HomeWindow.Width - 92)
        $buttonY = [int]($HomeWindow.Top + 22)
        Invoke-LeftClickAt -X $buttonX -Y $buttonY
        return Get-WindowInfo -ProcessId $ProcessId -Title "Wevito Basket" -TimeoutMs 3000
    }
}

$shellProcess = $null
$notepadProcess = $null

try {
    $shellProcess = Start-Process -FilePath $ShellExe -PassThru
    Start-Sleep -Milliseconds $StartupDelayMs

    $homeWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito Home Panel"
    $roamWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito Roam Band"

    Set-WindowForeground -WindowInfo $homeWindow
    $focusedDesktop = Save-DesktopCapture -Name "01-focused-desktop"
    $focusedHome = Save-WindowCapture -Name "02-focused-home" -WindowInfo $homeWindow

    $notepadProcess = Start-Process -FilePath "notepad.exe" -PassThru
    Start-Sleep -Milliseconds 1000
    $notepadWindow = Get-WindowInfo -ProcessId $notepadProcess.Id -TimeoutMs 15000
    Set-WindowForeground -WindowInfo $notepadWindow

    $passiveDesktop = Save-DesktopCapture -Name "03-passive-desktop"
    $passiveRoam = Save-WindowCapture -Name "04-passive-roam-band" -WindowInfo $roamWindow

    Set-ClipboardWithRetry -Value "https://openai.com/"
    Invoke-GlobalHotkey -Keys "^+p"
    $pinnedDesktop = Save-DesktopCapture -Name "05-pinned-desktop"
    $pinnedHome = Save-WindowCapture -Name "06-pinned-home" -WindowInfo $homeWindow

    Invoke-GlobalHotkey -Keys "^+b"
    Set-WindowForeground -WindowInfo $homeWindow
    $basketWindow = Open-BasketWindow -HomeWindow $homeWindow -ProcessId $shellProcess.Id
    $basketDesktop = Save-DesktopCapture -Name "07-basket-desktop"
    $basketPopup = Save-WindowCapture -Name "08-basket-popup" -WindowInfo $basketWindow

    $summary = [ordered]@{
        captured_at = (Get-Date).ToString("s")
        output_dir = $OutputDir
        shell_pid = $shellProcess.Id
        notepad_pid = if ($null -ne $notepadProcess) { $notepadProcess.Id } else { $null }
        windows = @(
            [ordered]@{ title = $homeWindow.Title; left = $homeWindow.Left; top = $homeWindow.Top; width = $homeWindow.Width; height = $homeWindow.Height },
            [ordered]@{ title = $roamWindow.Title; left = $roamWindow.Left; top = $roamWindow.Top; width = $roamWindow.Width; height = $roamWindow.Height },
            [ordered]@{ title = $basketWindow.Title; left = $basketWindow.Left; top = $basketWindow.Top; width = $basketWindow.Width; height = $basketWindow.Height }
        )
        screenshots = [ordered]@{
            focused_desktop = $focusedDesktop
            focused_home = $focusedHome
            passive_desktop = $passiveDesktop
            passive_roam_band = $passiveRoam
            pinned_desktop = $pinnedDesktop
            pinned_home = $pinnedHome
            basket_desktop = $basketDesktop
            basket_popup = $basketPopup
        }
    }

    $summaryPath = Join-Path $OutputDir "summary.json"
    $summary | ConvertTo-Json -Depth 6 | Set-Content -Path $summaryPath -Encoding UTF8
    Write-Output "summary=$summaryPath"
}
finally {
    if ($null -ne $notepadProcess -and -not $notepadProcess.HasExited) {
        Stop-Process -Id $notepadProcess.Id -Force
    }

    if ($null -ne $shellProcess -and -not $shellProcess.HasExited) {
        Stop-Process -Id $shellProcess.Id -Force
    }
}
