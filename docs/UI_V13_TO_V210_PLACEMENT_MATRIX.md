# HyperBoostX v1.3 to v2.10 UI Placement Matrix

Status: source placement implemented, runtime screenshot smoke pending.

| v1.3 Panel / Flow | v2.10.0 Page | Beginner | Advanced | Expert | Placement Notes |
| --- | --- | --- | --- | --- | --- |
| Dashboard overview | Dashboard | Yes | Yes | Yes | Hero limited to Scan, Boost, Restore; secondary tools below. |
| One-click boost | One Click Boost | Yes | Yes | Yes | Preview/apply/undo/report actions use the v2.10 action map. |
| Smart scan | Dashboard / Smart Scan | Dashboard entry | Yes | Yes | Smart Scan route remains available; Beginner starts from Dashboard. |
| AI recommendation | Smart Recommendation | Yes | Yes | Yes | Raw provider/backend details stay in Advanced Details. |
| Gaming mode | Gaming Mode | Yes | Yes | Yes | Auto mode stays restore-aware. |
| Startup manager | Startup | Yes | Yes | Yes | Startup list and selected apply flow remain grouped. |
| Background apps/process review | Background Apps / Process Analyzer | Background Apps | Yes | Yes | Protected process guard remains visible. |
| Cleanup | Cleanup | Yes | Yes | Yes | Personal folders excluded; no delete-first UI. |
| Storage | Storage | Yes | Yes | Yes | Storage status comes before cleanup guidance. |
| GPU/vendor guidance | GPU Center | Yes | Yes | Yes | No OC/undervolt/driver-service automation. |
| Driver recommendation | Driver & Update Center | Yes | Yes | Yes | Manual/OEM guidance only. |
| Gaming booster | Gaming Booster | Yes | Yes | Yes | Game detection and undo/report remain visible. |
| Game library | Game Library | No | Yes | Yes | Empty library state is explicit. |
| Game profiles | Game Profiles | No | Yes | Yes | Apply requires selected game and confirmation. |
| Streaming | Streaming Mode | Yes | Yes | Yes | Audio/camera handoff stays safe. |
| Creator mode | Creator Mode | Yes | Yes | Yes | Creator readiness and background pressure separated. |
| Network booster | Network Booster | Yes | Yes | Yes | Diagnostics before reset/flush. |
| DNS/latency | DNS & Latency Tools | Yes | Yes | Yes | No fake ping guarantee. |
| Privacy | Privacy Center | Yes | Yes | Yes | Browser sessions and personal data require explicit warning. |
| Security health | Security & Health | Yes | Yes | Yes | Defender/Firewall/anti-cheat disable is blocked. |
| Apps manager | Apps Manager | Yes | Yes | Yes | Inventory before uninstall guidance. |
| App uninstall | App Uninstaller | Yes | Yes | Yes | Preview/confirm only. |
| Tweaks center | Tweaks Center | Yes | Yes | Yes | Allowlisted, preview-first. |
| Windows features | Windows Features | Yes | Yes | Yes | Admin/restart state visible. |
| Update control | Update Control | Yes | Yes | Yes | Permanent disable is blocked. |
| Repair tools | Repair Tools | Yes | Yes | Yes | Admin/time warnings visible. |
| Restore and backup | Restore & Backup | Yes | Yes | Yes | Restore preview/apply/verify/export grouped. |
| Advanced tweaks/services/power/visual effects | Advanced System pages | No | Yes | Yes | Expert detail does not bypass safety. |
| Reports/history | Reports / Performance History / Performance Report | No | Yes | Yes | Report/export actions remain available. |
| Automation/rules | Scheduled Automation / Task & Rule System | No | Yes | Yes | Safe-only and dry-run by default. |
| Plugin marketplace | Plugin Marketplace | No | No | Yes | Boundary/roadmap page; not claimed as full marketplace. |
| Cloud/license | Cloud & License Boundary | No | No | Yes | Boundary page; local-first app remains usable. |
| RGB detector | RGB Software Detector | No | No | Yes | Detection/guidance only, not global RGB control. |

## Beginner Baseline

Beginner mode intentionally keeps the sidebar short: Dashboard, One Click Boost, Gaming Mode, Smart Recommendation, Performance, Startup, Background Apps, Cleanup, Storage, GPU Center, Gaming Booster, Streaming Mode, Creator Mode, Network Booster, DNS & Latency Tools, Privacy Center, Security & Health, Apps Manager, Tweaks Center, Windows Features, Update Control, Repair Tools, Driver & Update Center, App Uninstaller, Restore & Backup, Settings, and About.

## Expert Boundary

Expert Preview exposes all stable-real pages for audit and diagnostics. It does not enable unsafe actions, route around token checks, disable Safety Guard, or convert roadmap/boundary pages into full features.
