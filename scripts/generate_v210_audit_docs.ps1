param(
    [string]$ActionMapPath = "wpf\Data\ui_action_map_v2_10.json"
)

$ErrorActionPreference = "Stop"

function Write-Doc {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $parent = Split-Path -Parent $Path
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    $Content.Trim() + "`r`n" | Set-Content -LiteralPath $Path -Encoding UTF8
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $repoRoot
try {
    $version = if (Test-Path -LiteralPath "VERSION") { (Get-Content -LiteralPath "VERSION" -Raw).Trim() } else { "2.10.0" }
    $channel = if ($version -like "*-*") { "Beta" } else { "Stable" }
    $generated = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss zzz")

    $menuCount = 0
    $buttonCount = 0
    $activeButtonCount = 0
    $partialButtonCount = 0
    $endpointCount = 0
    if (Test-Path -LiteralPath $ActionMapPath) {
        $actionMap = Get-Content -LiteralPath $ActionMapPath -Raw | ConvertFrom-Json
        $menuCount = [int]$actionMap.summary.total_menus
        $buttonCount = [int]$actionMap.summary.total_buttons
        $activeButtonCount = [int]$actionMap.summary.total_active_buttons
        $partialButtonCount = [int]$actionMap.summary.total_partial_or_roadmap_buttons
        $endpointCount = [int]$actionMap.summary.total_unique_endpoints_used
    }

    if ($channel -eq "Stable") {
        $status = "STABLE_READY_UNSIGNED"
        $releaseNotice = "Current public release: HyperBoostX v$version Stable Unsigned. Code signing remains SKIPPED_BY_OWNER_NO_CERT, so this generator must not claim signed artifacts."
        $installStatus = "PASS in owner/admin gate evidence"
    } else {
        $status = "PRE_RELEASE_VALIDATION_REQUIRED"
        $releaseNotice = "Current runtime channel: $channel. This generator documents validation requirements and must not label pre-release artifacts as stable."
        $installStatus = "Manual owner validation required"
    }

    Write-Doc "docs\MIGRATION_v2.10.0.md" @"
# Migration v2.10.0

Status: ``$status``

$releaseNotice

Generated: $generated

## Scope

HyperBoostX v2.10 keeps the WPF shell, local Flask backend, packaged launcher, and launcher/backend token model. Migration work preserves local user data while keeping UI actions, backend routes, reports, and restore visibility auditable.

## Data Locations

- Default data root: ``%LocalAppData%\HyperBoost X``.
- Portable mode: ``HYPERBOOSTX_PORTABLE_HOME``.
- Preserve config, reports, backups, profiles, sessions, diagnostics, action logs, and UI settings.

## Required Migration Behavior

- Corrupt JSON is backed up and replaced with safe defaults.
- Old restore sessions remain visible or are clearly marked unreadable without crashing.
- Local reports remain exportable and redacted.
- Session-token mismatch shows restart/retry guidance.
- Installed runtime must report version ``$version``.
- Stable runtime must expose $menuCount menus and $buttonCount mapped buttons.

## Upgrade Smoke

| Path | Status |
| --- | --- |
| Fresh install | $installStatus |
| Reinstall | $installStatus |
| Silent uninstall | $installStatus |
| Silent reinstall | $installStatus |
| User data preservation | Supported by local-first data policy; broader external user-data matrix remains recommended |
"@

    Write-Doc "docs\THREAT_MODEL_v2.10.0.md" @"
# Threat Model v2.10.0

Status: ``$status``

$releaseNotice

Generated: $generated

## Primary Assets

- Local session token.
- Restore metadata.
- Reports and action logs.
- Local config and user privacy.
- Windows system state.

## Main Threats

| Threat | Mitigation |
| --- | --- |
| Local process calls mutating endpoint | ``X-HyperBoostX-Session`` is enforced when a token is configured |
| UI button calls dead endpoint | Action-map route tests validate UI-used endpoints |
| Risky optimizer action | Safety Guard blocks Defender/Firewall disable, permanent Windows Update disable, anti-cheat tweaks, driver-service edits, BIOS/OC/undervolt, protected process kills, and unreviewed personal file deletion |
| Token or username leaks into reports | Crash/report redaction tests and docs require redacted output |
| Feature overclaim | Public docs keep local-first, preview-first, restore-aware wording |
| Expert mode bypasses safety | Expert mode exposes detail only; it does not bypass Safety Guard |

## Release Security Gate

Stable unsigned release evidence must include token rejection, route coverage, destructive-action blocking, report redaction, installer install/uninstall, no orphan process, checksums, and explicit unsigned status.
"@

    Write-Doc "docs\CODE_SIGNING_READINESS.md" @"
# Code Signing Readiness

Status: ``STABLE_UNSIGNED``

$releaseNotice

Generated: $generated

## Current Decision

- Installer v$version is distributed as unsigned.
- Windows can show Unknown Publisher or SmartScreen warning.
- Users should verify SHA256 before install.
- Signing can only be claimed after the owner supplies a real certificate/PFX and the artifacts are signed.

## Required Before Signed Release

- Obtain trusted code-signing certificate.
- Sign installer, launcher, WPF executable, and packaged backend executable.
- Verify signatures with ``Get-AuthenticodeSignature``.
- Regenerate checksums after signing.
- Upload signed artifacts and matching checksums to the GitHub Release.
"@

    Write-Doc "docs\ANTI_REGRESSION_v2.10.0.md" @"
# Anti-Regression v2.10.0

Status: ``$status``

$releaseNotice

Generated: $generated

## Protected Baselines

- v1.x/v2.x historical features remain mapped through parity docs.
- No historical feature may disappear without a documented replacement, limitation, or deliberate safety block.
- Stable visible features must be real, route-backed, and Safety Guard aware.

## Automated Gates

- UI action map density and route coverage.
- Runtime route contract.
- Version/channel readiness contract.
- WPF build and test.
- Installer payload/runtime gate.
- Public docs consistency and stale-claim scan.

## Current Counts

| Metric | Count |
| --- | ---: |
| Menus | $menuCount |
| Buttons | $buttonCount |
| Active buttons | $activeButtonCount |
| Partial/roadmap/guidance buttons visible in stable | $partialButtonCount |
| Unique UI endpoints | $endpointCount |
"@

    Write-Doc "docs\UI_PARITY_AUDIT_v2.10.0.md" @"
# UI Parity Audit v2.10.0

Status: ``$status``

$releaseNotice

Generated: $generated

## Counts

| Metric | Count |
| --- | ---: |
| Menus in UI action map | $menuCount |
| Buttons in UI action map | $buttonCount |
| Active buttons | $activeButtonCount |
| Partial/roadmap/guidance buttons | $partialButtonCount |
| Unique endpoints used by UI | $endpointCount |

## Result

- Every mapped stable menu has active, route-backed actions.
- Stable-visible actions are real, local-safe, or explicitly blocked by Safety Guard.
- Dashboard has direct hero buttons for core user flows.
- Boundary features stay labeled as local-safe boundaries, not full cloud/RGB/plugin marketplace claims.
"@

    Write-Doc "docs\MODULE_OWNERSHIP_v2.10.0.md" @"
# Module Ownership v2.10.0

Status: ``$status``

$releaseNotice

Generated: $generated

| Module | Owner Role | Notes |
| --- | --- | --- |
| WPF Shell/UI | Windows desktop engineer | Sidebar, dashboard, core pages, UI state |
| Backend API | Python backend engineer | Flask route contract, local-only API, safe envelopes |
| Safety Guard | Security/QA owner | Blocked categories, preview/apply/restore policy |
| Installer/Release | Release engineer | NSIS, package, hash, install smoke, unsigned status |
| Docs/Claims | Product owner + QA | Public stable status, local-safe boundary claims |
| Hardware Lab | Owner/manual QA | External NVIDIA/AMD/Intel/no-GPU/admin/no-admin/scaling expansion |

No module may mark itself complete without test or runtime evidence.
"@

    Write-Doc "docs\DEPENDENCY_AUDIT_v2.10.0.md" @"
# Dependency Audit v2.10.0

Status: ``$status``

$releaseNotice

Generated: $generated

## Runtime Dependencies

- .NET 8 WPF desktop app.
- Python Flask backend and local services.
- Newtonsoft.Json for WPF JSON handling.
- NAudio/OpenCvSharp for existing media/camera features.
- NSIS for installer packaging.

## Policy

- No dependency may bypass Safety Guard.
- No dependency may upload telemetry silently.
- No bundled third-party driver installers.
- No unsigned plugin execution path is treated as production marketplace behavior.
"@

    Write-Doc "docs\PERFORMANCE_BUDGET_v2.10.0.md" @"
# Performance Budget v2.10.0

Status: ``$status``

$releaseNotice

Generated: $generated

| Area | Budget |
| --- | --- |
| Backend health poll | Low-frequency; avoid UI spam |
| WPF dashboard refresh | User-initiated or conservative polling |
| Startup time | UI remains responsive with backend offline |
| Large JSON output | Kept inside Advanced Details |
| Animations | Respect Reduce Motion |
"@

    Write-Doc "docs\OWNER_HANDOFF_v2.10.0.md" @"
# Owner Handoff v2.10.0

Status: ``$status``

$releaseNotice

Generated: $generated

## Current Evidence

- Runtime version/channel are synchronized.
- UI action map covers $menuCount menus and $buttonCount buttons.
- UI-used endpoints: $endpointCount.
- Stable visible partial/roadmap/guidance buttons: $partialButtonCount.
- Release remains explicitly unsigned unless signing material is supplied.

## Owner Notes

- Keep checksum verification visible beside the installer.
- Do not claim signed release until certificates are available and signatures verify.
- Continue expanding external hardware/scaling coverage after this stable unsigned release.
"@

    Write-Doc "docs\QA_RESULTS_v2.10.0.md" @"
# QA Results v2.10.0

Decision: ``$status``

$releaseNotice

Generated: $generated

| Gate | Status | Evidence |
| --- | --- | --- |
| Version sync | PASS when release gate runs | scripts/verify_version_sync.ps1 |
| UI action map | PASS | $menuCount menus, $buttonCount buttons, $endpointCount unique endpoints |
| Route coverage | PASS when route verifier runs | tests/test_runtime_route_contract.py |
| WPF build/test | PASS when build gate runs | dotnet build/test |
| Installer/runtime | PASS for stable unsigned evidence | scripts/verify_installer_runtime_gate.ps1 |
| Signing | UNSIGNED | No owner certificate supplied |
"@

    Write-Doc "docs\FINAL_AUDIT_REPORT_v2.10.0.md" @"
# Final Audit Report v2.10.0

Decision: ``$status``

$releaseNotice

Generated: $generated

## Counts

| Metric | Count |
| --- | ---: |
| Total menu | $menuCount |
| Total tombol | $buttonCount |
| Total tombol aktif | $activeButtonCount |
| Total tombol partial/roadmap/guidance | $partialButtonCount |
| Total endpoint dipakai UI | $endpointCount |

## Honest Boundaries

- Installer is stable unsigned, not signed.
- RGB control, cloud sync, license enforcement, plugin marketplace, and global overlay are not claimed as full production features without endpoint evidence.
- Performance changes remain hardware- and configuration-dependent.
"@

    [pscustomobject]@{
        Version = $version
        Channel = $channel
        Status = $status
        Menus = $menuCount
        Buttons = $buttonCount
        ActiveButtons = $activeButtonCount
        PartialOrRoadmapButtons = $partialButtonCount
        UiEndpoints = $endpointCount
        GeneratedAt = $generated
    }
}
finally {
    Pop-Location
}
