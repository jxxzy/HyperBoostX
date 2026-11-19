# Legacy To v2 Mapping

Audit date: 2026-06-27

## Mapping Evidence

- Source file: `wpf/ViewModels/LegacyFeatureCatalog.cs`
- Legacy tool mappings: 250
- Catalog page keys: 55
- WPF routes registered: 55
- Sidebar items: 52

## Mapping Summary

| Legacy category | v2 page/group | Status |
| --- | --- | --- |
| Safe/Balanced/Extreme/Custom Boost | One Click Boost, Performance Boost | RESTORED |
| Smart Scan / Recommendations / AI | Dashboard, Smart Recommendation, AI Center | WIRED |
| Startup optimizer | Startup Manager | WIRED |
| Background apps / process pressure | Background Apps, Process Analyzer, HyperBalance | READ_ONLY / PREVIEW_ONLY |
| Cleanup/storage | Cleanup, Storage | PREVIEW_ONLY / APPLY_READY for safe metadata |
| GPU/NVIDIA/AMD/Intel | GPU Center, NVIDIA Copilot | READ_ONLY / CREDENTIAL_REQUIRED |
| Gaming profiles/library | Gaming Booster, Game Library, Game Profiles, Auto Gaming | PREVIEW_ONLY / APPLY_READY when approved |
| Streaming/creator/mic/webcam | Streaming Center, Creator Mode, Advanced Mic Mixer, Webcam Studio | READ_ONLY / MANUAL where hardware needed |
| Network/DNS/latency | Network Booster, DNS & Latency, Network Tools | WIRED |
| Privacy/security/apps/tweaks | Privacy Center, Security Health, Apps Manager, Tweaks Center | READ_ONLY / PREVIEW_ONLY |
| Repair/restore/automation | Repair Tools, Restore Backup, Restore Point Manager, Scheduled Automation | PREVIEW_ONLY / NEEDS_ADMIN |

## Rule

Legacy cards are mapping/status only. The actual user-facing controls are exposed in sidebar pages, functional bodies, route-backed action sections, and the shared Safety Workflow Bar.

