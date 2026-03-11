param(
    [string]$ExePath = "",
    [string]$ExeArgs = "",
    [int]$LaunchWaitSeconds = 6,
    [int]$RoamSampleSeconds = 8
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
if ($ExePath -eq "") {
    $ExePath = Join-Path $projectRoot "builds\release\WevitoDesktopPet-latest-win64.exe"
}

if (-not (Test-Path $ExePath)) {
    throw "Missing executable: $ExePath"
}

Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class DesktopProbeNative {
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam);
}
"@

$MOUSEEVENTF_LEFTDOWN = 0x0002
$MOUSEEVENTF_LEFTUP = 0x0004
$WM_LBUTTONDOWN = 0x0201
$WM_LBUTTONUP = 0x0202

function Get-ProfilePaths {
    param([string]$AppDataRoot)

    $userDataRoot = Join-Path $AppDataRoot "Godot\app_userdata\Wevito"
    @{
        UserDataRoot = $userDataRoot
        SavePath = Join-Path $userDataRoot "save_slot.json"
        ReportPath = Join-Path $userDataRoot "automation_report.json"
        CommandPath = Join-Path $userDataRoot "overlay_command.json"
        RuntimeStatePath = Join-Path $userDataRoot "runtime_state.json"
    }
}

function Start-IsolatedWevito {
    param(
        [string]$Path,
        [string]$AppDataRoot,
        [hashtable]$EnvVars,
        [string]$Arguments = ""
    )

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $Path
    $psi.WorkingDirectory = Split-Path -Parent $Path
    $psi.Arguments = $Arguments
    $psi.UseShellExecute = $false
    $psi.EnvironmentVariables["APPDATA"] = $AppDataRoot
    foreach ($key in $EnvVars.Keys) {
        $psi.EnvironmentVariables[$key] = [string]$EnvVars[$key]
    }
    [System.Diagnostics.Process]::Start($psi)
}

function Wait-ForSaveFile {
    param(
        [string]$SavePath,
        [int]$TimeoutSeconds = 20
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $SavePath) {
            return
        }
        Start-Sleep -Milliseconds 250
    }
    throw "Timed out waiting for save file: $SavePath"
}

function Read-SaveJson {
    param([string]$SavePath)

    if (-not (Test-Path $SavePath)) {
        return $null
    }
    $raw = Get-Content -Path $SavePath -Raw
    if ($raw.Trim() -eq "") {
        return $null
    }
    $raw | ConvertFrom-Json
}

function Read-RuntimeState {
    param(
        [string]$ProjectRoot,
        [string]$AppDataRoot,
        [string]$RuntimeStatePath
    )

    if (Test-Path $RuntimeStatePath) {
        Remove-Item -Path $RuntimeStatePath -Force
    }

    Invoke-OverlayCommand -ProjectRoot $ProjectRoot -AppDataRoot $AppDataRoot -ScriptName "dump-runtime-state.ps1"
    $deadline = (Get-Date).AddSeconds(5)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $RuntimeStatePath) {
            $raw = Get-Content -Path $RuntimeStatePath -Raw
            if ($raw.Trim() -ne "") {
                return ($raw | ConvertFrom-Json)
            }
        }
        Start-Sleep -Milliseconds 250
    }
    throw "Timed out waiting for runtime state dump."
}

function Wait-ForMainWindowHandle {
    param(
        [System.Diagnostics.Process]$Process,
        [int]$TimeoutSeconds = 20
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $Process.Refresh()
        if ($Process.MainWindowHandle -ne 0) {
            return [IntPtr]$Process.MainWindowHandle
        }
        Start-Sleep -Milliseconds 250
    }
    throw "Timed out waiting for window handle."
}

function Get-ForegroundProcessId {
    $hwnd = [DesktopProbeNative]::GetForegroundWindow()
    if ($hwnd -eq [IntPtr]::Zero) {
        return 0
    }
    $windowPid = [uint32]0
    [DesktopProbeNative]::GetWindowThreadProcessId($hwnd, [ref]$windowPid) | Out-Null
    return [int]$windowPid
}

function Get-WindowRectInfo {
    param([IntPtr]$Handle)

    $rect = New-Object DesktopProbeNative+RECT
    if (-not [DesktopProbeNative]::GetWindowRect($Handle, [ref]$rect)) {
        throw "GetWindowRect failed."
    }
    [PSCustomObject]@{
        Left = $rect.Left
        Top = $rect.Top
        Width = $rect.Right - $rect.Left
        Height = $rect.Bottom - $rect.Top
    }
}

function Get-MaxPetXDelta {
    param(
        $BeforeSave,
        $AfterSave
    )

    if ($null -eq $BeforeSave -or $null -eq $AfterSave) {
        return 0.0
    }
    $beforePets = @($BeforeSave.pets)
    $afterPets = @($AfterSave.pets)
    $maxDelta = 0.0
    for ($i = 0; $i -lt [Math]::Min($beforePets.Count, $afterPets.Count); $i++) {
        $beforeX = [double]$beforePets[$i].position.x
        $afterX = [double]$afterPets[$i].position.x
        $delta = [Math]::Abs($afterX - $beforeX)
        if ($delta -gt $maxDelta) {
            $maxDelta = $delta
        }
    }
    return [Math]::Round($maxDelta, 2)
}

function Invoke-OverlayCommand {
    param(
        [string]$ProjectRoot,
        [string]$AppDataRoot,
        [string]$ScriptName,
        [string[]]$Arguments = @()
    )

    $oldAppData = $env:APPDATA
    try {
        $env:APPDATA = $AppDataRoot
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $ProjectRoot ("tools\" + $ScriptName)) @Arguments | Out-Null
    }
    finally {
        $env:APPDATA = $oldAppData
    }
}

function Invoke-LeftClick {
    param(
        [int]$X,
        [int]$Y,
        [int]$SettleMs = 350
    )

    [DesktopProbeNative]::SetCursorPos($X, $Y) | Out-Null
    Start-Sleep -Milliseconds 120
    [DesktopProbeNative]::mouse_event($MOUSEEVENTF_LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 50
    [DesktopProbeNative]::mouse_event($MOUSEEVENTF_LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $SettleMs
}

function Invoke-WindowClick {
    param(
        [IntPtr]$Handle,
        [int]$X,
        [int]$Y,
        [int]$SettleMs = 350
    )

    $lParamValue = (($Y -band 0xFFFF) -shl 16) -bor ($X -band 0xFFFF)
    $lParam = [IntPtr]::new($lParamValue)
    [DesktopProbeNative]::PostMessage($Handle, $WM_LBUTTONDOWN, [UIntPtr]::new(1), $lParam) | Out-Null
    Start-Sleep -Milliseconds 60
    [DesktopProbeNative]::PostMessage($Handle, $WM_LBUTTONUP, [UIntPtr]::Zero, $lParam) | Out-Null
    Start-Sleep -Milliseconds $SettleMs
}

function Clamp-Value {
    param(
        [double]$Value,
        [double]$Min,
        [double]$Max
    )

    return [Math]::Max($Min, [Math]::Min($Max, $Value))
}

function Get-OverlayPanelRect {
    param(
        [int]$WindowWidth,
        [int]$WindowHeight,
        [int]$PetCount
    )

    $count = [Math]::Max(1, $PetCount)
    $desktopStageW = Clamp-Value -Value ($WindowWidth * 0.06) -Min 120.0 -Max 180.0
    $desktopTotalW = ($desktopStageW * $count) + (10.0 * [Math]::Max(0, $count - 1))
    $panelW = [Math]::Min([double]$WindowWidth, [Math]::Max(320.0, $desktopTotalW + 20.0 + 16.0))
    $panelH = [Math]::Min([double]$WindowHeight, 420.0)
    [PSCustomObject]@{
        Left = [int][Math]::Round([Math]::Max(0.0, $WindowWidth - $panelW - 20.0))
        Top = [int][Math]::Round([Math]::Max(0.0, $WindowHeight - $panelH - 20.0))
        Width = [int][Math]::Round($panelW)
        Height = [int][Math]::Round($panelH)
    }
}

function Get-PinnedBasketButtonCenter {
    param(
        [pscustomobject]$WindowRect,
        [int]$PetCount
    )

    $panel = Get-OverlayPanelRect -WindowWidth $WindowRect.Width -WindowHeight $WindowRect.Height -PetCount $PetCount
    $iconWidth = 28
    $basketWidth = 40
    $iconGap = 4
    $rightX = $panel.Left + $panel.Width - 10 - $iconWidth
    $secondaryX = $rightX - $basketWidth - $iconGap
    [PSCustomObject]@{
        ScreenX = $WindowRect.Left + $secondaryX + [int]($basketWidth / 2)
        ScreenY = $WindowRect.Top + $panel.Top + 6 + 12
        ClientX = $secondaryX + [int]($basketWidth / 2)
        ClientY = $panel.Top + 6 + 12
    }
}

function Get-BasketCaptureButtonCenter {
    param([pscustomobject]$WindowRect)

    $cardLeft = $WindowRect.Left + [int][Math]::Round(($WindowRect.Width - 292.0) * 0.5)
    $cardTop = $WindowRect.Top + [int][Math]::Round(($WindowRect.Height - 336.0) * 0.5)
    [PSCustomObject]@{
        ScreenX = $cardLeft + 12 + 68
        ScreenY = $cardTop + 74 + 12
        ClientX = ($cardLeft - $WindowRect.Left) + 12 + 68
        ClientY = ($cardTop - $WindowRect.Top) + 74 + 12
    }
}

function Get-RectCenter {
    param(
        $Rect,
        $WindowPosition = $null
    )

    if ($null -eq $Rect) {
        return $null
    }
    $offsetX = 0.0
    $offsetY = 0.0
    if ($null -ne $WindowPosition) {
        $offsetX = [double]$WindowPosition.x
        $offsetY = [double]$WindowPosition.y
    }
    [PSCustomObject]@{
        ClientX = [int][Math]::Round([double]$Rect.x + ([double]$Rect.w * 0.5))
        ClientY = [int][Math]::Round([double]$Rect.y + ([double]$Rect.h * 0.5))
        ScreenX = [int][Math]::Round($offsetX + [double]$Rect.x + ([double]$Rect.w * 0.5))
        ScreenY = [int][Math]::Round($offsetY + [double]$Rect.y + ([double]$Rect.h * 0.5))
    }
}

$probeRoot = Join-Path $env:TEMP ("wevito-desktop-probe-" + [guid]::NewGuid().ToString("N"))
$appDataRoot = Join-Path $probeRoot "AppData\Roaming"
New-Item -ItemType Directory -Path $appDataRoot -Force | Out-Null
$paths = Get-ProfilePaths -AppDataRoot $appDataRoot
New-Item -ItemType Directory -Path $paths.UserDataRoot -Force | Out-Null

# Seed a realistic save with automation first.
$seed = Start-IsolatedWevito -Path $ExePath -AppDataRoot $appDataRoot -EnvVars @{
    WEVITO_AUTOMATION = "1"
    WEVITO_AUTOMATION_SCENARIO = "desktop_probe_seed"
} -Arguments $ExeArgs
if (-not $seed.WaitForExit(45000)) {
    $seed.Kill()
    $seed.WaitForExit()
    throw "Seed automation timed out."
}
if ($seed.ExitCode -ne 0) {
    throw "Seed automation failed with exit code $($seed.ExitCode)."
}

$app = Start-IsolatedWevito -Path $ExePath -AppDataRoot $appDataRoot -EnvVars @{} -Arguments $ExeArgs
try {
    Start-Sleep -Seconds $LaunchWaitSeconds
    Wait-ForSaveFile -SavePath $paths.SavePath -TimeoutSeconds 20
    $appHandle = Wait-ForMainWindowHandle -Process $app -TimeoutSeconds 20
    $initialRect = Get-WindowRectInfo -Handle $appHandle
    $initialSave = Read-SaveJson -SavePath $paths.SavePath

    $notepad = Start-Process -FilePath "notepad.exe" -PassThru
    try {
        Start-Sleep -Seconds 1
        $null = [DesktopProbeNative]::SetForegroundWindow([IntPtr]$notepad.MainWindowHandle)
        Start-Sleep -Seconds 2

        $foregroundPidAfterNotepad = Get-ForegroundProcessId
        $unfocusedRect = Get-WindowRectInfo -Handle $appHandle
        $saveBeforeRoam = Read-SaveJson -SavePath $paths.SavePath
        Start-Sleep -Seconds $RoamSampleSeconds
        $saveAfterRoam = Read-SaveJson -SavePath $paths.SavePath
        $roamDelta = Get-MaxPetXDelta -BeforeSave $saveBeforeRoam -AfterSave $saveAfterRoam

        Set-Clipboard -Value "https://example.com/from-desktop-probe"
        Invoke-OverlayCommand -ProjectRoot $projectRoot -AppDataRoot $appDataRoot -ScriptName "capture-basket-link.ps1"
        Start-Sleep -Seconds 2
        $afterCaptureSave = Read-SaveJson -SavePath $paths.SavePath
        $capturedLinks = @($afterCaptureSave.link_basket)

        Invoke-OverlayCommand -ProjectRoot $projectRoot -AppDataRoot $appDataRoot -ScriptName "toggle-overlay-ui.ps1" -Arguments @("-Command", "pin")
        Start-Sleep -Seconds 1
        $foregroundPidAfterPin = Get-ForegroundProcessId

        Set-Clipboard -Value "https://example.com/from-pinned-click"
        $runtimeStateBeforeClick = Read-RuntimeState -ProjectRoot $projectRoot -AppDataRoot $appDataRoot -RuntimeStatePath $paths.RuntimeStatePath
        $basketButtonCenter = Get-RectCenter -Rect $runtimeStateBeforeClick.basket_button -WindowPosition $runtimeStateBeforeClick.window_position
        if ($null -eq $basketButtonCenter) {
            throw "Runtime state did not include basket button coordinates."
        }
        Invoke-LeftClick -X $basketButtonCenter.ScreenX -Y $basketButtonCenter.ScreenY -SettleMs 700
        $runtimeStateAfterBasketClick = Read-RuntimeState -ProjectRoot $projectRoot -AppDataRoot $appDataRoot -RuntimeStatePath $paths.RuntimeStatePath
        $foregroundPidAfterBasketClick = Get-ForegroundProcessId
        $captureButtonCenter = Get-RectCenter -Rect $runtimeStateAfterBasketClick.basket_capture_button -WindowPosition $runtimeStateAfterBasketClick.window_position
        $afterPinnedClickSave = Read-SaveJson -SavePath $paths.SavePath
        $foregroundPidAfterPinnedClick = $foregroundPidAfterBasketClick
        if ([bool]$runtimeStateAfterBasketClick.basket_overlay_visible -and $null -ne $captureButtonCenter) {
            Invoke-LeftClick -X $captureButtonCenter.ScreenX -Y $captureButtonCenter.ScreenY -SettleMs 700
            Start-Sleep -Seconds 2
            $afterPinnedClickSave = Read-SaveJson -SavePath $paths.SavePath
            $foregroundPidAfterPinnedClick = Get-ForegroundProcessId
        }

        $null = [DesktopProbeNative]::SetForegroundWindow([IntPtr]$notepad.MainWindowHandle)
        Start-Sleep -Seconds 1

        Invoke-OverlayCommand -ProjectRoot $projectRoot -AppDataRoot $appDataRoot -ScriptName "toggle-overlay-ui.ps1" -Arguments @("-Command", "release")
        Start-Sleep -Seconds 1
        $foregroundPidAfterRelease = Get-ForegroundProcessId

        $result = [PSCustomObject]@{
            SavePath = $paths.SavePath
            InitialRect = $initialRect
            UnfocusedRect = $unfocusedRect
            ForegroundStayedOnNotepad = ($foregroundPidAfterNotepad -eq $notepad.Id)
            PinDidNotStealFocus = ($foregroundPidAfterPin -eq $notepad.Id)
            PinnedClickFocusedGame = ($foregroundPidAfterPinnedClick -eq $app.Id)
            BasketOverlayOpenedFromPinnedClick = [bool]$runtimeStateAfterBasketClick.basket_overlay_visible
            BasketClickForegroundPid = $foregroundPidAfterBasketClick
            ReleaseDidNotStealFocus = ($foregroundPidAfterRelease -eq $notepad.Id)
            RoamDelta = $roamDelta
            BasketCount = @($afterPinnedClickSave.link_basket).Count
            BasketContainsProbeLink = ($capturedLinks | Where-Object { $_.url -eq "https://example.com/from-desktop-probe" } | Measure-Object).Count -gt 0
            BasketContainsPinnedClickLink = (@($afterPinnedClickSave.link_basket) | Where-Object { $_.url -eq "https://example.com/from-pinned-click" } | Measure-Object).Count -gt 0
        }

        $result

        if (-not $result.ForegroundStayedOnNotepad) {
            throw "Foreground focus did not stay on Notepad after switching away from Wevito."
        }
        if (-not $result.PinDidNotStealFocus) {
            throw "Pinned HUD stole focus unexpectedly."
        }
        if (-not $result.ReleaseDidNotStealFocus) {
            throw "Release command stole focus unexpectedly."
        }
        if ($result.RoamDelta -le 0.0) {
            throw "Pets did not roam while another app was foreground."
        }
        if (-not $result.BasketContainsProbeLink) {
            throw "External basket capture did not persist the clipboard link."
        }
        if (-not $result.BasketOverlayOpenedFromPinnedClick) {
            throw "Pinned HUD basket click did not open the basket overlay."
        }
        if (-not $result.BasketContainsPinnedClickLink) {
            throw "Pinned HUD click interaction did not capture the clipboard link."
        }
    }
    finally {
        if ($null -ne $notepad -and -not $notepad.HasExited) {
            Stop-Process -Id $notepad.Id -Force
        }
    }
}
finally {
    if ($null -ne $app -and -not $app.HasExited) {
        Stop-Process -Id $app.Id -Force
    }
}
