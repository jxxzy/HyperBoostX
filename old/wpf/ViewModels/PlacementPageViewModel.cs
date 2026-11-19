using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace HyperBoostX.ViewModels
{
    public sealed class PlacementSectionViewModel
    {
        public string Title { get; init; } = "Module";
        public string Description { get; init; } = "";
        public ObservableCollection<string> Items { get; } = new();
    }

    public sealed class WorkflowStepViewModel
    {
        public string Step { get; init; } = "01";
        public string Title { get; init; } = "Inspect";
        public string Detail { get; init; } = "Load local evidence before reviewing changes.";
    }

    public sealed class PlacementPageSpec
    {
        public string Key { get; init; } = "Dashboard";
        public string Purpose { get; init; } = "Review the current feature status, run a safe preview, then inspect report and restore evidence.";
        public string EmptyState { get; init; } = "Run the first action to load local evidence. No recommendation is fabricated before data exists.";
        public string ResultIntro { get; init; } = "No action has run yet. Results will appear here after the local backend returns data.";
        public string SafetyNote { get; init; } = "Safety Guard remains active in every mode. Preview, confirmation, restore metadata, and reporting stay required for mutating flows.";
        public string RestoreNote { get; init; } = "Restore evidence is kept close to apply/history actions so changes remain reversible where supported.";
        public string StateTitle { get; init; } = "Operational Snapshot";
        public string WorkspaceTitle { get; init; } = "Feature Workspace";
        public string ActionTitle { get; init; } = "Operational Runbook";
        public string ActionHint { get; init; } = "Inspect local evidence first, review the safe scope, then use guarded actions only when the report and restore path are visible.";
        public string SecondaryActionTitle { get; init; } = "Evidence Actions";
        public string ResultTitle { get; init; } = "Audit Console";
        public string SafetyTitle { get; init; } = "Safety Boundary";
        public string RecommendationsTitle { get; init; } = "Usage Notes";
        public IReadOnlyList<PlacementSectionViewModel> Sections { get; init; } = Array.Empty<PlacementSectionViewModel>();
        public IReadOnlyDictionary<string, string> ActionLabels { get; init; } = new Dictionary<string, string>();
        public IReadOnlySet<string> HiddenActionKinds { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public class PlacementPageViewModel : CyberPageViewModel
    {
        private string _resultSummary = "No action has run yet. Use a scan or review action before applying anything.";

        protected PlacementPageViewModel(string title, string subtitle, string featureKey = null)
            : base(title, subtitle, featureKey)
        {
            ConfigurePlacement(ResolveFeatureKey());
        }

        public PlacementPageViewModel(string featureKey, string title, string subtitle, string primaryAction, params string[] recommendations)
            : base(title, subtitle, featureKey)
        {
            PrimaryAction = primaryAction;
            foreach (var recommendation in recommendations)
                Recommendations.Add(recommendation);
            ConfigurePlacement(featureKey);
        }

        public string Purpose { get; private set; } = "";
        public string EmptyState { get; private set; } = "";
        public string SafetyNote { get; private set; } = "";
        public string RestoreNote { get; private set; } = "";
        public string StateTitle { get; private set; } = "Operational Snapshot";
        public string WorkspaceTitle { get; private set; } = "Feature Workspace";
        public string ActionTitle { get; private set; } = "Operational Runbook";
        public string ActionHint { get; private set; } = "";
        public string SecondaryActionTitle { get; private set; } = "Evidence Actions";
        public string ResultTitle { get; private set; } = "Audit Console";
        public string SafetyTitle { get; private set; } = "Safety Boundary";
        public string RecommendationsTitle { get; private set; } = "Usage Notes";
        public string ResultSummary { get => _resultSummary; set => SetProperty(ref _resultSummary, value); }
        public ObservableCollection<PlacementSectionViewModel> PlacementSections { get; } = new();
        public ObservableCollection<FeatureActionViewModel> PrimaryPlacementActions { get; } = new();
        public ObservableCollection<FeatureActionViewModel> SecondaryPlacementActions { get; } = new();
        public ObservableCollection<FeatureActionViewModel> RestorePlacementActions { get; } = new();
        public ObservableCollection<string> AdvancedRouteLines { get; } = new();
        public ObservableCollection<WorkflowStepViewModel> WorkflowSteps { get; } = new();

        private void ConfigurePlacement(string featureKey)
        {
            var spec = PlacementPageCatalog.Get(featureKey, Title, Subtitle);
            Purpose = spec.Purpose;
            EmptyState = spec.EmptyState;
            SafetyNote = spec.SafetyNote;
            RestoreNote = spec.RestoreNote;
            StateTitle = spec.StateTitle;
            WorkspaceTitle = spec.WorkspaceTitle;
            ActionTitle = spec.ActionTitle;
            ActionHint = spec.ActionHint;
            SecondaryActionTitle = spec.SecondaryActionTitle;
            ResultTitle = spec.ResultTitle;
            SafetyTitle = spec.SafetyTitle;
            RecommendationsTitle = spec.RecommendationsTitle;
            ResultSummary = spec.ResultIntro;
            LiveResultTitle = "Advanced Details";
            LiveResult = "Redacted backend detail appears here after a user action. The readable result above stays the primary view.";

            PlacementSections.Clear();
            foreach (var section in spec.Sections)
                PlacementSections.Add(section);

            PrimaryPlacementActions.Clear();
            SecondaryPlacementActions.Clear();
            RestorePlacementActions.Clear();
            AdvancedRouteLines.Clear();
            WorkflowSteps.Clear();
            foreach (var step in BuildWorkflowSteps(spec))
                WorkflowSteps.Add(step);

            foreach (var action in FeatureActions.Select(action => CloneAction(action, spec)))
            {
                var kind = GetActionKind(action);
                AdvancedRouteLines.Add($"{action.Method} {action.Path} - {action.Label}");

                if (spec.HiddenActionKinds.Contains(kind) || IsInternalBeginnerAction(kind))
                    continue;

                if (kind == "restore")
                    RestorePlacementActions.Add(action);
                else if (kind is "primary" or "preview" or "apply")
                    PrimaryPlacementActions.Add(action);
                else
                    SecondaryPlacementActions.Add(action);
            }

            if (PrimaryPlacementActions.Count == 0 && FeatureActions.Count > 0)
            {
                var fallback = FeatureActions
                    .Select(action => CloneAction(action, spec))
                    .FirstOrDefault(action => !spec.HiddenActionKinds.Contains(GetActionKind(action)) && !IsInternalBeginnerAction(GetActionKind(action)));
                if (fallback != null)
                    PrimaryPlacementActions.Add(fallback);
            }

            if (AdvancedRouteLines.Count == 0)
                AdvancedRouteLines.Add("No backend action map entry is available for this page.");
        }

        private string ResolveFeatureKey()
        {
            if (!string.IsNullOrWhiteSpace(FeatureKey))
                return FeatureKey;

            var typeName = GetType().Name;
            return typeName.EndsWith("ViewModel", StringComparison.Ordinal)
                ? typeName[..^"ViewModel".Length]
                : typeName;
        }

        private static bool IsInternalBeginnerAction(string kind) =>
            kind is "readiness" or "audit" or "refresh";

        private static FeatureActionViewModel CloneAction(FeatureActionViewModel source, PlacementPageSpec spec)
        {
            var clone = new FeatureActionViewModel
            {
                Id = source.Id,
                MenuKey = source.MenuKey,
                Label = BuildActionLabel(source, spec),
                Command = source.Command,
                Method = source.Method,
                Path = source.Path,
                Payload = source.Payload == null ? null : (JObject)source.Payload.DeepClone(),
                RequiresAdmin = source.RequiresAdmin,
                PreviewRequired = source.PreviewRequired,
                ConfirmationRequired = source.ConfirmationRequired,
                SafetyGuard = source.SafetyGuard,
                Restore = source.Restore,
                IsDestructive = source.IsDestructive,
                Partial = source.Partial,
                TestCoverage = source.TestCoverage,
                Tooltip = source.Tooltip,
                SuccessState = source.SuccessState,
                ErrorState = source.ErrorState,
                LoadingState = source.LoadingState,
                Status = source.Status,
                IsEnabled = source.IsEnabled
            };
            return clone;
        }

        private static IEnumerable<WorkflowStepViewModel> BuildWorkflowSteps(PlacementPageSpec spec)
        {
            var sections = spec.Sections.ToList();
            yield return new WorkflowStepViewModel
            {
                Step = "01",
                Title = sections.Count > 0 ? $"Inspect {sections[0].Title}" : "Inspect Evidence",
                Detail = sections.Count > 0 ? sections[0].Description : spec.EmptyState
            };

            yield return new WorkflowStepViewModel
            {
                Step = "02",
                Title = sections.Count > 1 ? $"Review {sections[1].Title}" : "Review Scope",
                Detail = sections.Count > 1 ? sections[1].Description : spec.ActionHint
            };

            yield return new WorkflowStepViewModel
            {
                Step = "03",
                Title = sections.Count > 2 ? $"Recover {sections[2].Title}" : "Recover Safely",
                Detail = sections.Count > 2 ? sections[2].Description : spec.RestoreNote
            };
        }

        private static string BuildActionLabel(FeatureActionViewModel action, PlacementPageSpec spec)
        {
            var kind = GetActionKind(action);
            if (spec.ActionLabels.TryGetValue(kind, out var label))
                return label;

            return kind switch
            {
                "primary" => "Open Feature Status",
                "preview" => "Review Evidence Plan",
                "apply" => "Apply Reviewed Selection",
                "restore" => "Restore Recorded Change",
                "export" => "Export Redacted Report",
                "log" => "Open Action History",
                "help" => "Open Safety Guide",
                _ => action.Label
            };
        }

        private static string GetActionKind(FeatureActionViewModel action)
        {
            var id = action.Id ?? string.Empty;
            var parts = id.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
                return parts[^2];
            if (parts.Length >= 2)
                return parts[^1];

            if (action.Path.Contains("restore", StringComparison.OrdinalIgnoreCase) ||
                action.Path.Contains("undo", StringComparison.OrdinalIgnoreCase))
                return "restore";
            if (action.Path.Contains("apply", StringComparison.OrdinalIgnoreCase))
                return "apply";
            if (action.Path.Contains("preview", StringComparison.OrdinalIgnoreCase))
                return "preview";
            return "primary";
        }
    }

    public static class PlacementPageCatalog
    {
        private sealed record SectionSeed(string Title, string Description, string[] Items);

        private sealed record PageSeed(
            string Purpose,
            string EmptyState,
            string ResultIntro,
            string SafetyNote,
            string RestoreNote,
            string WorkspaceTitle,
            SectionSeed[] Sections,
            Dictionary<string, string> Actions,
            string[] HiddenKinds);

        private static readonly string[] DefaultHiddenKinds = { "readiness", "audit", "refresh" };

        private static readonly IReadOnlyDictionary<string, PageSeed> Pages = new Dictionary<string, PageSeed>(StringComparer.OrdinalIgnoreCase)
        {
            ["PerformanceBoost"] = Page(
                "Scan CPU, RAM, disk, startup, and background pressure before building a reversible performance plan.",
                "No performance pressure scan has run yet. Load local evidence before applying any fix.",
                "Performance output will show pressure, approved actions, and report history after scan or preview.",
                "Blocked: unsafe service disables, protected-process kills, driver edits, and arbitrary shell commands.",
                "Performance restore uses boost undo/session metadata when an approved plan changed system state.",
                "Performance Overview",
                Actions(("primary", "Scan Performance Pressure"), ("preview", "Preview Safe Performance Fix"), ("apply", "Apply Approved Performance Fix"), ("restore", "Restore Performance Changes"), ("export", "Export Performance Report")),
                Hidden(),
                Section("CPU / RAM Pressure", "Local pressure view for foreground apps, background load, and memory headroom.", "CPU pressure", "RAM pressure", "CPU/RAM Optimizer handoff", "HyperBalance plan"),
                Section("Disk / Startup Pressure", "Startup and disk pressure are reviewed before any optimization plan is applied.", "Disk pressure", "Startup pressure", "Background process pressure", "Safe recommendations"),
                Section("Plan Review", "Beginner mode shows only allowlisted fixes with readable impact.", "Preview first", "Approval required", "Before/after report", "Undo evidence")),

            ["StartupManager"] = Page(
                "Inspect startup apps, impact, publisher, path, and selected changes before applying anything.",
                "Startup entries have not been loaded yet. Use Load Startup Apps to collect local startup data.",
                "Startup results will show item count, impact, preview, apply status, and restore evidence.",
                "Blocked: disabling driver, audio, security, anti-cheat, and required vendor startup entries.",
                "Startup restore uses session metadata for entries changed through the approved apply path.",
                "Startup Impact Summary",
                Actions(("primary", "Load Startup Apps"), ("preview", "Scan Startup Impact"), ("apply", "Preview Startup Changes"), ("restore", "Restore Startup State"), ("export", "Export Startup Report")),
                Hidden(),
                Section("Startup Apps Table", "Rows are loaded from the backend before per-item controls are available.", "Publisher", "Path", "Impact", "Status"),
                Section("Selected App Detail", "Selected app actions stay individual and preview-first.", "Enable selected app", "Disable selected app", "Delay startup app", "High impact apps"),
                Section("Restore & History", "Startup changes stay traceable and reversible where supported.", "Restore session", "Action history", "Export report")),

            ["BackgroundApps"] = Page(
                "Review running and background process pressure with protected-process guidance.",
                "No process pressure scan has run yet. Scan background apps to load local process evidence.",
                "Process output will show pressure, protected items, and safe-stop guidance after scan.",
                "System, security, anti-cheat, driver, audio, and vendor utility processes remain protected.",
                "Stop guidance is review-only unless the backend returns an allowlisted reversible action.",
                "Process Pressure",
                Actions(("primary", "Scan Background Apps"), ("preview", "Review Process Pressure"), ("apply", "Review Safe Stop Candidate"), ("restore", "Open Protected Process List"), ("export", "Export Process Report")),
                Hidden("restore"),
                Section("Running Apps", "Foreground and background apps are separated before any recommendation.", "Process name", "CPU / RAM pressure", "Publisher", "Window state"),
                Section("Protected Processes", "Required system and vendor processes are shown as protected, not as boost targets.", "Security tools", "Anti-cheat", "Driver services", "Vendor utilities"),
                Section("Selected Process Detail", "Beginner mode explains impact and safety instead of force-killing apps.", "Safe stop suggestions", "Whitelist note", "Manual close guidance")),

            ["Cleanup"] = Page(
                "Scan safe cleanup categories, protect personal folders, then clean only selected approved items.",
                "Cleanup scan has not run yet. Personal folders and browser sessions remain excluded by default.",
                "Cleanup output will show selected categories, estimated size, warnings, and cleanup history.",
                "Blocked: Documents, Downloads, Desktop, game saves, project folders, browser sessions, and unreviewed user-file deletion.",
                "Cleanup restore covers supported cleanup metadata only; personal files stay out of default cleanup scope.",
                "Cleanup Categories",
                Actions(("primary", "Run Cleanup Scan"), ("preview", "Preview Safe Cleanup"), ("apply", "Clean Selected Items"), ("restore", "Restore Last Cleanup"), ("export", "Export Cleanup Report")),
                Hidden(),
                Section("Safe Cleanup Scope", "Cleanup starts as a report, not a delete operation.", "Temp files", "Windows cache", "Recycle Bin", "Windows Update cache"),
                Section("Browser & Personal Protection", "Browser sessions and personal folders require explicit warning before any action.", "Browser cache review", "Excluded personal folders", "Game saves protected", "Project folders protected"),
                Section("Large File Finder", "Large and duplicate files are review-only until the owner selects items.", "Large files", "Duplicates", "Selected item detail")),

            ["Storage"] = Page(
                "Review drive usage, disk pressure, large files, and cleanup guidance without destructive defaults.",
                "Storage status has not loaded yet. Scan storage usage to read local drive data.",
                "Storage output will show drive usage, pressure, cleanup preview, and export status.",
                "Blocked: destructive personal-file cleanup, unreviewed duplicate deletion, and protected folder removal.",
                "Storage cleanup restore is limited to supported cleanup sessions and approved cleanup metadata.",
                "Storage Overview",
                Actions(("primary", "Scan Storage Usage"), ("preview", "Preview Storage Cleanup"), ("export", "Export Storage Report"), ("help", "Open Storage Guidance")),
                Hidden("apply", "restore"),
                Section("System Drive Usage", "System drive usage is separated from cleanup recommendation.", "Drive list", "Used / free space", "Disk pressure", "Storage settings handoff"),
                Section("Large File Review", "Large files and duplicates are surfaced for manual review.", "Large files", "Duplicate candidates", "Personal file warning", "Review-only mode"),
                Section("Smart Recommendation", "Recommendations explain impact without claiming automatic cleanup.", "Cache candidates", "Safe cleanup preview", "Export report")),

            ["OneClickBoost"] = Page(
                "Create a mode-based boost plan, review approved actions, and keep undo evidence visible.",
                "No boost plan exists yet. Preview a plan before starting any boost.",
                "Boost output will show the selected mode, approved action list, report, and restore route.",
                "Blocked: arbitrary AI shell commands, unsafe services, security disables, and protected-process kills.",
                "Last boost can be restored through the supported undo/session flow after an approved boost.",
                "Boost Plan",
                Actions(("primary", "Preview Boost Plan"), ("preview", "Review Approved Boost Scope"), ("apply", "Start Safe Boost"), ("restore", "Restore Last Boost"), ("export", "Export Boost Report")),
                Hidden(),
                Section("Boost Mode Selector", "Choose a safe scope before any apply action.", "Safe", "Balanced", "Before Gaming", "Custom"),
                Section("Custom Checklist", "Custom boost is a checklist of guarded categories, not a raw command runner.", "Performance", "Background", "Cleanup", "Network", "Visual", "Update control", "Security safe"),
                Section("Approved Actions", "Only reviewed, allowlisted actions can be applied.", "Preview boost plan", "Before/after report", "Undo evidence")),

            ["AutoGamingMode"] = Page(
                "Detect games, preview game profiles, apply selected safe profile metadata, and restore normal mode.",
                "No game has been detected yet. Use Detect Games before enabling a profile.",
                "Gaming Mode output will show detected game, profile preview, session report, and restore status.",
                "Protected apps stay locked while gaming; browsers and work apps are not treated as games by default.",
                "Normal mode restore is available after an approved gaming profile changes state.",
                "Gaming Mode",
                Actions(("primary", "Detect Games"), ("preview", "Preview Gaming Profile"), ("apply", "Start Gaming Mode"), ("restore", "Restore Normal Mode"), ("export", "Export Gaming Report")),
                Hidden(),
                Section("Auto Activation", "Gaming Mode prepares a reversible profile only after real game detection.", "Detected game", "Auto activation", "Session status", "Restore after close"),
                Section("Game Profiles", "Profiles stay tied to selected games and visible rules.", "Game library", "Selected profile", "Overlay policy", "Background rules"),
                Section("Protected Apps", "Capture tools, anti-cheat, vendor apps, and security tools remain protected.", "Protected app list", "Manual review", "Session report")),

            ["AIPerformanceAdvisor"] = Page(
                "Run local analysis, generate a readable action plan, and apply only owner-approved safe fixes.",
                "Smart Recommendation has not run yet. Run Smart Scan to collect local evidence.",
                "AI Hub output will show system analysis, action plan, approval state, and reasoning summary.",
                "AI output cannot execute arbitrary shell commands or bypass Safety Guard.",
                "Approved fixes use normal restore metadata when a supported mutating flow is applied.",
                "Smart Recommendation / AI Hub",
                Actions(("primary", "Run Smart Scan"), ("preview", "Generate Action Plan"), ("apply", "Apply Approved Fixes"), ("restore", "Restore AI Changes"), ("export", "Export Recommendation Report")),
                Hidden(),
                Section("System Analysis", "Local scan evidence is separated from AI guidance.", "Auto scan status", "Performance score", "System analysis", "Maintenance reminder"),
                Section("Smart Suggestions", "Suggestions explain why they exist and stay honest about limits.", "Action plan", "Approval panel", "Reasoning summary", "No FPS guarantee"),
                Section("AI Modules", "AI Performance Advisor and NVIDIA API provider stay approval-gated.", "AI Performance Advisor", "NVIDIA API Provider", "Advanced reasoning collapsed")),

            ["GpuCenter"] = Page(
                "Detect GPU vendor, VRAM, temperature when available, overlays, and driver handoff guidance.",
                "GPU status has not loaded yet. Refresh GPU status to read local hardware data.",
                "GPU output will show vendor, driver, overlay guidance, report export, and safe handoff status.",
                "Blocked: overclock, undervolt, BIOS edits, forced driver-service changes, and silent driver installs.",
                "Driver or GPU changes remain manual; restore evidence appears only for supported HyperBoostX actions.",
                "GPU Summary",
                Actions(("primary", "Refresh GPU Status"), ("preview", "Review GPU Guidance"), ("export", "Export GPU Report"), ("log", "GPU History")),
                Hidden("apply", "restore"),
                Section("Vendor Detection", "GPU data is read from local backend evidence where available.", "NVIDIA / AMD / Intel / Microsoft Basic", "Model", "Driver status", "VRAM usage"),
                Section("Overlay & Vendor Apps", "Overlay and RGB/vendor app status are guidance-only unless a safe backend route exists.", "Overlay status", "RGB/vendor app status", "Temperature if available"),
                Section("Driver Center Handoff", "Driver updates are manual OEM/vendor handoffs.", "Open Driver Center", "Official source reminder", "No silent install")),

            ["HardwareVendorCenter"] = Page(
                "Analyze OEM/vendor utilities, RGB/LCD helpers, overlays, startup entries, and service pressure without breaking required hardware controls.",
                "Vendor App Analyzer has not scanned yet. Run vendor analysis to classify utilities before changing startup or services.",
                "Vendor output will show detected utilities, protected roles, safe recommendations, MSI submodule status, and report history.",
                "Blocked: fan/RGB/LCD control breaks, firmware/BIOS edits, driver-service disables, silent uninstall, and blind vendor-service kill.",
                "Vendor changes remain preview-first and restore-aware; required display, fan, audio, and hardware-control utilities stay protected.",
                "Vendor App Analyzer",
                Actions(("primary", "Scan Vendor Utilities"), ("preview", "Build Vendor Safe Plan"), ("apply", "Apply Selected Vendor Changes"), ("restore", "Restore Vendor Startup State"), ("export", "Export Vendor Report"), ("help", "Open Vendor Safety Guide")),
                Hidden(),
                Section("Vendor Inventory", "Detects known OEM/vendor tools and labels them by role before guidance is shown.", "MSI Center / Dragon Center", "ASUS Armoury Crate", "Gigabyte Control Center", "Lenovo / Dell / HP / Acer tools"),
                Section("Service & Startup Roles", "Classifies each item so required controls are not treated like generic background noise.", "Required hardware control", "Optional startup item", "RGB/LCD/display helper", "Overlay or monitoring"),
                Section("MSI Safe Optimizer Submodule", "MSI-specific guidance stays inside Vendor Center or Advanced mode and never disables fan/RGB/driver services blindly.", "MSI Center status", "Mystic Light / Nahimic", "Afterburner / RTSS", "TRCC / KANALI / HiMOS protection")),

            ["GamingBooster"] = Page(
                "Analyze gaming setup, choose a real game/profile, run targeted boost, and keep restore one step away.",
                "No gaming setup analysis has run yet. Analyze gaming setup before boosting.",
                "Booster output will show detected game, selected profile, booster actions, and report history.",
                "Blocked: boosting browsers as games, protected-process kills, anti-cheat tweaks, and unsafe service disables.",
                "Gaming changes can be restored through boost undo/session metadata after approved apply.",
                "Gaming Setup Analysis",
                Actions(("primary", "Analyze Gaming Setup"), ("preview", "Review Booster Actions"), ("apply", "Boost Selected Game"), ("restore", "Restore Gaming Changes"), ("export", "Export Booster Report")),
                Hidden(),
                Section("Detected Game", "A targeted boost needs a selected or detected game.", "Detected game", "Selected game profile", "Game library handoff"),
                Section("Booster Actions", "Boost scope is visible before apply.", "Overlay review", "Background review", "Targeted game boost", "Booster report"),
                Section("Restore Path", "Undo and report actions stay nearby.", "Restore gaming changes", "Action history", "Export report")),

            ["CreatorMode"] = Page(
                "Check creator app context, render readiness, scratch space, cache, and safe creator focus mode.",
                "Creator workspace has not been scanned yet. Scan creator workspace to inspect local readiness.",
                "Creator output will show app detection, render readiness, cache review, and profile status.",
                "Blocked: deleting project files, aggressive cache cleanup during active renders, and protected-process kills.",
                "Creator defaults can be restored after a supported approved creator profile is applied.",
                "Creator Workspace",
                Actions(("primary", "Scan Creator Workspace"), ("preview", "Check Render Readiness"), ("apply", "Apply Creator Profile"), ("restore", "Restore Creator Defaults"), ("export", "Export Creator Report")),
                Hidden(),
                Section("Creator App Detection", "Recognizes active editing/rendering context where local data exists.", "Premiere / After Effects", "DaVinci / Blender", "CapCut", "Active render app"),
                Section("Readiness", "Creator readiness focuses on headroom, disk, and GPU state.", "RAM readiness", "Scratch space", "GPU readiness", "Background app review"),
                Section("Cache & Focus", "Cache cleanup is conservative and preview-first.", "Safe creator cache cleanup", "Creator focus mode", "Creator report")),

            ["NetworkBooster"] = Page(
                "Run adapter, DNS, latency, jitter, packet loss, and TCP/IP diagnostics before any network fix.",
                "Network diagnostics have not run yet. Run diagnostics before changing anything.",
                "Network output will show adapter, DNS, latency, jitter, packet loss, safe fix preview, and history.",
                "Blocked: destructive reset without confirmation, fake ping claims, and permanent network service hacks.",
                "Network changes require preview and can expose restore/history where a supported route exists.",
                "Network Diagnostic",
                Actions(("primary", "Run Network Diagnostic"), ("preview", "Preview Safe Network Fix"), ("apply", "Apply Safe Network Fix"), ("restore", "Restore Network Changes"), ("export", "Export Network Report")),
                Hidden(),
                Section("Connection Evidence", "Network actions start with measured local context.", "Active adapter", "DNS status", "Ping / latency", "Bandwidth snapshot if available"),
                Section("Stability Checks", "Jitter, packet loss, and TCP/IP health are shown separately.", "Jitter", "Packet loss", "TCP/IP health", "Risky reset area"),
                Section("History", "No ping-lower guarantee is shown; reports keep measured results.", "Network history", "Safe fix preview", "Export report")),

            ["DnsLatencyTools"] = Page(
                "Test DNS speed, ping, jitter, packet loss, traceroute, and gaming latency before DNS changes.",
                "No DNS or latency test has run yet. Test DNS speed or ping first.",
                "DNS output will show provider comparison, ping, traceroute, change preview, and history.",
                "Blocked: unapproved DNS changes, fake latency values, and destructive network resets.",
                "DNS restore uses supported settings/session evidence where available.",
                "DNS & Latency",
                Actions(("primary", "Test DNS Speed"), ("preview", "Compare DNS Providers"), ("apply", "Preview DNS Change"), ("restore", "Restore DNS Settings"), ("export", "Export DNS Report")),
                Hidden(),
                Section("Current DNS", "DNS provider data is measured locally where supported.", "Current DNS", "Provider comparison", "DNS speed table"),
                Section("Latency Tools", "Latency checks stay evidence-based.", "Ping test", "Jitter / packet loss", "Traceroute", "Gaming latency check"),
                Section("Change Preview", "DNS changes require review and approval.", "Preview DNS change", "Restore DNS", "Result history")),

            ["PrivacyCenter"] = Page(
                "Review privacy controls, Windows shortcuts, browser-session warnings, and personal-folder protection.",
                "Privacy settings have not been scanned yet. Scan privacy settings to collect local evidence.",
                "Privacy output will show reviewed categories, warnings, shortcuts, and export status.",
                "Blocked: personal-folder cleanup, browser session deletion, and scary unsupported privacy claims.",
                "Privacy defaults remain manual/Windows-controlled unless a supported restore route is available.",
                "Privacy Overview",
                Actions(("primary", "Scan Privacy Settings"), ("preview", "Review Camera & Microphone Access"), ("export", "Export Privacy Report"), ("help", "Open Windows Privacy Guidance")),
                Hidden("apply", "restore"),
                Section("Windows Privacy Controls", "Privacy is shown as settings guidance, not as a hidden cleanup routine.", "Location privacy", "Camera access", "Microphone access", "Activity history"),
                Section("Sensitive Data Boundaries", "Sensitive folders and sessions are protected by default.", "Diagnostic data", "Browser sessions warning", "Personal folder protection"),
                Section("Shortcuts", "Use official Windows settings for final changes.", "Windows Privacy Settings", "Camera settings", "Microphone settings")),

            ["SecurityHealth"] = Page(
                "Collect read-only security evidence for Defender, Firewall, updates, protected apps, and blocked risky tweaks.",
                "Run security check to collect local evidence before showing a verdict.",
                "Security output will show checked status, protected apps, blocked risk categories, and health report.",
                "Blocked: disabling Defender, Firewall, anti-cheat, driver services, or permanently disabling Windows Update.",
                "Safe defaults can be reviewed only through supported guarded restore paths.",
                "Security Evidence",
                Actions(("primary", "Run Security Check"), ("preview", "Review Protected Apps"), ("restore", "Restore Safe Defaults"), ("export", "Export Health Report"), ("help", "Open Windows Security")),
                Hidden("apply"),
                Section("Security Overview", "Security status is evidence-based and read-only unless a safe route exists.", "Defender status", "Firewall status", "Windows Update security status"),
                Section("Protection Guards", "Risky performance ideas are blocked instead of exposed as tweaks.", "Protected apps", "Risky tweaks blocked", "System Reality Guard", "LCD Guard", "Defender Guard"),
                Section("Health Report", "Reports explain what was checked and what was unavailable.", "Security evidence", "Admin-required state", "Export health report")),

            ["AppsManager"] = Page(
                "Inventory installed and running apps, explain impact, then hand off uninstall to a safe workflow.",
                "Apps have not been scanned yet. Scan installed apps to load inventory.",
                "Apps output will show installed/running apps, selected app detail, impact guidance, and history.",
                "Protected system apps stay blocked and no app is removed silently.",
                "Uninstall and residual cleanup stay in dedicated confirmation-first flows.",
                "App Inventory",
                Actions(("primary", "Scan Installed Apps"), ("preview", "Refresh Running Apps"), ("apply", "Review Selected App"), ("export", "Export Apps Report"), ("help", "Open App Uninstaller")),
                Hidden("restore"),
                Section("Installed Apps List", "The app list is searchable before any action is suggested.", "Search / filter", "Publisher", "Size", "Install date"),
                Section("Running Apps", "Running apps are separated from startup and uninstall decisions.", "Running apps", "Startup apps shortcut", "App impact guidance"),
                Section("Selected App Details", "Uninstall remains a handoff, not silent removal.", "Selected app details", "Safe uninstall handoff", "App history")),

            ["TweaksCenter"] = Page(
                "Review allowlisted tweak categories, risk level, and selected tweak explanation before approval.",
                "No tweaks have been scanned yet. Scan available tweaks to load allowlisted options.",
                "Tweaks output will show categories, selected tweak explanation, preview, apply, and restore status.",
                "Unsafe tweak categories are blocked; Expert detail cannot bypass Safety Guard.",
                "Tweaks restore uses supported session metadata when approved tweaks changed state.",
                "Safe Tweaks",
                Actions(("primary", "Scan Available Tweaks"), ("preview", "Preview Safe Tweaks"), ("apply", "Apply Approved Tweaks"), ("restore", "Restore Tweaks"), ("export", "Export Tweaks Report")),
                Hidden(),
                Section("Safe Tweaks Filter", "Beginner mode starts with safe-only filters.", "Performance tweaks", "Gaming tweaks", "Network tweaks", "Visual tweaks"),
                Section("Selected Tweak Explanation", "Each tweak needs a readable explanation and risk level.", "Risk level", "Expected impact", "Blocked category notice"),
                Section("Approval", "Apply requires preview, approval, and restore metadata where supported.", "Preview tweaks", "Apply approved tweaks", "Restore tweaks")),

            ["WindowsFeatures"] = Page(
                "Review optional Windows features, categories, status, admin needs, and restart warnings.",
                "Windows optional features have not been scanned yet. Review optional features first.",
                "Feature output will show categories, status, admin/restart warning, and report export.",
                "Blocked: silent enable/disable, hidden component changes, and unsupported OS-level changes.",
                "Feature state restore depends on Windows support and explicit review.",
                "Optional Features",
                Actions(("primary", "Scan Windows Features"), ("preview", "Review Optional Features"), ("export", "Export Feature Report"), ("help", "Open Windows Optional Features")),
                Hidden("apply", "restore"),
                Section("Feature Categories", "Optional features are grouped before any recommendation.", "Development features", "Compatibility features", "Virtualization features", "Media & gaming features"),
                Section("Feature Status", "Admin and restart requirements are visible.", "Feature status", "Restart warning", "Admin warning"),
                Section("Preview", "Final changes stay in Windows UI/admin path.", "Preview feature changes", "Open optional features")),

            ["UpdateControl"] = Page(
                "Show Windows Update status, pending updates, active hours, and temporary guidance without permanent disable.",
                "Update status has not been checked yet. Check update status before any guidance.",
                "Update output will show status, active hours, pending updates, update cleanup, and export.",
                "Blocked: permanently disabling Windows Update, driver service hacks, and disabling security services.",
                "Rollback/restore guidance is available only for supported update-related sessions.",
                "Windows Update Status",
                Actions(("primary", "Check Update Status"), ("preview", "Review Active Hours"), ("export", "Export Update Report"), ("help", "Open Windows Update")),
                Hidden("apply", "restore"),
                Section("Update Status", "Update control is status and temporary guidance, not permanent disable.", "Last checked", "Pending updates", "Active hours", "Driver update visibility"),
                Section("Temporary Guidance", "Pause guidance stays reversible and visible.", "Temporary pause guidance", "Update cleanup", "Blocked permanent disable notice"),
                Section("Handoff", "Use Windows Update for final OS-managed decisions.", "Open Windows Update", "Export update report")),

            ["RepairTools"] = Page(
                "Review repair readiness, SFC/DISM modules, admin requirements, and reports before running approved repair.",
                "Repair readiness has not been scanned yet. Scan repair readiness first.",
                "Repair output will show module status, admin warning, repair report, and restore evidence.",
                "Blocked: arbitrary repair commands, hidden shell execution, and unreviewed system changes.",
                "Repair sessions record report/restore evidence where a supported repair route exists.",
                "Repair Modules",
                Actions(("primary", "Scan Repair Readiness"), ("preview", "Preview Repair Plan"), ("apply", "Run Approved Repair"), ("restore", "Restore Repair Session"), ("export", "Open Repair Report")),
                Hidden(),
                Section("Repair Dashboard", "Repair options are separated by module and admin need.", "System file repair", "SFC scan", "DISM health check", "DISM restore health"),
                Section("Windows Repair Areas", "Repair areas are preview/report oriented.", "Windows Update repair", "Network repair", "Microsoft Store / app repair", "Cache repair"),
                Section("Admin Required", "Long-running repair actions are never silent.", "Admin warning", "Time warning", "Repair report")),

            ["DriverUpdateCenter"] = Page(
                "Inventory device driver status and provide manual OEM/vendor update handoffs without silent installs.",
                "Driver status has not been scanned yet. Scan driver status to load inventory.",
                "Driver output will show device categories, guidance, backup reminder, and report export.",
                "Blocked: overclock, undervolt, BIOS edits, forced driver-service changes, and silent driver installs.",
                "Driver backup/restore remains manual or supported-route only; no service is disabled.",
                "Driver Inventory",
                Actions(("primary", "Scan Driver Status"), ("preview", "Review GPU Driver"), ("export", "Export Driver Report"), ("help", "Open OEM Driver Guidance")),
                Hidden("apply", "restore"),
                Section("Driver Overview", "Driver status is organized by device category.", "GPU driver status", "Network driver status", "Audio driver status", "Storage driver status"),
                Section("Peripherals", "USB, Bluetooth, and peripherals are shown as manual guidance.", "USB / Bluetooth", "Peripheral status", "Vendor / OEM guidance"),
                Section("Manual Update Handoff", "Updates stay outside silent automation.", "Open OEM driver page", "Backup before driver change", "Driver report")),

            ["AppUninstaller"] = Page(
                "Review installed apps, selected app detail, uninstall preview, residual cleanup, and confirmation.",
                "Installed apps have not been loaded yet. Scan installed apps before selecting an app.",
                "Uninstaller output will show selected app detail, preview, residual cleanup, and history.",
                "No silent uninstall. Protected system apps and required utilities stay blocked.",
                "Restore/report entries are visible after approved uninstall-related flows where supported.",
                "Installed Apps",
                Actions(("primary", "Scan Installed Apps"), ("preview", "Review Selected App"), ("apply", "Preview Uninstall"), ("export", "Export App Report"), ("help", "Open Windows Uninstall Settings")),
                Hidden("restore"),
                Section("Installed Apps List", "Search and filter come before any uninstall action.", "Search / filter apps", "Publisher", "Size", "Install date"),
                Section("Selected App Detail", "Selected app risk and protection are explicit.", "Protected system app warning", "Confirmation panel", "Uninstall preview"),
                Section("Residual Cleanup", "Residual cleanup stays review-only until approved.", "Residual cleanup preview", "Uninstall history", "Export app report")),

            ["RestoreBackup"] = Page(
                "Review restore readiness, applied changes, sessions, evidence, restore points, and rollback timeline.",
                "No restore session is selected yet. Open restore sessions to review available recovery evidence.",
                "Restore output will show session list, verify status, rollback preview, and export result.",
                "Restore actions require matching metadata and never fabricate rollback support.",
                "Selected rollback is disabled until a real session/evidence item is available.",
                "Restore Sessions",
                Actions(("primary", "Open Restore Sessions"), ("preview", "Verify Restore Evidence"), ("apply", "Rollback Selected Session"), ("restore", "Create Restore Point"), ("export", "Export Restore Report"), ("log", "Open Action History")),
                Hidden(),
                Section("Restore Readiness", "Restore readiness explains what can be rolled back and what needs admin support.", "Applied changes", "Restore sessions list", "Last boost restore", "Last cleanup restore"),
                Section("Evidence", "Evidence is grouped by feature so missing metadata is obvious.", "Driver evidence", "Network evidence", "Startup evidence", "Cleanup evidence"),
                Section("Rollback Timeline", "Rollback stays selected-session based.", "Create restore point", "Verify restore evidence", "Rollback selected change")),
        };

        public static PlacementPageSpec Get(string key, string title, string subtitle)
        {
            if (Pages.TryGetValue(key, out var seed))
                return FromSeed(key, seed);

            return BuildFallback(key, title, subtitle);
        }

        private static PlacementPageSpec FromSeed(string key, PageSeed seed) => new()
        {
            Key = key,
            Purpose = seed.Purpose,
            EmptyState = seed.EmptyState,
            ResultIntro = seed.ResultIntro,
            SafetyNote = seed.SafetyNote,
            RestoreNote = seed.RestoreNote,
            WorkspaceTitle = seed.WorkspaceTitle,
            Sections = seed.Sections.Select(ToSection).ToArray(),
            ActionLabels = seed.Actions,
            HiddenActionKinds = new HashSet<string>(seed.HiddenKinds.Concat(DefaultHiddenKinds), StringComparer.OrdinalIgnoreCase)
        };

        private static PlacementPageSpec BuildFallback(string key, string title, string subtitle)
        {
            var seed = BuildFallbackSeed(key, title, subtitle);
            return new PlacementPageSpec
            {
                Key = key,
                Purpose = seed.Purpose,
                EmptyState = seed.EmptyState,
                ResultIntro = seed.ResultIntro,
                SafetyNote = seed.SafetyNote,
                RestoreNote = seed.RestoreNote,
                WorkspaceTitle = seed.WorkspaceTitle,
                Sections = seed.Sections.Select(ToSection).ToArray(),
                ActionLabels = seed.Actions,
                HiddenActionKinds = new HashSet<string>(seed.HiddenKinds.Concat(DefaultHiddenKinds), StringComparer.OrdinalIgnoreCase)
            };
        }

        private static PageSeed BuildFallbackSeed(string key, string title, string subtitle)
        {
            var purpose = string.IsNullOrWhiteSpace(subtitle)
                ? "Load local evidence, inspect the feature scope, and use guarded actions only where supported."
                : subtitle;

            if (Matches(key, "Dashboard"))
                return Page(
                    "Dashboard is the PC status center: system overview, score, quick actions, AI recommendation, recent activity, and safety state in one place.",
                    "Dashboard has not refreshed local PC status yet. Refresh system overview before trusting recommendations.",
                    "Dashboard output will show current health, recent activity, safety state, and action history after refresh.",
                    "Dashboard never auto-applies tweaks. Quick actions still route through preview, approval, report, and restore gates.",
                    "Restore status is shown beside quick actions so the user sees recovery readiness before applying anything.",
                    "PC Status Center",
                    Actions(("primary", "Refresh PC Overview"), ("preview", "Review Smart Recommendations"), ("apply", "Preview Quick Action"), ("restore", "Restore Last Change"), ("export", "Export Technician Report")),
                    Hidden(),
                    Section("System Overview", "Core PC state is shown as evidence, not marketing.", "CPU", "RAM", "GPU", "Disk", "Network", "OS version", "Uptime", "Power plan"),
                    Section("HyperBoost Score", "Scores are explainable diagnostics rather than guaranteed performance gains.", "Performance score", "Gaming score", "Network score", "Startup score", "Storage score", "Safety score"),
                    Section("Recent & Safety", "Recent activity and safety blockers stay visible next to quick actions.", "Last scan", "Last boost", "Last restore", "Blocked risky action", "Backend token status"));

            if (Matches(key, "SmartScan"))
                return Page(
                    "Smart Scan is the main scanner for performance, startup, services, storage, GPU, network, LCD apps, Windows health, and gaming readiness.",
                    "Smart Scan has not run yet. Run the scan before showing fixes or AI recommendations.",
                    "Scan results will show score, problem list, risk level, safe fixes, manual-only recommendations, AI summary, and export status.",
                    "Smart Scan cannot apply fixes directly; selected fixes require preview, approval, restore metadata, and Safety Guard.",
                    "A restore point recommendation is shown before selected fixes become available.",
                    "Smart Scan Results",
                    Actions(("primary", "Run Smart Scan"), ("preview", "Preview Selected Fixes"), ("apply", "Apply Selected Safe Fixes"), ("restore", "Restore Last Scan Fix"), ("export", "Export Smart Scan Report")),
                    Hidden(),
                    Section("Performance Scan", "Reads performance, CPU/RAM, background apps, startup, services, and scheduled tasks.", "CPU/RAM pressure", "Startup impact", "Background apps", "Services", "Scheduled tasks"),
                    Section("Hardware & Gaming Scan", "Checks GPU, overlays, RGB/LCD/vendor software, driver health, and gaming readiness.", "GPU config", "Driver health", "Overlay conflict", "RGB conflict", "LCD display software CPU", "Gaming readiness"),
                    Section("Fix Review", "Results are split into safe fixes, manual-only recommendations, and blocked risky requests.", "Risk level", "Safe fix list", "Manual-only list", "AI summary", "Export report"));

            if (Matches(key, "HyperBoostScore"))
                return Page(
                    "HyperBoost Score explains overall health through performance, gaming, network, startup, storage, safety, and stability signals.",
                    "No score has been calculated yet. Calculate score after collecting current system evidence.",
                    "Score output will show category scores, reasons, warnings, and the evidence behind each recommendation.",
                    "Scores are guidance only and never promise guaranteed FPS, ping, latency, or smoothness gains.",
                    "Before/after comparison is stored only after supported scans or approved actions generate evidence.",
                    "Score Breakdown",
                    Actions(("primary", "Calculate HyperBoost Score"), ("preview", "Explain Score Factors"), ("export", "Export Score Report"), ("log", "Open Score History")),
                    Hidden("apply", "restore"),
                    Section("Score Categories", "Each score maps to a visible system signal.", "Overall", "Performance", "Gaming", "Network", "Startup", "Storage", "Safety", "Stability"),
                    Section("Evidence", "Score reasons are readable and tied to local checks.", "Bottleneck hints", "Warnings", "Recent activity", "Health score"),
                    Section("Comparison", "Before/after output is shown only when measured evidence exists.", "Last scan", "Last boost", "Export report"));

            if (Matches(key, "CpuRamOptimizer", "HyperBalance", "ProcessAnalyzer"))
                return Page(
                    "Analyze CPU/RAM pressure, top processes, memory headroom, startup impact, runaway processes, and safe balance recommendations.",
                    $"{title} has not analyzed CPU/RAM pressure yet. Refresh process and memory evidence first.",
                    $"{title} output will show CPU usage, RAM usage, cached/commit pressure, top processes, and AI explanation.",
                    "Critical system, security, anti-cheat, driver, LCD, and vendor-control processes are protected from automatic kill actions.",
                    "Process optimization is review-first; restore metadata is required where a supported change is applied.",
                    "CPU/RAM Pressure Analysis",
                    Actions(("primary", "Refresh CPU/RAM Evidence"), ("preview", "Analyze High Usage"), ("apply", "Apply Safe Memory Action"), ("restore", "Restore Process Policy"), ("export", "Export Process Report")),
                    Hidden(),
                    Section("CPU Overview", "CPU readings are contextual, not panic labels.", "CPU usage", "Thread/load overview", "Top CPU processes", "i9 8c/16t context", "11-17% idle can be normal with display tools"),
                    Section("RAM Overview", "Memory recommendations focus on pressure and runaway processes, not fake RAM-cleaner claims.", "RAM usage", "Cached memory", "Commit usage", "Memory pressure", "Runaway process detection"),
                    Section("Impact & Recommendation", "Startup/background impact and AI recommendation stay visible before action.", "Startup impact", "Background app impact", "AI recommendation", "Ignore/protect list"));

            if (Matches(key, "CpuTurboDiagnostic"))
                return Page(
                    "Diagnose CPU turbo with base/current clock, load sample validity, thermal/power hints, Windows power policy, MSI mode, and BIOS checklist.",
                    "CPU turbo has not been sampled yet. Run a valid load sample before judging boost behavior.",
                    "Turbo output will show base/current clock, load validity, suspected causes, and safe manual checklist.",
                    "BIOS, voltage, overclock, undervolt, and thermal-protection changes are never automated.",
                    "Power-policy changes require preview, approval, and restore metadata where supported.",
                    "CPU Turbo Diagnostic",
                    Actions(("primary", "Check CPU Turbo"), ("preview", "Run Load Sample"), ("apply", "Preview Power Policy Fix"), ("restore", "Restore Power Policy"), ("export", "Export CPU Report")),
                    Hidden(),
                    Section("Clock Evidence", "Low-load tests are invalid and shown as such.", "Base clock", "Current clock", "Load percent", "Turbo verdict"),
                    Section("Cause Checklist", "Potential causes are separated by Windows, vendor, thermal, and BIOS/manual areas.", "Power plan", "Processor state", "MSI Center mode", "Thermal throttling", "Power limit"),
                    Section("Manual Boundary", "HyperBoostX explains BIOS/vendor checks without touching firmware.", "BIOS checklist", "No voltage automation", "Report export"));

            if (Matches(key, "DriverRecommendation", "DriverUpdateCenter"))
                return Page(
                    "Review driver status, unknown devices, backup/restore readiness, and vendor/OEM update guidance without silent installs.",
                    "Driver inventory has not loaded yet. Scan drivers before showing update recommendations.",
                    "Driver output will show category status, backup reminder, vendor source guidance, and report export.",
                    "Auto-delete driver, forced driver update, unofficial source install, BIOS/firmware edits, and driver-service disables are blocked.",
                    "Driver backup/restore stays available for safety and is not license-locked.",
                    "Driver Health Center",
                    Actions(("primary", "Scan Drivers"), ("preview", "Generate Driver Recommendation"), ("apply", "Create Driver Backup Plan"), ("restore", "Restore Driver Backup"), ("export", "Export Driver List")),
                    Hidden(),
                    Section("Driver Summary", "Driver status is grouped by device class and evidence quality.", "GPU driver", "Audio driver", "Network driver", "Storage driver", "Unknown devices"),
                    Section("Vendor Guidance", "Recommendations point to official vendor/OEM handoff, not hidden downloads.", "NVIDIA / AMD / Intel", "OEM support page", "Windows Update status", "Manual install warning"),
                    Section("Backup & Restore", "Safety actions remain visible independent of license tier.", "Backup drivers", "Restore drivers", "Create restore point", "Export list"));

            if (Matches(key, "OverlayConflictDetector", "RgbSoftwareDetector"))
                return Page(
                    "Detect overlay, RGB, capture, launcher, and vendor-control conflicts that can affect gaming or streaming without forcing closures.",
                    $"{title} has not scanned conflicts yet. Run the detector before changing a gaming profile.",
                    $"{title} output will show detected tools, conflict level, protected state, and manual recommendation.",
                    "HyperBoostX does not disable RGB/fan/LCD/control services blindly and never modifies anti-cheat or game processes.",
                    "Conflict changes are manual or profile-scoped with restore/report evidence where supported.",
                    "Conflict Detector",
                    Actions(("primary", "Scan Conflicts"), ("preview", "Review Conflict Plan"), ("apply", "Apply Selected Safe Profile"), ("restore", "Restore Conflict Profile"), ("export", "Export Conflict Report")),
                    Hidden(),
                    Section("Detected Tools", "Detected apps are categorized before any suggestion is made.", "Discord / Steam / Game Bar", "NVIDIA / AMD overlays", "OBS / capture tools", "RGB software"),
                    Section("Conflict Risk", "Risk explains why a tool might affect games or streaming.", "Overlay conflict", "RGB polling", "Capture hook", "Protected app warning"),
                    Section("Manual Guidance", "Closure/disable remains explicit and reversible.", "Manual close", "Ignore list", "Game profile report"));

            if (Matches(key, "Reports", "ActionHistory", "PerformanceHistory", "PerformanceReport"))
                return Page(
                    "Collect before/after reports, scan history, boost results, cleanup/network/driver evidence, action log, and redacted technician export.",
                    $"{title} has no selected report yet. Open latest report or export technician report after a scan/action.",
                    $"{title} output will show report metadata, before/after evidence, redaction status, and linked restore session.",
                    "Reports must redact tokens, API keys, usernames, local profile paths, webhooks, and future license keys.",
                    "Restore links are shown only when real action/session metadata exists.",
                    "Reports & History",
                    Actions(("primary", "Open Latest Report"), ("preview", "Compare Before / After"), ("restore", "Open Linked Restore Session"), ("export", "Export Technician Report"), ("log", "Open Action Log")),
                    Hidden(),
                    Section("Report Library", "Reports are grouped by feature and generated time.", "Smart Scan", "Boost", "Cleanup", "Network", "GPU / Driver", "Repair"),
                    Section("Before / After", "Comparisons show measured local evidence only.", "Before snapshot", "After snapshot", "Failure detail", "Partial status"),
                    Section("Export", "Exports are redacted and shareable with a technician.", "JSON / Markdown", "Redaction check", "Restore metadata link"));

            if (Matches(key, "MasterTestEngine"))
                return Page(
                    "Embedded Test Center runs known v2.10 suites for Feature Coverage, Action Coverage, UI properness, backend endpoints, Safety Guard, restore, installer evidence, Discord webhook policy, and Action History.",
                    "No Test Center suite has run yet. Choose a suite or run the Owner Lab gate to create local evidence.",
                    "Test Center output will show pass/fail/blocked suites, report paths, Discord dry-run status, and redacted Action History records.",
                    "Only allowlisted suite IDs can run from the app. No arbitrary PowerShell or raw command execution is exposed.",
                    "Restore and Safety Guard suites verify evidence paths; no unsupported rollback is fabricated.",
                    "Owner Lab Test Center",
                    Actions(("primary", "Open Test Center Status"), ("preview", "Review Suite Registry"), ("apply", "Run Feature Integrity Test"), ("restore", "Cancel Running Suite"), ("export", "Open Test Center History")),
                    Hidden(),
                    Section("Overview", "Release-critical state is summarized before any suite is launched.", "Build version", "Installer timestamp", "Action map timestamp", "Backend health", "Token status"),
                    Section("Suites", "Known suite IDs are selectable and safe to invoke from the app.", "Owner Lab gate", "Public candidate gate", "Full Feature Integrity Test", "Backend Endpoint Test", "UI Properness Gate"),
                    Section("Results", "Latest suite output is kept readable before raw logs.", "Last run", "Last failed suite", "Duration", "Report path"),
                    Section("Discord", "Webhook reporter is off by default and uses redaction, rate-limit, dedupe, and no mentions.", "Discord OFF", "Dry-run test", "Send latest failures", "History"),
                    Section("Raw Logs", "Expert details are redacted and connected to Action History.", "Action History", "Suite JSON", "Markdown report"));

            if (Matches(key, "ReleaseReadiness", "FeatureAudit", "FeatureAuditMatrix"))
                return Page(
                    "Audit build, routes, actions, UI navigation, screenshots, installer/runtime, admin rollback, hardware matrix, and release documentation before any public stable claim.",
                    $"{title} has not loaded gate evidence yet. Run automated gates before release decisions.",
                    $"{title} output will show pass/fail/partial gates, blockers, generated reports, and next repair items.",
                    "Public stable is blocked unless build, route, action, UI, safety, restore, installer, runtime, docs, and hardware evidence pass.",
                    "Gate reports and rollback/admin evidence stay attached to release readiness.",
                    "Release QA",
                    Actions(("primary", "Run Gate Status"), ("preview", "Review Blockers"), ("apply", "Run Approved QA Script"), ("restore", "Open Rollback Evidence"), ("export", "Export Release Gate Report")),
                    Hidden(),
                    Section("Automated Gates", "Source gates run without manual checklist drift.", "Build", "Route audit", "Action registry", "UI navigation", "Dead button check", "Safety tests"),
                    Section("Runtime Gates", "Installer and owner/admin evidence are separate from source-only pass.", "Installed runtime", "Backend lifecycle", "Token sync", "Admin rollback", "Silent reinstall/uninstall"),
                    Section("Release Decision", "The release label follows evidence, not desire.", "Stable blockers", "Owner lab status", "Unsigned note", "Hardware matrix"));

            if (Matches(key, "PluginMarketplace"))
                return Page(
                    "Show plugin trust policy, signed package requirements, manifest boundary, permissions, uninstall path, and why arbitrary plugins remain blocked.",
                    "Plugin Trust Boundary has not loaded registry policy yet. Review trust rules before adding extensions.",
                    "Plugin output will show catalog policy, blocked install reasons, permission scope, and audit history.",
                    "Unsigned plugin install, arbitrary code execution, hidden scripts, and unsafe marketplace claims are blocked.",
                    "Plugin uninstall and safety recovery must exist before third-party distribution is allowed.",
                    "Plugin Trust Boundary",
                    Actions(("primary", "Open Plugin Policy"), ("preview", "Validate Plugin Manifest"), ("apply", "Block Unsafe Plugin Install"), ("restore", "Open Plugin Audit"), ("export", "Export Plugin Trust Report")),
                    Hidden(),
                    Section("Manifest Policy", "Future plugins need strict manifest and permission scope.", "Manifest", "Permissions", "Signature/trust", "Version"),
                    Section("Install Boundary", "Install remains blocked unless trust rules pass.", "Unsigned blocked", "Arbitrary execution blocked", "Uninstall required"),
                    Section("Audit", "Plugin activity must be reportable and reversible.", "Audit log", "Safety review", "Recovery policy"));

            if (Matches(key, "CloudSyncLicense"))
                return Page(
                    "License Center prepares activation server, device fingerprint, signed token, offline grace, device limit, and feature gating without locking safety or restore.",
                    "License status has not loaded yet. Check local license status before activation or device actions.",
                    "License output will show plan, activation state, device slots, offline grace, allowed features, and safety features that remain unlocked.",
                    "Expired or missing license cannot disable Safety Guard, Restore & Backup, rollback, critical repair, uninstall, or report export.",
                    "Activation/deactivation stays explicit; local safety and restore flows continue even when license is invalid.",
                    "License Center",
                    Actions(("primary", "Check License Status"), ("preview", "Review Device Slots"), ("apply", "Activate License Key"), ("restore", "Deactivate This Device"), ("export", "Export License Status")),
                    Hidden(),
                    Section("Activation", "The client sends license key plus device fingerprint to a server authority when production licensing is configured.", "License key", "Device fingerprint", "Signed token", "Server authority"),
                    Section("Device & Offline", "Device limits and offline grace are visible to the user.", "Device count", "Max devices", "Offline grace", "Expiry"),
                    Section("Safety Boundary", "Safety/recovery features are never paywalled.", "Safety Guard unlocked", "Restore unlocked", "Rollback unlocked", "Critical repair unlocked"));

            if (Matches(key, "AICenter", "NvidiaCopilot"))
                return Page(
                    "AI works across performance, gaming, GPU, network, driver, repair, cleanup, power, LCD, and safety as explanation, plan, and approval-gated action mapping.",
                    $"{title} has not loaded AI provider status yet. Check provider/model/router status before using AI suggestions.",
                    $"{title} output will show model registry, provider health, redaction, safety decision, and approval queue.",
                    "AI cannot run arbitrary shell commands, bypass Safety Guard, apply destructive actions, or expose saved API keys.",
                    "AI-approved actions still use the normal Action Registry, preview, approval, backup, restore, and report path.",
                    "Universal AI Layer",
                    Actions(("primary", "Check AI Status"), ("preview", "Preview AI Plan"), ("apply", "Approve Selected AI Actions"), ("restore", "Restore AI-Applied Changes"), ("export", "Export AI Audit")),
                    Hidden(),
                    Section("Model Router", "Model routing stays visible and fallback-safe.", "10 NVIDIA models", "Safety model", "Fallback model", "Provider health"),
                    Section("AI Advisors", "AI summaries appear per feature, not just as a chatbot.", "Performance", "Gaming", "GPU", "Network", "Driver", "Repair", "Cleanup", "Power", "LCD", "Safety"),
                    Section("Action Boundary", "AI plans map to official action IDs only.", "Approval queue", "Safety Guard block", "Why explanation", "No auto-apply"));

            if (Matches(key, "StreamingCenter", "AdvancedMicMixer", "WebcamStudio", "CameraTracking"))
                return Page(
                    "Check streaming/creator readiness: audio devices, mic routing, camera privacy, OBS conflicts, GPU encoder, background impact, and upload readiness.",
                    $"{title} has not scanned streaming readiness yet. Scan devices and conflicts before applying a profile.",
                    $"{title} output will show audio/camera status, conflict hints, profile preview, and export status.",
                    "HyperBoostX does not silently activate camera/microphone, edit drivers, or record in the background.",
                    "Streaming/creator profiles are reversible where a supported profile route exists.",
                    "Streaming & Creator Readiness",
                    Actions(("primary", "Scan Streaming Readiness"), ("preview", "Review Device Health"), ("apply", "Apply Selected Profile"), ("restore", "Restore Profile"), ("export", "Export Streaming Report")),
                    Hidden(),
                    Section("Audio", "Audio devices and mic tools are inspected without driver edits.", "Mic health", "Advanced mic mixer", "Voicemeeter guidance", "Audio device health"),
                    Section("Camera", "Camera controls are privacy-aware and opt-in.", "Camera device health", "Webcam diagnostics", "Camera tracking opt-in", "Privacy permission"),
                    Section("Streaming Conflicts", "Streaming readiness includes background, GPU, and network context.", "OBS conflict", "Overlay conflict", "GPU encoder hint", "Upload readiness"));

            if (Matches(key, "NetworkTools", "NetworkOptimization"))
                return Page(
                    "Expose network diagnostics, adapter info, DNS, latency, packet loss, gaming latency, and preview-only network optimization.",
                    $"{title} has not loaded network evidence yet. Run network scan before optimization.",
                    $"{title} output will show adapter, DNS, latency, packet loss, preview plan, and rollback status.",
                    "Network stack reset, permanent DNS change, and registry network tweaks require preview, backup, approval, and restore.",
                    "Prior DNS/settings are stored for rollback where supported.",
                    "Network Tools",
                    Actions(("primary", "Run Network Scan"), ("preview", "Preview Network Optimization"), ("apply", "Apply Safe Network Action"), ("restore", "Restore Network Settings"), ("export", "Export Network Report")),
                    Hidden(),
                    Section("Diagnostics", "Network evidence comes before optimization.", "Adapter info", "IP config", "Ping gateway", "Ping DNS", "Latency"),
                    Section("Reliability", "Reliability checks are separate from tweak suggestions.", "Packet loss", "DNS benchmark", "Gaming latency advisor"),
                    Section("Safe Actions", "Risky changes stay preview-first.", "Flush DNS safely", "Reset stack warning", "DNS rollback"));

            if (Matches(key, "SystemRealityGuard", "LcdPerformanceGuard"))
                return Page(
                    "Explain real background CPU causes from LCD/display apps, sensor polling, Defender scans, CPU turbo, MSI Center, and vendor utilities without panic labels.",
                    $"{title} has not run a reality scan yet. Scan before labeling CPU/background usage as a problem.",
                    $"{title} output will show LCD apps, CPU/RAM impact, supported modes, protected apps, and safe recommendations.",
                    "KANALI/TRCC/HiMOS/MSI Center/AIO LCD utilities are not force-killed by default because displays and live wallpapers may require them.",
                    "LCD/vendor changes require approval and backup/restore metadata where supported.",
                    "LCD Performance Guard",
                    Actions(("primary", "Scan LCD Apps"), ("preview", "Preview Display-Safe Mode"), ("apply", "Apply Approved LCD Profile"), ("restore", "Restore LCD Profile"), ("export", "Export LCD Report")),
                    Hidden(),
                    Section("LCD App Detection", "Detects display/live wallpaper/sensor utilities and helper processes.", "KANALI", "TRCC", "HiMOS", "MSI Center", "AIO LCD tools", "USB_LCD / USBLCDNEW", "ffmpeg / HWiNFO"),
                    Section("Safe Modes", "Modes keep displays active while reducing optional load where safe.", "Display Only", "Sensor Minimal", "Live Wallpaper Only", "Full Monitoring", "Gaming Quiet", "Creator Display"),
                    Section("Recommendations", "Recommendations focus on vendor UI settings before process changes.", "Reduce polling", "Disable overlay if unused", "Disable logging", "Do not close required display app"));

            if (Matches(key, "DefenderScanGuard", "SecurityRealityAudit"))
                return Page(
                    "Review Defender scan state, security reality, remote access, scheduled tasks, PowerShell activity, and vendor services using evidence-based classifications.",
                    $"{title} has not loaded security evidence yet. Run audit before making security claims.",
                    $"{title} output will show scan context, suspicious/manual-review items, blocked actions, and report export.",
                    "Defender disable, Firewall disable, broad exclusions, kill MsMpEng, panic deletion, and permanent Windows Update disable are blocked.",
                    "Security repair/restore is available only through supported safe defaults and review flows.",
                    "Security Reality",
                    Actions(("primary", "Run Security Reality Audit"), ("preview", "Review Risky Findings"), ("apply", "Restore Safe Defaults"), ("restore", "Open Restore Evidence"), ("export", "Export Security Audit")),
                    Hidden(),
                    Section("Evidence Classifier", "Signed Microsoft/Intel/OEM/vendor components are not panic-labeled without evidence.", "Signed components", "AppData/Temp scripts", "Remote access", "Scheduled tasks"),
                    Section("Defender Context", "High Defender CPU can be normal during a scan.", "Scan state", "CPU impact", "Exclusion advice", "Blocked disable action"),
                    Section("Review Output", "Findings are explainable and exportable.", "Manual review", "Blocked actions", "Safer alternative"));

            if (Matches(key, "AdvancedTweaks", "WindowsServices", "PowerOptimization", "VisualEffects", "MsiSafeOptimizer"))
                return Page(
                    "Advanced system controls are organized as preview-first expert tools with risk labels, backup expectations, and restore metadata.",
                    $"{title} has not loaded advanced evidence yet. Preview the scope before enabling controls.",
                    $"{title} output will show risk level, preview plan, admin requirements, and restore/report status.",
                    "Unknown registry/service tweaks, driver/security service edits, BIOS/OC/undervolt, fan/RGB/LCD service breaks, and permanent update/security disables are blocked.",
                    "Every mutating advanced action requires backup/restore metadata where supported.",
                    "Advanced System Studio",
                    Actions(("primary", "Load Advanced Status"), ("preview", "Preview Expert Plan"), ("apply", "Apply Approved Expert Action"), ("restore", "Restore Advanced Change"), ("export", "Export Expert Report")),
                    Hidden(),
                    Section("Risk Review", "Risk is visible before controls.", "Risk level", "Admin required", "Backup required", "Restore availability"),
                    Section("Domain Controls", "Controls stay scoped to this menu.", "Services", "Power plan", "Visual effects", "MSI utility state", "Expert tweak catalog"),
                    Section("Safety Guard", "Expert mode exposes more detail but never bypasses blocks.", "Blocked category", "Safer alternative", "Action history"));

            if (Matches(key, "RestorePointManager"))
                return Page(
                    "Manage restore point readiness, HyperBoostX restore sessions, backup metadata, verification, and rollback history.",
                    "Restore point status has not loaded yet. Check restore readiness before mutating actions.",
                    "Restore output will show point status, session metadata, verify status, and rollback/export options.",
                    "Restore/undo is never license-locked and missing metadata must block fake rollback claims.",
                    "Restore point creation may require admin and must report failure clearly.",
                    "Restore Point Manager",
                    Actions(("primary", "Check Restore Points"), ("preview", "Verify Backup Metadata"), ("apply", "Create Restore Point"), ("restore", "Restore Selected Point"), ("export", "Export Restore Metadata")),
                    Hidden(),
                    Section("Restore Points", "Windows restore status is shown with admin requirements.", "Restore point available", "Create restore point", "Admin status"),
                    Section("HyperBoostX Backups", "Action-specific backups are grouped by feature.", "Registry backup", "Service backup", "Startup backup", "DNS backup", "Power plan backup"),
                    Section("Integrity", "Rollback needs verified metadata.", "Verify backup integrity", "Rollback history", "Export metadata"));

            if (Matches(key, "ScheduledAutomation", "TaskRuleSystem"))
                return Page(
                    "Build scheduled scans, report jobs, and dry-run rules with explicit approval boundaries and Safety Guard policy.",
                    $"{title} has not loaded automation rules yet. Review rules in dry-run mode first.",
                    $"{title} output will show schedules, rule previews, blocked risks, and action history.",
                    "Automation cannot run dangerous unattended actions, arbitrary PowerShell, or mutating Expert actions without explicit setup.",
                    "Automated mutating actions require restore metadata and owner approval.",
                    "Automation Studio",
                    Actions(("primary", "Open Automation Rules"), ("preview", "Dry-Run Rule Preview"), ("apply", "Enable Approved Rule"), ("restore", "Disable Rule"), ("export", "Export Automation Report")),
                    Hidden(),
                    Section("Schedules", "Default automation is scan/report only.", "Smart Scan schedule", "Report export", "Maintenance reminder"),
                    Section("Rules", "Rules are readable and testable before enable.", "Trigger", "Conditions", "Allowed actions", "Blocked actions"),
                    Section("Safety", "Safety Guard remains the authority.", "Dry-run", "Approval required", "Restore metadata"));

            if (Matches(key, "UtilitiesTools", "BenchmarkLab", "KnowledgeBase", "Settings", "About"))
                return Page(
                    $"{title} provides support utilities, local configuration, diagnostics, documentation, and evidence export without hidden system changes.",
                    $"{title} has not loaded current status yet. Open status or documentation before changing settings.",
                    $"{title} output will show local settings, diagnostics, help content, or benchmark/report evidence.",
                    "Utility pages cannot execute raw scripts or hide destructive actions behind generic buttons.",
                    "Settings and support changes are local and exportable; safety/restore remain unlocked.",
                    $"{title} Support Workspace",
                    Actions(("primary", "Open Status"), ("preview", "Review Settings"), ("apply", "Save Reviewed Settings"), ("restore", "Reset Local Setting"), ("export", "Export Support Report")),
                    Hidden(),
                    Section("Local Status", "Utility state is local-first and readable.", "App settings", "Backend status", "Diagnostics", "Version info"),
                    Section("Support Tools", "Tools help inspect and export evidence.", "Knowledge base", "Benchmark report", "Local data folder", "Troubleshooting"),
                    Section("Safety", "Settings never disable safety or restore.", "Safety Guard", "Restore access", "Redacted export"));

            return Page(
                purpose,
                $"{title} has not loaded local evidence yet. Use the first action to load status for this feature.",
                $"{title} output will appear here after a backend action returns. Sensitive values are redacted in advanced detail.",
                BuildSafetyNote(key),
                BuildRestoreNote(key),
                $"{title} Workspace",
                Actions(("primary", "Open Feature Status"), ("preview", "Review Evidence Plan"), ("apply", "Apply Reviewed Selection"), ("restore", "Restore Recorded Change"), ("export", "Export Redacted Report")),
                Hidden(),
                Section("Status Evidence", "Local status, warnings, and supported actions are listed before any recommendation is trusted.", "Local status", "Warnings", "Supported actions", "Mode", "Risk level"),
                Section("Review Scope", "Reports, history, restore evidence, and handoffs stay separate from mutating flows.", "Action history", "Redacted report export", "Restore evidence where supported", "Backend route"),
                Section("Safety Boundary", "Blocked actions remain visible so Expert mode cannot bypass Safety Guard.", "Blocked risky actions", "Admin requirements", "Manual handoff when needed"));
        }

        private static bool Matches(string key, params string[] values) =>
            values.Any(value => key.Equals(value, StringComparison.OrdinalIgnoreCase) ||
                                key.Contains(value, StringComparison.OrdinalIgnoreCase));

        private static PageSeed Page(
            string purpose,
            string emptyState,
            string resultIntro,
            string safetyNote,
            string restoreNote,
            string workspaceTitle,
            Dictionary<string, string> actions,
            string[] hiddenKinds,
            params SectionSeed[] sections) =>
            new(purpose, emptyState, resultIntro, safetyNote, restoreNote, workspaceTitle, sections, actions, hiddenKinds);

        private static Dictionary<string, string> Actions(params (string Kind, string Label)[] labels) =>
            labels.ToDictionary(item => item.Kind, item => item.Label, StringComparer.OrdinalIgnoreCase);

        private static string[] Hidden(params string[] kinds) => kinds ?? Array.Empty<string>();

        private static SectionSeed Section(string title, string description, params string[] items) =>
            new(title, description, items);

        private static PlacementSectionViewModel ToSection(SectionSeed seed)
        {
            var section = new PlacementSectionViewModel
            {
                Title = seed.Title,
                Description = seed.Description
            };
            foreach (var item in seed.Items)
                section.Items.Add(item);
            return section;
        }

        private static string BuildSafetyNote(string key)
        {
            if (key.Contains("Gpu", StringComparison.OrdinalIgnoreCase) || key.Contains("Driver", StringComparison.OrdinalIgnoreCase))
                return "Blocked: overclock, undervolt, BIOS edits, forced driver-service changes, and silent driver installs.";
            if (key.Contains("Cleanup", StringComparison.OrdinalIgnoreCase) || key.Contains("Privacy", StringComparison.OrdinalIgnoreCase))
                return "Blocked: personal folders, browser sessions, game saves, project folders, and unreviewed user-file deletion.";
            if (key.Contains("Security", StringComparison.OrdinalIgnoreCase) || key.Contains("Defender", StringComparison.OrdinalIgnoreCase) || key.Contains("Update", StringComparison.OrdinalIgnoreCase))
                return "Blocked: disabling Defender, Firewall, anti-cheat, driver services, or permanently disabling Windows Update.";
            return "Safety Guard blocks anti-cheat tweaks, security-service disables, driver-service edits, BIOS/OC/undervolt actions, and protected process kills.";
        }

        private static string BuildRestoreNote(string key)
        {
            if (key.Contains("Restore", StringComparison.OrdinalIgnoreCase))
                return "Restore preview, verify, rollback, and export are grouped so recovery stays visible.";
            if (key.Contains("Cleanup", StringComparison.OrdinalIgnoreCase))
                return "Cleanup apply requires preview/report output. Personal files remain excluded from default cleanup scope.";
            return "Before mutating actions, HyperBoostX requires preview and restore metadata where the backend supports undo.";
        }
    }
}
