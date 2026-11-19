param([string]$Cli,[string]$DataDirectory,[string]$OutputFile)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
if(!$Cli){$Cli=Join-Path $project 'src/HyperBoostX.Cli/bin/Release/net8.0-windows/hbx.exe'}
if(!$DataDirectory){$DataDirectory=Join-Path $project 'artifacts/audit-data'}
if(!$OutputFile){$OutputFile=Join-Path $project 'artifacts/diagnostic-audit.json'}
$catalog=& $Cli registry --data-dir $DataDirectory
if($LASTEXITCODE -ne 0){throw 'Cannot read runtime registry.'}
$results=@()
foreach($center in (($catalog -join "`n")|ConvertFrom-Json).DiagnosticCenters){
 $timer=[Diagnostics.Stopwatch]::StartNew()
 $raw=& $Cli scan $center --data-dir $DataDirectory
 $code=$LASTEXITCODE
 if($code -eq 0){
  $report=($raw -join "`n")|ConvertFrom-Json
  $results += [pscustomobject]@{Center=$center;ExitCode=$code;Facts=$report.Facts.Count;Unavailable=@($report.Facts|Where-Object Availability -eq 'Unavailable').Count;Seconds=[math]::Round($timer.Elapsed.TotalSeconds,2)}
 }else{$results += [pscustomobject]@{Center=$center;ExitCode=$code}}
 Write-Output "$center : exit $code"
}
New-Item -ItemType Directory -Force -Path (Split-Path ([IO.Path]::GetFullPath($OutputFile)) -Parent)|Out-Null
$results|ConvertTo-Json|Set-Content -LiteralPath $OutputFile -Encoding UTF8
if(@($results|Where-Object ExitCode -ne 0).Count -gt 0){throw 'One or more diagnostic centers failed. Inspect the output report.'}
