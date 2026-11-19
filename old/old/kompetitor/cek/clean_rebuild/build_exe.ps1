$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$App = Join-Path $Root "app\kentang_clean.py"
$Dist = Join-Path $Root "dist"
$Build = Join-Path $Root "build"

if (Test-Path $Dist) {
    Remove-Item -LiteralPath $Dist -Recurse -Force
}
if (Test-Path $Build) {
    Remove-Item -LiteralPath $Build -Recurse -Force
}

python -m PyInstaller `
    --noconfirm `
    --clean `
    --onefile `
    --windowed `
    --name KentangOptimizerClean `
    --specpath $Root `
    --distpath $Dist `
    --workpath $Build `
    $App

Write-Host "Built: $(Join-Path $Dist 'KentangOptimizerClean.exe')"
