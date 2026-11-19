$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$runId=[Guid]::NewGuid().ToString('N').Substring(0,8)
$install=Join-Path $root ('artifacts/install-lab/'+$runId+'/HyperBoostX')
$data=Join-Path $root ('artifacts/install-lab/'+$runId+'/data')
if(Test-Path 'HKCU:\Software\HyperBoostX-v3'){throw 'An existing registered v3 installation is present; use an isolated Windows account for this test.'}
if(Test-Path $install){throw 'Install lab path already exists; inspect it before reusing.'}
$results=@()
foreach($phase in @('Fresh install','Same-version repair')){
 $p=Start-Process -FilePath (Join-Path $root 'dist/HyperBoostX-v3-Setup-x64.exe') -ArgumentList ('/S /D='+$install) -PassThru -Wait -WindowStyle Hidden
 if($p.ExitCode -ne 0){throw ($phase+' failed with exit '+$p.ExitCode)}
 if(!(Test-Path (Join-Path $install 'HyperBoostX.exe'))){throw 'Desktop executable missing.'}
 $check=& (Join-Path $install 'hbx.exe') self-check --data-dir $data
 if($LASTEXITCODE -ne 0){throw 'Installed recovery CLI failed.'}
 $results += [pscustomobject]@{Test=$phase;Result='PASS';Exit=$p.ExitCode}
}
$reg=Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX-v3'
if($reg.UninstallString -ne ('"'+(Join-Path $install 'Uninstall.exe')+'"')){throw 'Uninstall command has incorrect quoting.'}
$results += [pscustomobject]@{Test='Uninstall registration';Result='PASS'}
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'ui-smoke.ps1') -Executable (Join-Path $install 'HyperBoostX.exe') -DataDirectory $data -OutputDirectory (Join-Path $root 'artifacts/installed-ui')
if($LASTEXITCODE -ne 0){throw 'Installed UI test failed.'}
$results += [pscustomobject]@{Test='Installed UI';Result='PASS'}
$marker=Join-Path $install 'user-file-must-survive.txt'
Set-Content -LiteralPath $marker -Value 'Uninstaller must preserve files not shipped by the installer.'
$p=Start-Process -FilePath (Join-Path $install 'Uninstall.exe') -ArgumentList '/S' -Wait -PassThru -WindowStyle Hidden
if($p.ExitCode -ne 0){throw 'Silent uninstall failed.'}
if(Test-Path (Join-Path $install 'HyperBoostX.exe')){throw 'Desktop executable remained after uninstall.'}
if(Test-Path (Join-Path $install 'Uninstall.exe')){throw 'Uninstaller executable remained after uninstall.'}
if(@(Get-ChildItem -LiteralPath $install -Directory).Count -ne 0){throw 'Empty payload directories remained after uninstall.'}
if(!(Test-Path $marker)){throw 'Uninstaller removed unrelated user data.'}
if(!(Test-Path (Join-Path $data 'hyperboostx.db'))){throw 'Recovery data was removed.'}
if(Test-Path 'HKCU:\Software\HyperBoostX-v3'){throw 'Installation registration remained.'}
$results += [pscustomobject]@{Test='Silent uninstall / preserve unrelated file and app data';Result='PASS'}
$results | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'artifacts/installer-smoke.json') -Encoding UTF8
$results | Format-Table
