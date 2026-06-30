# HyperBoostX Legacy Feature Matrix

Audit date: `2026-06-26`
Current target: `v2.0.1 Flow Restoration & Runtime Fix Patch`

Allowed status values used here: `Complete`, `Partial`, `Missing`, `Broken`, `Roadmap Only`, `Needs Hardware Lab`, `Needs Owner Credential`, `Needs Admin Permission`, `Not Found In History`.

| Feature | First Seen Version | Old Behavior | Current UI Exists | Current Backend Exists | Test Exists | Status | Regression Found | Required Fix |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| One Click Boost | v1.0.x | Apply gaming/latency/streaming style boost profile | Yes, Dashboard and One Click Boost | Yes, `/api/boost/plan`, `/api/boost/apply`, `/api/boost/undo` | Yes | Complete | Dashboard used removed `/api/triple-ai/full-flow` route | Fixed in v2.0.1 by routing to `/api/boost/plan` |
| Smart Scan | v1.3.0/v2.0.0 | Scan stats, GPU, overlays, startup, profile | Yes, Dashboard button | Yes, `/api/scan/smart` | Yes | Complete | `/api/scan/smart` alias missing | Fixed in v2.0.1 |
| CPU/RAM cleanup | v1.0.x | Safe memory/process guidance | Yes, HyperBalance/Process Analyzer | Yes, process and advisor services | Yes | Partial | No destructive RAM cleaner by design | Keep as safe recommendations; no force-kill system processes |
| Temp cleanup | v1.0.x | Clean temp/cache categories | Yes, Cleanup | Yes, `/api/cleanup/*` | Yes | Partial | Apply remains conservative | Mature safe deletion lab coverage before claiming complete cleanup |
| Startup optimization | v1.0.x | List, preview, apply, restore startup items | Yes, Startup Manager | Yes, `/api/startup/list`, `/api/startup/items`, preview/apply/restore | Yes | Partial | Conservative facade only records safe metadata | Add per-item enable/disable validation and rollback lab tests |
| Network optimization | v1.0.x | DNS/ping/TCP network tools | Yes, Network Tools | Yes, `/api/network/*` | Yes | Complete | `/api/network/dns` alias missing | Fixed in v2.0.1 |
| Flush DNS | v1.0.x | Flush DNS with admin handling | Yes, Network Tools | Yes, `/api/network/flush-dns` | Yes | Needs Admin Permission | Admin-required path needs real elevated smoke | Keep structured `admin_required` response |
| TCP optimize | v1.0.x | Safe TCP normal autotuning | Yes, Network Tools | Yes, `/api/network/optimize-tcp` | Yes | Needs Admin Permission | Requires elevated Windows validation | Do not add aggressive registry tweaks |
| Ultimate Performance plan | v1.x | Try power plan, fallback safely | Yes, Settings/Boost guidance | Partial via boost profile guidance | .NET NVIDIA safety tests cover admin metadata | Partial | Full apply/restore not revalidated in v2 shell | Add power-plan apply/restore route and admin fallback test before complete claim |
| Restore point / backup | v1.0.x | Restore metadata and rollback visibility | Yes, Restore & Backup | Yes, `/api/restore/*` | Yes | Partial | Metadata facade, not full Windows restore point validation | Add multi-scenario rollback lab validation |
| Undo / rollback | v1.0.x | Undo supported changes | Yes, Dashboard and Restore & Backup | Yes, `/api/boost/undo`, restore aliases | Yes | Partial | System-level rollback limited to supported actions | Keep visible and avoid overclaiming universal rollback |
| Before/after report | v1.3.0/v2.0.0 | Capture local before/after counters | Yes, Performance Report | Yes, report service/export | Yes | Complete | Export aliases needed | Fixed route contract in v2.0.1 |
| Beginner mode | v1.x | Safe default mode | Yes, Settings/topbar | Yes, UI config | Yes | Complete | None confirmed | Keep default |
| Advanced mode | v1.x | More controls visible | Yes, Settings | Yes, UI config | Yes | Complete | None confirmed | Keep clear warnings |
| Expert preview | v1.4.0 | Preview advanced/unsafe boundaries | Yes, Settings | Yes, UI config | Yes | Complete | None confirmed | Keep preview-only language |
| Advanced Mic / Voice Meter | v1.2.8-v1.2.10 | Mic diagnostics, meter, gain/gate/compressor preview | Yes, Streaming Center restored in v2.0.1 | Yes, `/api/streaming/status` legacy toolkit | Yes | Partial | v2 generic page hid old controls | Fixed UI surface; real DSP remains guidance |
| Mic diagnostics | v1.2.8-v1.2.10 | Diagnostics and privacy checks | Yes | Yes, toolkit guidance | Yes | Partial | v2 generic page hid button | Fixed visible button; hardware test needed for real devices |
| Audio service reset | v1.2.8-v1.3.0 | Restart audio service tooling | No active destructive button | No active mutating route | No | Needs Admin Permission | Not restored as active due risk | Keep as guide/admin-lab backlog |
| Sound settings shortcut | v1.2.8-v1.2.10 | Open Windows sound settings | Yes | N/A local UI action | Static .NET test | Complete | Hidden in v2 generic page | Fixed in v2.0.1 |
| Volume Mixer shortcut | v1.2.10 | Open Windows Volume Mixer | Yes | N/A local UI action | Static .NET test | Complete | Hidden in v2 generic page | Fixed in v2.0.1 |
| Microphone privacy shortcut | v1.2.8-v1.2.10 | Open privacy settings | Yes | N/A local UI action | Static .NET test | Complete | Hidden in v2 generic page | Fixed in v2.0.1 |
| Voicemeeter detection | v1.2.10 | Detect standard install paths | Yes | UI local detection | Static .NET test | Complete | Hidden in v2 generic page | Fixed in v2.0.1 |
| Voicemeeter launch | v1.2.10 | Launch if installed | Yes | UI local action | Static .NET test | Needs Hardware Lab | Needs installed app check with Voicemeeter | Manual lab test |
| Voicemeeter official download | v1.2.10 | Open official page | Yes | N/A local UI action | Static .NET test | Complete | Hidden in v2 generic page | Fixed in v2.0.1 |
| Webcam diagnostics | v1.2.8-v1.2.10 | Camera scan/diagnostics | Yes, Streaming Center restored | Yes, toolkit guidance | Yes | Partial | v2 generic page hid old controls | Fixed UI surface; real camera lab needed |
| Camera scan | v1.2.8-v1.2.10 | Scan camera devices | Yes | UI guidance | Static .NET test | Needs Hardware Lab | No real camera smoke in current pass | Add device lab |
| Windows Camera app launch | v1.2.8-v1.2.10 | Open Camera app | Yes | N/A local UI action | Static .NET test | Complete | Hidden in v2 generic page | Fixed in v2.0.1 |
| Camera settings shortcut | v1.2.8-v1.2.10 | Open camera settings | Yes | N/A local UI action | Static .NET test | Complete | Hidden in v2 generic page | Fixed in v2.0.1 |
| Camera privacy shortcut | v1.2.8-v1.2.10 | Open camera privacy | Yes | N/A local UI action | Static .NET test | Complete | Hidden in v2 generic page | Fixed in v2.0.1 |
| Device Manager shortcut | v1.2.8-v1.2.10 | Open Device Manager | Yes | N/A local UI action | Static .NET test | Complete | Hidden in v2 generic page | Fixed in v2.0.1 |
| Webcam brightness/contrast/sharpness/exposure/FPS profile | v1.2.10 | Profile sliders and guidance | Yes, sliders restored | Yes, toolkit guidance | Static .NET test | Partial | v2 generic page hid sliders | Fixed profile UI; no forced driver writes |
| Streaming / low-light / sharp-face presets | v1.2.10 | Preset profile output | Yes | UI local profile output | Static .NET test | Partial | v2 generic page hid presets | Fixed visible presets; guide-only hardware-safe behavior |
| OBS profile output | v1.2.10 | OBS recommendation output | Yes | `/api/streaming/status` toolkit | Yes | Complete | Hidden in v2 generic page | Fixed in v2.0.1 |
| TikTok LIVE Studio profile output | v1.2.10 | TikTok recommendation output | Yes | `/api/streaming/status` toolkit | Yes | Complete | Hidden in v2 generic page | Fixed in v2.0.1 |
| Discord camera profile output | v1.2.10 | Discord camera recommendation | Yes | `/api/streaming/status` toolkit | Yes | Complete | Hidden in v2 generic page | Fixed in v2.0.1 |
| Error/audit Discord webhook | v1.1.0 | Send crash/audit alerts | Service exists | WPF service exists | Yes | Needs Owner Credential | Real delivery requires webhook | Manual credential test |
| Release-update Discord webhook | v1.2.12 | Separate update webhook | Service/secret storage exists | WPF service exists | Yes | Needs Owner Credential | Real delivery requires webhook | Manual credential test |
| Webhook Credential Manager storage | v1.1.0-v1.2.12 | Store secrets outside app-state | Yes, Settings service foundation | SecureSecretStoreService | Yes | Complete | None confirmed | Keep redaction tests |
| Raw webhook not in UI/log/state | v1.2.11-v1.2.12 | Redact and avoid plaintext app-state | Yes | Secure storage/redaction | Yes | Complete | None confirmed | Keep tests |
| NVIDIA provider config | v1.2.13-v1.2.14 | NVIDIA NIM provider config | Settings/services exist | Config/service exists | Yes | Complete | None confirmed | Keep docs current |
| NVIDIA API key via Credential Manager | v1.2.13-v1.2.14 | Store key securely | Yes | SecureSecretStoreService | Yes | Complete | None confirmed | Real connection needs key |
| 10 NVIDIA models selectable | v1.2.13-v1.2.14 | Model registry | Service exists | Service exists | Yes | Complete | None confirmed | UI selection lab still recommended |
| NVIDIA connection test | v1.2.13-v1.2.14 | Test provider/key/model | Service exists | Service exists | Yes | Needs Owner Credential | No key in this pass | Owner credential test |
| AI approval before optimization | v1.2.13-v1.2.14 | Plan-only until approved | Yes | Safety guard/service | Yes | Complete | None confirmed | Keep session/token gates |
| GPU Center NVIDIA/AMD/Intel/MicrosoftBasic/Unknown | v1.3.0 | Universal GPU detection/profile | Yes | `/api/hardware/*`, `/api/gpu/*` | Yes | Complete | None confirmed | Hardware lab for real sensors |
| GPU temperature/usage/VRAM | v1.3.0 | Sensor fields when available | Yes | Hardware services | Yes | Needs Hardware Lab | Sensors unavailable on some PCs | Lab matrix required |
| Protected process evaluator | v1.3.0 | Block anti-cheat/driver/security actions | Yes | `/api/protection/*` | Yes | Complete | None confirmed | Keep negative tests |
| Localhost-only backend | v1.3.0 | Bind to 127.0.0.1 | N/A | Backend server | Yes | Complete | None confirmed | Keep verifier |
| Session token middleware | v1.3.0 | Reject unauthorized mutating requests when enabled | N/A | API middleware | Yes | Complete | None confirmed | Keep verifier |
| Job queue/progress/cancel | v1.3.0 | Start/progress/cancel jobs | UI partial | `/api/jobs/*` | Yes | Partial | v2 pages do not deeply expose job queue | Add richer Job Progress UI |
| Cyber WPF shell | v1.4.0 branch | Shell-only MainWindow with routed Views | Yes | N/A | Yes | Complete | v1.4 has branch/release note but no tag | Decide archival tag/release later |
| Settings reduce motion/accent persistence | v1.4.0 | Persist UI settings | Yes | Local config | Yes | Complete | No schema metadata before v2.0.1 | Fixed schema/migration metadata |
| Feature Audit | v1.4.0 | Read-only audit surface | Yes | `/api/feature-audit/run`, `/status` | Yes | Complete | `/status` alias missing | Fixed in v2.0.1 |
| RGB control | v1.4.0 foundation | Detect only; control roadmap | UI roadmap | Detection only | Yes | Roadmap Only | None; must not overclaim | Keep roadmap language |
| Plugin SDK/marketplace | v1.4.0 foundation | Registry foundation only | UI roadmap | Registry foundation | Yes | Roadmap Only | None; must not overclaim | Keep roadmap language |
| External monitor overlay | v2 roadmap | External monitor requested | Not active | Not active | No | Roadmap Only | Not implemented | Do not claim complete |
| License enforcement | Roadmap | Future paid license | Not active | Not active | No | Roadmap Only | Not implemented | Do not lock core features |
