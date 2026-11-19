param(
    [string]$ActionMapPath = "wpf\Data\ui_action_map_v2_10.json",
    [string]$NavigationPath = "wpf\ViewModels\MainWindowViewModel.cs",
    [string]$RoutesPath = "wpf\MainWindow.xaml.cs",
    [string]$ReportPath = "docs\FULL_FEATURE_REALNESS_AUDIT_v2.10.0.md",
    [string]$JsonReportPath = "docs\runtime-audit\full_feature_realness_audit.json"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $repoRoot
try {
    $contractPath = "scripts\lib\HyperBoostXReleaseContract.ps1"
    if (Test-Path -LiteralPath $contractPath) {
        . $contractPath
        $contract = Get-HyperBoostXReleaseContract
    }
    else {
        $contract = [pscustomobject]@{
            ExpectedStableMenus = 73
            ExpectedStableButtons = 606
            ExpectedUniqueUiEndpoints = 173
            ExpectedNonRealVisibleInStable = 0
        }
    }

    $checks = New-Object System.Collections.Generic.List[object]
    function Add-Check([string]$Name, [bool]$Ok, [string]$Evidence) {
        $checks.Add([pscustomobject]@{
            name = $Name
            ok = $Ok
            evidence = $Evidence
        })
    }

    $payload = Get-Content -LiteralPath $ActionMapPath -Raw | ConvertFrom-Json
    $menus = @($payload.menus)
    $actions = @($menus | ForEach-Object { @($_.actions) })
    $uniqueEndpoints = @(@(
        foreach ($action in $actions) {
            $path = [string]$action.path
            if ([string]::IsNullOrWhiteSpace($path) -or -not $path.StartsWith("/api/")) { continue }
            "{0} {1}" -f ([string]$action.method).ToUpperInvariant(), $path.Split("?")[0]
        }
    ) | Sort-Object -Unique)

    $nonRealMenus = @($menus | Where-Object { [string]$_.status -ne "Real" })
    $nonRealActions = @($actions | Where-Object { [string]$_.status -ne "Real" -or $_.partial -eq $true })
    $placeholderMenus = @($menus | Where-Object {
        [string]$_.key -eq "Default" -or
        [string]$_.category -eq "Fallback" -or
        [string]$_.label -match "Default Fallback|Fallback"
    })
    $placeholderActions = @($actions | Where-Object { [string]$_.label -match "Default Fallback|Fallback" })
    $unguardedMutations = @($actions | Where-Object { [string]$_.method -ne "GET" -and $_.safety_guard -ne $true })
    $mutationsWithoutPreview = @($actions | Where-Object {
        [string]$_.method -ne "GET" -and $_.preview_required -ne $true -and [string]$_.id -notmatch "\.preview\."
    })
    $applyRestoreWithoutRestore = @($actions | Where-Object {
        ([string]$_.id -match "\.(apply|restore)\." -or $_.is_destructive -eq $true) -and $_.restore -ne $true
    })

    $navigationSource = Get-Content -LiteralPath $NavigationPath -Raw
    $routeSource = Get-Content -LiteralPath $RoutesPath -Raw
    $navigationKeys = @([regex]::Matches($navigationSource, 'Key\s*=\s*"([^"]+)"') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
    $menuKeys = @($menus | ForEach-Object { [string]$_.key })
    $missingNavigation = @($menuKeys | Where-Object { $navigationKeys -notcontains $_ })
    $missingRoutes = @($menuKeys | Where-Object {
        $key = [regex]::Escape($_)
        $routeSource -notmatch "_navigationService\.Register\(`"$key`"" -and
        $routeSource -notmatch "RegisterLegacyRoute\(`"$key`""
    })

    $requiredEndpoints = @(
        "GET /api/action-log",
        "GET /api/lcd/modes",
        "GET /api/license/status",
        "POST /api/license/activate-local",
        "POST /api/license/deactivate-local",
        "POST /api/license/refresh-local"
    )
    $missingRequiredEndpoints = @($requiredEndpoints | Where-Object { $uniqueEndpoints -notcontains $_ })
    $requiredMenuKeys = @("ActionHistory", "CloudSyncLicense", "LcdPerformanceGuard", "SystemRealityGuard", "ReleaseReadiness")
    $missingRequiredMenus = @($requiredMenuKeys | Where-Object { $menuKeys -notcontains $_ })

    $staleDocs = New-Object System.Collections.Generic.List[object]
    $skipDocPaths = @()
    foreach ($pathToSkip in @($ReportPath, $JsonReportPath)) {
        if (Test-Path -LiteralPath $pathToSkip) {
            $skipDocPaths += (Resolve-Path -LiteralPath $pathToSkip).Path
        }
    }

    if (Test-Path -LiteralPath "docs") {
        Get-ChildItem -LiteralPath "docs" -Recurse -File -Include *.md,*.json |
            Where-Object { $_.FullName -notmatch "\\archive\\" -and $skipDocPaths -notcontains $_.FullName } |
            ForEach-Object {
                $text = Get-Content -LiteralPath $_.FullName -Raw
                $hits = @()
                foreach ($pattern in @("Default Fallback", "STABLE_READY_UNSIGNED", "stable unsigned release", "OWNER_LAB_REPAIR_REQUIRED")) {
                    if ($text -match [regex]::Escape($pattern)) { $hits += $pattern }
                }
                if ($hits.Count -gt 0) {
                    $staleDocs.Add([pscustomobject]@{
                        path = $_.FullName.Substring($repoRoot.Length + 1)
                        hits = $hits
                    })
                }
            }
    }

    Add-Check "action map menu count" ($menus.Count -eq $contract.ExpectedStableMenus) "actual=$($menus.Count); expected=$($contract.ExpectedStableMenus)"
    Add-Check "action map button count" ($actions.Count -eq $contract.ExpectedStableButtons) "actual=$($actions.Count); expected=$($contract.ExpectedStableButtons)"
    Add-Check "action map unique endpoints" ($uniqueEndpoints.Count -eq $contract.ExpectedUniqueUiEndpoints) "actual=$($uniqueEndpoints.Count); expected=$($contract.ExpectedUniqueUiEndpoints)"
    Add-Check "all menu statuses are Real" ($nonRealMenus.Count -eq 0) "non_real_menus=$($nonRealMenus.Count)"
    Add-Check "all action statuses are Real" ($nonRealActions.Count -eq 0) "non_real_actions=$($nonRealActions.Count)"
    Add-Check "no Default/Fallback menu remains" ($placeholderMenus.Count -eq 0 -and $placeholderActions.Count -eq 0) "menus=$($placeholderMenus.Count); actions=$($placeholderActions.Count)"
    Add-Check "all menus are visible in WPF navigation" ($missingNavigation.Count -eq 0) (($missingNavigation -join ", ") -replace "^$", "none")
    Add-Check "all menus have WPF routes" ($missingRoutes.Count -eq 0) (($missingRoutes -join ", ") -replace "^$", "none")
    Add-Check "non-GET actions keep Safety Guard" ($unguardedMutations.Count -eq 0) "unguarded_mutations=$($unguardedMutations.Count)"
    Add-Check "non-preview mutations require preview state" ($mutationsWithoutPreview.Count -eq 0) "missing_preview=$($mutationsWithoutPreview.Count)"
    Add-Check "apply/restore/destructive actions expose restore boundary" ($applyRestoreWithoutRestore.Count -eq 0) "missing_restore=$($applyRestoreWithoutRestore.Count)"
    Add-Check "required owner-lab menus exist" ($missingRequiredMenus.Count -eq 0) (($missingRequiredMenus -join ", ") -replace "^$", "none")
    Add-Check "required real endpoints exist" ($missingRequiredEndpoints.Count -eq 0) (($missingRequiredEndpoints -join ", ") -replace "^$", "none")
    Add-Check "non-archive docs have no stale release/fallback claims" ($staleDocs.Count -eq 0) "stale_files=$($staleDocs.Count)"

    $failed = @($checks | Where-Object { -not $_.ok })
    $status = if ($failed.Count -eq 0) { "PASS" } else { "FAIL" }
    $metrics = [ordered]@{
        menus = $menus.Count
        buttons = $actions.Count
        active_buttons = [int]$payload.summary.total_active_buttons
        unique_endpoints = $uniqueEndpoints.Count
        partial_or_roadmap_buttons = [int]$payload.summary.total_partial_or_roadmap_buttons
        guarded_destructive_buttons = [int]$payload.summary.total_guarded_destructive_buttons
        mutating_buttons = @($actions | Where-Object { [string]$_.method -ne "GET" }).Count
        preview_required_buttons = @($actions | Where-Object { $_.preview_required -eq $true }).Count
        restore_boundary_buttons = @($actions | Where-Object { $_.restore -eq $true }).Count
        admin_required_buttons = @($actions | Where-Object { $_.requires_admin -eq $true }).Count
    }

    $report = [ordered]@{
        generated_at = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss zzz")
        status = $status
        release_status = "OWNER_LAB_RELEASE_CANDIDATE_RUNTIME_BLOCKED"
        public_stable = $false
        metrics = $metrics
        checks = $checks.ToArray()
        stale_docs = $staleDocs.ToArray()
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $JsonReportPath) | Out-Null
    $report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $JsonReportPath -Encoding UTF8

    $lines = @()
    $lines += "# Full Feature Realness Audit v2.10.0"
    $lines += ""
    $lines += "Generated: $($report.generated_at)"
    $lines += ""
    $lines += "Status: ``$status``"
    $lines += ""
    $lines += "Release status: ``OWNER_LAB_RELEASE_CANDIDATE_RUNTIME_BLOCKED``. The UI/action/source surface is audited as real and route-backed, but public stable remains blocked until installed-runtime, rollback/admin, hardware matrix, and signing/public-release evidence pass."
    $lines += ""
    $lines += "## Metrics"
    $lines += ""
    $lines += "| Metric | Count |"
    $lines += "| --- | ---: |"
    foreach ($key in $metrics.Keys) {
        $lines += "| $($key -replace '_', ' ') | $($metrics[$key]) |"
    }
    $lines += ""
    $lines += "## Checks"
    $lines += ""
    $lines += "| Check | Result | Evidence |"
    $lines += "| --- | --- | --- |"
    foreach ($check in $checks) {
        $result = if ($check.ok) { "PASS" } else { "FAIL" }
        $lines += "| $($check.name) | $result | $($check.evidence) |"
    }
    $lines += ""
    $lines += "## Realness Boundary"
    $lines += ""
    $lines += "- ``Real`` means visible, route-backed, locally testable, and Safety Guard aware."
    $lines += "- Driver install, plugin installation, production license enforcement, cloud sync, vendor RGB/LCD control, Defender disable, permanent Windows Update disable, BIOS/OC/undervolt, and broad personal-file cleanup are not claimed as automatic production actions."
    $lines += "- Safe optimizer behavior intentionally returns preview, manual handoff, blocked, or restore-required states when a Windows action is risky."
    $lines += ""
    $lines += "## Answer"
    $lines += ""
    if ($status -eq "PASS") {
        $lines += "All mapped v2.10 pages and buttons are real as an owner-lab release candidate: 73 menus, 606 active buttons, 173 unique API endpoints, no Default/Fallback placeholder, no non-real stable-visible action, and all mutating actions guarded."
    }
    else {
        $lines += "The audit found blockers. See failed checks above before calling the UI/action surface complete."
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $ReportPath) | Out-Null
    $lines | Set-Content -LiteralPath $ReportPath -Encoding UTF8

    [pscustomobject]@{
        Status = $status
        Menus = $metrics.menus
        Buttons = $metrics.buttons
        UniqueEndpoints = $metrics.unique_endpoints
        GuardedDestructiveButtons = $metrics.guarded_destructive_buttons
        FailedChecks = $failed.Count
        ReportPath = $ReportPath
        JsonReportPath = $JsonReportPath
    }

    if ($failed.Count -gt 0) {
        exit 1
    }
}
finally {
    Pop-Location
}
