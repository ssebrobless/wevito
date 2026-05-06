param(
    [string]$Source = ".\docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.md",
    [string]$Output = ".\docs\WEVITO_PROJECT_COMPLETION_DASHBOARD_2026-05-05.pdf",
    [switch]$KeepHtml
)

$ErrorActionPreference = "Stop"

function Convert-InlineMarkdown {
    param([string]$Text)

    $encoded = [System.Net.WebUtility]::HtmlEncode($Text)
    $encoded = [regex]::Replace($encoded, '`([^`]+)`', '<code>$1</code>')
    return $encoded
}

function Flush-Paragraph {
    param(
        [System.Text.StringBuilder]$Builder,
        [ref]$Paragraph
    )

    if ($Paragraph.Value.Count -gt 0) {
        $text = ($Paragraph.Value -join " ").Trim()
        if ($text.Length -gt 0) {
            [void]$Builder.AppendLine("<p>$(Convert-InlineMarkdown $text)</p>")
        }
        $Paragraph.Value = @()
    }
}

function Flush-List {
    param(
        [System.Text.StringBuilder]$Builder,
        [ref]$ListItems
    )

    if ($ListItems.Value.Count -gt 0) {
        [void]$Builder.AppendLine("<ul>")
        foreach ($item in $ListItems.Value) {
            [void]$Builder.AppendLine("<li>$(Convert-InlineMarkdown $item)</li>")
        }
        [void]$Builder.AppendLine("</ul>")
        $ListItems.Value = @()
    }
}

function Flush-Table {
    param(
        [System.Text.StringBuilder]$Builder,
        [ref]$TableRows
    )

    if ($TableRows.Value.Count -eq 0) {
        return
    }

    [void]$Builder.AppendLine("<table>")
    $isHeader = $true
    foreach ($row in $TableRows.Value) {
        $cells = $row.Trim().Trim("|").Split("|") | ForEach-Object { $_.Trim() }
        if (($cells | Where-Object { $_ -notmatch '^-{3,}:?$|^:?-{3,}:?$' }).Count -eq 0) {
            continue
        }

        [void]$Builder.AppendLine("<tr>")
        foreach ($cell in $cells) {
            $tag = if ($isHeader) { "th" } else { "td" }
            [void]$Builder.AppendLine("<$tag>$(Convert-InlineMarkdown $cell)</$tag>")
        }
        [void]$Builder.AppendLine("</tr>")
        $isHeader = $false
    }
    [void]$Builder.AppendLine("</table>")
    $TableRows.Value = @()
}

$repoRoot = (Resolve-Path -LiteralPath ".").Path
$sourcePath = (Resolve-Path -LiteralPath $Source).Path
$outputPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Output))
$outputDir = Split-Path -Parent $outputPath

if (-not (Test-Path -LiteralPath $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

$edgeCandidates = @(
    "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    "C:\Program Files\Microsoft\Edge\Application\msedge.exe"
)
$edge = $edgeCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $edge) {
    throw "Microsoft Edge was not found. Install Edge or update this script to use another headless Chromium executable."
}

$lines = Get-Content -LiteralPath $sourcePath
$body = [System.Text.StringBuilder]::new()
$paragraph = @()
$listItems = @()
$tableRows = @()
$inCode = $false
$codeLines = @()

foreach ($line in $lines) {
    if ($line -match '^```') {
        Flush-Paragraph $body ([ref]$paragraph)
        Flush-List $body ([ref]$listItems)
        Flush-Table $body ([ref]$tableRows)

        if ($inCode) {
            $code = [System.Net.WebUtility]::HtmlEncode(($codeLines -join "`n"))
            [void]$body.AppendLine("<pre><code>$code</code></pre>")
            $codeLines = @()
            $inCode = $false
        } else {
            $inCode = $true
        }
        continue
    }

    if ($inCode) {
        $codeLines += $line
        continue
    }

    if ($line -match '^\s*$') {
        Flush-Paragraph $body ([ref]$paragraph)
        Flush-List $body ([ref]$listItems)
        Flush-Table $body ([ref]$tableRows)
        continue
    }

    if ($line -match '^\|.+\|$') {
        Flush-Paragraph $body ([ref]$paragraph)
        Flush-List $body ([ref]$listItems)
        $tableRows += $line
        continue
    }

    Flush-Table $body ([ref]$tableRows)

    if ($line -match '^(#{1,3})\s+(.+)$') {
        Flush-Paragraph $body ([ref]$paragraph)
        Flush-List $body ([ref]$listItems)
        $level = $Matches[1].Length
        $text = Convert-InlineMarkdown $Matches[2]
        [void]$body.AppendLine("<h$level>$text</h$level>")
        continue
    }

    if ($line -match '^\s*-\s+(.+)$') {
        Flush-Paragraph $body ([ref]$paragraph)
        $listItems += $Matches[1]
        continue
    }

    if ($line -match '^\s*\d+\.\s+(.+)$') {
        Flush-Paragraph $body ([ref]$paragraph)
        $listItems += $Matches[1]
        continue
    }

    Flush-List $body ([ref]$listItems)
    $paragraph += $line
}

Flush-Paragraph $body ([ref]$paragraph)
Flush-List $body ([ref]$listItems)
Flush-Table $body ([ref]$tableRows)

$htmlPath = [System.IO.Path]::ChangeExtension($outputPath, ".html")
$css = @"
body {
  font-family: "Segoe UI", Arial, sans-serif;
  color: #18202a;
  margin: 42px;
  line-height: 1.45;
}
h1 {
  border-bottom: 3px solid #22344d;
  color: #102033;
  padding-bottom: 10px;
}
h2 {
  color: #1d3959;
  border-bottom: 1px solid #d5dde8;
  padding-bottom: 5px;
  margin-top: 30px;
}
h3 {
  color: #24384d;
  margin-top: 24px;
}
table {
  width: 100%;
  border-collapse: collapse;
  margin: 14px 0 22px;
  font-size: 12px;
}
th, td {
  border: 1px solid #cbd5e1;
  padding: 7px 8px;
  vertical-align: top;
}
th {
  background: #eef4fb;
  color: #102033;
  text-align: left;
}
pre {
  background: #101820;
  color: #e8f2ff;
  padding: 14px 16px;
  border-radius: 8px;
  overflow-x: auto;
  font-size: 12px;
}
code {
  font-family: "Cascadia Mono", Consolas, monospace;
  font-size: 0.95em;
}
p, li {
  font-size: 13px;
}
ul {
  margin-top: 6px;
}
@page {
  size: Letter;
  margin: 0.55in;
}
"@

$html = @"
<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <title>Wevito Project Completion Dashboard</title>
  <style>
$css
  </style>
</head>
<body>
$($body.ToString())
</body>
</html>
"@

Set-Content -LiteralPath $htmlPath -Value $html -Encoding UTF8

$userDataDir = Join-Path $env:TEMP ("wevito-edge-pdf-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $userDataDir | Out-Null

try {
    $htmlUri = ([System.Uri]$htmlPath).AbsoluteUri
    $args = @(
        "--headless",
        "--disable-gpu",
        "--no-first-run",
        "--disable-extensions",
        "--user-data-dir=$userDataDir",
        "--print-to-pdf=$outputPath",
        $htmlUri
    )

    $process = Start-Process -FilePath $edge -ArgumentList $args -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) {
        throw "Edge PDF export failed with exit code $($process.ExitCode)."
    }

    if (-not (Test-Path -LiteralPath $outputPath)) {
        throw "PDF export completed but output file was not created: $outputPath"
    }

    if (-not $KeepHtml -and (Test-Path -LiteralPath $htmlPath)) {
        Remove-Item -LiteralPath $htmlPath -Force
    }

    Get-Item -LiteralPath $outputPath | Select-Object FullName, Length, LastWriteTime
}
finally {
    if (Test-Path -LiteralPath $userDataDir) {
        Remove-Item -LiteralPath $userDataDir -Recurse -Force
    }
}
