# HyperBoostX

[![Stable Release](https://img.shields.io/badge/Stable-v1.4.0-16a34a?style=for-the-badge&logo=github&logoColor=white)](https://github.com/jxxzy/HyperBoostX/releases/tag/v1.4.0)
[![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-2563eb?style=for-the-badge&logo=windows&logoColor=white)](#requirements)
[![Safety Guard](https://img.shields.io/badge/Safety%20Guard-Always%20On-22c55e?style=for-the-badge)](#safety-guard)

HyperBoostX is a safe AI Windows Gaming Optimizer. It scans local PC pressure, detects bottlenecks, builds safe gaming plans, checks overlay conflicts, protects important processes, compares before/after reports, and keeps restore metadata visible.

HyperBoostX does not guarantee FPS increase on every PC. It is not an overclocking, undervolting, BIOS, anti-cheat bypass, or vendor-driver replacement tool.

## Download

| Item | Link |
| --- | --- |
| Installer | [HyperBoostXInstaller.exe](https://github.com/jxxzy/HyperBoostX/releases/download/v1.4.0/HyperBoostXInstaller.exe) |
| Checksums | [SHA256SUMS.txt](https://github.com/jxxzy/HyperBoostX/releases/download/v1.4.0/SHA256SUMS.txt) |
| Release notes | [RELEASE_NOTES_v1.4.0.md](RELEASE_NOTES_v1.4.0.md) |

If the installer is unsigned, Windows may show Unknown Publisher or SmartScreen. Use the official GitHub Release and verify SHA256 before installing.

## Requirements

- Windows 10 or Windows 11.
- .NET 8 Windows Desktop Runtime for development builds.
- Local Python Flask backend is packaged for release builds.
- Backend binds to `127.0.0.1` only.

## v1.4.0 Highlights

- Real cyber WPF shell is active in the running app: `wpf/MainWindow.xaml` is now a shell, global theme/style dictionaries are merged in `wpf/App.xaml`, and pages load from `wpf/Views/*`.
- Cyber dashboard includes HyperBoostX hero, PC Health, Gaming, Streaming, Startup, Network, Safety, CPU/RAM/GPU/VRAM/Storage/Network/Power/Restore/backend cards, scanner line, hover cards, score rings, and action buttons.
- WPF Settings now includes Enable Animations, Reduce Motion, Accent color, Beginner Mode, Advanced Mode, and Expert Preview with local persistence.
- Local AI Performance Advisor for GPU, CPU, VRAM, RAM, storage, startup, and stutter-style bottleneck diagnosis.
- HyperBoostX Knowledge Base for DLSS, FSR, XeSS, Resizable BAR, Game Mode, HAGS, VRR, V-Sync, G-Sync, FreeSync, Frame Generation, Reflex, AFMF, and HYPR-RX.
- HyperBoost Score Engine with deterministic Gaming, AI, Health, Streaming, Storage, Network, and Security scores.
- Performance history and before/after timeline endpoints.
- Game database, custom game profile storage, safe profile preview/apply/restore metadata.
- Overlay Conflict Detector and Protected Process List APIs.
- Process Analyzer, Startup Manager facade, Cleanup preview/report facade, Network diagnostics, Gaming Essentials helper, Streaming Center, RGB detection, and plugin registry foundation.
- Vendor-aware GPU Center guidance for NVIDIA, AMD Radeon, Intel Arc/iGPU, Microsoft Basic Display Adapter, and unknown fallback.
- Driver Recommendation Center that never fabricates latest-driver numbers and never auto-downloads drivers.
- Local JSON storage under `%LocalAppData%\HyperBoost X` with corrupt-file backup and portable-mode support through `HYPERBOOSTX_PORTABLE_HOME`.
- Session token enforcement for mutating endpoints when `HYPERBOOSTX_SESSION_TOKEN` is enabled.
- Local action log with redaction for tokens, usernames, and sensitive paths.
- Tests expanded to cover v1.4 API contracts, advisor diagnosis, knowledge base, protection blocking, history, settings, corrupted JSON recovery, and roadmap-only feature boundaries.

## Cyber UI Screenshots

Release screenshots should be captured from the installed app after smoke validation:

- `docs/screenshots/wpf-cyber-dashboard.png` - dashboard hero, score rings, system cards, backend pulse.
- `docs/screenshots/wpf-cyber-settings.png` - motion/accent/mode settings.
- `docs/screenshots/wpf-cyber-feature-audit.png` - read-only audit and Safety Guard indicators.

## Safety Guard

HyperBoostX blocks or refuses to automate:

- Forced Windows Defender disable.
- Permanent Windows Update disable.
- Anti-cheat process or service changes.
- GPU, audio, or network driver service disabling.
- BIOS, UEFI, overclock, undervolt, or voltage actions.
- Arbitrary AI-generated shell execution.
- Destructive cleanup of user documents, downloads, desktop, pictures, videos, music, game saves, or system files.

Mutating flow stays plan-first: scan, explain, Safety Guard, user approval, restore metadata, apply supported safe action, report, and undo/restore visibility.

## Important APIs

All mutating endpoints require `X-HyperBoostX-Session` when `HYPERBOOSTX_SESSION_TOKEN` is present.

Core endpoints include:

- `GET /api/health`, `GET /api/version`
- `POST /api/boost/plan`, `POST /api/boost/apply`, `POST /api/boost/undo`
- `GET /api/advisor/performance`, `POST /api/advisor/performance`
- `GET /api/knowledge/terms`, `GET /api/knowledge/terms/{term}`
- `GET /api/score/engine`
- `GET /api/games/library`, `POST /api/games/profile/preview`, `POST /api/games/profile/apply`
- `GET /api/overlays/status`, `GET /api/overlays/recommendations`
- `GET /api/protection/processes`, `POST /api/protection/evaluate-action`
- `GET /api/benchmark/history`, `POST /api/benchmark/manual`, `POST /api/benchmark/import-csv`
- `GET /api/gpu/vendor-guide`, `GET /api/gpu/recommendations`, `GET /api/drivers/recommendation`
- `GET /api/product/storage`, `GET /api/product/v2-roadmap`

See [docs/API_REFERENCE.md](docs/API_REFERENCE.md) for the full contract.

## Beginner Path

1. Start the app through the HyperBoostX launcher.
2. Check backend status at the bottom/sidebar and top status area.
3. Run a smart scan or open AI Performance Advisor.
4. Review detected bottlenecks and Safety Guard notes.
5. Use safe boost/profile preview before applying anything.
6. Export a before/after report and keep restore metadata.

## GPU Support

HyperBoostX detects and guides:

- NVIDIA GeForce RTX/GTX and NVIDIA App/overlay paths.
- AMD Radeon RX/Vega/integrated graphics and AMD Software guidance.
- Intel Arc, Iris Xe, UHD, and iGPU safe fallback.
- Microsoft Basic Display Adapter and unknown GPU safe mode.

HyperBoostX is not official NVIDIA, AMD, Intel, MSI, Microsoft, or any vendor software.

## Roadmap-Only In v1.4.0

These are intentionally not claimed as complete release features:

- Global similar-hardware benchmark comparison, until a verified dataset exists.
- Third-party plugin SDK and marketplace.
- RGB device control. v1.4 detects RGB software only.
- Performance monitor overlay.
- HyperBoostX Cloud sync.
- Paid license lock or activation server.
- Automatic driver download/install.

## Verify SHA256

From PowerShell in the download folder:

```powershell
Get-FileHash .\HyperBoostXInstaller.exe -Algorithm SHA256
Get-Content .\SHA256SUMS.txt
```

The hash must match the release checksum.

## Build And Test

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify_repo.ps1
app\venv\Scripts\python.exe -m pytest -q tests
dotnet build HyperBoostX.sln -v minimal
dotnet test dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj -c Debug
```

## License Roadmap

No v1.4.0 feature is locked behind a license. Any professional license, activation, or cloud feature remains roadmap-only.
