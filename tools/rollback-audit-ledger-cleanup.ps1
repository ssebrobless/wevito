param(
    [string]$SummaryPath,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Info {
    param([string]$Message)
    Write-Host "[rollback-audit-ledger-cleanup] $Message"
}

function Resolve-KillSwitchSentinel {
    if (-not [string]::IsNullOrWhiteSpace($env:WEVITO_KILL_SWITCH_SENTINEL)) {
        return $env:WEVITO_KILL_SWITCH_SENTINEL
    }

    $local = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    return (Join-Path $local "WevitoVNext\kill-switch.active")
}

function Test-IsUnderPath {
    param(
        [string]$Path,
        [string]$Root
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    return $fullPath.Equals($fullRoot, [StringComparison]::OrdinalIgnoreCase) -or
        $fullPath.StartsWith($fullRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
        $fullPath.StartsWith($fullRoot + [System.IO.Path]::AltDirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}

function Exit-WithEvidence {
    param(
        [int]$Code,
        [string]$EvidencePath,
        [object[]]$Moves,
        [string]$Reason
    )

    if (-not [string]::IsNullOrWhiteSpace($EvidencePath)) {
        $parent = Split-Path -Parent $EvidencePath
        if (-not [string]::IsNullOrWhiteSpace($parent)) {
            New-Item -ItemType Directory -Force -Path $parent | Out-Null
        }

        [ordered]@{
            schemaVersion = "1"
            packetKind = "audit_ledger_cleanup_rolled_back"
            createdAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
            dryRun = [bool]$DryRun
            exitCode = $Code
            reason = $Reason
            moves = $Moves
        } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $EvidencePath -Encoding UTF8
    }

    if ($Code -eq 0) {
        Write-Info $Reason
    } else {
        [Console]::Error.WriteLine("[rollback-audit-ledger-cleanup] $Reason")
    }

    exit $Code
}

if (Test-Path -LiteralPath (Resolve-KillSwitchSentinel)) {
    [Console]::Error.WriteLine("[rollback-audit-ledger-cleanup] KillSwitch sentinel is active; refusing rollback.")
    exit 3
}

if ([string]::IsNullOrWhiteSpace($SummaryPath)) {
    if ($DryRun) {
        Write-Info "Dry-run requested without a summary path; no rollback plan loaded."
        exit 0
    }

    [Console]::Error.WriteLine("[rollback-audit-ledger-cleanup] Missing -SummaryPath.")
    exit 4
}

if (-not (Test-Path -LiteralPath $SummaryPath)) {
    [Console]::Error.WriteLine("[rollback-audit-ledger-cleanup] Summary path does not exist: $SummaryPath")
    exit 4
}

try {
    $summary = Get-Content -LiteralPath $SummaryPath -Raw | ConvertFrom-Json
} catch {
    [Console]::Error.WriteLine("[rollback-audit-ledger-cleanup] Summary JSON is malformed: $($_.Exception.Message)")
    exit 4
}

if ($null -eq $summary.auditRoot -or $null -eq $summary.moved) {
    [Console]::Error.WriteLine("[rollback-audit-ledger-cleanup] Summary is missing auditRoot or moved fields.")
    exit 4
}

$auditRoot = [System.IO.Path]::GetFullPath([string]$summary.auditRoot)
$summaryDirectory = Split-Path -Parent ([System.IO.Path]::GetFullPath($SummaryPath))
if ([string]::IsNullOrWhiteSpace($summaryDirectory)) {
    $summaryDirectory = Join-Path $auditRoot "cleanup-summaries"
}

$summaryId = [System.IO.Path]::GetFileNameWithoutExtension($SummaryPath)
$evidencePath = Join-Path $summaryDirectory "rollback-$summaryId.json"
$moves = @()

foreach ($rawMove in @($summary.moved)) {
    if ($null -eq $rawMove.source -or $null -eq $rawMove.destination) {
        Exit-WithEvidence 4 $evidencePath $moves "Summary move entry is missing source or destination."
    }

    $source = [System.IO.Path]::GetFullPath([string]$rawMove.source)
    $destination = [System.IO.Path]::GetFullPath([string]$rawMove.destination)
    $preHash = if ($null -ne $rawMove.preMoveSha256) { [string]$rawMove.preMoveSha256 } else { [string]$rawMove.sha256 }
    $postHash = if ($null -ne $rawMove.postMoveSha256) { [string]$rawMove.postMoveSha256 } else { [string]$rawMove.afterSha256 }

    if ([string]::IsNullOrWhiteSpace($preHash) -or [string]::IsNullOrWhiteSpace($postHash)) {
        Exit-WithEvidence 4 $evidencePath $moves "Summary move entry is missing pre/post sha256 fields."
    }

    if (-not (Test-IsUnderPath -Path $source -Root $auditRoot) -or
        -not (Test-IsUnderPath -Path $destination -Root $auditRoot)) {
        Exit-WithEvidence 4 $evidencePath $moves "Summary move entry points outside the audit root."
    }

    $moves += [ordered]@{
        source = $source
        destination = $destination
        preMoveSha256 = $preHash
        postMoveSha256 = $postHash
        state = "planned"
    }
}

if ($DryRun) {
    Exit-WithEvidence 0 $evidencePath $moves "Dry-run complete; no files moved."
}

# Preflight every move before mutating anything.
for ($i = 0; $i -lt $moves.Count; $i++) {
    $move = $moves[$i]
    if (-not (Test-Path -LiteralPath $move.destination)) {
        if ((Test-Path -LiteralPath $move.source) -and
            ((Get-FileHash -LiteralPath $move.source -Algorithm SHA256).Hash.ToLowerInvariant() -eq $move.preMoveSha256.ToLowerInvariant())) {
            $move.state = "already_restored"
            continue
        }

        Exit-WithEvidence 5 $evidencePath $moves "Destination missing before rollback: $($move.destination)"
    }

    $actualDestinationHash = (Get-FileHash -LiteralPath $move.destination -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualDestinationHash -ne $move.postMoveSha256.ToLowerInvariant()) {
        $move.state = "destination_hash_mismatch"
        $move.actualDestinationSha256 = $actualDestinationHash
        Exit-WithEvidence 2 $evidencePath $moves "Destination sha256 mismatch: $($move.destination)"
    }
}

foreach ($move in $moves) {
    if ($move.state -eq "already_restored") {
        continue
    }

    $parent = Split-Path -Parent $move.source
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    Move-Item -LiteralPath $move.destination -Destination $move.source
    $actualSourceHash = (Get-FileHash -LiteralPath $move.source -Algorithm SHA256).Hash.ToLowerInvariant()
    $move.actualSourceSha256 = $actualSourceHash
    if ($actualSourceHash -ne $move.preMoveSha256.ToLowerInvariant()) {
        $move.state = "source_hash_mismatch_after_restore"
        Exit-WithEvidence 2 $evidencePath $moves "Source sha256 mismatch after restore: $($move.source)"
    }

    $move.state = "restored"
}

Exit-WithEvidence 0 $evidencePath $moves "Rollback complete."
