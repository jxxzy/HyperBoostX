param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$forbidden = @(
    'Coming soon',
    'TODO',
    '2\.0\.0',
    '--%',
    '17 ms',
    'Vendor Mode',
    'Driver Actions',
    'Text\s*=\s*"[^"]*CPU\s*=',
    'Text\s*=\s*"[^"]*RAM\s*=',
    'Text\s*=\s*"[^"]*GPU\s*='
)

$files = Get-ChildItem -Path (Join-Path $RepoRoot "wpf") -Include *.cs,*.xaml,*.md -Recurse |
    Where-Object { $_.FullName -notmatch "\\(bin|obj|temp_obj)\\" }

foreach ($file in $files) {
    $relative = Resolve-Path -LiteralPath $file.FullName -Relative
    $lines = Get-Content -LiteralPath $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        foreach ($pattern in $forbidden) {
            if ($lines[$i] -match $pattern) {
                $failures.Add("${relative}:$($i + 1): $($lines[$i].Trim())")
            }
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: placeholder/fake UI guard" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: placeholder/fake UI guard" -ForegroundColor Green
