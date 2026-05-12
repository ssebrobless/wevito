param(
    [string]$Version = "dev",
    [string]$GodotPath = "",
    [switch]$Debug,
    [int]$ExportTimeoutSeconds = 900,
    [switch]$NoStagingProject,
    [switch]$KeepExportStaging
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$BuildDir = Join-Path $ProjectRoot "builds\release"
$PresetName = "Windows Desktop"
$AppExeBase = "WevitoDesktopPet"
$HelperBuildScript = Join-Path $ProjectRoot "tools\build-desktop-bridge.ps1"
$HelperExe = Join-Path $ProjectRoot "builds\desktop_bridge\WevitoDesktopBridge.exe"

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

function Join-ProcessArguments {
    param([string[]]$Arguments)

    ($Arguments | ForEach-Object {
            $argument = [string]$_
            if ($argument -match '[\s"]') {
                '"' + $argument.Replace('"', '\"') + '"'
            }
            else {
                $argument
            }
        }) -join " "
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
$ExternalAssetsDir = Join-Path $BuildDir "assets"
$BundleName = "{0}-v{1}-win64" -f $AppExeBase, $Version
$BundleDir = Join-Path $BuildDir $BundleName
$BundleZip = Join-Path $BuildDir ("{0}.zip" -f $BundleName)
$LatestBundleZip = Join-Path $BuildDir ("{0}-latest-win64.zip" -f $AppExeBase)

function Copy-DirectoryClean {
    param(
        [string]$Source,
        [string]$Destination,
        [switch]$AllowOutsideBuildDir
    )

    if (-not (Test-Path $Source)) {
        throw "Missing required package source: $Source"
    }

    $resolvedBuildDir = [System.IO.Path]::GetFullPath($BuildDir)
    $resolvedDestination = [System.IO.Path]::GetFullPath($Destination)
    if (-not $AllowOutsideBuildDir -and -not $resolvedDestination.StartsWith($resolvedBuildDir, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean destination outside build directory: $Destination"
    }

    if (Test-Path $Destination) {
        Remove-Item -LiteralPath $Destination -Recurse -Force
    }

    $parent = Split-Path -Parent $Destination
    if (-not (Test-Path $parent)) {
        New-Item -ItemType Directory -Path $parent | Out-Null
    }
    Copy-Item -Path $Source -Destination $Destination -Recurse -Force
}

function Copy-ReleasePackageAssets {
    $assetMappings = @(
        @{ Source = Join-Path $ProjectRoot "sprites_runtime"; Destination = Join-Path $ExternalAssetsDir "sprites_runtime" },
        @{ Source = Join-Path $ProjectRoot "sprites_shared_runtime"; Destination = Join-Path $ExternalAssetsDir "sprites_shared_runtime" },
        @{ Source = Join-Path $ProjectRoot "sprites\egg"; Destination = Join-Path $ExternalAssetsDir "sprites\egg" },
        @{ Source = Join-Path $ProjectRoot "vnext\content"; Destination = Join-Path $ExternalAssetsDir "vnext\content" }
    )

    foreach ($mapping in $assetMappings) {
        Copy-DirectoryClean -Source $mapping.Source -Destination $mapping.Destination
    }

    Get-ChildItem -Path $ExternalAssetsDir -Filter "*.import" -Recurse -File -ErrorAction SilentlyContinue |
        Remove-Item -Force
    Get-ChildItem -Path $ExternalAssetsDir -Directory -Force -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -eq ".staging" } |
        Remove-Item -Recurse -Force
}

function New-ReleaseBundle {
    param(
        [string]$ExecutablePath,
        [string]$DesktopBridgePath
    )

    Copy-DirectoryClean -Source $ExternalAssetsDir -Destination (Join-Path $BundleDir "assets")
    Copy-Item -Path $ExecutablePath -Destination (Join-Path $BundleDir "WevitoDesktopPet.exe") -Force
    Copy-Item -Path $DesktopBridgePath -Destination (Join-Path $BundleDir "WevitoDesktopBridge.exe") -Force

    if (Test-Path $BundleZip) {
        Remove-Item -LiteralPath $BundleZip -Force
    }
    Compress-Archive -Path (Join-Path $BundleDir "*") -DestinationPath $BundleZip -Force
    Copy-Item -Path $BundleZip -Destination $LatestBundleZip -Force
}

function New-GodotExportStagingProject {
    $stageRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("wevito-godot-export-" + [System.Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $stageRoot | Out-Null

    Copy-Item -Path (Join-Path $ProjectRoot "project.godot") -Destination (Join-Path $stageRoot "project.godot") -Force
    Copy-Item -Path (Join-Path $ProjectRoot "export_presets.cfg") -Destination (Join-Path $stageRoot "export_presets.cfg") -Force
    Copy-Item -Path (Join-Path $ProjectRoot "icon.svg") -Destination (Join-Path $stageRoot "icon.svg") -Force
    Copy-DirectoryClean -Source (Join-Path $ProjectRoot "scripts") -Destination (Join-Path $stageRoot "scripts") -AllowOutsideBuildDir
    Copy-DirectoryClean -Source (Join-Path $ProjectRoot "scenes") -Destination (Join-Path $stageRoot "scenes") -AllowOutsideBuildDir
    Copy-DirectoryClean -Source (Join-Path $ProjectRoot "vnext\content") -Destination (Join-Path $stageRoot "vnext\content") -AllowOutsideBuildDir

    return $stageRoot
}

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
    "C:\Users\fishe\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.6.1-stable_win64_console.exe",
    "C:\Users\fishe\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.6.1-stable_win64.exe",
    "C:\Program Files\Godot\Godot_v4.6.1-stable_win64_console.exe",
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

if (-not (Test-Path $HelperBuildScript)) {
    throw "Missing desktop bridge build script: $HelperBuildScript"
}

Write-Host "Building Wevito desktop bridge..."
& powershell -NoProfile -ExecutionPolicy Bypass -File $HelperBuildScript
if ($LASTEXITCODE -ne 0) {
    throw "Desktop bridge build failed with exit code $LASTEXITCODE"
}
if (-not (Test-Path $HelperExe)) {
    throw "Desktop bridge executable not found after build: $HelperExe"
}

Write-Host "Building Wevito..."
Write-Host "Godot: $ResolvedGodot"
Write-Host "Preset: $PresetName"
Write-Host "Output: $VersionedExe"

$BuildStart = Get-Date
$ExportStdOutPath = [System.IO.Path]::GetTempFileName()
$ExportStdErrPath = [System.IO.Path]::GetTempFileName()
$ExportProjectRoot = $ProjectRoot
$ExportStageRoot = ""
if (-not $NoStagingProject) {
    $ExportStageRoot = New-GodotExportStagingProject
    $ExportProjectRoot = $ExportStageRoot
    Write-Host "Using lean Godot export staging project: $ExportStageRoot"
}
$ExportArguments = Join-ProcessArguments -Arguments @("--headless", "--path", $ExportProjectRoot, $ExportFlag, $PresetName, $VersionedExe)
try {
    $ExportProcess = Start-Process -FilePath $ResolvedGodot -ArgumentList $ExportArguments -NoNewWindow -PassThru -RedirectStandardOutput $ExportStdOutPath -RedirectStandardError $ExportStdErrPath
    $ExportCompleted = $ExportProcess.WaitForExit($ExportTimeoutSeconds * 1000)
    if (-not $ExportCompleted) {
        try {
            $ExportProcess.Kill($true)
        }
        catch {
            try {
                $ExportProcess.Kill()
            }
            catch {
            }
        }

        $ExportOutputText = @(
            if (Test-Path $ExportStdOutPath) { Get-Content -Path $ExportStdOutPath -Raw -ErrorAction SilentlyContinue }
            if (Test-Path $ExportStdErrPath) { Get-Content -Path $ExportStdErrPath -Raw -ErrorAction SilentlyContinue }
        ) -join [Environment]::NewLine
        if ($ExportOutputText.Trim() -ne "") {
            Write-Host "Godot export output before timeout:" -ForegroundColor Yellow
            Write-Host $ExportOutputText
        }

        throw "Godot export timed out after $ExportTimeoutSeconds seconds."
    }

    $ExportProcess.Refresh()
    $ExitCode = if ($null -ne $ExportProcess.ExitCode) { [int]$ExportProcess.ExitCode } else { 0 }
    $ExportOutputText = @(
        if (Test-Path $ExportStdOutPath) { Get-Content -Path $ExportStdOutPath -Raw -ErrorAction SilentlyContinue }
        if (Test-Path $ExportStdErrPath) { Get-Content -Path $ExportStdErrPath -Raw -ErrorAction SilentlyContinue }
    ) -join [Environment]::NewLine
}
finally {
    Remove-Item -Path $ExportStdOutPath, $ExportStdErrPath -Force -ErrorAction SilentlyContinue
    if ($ExportStageRoot -ne "" -and -not $KeepExportStaging -and (Test-Path $ExportStageRoot)) {
        Remove-Item -LiteralPath $ExportStageRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
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

$BuildHelperTarget = Join-Path $BuildDir "WevitoDesktopBridge.exe"
Copy-Item -Path $HelperExe -Destination $BuildHelperTarget -Force
Copy-ReleasePackageAssets
New-ReleaseBundle -ExecutablePath $ResolvedOutput -DesktopBridgePath $BuildHelperTarget

$BuildType = if ($Debug) { "debug" } else { "release" }
@(
    "timestamp=$(Get-Date -Format s)",
    "version=$Version",
    "build_type=$BuildType",
    "godot=$ResolvedGodot",
    "output=$ResolvedOutput",
    "latest_available=$LatestAvailable",
    "desktop_bridge=$BuildHelperTarget",
    "external_assets=$ExternalAssetsDir",
    "bundle_dir=$BundleDir",
    "bundle_zip=$BundleZip",
    "latest_bundle_zip=$LatestBundleZip",
    "export_staging_project=$(-not $NoStagingProject)"
) | Set-Content -Path $BuildInfo -Encoding UTF8

Write-Host ""
Write-Host "Build complete:"
Write-Host "- $ResolvedOutput"
Write-Host "- $LatestAvailable"
Write-Host "- $ExternalAssetsDir"
Write-Host "- $BundleZip"
