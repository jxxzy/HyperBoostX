using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace HyperBoostX.ViewModels
{
    public sealed class PlacementSectionViewModel
    {
        public string Title { get; init; } = "Section";
        public string Description { get; init; } = "";
        public ObservableCollection<string> Items { get; } = new();
    }

    public sealed class PlacementPageSpec
    {
        public string Key { get; init; } = "Dashboard";
        public string Purpose { get; init; } = "Review the current feature status, run a safe preview, then inspect report and restore evidence.";
        public string EmptyState { get; init; } = "No live result yet. Run the preview or refresh action to load local backend data.";
        public string ResultIntro { get; init; } = "No action has run yet. Results will appear here after the local backend returns data.";
        public string SafetyNote { get; init; } = "Safety Guard remains active in every mode. Preview, confirmation, restore metadata, and reporting stay required for mutating flows.";
        public string RestoreNote { get; init; } = "Restore evidence is kept close to apply/history actions so changes remain reversible where the backend supports undo.";
        public IReadOnlyList<PlacementSectionViewModel> Sections { get; init; } = Array.Empty<PlacementSectionViewModel>();
        public IReadOnlyDictionary<string, string> ActionLabels { get; init; } = new Dictionary<string, string>();
    }

    public class PlacementPageViewModel : CyberPageViewModel
    {
        private string _resultSummary = "No action has run yet. Use preview or refresh before applying anything.";

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
        public string ResultSummary { get => _resultSummary; set => SetProperty(ref _resultSummary, value); }
        public ObservableCollection<PlacementSectionViewModel> PlacementSections { get; } = new();
        public ObservableCollection<FeatureActionViewModel> PrimaryPlacementActions { get; } = new();
        public ObservableCollection<FeatureActionViewModel> SecondaryPlacementActions { get; } = new();
        public ObservableCollection<FeatureActionViewModel> RestorePlacementActions { get; } = new();
        public ObservableCollection<string> AdvancedRouteLines { get; } = new();

        private void ConfigurePlacement(string featureKey)
        {
            var spec = PlacementPageCatalog.Get(featureKey, Title, Subtitle);
            Purpose = spec.Purpose;
            EmptyState = spec.EmptyState;
            SafetyNote = spec.SafetyNote;
            RestoreNote = spec.RestoreNote;
            ResultSummary = spec.ResultIntro;
            LiveResultTitle = "Advanced Details";
            LiveResult = "Raw backend payload is shown only here after an action runs. Beginner flow uses the summary above.";

            PlacementSections.Clear();
            foreach (var section in spec.Sections)
                PlacementSections.Add(section);

            PrimaryPlacementActions.Clear();
            SecondaryPlacementActions.Clear();
            RestorePlacementActions.Clear();
            AdvancedRouteLines.Clear();

            foreach (var action in FeatureActions.Select(action => CloneAction(action, spec)))
            {
                AdvancedRouteLines.Add($"{action.Method} {action.Path} - {action.Label}");
                var kind = GetActionKind(action);
                if (kind == "restore" || action.Restore && (kind == "undo" || action.Path.Contains("restore", StringComparison.OrdinalIgnoreCase)))
                    RestorePlacementActions.Add(action);
                else if (kind is "primary" or "preview" or "apply")
                    PrimaryPlacementActions.Add(action);
                else
                    SecondaryPlacementActions.Add(action);
            }

            if (PrimaryPlacementActions.Count == 0 && FeatureActions.Count > 0)
                PrimaryPlacementActions.Add(CloneAction(FeatureActions[0], spec));

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

        private static string BuildActionLabel(FeatureActionViewModel action, PlacementPageSpec spec)
        {
            var kind = GetActionKind(action);
            if (spec.ActionLabels.TryGetValue(kind, out var label))
                return label;

            if (kind == "apply")
                return "Apply reviewed selection";
            if (kind == "restore")
                return "Open restore evidence";
            if (kind == "refresh")
                return "Refresh backend status";
            if (kind == "log")
                return "Open action history";
            if (kind == "audit")
                return "Open audit status";
            if (kind == "help")
                return "Open safety help";

            return action.Label;
        }

        private static string GetActionKind(FeatureActionViewModel action)
        {
            var id = action.Id ?? string.Empty;
            var parts = id.Split('.');
            if (parts.Length >= 2)
                return parts[^2].Equals(action.MenuKey, StringComparison.OrdinalIgnoreCase) ? parts[^1] : parts[^2];

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
        private static readonly IReadOnlyDictionary<string, string[]> SectionMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["OneClickBoost"] = new[] { "Scan first, create a safe boost plan, approve selected actions only.", "Before/after report and undo evidence stay visible before release.", "No AI-generated shell command is executed." },
            ["AIPerformanceAdvisor"] = new[] { "Smart recommendation reads local pressure and returns explainable guidance.", "Beginner copy avoids jargon; advanced payload stays in Advanced Details.", "Recommendations are guidance, not guaranteed FPS claims." },
            ["AutoGamingMode"] = new[] { "Detects gaming context and prepares a reversible mode profile.", "Browser and work apps are not treated as games by default.", "Apply requires confirmation and restore metadata." },
            ["StartupManager"] = new[] { "Startup entries are listed before any change.", "Beginner flow uses review-only recommendations.", "Restore sessions are shown near apply history." },
            ["BackgroundApps"] = new[] { "Background pressure is read-only by default.", "System, security, driver, anti-cheat, and vendor processes are protected.", "Close/kill guidance is never automatic." },
            ["ProcessAnalyzer"] = new[] { "Heavy-process analysis is separated from protected-process decisions.", "High usage is reported with evidence instead of panic labels.", "Expert detail still cannot bypass Safety Guard." },
            ["Cleanup"] = new[] { "Cleanup starts with scan and preview only.", "Documents, Downloads, Desktop, projects, game saves, and media are excluded.", "Delete/apply actions require approval and report output." },
            ["Storage"] = new[] { "Storage status is read before cleanup guidance.", "Duplicate/personal file cleanup remains review-only.", "Cleanup report export stays available." },
            ["GpuCenter"] = new[] { "GPU detection shows vendor/model/driver status when backend data exists.", "Driver guidance uses official/OEM handoff; no silent install.", "No overclock, undervolt, BIOS, or driver-service disable is exposed." },
            ["GamingBooster"] = new[] { "Gaming plan is previewed before apply.", "Game detection must be real; browsers are excluded from game assumptions.", "Undo and report actions remain one step away." },
            ["GameLibrary"] = new[] { "Game scan and empty-library states are explicit.", "Profiles are not applied without a selected game.", "Session history supports before/after comparison." },
            ["GameProfiles"] = new[] { "Profiles are previewed with game-specific context.", "Apply stays confirmation-gated.", "History and export show what changed." },
            ["StreamingCenter"] = new[] { "Streaming mode separates OBS/Discord/TikTok guidance from gaming tweaks.", "Audio/camera settings are handed off safely.", "No hidden capture or driver edit is performed." },
            ["CreatorMode"] = new[] { "Creator readiness focuses on background pressure and streaming status.", "Recommendations are safe and reversible.", "Raw telemetry stays behind Advanced Details." },
            ["NetworkBooster"] = new[] { "Diagnostics and DNS/latency checks come before any network action.", "No fake ping-lower guarantee is shown.", "Reset/flush actions require confirmation and human-friendly failures." },
            ["DnsLatencyTools"] = new[] { "DNS tests report measured local results.", "DNS changes require approval.", "Latency guidance remains evidence-based." },
            ["NetworkTools"] = new[] { "Network diagnostics, DNS tests, and export are separated.", "Risky reset flows stay guarded.", "404/401/500 backend states are shown as safe failures." },
            ["PrivacyCenter"] = new[] { "Privacy actions are not performance defaults.", "Cookies, sessions, personal folders, and browser profiles need explicit warning.", "Reports redact sensitive values." },
            ["SecurityHealth"] = new[] { "Security state is read-only guidance.", "Defender, Firewall, anti-cheat, and update services are protected.", "Admin-required states are labeled clearly." },
            ["AppsManager"] = new[] { "Installed/running apps are inventoried before uninstall guidance.", "Protected system apps stay blocked.", "Uninstall is preview/confirmation first." },
            ["AppUninstaller"] = new[] { "Uninstall uses preview and owner confirmation.", "System apps and protected utilities are not silently removed.", "History/report entries remain visible." },
            ["TweaksCenter"] = new[] { "Tweaks are allowlisted and previewed.", "Unsafe tweak categories are blocked.", "Expert mode exposes detail without bypassing Safety Guard." },
            ["WindowsFeatures"] = new[] { "Optional features are reviewed before any OS-level change.", "Admin and restart needs are visible.", "Silent enable/disable is not exposed." },
            ["UpdateControl"] = new[] { "Update status is shown without permanent disable claims.", "Temporary guidance stays reversible.", "Permanent Windows Update disable is blocked." },
            ["RepairTools"] = new[] { "Repair actions show admin/time warnings.", "SFC/DISM style flows are preview/report oriented.", "No arbitrary repair command is generated by AI." },
            ["DriverUpdateCenter"] = new[] { "Driver list and recommendations are manual guidance.", "No fabricated latest-version or silent installer claim.", "Driver services remain protected." },
            ["RestoreBackup"] = new[] { "Restore sessions, preview, apply, verify, and export stay together.", "Restore metadata is required before mutating actions.", "Failure states explain what is missing." },
            ["FeatureAudit"] = new[] { "Feature audit is read-only.", "Release, route, docs, and safety status are visible.", "Audit result must not mutate Windows state." },
            ["About"] = new[] { "Version, health, update, and support identity stay factual.", "No marketing performance guarantee is shown.", "Local backend details are visible in Advanced Details." }
        };

        private static readonly IReadOnlyDictionary<string, string> PurposeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["OneClickBoost"] = "Create a safe, preview-first boost plan and keep undo/report evidence visible.",
            ["GpuCenter"] = "Detect GPU and driver context, then provide safe vendor guidance without hardware-risk automation.",
            ["Cleanup"] = "Preview safe cleanup scope before any delete-like action and protect personal files.",
            ["StartupManager"] = "Review startup pressure, apply selected changes only, and keep restore history close.",
            ["NetworkBooster"] = "Run real diagnostics first, then expose guarded network actions with honest latency language.",
            ["RestoreBackup"] = "Make restore, verify, and export actions obvious before and after mutating flows."
        };

        public static PlacementPageSpec Get(string key, string title, string subtitle)
        {
            var items = SectionMap.TryGetValue(key, out var mapped)
                ? mapped
                : new[] { subtitle, "Run preview or refresh to load real local backend data.", "Apply actions remain guarded and restore-aware where applicable." };

            var section = new PlacementSectionViewModel
            {
                Title = "Feature Placement",
                Description = "Content follows the v1.3 order: status, feature sections, primary action, result/history, advanced details, restore and safety."
            };
            foreach (var item in items)
                section.Items.Add(item);

            return new PlacementPageSpec
            {
                Key = key,
                Purpose = PurposeMap.TryGetValue(key, out var purpose)
                    ? purpose
                    : $"Use {title} through the stable v2.10 flow: inspect status, preview the action, review output, then use guarded apply/restore only when available.",
                EmptyState = $"No {title} result yet. Use the first action to load real local backend data.",
                ResultIntro = $"Waiting for {title} output. The summary will stay readable, and raw JSON stays in Advanced Details.",
                SafetyNote = BuildSafetyNote(key),
                RestoreNote = BuildRestoreNote(key),
                Sections = new[] { section },
                ActionLabels = BuildActionLabels(title, key)
            };
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
                return "Restore preview, apply, verify, and export are grouped here so the recovery path is always visible.";
            if (key.Contains("Cleanup", StringComparison.OrdinalIgnoreCase))
                return "Cleanup apply requires preview/report output. Personal files remain excluded from restore-dependent cleanup scope.";
            return "Before mutating actions, HyperBoostX requires preview and restore metadata where the backend supports undo.";
        }

        private static IReadOnlyDictionary<string, string> BuildActionLabels(string title, string key)
        {
            var noun = title.Replace("&", "and", StringComparison.OrdinalIgnoreCase);
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["primary"] = key switch
                {
                    "GpuCenter" => "Refresh GPU status",
                    "Cleanup" => "Scan cleanup scope",
                    "NetworkBooster" => "Run network diagnostics",
                    "DnsLatencyTools" => "Test DNS and latency",
                    "StartupManager" => "Load startup items",
                    "RestoreBackup" => "Open restore sessions",
                    _ => $"Run {noun}"
                },
                ["preview"] = key switch
                {
                    "Cleanup" => "Preview safe cleanup",
                    "OneClickBoost" => "Review safe actions",
                    "GpuCenter" => "Review GPU guidance",
                    _ => $"Preview {noun}"
                },
                ["apply"] = key switch
                {
                    "Cleanup" => "Apply safe cleanup",
                    "OneClickBoost" => "Apply approved boost",
                    "StartupManager" => "Apply selected startup changes",
                    "RestoreBackup" => "Apply selected restore",
                    _ => "Apply reviewed selection"
                },
                ["restore"] = key switch
                {
                    "RestoreBackup" => "Verify restore evidence",
                    _ => "Open restore evidence"
                },
                ["export"] = $"Export {noun} report"
            };
        }
    }
}
