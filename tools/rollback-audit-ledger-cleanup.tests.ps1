Describe "rollback-audit-ledger-cleanup" {
    BeforeEach {
        $script:Root = Join-Path ([System.IO.Path]::GetTempPath()) ("wevito-rollback-tests-" + [Guid]::NewGuid().ToString("N"))
        $script:AuditRoot = Join-Path $script:Root "audit"
        $script:SummaryRoot = Join-Path $script:AuditRoot "cleanup-summaries"
        $script:ArchiveRoot = Join-Path $script:AuditRoot "archive"
        New-Item -ItemType Directory -Force -Path $script:SummaryRoot, $script:ArchiveRoot | Out-Null
        $script:Source = Join-Path $script:AuditRoot "20260101-archived.jsonl"
        $script:Destination = Join-Path $script:ArchiveRoot "20260101-archived.jsonl"
        "old-ledger" | Set-Content -LiteralPath $script:Destination -Encoding UTF8
        $script:Hash = (Get-FileHash -LiteralPath $script:Destination -Algorithm SHA256).Hash.ToLowerInvariant()
        $script:SummaryPath = Join-Path $script:SummaryRoot "cleanup-summary.json"
        [ordered]@{
            schemaVersion = "1"
            mode = "apply"
            auditRoot = $script:AuditRoot
            archiveRoot = $script:ArchiveRoot
            movedCount = 1
            moved = @(
                [ordered]@{
                    source = $script:Source
                    destination = $script:Destination
                    sha256 = $script:Hash
                    afterSha256 = $script:Hash
                    preMoveSha256 = $script:Hash
                    postMoveSha256 = $script:Hash
                }
            )
        } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $script:SummaryPath -Encoding UTF8
        $env:WEVITO_KILL_SWITCH_SENTINEL = Join-Path $script:Root "kill-switch.active"
    }

    AfterEach {
        Remove-Item Env:\WEVITO_KILL_SWITCH_SENTINEL -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $script:Root -Recurse -Force -ErrorAction SilentlyContinue
    }

    It "happy path restore" {
        & "$PSScriptRoot\rollback-audit-ledger-cleanup.ps1" -SummaryPath $script:SummaryPath
        $LASTEXITCODE | Should Be 0
        Test-Path -LiteralPath $script:Source | Should Be $true
        Test-Path -LiteralPath $script:Destination | Should Be $false
        (Get-FileHash -LiteralPath $script:Source -Algorithm SHA256).Hash.ToLowerInvariant() | Should Be $script:Hash
    }

    It "sha256 mismatch abort" {
        "tampered" | Set-Content -LiteralPath $script:Destination -Encoding UTF8
        & "$PSScriptRoot\rollback-audit-ledger-cleanup.ps1" -SummaryPath $script:SummaryPath
        $LASTEXITCODE | Should Be 2
        Test-Path -LiteralPath $script:Destination | Should Be $true
        Test-Path -LiteralPath $script:Source | Should Be $false
    }

    It "missing summary abort" {
        & "$PSScriptRoot\rollback-audit-ledger-cleanup.ps1" -SummaryPath (Join-Path $script:Root "missing.json")
        $LASTEXITCODE | Should Be 4
    }

    It "KillSwitch refusal" {
        "stop" | Set-Content -LiteralPath $env:WEVITO_KILL_SWITCH_SENTINEL -Encoding UTF8
        & "$PSScriptRoot\rollback-audit-ledger-cleanup.ps1" -SummaryPath $script:SummaryPath
        $LASTEXITCODE | Should Be 3
        Test-Path -LiteralPath $script:Destination | Should Be $true
        Test-Path -LiteralPath $script:Source | Should Be $false
    }
}
