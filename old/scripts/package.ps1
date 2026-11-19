$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
 & dotnet test HyperBoostX.sln -c Release --nologo --logger 'trx;LogFileName=core.trx' --results-directory artifacts/test-results
 if($LASTEXITCODE -ne 0){throw 'Tests failed.'}
 $publish=Join-Path $root 'artifacts/publish/win-x64'
 # Preserve the previous payload and always publish into an empty directory.
 # A reused publish tree can silently ship stale DLLs and nested license folders.
 if(Test-Path -LiteralPath $publish){
  $resolved=[IO.Path]::GetFullPath($publish)
  $expected=[IO.Path]::GetFullPath((Join-Path $root 'artifacts/publish/win-x64'))
  $archive=[IO.Path]::GetFullPath((Join-Path $root ('artifacts/publish/previous-'+[Guid]::NewGuid().ToString('N'))))
  $allowed=[IO.Path]::GetFullPath((Join-Path $root 'artifacts/publish'))+[IO.Path]::DirectorySeparatorChar
  if($resolved -ne $expected -or !$archive.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase) -or ((Get-Item -LiteralPath $resolved).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Unsafe publish directory.'}
  Move-Item -LiteralPath $resolved -Destination $archive
 }
 & dotnet publish src/HyperBoostX.App/HyperBoostX.App.csproj -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o $publish
 if($LASTEXITCODE -ne 0){throw 'Desktop publish failed.'}
 & dotnet publish src/HyperBoostX.Cli/HyperBoostX.Cli.csproj -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o $publish
 if($LASTEXITCODE -ne 0){throw 'CLI publish failed.'}
 & dotnet publish src/HyperBoostX.Worker/HyperBoostX.Worker.csproj -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o $publish
 if($LASTEXITCODE -ne 0){throw 'Privileged worker publish failed.'}
 if(!(Test-Path -LiteralPath (Join-Path $publish 'HyperBoostX.Worker.exe'))){throw 'Published worker executable is missing.'}
 Copy-Item -LiteralPath README.md,RELEASE_STATUS.md,PRIVACY.md,THIRD_PARTY_NOTICES.md -Destination $publish
 New-Item -ItemType Directory -Force -Path (Join-Path $publish 'docs') | Out-Null
 Copy-Item -LiteralPath docs/AUDIT_FIX_2026-11-16.md -Destination (Join-Path $publish 'docs')
 Copy-Item -LiteralPath packaging/licenses -Destination $publish -Recurse -Force
 $files=Get-ChildItem -LiteralPath $publish -File -Recurse
 $install=@();$uninstall=@();$directories=@()
 foreach($file in $files){
  $relative=$file.FullName.Substring($publish.Length+1)
  $parent=Split-Path $relative -Parent
  $install += 'SetOutPath "$INSTDIR'+ $(if($parent){'\'+$parent}else{''})+'"'
  $install += 'File "'+$file.FullName+'"'
  $uninstall += 'Delete "$INSTDIR\'+$relative+'"'
  if($parent){$directories += $parent}
 }
 foreach($dir in @($directories | Sort-Object -Unique | Sort-Object Length -Descending)){$uninstall += 'RMDir "$INSTDIR\'+$dir+'"'}
 $install | Set-Content -LiteralPath packaging/payload-install.nsh -Encoding UTF8
 $uninstall | Set-Content -LiteralPath packaging/payload-uninstall.nsh -Encoding UTF8
 New-Item -ItemType Directory -Force -Path dist | Out-Null
 & 'C:\Program Files (x86)\NSIS\makensis.exe' /WX /V2 packaging/installer.nsi
 if($LASTEXITCODE -ne 0){throw 'Installer compile failed.'}
 Compress-Archive -Path ($publish+'\*') -DestinationPath dist/HyperBoostX-v3-Portable-x64.zip -Force
 Get-FileHash dist/HyperBoostX-v3-Setup-x64.exe,dist/HyperBoostX-v3-Portable-x64.zip -Algorithm SHA256 | ForEach-Object {$_.Hash+'  '+(Split-Path $_.Path -Leaf)} | Set-Content -LiteralPath dist/SHA256SUMS.txt -Encoding ASCII
 Write-Output 'Installer, portable archive and checksums created in dist.'
} finally {Pop-Location}
