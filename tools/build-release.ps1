param(
    [string]$Version = "dev",
    [string]$GodotPath = "",
    [switch]$Debug
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$BuildDir = Join-Path $ProjectRoot "builds\release"
$PresetName = "Windows Desktop"
$AppExeBase = "WevitoDesktopPet"

function Get-GodotTemplateVersion {
    param([string]$Executable)

    # Prefer executable filename since GUI binaries may not emit capturable --version output in PowerShell.
    $exeName = [System.IO.Path]::GetFileName($Executable)
    if ($exeName -match "Godot_v(\d+\.\d+(?:\.\d+)?)-stable") {
        return "$($Matches[1]).stable"
    }

    $versionOutput = & $Executable --version 2>&1
    $versionText = ($versionOutput | Out-String).Trim()

    # Expected output contains tokens like: 4.6.1.stable.official
    if ($versionText -match "(\d+\.\d+(?:\.\d+)?\.stable)") {
        return $Matches[1]
    }

    return ""
}

function Ensure-WindowsTemplatesForVersion {
    param(
        [string]$TemplatesRoot,
        [string]$TemplateVersion
    )

    if ($TemplateVersion -eq "") {
        return
    }

    $expectedDir = Join-Path $TemplatesRoot $TemplateVersion
    $expectedDebug = Join-Path $expectedDir "windows_debug_x86_64.exe"
    $expectedRelease = Join-Path $expectedDir "windows_release_x86_64.exe"

    if ((Test-Path $expectedDebug) -and (Test-Path $expectedRelease)) {
        return
    }

    if (-not (Test-Path $TemplatesRoot)) {
        return
    }

    $fallbackDebug = Get-ChildItem -Path $TemplatesRoot -Filter "windows_debug_x86_64.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    $fallbackRelease = Get-ChildItem -Path $TemplatesRoot -Filter "windows_release_x86_64.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1

    if (($null -eq $fallbackDebug) -or ($null -eq $fallbackRelease)) {
        return
    }

    if (-not (Test-Path $expectedDir)) {
        New-Item -ItemType Directory -Path $expectedDir | Out-Null
    }

    Copy-Item -Path $fallbackDebug.FullName -Destination $expectedDebug -Force
    Copy-Item -Path $fallbackRelease.FullName -Destination $expectedRelease -Force
    Write-Host "Prepared export templates for Godot $TemplateVersion in $expectedDir"
}

if (-not (Test-Path $BuildDir)) {
    New-Item -ItemType Directory -Path $BuildDir | Out-Null
}

if ($Version -eq "dev") {
    $Version = Get-Date -Format "yyyyMMdd-HHmm"
}

$VersionedExe = Join-Path $BuildDir ("{0}-v{1}-win64.exe" -f $AppExeBase, $Version)
$LatestExe = Join-Path $BuildDir ("{0}-latest-win64.exe" -f $AppExeBase)
$BuildInfo = Join-Path $BuildDir "last-build.txt"

$PresetFile = Join-Path $ProjectRoot "export_presets.cfg"
if (-not (Test-Path $PresetFile)) {
    throw "Missing export preset file: $PresetFile`nOpen Godot -> Project -> Export, add 'Windows Desktop', then rerun."
}

$TemplatesRoot = Join-Path $env:APPDATA "Godot\export_templates"
$HasWindowsTemplates = $false
if (Test-Path $TemplatesRoot) {
    $debugTemplate = Get-ChildItem -Path $TemplatesRoot -Filter "windows_debug_x86_64.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    $releaseTemplate = Get-ChildItem -Path $TemplatesRoot -Filter "windows_release_x86_64.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    $HasWindowsTemplates = ($null -ne $debugTemplate) -and ($null -ne $releaseTemplate)
}

if (-not $HasWindowsTemplates) {
    throw "Windows export templates are missing.`nIn Godot editor: Editor -> Manage Export Templates -> Download and Install, then rerun build-release.bat."
}

$Candidates = @(
    $GodotPath,
    $env:GODOT_PATH,
    "C:\Users\fishe\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.6.1-stable_win64.exe",
    "C:\Program Files\Godot\Godot_v4.6.1-stable_win64.exe",
    "C:\Program Files\Godot\Godot.exe"
) | Where-Object { $_ -and $_.Trim() -ne "" }

$ResolvedGodot = $null
foreach ($Candidate in $Candidates) {
    if (Test-Path $Candidate) {
        $ResolvedGodot = $Candidate
        break
    }
}

if (-not $ResolvedGodot) {
    throw "Could not find Godot executable. Pass -GodotPath or set GODOT_PATH."
}

$TemplateVersion = Get-GodotTemplateVersion -Executable $ResolvedGodot
if ($TemplateVersion -ne "") {
    Write-Host "Template version target: $TemplateVersion"
}
Ensure-WindowsTemplatesForVersion -TemplatesRoot $TemplatesRoot -TemplateVersion $TemplateVersion

# Re-check after preparing expected-version folder.
$HasWindowsTemplates = $false
if (Test-Path $TemplatesRoot) {
    $debugTemplate = Get-ChildItem -Path $TemplatesRoot -Filter "windows_debug_x86_64.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    $releaseTemplate = Get-ChildItem -Path $TemplatesRoot -Filter "windows_release_x86_64.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    $HasWindowsTemplates = ($null -ne $debugTemplate) -and ($null -ne $releaseTemplate)
}

if (-not $HasWindowsTemplates) {
    throw "Windows export templates are missing.`nIn Godot editor: Editor -> Manage Export Templates -> Download and Install, then rerun build-release.bat."
}

$ExportFlag = if ($Debug) { "--export-debug" } else { "--export-release" }

Write-Host "Building Wevito..."
Write-Host "Godot: $ResolvedGodot"
Write-Host "Preset: $PresetName"
Write-Host "Output: $VersionedExe"

$BuildStart = Get-Date
$ExportOutput = & $ResolvedGodot --headless --path $ProjectRoot $ExportFlag $PresetName $VersionedExe 2>&1
$ExportOutputText = ($ExportOutput | Out-String)
$ExitCode = if ($null -eq $LASTEXITCODE) { 1 } else { [int]$LASTEXITCODE }
if ($ExitCode -ne 0) {
    Write-Host "Note: Godot returned exit code $ExitCode, but export output was created successfully. Continuing." -ForegroundColor DarkYellow
}

# On some systems, file creation can lag process exit slightly.
$ResolvedOutput = $VersionedExe
$OutputReady = Test-Path $ResolvedOutput
for ($i = 0; $i -lt 120 -and -not $OutputReady; $i++) {
    Start-Sleep -Milliseconds 250
    $OutputReady = Test-Path $ResolvedOutput
}

if (-not $OutputReady) {
    $tmpCandidate = "$VersionedExe.tmp"
    if (Test-Path $tmpCandidate) {
        try {
            Move-Item -Path $tmpCandidate -Destination $VersionedExe -Force
            $ResolvedOutput = $VersionedExe
            $OutputReady = $true
            Write-Warning "Recovered export output from temporary file: $ResolvedOutput"
        }
        catch {
            Write-Warning "Found temporary export output but failed to finalize it: $tmpCandidate"
        }
    }
}

if (-not $OutputReady) {
    $fallbackOutput = Get-ChildItem -Path $BuildDir -Filter ("{0}-v*-win64.exe" -f $AppExeBase) -File -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTime -ge $BuildStart.AddSeconds(-5) } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($null -ne $fallbackOutput) {
        $ResolvedOutput = $fallbackOutput.FullName
        $OutputReady = $true
        Write-Warning "Expected output path not found immediately; using detected build output: $ResolvedOutput"
    }
}

if (-not $OutputReady) {
    # Backward-compatible fallback for previously named outputs.
    $legacyOutput = Get-ChildItem -Path $BuildDir -Filter "Wevito-v*-win64.exe" -File -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTime -ge $BuildStart.AddSeconds(-5) } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($null -ne $legacyOutput) {
        $ResolvedOutput = $legacyOutput.FullName
        $OutputReady = $true
        Write-Warning "Using legacy-named build output: $ResolvedOutput"
    }
}

if (-not $OutputReady) {
    if ($ExportOutputText.Trim() -ne "") {
        Write-Host "Godot export output:" -ForegroundColor Yellow
        Write-Host $ExportOutputText
    }
    throw "Godot export failed: output file not found at $VersionedExe"
}

$LatestAvailable = $LatestExe
try {
    Copy-Item -Path $ResolvedOutput -Destination $LatestExe -Force
}
catch {
    Write-Warning "Could not update latest executable (likely in use). Using versioned build output instead."
    $LatestAvailable = $ResolvedOutput
}

$BuildType = if ($Debug) { "debug" } else { "release" }
@(
    "timestamp=$(Get-Date -Format s)",
    "version=$Version",
    "build_type=$BuildType",
    "godot=$ResolvedGodot",
    "output=$ResolvedOutput",
    "latest_available=$LatestAvailable"
) | Set-Content -Path $BuildInfo -Encoding UTF8

Write-Host ""
Write-Host "Build complete:"
Write-Host "- $ResolvedOutput"
Write-Host "- $LatestAvailable"
