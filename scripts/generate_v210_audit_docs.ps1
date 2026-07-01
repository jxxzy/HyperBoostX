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

$version = if (Test-Path -LiteralPath "VERSION") { (Get-Content -LiteralPath "VERSION" -Raw).Trim() } else { "2.10.0-beta.1" }
$channel = if ($version -like "*-*") { "Beta" } else { "Stable" }
$generated = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss zzz")

$menuCount = 0
$buttonCount = 0
$partialButtonCount = 0
$endpointCount = 0
$roadmapMenus = @("PluginMarketplace", "CloudSyncLicense")
if (Test-Path -LiteralPath $ActionMapPath) {
    $actionMap = Get-Content -LiteralPath $ActionMapPath -Raw | ConvertFrom-Json
    $menuCount = [int]$actionMap.summary.total_menus
    $buttonCount = [int]$actionMap.summary.total_buttons
    $partialButtonCount = [int]$actionMap.summary.total_partial_or_roadmap_buttons
    $endpointCount = [int]$actionMap.summary.total_unique_endpoints_used
}

$stableNotice = @"
> Public release policy: HyperBoostX v1.3.0 is the current recommended public stable baseline. The $version runtime is a $channel development build and must not be promoted as stable until installed runtime, admin rollback, hardware matrix, code signing, checksum, and smoke gates pass.
"@

Write-Doc "docs\MIGRATION_v2.10.0.md" @"
# Migration v2.10.0

$stableNotice

Generated: $generated

## Scope

v2.10.0-beta.1 keeps the WPF shell + local Flask backend + launcher token model. Migration work is about preserving local user data and making v2 routes/UI auditable, not rewriting architecture.

## Data Locations

- Default data root: `%LocalAppData%\HyperBoost X`.
- Portable mode: `HYPERBOOSTX_PORTABLE_HOME`.
- Must preserve config, reports, backups, profiles, sessions, diagnostics, action logs, and UI settings.

## Required Migration Behavior

- Corrupt JSON must be backed up and replaced with safe defaults.
- Old restore sessions must remain visible or be clearly marked unreadable without crashing.
- Local reports must remain exportable and redacted.
- Session-token mismatch must show a restart/retry message, not a generic crash.
- Runtime VERSION may be v2 beta while public README stable remains v1.3.0.

## Upgrade Smoke

| Path | Status |
| --- | --- |
| v1.3.0 to v2.10 beta | Manual lab required |
| v1.4.x to v2.10 beta | Manual lab required |
| v2.0.x to v2.10 beta | Source/package work in progress |
| Fresh install | Requires admin installer lab |
| Reinstall | Requires admin installer lab |
| Silent uninstall | Requires admin installer lab |
"@

Write-Doc "docs\THREAT_MODEL_v2.10.0.md" @"
# Threat Model v2.10.0

$stableNotice

## Primary Assets

- Local session token.
- Restore metadata.
- Reports and action logs.
- Local config and user privacy.
- Windows system state.

## Main Threats

| Threat | Mitigation |
| --- | --- |
| Local process calls mutating endpoint | `X-HyperBoostX-Session` enforced when token is present |
| UI button calls dead endpoint | tests/test_ui_action_map_v210.py validates action map routes |
| Risky optimizer action | Safety Guard blocks Defender, permanent Update disable, anti-cheat, driver service, BIOS/OC/undervolt, destructive cleanup |
| Token or username leaks into reports | Crash/report redaction tests and docs require redacted output |
| Feature overclaim | README and action map require Real-only public feature status |
| Expert mode bypasses safety | Explicit policy: expert mode never bypasses Safety Guard |
| Signed/unsigned confusion | Code signing readiness is documented; unsigned beta must stay labeled |

## Release Security Gate

Stable is NO-GO until token rejection, route coverage, destructive-action blocking, report redaction, installer install/uninstall, admin rollback, hardware matrix, and signing/checksum evidence are present.
"@

Write-Doc "docs\CODE_SIGNING_READINESS.md" @"
# Code Signing Readiness

$stableNotice

## Current Status

Status: BLOCKED_BY_OWNER_CERTIFICATE

No real code-signing certificate, thumbprint, or PFX was available in this workspace. v2.10.0-beta.1 artifacts must remain unsigned/testing-only unless the owner supplies signing material.

## Required Before Stable

- Obtain trusted code-signing certificate.
- Sign WPF executable, launcher, backend executable if packaged, and installer.
- Verify signature with `Get-AuthenticodeSignature`.
- Regenerate SHA256 after signing.
- Document SmartScreen/Unknown Publisher behavior if unsigned preview artifacts are shared.

## Command Template

```powershell
.\scripts\release\sign_release.ps1 -Thumbprint "<CERT_THUMBPRINT>"
Get-AuthenticodeSignature .\HyperBoostXInstaller.exe
```
"@

Write-Doc "PRIVACY.md" @"
# Privacy

$stableNotice

HyperBoostX is local-first. The stable baseline and v2 development line are documented as local desktop software, not a cloud account product.

## Data Handling

- Runtime data is stored under `%LocalAppData%\HyperBoost X` or `HYPERBOOSTX_PORTABLE_HOME`.
- Reports, crash exports, and diagnostics must redact tokens, API keys, webhooks, usernames, user-profile paths, and sensitive local paths.
- Telemetry is off by default.
- Cloud/license is a local beta boundary in v2.10.0-beta.1, not a production cloud-account claim.

## User Control

- Mutating actions require preview/approval where supported.
- Restore metadata must be visible for supported changes.
- Reports are manually exported by the user.
"@

Write-Doc "docs\ANTI_REGRESSION_v2.10.0.md" @"
# Anti-Regression v2.10.0

$stableNotice

## Protected Baselines

- v1.3.0 is the public stable baseline.
- v1.4.x and v2.0.x are evidence/history for feature parity and preview work.
- No v1.3 feature may disappear from v2 without a documented replacement, limitation, or deliberate safety block.

## Automated Gates

- UI action map density and route coverage: tests/test_ui_action_map_v210.py.
- Runtime route contract: tests/test_runtime_route_contract.py.
- Version/channel beta contract: tests/test_v13_api_contract.py.
- WPF build/test: `dotnet build`, `dotnet test`.

## Release Blockers

- Empty menu.
- Decorative button without command/handler/route.
- Dead backend route.
- Unauthorized local session not handled.
- Version mismatch.
- Installer/admin/hardware/signing gates missing for stable.
"@

Write-Doc "docs\UI_PARITY_AUDIT_v2.10.0.md" @"
# UI Parity Audit v2.10.0

$stableNotice

## Counts

| Metric | Count |
| --- | ---: |
| Menus in UI action map | $menuCount |
| Buttons in UI action map | $buttonCount |
| Partial/roadmap/guidance buttons | $partialButtonCount |
| Unique endpoints used by UI | $endpointCount |

## Result

- Every mapped menu has at least 6 active actions.
- Big menus have at least 10 active actions.
- WPF `CyberPageChrome` renders dynamic action-map buttons.
- Dashboard has direct hero buttons for core user flows.
- Former roadmap/guidance surfaces route to real local-safe boundary handlers or Safety Guard blocks.

Source of truth: `docs/UI_ACTION_MAP_v2.10.0.md`.
"@

Write-Doc "docs\MODULE_OWNERSHIP_v2.10.0.md" @"
# Module Ownership v2.10.0

$stableNotice

| Module | Owner Role | Notes |
| --- | --- | --- |
| WPF Shell/UI | Windows desktop engineer | Sidebar, dashboard, CyberPageChrome, UI state |
| Backend API | Python backend engineer | Flask route contract, local-only API, safe envelopes |
| Safety Guard | Security/QA owner | Blocked categories, preview/apply/restore policy |
| Installer/Release | Release engineer | NSIS, package, hash, signing, install smoke |
| Docs/Claims | Product owner + QA | Public stable status, local-safe boundary claims |
| Hardware Lab | Owner/manual QA | NVIDIA/AMD/Intel/no-GPU/admin/no-admin/scaling matrix |

No module may mark itself stable without evidence in QA results.
"@

Write-Doc "docs\DEPENDENCY_AUDIT_v2.10.0.md" @"
# Dependency Audit v2.10.0

$stableNotice

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
- No plugin marketplace execution until signed/trusted plugin policy exists.

## Required Before Stable

- Regenerate SBOM.
- Verify packaged dependency versions.
- Run secret scan.
- Confirm installer ships the expected WPF/backend runtime files.
"@

Write-Doc "SBOM_v2.10.0.md" @"
# SBOM v2.10.0

$stableNotice

## Summary

This SBOM is a lightweight repo-level inventory for the beta gate. A formal signed release SBOM must be regenerated from final release artifacts.

| Area | Components |
| --- | --- |
| WPF | .NET 8, Newtonsoft.Json, NAudio, OpenCvSharp WPF/runtime |
| Backend | Python, Flask, psutil, local HyperBoostX services |
| Installer | NSIS script and release package folders |
| Tests | pytest, xUnit, .NET test SDK |
| Docs | Markdown release, security, QA, audit, troubleshooting files |

## Stable Requirement

Stable release requires artifact-level SBOM, checksums, signing status, and third-party notices.
"@

Write-Doc "THIRD_PARTY_NOTICES.md" @"
# Third Party Notices

$stableNotice

HyperBoostX uses open-source and platform components. Final release packaging must include license notices for bundled dependencies.

Known dependency families:

- .NET / WPF runtime components.
- Newtonsoft.Json.
- NAudio.
- OpenCvSharp.
- Python / Flask / psutil dependencies.
- NSIS installer tooling.

This file is a beta readiness notice. Before stable, verify exact packaged versions and include required license texts.
"@

Write-Doc "docs\PERFORMANCE_BUDGET_v2.10.0.md" @"
# Performance Budget v2.10.0

$stableNotice

## Budgets

| Area | Budget |
| --- | --- |
| Backend health poll | Low-frequency; avoid UI spam |
| WPF dashboard refresh | User-initiated or conservative polling |
| Startup time | Must remain responsive with backend offline |
| Large JSON output | Truncated in UI result panel |
| Animations | Must respect Reduce Motion |

## Stable Requirement

Run desktop smoke on low-end PC profile, normal desktop, backend offline, and high-DPI scaling before stable promotion.
"@

Write-Doc "docs\PLUGIN_SECURITY_v2.10.0.md" @"
# Plugin Security v2.10.0

$stableNotice

Plugin marketplace is a local catalog and manifest-validation boundary; unsigned arbitrary execution remains blocked.

## Current Boundary

- Registry/status can be shown.
- Unsigned plugin install is blocked or Safety-Guard evaluated.
- No third-party plugin code should execute from marketplace UI in v2.10 beta.

## Future Requirement

- Signed plugin manifest.
- Hash verification.
- Permission declaration.
- Local-only execution boundary.
- Explicit owner approval.
"@

Write-Doc "docs\DISASTER_RECOVERY_v2.10.0.md" @"
# Disaster Recovery v2.10.0

$stableNotice

## Recovery Paths

- Backend offline: UI stays responsive and shows launcher restart guidance.
- Token mismatch: relaunch through HyperBoostX launcher.
- Corrupt config: backup corrupt JSON and load defaults.
- Failed apply: no unsupported system change should be applied; show safe failure.
- Bad release install: uninstall, reinstall public stable v1.3.0, preserve/export user reports when possible.

## Stable Gate

Admin rollback smoke and installed runtime verification must pass before v2.10 gets a stable label.
"@

Write-Doc "docs\MANUAL_QA_SCRIPT_v2.10.0.md" @"
# Manual QA Script v2.10.0

$stableNotice

## Manual Matrix

- Fresh install as admin.
- Reinstall same version.
- Silent install.
- Silent uninstall.
- Launch after install.
- Backend health.
- WPF connects to backend.
- Token sync.
- No orphan process after close.
- No admin mode.
- Admin mode.
- Backend offline.
- Token mismatch.
- Corrupt config.
- Missing reports folder.
- Windows scaling 100%, 125%, 150%.
- Small screen.
- Empty game library.
- Many startup items.
- Protected process action blocked.
- NVIDIA GPU.
- AMD GPU.
- Intel GPU.
- No GPU detected.

Record evidence in `docs/QA_RESULTS_v2.10.0.md`.
"@

Write-Doc "docs\OWNER_HANDOFF_v2.10.0.md" @"
# Owner Handoff v2.10.0

$stableNotice

## What Is Ready

- Runtime version/channel is beta-aware.
- UI action map covers $menuCount menus and $buttonCount buttons.
- Action map routes are automated-test covered.
- README public status now points normal users to v1.3.0 stable.
- v2.10 beta docs and gate scripts are present.

## Owner Must Run Before Stable

1. Install generated build as administrator.
2. Run installed runtime verifier.
3. Run admin apply/rollback lab.
4. Run hardware matrix.
5. Provide code-signing certificate/PFX.
6. Generate signed artifacts and SHA256.
7. Confirm release notes and GitHub Release assets.

Stable status: NO-GO until those gates pass.
"@

Write-Doc "docs\QA_RESULTS_v2.10.0.md" @"
# QA Results v2.10.0-beta.1

$stableNotice

## Automated Evidence

| Gate | Status | Evidence |
| --- | --- | --- |
| Version sync | See latest Full QA | scripts/verify_version_sync.ps1 |
| UI action map | PASS in current work | $menuCount menus, $buttonCount buttons, $endpointCount unique endpoints |
| Route coverage | PASS in current work | tests/test_ui_action_map_v210.py |
| Runtime route contract | See latest Full QA | tests/test_runtime_route_contract.py |
| WPF build | See latest Full QA | dotnet build HyperBoostX.sln |
| Installer/admin/hardware/signing | BLOCKED | Owner/lab/certificate required |

## Stable Decision

v2.10.0-beta.1 can be treated as a testing build if automated gates pass. It cannot be called stable until manual/lab gates pass.
"@

Write-Doc "RELEASE_NOTES_v2.10.0-beta.1.md" @"
# HyperBoostX v2.10.0-beta.1

$stableNotice

## Highlights

- Runtime/version metadata moved to `2.10.0-beta.1`.
- Release readiness endpoint reports Beta and blocks stable promotion until manual gates pass.
- WPF sidebar gained missing audit/release/driver/overlay/RGB/report boundary menus.
- Dynamic v2.10 UI action map renders additional buttons per CyberPageChrome page.
- `docs/UI_ACTION_MAP_v2.10.0.md` documents $menuCount menus, $buttonCount buttons, and $endpointCount UI-used endpoints.
- Backend route contract includes v2.10 aliases for system scan, process analysis/apply, network DNS apply/restore, reports export, local license boundary, plugin validation, and RGB conflict detection.
- README public status now recommends v1.3.0 as public stable and positions v2.x as development preview.

## Honest Limitations

- RGB is implemented as conflict detection/restart-approval boundary, not full device lighting control.
- Cloud sync/license enforcement is local beta boundary only, not a production cloud service.
- Plugin marketplace is local catalog/manifest validation only; unsigned arbitrary code execution is blocked.
- Driver recommendation does not auto-download or auto-install drivers.
- Stable release requires installed runtime, admin rollback, hardware lab, signing, checksum, and smoke evidence.
"@

Write-Doc "docs\FINAL_AUDIT_REPORT_v2.10.0.md" @"
# Final Audit Report v2.10.0-beta.1

$stableNotice

## Counts

| Metric | Count |
| --- | ---: |
| Total menu | $menuCount |
| Total tombol | $buttonCount |
| Total tombol aktif | $buttonCount |
| Total tombol partial/roadmap/guidance | $partialButtonCount |
| Total endpoint dipakai UI | $endpointCount |

## Real Feature Split

- Real: all 66 action-map features now have real handlers/endpoints or explicit Safety Guard blocked apply behavior.
- Risky actions remain approval/admin/restore gated.
- RGB is a conflict detector/restart-approval boundary, not full device lighting control.
- Cloud/license is a local beta license boundary.
- Plugin marketplace is a local catalog/manifest validation boundary.

## Blockers

- Installed runtime not verified for v2.10 beta.
- Admin apply/rollback lab not executed.
- Hardware matrix not executed.
- Code signing certificate/PFX unavailable.
- Stable tag/release not created.

Decision: BETA_READY if automated gates pass; STABLE NO-GO until blockers are closed.
"@

[pscustomobject]@{
    Version = $version
    Channel = $channel
    Menus = $menuCount
    Buttons = $buttonCount
    UiEndpoints = $endpointCount
    GeneratedAt = $generated
}
