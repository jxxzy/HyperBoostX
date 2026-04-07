param(
    [string]$StableVersion = "1.1.0",
    [string]$StableUiLabel = "1.1.0 - 2026",
    [switch]$WhatIfOnly
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $repoRoot

$targets = @(
    @{
        Path = "wpf\HyperBoostX.csproj"
        Replacements = @(
            @{ Old = "<Version>1.1.0-beta</Version>"; New = "<Version>$StableVersion</Version>" },
            @{ Old = "<InformationalVersion>1.1.0-beta</InformationalVersion>"; New = "<InformationalVersion>$StableVersion</InformationalVersion>" }
        )
    },
    @{
        Path = "launcher\HyperBoostLauncher.csproj"
        Replacements = @(
            @{ Old = "<Version>1.1.0-beta</Version>"; New = "<Version>$StableVersion</Version>" },
            @{ Old = "<InformationalVersion>1.1.0-beta</InformationalVersion>"; New = "<InformationalVersion>$StableVersion</InformationalVersion>" }
        )
    },
    @{
        Path = "HyperBoostXInstaller.nsi"
        Replacements = @(
            @{ Old = 'WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX" "DisplayVersion" "1.1.0-beta"'; New = 'WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX" "DisplayVersion" "' + $StableVersion + '"' }
        )
    },
    @{
        Path = "wpf\MainWindow.xaml"
        Replacements = @(
            @{ Pattern = 'Name="AboutVersionText"\s+Text="[^"]+"'; New = 'Name="AboutVersionText" Text="' + $StableUiLabel + '"' }
        )
    },
    @{
        Path = "wpf\MainWindow.xaml.cs"
        Replacements = @(
            @{ Old = '?? "1.1.0-beta";'; New = '?? "' + $StableVersion + '";' }
        )
    },
    @{
        Path = "wpf\Services\AppUpdateService.cs"
        Replacements = @(
            @{ Old = 'client.DefaultRequestHeaders.UserAgent.ParseAdd("HyperBoostX/1.1.0-beta");'; New = 'client.DefaultRequestHeaders.UserAgent.ParseAdd("HyperBoostX/' + $StableVersion + '");' }
        )
    },
    @{
        Path = "app\__init__.py"
        Replacements = @(
            @{ Old = '__version__ = "1.1.0-beta"'; New = '__version__ = "' + $StableVersion + '"' }
        )
    },
    @{
        Path = "app\core\config.py"
        Replacements = @(
            @{ Old = 'VERSION = "1.1.0-beta"'; New = 'VERSION = "' + $StableVersion + '"' }
        )
    },
    @{
        Path = "app\api\health.py"
        Replacements = @(
            @{ Old = '"version": "1.1.0-beta"'; New = '"version": "' + $StableVersion + '"' }
        )
    },
    @{
        Path = "app\dev_client.py"
        Replacements = @(
            @{ Old = 'app.setApplicationVersion("1.1.0-beta")'; New = 'app.setApplicationVersion("' + $StableVersion + '")' }
        )
    },
    @{
        Path = "README.md"
        Replacements = @(
            @{ Old = '- `1.1.0-beta`'; New = ('- `' + $StableVersion + '`') },
            @{ Old = '## What changed in `1.1.0-beta`'; New = ('## What changed in `' + $StableVersion + '`') },
            @{ Old = '- GitHub prerelease: `v1.1.0-beta`'; New = ('- GitHub release: `v' + $StableVersion + '`') }
        )
    }
)

foreach ($target in $targets) {
    $path = Join-Path $repoRoot $target.Path
    if (-not (Test-Path $path)) {
        throw "Missing target file: $($target.Path)"
    }

    $content = Get-Content -LiteralPath $path -Raw
    $updated = $content

    foreach ($replacement in $target.Replacements) {
        if ($replacement.ContainsKey("Pattern")) {
            $next = [System.Text.RegularExpressions.Regex]::Replace($updated, $replacement.Pattern, $replacement.New)
            if ($next -ne $updated) {
                $updated = $next
            }
            else {
                Write-Warning "Pattern not found in $($target.Path): $($replacement.Pattern)"
            }
        }
        elseif ($updated.Contains($replacement.Old)) {
            $updated = $updated.Replace($replacement.Old, $replacement.New)
        }
        else {
            Write-Warning "Text not found in $($target.Path): $($replacement.Old)"
        }
    }

    if ($updated -ne $content) {
        if ($WhatIfOnly) {
            Write-Host "[DRY RUN] Would update $($target.Path)"
        }
        else {
            [System.IO.File]::WriteAllText($path, $updated, [System.Text.Encoding]::UTF8)
            Write-Host "Updated $($target.Path)"
        }
    }
    else {
        Write-Host "No change needed for $($target.Path)"
    }
}

Write-Host ""
Write-Host "Stable release preparation script completed."
if ($WhatIfOnly) {
    Write-Host "No files were modified because -WhatIfOnly was used."
}
