using System;
using System.Collections.Generic;

namespace HyperBoostX.ViewModels
{
    public static class LegacyFeatureCatalog
    {
        private sealed record Tool(string Category, string Title, string Flow, string Safety, string Route);

        private static readonly IReadOnlyDictionary<string, IReadOnlyList<Tool>> Tools = new Dictionary<string, IReadOnlyList<Tool>>(StringComparer.OrdinalIgnoreCase)
        {
            ["About HyperBoostX"] = new[]
            {
                T("Product", "Feature overview from v1.3", "Dashboard, Boost, Startup, Cleanup, Network, Gaming, Streaming, Creator, Privacy, Security, Repair, Drivers, Tweaks, Services, Power, Visual, Backup, Automation, AI, Discord, update, testing, and localization remain visible.", "Info", "/api/version /api/update/check"),
                T("Architecture", "Desktop + backend + launcher", "WPF client, local Flask backend, and .NET launcher lifecycle are preserved in the modern shell.", "Local", "/api/health"),
                T("Release", "Release channel notes", "Latest release status, installer asset, manual install fallback, beta/stable block, and readiness messaging are kept.", "Review", "/api/update/check"),
                T("Support", "Donation and author block", "MR.4NONY / HYPERINDO CYBER TEAM identity and support link remain in About/Release notes.", "Info", "Release notes / About"),
            },
            ["About"] = new[]
            {
                T("Product", "Feature overview from v1.3", "Full feature list, purpose, architecture, developer, version, update status, donation, and tagline are restored as modern About content.", "Info", "/api/version /api/update/check"),
                T("Safety", "Release safety notes", "Public wording avoids fake FPS, fake vendor partnership, and unsafe tweak claims.", "Guard", "/api/feature-audit/status"),
            },
            ["AIPerformanceAdvisor"] = new[]
            {
                T("AI", "Ask AI / Fix my PC", "AI produces plan-only suggestions until user approval; shell execution is blocked.", "Approval", "/api/advisor/plan"),
                T("AI", "Prepare gaming / clean safely / fix network", "Legacy quick prompts map to smart scan, boost preview, cleanup preview, and network diagnostics.", "Preview", "/api/scan/smart"),
                T("AI", "Approve, skip, reject actions", "Action approval queue is preserved with Safety Guard and restore metadata.", "Guard", "/api/advisor/safe-actions"),
                T("NVIDIA", "Context refresh and provider fallback", "NVIDIA provider status, model registry, and fallback remain visible without exposing secrets.", "Secret-safe", "/api/nvidia/test-connection"),
            },
            ["AICenter"] = new[]
            {
                T("AI", "HyperBoostX Copilot hub", "Central place for smart scan, advisor plan, safe actions, approvals, and action history.", "Approval", "/api/advisor/safe-actions"),
                T("AI", "Why / why not reasoning", "AI explains blocked actions and maps risky requests to safe alternatives.", "Guard", "/api/protection/evaluate-action"),
                T("Automation", "Create AI automation", "AI-created automation remains dry-run first and cannot bypass allowlists.", "Dry-run", "/api/automation/preview"),
            },
            ["NvidiaCopilot"] = new[]
            {
                T("NVIDIA", "Save API key / test connection", "Provider credential storage remains secure and never displays the saved key.", "Secret-safe", "/api/nvidia/test-connection"),
                T("NVIDIA", "Model registry and fallback", "Legacy NVIDIA model choices and fallback status are visible for troubleshooting.", "Review", "Settings / NVIDIA provider"),
                T("Safety", "Approval-gated AI actions", "NVIDIA output cannot apply tweaks directly; it must pass Safety Guard.", "Guard", "/api/advisor/safe-actions"),
            },
            ["OneClickBoost"] = new[]
            {
                T("Boost", "Run Safe / Balanced / Extreme / Custom Boost", "v1.3 boost modes become plan-first previews; Extreme remains expert/blocked unless allowed.", "Preview", "/api/boost/plan"),
                T("v1.3 Controls", "1. Run Safe Boost / 2. Run Balanced Boost / 3. Run Extreme Boost", "The numbered v1.3 boost entry points are restored as clear presets, with Custom Boost kept as an explicit owner-reviewed flow.", "Preview", "/api/boost/plan"),
                T("Custom Boost", "Clear standby, Optimize RAM, Best Performance, process priority", "Legacy custom checkboxes are represented as safe action candidates before anything is applied.", "Approval", "/api/advisor/safe-actions"),
                T("Custom Boost", "Delete temp, Clear cache ringan, Empty recycle bin, Flush DNS", "Cleanup/network options from v1.3 are routed through preview/export flows and stay reversible where possible.", "Preview", "/api/cleanup/preview /api/network/diagnostics"),
                T("Safety", "Skip antivirus & system critical / Auto create restore point", "The v1.3 safety checkboxes are always visible and mapped to Safety Guard plus restore metadata.", "Guard", "/api/protection/processes /api/restore/sessions"),
                T("Boost", "Boost before gaming", "Gaming preparation scans active game, background pressure, overlays, startup, and restore readiness.", "Review", "/api/scan/smart"),
                T("Memory", "Clear standby / optimize RAM", "Safe memory guidance stays reviewable and reportable; no hidden killer action.", "Approval", "/api/advisor/safe-actions"),
                T("Recovery", "Create restore point / undo optimization", "Restore metadata and undo route stay visible before any apply.", "Restore", "/api/boost/undo"),
            },
            ["PerformanceBoost"] = new[]
            {
                T("Performance", "Activate game boost / work mode / daily mode", "Profiles are preserved as safe plan presets with local diagnostics first.", "Preview", "/api/boost/plan"),
                T("CPU", "High Performance, Ultimate Performance, Prioritize Foreground App", "v1.3 CPU actions become readable recommendations and guarded power/profile previews.", "Guard", "/api/power/preview /api/processes/background-pressure"),
                T("CPU", "Set Process Priority, CPU Core Optimization, Disable Power Throttling", "Process-level and core-level tuning remains expert-reviewed; no hidden global mutation is run.", "Expert", "/api/protection/evaluate-action"),
                T("RAM/Disk", "Clear Standby Memory, Free Unused RAM, Memory Leak Check, Auto RAM Cleanup", "Memory tools are surfaced as diagnostics and advisor-safe actions, while disk cleanup stays preview-first.", "Preview", "/api/processes/background-pressure /api/cleanup/scan"),
                T("Profiles", "Daily Mode / Work Mode / Gaming Mode / Streaming Mode / Extreme Mode / Custom Mode", "The v1.3 profile strip is restored as named presets with preview, report, undo, and reset-to-default flows.", "Preview", "/api/boost/plan"),
                T("CPU/RAM", "CPU core, priority, RAM cleanup", "Legacy performance controls are mapped to review-only recommendations or guarded actions.", "Guard", "/api/processes/background-pressure"),
                T("Startup", "Startup impact analysis", "Startup pressure remains integrated with performance recommendations.", "Read-only", "/api/startup/items"),
                T("Report", "Before/after export", "Performance report export replaces unverified FPS claims.", "Report", "/api/reports/export"),
            },
            ["AutoGamingMode"] = new[]
            {
                T("Gaming", "Auto detect game", "Detect running games and keep browsers/work apps out of game mode by default.", "Read-only", "/api/games/running"),
                T("Profiles", "Apply Balanced Mode / Apply Competitive Mode / Apply Streaming Mode", "The v1.3 mode buttons are restored as explicit profile previews with undo after exit.", "Preview", "/api/auto-gaming/preview"),
                T("Session", "Auto Activation & Restore After Exit", "Game session state, active profile, and undo are visible instead of hidden background automation.", "Restore", "/api/games/session/history /api/boost/undo"),
                T("Manual", "Disable Updater Apps, Disable Cloud Sync, Auto cleanup saat RAM tinggi", "Manual gaming checklist items stay reviewable and protected-app aware.", "Approval", "/api/protection/evaluate-action"),
                T("Profiles", "Competitive / balanced / streaming mode", "Legacy gaming profiles are previewed and restored after close.", "Preview", "/api/auto-gaming/preview"),
                T("Game tuning", "Priority, affinity, overlay review", "Game priority and overlay guidance require explicit review and restore metadata.", "Approval", "/api/games/profile/preview"),
                T("Recovery", "Undo last gaming boost", "Restore/undo is part of the game session flow.", "Restore", "/api/boost/undo"),
            },
            ["GamingBooster"] = new[]
            {
                T("Gaming", "Boost specific game", "Game selection, process detection, and profile preview replace blind global boosting.", "Preview", "/api/games/profile/preview"),
                T("Quick Boost", "Boost Now - Safe / Competitive / Streaming", "The three v1.3 quick gaming boost buttons are restored as safe, competitive, and streaming presets.", "Approval", "/api/boost/plan"),
                T("Manual Booster", "Process Optimizer, Network Optimizer, Visual Optimizer, Manual Booster Setup", "Manual controls remain visible as a checklist routed to guarded process/network/visual routes.", "Guard", "/api/games/profile/preview"),
                T("Game Session", "Apply Game Priority, Apply CPU Affinity, Boost Specific Game Only", "Priority and affinity controls are owner-reviewed and never applied to the wrong process silently.", "Expert", "/api/games/profile/preview"),
                T("Whitelist", "Add Whitelist / Remove Whitelist / Reset Whitelist", "v1.3 whitelist intent is restored as protected-app review so launchers, anti-cheat, OBS, Discord, and drivers stay safe.", "Guard", "/api/protection/processes"),
                T("Modes", "Safe / competitive / streaming", "v1.3 modes stay available as named plan presets.", "Approval", "/api/boost/plan"),
                T("Overlay", "Clean stream overlay / overlay detector", "Overlay checks are diagnostic and do not kill protected apps automatically.", "Read-only", "/api/overlays/status"),
            },
            ["GameLibrary"] = new[]
            {
                T("Library", "Scan Steam/Epic/Xbox/Battle.net/EA/Ubisoft/Riot", "Game discovery is restored as local scan with manual add/remove fallback.", "Read-only", "/api/games/scan"),
                T("Library", "Manual game add/remove", "Manual entries preserve v1.3 usability when launchers are not detected.", "Review", "/api/games/add"),
            },
            ["GameProfiles"] = new[]
            {
                T("Profiles", "Apply selected game profile", "Profiles preview first and require approval before apply.", "Approval", "/api/games/profile/preview"),
                T("Profiles", "History and restore", "Game session history/export remains available for rollback and comparison.", "Restore", "/api/games/session/history"),
            },
            ["HyperBalance"] = new[]
            {
                T("Balance", "Game + stream balance", "Foreground game, OBS/Discord, and protected apps are balanced without force-closing protected processes.", "Read-only", "/api/processes/background-pressure"),
                T("Protection", "Protected app whitelist", "Discord, Steam, OBS, anti-cheat, drivers, security, and vendor tools stay protected unless reviewed.", "Guard", "/api/protection/processes"),
                T("Report", "Balance recommendations", "Recommendations are exported rather than silently applied.", "Report", "/api/processes/export-report"),
            },
            ["ProcessAnalyzer"] = new[]
            {
                T("Processes", "Scan background processes", "Read-only heavy process pressure view is preserved and normalized.", "Read-only", "/api/processes/heavy"),
                T("Processes", "Auto kill saat boost", "Force-close behavior is replaced by evaluate-action and explicit approval.", "Guard", "/api/protection/evaluate-action"),
                T("Startup", "Startup impact", "Startup impact and process pressure are shown together.", "Read-only", "/api/processes/startup-impact"),
            },
            ["BackgroundApps"] = new[]
            {
                T("Background", "Background app pressure", "v1.3 background trim content is restored as read-only list plus safe recommendations.", "Read-only", "/api/processes/background-pressure"),
                T("Guard", "Block/close review", "Closing apps requires Safety Guard evaluation; browsers are work apps, not games.", "Approval", "/api/protection/evaluate-action"),
            },
            ["StartupManager"] = new[]
            {
                T("Startup", "View all startup apps", "Startup inventory and impact are restored.", "Read-only", "/api/startup/items"),
                T("v1.3 Buttons", "1. View All Startup Apps / 2. Enable / Disable Startup / 3. Delay Startup Apps", "Numbered startup actions from v1.3 are restored with preview, target selection, and restore-first messaging.", "Preview", "/api/startup/items /api/startup/preview"),
                T("v1.3 Buttons", "4. Startup Impact Analyzer / 5. Safe Disable Recommendation", "Impact scoring and safe-disable guidance are visible before any disable action.", "Read-only", "/api/startup/items"),
                T("Profiles", "Gaming Startup / Work Startup / Minimal Startup", "The v1.3 startup profiles remain as named presets, but mutating changes require approval and rollback metadata.", "Approval", "/api/startup/preview"),
                T("Tools", "Open Startup Folder / Open Common Startup / Startup Services (Advanced)", "Folder shortcuts and advanced service review are exposed as manual/guarded flows.", "Manual", "Windows Startup folders / /api/windows/services"),
                T("Startup", "Enable / disable / delay startup", "Changes are previewed and require approval plus restore session.", "Approval", "/api/startup/preview"),
                T("Startup", "Safe disable recommendation", "Recommendations avoid security, driver, anti-cheat, and essential vendor entries.", "Guard", "/api/startup/preview"),
                T("Recovery", "Restore default startup", "Startup restore route remains visible.", "Restore", "/api/startup/restore"),
            },
            ["Cleanup"] = new[]
            {
                T("Cleanup", "Temp, cache, logs, recycle bin", "Safe cleanup allowlist is restored without deleting personal folders by default.", "Preview", "/api/cleanup/scan"),
                T("Quick Scan", "Scan Junk Files, Scan Temp Files, Scan Cache System, Scan Recycle Bin", "v1.3 scan buttons are restored as safe read/preview actions with a clear result report.", "Read-only", "/api/cleanup/scan"),
                T("Clean Now", "Clean Junk Files, Clear Temp Files, Empty Recycle Bin, Clear System Cache", "Cleanup applies only after preview/confirmation and excludes personal folders by default.", "Approval", "/api/cleanup/preview"),
                T("Browser", "Clear Cache, Clear Cookies, Clear History, Clear Download History, Clear Saved Sessions", "Browser cleanup keeps session/cookie warnings visible before action.", "Warning", "/api/privacy/preview"),
                T("Analyzer", "Scan >100MB / >500MB / >1GB, Scan Duplicates, Keep Original", "Large-file and duplicate scans are restored as review-only workflows.", "Review", "/api/cleanup/preview"),
                T("Automation", "Safe Only / Moderate / Advanced / Schedule Daily / Weekly / Monthly", "v1.3 cleanup modes and schedule buttons remain visible; automation defaults to report-only.", "Dry-run", "/api/automation/preview"),
                T("Storage", "Duplicate and large-file scan", "Duplicate/large-file cleanup remains review-only.", "Review", "/api/cleanup/preview"),
                T("Privacy", "Clipboard, recent files, browser cache", "Privacy cleanup warns about sessions/cookies before any apply.", "Approval", "/api/privacy/preview"),
                T("Report", "Cleanup report/export", "Cleanup output is reportable and reversible where applicable.", "Report", "/api/cleanup/export-report"),
            },
            ["Storage"] = new[]
            {
                T("Storage", "Analyze drive / scan all drives", "System drive status and storage class are restored as read-only diagnostics.", "Read-only", "/api/storage/status"),
                T("Drive Tools", "All Devices / System Files / Windows Temp / Prefetch Files", "v1.3 drive filters are restored as explicit scope labels to avoid accidental personal data cleanup.", "Review", "/api/storage/drives"),
                T("Drive Cleanup", "Windows Update Cache / Delivery Optimization Files / Error Reports & Logs", "Advanced cleanup categories are visible but require preview before removal.", "Preview", "/api/cleanup/preview"),
                T("Drive Actions", "Storage Health Check / Cleanup Drive / Open Location", "Storage actions are tied to status, scan result, and safe cleanup report routes.", "Report", "/api/storage/status /api/cleanup/export-report"),
                T("Storage", "Internal/removable scan", "Drive scope stays explicit to avoid accidental personal data cleanup.", "Review", "/api/storage/drives"),
                T("Cleanup", "Cleanup drive", "Cleanup uses safe temp/cache preview before apply.", "Preview", "/api/cleanup/preview"),
            },
            ["GpuCenter"] = new[]
            {
                T("GPU", "Refresh GPU center", "Detected GPU, VRAM, driver, vendor, overlays, and profile guidance remain live.", "Read-only", "/api/gpu/status"),
                T("GPU", "Apply GPU optimization", "GPU guidance excludes overclock, undervolt, voltage, BIOS, and driver-service hacks.", "Guard", "/api/gpu/recommendations"),
                T("Driver", "Driver recommendation", "Driver status uses official/OEM/manual guidance only.", "Manual", "/api/drivers/recommendation"),
                T("Report", "Export GPU report", "GPU report export is preserved.", "Report", "/api/gpu/export-report"),
            },
            ["DriverUpdateCenter"] = new[]
            {
                T("Driver", "Deep scan / outdated driver review", "Driver list and recommendation are visible without fabricated latest versions.", "Read-only", "/api/drivers/list"),
                T("Scanner", "Quick Scan / Full Scan / Deep Scan", "v1.3 driver scanner levels are restored as status/recommendation views without fake latest-version claims.", "Read-only", "/api/drivers/list"),
                T("Driver Manager", "Rollback Driver / Reinstall Driver / Backup Drivers / Restore Drivers", "Driver rollback, reinstall, backup, and restore flows stay manual/OEM-safe and never auto-run hidden installers.", "Manual", "Device Manager / vendor source"),
                T("Updates", "SMART DRIVER UPDATE / QUICK FIX DRIVER / Update All Drivers", "Legacy update buttons route to recommendation, official-source checks, and Safety Guard.", "Guard", "/api/drivers/recommendation"),
                T("Control", "Block Driver Update / Safe Only / Beta Driver / Advanced Driver", "Driver blocker and safety modes stay visible as policy guidance, not silent system hacks.", "Guard", "/api/protection/evaluate-action"),
                T("Driver", "Backup / restore drivers", "Driver backup/restore guidance points to Windows/vendor-safe paths.", "Manual", "Device Manager / vendor source"),
                T("Driver", "Block driver update / auto rules", "Driver update blocker is informational/preview-first; no hidden system policy hacks.", "Guard", "/api/protection/evaluate-action"),
            },
            ["NetworkTools"] = new[]
            {
                T("Network", "DNS speed, ping, diagnostics", "DNS, latency, local diagnostics, and hostname are live.", "Read-only", "/api/network/diagnostics"),
                T("Network", "Flush DNS / apply fastest DNS", "DNS changes require explicit action; destructive reset is separate and guarded.", "Approval", "/api/network/flush-dns"),
                T("Network", "Continuous ping / geo ping / adapter priority", "Legacy network tools are represented as diagnostics and reports.", "Report", "/api/network/export-report"),
            },
            ["NetworkBooster"] = new[]
            {
                T("Network", "Gaming/streaming latency mode", "Legacy network booster routes to diagnostics, DNS test, and safe flush only.", "Preview", "/api/network/diagnostics"),
                T("Network Modes", "Activate Gaming Network Mode / Activate Streaming Network Mode", "v1.3 network modes are restored as diagnostics-first presets without ping guarantees.", "Preview", "/api/network/diagnostics"),
                T("Adapter", "Toggle Network Adapter / Restart Network Adapter / Set Adapter Priority", "Adapter operations are manual/reviewed and never run as hidden destructive reset.", "Manual", "/api/protection/evaluate-action"),
                T("Bandwidth", "Bandwidth Control / Limit Bandwidth App / Block Background Data Tracking", "Bandwidth and blocking actions are routed through Safety Guard and protected-app review.", "Guard", "/api/protection/evaluate-action"),
                T("Recovery", "Backup Network Config / Restore Network Config / Safe Network Mode", "Network recovery and backup labels from v1.3 stay visible before risky changes.", "Restore", "/api/network/export-report"),
                T("Guard", "Network reset review", "Destructive reset is never automatic and is evaluated by Safety Guard.", "Guard", "/api/protection/evaluate-action"),
            },
            ["DnsLatencyTools"] = new[]
            {
                T("DNS", "Test DNS speed", "DNS test route remains visible and measurable.", "Read-only", "/api/network/dns"),
                T("DNS", "Cloudflare DNS / Custom DNS / Apply DNS / Auto Select Fastest DNS", "v1.3 DNS choices are surfaced as measured profiles and explicit apply previews.", "Approval", "/api/network/dns"),
                T("Latency", "Quick Latency Test / Continuous Latency Test / Geo Ping Test", "Latency tools are restored as measurable diagnostics with exported reports.", "Report", "/api/network/ping"),
                T("IP Tools", "Run Traceroute / Renew IP / Release IP / View DNS Cache", "Network troubleshooting actions stay separate from boost actions and are user-triggered only.", "Manual", "/api/network/diagnostics"),
                T("Recovery", "Clear DNS Cache / Backup DNS Setting / Restore Default DNS", "DNS cache and restore controls remain visible with no hidden DNS mutation.", "Restore", "/api/network/export-report"),
                T("Latency", "Ping/continuous/geo tests", "Latency tools are reportable and avoid fake ping claims.", "Report", "/api/network/ping"),
            },
            ["NetworkOptimization"] = new[]
            {
                T("Network", "TCP/profile optimization", "Network profile changes are preview-first and exclude dangerous registry hacks.", "Preview", "/api/network/diagnostics"),
                T("Recovery", "Backup/restore network config", "Network config backup remains visible before risky changes.", "Restore", "/api/network/export-report"),
            },
            ["StreamingCenter"] = new[]
            {
                T("Streaming", "OBS/TikTok/Discord profile", "Streaming profile output, app protection, and overlay guidance are restored.", "Read-only", "/api/streaming/status"),
                T("Start", "Start Streaming Optimization / Refresh Detect / Restore After Streaming", "v1.3 streaming session buttons are restored with explicit restore after stream.", "Restore", "/api/streaming/status"),
                T("App Priority", "Apply App Priority / Prioritize Encoder Process", "Encoder and app-priority controls are review-first and never touch unrelated apps silently.", "Approval", "/api/streaming/recommendations"),
                T("Streaming Focus", "Optimize CPU Encoder / Reserve RAM for Stream / Optimize GPU Encoder", "v1.3 CPU/RAM/GPU streaming tools are mapped to diagnostics and profile output.", "Preview", "/api/creator/recommendations"),
                T("Network/Overlay", "Optimize Upload / Ping Stabilization / Clean Stream Overlay", "Streaming network and overlay controls are reportable diagnostics, not fake ping/FPS claims.", "Report", "/api/network/diagnostics"),
                T("Balance", "Stream + Gaming Balance Mode / Apply Balance Mode", "Game plus stream balancing is preserved through protected process review and background pressure analysis.", "Guard", "/api/processes/background-pressure"),
                T("Mic", "Voice Meter / Voicemeeter / volume mixer", "Mic meter and Voicemeeter guidance stay manual and never rewire drivers silently.", "Manual", "/api/streaming/recommendations"),
                T("Webcam", "Camera presets and diagnostics", "Brightness, contrast, sharpness, exposure, FPS, privacy, and device manager links remain.", "Privacy", "/api/camera-tracking/status"),
                T("Network", "Streaming network mode", "Streaming network diagnostics and reports are preserved.", "Report", "/api/network/diagnostics"),
            },
            ["AdvancedMicMixer"] = new[]
            {
                T("Mic", "Start/stop mic meter", "Mic level guidance remains local and visible.", "Manual", "/api/streaming/status"),
                T("Mic", "Gain, gate, compressor, monitor mix, limiter", "Audio tuning is guidance/profile output, not driver mutation.", "No driver edit", "/api/streaming/export-profile"),
                T("Voicemeeter", "Detect/open/download Voicemeeter", "VB-Audio tools are opened manually only.", "Manual", "Windows/VB-Audio"),
            },
            ["WebcamStudio"] = new[]
            {
                T("Camera", "Stream / low-light / sharp-detail presets", "Camera profiles are preview guidance with privacy controls visible.", "Privacy", "/api/camera-tracking/status"),
                T("Camera", "Scan cameras / camera diagnostics", "Black-preview and permission guidance are preserved.", "Manual", "Windows Camera settings"),
            },
            ["CameraTracking"] = new[]
            {
                T("Tracking", "Enable/disable tracking profile", "Camera tracking remains opt-in and never starts silently.", "Opt-in", "/api/camera-tracking/status"),
                T("Tracking", "Smoothness, dead zone, framing", "Tracking settings are shown as local profile guidance.", "Privacy", "/api/camera-tracking/preview"),
            },
            ["CreatorMode"] = new[]
            {
                T("Creator", "Start creator optimization", "Editing/rendering profile scans RAM, disk, GPU, and background pressure first.", "Preview", "/api/creator/status"),
                T("Profiles", "Creator Profile / CapCut Mode / Creator Setup", "v1.3 creator presets are restored as explicit editing/rendering profiles.", "Preview", "/api/creator/status"),
                T("Resources", "Allocate More RAM / Lock Creator Resources / Manage Media Cache", "Resource allocation and cache controls are guidance/preview first so production apps are not closed blindly.", "Approval", "/api/creator/recommendations"),
                T("Optimization", "Optimize Creator CPU / RAM / GPU / Disk / Network", "Creator-specific tuning remains visible and reportable without unsafe driver/service hacks.", "Report", "/api/creator/recommendations"),
                T("Focus", "Enable Creator Focus Mode / Optimize Creator Visuals", "Focus and visual tweaks are opt-in and reversible.", "Restore", "/api/visual-effects/preview"),
                T("Creator", "Allocate RAM / temp to fastest drive / media cache", "Creator recommendations avoid closing production apps without review.", "Approval", "/api/creator/recommendations"),
                T("Report", "Creator report export", "Creator readiness and before/after results are reportable.", "Report", "/api/reports/export"),
            },
            ["GamingEssentials"] = new[]
            {
                T("Essentials", "DirectX/VC++/OBS/launchers checklist", "Official-source install previews are preserved; no silent installers.", "Manual", "/api/essentials/check"),
                T("Essentials", "Install preview", "Runtime installation remains preview-only unless owner confirms outside hidden automation.", "Approval", "/api/essentials/install-preview"),
            },
            ["PrivacyCenter"] = new[]
            {
                T("Privacy", "Clipboard, recent files, cookies, saved sessions", "Legacy privacy cleanup is preserved with explicit session/cookie warning.", "Warning", "/api/privacy/preview"),
                T("Permissions", "Camera/mic/location app permissions", "Windows privacy pages and permission guidance stay manual.", "Manual", "Windows Settings"),
                T("Network", "Block app internet / tracking", "Network/privacy blocks require review and do not run silently.", "Guard", "/api/protection/evaluate-action"),
            },
            ["SecurityHealth"] = new[]
            {
                T("Security", "Defender, Firewall, Security Center", "Security health is read-only guidance; disabling security is blocked.", "Blocked", "/api/security/status"),
                T("Security", "Patch status and suspicious app review", "Security recommendations preserve admin-aware/manual handling.", "Manual", "/api/update-control/status"),
            },
            ["ProtectedApps"] = new[]
            {
                T("Protection", "Protected app/process list", "Anti-cheat, driver, audio, network, security, and gaming companion protections stay visible.", "Guard", "/api/protection/processes"),
                T("Protection", "Evaluate risky action", "Any close/disable/remove request is evaluated before action.", "Approval", "/api/protection/evaluate-action"),
            },
            ["AppsManager"] = new[]
            {
                T("Apps", "Analyze app usage / impact", "Installed/running app overview and impact guidance are restored.", "Read-only", "/api/apps/list"),
                T("Controls", "Block Launch / Background / Network", "App control buttons from v1.3 are restored as Safety Guard evaluations.", "Guard", "/api/protection/evaluate-action"),
                T("Startup Shortcut", "Open Startup Manager / Delay Startup", "App startup shortcuts route to the modern Startup page and preview flow.", "Preview", "/api/startup/items"),
                T("Cleanup", "Clear Cache Aplikasi / Remove Leftover Files", "App cleanup remains preview-first and avoids personal data folders.", "Preview", "/api/cleanup/preview"),
                T("Apps", "AI optimize apps", "AI app recommendations remain plan-only.", "Approval", "/api/advisor/plan"),
                T("Control", "Block launch/background/network", "App control actions route through Safety Guard.", "Guard", "/api/protection/evaluate-action"),
            },
            ["AppUninstaller"] = new[]
            {
                T("Uninstall", "Standard/batch/silent review", "Uninstall is confirmation-first and never hidden.", "Confirm", "/api/apps/uninstall-preview"),
                T("Uninstall", "Batch Uninstall Review / Fix Broken Uninstall / Repair Broken Install", "v1.3 uninstall repair tools are preserved as manual/preview workflows.", "Confirm", "/api/apps/uninstall-preview"),
                T("Cleanup", "Deep Residual Clean / Kill Process + Force Cleanup", "Aggressive removal labels remain visible but are guarded and never run silently.", "Guard", "/api/protection/evaluate-action"),
                T("Backup", "Backup Sebelum Uninstall / Restore Perubahan", "App backup and restore intent is visible before uninstall or residual cleanup.", "Restore", "/api/restore/sessions"),
                T("Cleanup", "Residual clean", "Residual cleanup stays safe preview and excludes personal data.", "Preview", "/api/cleanup/preview"),
                T("Backup", "Backup app state before uninstall", "App-state backup guidance remains visible.", "Restore", "/api/restore/sessions"),
            },
            ["TweaksCenter"] = new[]
            {
                T("Tweaks", "Performance/gaming/network/privacy/system/UI/power/startup tweaks", "Legacy tweak categories are restored as allowlisted preview flows.", "Allowlist", "/api/system-config/tweaks"),
                T("Actions", "APPLY SMART TWEAKS / Apply All Safe Tweaks / Customize per Tweak", "v1.3 tweak buttons are restored as allowlisted preview actions.", "Allowlist", "/api/system-config/tweaks/preview"),
                T("Categories", "Apply Performance Tweaks / Gaming Tweaks / Network Tweaks / Privacy Tweaks", "Category-specific tweak groups remain visible and mapped to safe previews.", "Preview", "/api/system-config/tweaks"),
                T("Categories", "Apply System Tweaks / UI Tweaks / Power Tweaks / Startup Tweaks", "System-level tweak groups require restore metadata and Safety Guard.", "Restore", "/api/restore/sessions"),
                T("Tweaks", "Safe only / advanced mode", "Risk mode labels are visible and dangerous tweaks stay blocked outside Expert review.", "Guard", "/api/system-config/tweaks/preview"),
                T("Recovery", "Backup registry/settings and undo tweak", "Restore metadata is required before mutating tweak apply.", "Restore", "/api/restore/sessions"),
            },
            ["AdvancedTweaks"] = new[]
            {
                T("Advanced", "Registry preset, boot config, system flags", "Advanced actions are Expert preview-only and blocked by default for beginners.", "Expert", "/api/system-config/tweaks/preview"),
                T("Advanced", "APPLY ADVANCED TWEAKS / Apply Registry Preset / Apply Fast Boot Tweak", "The v1.3 advanced action strip is restored with Expert warnings.", "Expert", "/api/system-config/tweaks/preview"),
                T("Low-level", "Apply Driver-Level Flags / Apply Kernel Behavior Tweak / Apply Low-Level Performance", "Low-level labels remain visible but are blocked/guarded unless there is a safe supported route.", "Blocked", "/api/protection/evaluate-action"),
                T("Hardware", "Advanced Hardware Tweaks / Adaptive Recovery Logic", "Hardware-related tuning is informational and guarded; no voltage, BIOS, or driver-service mutation is run.", "Guard", "/api/protection/evaluate-action"),
                T("Advanced", "Kernel, driver-level, low-level performance", "Driver/kernel/service mutations are guarded and never automatic.", "Guard", "/api/protection/evaluate-action"),
                T("Explorer", "Context menu, taskbar, dark mode", "UI tweaks are reversible guidance, not blind script execution.", "Restore", "/api/visual-effects/preview"),
            },
            ["WindowsFeatures"] = new[]
            {
                T("Features", "Developer/gaming/creator/minimal presets", "Optional Windows features are reviewed before OS-level changes.", "Admin", "/api/windows/features"),
                T("Actions", "APPLY FEATURE OPTIMIZATION / Enable Feature / Disable Feature", "Feature manager actions from v1.3 are restored as preview/admin-aware flows.", "Admin", "/api/windows/features/preview"),
                T("Presets", "Developer Features / Gaming Features / Creator Features / Minimal Preset", "Optional-feature presets are visible and require explicit owner/admin action.", "Manual", "/api/windows/features"),
                T("Recovery", "Backup Feature Config / Restore Feature Config", "Feature backup/restore intent is preserved before OS-level changes.", "Restore", "/api/restore/sessions"),
                T("Features", "Enable/disable optional features", "Feature changes require explicit owner/admin flow and can require restart.", "Manual", "/api/windows/features/preview"),
                T("Recovery", "Backup/restore feature config", "Feature config backup remains visible.", "Restore", "/api/restore/sessions"),
            },
            ["WindowsServices"] = new[]
            {
                T("Services", "Inspect/start/stop/reset services", "Services are visible but protected driver/security/anti-cheat services cannot be disabled silently.", "Guard", "/api/windows/services"),
                T("Service Controls", "All services / Bulk Disable Safe Services / Bulk Stop Safe Services", "Bulk actions are restored as preview-only and cannot touch protected services automatically.", "Guard", "/api/windows/services/preview"),
                T("Profiles", "Gaming services / Streaming services / Creator services", "Service profiles are named and reviewable with rollback guidance.", "Preview", "/api/windows/services/preview"),
                T("Recovery", "Backup Service Config / Restore Service Config", "Service backup and restore intent is visible before changes.", "Restore", "/api/restore/sessions"),
                T("Profiles", "Gaming/streaming/creator service profiles", "Service profiles are preview-first and require owner approval.", "Preview", "/api/windows/services/preview"),
                T("Backup", "Backup service config", "Service rollback guidance is preserved.", "Restore", "/api/restore/sessions"),
            },
            ["UpdateControl"] = new[]
            {
                T("Updates", "Pause/schedule/active hours", "Update control is temporary/reversible; permanent disable is blocked.", "Guard", "/api/update-control/status"),
                T("Update Buttons", "Check Update / Check Latest Release / Auto Check Updates", "v1.3 update checks stay visible and owner-controlled.", "Review", "/api/update/check"),
                T("Windows Update", "Pause Updates / Background Update Control / Update Rollback", "Update operations are temporary, reversible, and never permanent-disable hacks.", "Guard", "/api/update-control/preview"),
                T("Maintenance", "Windows Update Cache / Clear Download History / Backup Update Settings", "Update cleanup and backup actions are preview-first.", "Preview", "/api/cleanup/preview"),
                T("Updates", "Driver update manager / Store app updates", "Update/driver links stay manual and official-source oriented.", "Manual", "/api/update-control/preview"),
                T("Cleanup", "Windows Update cache", "Update cache cleanup uses preview and restore-aware messaging.", "Preview", "/api/cleanup/preview"),
            },
            ["RepairTools"] = new[]
            {
                T("Repair", "SFC/DISM quick/full repair", "Repair commands are admin/time-aware previews; no arbitrary AI shell.", "Admin", "/api/repair/status"),
                T("Repair Buttons", "Quick Repair / Full System Repair / Auto Fix All", "v1.3 repair presets are restored with admin/time warnings and no silent shell execution.", "Admin", "/api/repair/preview"),
                T("Repair Categories", "Network Repair / Windows Services Repair / App Store Repair / Disk Repair", "Legacy repair categories are exposed as preview/report actions.", "Preview", "/api/repair/preview"),
                T("Repair Categories", "Audio Repair / Display Repair / Windows Update Repair / Registry Repair", "Advanced repair categories stay guarded and reportable.", "Guard", "/api/repair/preview"),
                T("Advanced", "Advanced Fixes / Clear Corrupt Cache / Cleanup Registry Ringan", "Riskier repair labels are visible as guarded flows, not arbitrary scripts.", "Guard", "/api/protection/evaluate-action"),
                T("Repair", "Network, services, Store, disk, audio, display, update repair", "Legacy repair categories stay visible and route to safe preview/report flows.", "Preview", "/api/repair/preview"),
                T("Cleanup", "Cache/temp/registry ringan", "Repair cleanup is safe-mode first with no destructive registry scripts.", "Guard", "/api/cleanup/preview"),
            },
            ["PowerOptimization"] = new[]
            {
                T("Power", "Balanced AI / Ultra Performance / Ultra Battery", "Power profiles preview battery/thermal impact before apply.", "Preview", "/api/power/status"),
                T("Power Modes", "Balanced Mode / Battery Intelligence / CPU Power Control", "v1.3 power controls are restored as profile previews with thermal/battery notes.", "Preview", "/api/power/preview"),
                T("Automation", "Apply AI Auto Power Rules / Adaptive System Control", "AI power automation remains dry-run first and owner-controlled.", "Dry-run", "/api/automation/preview"),
                T("Recovery", "Backup Power Config / Restore Power Config", "Power configuration backup/restore stays visible before profile changes.", "Restore", "/api/restore/sessions"),
                T("Power", "CPU, GPU, disk, network power control", "Hardware power guidance avoids low-level unsafe toggles.", "Guard", "/api/power/preview"),
                T("Recovery", "Backup/restore power config", "Power changes remain reversible.", "Restore", "/api/restore/sessions"),
            },
            ["VisualEffects"] = new[]
            {
                T("Visual", "Best performance / balanced / best appearance", "Visual presets are reversible and preview-first.", "Preview", "/api/visual-effects/status"),
                T("Visual Controls", "Animation Control / Advanced Visual Tweaks / Adaptive Visual Engine", "v1.3 visual controls are restored as reversible UI/profile previews.", "Restore", "/api/visual-effects/preview"),
                T("Profiles", "Background UI Optimization / Comfortable / Compact", "Visual experience presets stay user-controlled and readable.", "Review", "/api/visual-effects/preview"),
                T("Recovery", "Backup Visual Setting / Restore Visual Setting", "Visual settings can be restored after applying a preset.", "Restore", "/api/restore/sessions"),
                T("Visual", "Animation, transparency, window/explorer/input effects", "v1.3 visual controls are restored as readable modern cards.", "Restore", "/api/visual-effects/preview"),
                T("Profiles", "Gaming/streaming UI optimization", "Scenario visual profiles remain opt-in.", "Review", "/api/visual-effects/preview"),
            },
            ["RestoreBackup"] = new[]
            {
                T("Backup", "Full backup / selective backup", "System, registry/settings, drivers, services, visual, and app-state backup concepts remain visible.", "Restore", "/api/restore/sessions"),
                T("Backup", "CREATE FULL BACKUP / Backup System Config / Backup System State", "v1.3 backup buttons remain visible as restore-session workflows.", "Restore", "/api/restore/sessions"),
                T("Snapshot", "Create Snapshot State / Clean state / Before tweak", "Snapshot labels from v1.3 are restored as metadata-first restore sessions.", "Restore", "/api/restore/sessions"),
                T("Validation", "Deep Restore Scan / Analyze Restore Impact", "Restore impact analysis remains visible before rollback.", "Review", "/api/restore/preview"),
                T("Restore", "Quick rollback / one-click restore", "Restore preview/apply/verify flow remains explicit.", "Confirm", "/api/restore/preview"),
                T("Protection", "Backup protection and scheduler", "Backup scheduler guidance is restored without hidden tasks by default.", "Manual", "/api/automation/preview"),
            },
            ["RestorePointManager"] = new[]
            {
                T("Restore", "Smart/tagged restore point", "Restore point creation is admin-aware and metadata-first.", "Admin", "/api/restore-points/status"),
                T("Restore Point", "CREATE SMART RESTORE POINT / Cleanup Old Points", "v1.3 restore point controls are restored with admin-aware status and cleanup review.", "Admin", "/api/restore-points/status"),
                T("Impact", "Analyze Restore Impact / Deep Restore Scan", "Restore impact remains visible before rollback or cleanup.", "Review", "/api/restore-points/preview"),
                T("Validation", "Safe restore validator / impact analysis", "Restore impact and validation remain visible before rollback.", "Review", "/api/restore-points/preview"),
                T("Maintenance", "Cleanup restore points / system protection", "Restore maintenance requires explicit review.", "Manual", "/api/restore/export"),
            },
            ["ScheduledAutomation"] = new[]
            {
                T("Automation", "Autonomous/assisted modes", "Automation is scan/report-first by default and mutating rules require owner setup.", "Dry-run", "/api/automation/rules"),
                T("Scheduler", "Schedule Daily Optimization / Schedule Weekly Optimization / Deferred Task Manager", "v1.3 scheduling is restored as dry-run/report-first automation.", "Dry-run", "/api/automation/preview"),
                T("Decision", "Decision Engine Review / Adaptive Recovery Logic", "Automation decisions remain auditable and cannot bypass Safety Guard.", "Guard", "/api/action-log"),
                T("AI", "Predictive automation / decision engine", "AI automation stays allowlisted and auditable.", "Guard", "/api/automation/preview"),
                T("Audit", "Deferred tasks, audit trail, maintenance windows", "Automation history and safety review remain visible.", "Report", "/api/action-log"),
            },
            ["TaskRuleSystem"] = new[]
            {
                T("Rules", "Build workflow / dry-run / enable-disable", "Task rules are previewed and cannot run dangerous unattended actions.", "Dry-run", "/api/automation/preview"),
                T("Rules", "Apply Goal / Apply Engine Settings / Activate Selected Mode", "v1.3 rule-engine buttons are restored as dry-run rule previews.", "Dry-run", "/api/automation/preview"),
                T("Rules", "Allow Selected / Allow Hanya Tertentu / Block Sementara", "Allow/block style controls are evaluated before any automation action can run.", "Guard", "/api/protection/evaluate-action"),
                T("Rules", "Silent executor review", "Silent execution is review-only unless explicitly configured by owner.", "Guard", "/api/automation/rules"),
            },
            ["UtilitiesTools"] = new[]
            {
                T("Utilities", "Storage, diagnostics, repair, network, control", "v1.3 toolbox categories are restored as safe utility cards.", "Safe", "/api/utilities/status"),
                T("Utility Presets", "AUTO FIX SYSTEM / Repair Utilities / FULL SYSTEM MAINTENANCE", "Long-running utilities stay explicit, time-aware, and report-first.", "Admin", "/api/master-test/status"),
                T("Utility Groups", "Automation-Linked Utilities / Backup Cleaner / Bottleneck Detection", "Utility groups are restored as risk-labeled diagnostics rather than raw script execution.", "Risk-labeled", "/api/utilities/status"),
                T("Shell", "CMD / PowerShell shortcuts", "Console-style tools are represented as manual shortcuts only; AI cannot run arbitrary shell.", "Manual", "Windows tools"),
                T("Utilities", "Filesystem, security, registry, performance, driver, display, power, monitoring", "Utility actions are labeled by risk and routed to preview/report endpoints.", "Risk-labeled", "/api/product/storage"),
                T("Maintenance", "Auto fix system / quick clean repair / full maintenance", "Maintenance presets remain preview/report-first.", "Preview", "/api/master-test/status"),
            },
            ["MasterTestEngine"] = new[]
            {
                T("Testing", "Unit, integration, UI flow, E2E, regression", "Legacy test suite buttons are restored as QA matrix/status output.", "Smoke", "/api/master-test/status"),
                T("QA", "Full QA Matrix / Feature Audit Full / Audit Trail Detail", "v1.3 release-gate test surfaces remain visible in the modern test page.", "Gate", "/api/feature-audit/matrix"),
                T("QA", "Runtime route contract / UI click-flow / installed validation", "The latest app uses automated contract tests plus live UI automation before release.", "Smoke", "dotnet test / pytest / UI Automation"),
                T("Testing", "Performance, stress, stability, security, compatibility", "Test layers and metrics remain visible before release.", "Report", "/api/master-test/run"),
                T("Release", "Installed validation gate", "Release stays blocked until installed validation passes.", "Gate", "/api/feature-audit/status"),
            },
            ["FeatureAudit"] = new[]
            {
                T("Audit", "Quick/full feature audit", "Read-only feature audit is preserved with docs sync and Safety Guard checks.", "Read-only", "/api/feature-audit/run"),
                T("Audit", "Send report / open logs", "Report export and logs remain available for QA evidence.", "Report", "/api/action-log"),
            },
            ["FeatureAuditMatrix"] = new[]
            {
                T("Matrix", "v1.3 parity matrix", "Feature pass/roadmap/blocker states remain explicit.", "Read-only", "/api/feature-audit/matrix"),
                T("Gate", "Release readiness", "Public release is blocked until installer/runtime validation passes.", "Gate", "/api/update/check"),
            },
            ["KnowledgeBase"] = new[]
            {
                T("Knowledge", "DLSS/FSR/XeSS/VRR/Reflex/AFMF topics", "Beginner-friendly tuning knowledge remains local and searchable.", "Read-only", "/api/kb/topics"),
                T("Knowledge", "Safety and no-FPS-guarantee guidance", "Optimization explanations avoid misleading claims.", "Info", "/api/kb/search?q=safety"),
            },
            ["BenchmarkLab"] = new[]
            {
                T("Benchmark", "Manual FPS / CSV import", "Manual benchmark and import flow are preserved.", "Manual", "/api/benchmark/manual"),
                T("History", "Local benchmark history/export", "Benchmark data stays local and exportable.", "Report", "/api/benchmark/export"),
            },
            ["PerformanceHistory"] = new[]
            {
                T("History", "Timeline/trends/compare", "Before/after scan history and trend comparison remain visible.", "Read-only", "/api/history/timeline"),
                T("Export", "History export", "Local history is exportable.", "Report", "/api/history/export"),
            },
            ["PerformanceReport"] = new[]
            {
                T("Report", "Latest report / compare", "Performance report uses local counters and no guaranteed FPS claims.", "Report", "/api/reports/latest"),
                T("Export", "JSON/MD export", "Report export stays visible.", "Report", "/api/reports/export"),
            },
            ["Settings"] = new[]
            {
                T("Settings", "Theme, sidebar, language", "v1.3 UI settings are preserved with reduced motion and privacy-first defaults.", "Local", "ui_settings.json"),
                T("AI", "NVIDIA settings", "NVIDIA provider key/model/fallback settings stay secure.", "Secret-safe", "Secure settings"),
                T("Discord", "Webhook reporting/update notification", "Webhook URLs are validated and redacted.", "Secret-safe", "/api/webhooks/test-error"),
                T("Updates", "Auto check/install toggles", "Update checks remain manual/owner-controlled.", "Review", "/api/update/check"),
            },
        };

        public static void Apply(CyberPageViewModel page)
        {
            if (page == null)
                return;

            var key = string.IsNullOrWhiteSpace(page.FeatureKey) ? page.Title : page.FeatureKey;
            if (!TryGetTools(key, page.Title, out var tools))
                return;

            foreach (var tool in tools)
            {
                page.LegacyTools.Add(new LegacyToolViewModel
                {
                    Category = tool.Category,
                    Title = tool.Title,
                    Flow = tool.Flow,
                    Safety = tool.Safety,
                    Route = tool.Route,
                });
            }
        }

        private static bool TryGetTools(string key, string title, out IReadOnlyList<Tool> tools)
        {
            if (Tools.TryGetValue(key ?? string.Empty, out tools) || Tools.TryGetValue(title ?? string.Empty, out tools))
                return true;

            var normalizedKey = NormalizeKey(key);
            var normalizedTitle = NormalizeKey(title);
            foreach (var item in Tools)
            {
                var normalizedCatalogKey = NormalizeKey(item.Key);
                if (string.Equals(normalizedCatalogKey, normalizedKey, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(normalizedCatalogKey, normalizedTitle, StringComparison.OrdinalIgnoreCase))
                {
                    tools = item.Value;
                    return true;
                }
            }

            tools = Array.Empty<Tool>();
            return false;
        }

        private static string NormalizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("&", string.Empty, StringComparison.Ordinal)
                .Replace("/", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace(".", string.Empty, StringComparison.Ordinal)
                .Trim();
        }

        private static Tool T(string category, string title, string flow, string safety, string route)
        {
            return new Tool(category, title, flow, safety, route);
        }
    }
}
