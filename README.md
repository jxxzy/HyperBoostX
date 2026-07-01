<div align="center">

<img src="https://capsule-render.vercel.app/api?type=waving&height=220&color=0:020617,45:0f172a,100:16a34a&text=HyperBoostX&fontColor=ffffff&fontSize=56&fontAlignY=38&desc=Safe%20Local%20Windows%20PC%20Control%20Center&descAlignY=58&animation=fadeIn" alt="HyperBoostX banner" />

<img src="https://readme-typing-svg.demolab.com?font=JetBrains+Mono&weight=700&size=22&duration=2400&pause=900&color=22C55E&center=true&vCenter=true&width=900&lines=Preview-first+optimization;Safety+Guard+always+on;Gaming+readiness+and+local+diagnostics;No+fake+FPS+or+ping+guarantees" alt="HyperBoostX typing animation" />

<br />

![Stable](https://img.shields.io/badge/Stable-v2.10.0%20unsigned-16a34a?style=for-the-badge&logo=windows&logoColor=white)
![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-2563eb?style=for-the-badge&logo=windows&logoColor=white)
![Safety Guard](https://img.shields.io/badge/Safety%20Guard-Always%20On-22c55e?style=for-the-badge&logo=shield&logoColor=white)
![Local First](https://img.shields.io/badge/Local%20First-Privacy%20Aware-0ea5e9?style=for-the-badge&logo=lock&logoColor=white)
![Unsigned](https://img.shields.io/badge/Code%20Signing-Unsigned%20Owner%20Build-f59e0b?style=for-the-badge&logo=gnupg&logoColor=white)

<br />

<a href="https://github.com/jxxzy/HyperBoostX/releases/tag/v2.10.0">
  <img src="https://img.shields.io/badge/Download-v2.10.0%20Release-22c55e?style=for-the-badge&logo=github&logoColor=white" alt="Download HyperBoostX v2.10.0" />
</a>
<a href="#quick-start">
  <img src="https://img.shields.io/badge/Quick%20Start-Install%20Guide-2563eb?style=for-the-badge&logo=powershell&logoColor=white" alt="Quick start" />
</a>
<a href="#safety-guard">
  <img src="https://img.shields.io/badge/Read-Safety%20Guard-ef4444?style=for-the-badge&logo=securityscorecard&logoColor=white" alt="Safety Guard" />
</a>

</div>

---

## Release Status

Current release: **HyperBoostX v2.10.0 Stable Unsigned**.

HyperBoostX is a Windows desktop control center for safe local PC diagnosis, gaming readiness, hardware-aware recommendations, preview-first optimization, restore visibility, cleanup, reports, and safety-first system actions.

| Area | Status |
| --- | --- |
| Current version | `2.10.0` |
| Release channel | `Stable Unsigned` |
| GitHub Release | Published at `v2.10.0` |
| Installer | `HyperBoostXInstaller.exe` |
| Code signing | `SKIPPED_BY_OWNER_NO_CERT` |
| Backend | Local only, binds to `127.0.0.1` |
| Safety Guard | Always on |
| Telemetry | No silent cloud telemetry |
| Performance claim | No guaranteed FPS, ping, latency, or smoothness claim |

Windows may show **Unknown Publisher** or **SmartScreen** because the installer is unsigned. Verify SHA256 before installing.

## Download

Latest release:

```text
https://github.com/jxxzy/HyperBoostX/releases/tag/v2.10.0
```

Primary artifact:

```text
HyperBoostXInstaller.exe
```

Current installer SHA256:

```text
3956493b2f9586a13c436a52560dab2def9476d2e38a0f02891bee0a1b084d89  HyperBoostXInstaller.exe
```

Verify with PowerShell:

```powershell
Get-FileHash .\HyperBoostXInstaller.exe -Algorithm SHA256
```

## What HyperBoostX Is

HyperBoostX is not a one-click miracle booster. It is a local-first Windows optimization and diagnostics workspace built around:

- diagnose first
- preview before apply
- user approval before mutating changes
- Safety Guard checks
- restore and undo visibility where supported
- redacted local reports
- hardware-aware recommendations
- safer gaming readiness workflows

## Feature Surface

| Metric | Count |
| --- | ---: |
| Menus | 72 |
| Active buttons/actions | 596 |
| Partial/roadmap/guidance buttons in stable action map | 0 |
| Unique UI endpoints | 165 |
| Backend API route rules | 365 |
| Unique backend API paths | 361 |
| Guarded destructive buttons | 20 |
| Python tests | 72 passed |
| .NET tests | 38 passed |

Installed runtime verification must confirm the feature registry exposes:

```text
stable_visible_features = 72
stable_visible_buttons = 596
non_real_visible_in_stable = 0
```

Backend health/version alone is not enough for a stable gate.

## Feature Overview

| Area | Status | Boundary |
| --- | --- | --- |
| Dashboard | Real | Local system overview, health, scan, and report access |
| One Click Boost | Real-safe | Preview, apply, undo visibility, and Safety Guard |
| Smart Scan / Advisor | Real | Local scan and recommendation workflow |
| Game Library / Profiles | Real-safe | Local profiles and gaming readiness |
| GPU Center | Real guidance | Vendor detection and driver guidance, not automatic driver install |
| Process Analyzer | Real | Heavy process visibility and protected process blocking |
| Startup Manager | Real-safe | Preview, apply, restore metadata |
| Cleanup | Real-safe | Preview/report flow without destructive user-file deletion by default |
| Network Tools | Real-safe | Diagnostics, ping, DNS preview/apply/restore |
| Power Optimization | Real-safe | Power-plan guidance with admin boundaries |
| Visual Effects | Real-safe | Supported visual settings preview/apply/restore |
| Security Health | Real | Defender, firewall, and update status visibility |
| Repair Tools | Real-safe | SFC/DISM/CHKDSK paths with explicit approval/admin gate |
| Restore / Rollback | Real-safe | Session metadata, export, rollback visibility |
| Reports / Logs | Real | JSON/TXT/MD export with redaction expectations |
| Beginner / Advanced / Expert | Real | More detail without bypassing Safety Guard |
| RGB Software Detector | Boundary-safe | Software/conflict detection, not full lighting control |
| Plugins | Boundary-safe | Local catalog/manifest validation only |
| License / Cloud | Boundary-safe | Local boundary state, not production cloud sync or license server |

## Safety Guard

HyperBoostX blocks dangerous or irreversible actions, including:

- forced Windows Defender disable
- permanent Windows Update disable
- anti-cheat process or service tweaks
- GPU, audio, or network driver service disable
- BIOS/UEFI changes
- overclock, undervolt, or voltage changes
- arbitrary AI-generated shell execution
- destructive cleanup of user documents, downloads, desktop, pictures, videos, music, game saves, or system files
- protected process kill

Supported mutating flows follow:

```text
Preview -> Explicit Approval -> Safety Check -> Apply -> Report -> Restore/Undo Visibility
```

Expert Mode exposes more details, but it does not bypass Safety Guard.

## Honest Boundaries

Allowed claims:

- safe local optimization
- preview-first changes
- restoreable local actions where implemented
- hardware-aware recommendations
- local-first diagnosis
- gaming readiness checks
- redacted reports

Blocked claims:

- No guaranteed FPS increase
- No guaranteed ping or latency reduction
- No official NVIDIA, AMD, Intel, Microsoft, MSI, ASUS, Corsair, or OEM partnership claim
- No anti-drop 100% claim
- No auto-fix everything claim
- No automatic latest driver installation claim
- No full RGB device lighting control claim
- No production cloud sync claim
- No production license server claim

## Requirements

| Requirement | Detail |
| --- | --- |
| OS | Windows 10 / Windows 11 |
| Runtime | Packaged WPF app plus local backend |
| Backend host | `127.0.0.1` |
| Installer | Admin installer |
| Code signing | Unsigned unless owner supplies certificate/PFX |
| Network | Local backend only by default |

## Quick Start

1. Download `HyperBoostXInstaller.exe` from the v2.10.0 GitHub Release.
2. Verify SHA256 with `Get-FileHash`.
3. Run the installer as administrator.
4. Launch from Start Menu or the desktop shortcut.
5. Start with Dashboard -> Smart Scan -> Review Recommendations -> Preview -> Apply Approved Only.

## Build And Test

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build_stable_release.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\owner_admin_stable_gate.ps1 -ExpectedVersion 2.10.0 -BackendPort 5000
app\venv\Scripts\python.exe -m pytest -q tests
dotnet test dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj -c Release
```

Minimum expected local test result:

- Python: 72 passed
- .NET: 38 passed

Stable installed gates must verify:

- `/api/health`
- `/api/version`
- `/api/features/audit`
- `/api/features/stable-visible`
- `/api/features/non-real`
- package includes `wpf\Data\ui_action_map_v2_10.json`
- public QA/runtime evidence is redacted

## Privacy And Local Data

- Runtime data is stored under `%LocalAppData%\HyperBoost X`.
- Portable mode is supported through `HYPERBOOSTX_PORTABLE_HOME`.
- Reports and logs must redact API keys, AI keys, GitHub tokens, Discord tokens/webhooks, bearer tokens, local session tokens, Windows usernames, user-profile paths, sensitive local paths, and future license keys.
- Telemetry is off by default.
- No silent cloud telemetry is enabled.

## Local Backend

Default base URL:

```text
http://127.0.0.1:5000
```

Core endpoints:

- `GET /api/health`
- `GET /api/version`
- `GET /api/release/readiness`
- `GET /api/features/audit`
- `GET /api/features/stable-visible`
- `GET /api/features/non-real`

Mutating endpoints require `X-HyperBoostX-Session` when `HYPERBOOSTX_SESSION_TOKEN` is present.

## Documentation

| Document | Path |
| --- | --- |
| Release Notes | `docs/release-notes/RELEASE_NOTES_v2.10.0.md` |
| Final Audit Report | `docs/FINAL_AUDIT_REPORT_v2.10.0.md` |
| Release Gate Result | `docs/RELEASE_GATE_RESULT.md` |
| QA Results | `docs/QA_RESULTS.md` |
| Feature Matrix | `docs/FEATURE_MATRIX.md` |
| Feature Truth Matrix | `docs/FEATURE_TRUTH_MATRIX_v2.10.0.md` |
| Security Policy | `SECURITY.md` |
| User Guide | `docs/USER_GUIDE.md` |
| Troubleshooting | `docs/TROUBLESHOOTING.md` |
| API Reference | `docs/API_REFERENCE.md` |
| Hardware Matrix | `docs/HARDWARE_MATRIX_v2.10.0.md` |
| Code Signing Readiness | `docs/CODE_SIGNING_READINESS.md` |

## Troubleshooting

Backend not responding:

```text
Launch through HyperBoostX.exe so the launcher starts backend and WPF together.
```

Unauthorized local session:

```text
Close HyperBoostX -> stop orphan backend -> relaunch from installed launcher.
```

SmartScreen warning:

```text
This release is unsigned. Verify SHA256 before continuing.
```

Feature count mismatch:

```text
stable_visible_features must be 72
stable_visible_buttons must be 596
non_real_visible_in_stable must be 0
```

If the count is 0, the installed runtime is not reading `ui_action_map_v2_10.json` correctly.

## Developer Release Checklist

- VERSION is `2.10.0`.
- README matches current release status.
- No stale beta/NO-GO wording exists in current docs.
- Python tests pass.
- .NET tests pass.
- Backend route contract passes.
- WPF UI/UX quality gate passes.
- Real usability gate passes.
- Package contains `ui_action_map_v2_10.json`.
- Installed runtime reads 72 menus and 596 actions.
- Public runtime evidence is redacted.
- Secret scan passes.
- PowerShell syntax scan passes.
- Installer payload verification passes.
- Silent uninstall/reinstall passes.
- Release notes and checksums are updated.

## Contributing Rules

- Keep Safety Guard active.
- Do not add forced Defender disable.
- Do not add permanent Windows Update disable.
- Do not add anti-cheat tweaks.
- Do not disable GPU/audio/network driver services.
- Do not add BIOS/UEFI/OC/undervolt/voltage changes.
- Do not add arbitrary shell execution.
- Do not add destructive cleanup of user files.
- Do not claim guaranteed FPS gains.
- Do not add vendor partnership wording unless real and documented.

Run checks before PR:

```powershell
app\venv\Scripts\python.exe -m pytest -q tests
dotnet test dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify_public_evidence_redaction.ps1
```

## Disclaimer

HyperBoostX provides safe local optimization guidance and supported reversible actions where implemented. Hardware, drivers, Windows configuration, games, permissions, background software, thermals, and admin rights differ per PC. No performance improvement is guaranteed.

HyperBoostX is not official NVIDIA, AMD, Intel, Microsoft, MSI, ASUS, Corsair, or OEM software.

<div align="center">

<img src="https://capsule-render.vercel.app/api?type=waving&height=120&section=footer&color=0:16a34a,45:0f172a,100:020617" alt="HyperBoostX footer" />

**Safe first. Fast when possible. Honest always.**

</div>
