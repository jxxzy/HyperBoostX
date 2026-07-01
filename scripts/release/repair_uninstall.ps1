$ErrorActionPreference = "Stop"

$installDir = Join-Path ${env:ProgramFiles} "HyperBoostX"
$startMenuDir = Join-Path ${env:ProgramData} "Microsoft\Windows\Start Menu\Programs\HyperBoostX"
$desktopShortcut = Join-Path ${env:Public} "Desktop\HyperBoostX.lnk"
$uninstallKey = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX"
$processes = @("HyperBoostLauncher", "HyperBoostX", "hyperboost_backend")

function Stop-HyperBoostProcesses {
    Write-Host "Stopping HyperBoostX processes..."

    foreach ($name in $processes) {
        Get-Process -Name $name -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Process -FilePath "taskkill.exe" -ArgumentList "/IM $name.exe /T /F" -WindowStyle Hidden -Wait -ErrorAction SilentlyContinue
    }

    if (Test-Path $installDir) {
        $installedProcesses = Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object {
            $_.ExecutablePath -and $_.ExecutablePath.StartsWith($installDir, [System.StringComparison]::OrdinalIgnoreCase)
        }

        foreach ($process in $installedProcesses) {
            Invoke-CimMethod -InputObject $process -MethodName Terminate -ErrorAction SilentlyContinue | Out-Null
        }
    }

    Start-Sleep -Seconds 2
}

function Remove-InstallDirectory {
    if (!(Test-Path $installDir)) {
        return
    }

    Write-Host "Removing install directory..."
    $resolvedInstallDir = (Resolve-Path -LiteralPath $installDir).Path
    $programFilesRoot = [System.IO.Path]::GetFullPath(${env:ProgramFiles}).TrimEnd('\')
    if (-not $resolvedInstallDir.StartsWith($programFilesRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove path outside Program Files: $resolvedInstallDir"
    }

    Get-ChildItem -LiteralPath $installDir -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            $_.Attributes = 'Normal'
        } catch {
        }
    }

    $pathsToRemove = @(
        (Join-Path $installDir "launcher"),
        (Join-Path $installDir "wpf"),
        (Join-Path $installDir "backend"),
        $installDir
    )

    foreach ($path in $pathsToRemove) {
        if (Test-Path $path) {
            $resolvedPath = (Resolve-Path -LiteralPath $path).Path
            if (-not $resolvedPath.StartsWith($resolvedInstallDir, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "Refusing to remove path outside install directory: $resolvedPath"
            }

            try {
                Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction Stop
            } catch {
                Start-Sleep -Milliseconds 400
                Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction Stop
            }
        }
    }
}

Stop-HyperBoostProcesses

Write-Host "Removing shortcuts..."
if (Test-Path $desktopShortcut) {
    Remove-Item -LiteralPath $desktopShortcut -Force
}

if (Test-Path $startMenuDir) {
    Remove-Item -LiteralPath $startMenuDir -Recurse -Force
}

Write-Host "Removing uninstall registry entry..."
if (Test-Path $uninstallKey) {
    Remove-Item -LiteralPath $uninstallKey -Recurse -Force
}

Remove-InstallDirectory

Write-Host ""
Write-Host "HyperBoostX cleanup selesai."
