param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$OutputRoot = ".\vnext\artifacts\c-phase-58-adapter-proof-packets"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ProbeScript = Join-Path $ProjectRoot "tools\probe-vnext-pet-tasks.ps1"
$ResolvedOutputRoot = [System.IO.Path]::GetFullPath((Join-Path $ProjectRoot $OutputRoot))
$AdapterRoot = Join-Path $ResolvedOutputRoot "adapters"
$SummaryJsonPath = Join-Path $ResolvedOutputRoot "adapter-proof-summary.json"
$SummaryMarkdownPath = Join-Path $ResolvedOutputRoot "adapter-proof-summary.md"
$ProbeRoot = Join-Path $ProjectRoot "vnext\artifacts\pet-task-probes"
$PetTaskRoot = Join-Path $ProjectRoot "vnext\artifacts\pet-tasks"

if (-not (Test-Path $ProbeScript)) {
    throw "Missing PET TASKS probe script: $ProbeScript"
}

if (Test-Path $ResolvedOutputRoot) {
    Remove-Item -LiteralPath $ResolvedOutputRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $AdapterRoot | Out-Null

$proofs = @(
    [ordered]@{
        family = "localDocs"
        text = "summarize the local docs"
        extraArgs = @()
    },
    [ordered]@{
        family = "spriteAudit"
        text = "review goose baby female blue sprites"
        extraArgs = @()
    },
    [ordered]@{
        family = "assetInventory"
        text = "inventory assets in sprites_runtime"
        extraArgs = @()
    },
    [ordered]@{
        family = "petState"
        text = "review pet state"
        extraArgs = @()
    },
    [ordered]@{
        family = "codeReview"
        text = "review the code in Wevito.VNext.Core"
        extraArgs = @()
    },
    [ordered]@{
        family = "codePatchPlan"
        text = "plan a code fix in vnext"
        extraArgs = @()
    },
    [ordered]@{
        family = "buildProof"
        text = "run a build proof"
        extraArgs = @("-ApproveBeforePreview")
    },
    [ordered]@{
        family = "translateText"
        text = "translate Hello goose to Spanish"
        extraArgs = @("-ExpectExecuteEnabledAfterPreview")
    },
    [ordered]@{
        family = "audioAssist"
        text = "boost my PC volume"
        extraArgs = @()
    },
    [ordered]@{
        family = "screenCapture"
        text = "screenshot the Wevito window"
        extraArgs = @("-ApproveBeforePreview")
    }
)

function Convert-ToSafeName {
    param([string]$Name)

    return ($Name -replace "[^A-Za-z0-9_.-]", "-").ToLowerInvariant()
}

function Copy-IfPresent {
    param(
        [string]$Source,
        [string]$Destination
    )

    if ([string]::IsNullOrWhiteSpace($Source) -or -not (Test-Path -LiteralPath $Source)) {
        return $null
    }

    $parent = Split-Path -Parent $Destination
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
    Copy-Item -LiteralPath $Source -Destination $Destination -Force
    return $Destination
}

function Copy-DirectoryIfPresent {
    param(
        [string]$Source,
        [string]$Destination
    )

    if ([string]::IsNullOrWhiteSpace($Source) -or -not (Test-Path -LiteralPath $Source)) {
        return $null
    }

    if (Test-Path -LiteralPath $Destination) {
        Remove-Item -LiteralPath $Destination -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Destination) | Out-Null
    Copy-Item -LiteralPath $Source -Destination $Destination -Recurse -Force
    return $Destination
}

function Stop-LocalProofProcesses {
    Get-Process Wevito.VNext.Shell, Wevito.VNext.Broker -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -like "$ProjectRoot*" } |
        ForEach-Object {
            Stop-Process -Id $_.Id -Force
        }

    Start-Sleep -Milliseconds 750
}

function Find-SummaryPath {
    param(
        [object[]]$Output,
        [datetime]$StartedAt
    )

    foreach ($line in $Output) {
        $text = [string]$line
        if ($text -match "summary=(.+)$") {
            return $Matches[1].Trim()
        }
    }

    if (-not (Test-Path -LiteralPath $ProbeRoot)) {
        return $null
    }

    $candidate = Get-ChildItem -LiteralPath $ProbeRoot -Directory |
        Where-Object { $_.CreationTime -ge $StartedAt.AddSeconds(-2) -and (Test-Path -LiteralPath (Join-Path $_.FullName "summary.json")) } |
        Sort-Object CreationTime -Descending |
        Select-Object -First 1

    if ($null -eq $candidate) {
        return $null
    }

    return Join-Path $candidate.FullName "summary.json"
}

Write-Host "Building vNext Shell once for adapter proof pack..."
dotnet build (Join-Path $ProjectRoot "vnext\src\Wevito.VNext.Shell\Wevito.VNext.Shell.csproj") --configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "vNext Shell build failed with exit code $LASTEXITCODE"
}

$results = New-Object System.Collections.Generic.List[object]

foreach ($proof in $proofs) {
    $family = [string]$proof.family
    $safeFamily = Convert-ToSafeName $family
    $familyRoot = Join-Path $AdapterRoot $safeFamily
    New-Item -ItemType Directory -Force -Path $familyRoot | Out-Null

    $startedAt = Get-Date
    $args = @(
        "-NoProfile",
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        $ProbeScript,
        "-Configuration",
        $Configuration,
        "-TaskText",
        [string]$proof.text,
        "-ExpectedToolFamily",
        $family,
        "-SkipBuild"
    ) + @($proof.extraArgs)

    Write-Host "Running $family proof..."
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & powershell @args 2>&1
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    $output | Set-Content -LiteralPath (Join-Path $familyRoot "probe-output.txt") -Encoding UTF8

    if ($exitCode -ne 0) {
        Stop-LocalProofProcesses
        throw "$family proof failed with exit code $exitCode. See $(Join-Path $familyRoot "probe-output.txt")"
    }

    $summaryPath = Find-SummaryPath -Output $output -StartedAt $startedAt
    if ([string]::IsNullOrWhiteSpace($summaryPath) -or -not (Test-Path -LiteralPath $summaryPath)) {
        throw "$family proof did not produce a summary.json path."
    }

    $summary = Get-Content -LiteralPath $summaryPath -Raw | ConvertFrom-Json
    if ($summary.expected_tool_family -ne $family) {
        throw "$family proof expected '$family' but summary reported '$($summary.expected_tool_family)'."
    }

    if ($summary.target_hashes_unchanged -ne $true) {
        throw "$family proof changed the protected sprite hash check target."
    }

    if ($family -eq "audioAssist" -and $summary.execute_enabled_after_preview -eq $true) {
        throw "audioAssist proof unexpectedly enabled execution for an external audio boost request."
    }

    if ($family -eq "screenCapture" -and $summary.execute_enabled_after_preview -eq $true) {
        throw "screenCapture proof unexpectedly enabled execution during the report-only C-PHASE 58 proof."
    }

    $probeCopyRoot = Join-Path $familyRoot "probe"
    $reportCopyRoot = Join-Path $familyRoot "report"
    $traceCopyPath = Join-Path $familyRoot "Wevito.VNext.Shell.trace.log"
    $probeCopy = Copy-DirectoryIfPresent -Source ([string]$summary.output_dir) -Destination $probeCopyRoot
    if ($probeCopy) {
        foreach ($transientChild in @("data", "trace")) {
            $transientPath = Join-Path $probeCopy $transientChild
            if (Test-Path -LiteralPath $transientPath) {
                Remove-Item -LiteralPath $transientPath -Recurse -Force
            }
        }
    }
    $traceCopy = Copy-IfPresent -Source ([string]$summary.trace) -Destination $traceCopyPath
    $reportCopy = $null
    if (-not [string]::IsNullOrWhiteSpace([string]$summary.audit_path)) {
        $auditDirectory = Split-Path -Parent ([string]$summary.audit_path)
        $reportCopy = Copy-DirectoryIfPresent -Source $auditDirectory -Destination $reportCopyRoot
    }

    $relativeProbeCopy = Resolve-Path -LiteralPath $probeCopy -Relative
    $relativeTraceCopy = if ($traceCopy) { Resolve-Path -LiteralPath $traceCopy -Relative } else { "" }
    $relativeReportCopy = if ($reportCopy) { Resolve-Path -LiteralPath $reportCopy -Relative } else { "" }

    $results.Add([ordered]@{
        family = $family
        taskText = [string]$proof.text
        expectedToolFamily = [string]$summary.expected_tool_family
        auditPath = [string]$summary.audit_path
        copiedProbePath = $relativeProbeCopy
        copiedReportPath = $relativeReportCopy
        copiedTracePath = $relativeTraceCopy
        executeEnabledAfterPreview = $summary.execute_enabled_after_preview
        targetHashesUnchanged = $summary.target_hashes_unchanged
        shellAlive = $summary.shell_alive
    }) | Out-Null

    if (-not [string]::IsNullOrWhiteSpace([string]$summary.output_dir) -and
        (Test-Path -LiteralPath ([string]$summary.output_dir))) {
        Remove-Item -LiteralPath ([string]$summary.output_dir) -Recurse -Force
    }

    if (-not [string]::IsNullOrWhiteSpace([string]$summary.audit_path)) {
        $auditDirectory = Split-Path -Parent ([string]$summary.audit_path)
        if ($auditDirectory.StartsWith($PetTaskRoot, [System.StringComparison]::OrdinalIgnoreCase) -and
            (Test-Path -LiteralPath $auditDirectory)) {
            Remove-Item -LiteralPath $auditDirectory -Recurse -Force
        }
    }

    Stop-LocalProofProcesses
}

$summaryObject = [ordered]@{
    schemaVersion = 1
    capturedAt = (Get-Date).ToString("o")
    phase = "C-PHASE 58"
    outputRoot = (Resolve-Path -LiteralPath $ResolvedOutputRoot).Path
    total = $results.Count
    passed = ($results | Where-Object { $_.expectedToolFamily -eq $_.family }).Count
    failed = 0
    constraints = [ordered]@{
        modelCalls = "not allowed"
        screenRecording = "not allowed"
        externalAudioBoosterControl = "not allowed"
        runtimeSpriteMutation = "not allowed"
    }
    results = $results
}

$summaryObject | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $SummaryJsonPath -Encoding UTF8

$markdown = New-Object System.Collections.Generic.List[string]
$markdown.Add("# C-PHASE 58 Adapter Proof Summary")
$markdown.Add("")
$markdown.Add("Captured: $($summaryObject.capturedAt)")
$markdown.Add("")
$markdown.Add("| Family | Task text | Result | Execute enabled after preview | Copied report |")
$markdown.Add("| --- | --- | --- | --- | --- |")
foreach ($result in $results) {
    $execute = if ($null -eq $result.executeEnabledAfterPreview) { "" } else { [string]$result.executeEnabledAfterPreview }
    $report = if ([string]::IsNullOrWhiteSpace($result.copiedReportPath)) { "" } else { $result.copiedReportPath }
    $markdown.Add("| ``$($result.family)`` | $($result.taskText) | PASS | $execute | ``$report`` |")
}
$markdown.Add("")
$markdown.Add("Constraints held: no model calls, no screen recording, no external audio booster control, no sprite mutation.")
$markdown | Set-Content -LiteralPath $SummaryMarkdownPath -Encoding UTF8

Stop-LocalProofProcesses

Write-Host "adapterProofSummary=$SummaryJsonPath"
