# HyperBoostX v2.0 Development Preview

HyperBoostX v2.0 is part of the next major rebuild line. It focuses on the real WPF cyber shell, safe AI diagnosis, transparent plan-first optimization, restore visibility, local reports/history, and trust artifacts. The current recommended public stable baseline remains v1.3.0 unless the owner validates and promotes a later v2 release.

## Recommended Download

- `HyperBoostXInstaller.exe` from the official GitHub Release.
- Verify with `SHA256SUMS.txt` before installing.
- If the installer is unsigned, Windows may show Unknown Publisher or SmartScreen. This is expected until the owner signs the installer with a real code-signing certificate.

## Shipped In v2.0.0

- Real cyber WPF shell: `MainWindow` is shell-only and loads routed views from `wpf/Views/*`.
- Global cyber theme/style dictionaries: `Themes/CyberTheme.xaml`, `AccentColors.xaml`, `Animations.xaml`, and `Styles/*`.
- Cyber dashboard with HyperBoostX hero, score rings, scanner line, Safety Guard indicator, restore indicator, backend pulse, system cards, and action buttons.
- Settings for Enable Animations, Reduce Motion, Accent color, Beginner Mode, Advanced Mode, and Expert Preview with local `ui_settings.json` persistence.
- AI Performance Advisor, Knowledge Base, deterministic score engine, game library/profile metadata, overlay status, protected process evaluator, process analyzer, local benchmark history, GPU guidance, startup/cleanup/network facades, gaming essentials, streaming status, restore sessions, feature audit, action log, and local storage recovery.
- Session-token enforcement for mutating endpoints when `HYPERBOOSTX_SESSION_TOKEN` is enabled.
- Safety Guard blocks Defender disable, permanent Windows Update disable, anti-cheat changes, GPU/audio/network driver service changes, OC/undervolt/voltage/BIOS actions, arbitrary AI shell execution, and destructive cleanup.
- Installer silent reinstall fix: previous-install message box is skipped when running with `/S`.
- Static website starter under `website/` for download, feature, safety, screenshot, FAQ, and SHA256 guidance.

## Validation Summary

- Python tests: `52 passed`.
- .NET tests: `28 passed`.
- Debug and Release builds pass.
- Backend, WPF, launcher, package, installer, portable smoke, installed app smoke, silent reinstall, silent uninstall, fresh install, SHA256, and refined secret scan are release-gated locally.

## Honest Limitations

- No guaranteed FPS claim.
- No guaranteed ping claim.
- No official NVIDIA/AMD/Intel/Microsoft/MSI partnership claim.
- No overclocking, undervolting, voltage, BIOS, or anti-cheat feature.
- Driver Recommendation Center does not fabricate latest stable driver numbers and does not auto-download drivers.
- Similar-hardware benchmark comparison is roadmap until a verified dataset exists.
- RGB control, plugin SDK/marketplace, performance overlay, cloud sync, and license activation are roadmap-only.
- Streaming/Creator modes are safe foundations, not full replacements for professional streaming/creator suites.

## Blockers Outside The Repo

- Code signing requires owner certificate/PFX.
- Multi-machine lab validation requires additional hardware.

GitHub Release publication must be reported from the actual `git`/`gh` publishing result for the final v2.0.0 tag.
