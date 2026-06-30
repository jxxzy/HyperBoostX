param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$xamlFiles = Get-ChildItem -Path (Join-Path $RepoRoot "wpf") -Filter "*.xaml" -Recurse |
    Where-Object { $_.FullName -notmatch "\\(bin|obj|temp_obj)\\" }

foreach ($file in $xamlFiles) {
    $raw = Get-Content -LiteralPath $file.FullName -Raw
    $matches = [regex]::Matches($raw, '<Button\b(?<attrs>.*?)(?:/>|>.*?</Button>)', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    foreach ($match in $matches) {
        $attrs = $match.Groups['attrs'].Value
        $hasAction = $attrs -match '\bClick\s*=' -or $attrs -match '\bCommand\s*='
        if (-not $hasAction) {
            $label = if ($attrs -match 'Content\s*=\s*"([^"]+)"') { $Matches[1] } else { "<unknown>" }
            $failures.Add("Button without Click/Command in $($file.FullName): $label")
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: WPF button handler verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: WPF button handler verification" -ForegroundColor Green
