param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$SkipBuild,
    [switch]$OpenPetTasks,
    [string]$WindowTitle = "",
    [int]$StartupDelayMs = 2500
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$BuildShellExe = Join-Path $ProjectRoot "vnext\src\Wevito.VNext.Shell\bin\$Configuration\net8.0-windows\Wevito.VNext.Shell.exe"
$OutputRoot = Join-Path $ProjectRoot "vnext\artifacts\pet-tasks"
$RunStamp = "$(Get-Date -Format "yyyyMMdd-HHmmss")-capture-wevito-window"
$OutputDir = Join-Path $OutputRoot $RunStamp
$DataDir = Join-Path $OutputDir "data"
$TraceDir = Join-Path $OutputDir "trace"
$ScreenshotPath = Join-Path $OutputDir "screenshot.png"
$ManifestPath = Join-Path $OutputDir "manifest.json"
$SummaryPath = Join-Path $OutputDir "run-summary.md"

if (-not $SkipBuild) {
    dotnet build (Join-Path $ProjectRoot "vnext\src\Wevito.VNext.Shell\Wevito.VNext.Shell.csproj") --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "vNext Shell build failed with exit code $LASTEXITCODE"
    }
}

if (-not (Test-Path $BuildShellExe)) {
    throw "Missing shell executable: $BuildShellExe"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
New-Item -ItemType Directory -Force -Path $DataDir | Out-Null
New-Item -ItemType Directory -Force -Path $TraceDir | Out-Null

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class WevitoWindowCaptureNative
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

function New-CaptureScenario {
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

function Get-WindowInfo {
    param(
        [int]$ProcessId,
        [string]$Title,
        [int]$TimeoutMs = 10000
    )

    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    while ((Get-Date) -lt $deadline) {
        $windows = [WevitoWindowCaptureNative]::GetWindowsForProcess($ProcessId)
        $match = if ([string]::IsNullOrWhiteSpace($Title)) {
            $windows | Where-Object { $_.Title -like "Wevito*" } | Select-Object -First 1
        }
        else {
            $windows | Where-Object { $_.Title -eq $Title } | Select-Object -First 1
        }

        if ($null -ne $match) {
            return $match
        }

        Start-Sleep -Milliseconds 200
    }

    throw "Timed out waiting for Wevito window '$Title' for process $ProcessId."
}

function Set-WindowForeground {
    param($WindowInfo)

    $shell = New-Object -ComObject WScript.Shell
    for ($attempt = 0; $attempt -lt 8; $attempt++) {
        [void][WevitoWindowCaptureNative]::ShowWindow($WindowInfo.Handle, 5)
        [void][WevitoWindowCaptureNative]::SetForegroundWindow($WindowInfo.Handle)
        try { [void]$shell.AppActivate($WindowInfo.Title) } catch { }
        try { [void]$shell.AppActivate($WindowInfo.ProcessId) } catch { }
        Start-Sleep -Milliseconds 200
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

    $invoke = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $invoke.Invoke()
    Start-Sleep -Milliseconds 800
}

function Save-WindowScreenshot {
    param($WindowInfo)

    if ($WindowInfo.Width -le 0 -or $WindowInfo.Height -le 0) {
        throw "Cannot capture invalid window rectangle: $($WindowInfo.Width)x$($WindowInfo.Height)."
    }

    $bitmap = New-Object System.Drawing.Bitmap($WindowInfo.Width, $WindowInfo.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen($WindowInfo.Left, $WindowInfo.Top, 0, 0, $bitmap.Size)
        $bitmap.Save($ScreenshotPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$shellProcess = $null

try {
    $scenarioPath = New-CaptureScenario
    $shellProcess = Start-ShellProcess -ScenarioPath $scenarioPath
    Start-Sleep -Milliseconds $StartupDelayMs

    $homeWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito Home Panel"
    Set-WindowForeground -WindowInfo $homeWindow

    $captureWindow = $homeWindow
    $capturePreset = "wevitoWindow"
    $captureTarget = "wevitoWindow"

    if ($OpenPetTasks) {
        Invoke-Button -WindowInfo $homeWindow -AutomationId "WebToolsButton"
        Start-Sleep -Milliseconds 500
        $homeWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito Home Panel"
        Set-WindowForeground -WindowInfo $homeWindow
        Invoke-Button -WindowInfo $homeWindow -AutomationId "HelperTabButton"
        $captureWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito PET TASKS"
        Set-WindowForeground -WindowInfo $captureWindow
        $capturePreset = "proofSurface"
        $captureTarget = "proofSurface"
    }
    elseif (-not [string]::IsNullOrWhiteSpace($WindowTitle)) {
        $captureWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title $WindowTitle
        Set-WindowForeground -WindowInfo $captureWindow
    }

    Save-WindowScreenshot -WindowInfo $captureWindow

    $capturedAt = [DateTimeOffset]::UtcNow.ToString("o")
    $manifest = [ordered]@{
        schemaVersion = "wevito.capture.v1"
        requestId = [Guid]::NewGuid().ToString()
        preset = $capturePreset
        targetKind = $captureTarget
        outputKind = "screenshotPng"
        privacyLevel = "wevitoOnly"
        artifactRoot = $OutputDir
        outputPath = $ScreenshotPath
        manifestPath = $ManifestPath
        summaryPath = $SummaryPath
        windowTitle = $captureWindow.Title
        region = [ordered]@{
            x = $captureWindow.Left
            y = $captureWindow.Top
            width = $captureWindow.Width
            height = $captureWindow.Height
        }
        includeCursor = $false
        includeOverlayMetadata = $true
        didUploadOrShare = $false
        capturedAtUtc = $capturedAt
    }
    $manifest | ConvertTo-Json -Depth 8 | Set-Content -Path $ManifestPath -Encoding UTF8

    $summary = @"
# Wevito Window Capture

- Captured at: $capturedAt
- Window title: $($captureWindow.Title)
- Preset: $capturePreset
- Target: $captureTarget
- Privacy: wevitoOnly
- Output: $ScreenshotPath
- Manifest: $ManifestPath
- External upload/share: false
- Recording: false
"@
    $summary | Set-Content -Path $SummaryPath -Encoding UTF8

    Write-Output "summary=$SummaryPath"
    Write-Output "screenshot=$ScreenshotPath"
    Write-Output "manifest=$ManifestPath"
}
catch {
    $failurePath = Join-Path $OutputDir "failure.json"
    [ordered]@{
        captured_at = (Get-Date).ToString("s")
        output_dir = $OutputDir
        error = $_.Exception.Message
        category = $_.CategoryInfo.ToString()
        script_stack = $_.ScriptStackTrace
    } | ConvertTo-Json -Depth 6 | Set-Content -Path $failurePath -Encoding UTF8
    Write-Error "Wevito window capture failed. failure=$failurePath error=$($_.Exception.Message)"
    exit 1
}
finally {
    if ($null -ne $shellProcess -and -not $shellProcess.HasExited) {
        Stop-Process -Id $shellProcess.Id -Force
    }
}
