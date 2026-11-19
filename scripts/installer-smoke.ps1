param([switch]$SkipInteractiveUi)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$runId=[Guid]::NewGuid().ToString('N')
$lab=Join-Path $root ('artifacts/install-lab/'+$runId)
$install=Join-Path $lab 'Installed HyperBoostX'
$data=Join-Path $lab 'installed-data'
$portable=Join-Path $lab 'Portable HyperBoostX'
$portableData=Join-Path $lab 'portable-data'
$publish=Join-Path $root 'artifacts/publish/win-x64'
$setup=Join-Path $root 'dist/HyperBoostX-v3-Setup-x64.exe'
$zip=Join-Path $root 'dist/HyperBoostX-v3-Portable-x64.zip'
$evidenceDir=Join-Path $root 'artifacts/installer-evidence'
$installKey='HKCU:\Software\HyperBoostX-v3'
$uninstallKey='HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX-v3'
$desktopShortcut=Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'HyperBoostX v3.lnk'
$startFolder=Join-Path ([Environment]::GetFolderPath('Programs')) 'HyperBoostX v3'
foreach($path in @($installKey,$uninstallKey,$desktopShortcut,$startFolder)){
 if(Test-Path -LiteralPath $path){throw 'An existing HyperBoostX v3 registration or shortcut is present. Use a disposable Windows account.'}
}
if(Test-Path -LiteralPath $lab){throw 'Install lab path already exists.'}
foreach($path in @($publish,$setup,$zip)){if(!(Test-Path -LiteralPath $path)){throw 'Build the current package before testing it.'}}
New-Item -ItemType Directory -Force -Path $lab,$evidenceDir | Out-Null
$results=New-Object 'System.Collections.Generic.List[object]'
$failure=$null
function Record([string]$Name,[string]$Status,[string]$Detail){
 $results.Add([pscustomobject]@{Test=$Name;Result=$Status;Detail=$Detail})
}
function Run-Setup([string]$Phase){
 $p=Start-Process -FilePath $setup -ArgumentList ('/S /D='+$install) -PassThru -WindowStyle Hidden
 if(!$p.WaitForExit(120000)){throw "$Phase exceeded timeout."}
 if($p.ExitCode -ne 0){throw "$Phase failed with code $($p.ExitCode)."}
}
function Assert-Payload([string]$Destination){
 $files=@(Get-ChildItem -LiteralPath $publish -File -Recurse)
 if($files.Count -lt 3){throw 'Published payload is incomplete.'}
 foreach($file in $files){
  $relative=$file.FullName.Substring($publish.Length+1)
  $target=Join-Path $Destination $relative
  if(!(Test-Path -LiteralPath $target -PathType Leaf)){throw "Payload file missing: $relative"}
  if((Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash){throw "Payload hash mismatch: $relative"}
 }
 return $files.Count
}
function Assert-Cli([string]$Directory,[string]$Data){
 $check=& (Join-Path $Directory 'hbx.exe') self-check --data-dir $Data
 if($LASTEXITCODE -ne 0){throw 'Recovery CLI failed.'}
 $result=$check | ConvertFrom-Json
 if($result.Database -ne 'Integrity checked' -or $result.Pending -ne 0){throw 'Unexpected fresh recovery state.'}
 if(!(Test-Path -LiteralPath (Join-Path $Data 'hyperboostx.db'))){throw 'Isolated recovery database missing.'}
 $sample=& (Join-Path $Directory 'hbx.exe') telemetry --data-dir $Data
 if($LASTEXITCODE -ne 0){throw 'Telemetry CLI failed.'}
 $metrics=$sample | ConvertFrom-Json
 if($metrics.TotalMemory -le 0 -or $metrics.AvailableMemory -lt 0 -or $metrics.AvailableMemory -gt $metrics.TotalMemory -or $null -eq $metrics.CpuPercent -or $metrics.CpuPercent -lt 0 -or $metrics.CpuPercent -gt 100){throw 'Invalid real Windows telemetry sample.'}
}
try{
 Run-Setup 'Fresh install'
 $count=Assert-Payload $install
 Assert-Cli $install $data
 Record 'Fresh install' 'PASS' ("SHA256 verified for {0} payload files; isolated CLI and telemetry passed." -f $count)
 # Deliberately damage only a known fixture file inside the new lab installation.
 $repairFile=Join-Path $install 'README.md'
 if(!(Test-Path -LiteralPath $repairFile -PathType Leaf)){throw 'Repair fixture file is missing.'}
 Set-Content -LiteralPath $repairFile -Value 'Intentional isolated repair fixture'
 Run-Setup 'Same-version repair'
 $count=Assert-Payload $install
 Assert-Cli $install $data
 Record 'Same-version repair' 'PASS' ("Corrupted fixture restored; {0} payload files match source." -f $count)
 $reg=Get-ItemProperty -LiteralPath $uninstallKey
 if($reg.UninstallString -ne ('"'+(Join-Path $install 'Uninstall.exe')+'"')){throw 'Uninstall command has incorrect quoting.'}
 if($reg.InstallLocation -ne $install -or $reg.DisplayVersion -ne '3.0.0'){throw 'Uninstall registration does not match current build.'}
 Record 'Uninstall registration' 'PASS' 'Location, version and quoting verified.'
 if($SkipInteractiveUi){Record 'Installed UI' 'NOT_RUN' 'Interactive desktop deliberately excluded; not release UI evidence.'}
 else{
  & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'ui-smoke.ps1') -Executable (Join-Path $install 'HyperBoostX.exe') -DataDirectory $data -OutputDirectory (Join-Path $lab 'installed-ui')
  if($LASTEXITCODE -ne 0){throw 'Installed UI test failed.'}
  Record 'Installed UI' 'PASS' 'Packaged executable smoke completed.'
 }
 Expand-Archive -LiteralPath $zip -DestinationPath $portable
 $count=Assert-Payload $portable
 Assert-Cli $portable $portableData
 Record 'Portable' 'PASS' ("{0} payload hashes, isolated CLI and actual telemetry passed from path containing spaces." -f $count)
 $marker=Join-Path $install 'user-file-must-survive.txt'
 Set-Content -LiteralPath $marker -Value 'Unrelated file must survive uninstallation.'
 $markerHash=(Get-FileHash -LiteralPath $marker -Algorithm SHA256).Hash
 $p=Start-Process -FilePath (Join-Path $install 'Uninstall.exe') -ArgumentList '/S' -PassThru -WindowStyle Hidden
 if(!$p.WaitForExit(120000)){throw 'Uninstall parent exceeded timeout.'}
 if($p.ExitCode -ne 0){throw 'Silent uninstall returned failure.'}
 # NSIS may spawn a temporary child; await real removal, not just parent exit.
 $deadline=[DateTime]::UtcNow.AddSeconds(60)
 while(((Test-Path -LiteralPath $uninstallKey) -or (Test-Path -LiteralPath (Join-Path $install 'Uninstall.exe'))) -and [DateTime]::UtcNow -lt $deadline){Start-Sleep -Milliseconds 250}
 foreach($entry in @(Get-ChildItem -LiteralPath $publish -File -Recurse)){
  $relative=$entry.FullName.Substring($publish.Length+1)
  if(Test-Path -LiteralPath (Join-Path $install $relative)){throw "Installed payload remained: $relative"}
 }
 foreach($path in @($installKey,$uninstallKey,$desktopShortcut,$startFolder,(Join-Path $install 'Uninstall.exe'))){if(Test-Path -LiteralPath $path){throw 'Uninstaller left a registered product artifact.'}}
 if(!(Test-Path -LiteralPath $marker) -or (Get-FileHash -LiteralPath $marker -Algorithm SHA256).Hash -ne $markerHash){throw 'Uninstaller changed an unrelated user file.'}
 if(!(Test-Path -LiteralPath (Join-Path $data 'hyperboostx.db'))){throw 'Recovery database was removed.'}
 Assert-Cli $portable $portableData
 Record 'Uninstall and preservation' 'PASS' 'Payload, registration and shortcuts removed; unrelated file and isolated database preserved; portable remains runnable.'
}
catch{
 $failure=$_.Exception.Message
 Record 'Qualification' 'FAIL' $failure
 throw
}
finally{
 $report=[ordered]@{schemaVersion=1;commit=$env:GITHUB_SHA;windowsBuild=[Environment]::OSVersion.Version.ToString();interactiveUiRequested=!$SkipInteractiveUi;setupSha256=(Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash;portableSha256=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash;setupSignature=(Get-AuthenticodeSignature -LiteralPath $setup).Status.ToString();results=@($results.ToArray())}
 $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidenceDir 'installer-smoke.json') -Encoding UTF8
 $results | Format-Table -AutoSize
}
