param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$SkipBuild,
    [int]$StartupDelayMs = 2500,
    [string]$ScenarioPath = "",
    [switch]$SkipBasket,
    [switch]$SkipDevTools
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$BuildScript = Join-Path $ProjectRoot "tools\build-vnext.ps1"
$SpritePreviewScript = Join-Path $ProjectRoot "tools\render_runtime_sprite_previews.py"
$AuthoredRoot = Join-Path $ProjectRoot "sprites_authored_verified"
$PreferAuthored = [string]::Equals($env:WEVITO_VNEXT_PREFER_AUTHORED, "1", [System.StringComparison]::OrdinalIgnoreCase)
$PublishedShellExe = Join-Path $ProjectRoot "vnext\artifacts\shell\Wevito.VNext.Shell.exe"
$BuildShellExe = Join-Path $ProjectRoot "vnext\src\Wevito.VNext.Shell\bin\$Configuration\net8.0-windows\Wevito.VNext.Shell.exe"
$OutputRoot = Join-Path $ProjectRoot "vnext\artifacts\screenshots"
$RunStamp = Get-Date -Format "yyyyMMdd-HHmmss-fff"
$OutputDir = Join-Path $OutputRoot $RunStamp
$DataDir = Join-Path $OutputDir "data"
$TraceDir = Join-Path $OutputDir "trace"
$SpritePreviewDir = Join-Path $OutputDir "sprite-previews"

function Stop-WevitoProcesses {
    $processNames = @("Wevito.VNext.Shell", "Wevito.VNext.Broker")
    foreach ($name in $processNames) {
        Get-Process -Name $name -ErrorAction SilentlyContinue | ForEach-Object {
            try {
                Stop-Process -Id $_.Id -Force -ErrorAction Stop
            }
            catch {
                Write-Warning "Could not stop $name ($($_.Id)): $($_.Exception.Message)"
            }
        }
    }
    Start-Sleep -Milliseconds 350
}

function Minimize-DesktopWindows {
    try {
        $shell = New-Object -ComObject Shell.Application
        $shell.MinimizeAll()
        [System.Runtime.InteropServices.Marshal]::ReleaseComObject($shell) | Out-Null
    }
    catch {
        Write-Warning "Could not minimize desktop windows: $($_.Exception.Message)"
    }
    Start-Sleep -Milliseconds 450
}

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

Stop-WevitoProcesses
Minimize-DesktopWindows

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
New-Item -ItemType Directory -Force -Path $DataDir | Out-Null
New-Item -ItemType Directory -Force -Path $TraceDir | Out-Null
New-Item -ItemType Directory -Force -Path $SpritePreviewDir | Out-Null

if ($PreferAuthored) {
    & python $SpritePreviewScript --sprite-root (Join-Path $ProjectRoot "sprites_runtime") --authored-root $AuthoredRoot --output-root $SpritePreviewDir --prefer-authored
}
else {
    & python $SpritePreviewScript --sprite-root (Join-Path $ProjectRoot "sprites_runtime") --authored-root $AuthoredRoot --output-root $SpritePreviewDir
}
if ($LASTEXITCODE -ne 0) {
    throw "Sprite preview generation failed with exit code $LASTEXITCODE"
}

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

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

function Get-ToolWindowInfo {
    param(
        [int]$ProcessId,
        [int]$TimeoutMs = 5000
    )

    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    while ((Get-Date) -lt $deadline) {
        $windows = [WevitoVisualNative]::GetWindowsForProcess($ProcessId)
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
        try {
            $root = [System.Windows.Automation.AutomationElement]::FromHandle($Handle)
        }
        catch {
            Start-Sleep -Milliseconds 150
            continue
        }

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

            try {
                $match = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
            }
            catch {
                Start-Sleep -Milliseconds 150
                continue
            }
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
        try {
            $root = [System.Windows.Automation.AutomationElement]::FromHandle($Handle)
        }
        catch {
            Start-Sleep -Milliseconds 150
            continue
        }

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

            try {
                $match = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
            }
            catch {
                Start-Sleep -Milliseconds 150
                continue
            }
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
        $flattened = New-Object System.Drawing.Bitmap $Width, $Height
        $flattenedGraphics = [System.Drawing.Graphics]::FromImage($flattened)
        try {
            $flattenedGraphics.Clear([System.Drawing.Color]::FromArgb(0x55, 0x66, 0x6D))
            $flattenedGraphics.DrawImage($bitmap, 0, 0, $Width, $Height)
            $directory = [System.IO.Path]::GetDirectoryName($Path)
            if (-not [string]::IsNullOrWhiteSpace($directory)) {
                [System.IO.Directory]::CreateDirectory($directory) | Out-Null
            }
            $tempDirectory = if ([string]::IsNullOrWhiteSpace($directory)) { $PWD.Path } else { $directory }
            $tempPath = Join-Path $tempDirectory ([System.IO.Path]::GetRandomFileName() + ".png")
            $saved = $false
            $attempt = 0
            while (-not $saved -and $attempt -lt 3) {
                $attempt += 1
                try {
                    if ([System.IO.File]::Exists($Path)) {
                        [System.IO.File]::Delete($Path)
                    }
                    $flattened.Save($tempPath, [System.Drawing.Imaging.ImageFormat]::Png)
                    [System.IO.File]::Move($tempPath, $Path)
                    $saved = $true
                }
                catch {
                    if ([System.IO.File]::Exists($tempPath)) {
                        [System.IO.File]::Delete($tempPath)
                    }
                    if ($attempt -ge 3) {
                        throw
                    }
                    Start-Sleep -Milliseconds 180
                }
            }
        }
        finally {
            $flattenedGraphics.Dispose()
            $flattened.Dispose()
        }
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Save-WindowCapture {
    param(
        [string]$Name,
        [int]$ProcessId,
        [string]$Title
    )

    $WindowInfo = Get-WindowInfo -ProcessId $ProcessId -Title $Title -TimeoutMs 5000
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
        return Get-ToolWindowInfo -ProcessId $ProcessId -TimeoutMs 1800
    }
    catch {
        for ($attempt = 0; $attempt -lt 3; $attempt++) {
            Set-WindowForeground -WindowInfo $HomeWindow
            Start-Sleep -Milliseconds 150

            if (Invoke-WindowButtonById -WindowInfo $HomeWindow -AutomationIds @("LinkBinTabButton") -Names @("LINK BIN", "LINK BIN ACTIVE")) {
                try {
                    return Get-ToolWindowInfo -ProcessId $ProcessId -TimeoutMs 2200
                }
                catch {
                    Start-Sleep -Milliseconds 250
                }
            }

            if (-not (Invoke-WindowButtonById -WindowInfo $HomeWindow -AutomationIds @("WebToolsButton") -Names @("TOOLS", "HIDE"))) {
                Start-Sleep -Milliseconds 300
                continue
            }

            Start-Sleep -Milliseconds 250

            if (-not (Invoke-WindowButtonById -WindowInfo $HomeWindow -AutomationIds @("LinkBinTabButton") -Names @("LINK BIN", "LINK BIN ACTIVE"))) {
                Start-Sleep -Milliseconds 300
                continue
            }

            try {
                return Get-ToolWindowInfo -ProcessId $ProcessId -TimeoutMs 4000
            }
            catch {
                Start-Sleep -Milliseconds 350
            }
        }

        throw "Failed to open link bin tab."
    }
}

function Open-NeutralBackdrop {
    $screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $form = New-Object System.Windows.Forms.Form
    $form.StartPosition = [System.Windows.Forms.FormStartPosition]::Manual
    $form.Location = New-Object System.Drawing.Point($screen.Left, $screen.Top)
    $form.Size = New-Object System.Drawing.Size($screen.Width, $screen.Height)
    $form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::None
    $form.ShowInTaskbar = $false
    $form.BackColor = [System.Drawing.Color]::FromArgb(0x55, 0x66, 0x6D)
    $form.TopMost = $true
    $form.Show()
    Start-Sleep -Milliseconds 250
    return $form
}

function Start-ShellProcess {
    param(
        [string]$ExePath,
        [string]$ScenarioPath = ""
    )

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $ExePath
    $psi.WorkingDirectory = Split-Path -Parent $ExePath
    $psi.UseShellExecute = $false
    $psi.Environment["WEVITO_VNEXT_DATA_ROOT"] = $DataDir
    $psi.Environment["WEVITO_VNEXT_TRACE_DIR"] = $TraceDir
    if (-not [string]::IsNullOrWhiteSpace($ScenarioPath)) {
        $psi.Environment["WEVITO_VNEXT_SCENARIO_PATH"] = $ScenarioPath
    }
    return [System.Diagnostics.Process]::Start($psi)
}

function Wait-ForFocusSettle {
    param(
        [int]$Milliseconds = 900
    )

    Start-Sleep -Milliseconds $Milliseconds
}

$shellProcess = $null
$notepadProcess = $null
$backdropWindow = $null

try {
    $scenario = $null
    if (-not [string]::IsNullOrWhiteSpace($ScenarioPath) -and (Test-Path $ScenarioPath)) {
        try {
            $scenario = Get-Content -Path $ScenarioPath -Raw | ConvertFrom-Json
        }
        catch {
            $scenario = $null
        }
    }

    $backdropWindow = Open-NeutralBackdrop
    $shellProcess = Start-ShellProcess -ExePath $ShellExe -ScenarioPath $ScenarioPath
    Start-Sleep -Milliseconds $StartupDelayMs

    $homeWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito Home Panel"
    $roamWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito Roam Band"

    Set-WindowForeground -WindowInfo $homeWindow
    Wait-ForFocusSettle
    $homeWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito Home Panel"
    $focusedDesktop = Save-DesktopCapture -Name "01-focused-desktop"
    $focusedHome = Save-WindowCapture -Name "02-focused-home" -ProcessId $shellProcess.Id -Title "Wevito Home Panel"
    $focusedToolDesktop = $null
    $focusedToolPopup = $null
    if ($null -ne $scenario -and
        $scenario.toolIsOpen -eq $true -and
        -not [string]::IsNullOrWhiteSpace([string]$scenario.toolId)) {
        try {
            $scenarioToolWindow = Get-ToolWindowInfo -ProcessId $shellProcess.Id -TimeoutMs 2500
            $focusedToolDesktop = Save-DesktopCapture -Name "02a-focused-tool-desktop"
            $focusedToolPopup = Save-WindowCapture -Name "02b-focused-tool-popup" -ProcessId $shellProcess.Id -Title $scenarioToolWindow.Title
        }
        catch {
            $focusedToolDesktop = $null
            $focusedToolPopup = $null
        }
    }

    $notepadProcess = Start-Process -FilePath "notepad.exe" -PassThru
    Start-Sleep -Milliseconds 1000
    $notepadWindow = Get-WindowInfo -ProcessId $notepadProcess.Id -TimeoutMs 15000
    Set-WindowForeground -WindowInfo $notepadWindow

    $passiveDesktop = Save-DesktopCapture -Name "03-passive-desktop"
    $passiveRoam = Save-WindowCapture -Name "04-passive-roam-band" -ProcessId $shellProcess.Id -Title "Wevito Roam Band"

    Set-ClipboardWithRetry -Value "https://openai.com/"
    Invoke-GlobalHotkey -Keys "^+p"
    $pinnedDesktop = Save-DesktopCapture -Name "05-pinned-desktop"
    $pinnedHome = Save-WindowCapture -Name "06-pinned-home" -ProcessId $shellProcess.Id -Title "Wevito Home Panel"

    $basketDesktop = $null
    $basketPopup = $null
    $basketWindow = $null
    if (-not $SkipBasket) {
        Invoke-GlobalHotkey -Keys "^+b"
        $homeWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito Home Panel"
        Set-WindowForeground -WindowInfo $homeWindow
        $basketWindow = Open-BasketWindow -HomeWindow $homeWindow -ProcessId $shellProcess.Id
        $basketDesktop = Save-DesktopCapture -Name "07-basket-desktop"
        $basketPopup = Save-WindowCapture -Name "08-basket-popup" -ProcessId $shellProcess.Id -Title $basketWindow.Title
    }

    $devDesktop = $null
    $devPopup = $null
    $devWindow = $null
    if (-not $SkipDevTools) {
        Invoke-GlobalHotkey -Keys "^+d"
        Start-Sleep -Milliseconds 700
        try {
            $devWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito Dev Tools" -TimeoutMs 2500
        }
        catch {
            $devWindow = $null
        }
        if ($null -ne $devWindow) {
            $devDesktop = Save-DesktopCapture -Name "09-dev-desktop"
            $devPopup = Save-WindowCapture -Name "10-dev-popup" -ProcessId $shellProcess.Id -Title $devWindow.Title
        }
    }

    $homeWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito Home Panel"
    $roamWindow = Get-WindowInfo -ProcessId $shellProcess.Id -Title "Wevito Roam Band"
    try {
        $activeToolWindow = Get-ToolWindowInfo -ProcessId $shellProcess.Id -TimeoutMs 1200
    }
    catch {
        $activeToolWindow = $null
    }

    $windowSummaries = @(
        [ordered]@{ title = $homeWindow.Title; left = $homeWindow.Left; top = $homeWindow.Top; width = $homeWindow.Width; height = $homeWindow.Height },
        [ordered]@{ title = $roamWindow.Title; left = $roamWindow.Left; top = $roamWindow.Top; width = $roamWindow.Width; height = $roamWindow.Height }
    )
    if ($null -ne $activeToolWindow) {
        $windowSummaries += [ordered]@{ title = $activeToolWindow.Title; left = $activeToolWindow.Left; top = $activeToolWindow.Top; width = $activeToolWindow.Width; height = $activeToolWindow.Height }
    }

    $summary = [ordered]@{
        captured_at = (Get-Date).ToString("s")
        output_dir = $OutputDir
        shell_pid = $shellProcess.Id
        notepad_pid = if ($null -ne $notepadProcess) { $notepadProcess.Id } else { $null }
        windows = $windowSummaries
        screenshots = [ordered]@{
            focused_desktop = $focusedDesktop
            focused_home = $focusedHome
            focused_tool_desktop = $focusedToolDesktop
            focused_tool_popup = $focusedToolPopup
            passive_desktop = $passiveDesktop
            passive_roam_band = $passiveRoam
            pinned_desktop = $pinnedDesktop
            pinned_home = $pinnedHome
            basket_desktop = $basketDesktop
            basket_popup = $basketPopup
            dev_desktop = $devDesktop
            dev_popup = $devPopup
        }
        sprite_previews = [ordered]@{
            output_dir = $SpritePreviewDir
            index = (Join-Path $SpritePreviewDir "index.txt")
        }
    }

    $summaryPath = Join-Path $OutputDir "summary.json"
    $summary | ConvertTo-Json -Depth 6 | Set-Content -Path $summaryPath -Encoding UTF8
    Write-Output "summary=$summaryPath"
}
finally {
    if ($null -ne $backdropWindow) {
        try {
            $backdropWindow.Close()
            $backdropWindow.Dispose()
        }
        catch {
        }
    }

    if ($null -ne $notepadProcess -and -not $notepadProcess.HasExited) {
        Stop-Process -Id $notepadProcess.Id -Force
    }

    if ($null -ne $shellProcess -and -not $shellProcess.HasExited) {
        Stop-Process -Id $shellProcess.Id -Force
    }
}
