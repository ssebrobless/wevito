param(
    [string]$Species = "rat",
    [string]$Age = "adult",
    [string]$Gender = "male",
    [string]$Family = "",
    [string]$WindowTitle = "Google Gemini - Google Chrome",
    [int]$GenerationTimeoutSeconds = 360,
    [string]$AuthUser = "wbogusz24@gmail.com",
    [switch]$AllowLaunchFallback
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class GeminiLiveNative
{
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
}
"@

$MOUSEEVENTF_LEFTDOWN = 0x0002
$MOUSEEVENTF_LEFTUP = 0x0004

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$PrepareScript = if ([string]::IsNullOrWhiteSpace($Family)) {
    Join-Path $ProjectRoot "tools\prepare_gemini_handoff.py"
}
else {
    Join-Path $ProjectRoot "tools\prepare_motion_gemini_handoff.py"
}
$HandoffRoot = if ([string]::IsNullOrWhiteSpace($Family)) {
    Join-Path $ProjectRoot "incoming_sprites\gemini_handoff\$Species\$Age\$Gender"
}
else {
    Join-Path $ProjectRoot "incoming_sprites\gemini_handoff_motion\$Species\$Age\$Gender\$Family"
}
$HandoffDir = $HandoffRoot
$PromptPath = Join-Path $HandoffDir "4-prompt.txt"
$UploadPackPath = Join-Path $HandoffDir "1-upload-pack.png"
$SaveDir = Join-Path $HandoffDir "5-save-edited-board-here"
$DownloadsDir = Join-Path ([Environment]::GetFolderPath('UserProfile')) "Downloads"
$ChromePath = @(
    "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
    "C:\Program Files\Google\Chrome\Application\chrome.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($Family)) {
    python $PrepareScript --species $Species | Out-Null
}
else {
    python $PrepareScript --species $Species --family $Family | Out-Null
}

if (-not (Test-Path $PromptPath)) {
    throw "Missing prompt file: $PromptPath"
}
if (-not (Test-Path $UploadPackPath)) {
    throw "Missing upload pack image: $UploadPackPath"
}

function Get-GeminiProcess {
    $process = Get-Process chrome -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowTitle -like "*Google Gemini*" } |
        Select-Object -First 1
    if (-not $process) {
        if (-not $AllowLaunchFallback) {
            throw "Gemini Chrome window not found. Reuse the already-open logged-in Gemini window, or rerun with -AllowLaunchFallback."
        }
        if (-not $ChromePath) {
            throw "Gemini Chrome window not found and Chrome executable was not located."
        }
        $targetUrl = "https://gemini.google.com/?authuser=$([uri]::EscapeDataString($AuthUser))"
        & $ChromePath --new-window $targetUrl | Out-Null
        for ($attempt = 0; $attempt -lt 20 -and -not $process; $attempt++) {
            Start-Sleep -Seconds 1
            $process = Get-Process chrome -ErrorAction SilentlyContinue |
                Where-Object { $_.MainWindowTitle -like "*Google Gemini*" } |
                Select-Object -First 1
        }
    }
    if (-not $process) {
        throw "Gemini Chrome window not found: $WindowTitle"
    }
    return $process
}

function Focus-GeminiWindow {
    param([System.Diagnostics.Process]$Process)
    [void][GeminiLiveNative]::ShowWindow($Process.MainWindowHandle, 5)
    [void][GeminiLiveNative]::SetForegroundWindow($Process.MainWindowHandle)
    Start-Sleep -Milliseconds 500
}

function Close-TransientOverlays {
    $process = Get-GeminiProcess
    Focus-GeminiWindow $process
    for ($attempt = 0; $attempt -lt 3; $attempt++) {
        [System.Windows.Forms.SendKeys]::SendWait('{ESC}')
        Start-Sleep -Milliseconds 250
    }
}

function Has-PromptEdit {
    try {
        [void](Get-PromptEdit)
        return $true
    }
    catch {
        return $false
    }
}

function Get-GeminiRoot {
    $process = Get-GeminiProcess
    Focus-GeminiWindow $process
    return [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
}

function Find-FirstByName {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name
    )
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $Name
    )
    return $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Find-FirstByNames {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string[]]$Names
    )
    foreach ($name in $Names) {
        $element = Find-FirstByName -Root $Root -Name $name
        if ($element) {
            return $element
        }
    }
    return $null
}

function Find-AllByRegex {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Pattern
    )
    $results = New-Object System.Collections.ArrayList
    $all = $Root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    for ($index = 0; $index -lt $all.Count; $index++) {
        $element = $all.Item($index)
        $name = $element.Current.Name
        $class = $element.Current.ClassName
        $type = $element.Current.ControlType.ProgrammaticName
        if ("$name $class $type" -match $Pattern) {
            [void]$results.Add([object]$element)
        }
    }
    return @($results)
}

function Find-FirstByRegex {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Pattern
    )
    $matches = Find-AllByRegex -Root $Root -Pattern $Pattern
    if ($matches.Count -gt 0) {
        return $matches[0]
    }
    return $null
}

function Get-VisibleElementsByName {
    param([string]$Name)
    $root = Get-GeminiRoot
    $items = @()
    $all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    for ($index = 0; $index -lt $all.Count; $index++) {
        $element = $all.Item($index)
        if ($element.Current.Name -eq $Name -and -not $element.Current.IsOffscreen) {
            $items += $element
        }
    }
    return $items
}

function Wait-ForElementByName {
    param(
        [string]$Name,
        [int]$TimeoutMs = 10000
    )
    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    while ((Get-Date) -lt $deadline) {
        $root = Get-GeminiRoot
        $element = Find-FirstByName -Root $root -Name $Name
        if ($element) {
            return $element
        }
        Start-Sleep -Milliseconds 250
    }
    throw "Timed out waiting for Gemini element '$Name'."
}

function Ensure-ComposerReady {
    Close-TransientOverlays
    $root = Get-GeminiRoot
    $newChat = Find-FirstByName -Root $root -Name 'New chat'
    if ($newChat -and -not $newChat.Current.IsOffscreen -and $newChat.Current.IsEnabled) {
        if (-not (Try-InvokeElement -Element $newChat)) {
            Click-ElementCenter -Element $newChat
        }
        Start-Sleep -Milliseconds 700
        Close-TransientOverlays
    }
    for ($attempt = 0; $attempt -lt 6; $attempt++) {
        if (Has-PromptEdit) {
            return
        }

        $closeButtons = Get-VisibleElementsByName -Name 'Close'
        if ($closeButtons.Count -gt 0) {
            if (-not (Try-InvokeElement -Element $closeButtons[-1])) {
                Click-ElementCenter -Element $closeButtons[-1]
            }
            Start-Sleep -Milliseconds 400
            continue
        }

        [System.Windows.Forms.SendKeys]::SendWait('{ESC}')
        Start-Sleep -Milliseconds 300
    }

    if (-not (Has-PromptEdit)) {
        throw "Gemini composer did not become ready."
    }
}

function Invoke-Element {
    param([System.Windows.Automation.AutomationElement]$Element)
    $invoke = $Element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $invoke.Invoke()
}

function Try-InvokeElement {
    param([System.Windows.Automation.AutomationElement]$Element)
    try {
        Invoke-Element -Element $Element
        return $true
    }
    catch {
        return $false
    }
}

function Get-RemoveButtons {
    $root = Get-GeminiRoot
    return Find-AllByRegex -Root $root -Pattern '^Remove file '
}

function Clear-ExistingAttachments {
    for ($attempt = 0; $attempt -lt 10; $attempt++) {
        $buttons = Get-RemoveButtons
        if ($buttons.Count -eq 0) {
            return
        }

        foreach ($button in $buttons) {
            try {
                $invoke = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
                $invoke.Invoke()
                Start-Sleep -Milliseconds 250
            }
            catch {
            }
        }
    }
}

function Clear-DraftPrompt {
    try {
        $edit = Get-PromptEdit
        $edit.SetFocus()
        Start-Sleep -Milliseconds 150
        [System.Windows.Forms.SendKeys]::SendWait('^a')
        Start-Sleep -Milliseconds 100
        [System.Windows.Forms.SendKeys]::SendWait('{BACKSPACE}')
        Start-Sleep -Milliseconds 200
    }
    catch {
    }
}

function Get-PromptEdit {
    $root = Get-GeminiRoot
    $edit = Find-FirstByNames -Root $root -Names @('Enter a prompt for Gemini', 'Ask Gemini 3')
    if (-not $edit) {
        throw "Prompt edit not found."
    }
    return $edit
}

function Ensure-ImageMode {
    $root = Get-GeminiRoot
    $createImage = Find-FirstByRegex -Root $root -Pattern 'Create image'
    if (-not $createImage) {
        return
    }

    if (-not (Try-InvokeElement -Element $createImage) -and -not $createImage.Current.IsOffscreen) {
        Click-ElementCenter -Element $createImage
    }
    Start-Sleep -Milliseconds 700
}

function Paste-UploadPack {
    param([string]$Path)

    $beforeCount = (Get-RemoveButtons).Count
    $edit = Get-PromptEdit
    $image = [System.Drawing.Image]::FromFile($Path)
    try {
        [System.Windows.Forms.Clipboard]::SetImage($image)
    }
    finally {
        $image.Dispose()
    }

    $edit.SetFocus()
    Start-Sleep -Milliseconds 250
    [System.Windows.Forms.SendKeys]::SendWait('^v')

    $deadline = (Get-Date).AddSeconds(45)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 250
        $afterCount = (Get-RemoveButtons).Count
        if ($afterCount -gt $beforeCount) {
            return
        }
        $root = Get-GeminiRoot
        $attachedPreview = Find-FirstByRegex -Root $root -Pattern 'Remove file |uploaded image|Delete attachment|image preview'
        if ($attachedPreview) {
            return
        }
    }

    throw "Timed out waiting for Gemini to attach upload pack: $Path"
}

function Set-PromptText {
    param([string]$Text)
    $edit = Get-PromptEdit
    [System.Windows.Forms.Clipboard]::SetText($Text)
    $edit.SetFocus()
    Start-Sleep -Milliseconds 150
    [System.Windows.Forms.SendKeys]::SendWait('^a')
    Start-Sleep -Milliseconds 100
    [System.Windows.Forms.SendKeys]::SendWait('^v')
    Start-Sleep -Milliseconds 500
}

function Get-AIGeneratedButtons {
    $root = Get-GeminiRoot
    $items = @()
    $all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
    for ($index = 0; $index -lt $all.Count; $index++) {
        $element = $all.Item($index)
        if ($element.Current.Name -eq ', AI generated') {
            $items += $element
        }
    }
    return $items
}

function Get-ElementsByExactName {
    param([string]$Name)
    $root = Get-GeminiRoot
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $Name
    )
    $collection = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condition)
    $items = @()
    for ($index = 0; $index -lt $collection.Count; $index++) {
        $items += $collection.Item($index)
    }
    return $items
}

function Invoke-Send {
    $deadline = (Get-Date).AddSeconds(5)
    while ((Get-Date) -lt $deadline) {
        $root = Get-GeminiRoot
        $send = Find-FirstByName -Root $root -Name 'Send message'
        if ($send -and -not $send.Current.IsOffscreen) {
            Invoke-Element -Element $send
            Start-Sleep -Milliseconds 250
            return
        }
        Start-Sleep -Milliseconds 250
    }

    $edit = Get-PromptEdit
    $edit.SetFocus()
    Start-Sleep -Milliseconds 150
    [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
    Start-Sleep -Milliseconds 250
}

function Focus-ContentPane {
    $process = Get-GeminiProcess
    Focus-GeminiWindow $process
    [void][GeminiLiveNative]::SetCursorPos(1500, 520)
    Start-Sleep -Milliseconds 100
    [GeminiLiveNative]::mouse_event($MOUSEEVENTF_LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 60
    [GeminiLiveNative]::mouse_event($MOUSEEVENTF_LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 150
}

function Page-DownThread {
    param([int]$Count = 1)
    Focus-ContentPane
    for ($index = 0; $index -lt $Count; $index++) {
        [System.Windows.Forms.SendKeys]::SendWait('{PGDN}')
        Start-Sleep -Milliseconds 250
    }
}

function Jump-ToThreadEnd {
    Focus-ContentPane
    [System.Windows.Forms.SendKeys]::SendWait('^{END}')
    Start-Sleep -Milliseconds 450
    [System.Windows.Forms.SendKeys]::SendWait('{END}')
    Start-Sleep -Milliseconds 450
}

function Wait-ForNewAIGeneratedButton {
    param(
        [int]$BaselineCount,
        [int]$DownloadBaselineCount = 0
    )
    $deadline = (Get-Date).AddSeconds($GenerationTimeoutSeconds)
    $iteration = 0
    while ((Get-Date) -lt $deadline) {
        $iteration++
        if ($iteration -eq 1 -or $iteration % 6 -eq 0) {
            Jump-ToThreadEnd
        }
        Start-Sleep -Seconds 2
        $buttons = Get-AIGeneratedButtons
        if ($buttons.Count -gt $BaselineCount) {
            return $buttons[-1]
        }
        $downloadButtons = Get-ElementsByExactName -Name 'Download full size image'
        if ($downloadButtons.Count -gt $DownloadBaselineCount) {
            return $downloadButtons[-1]
        }
    }
    throw "Timed out waiting for Gemini image generation to finish."
}

function Get-VisiblePointForElement {
    param([System.Windows.Automation.AutomationElement]$Element)
    $process = Get-GeminiProcess
    $window = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    $windowRect = $window.Current.BoundingRectangle
    $rect = $Element.Current.BoundingRectangle

    $padding = 12
    $left = [Math]::Max($rect.Left, $windowRect.Left + $padding)
    $top = [Math]::Max($rect.Top, $windowRect.Top + $padding)
    $right = [Math]::Min($rect.Right, $windowRect.Right - $padding)
    $bottom = [Math]::Min($rect.Bottom, $windowRect.Bottom - $padding)

    if ($right -le $left -or $bottom -le $top) {
        throw "Element is not sufficiently visible to click."
    }

    return @{
        X = [int](($left + $right) / 2)
        Y = [int](($top + $bottom) / 2)
    }
}

function Try-GetVisiblePointForElement {
    param([System.Windows.Automation.AutomationElement]$Element)
    try {
        return Get-VisiblePointForElement -Element $Element
    }
    catch {
        return $null
    }
}

function Click-ElementCenter {
    param([System.Windows.Automation.AutomationElement]$Element)
    $elementName = $Element.Current.Name
    $elementType = $Element.Current.ControlType.ProgrammaticName
    $elementClass = $Element.Current.ClassName
    try {
        $point = Get-VisiblePointForElement -Element $Element
    }
    catch {
        Write-Host "Click fallback for element: '$elementName' [$elementType] class='$elementClass'"
        if (Try-InvokeElement -Element $Element) {
            Write-Host "Invoked element without visible click: '$elementName'"
            return
        }
        Write-Host "Could not invoke non-visible element: '$elementName'"
        throw
    }
    Write-Host "Clicking element: '$elementName' [$elementType] class='$elementClass' at ($($point.X), $($point.Y))"
    $x = $point.X
    $y = $point.Y
    [void][GeminiLiveNative]::SetCursorPos($x, $y)
    Start-Sleep -Milliseconds 120
    [GeminiLiveNative]::mouse_event($MOUSEEVENTF_LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 60
    [GeminiLiveNative]::mouse_event($MOUSEEVENTF_LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
}

function Wait-ForVisibleElementByName {
    param(
        [string]$Name,
        [int]$TimeoutSeconds = 30
    )
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $iteration = 0
    while ((Get-Date) -lt $deadline) {
        $iteration++
        if ($iteration -eq 1 -or $iteration % 6 -eq 0) {
            Jump-ToThreadEnd
        }
        $root = Get-GeminiRoot
        $all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
        for ($index = 0; $index -lt $all.Count; $index++) {
            $element = $all.Item($index)
            if ($element.Current.Name -eq $Name -and -not $element.Current.IsOffscreen) {
                return $element
            }
        }
        Start-Sleep -Milliseconds 250
    }
    throw "Timed out waiting for visible element '$Name'."
}

function Wait-ForAnyElementByName {
    param(
        [string]$Name,
        [int]$TimeoutSeconds = 30
    )
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $iteration = 0
    while ((Get-Date) -lt $deadline) {
        $iteration++
        if ($iteration -eq 1 -or $iteration % 6 -eq 0) {
            Jump-ToThreadEnd
        }
        $items = Get-ElementsByExactName -Name $Name
        if ($items.Count -gt 0) {
            return $items[-1]
        }
        Start-Sleep -Milliseconds 250
    }
    throw "Timed out waiting for element '$Name'."
}

function Reveal-LatestAIGeneratedButton {
    param([int]$PageDownAttempts = 20)
    for ($attempt = 0; $attempt -lt $PageDownAttempts; $attempt++) {
        $buttons = Get-AIGeneratedButtons
        if ($buttons.Count -gt 0) {
            for ($index = $buttons.Count - 1; $index -ge 0; $index--) {
                $candidate = $buttons[$index]
                $point = Try-GetVisiblePointForElement -Element $candidate
                if ($point) {
                    return $candidate
                }
            }
        }
        if ($attempt -eq 4 -or $attempt -eq 10 -or $attempt -eq 15) {
            Jump-ToThreadEnd
        }
        else {
            Page-DownThread -Count 1
        }
    }
    throw "Latest AI generated image did not become visible enough to click."
}

function Wait-ForClipboardImage {
    param([int]$TimeoutSeconds = 15)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if ([System.Windows.Forms.Clipboard]::ContainsImage()) {
            return [System.Windows.Forms.Clipboard]::GetImage()
        }
        Start-Sleep -Milliseconds 250
    }
    return $null
}

function Get-DownloadSnapshot {
    if (-not (Test-Path $DownloadsDir)) {
        return @{}
    }

    $snapshot = @{}
    Get-ChildItem -Path $DownloadsDir -File -ErrorAction SilentlyContinue | ForEach-Object {
        $snapshot[$_.FullName] = @{
            LastWriteTimeUtc = $_.LastWriteTimeUtc
            Length = $_.Length
        }
    }
    return $snapshot
}

function Wait-ForDownloadedImageFile {
    param(
        [hashtable]$Baseline,
        [int]$TimeoutSeconds = 45
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $candidates = Get-ChildItem -Path $DownloadsDir -File -ErrorAction SilentlyContinue |
            Where-Object {
                $_.Extension -in '.png', '.jpg', '.jpeg', '.webp' -and
                -not $_.Name.EndsWith('.crdownload', [System.StringComparison]::OrdinalIgnoreCase)
            } |
            Sort-Object LastWriteTimeUtc -Descending

        foreach ($candidate in $candidates) {
            $known = $Baseline[$candidate.FullName]
            $isNew = $null -eq $known
            $isUpdated = -not $isNew -and ($candidate.LastWriteTimeUtc -gt $known.LastWriteTimeUtc -or $candidate.Length -ne $known.Length)
            if (-not ($isNew -or $isUpdated)) {
                continue
            }

            Start-Sleep -Milliseconds 800
            $refreshed = Get-Item -LiteralPath $candidate.FullName -ErrorAction SilentlyContinue
            if ($null -eq $refreshed) {
                continue
            }

            return $refreshed.FullName
        }

        Start-Sleep -Milliseconds 400
    }

    throw "Timed out waiting for Gemini direct image download in $DownloadsDir"
}

function Convert-DownloadedImageToPng {
    param(
        [string]$SourcePath,
        [string]$TargetPath
    )

    $pythonCode = @"
from pathlib import Path
from PIL import Image
source = Path(r'''$SourcePath''')
target = Path(r'''$TargetPath''')
target.parent.mkdir(parents=True, exist_ok=True)
with Image.open(source) as image:
    image.save(target, format='PNG')
"@
    python -c $pythonCode
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $TargetPath)) {
        throw "Failed to convert downloaded Gemini image to PNG: $SourcePath"
    }
}

$promptText = Get-Content -Path $PromptPath -Raw

try {
    Ensure-ComposerReady
    Ensure-ImageMode
    Clear-ExistingAttachments
    Clear-DraftPrompt

    Write-Host "Attaching $UploadPackPath"
    Paste-UploadPack -Path $UploadPackPath

    Write-Host "Setting prompt text"
    Set-PromptText -Text $promptText

    $aiGeneratedCount = (Get-AIGeneratedButtons).Count
    $downloadButtonCount = (Get-ElementsByExactName -Name 'Download full size image').Count

    Write-Host "Submitting prompt"
    Invoke-Send

    Write-Host "Waiting for Gemini result"
    [void](Wait-ForNewAIGeneratedButton -BaselineCount $aiGeneratedCount -DownloadBaselineCount $downloadButtonCount)
    New-Item -ItemType Directory -Force -Path $SaveDir | Out-Null
    $resultSlug = if ([string]::IsNullOrWhiteSpace($Family)) {
        "$Species-$Age-$Gender-gemini-result.png"
    }
    else {
        "$Species-$Age-$Gender-$Family-gemini-result.png"
    }
    $targetPath = Join-Path $SaveDir $resultSlug
    $downloadBaseline = Get-DownloadSnapshot
    Jump-ToThreadEnd
    $downloadButton = $null
    try {
        $downloadButton = Wait-ForAnyElementByName -Name 'Download full size image' -TimeoutSeconds 10
    }
    catch {
        $aiButton = Reveal-LatestAIGeneratedButton
        Click-ElementCenter -Element $aiButton
        $downloadButton = Wait-ForAnyElementByName -Name 'Download full size image' -TimeoutSeconds 20
    }
    Write-Host "Downloading full size image directly"
    if (-not (Try-InvokeElement -Element $downloadButton)) {
        Click-ElementCenter -Element $downloadButton
    }

    try {
        $downloadedFile = Wait-ForDownloadedImageFile -Baseline $downloadBaseline -TimeoutSeconds 45
        Convert-DownloadedImageToPng -SourcePath $downloadedFile -TargetPath $targetPath
    }
    catch {
        Write-Warning "Direct download did not complete cleanly; falling back to clipboard copy. $($_.Exception.Message)"
        $copyImageButton = Wait-ForVisibleElementByName -Name 'Copy image' -TimeoutSeconds 15
        [System.Windows.Forms.Clipboard]::Clear()
        Click-ElementCenter -Element $copyImageButton
        $clipboardImage = Wait-ForClipboardImage -TimeoutSeconds 10
        if (-not $clipboardImage) {
            throw
        }

        $bitmap = New-Object System.Drawing.Bitmap $clipboardImage
        try {
            $bitmap.Save($targetPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $bitmap.Dispose()
            $clipboardImage.Dispose()
        }
    }

    Write-Host "Saved Gemini result to:"
    Write-Host $targetPath
}
catch {
    Clear-ExistingAttachments
    Clear-DraftPrompt
    throw
}
