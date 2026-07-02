using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HyperBoostX.Services;
using HyperBoostX.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HyperBoostX.Views
{
    public partial class CyberPageChrome : UserControl
    {
        private sealed class FeatureAction
        {
            public string Name { get; init; } = "Feature action";
            public string Method { get; init; } = "GET";
            public string Path { get; init; } = "/api/health";
            public object Payload { get; init; }
            public bool ConfirmationRequired { get; init; }
            public bool PreviewRequired { get; init; }
            public bool SafetyGuard { get; init; } = true;
            public bool IsDestructive { get; init; }
        }

        public CyberPageChrome()
        {
            InitializeComponent();
        }

        private async void RunPrimaryAction_Click(object sender, RoutedEventArgs e)
        {
            await RunMappedActionAsync("primary");
        }

        private async void RunPreviewAction_Click(object sender, RoutedEventArgs e)
        {
            await RunMappedActionAsync("preview");
        }

        private async void RunApplyAction_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmMutatingAction("Apply approved changes", "Only previously reviewed and approved changes should be applied. Restore metadata and Safety Guard remain required."))
                return;

            await RunMappedActionAsync("apply");
        }

        private async void RunUndoAction_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmMutatingAction("Undo or restore changes", "HyperBoostX will ask the backend for restore/undo handling. No unsupported system tweak will be forced."))
                return;

            await RunMappedActionAsync("undo");
        }

        private async void RunExportAction_Click(object sender, RoutedEventArgs e)
        {
            await RunMappedActionAsync("export");
        }

        private async void RunCatalogAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: FeatureActionViewModel catalogAction })
                return;

            if (catalogAction.ConfirmationRequired || catalogAction.IsDestructive)
            {
                var title = catalogAction.IsDestructive ? "Blocked or destructive action guard" : "Apply reviewed action";
                var message = catalogAction.IsDestructive
                    ? "This route is guarded because the requested operation can be risky. HyperBoostX will send the request only as a preview/guarded call and Safety Guard remains active."
                    : "Only continue after reviewing preview output. Restore metadata and Safety Guard remain required.";
                if (!ConfirmMutatingAction(title, message))
                    return;
            }

            await RunFeatureActionAsync(ToFeatureAction(catalogAction));
        }

        private async void RefreshBackendStatus_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not CyberPageViewModel page)
                return;

            try
            {
                page.Status = "Checking local backend...";
                SetActionButtonsEnabled(false);
                using var client = new HyperBoostBackendClient();
                var online = await client.HealthCheckAsync();
                page.Status = online
                    ? "Backend online. Mutating routes still require approval/session token."
                    : "Backend offline. UI remains safe and responsive.";
                page.LiveResultTitle = "Backend Status";
                page.LiveResult = online
                    ? $"Backend online at {client.BaseUrl}. Run the feature action to load live data."
                    : "Backend offline. Start HyperBoostX through the launcher to enable live feature flows.";
                page.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch (Exception ex)
            {
                page.Status = $"Backend check failed safely: {ex.Message}";
                page.LiveResultTitle = "Backend Status";
                page.LiveResult = BuildFriendlyError("Backend Status", ex);
                page.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            finally
            {
                SetActionButtonsEnabled(true);
            }
        }

        private async Task RunMappedActionAsync(string actionKind)
        {
            if (DataContext is not CyberPageViewModel page || page.IsBusy)
                return;

            var action = BuildFeatureAction(page, actionKind);
            await RunFeatureActionAsync(action);
        }

        private async Task RunFeatureActionAsync(FeatureAction action)
        {
            if (DataContext is not CyberPageViewModel page || page.IsBusy)
                return;

            page.IsBusy = true;
            SetActionButtonsEnabled(false);
            page.Status = $"Running {action.Name}...";
            page.LiveResultTitle = action.Name;
            page.LiveResult = $"Calling {action.Method} {action.Path}...";
            page.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            try
            {
                using var client = new HyperBoostBackendClient();
                object result = string.Equals(action.Method, "POST", StringComparison.OrdinalIgnoreCase)
                    ? await client.PostJsonRouteAsync(action.Path, action.Payload ?? new { })
                    : await client.GetJsonAsync(action.Path);

                var token = NormalizeBackendResult(result);
                page.Status = BuildStatusText(action, token);
                page.LiveResult = BuildReadableResult(action, token);
                page.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                UpdateMetricsFromResult(page, token, action);
            }
            catch (Exception ex)
            {
                page.Status = $"{action.Name} failed safely";
                page.LiveResult = $"{action.Name}\n{action.Method} {action.Path}\n\n{BuildFriendlyError(action.Name, ex)}";
                page.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            finally
            {
                page.IsBusy = false;
                SetActionButtonsEnabled(true);
            }
        }

        private static bool ConfirmMutatingAction(string title, string message)
        {
            var result = MessageBox.Show(
                $"{message}\n\nContinue only if you reviewed the preview and approve this action.",
                title,
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);
            return result == MessageBoxResult.OK;
        }

        private void SetActionButtonsEnabled(bool enabled)
        {
            foreach (var button in new[] { PrimaryActionButton, PreviewActionButton, ApplyActionButton, UndoActionButton, ExportActionButton, RefreshBackendButton })
                if (button != null)
                    button.IsEnabled = enabled;

            if (FeatureActionItems != null)
                FeatureActionItems.IsEnabled = enabled;
        }

        private static FeatureAction ToFeatureAction(FeatureActionViewModel action)
        {
            return new FeatureAction
            {
                Name = action.Label,
                Method = string.IsNullOrWhiteSpace(action.Method) ? "GET" : action.Method.ToUpperInvariant(),
                Path = string.IsNullOrWhiteSpace(action.Path) ? "/api/health" : action.Path,
                Payload = action.Payload ?? new JObject(),
                ConfirmationRequired = action.ConfirmationRequired,
                PreviewRequired = action.PreviewRequired,
                SafetyGuard = action.SafetyGuard,
                IsDestructive = action.IsDestructive,
            };
        }

        private static string BuildFriendlyError(string actionName, Exception ex)
        {
            var message = SensitiveTextRedactor.Redact(ex.Message ?? string.Empty);
            if (message.Contains("401", StringComparison.OrdinalIgnoreCase) || message.Contains("Unauthorized local session", StringComparison.OrdinalIgnoreCase))
                return $"{actionName} was rejected by the local session guard. Relaunch HyperBoostX through HyperBoostX.exe so the WPF client and backend share the same session token.";
            if (message.Contains("refused", StringComparison.OrdinalIgnoreCase) || message.Contains("No connection", StringComparison.OrdinalIgnoreCase))
                return $"{actionName} could not reach the local backend. Start HyperBoostX through the launcher, then retry.";
            if (message.Contains("400", StringComparison.OrdinalIgnoreCase))
                return $"{actionName} sent an invalid request payload. Run the preview flow or route contract test before applying changes.";
            if (message.Contains("404", StringComparison.OrdinalIgnoreCase))
                return $"{actionName} endpoint is unavailable in this build. Run backend route verification before release.";
            if (message.Contains("409", StringComparison.OrdinalIgnoreCase))
                return $"{actionName} requires preview, approval, or restore metadata. Review the result and try the preview flow first.";
            if (message.Contains("blocked", StringComparison.OrdinalIgnoreCase))
                return $"{actionName} was blocked by Safety Guard. No system change was applied. Detail: {message}";
            if (message.Contains("500", StringComparison.OrdinalIgnoreCase))
                return $"{actionName} hit a backend error. No system change was applied; export diagnostics from Feature Audit.";
            return $"{actionName} stopped safely. No system change was applied. Detail: {message}";
        }

        private static FeatureAction BuildFeatureAction(CyberPageViewModel page, string actionKind)
        {
            var pageKey = string.IsNullOrWhiteSpace(page.FeatureKey)
                ? page.GetType().Name.Replace("ViewModel", string.Empty, StringComparison.OrdinalIgnoreCase)
                : page.FeatureKey;
            var actions = new Dictionary<string, (FeatureAction Primary, FeatureAction Preview, FeatureAction Apply, FeatureAction Undo, FeatureAction Export)>(StringComparer.OrdinalIgnoreCase)
            {
                ["Dashboard"] = (
                    Post("Dashboard Smart Scan", "/api/scan/smart", new { goal = "dashboard", mode = "balanced" }),
                    Get("Dashboard Summary", "/api/dashboard/summary"),
                    Post("Dashboard Boost Preview", "/api/boost/preview", new { mode = "safe", source = "dashboard" }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Post("Dashboard Report Export", "/api/reports/export", new { format = "json" })),
                ["AIPerformanceAdvisor"] = (
                    Post("Run Smart Scan", "/api/scan/smart", new { goal = "gaming", mode = "balanced" }),
                    Post("Create Advisor Plan", "/api/advisor/plan", new { goal = "gaming", mode = "balanced" }),
                    Post("Apply Safe Boost Plan", "/api/boost/apply", new { user_approved = true, approved_action_ids = Array.Empty<string>() }),
                    Post("Undo Safe Boost Plan", "/api/boost/undo", new { }),
                    Get("Safe Advisor Actions", "/api/advisor/safe-actions")),
                ["AICenter"] = (
                    Get("AI Center Status", "/api/ai/status"),
                    Post("Create AI Safe Plan", "/api/ai/plan", new { goal = "gaming", mode = "balanced" }),
                    Post("Approve Reviewed AI Actions", "/api/ai/approve", new { user_approved = true, approved_action_ids = Array.Empty<string>() }),
                    Post("Reject AI Plan", "/api/ai/reject", new { reason = "user_requested_undo" }),
                    Get("AI Action Log", "/api/action-log")),
                ["NvidiaCopilot"] = (
                    Post("Test NVIDIA Provider", "/api/nvidia/test-connection", new { }),
                    Get("AI Provider Settings", "/api/settings"),
                    Post("NVIDIA Safety Recheck", "/api/protection/evaluate-action", new { action = "ai provider direct system change", target = "nvidia copilot" }),
                    Get("AI Center Status", "/api/ai/status"),
                    Get("Provider Action Log", "/api/action-log")),
                ["AutoGamingMode"] = (
                    Post("Preview Auto Gaming", "/api/auto-gaming/preview", new { mode = "Beginner" }),
                    Get("Auto Gaming Settings", "/api/auto-gaming/settings"),
                    Post("Enable Auto Gaming", "/api/auto-gaming/apply", new { user_approved = true, enabled = true, mode = "Beginner" }),
                    Post("Restore Auto Gaming", "/api/auto-gaming/restore", new { }),
                    Get("Restore Export", "/api/restore/export")),
                ["GameLibrary"] = (
                    Post("Scan Game Library", "/api/games/scan", new { }),
                    Get("Game Library", "/api/games/library"),
                    Post("Preview Valorant Profile", "/api/games/profile/preview", new { game_id = "valorant" }),
                    Get("Game Profile History", "/api/games/session/history"),
                    Post("Game Session Export", "/api/games/session/export", new { })),
                ["GameProfiles"] = (
                    Post("Preview Game Profile", "/api/games/profile/preview", new { game_id = "valorant" }),
                    Get("Game Library", "/api/games/library"),
                    Post("Apply Game Profile", "/api/games/profile/apply", new { game_id = "valorant", user_approved = true }),
                    Get("Game Profile History", "/api/games/session/history"),
                    Post("Game Session Export", "/api/games/session/export", new { })),
                ["GpuCenter"] = (
                    Get("GPU Vendor Guide", "/api/gpu/vendor-guide"),
                    Get("GPU Recommendations", "/api/gpu/recommendations"),
                    Get("GPU Status", "/api/gpu/status"),
                    Get("Driver Recommendation", "/api/drivers/recommendation"),
                    Post("GPU Report Export", "/api/gpu/export-report", new { })),
                ["HyperBalance"] = (
                    Get("Analyze Background Pressure", "/api/processes/background-pressure"),
                    Get("Balance Recommendations", "/api/processes/recommendations"),
                    Get("Protected Process Review", "/api/protection/processes"),
                    Get("Action Log", "/api/action-log"),
                    Post("Process Report Export", "/api/processes/export-report", new { })),
                ["OneClickBoost"] = (
                    Post("Create Safe Boost Plan", "/api/boost/plan", new { goal = "gaming", mode = "balanced" }),
                    Get("Review Approved Plan", "/api/advisor/safe-actions"),
                    Post("Apply Approved Safe Plan", "/api/boost/apply", new { user_approved = true, approved_action_ids = Array.Empty<string>() }),
                    Post("Undo Safe Boost Plan", "/api/boost/undo", new { }),
                    Post("Boost Report Export", "/api/reports/export", new { format = "json" })),
                ["ProcessAnalyzer"] = (
                    Get("Analyze Heavy Processes", "/api/processes/heavy"),
                    Get("Background Pressure", "/api/processes/background-pressure"),
                    Get("Process Recommendations", "/api/processes/recommendations"),
                    Get("Protected Process Review", "/api/protection/processes"),
                    Post("Process Report Export", "/api/processes/export-report", new { })),
                ["StartupManager"] = (
                    Get("Load Startup Items", "/api/startup/items"),
                    Post("Preview Startup Plan", "/api/startup/preview", new { items = Array.Empty<string>() }),
                    Post("Apply Startup Plan", "/api/startup/apply", new { items = Array.Empty<string>(), user_approved = true }),
                    Post("Restore Startup Changes", "/api/startup/restore", new { session_id = "" }),
                    Get("Startup Report Export", "/api/startup/export-report")),
                ["Cleanup"] = (
                    Get("Scan Cleanup", "/api/cleanup/scan"),
                    Post("Preview Cleanup", "/api/cleanup/preview", new { scope = "safe_temp_only" }),
                    Post("Apply Safe Cleanup", "/api/cleanup/apply", new { user_approved = true, scope = "safe_temp_only" }),
                    Get("Cleanup Report", "/api/cleanup/report"),
                    Get("Cleanup Report Export", "/api/cleanup/export-report")),
                ["NetworkTools"] = (
                    Get("Run Network Diagnostics", "/api/network/diagnostics"),
                    Get("DNS Test", "/api/network/dns"),
                    Post("Flush DNS", "/api/network/flush-dns", new { }),
                    Get("Network Diagnostics", "/api/network/diagnostics"),
                    Get("Network Report Export", "/api/network/export-report")),
                ["PerformanceBoost"] = (
                    Post("Create Performance Plan", "/api/boost/plan", new { goal = "performance", mode = "balanced" }),
                    Get("Background Pressure", "/api/processes/background-pressure"),
                    Post("Apply Approved Safe Plan", "/api/boost/apply", new { user_approved = true, approved_action_ids = Array.Empty<string>() }),
                    Post("Undo Safe Plan", "/api/boost/undo", new { }),
                    Post("Performance Report Export", "/api/reports/export", new { format = "json" })),
                ["BackgroundApps"] = (
                    Get("Analyze Background Apps", "/api/processes/background-pressure"),
                    Get("Heavy Process Preview", "/api/processes/heavy"),
                    Post("Evaluate Close Action", "/api/protection/evaluate-action", new { target = "browser background app", action = "review close" }),
                    Get("Protected Processes", "/api/protection/processes"),
                    Post("Process Report Export", "/api/processes/export-report", new { })),
                ["Storage"] = (
                    Get("Storage Status", "/api/storage/status"),
                    Get("Cleanup Scan", "/api/cleanup/scan"),
                    Post("Cleanup Preview", "/api/cleanup/preview", new { scope = "safe_temp_only" }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Get("Cleanup Report Export", "/api/cleanup/export-report")),
                ["GamingBooster"] = (
                    Post("Create Gaming Boost Plan", "/api/boost/plan", new { goal = "gaming", mode = "balanced" }),
                    Get("Running Game Detection", "/api/games/running"),
                    Post("Apply Approved Gaming Plan", "/api/boost/apply", new { user_approved = true, approved_action_ids = Array.Empty<string>() }),
                    Post("Undo Gaming Plan", "/api/boost/undo", new { }),
                    Post("Gaming Report Export", "/api/reports/export", new { format = "json" })),
                ["AdvancedMicMixer"] = (
                    Get("Streaming Audio Status", "/api/streaming/status"),
                    Get("Streaming Recommendations", "/api/streaming/recommendations"),
                    Post("Export Streaming Profile", "/api/streaming/export-profile", new { profile = "mic" }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Post("Streaming Profile Export", "/api/streaming/export-profile", new { profile = "mic" })),
                ["WebcamStudio"] = (
                    Get("Webcam Studio Status", "/api/streaming/status"),
                    Get("Camera Privacy Guidance", "/api/camera-tracking/status"),
                    Post("Export Webcam Profile", "/api/streaming/export-profile", new { profile = "webcam" }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Post("Webcam Report Export", "/api/streaming/export-profile", new { profile = "webcam" })),
                ["CameraTracking"] = (
                    Get("Camera Tracking Status", "/api/camera-tracking/status"),
                    Get("Camera Privacy Guidance", "/api/camera-tracking/status"),
                    Post("Camera Tracking Preview", "/api/camera-tracking/preview", new { mode = "local_opt_in" }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Post("Camera Tracking Export", "/api/streaming/export-profile", new { profile = "camera_tracking" })),
                ["NetworkBooster"] = (
                    Get("Network Diagnostics", "/api/network/diagnostics"),
                    Get("DNS and Latency Test", "/api/network/dns"),
                    Post("Flush DNS With Approval", "/api/network/flush-dns", new { user_approved = true }),
                    Get("Network Diagnostics", "/api/network/diagnostics"),
                    Get("Network Report Export", "/api/network/export-report")),
                ["DnsLatencyTools"] = (
                    Get("DNS Test", "/api/network/dns"),
                    Get("Latency Diagnostics", "/api/network/ping"),
                    Post("Flush DNS With Approval", "/api/network/flush-dns", new { user_approved = true }),
                    Get("Network Diagnostics", "/api/network/diagnostics"),
                    Get("Network Report Export", "/api/network/export-report")),
                ["NetworkOptimization"] = (
                    Get("Network Optimization Status", "/api/network/diagnostics"),
                    Get("DNS Test", "/api/network/dns"),
                    Post("Evaluate Network Reset", "/api/protection/evaluate-action", new { action = "network destructive reset", target = "network stack" }),
                    Get("Network Diagnostics", "/api/network/diagnostics"),
                    Get("Network Report Export", "/api/network/export-report")),
                ["StreamingCenter"] = (
                    Get("Streaming Status", "/api/streaming/status"),
                    Get("Streaming Recommendations", "/api/streaming/recommendations"),
                    Post("Export Streaming Profile", "/api/streaming/export-profile", new { profile = "streaming_center" }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Post("Streaming Profile Export", "/api/streaming/export-profile", new { profile = "streaming_center" })),
                ["PrivacyCenter"] = (
                    Get("Privacy Status", "/api/privacy/status"),
                    Post("Privacy Preview", "/api/privacy/preview", new { scope = "cache_only" }),
                    Post("Privacy Apply Guard", "/api/privacy/apply", new { user_approved = false }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Get("Action Log", "/api/action-log")),
                ["SecurityHealth"] = (
                    Get("Security Health", "/api/security/status"),
                    Post("Evaluate Unsafe Security Action", "/api/protection/evaluate-action", new { action = "disable defender", target = "security" }),
                    Post("Blocked Security Apply", "/api/protection/evaluate-action", new { action = "disable firewall", target = "security" }),
                    Get("Protected Processes", "/api/protection/processes"),
                    Get("Action Log", "/api/action-log")),
                ["AppsManager"] = (
                    Get("Installed Apps", "/api/apps/list"),
                    Get("App Impact", "/api/apps/impact"),
                    Post("Uninstall Preview", "/api/apps/uninstall-preview", new { app_id = "manual_selection_required" }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Get("Action Log", "/api/action-log")),
                ["AppUninstaller"] = (
                    Get("Installed Apps", "/api/apps/list"),
                    Post("Uninstall Preview", "/api/apps/uninstall-preview", new { app_id = "manual_selection_required" }),
                    Post("Uninstall Guard", "/api/apps/uninstall-preview", new { app_id = "manual_selection_required", user_approved = false }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Get("Action Log", "/api/action-log")),
                ["TweaksCenter"] = (
                    Get("Tweaks Status", "/api/system-config/tweaks"),
                    Post("Tweaks Preview", "/api/system-config/tweaks/preview", new { tweak_id = "safe_preview" }),
                    Post("Evaluate Tweak Apply", "/api/protection/evaluate-action", new { action = "apply system tweak", target = "windows" }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Get("Action Log", "/api/action-log")),
                ["AdvancedTweaks"] = (
                    Get("Advanced Tweaks Status", "/api/system-config/tweaks"),
                    Post("Advanced Tweaks Preview", "/api/system-config/tweaks/preview", new { tweak_id = "advanced_preview", mode = "expert" }),
                    Post("Blocked Advanced Tweak", "/api/protection/evaluate-action", new { action = "disable service", target = "windows service" }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Get("Action Log", "/api/action-log")),
                ["WindowsFeatures"] = (
                    Get("Windows Features", "/api/windows/features"),
                    Post("Feature Change Preview", "/api/windows/features/preview", new { feature = "manual_selection_required" }),
                    Post("Feature Apply Guard", "/api/windows/features/preview", new { feature = "manual_selection_required", user_approved = false }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Get("Action Log", "/api/action-log")),
                ["WindowsServices"] = (
                    Get("Windows Services", "/api/windows/services"),
                    Post("Service Change Preview", "/api/windows/services/preview", new { service = "manual_selection_required" }),
                    Post("Blocked Service Apply", "/api/protection/evaluate-action", new { action = "disable driver service", target = "windows service" }),
                    Get("Protected Processes", "/api/protection/processes"),
                    Get("Action Log", "/api/action-log")),
                ["UpdateControl"] = (
                    Get("Update Control Status", "/api/update-control/status"),
                    Post("Update Control Preview", "/api/update-control/preview", new { mode = "temporary_pause" }),
                    Post("Blocked Permanent Disable", "/api/protection/evaluate-action", new { action = "permanent windows update disable", target = "wuauserv" }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Get("Action Log", "/api/action-log")),
                ["RepairTools"] = (
                    Get("Repair Tool Status", "/api/repair/status"),
                    Post("Repair Preview", "/api/repair/preview", new { tool = "sfc" }),
                    Post("Repair Apply Guard", "/api/repair/preview", new { tool = "sfc", user_approved = false }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Post("Repair Report Export", "/api/reports/export", new { format = "json" })),
                ["DriverUpdateCenter"] = (
                    Get("Driver List", "/api/drivers/list"),
                    Get("Driver Recommendation", "/api/drivers/recommendation"),
                    Post("Driver Install Guard", "/api/protection/evaluate-action", new { action = "auto install driver", target = "gpu driver" }),
                    Get("GPU Status", "/api/gpu/status"),
                    Post("Driver Report Export", "/api/gpu/export-report", new { })),
                ["PowerOptimization"] = (
                    Get("Power Status", "/api/power/status"),
                    Post("Power Plan Preview", "/api/power/preview", new { plan = "balanced" }),
                    Post("Power Apply Guard", "/api/power/preview", new { plan = "balanced", user_approved = false }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Get("Action Log", "/api/action-log")),
                ["VisualEffects"] = (
                    Get("Visual Effects Status", "/api/visual-effects/status"),
                    Post("Visual Effects Preview", "/api/visual-effects/preview", new { preset = "balanced" }),
                    Post("Visual Effects Guard", "/api/visual-effects/preview", new { preset = "balanced", user_approved = false }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Get("Action Log", "/api/action-log")),
                ["RestorePointManager"] = (
                    Get("Restore Point Status", "/api/restore-points/status"),
                    Post("Restore Point Preview", "/api/restore-points/preview", new { action = "create" }),
                    Post("Restore Point Guard", "/api/restore-points/preview", new { action = "create", user_approved = false }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Get("Restore Export", "/api/restore/export")),
                ["ScheduledAutomation"] = (
                    Get("Automation Rules", "/api/automation/rules"),
                    Post("Automation Dry Run", "/api/automation/preview", new { rule = "scan_report_only" }),
                    Post("Automation Guard", "/api/automation/preview", new { rule = "safe_only", user_approved = false }),
                    Get("Action Log", "/api/action-log"),
                    Get("Action Log", "/api/action-log")),
                ["TaskRuleSystem"] = (
                    Get("Task Rules", "/api/automation/rules"),
                    Post("Rule Dry Run", "/api/automation/preview", new { rule = "dry_run" }),
                    Post("Rule Guard", "/api/automation/preview", new { rule = "safe_only", user_approved = false }),
                    Get("Action Log", "/api/action-log"),
                    Get("Action Log", "/api/action-log")),
                ["UtilitiesTools"] = (
                    Get("Utilities Status", "/api/utilities/status"),
                    Get("Product Storage", "/api/product/storage"),
                    Post("Utility Guard", "/api/protection/evaluate-action", new { action = "run raw script", target = "utility" }),
                    Get("Action Log", "/api/action-log"),
                    Get("Action Log", "/api/action-log")),
                ["MasterTestEngine"] = (
                    Get("Master Test Status", "/api/master-test/status"),
                    Get("Feature Audit Status", "/api/feature-audit/status"),
                    Post("Run Master Test Smoke", "/api/master-test/run", new { suite = "smoke" }),
                    Get("Update Check", "/api/update/check"),
                    Get("Action Log", "/api/action-log")),
                ["FeatureAuditMatrix"] = (
                    Get("Feature Audit Matrix", "/api/feature-audit/matrix"),
                    Get("Feature Audit Status", "/api/feature-audit/status"),
                    Get("Version Public Readiness", "/api/update/check"),
                    Get("Recovery Incomplete Jobs", "/api/recovery/incomplete-jobs"),
                    Get("Action Log", "/api/action-log")),
                ["BenchmarkLab"] = (
                    Get("Latest Benchmark", "/api/benchmark/latest"),
                    Get("Benchmark History", "/api/benchmark/history"),
                    Post("Save Manual Benchmark", "/api/benchmark/manual", new { game = "Manual Test", avg_fps = 0 }),
                    Get("Benchmark History", "/api/benchmark/history"),
                    Get("Benchmark Export", "/api/benchmark/export")),
                ["PerformanceHistory"] = (
                    Get("Performance Timeline", "/api/history/timeline"),
                    Get("Performance Trends", "/api/history/trends"),
                    Post("Record Scan History", "/api/history/scans", new { source = "wpf" }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Get("History Export", "/api/history/export")),
                ["PerformanceReport"] = (
                    Get("Latest Performance Report", "/api/reports/latest"),
                    Get("Compare Latest Report", "/api/history/compare"),
                    Post("Generate Report Export", "/api/reports/export", new { format = "json" }),
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Post("Report Export", "/api/reports/export", new { format = "json" })),
                ["CreatorMode"] = (
                    Get("Creator Status", "/api/creator/status"),
                    Get("Creator Recommendations", "/api/creator/recommendations"),
                    Get("Background Pressure", "/api/processes/background-pressure"),
                    Get("Streaming Status", "/api/streaming/status"),
                    Post("Creator Report Export", "/api/reports/export", new { format = "json" })),
                ["GamingEssentials"] = (
                    Get("Check Gaming Essentials", "/api/essentials/check"),
                    Get("Gaming Essentials List", "/api/essentials/list"),
                    Post("Install Preview", "/api/essentials/install-preview", new { item_id = "directx" }),
                    Get("Gaming Essentials List", "/api/essentials/list"),
                    Post("Install Preview", "/api/essentials/install-preview", new { item_id = "directx" })),
                ["RestoreBackup"] = (
                    Get("Restore Sessions", "/api/restore/sessions"),
                    Post("Restore Preview", "/api/restore/preview", new { session_id = "" }),
                    Post("Apply Restore", "/api/restore/apply", new { session_id = "" }),
                    Post("Verify Restore", "/api/restore/verify", new { session_id = "" }),
                    Get("Restore Export", "/api/restore/export")),
                ["ProtectedApps"] = (
                    Get("Protected Processes", "/api/protection/processes"),
                    Post("Evaluate Risky Action", "/api/protection/evaluate-action", new { target = "disable anti-cheat service" }),
                    Post("Reset Protected Defaults", "/api/protection/reset-defaults", new { }),
                    Get("Protected Processes", "/api/protection/processes"),
                    Get("Action Log", "/api/action-log")),
                ["KnowledgeBase"] = (
                    Get("Knowledge Topics", "/api/kb/topics"),
                    Get("Search Knowledge Base", "/api/kb/search?q=dlss"),
                    Get("DLSS Topic", "/api/kb/topic/dlss"),
                    Get("Safety Topic", "/api/kb/search?q=safety"),
                    Get("DLSS Topic", "/api/kb/topic/dlss")),
                ["Settings"] = (
                    Get("UI Settings", "/api/settings/ui"),
                    Get("Backend Settings", "/api/settings"),
                    Post("Save UI Settings Preview", "/api/settings/ui", new { mode = "Beginner", accent = "Blue", reduce_motion = false }),
                    Get("Backend Settings", "/api/settings"),
                    Get("Action Log", "/api/action-log")),
                ["FeatureAudit"] = (
                    Get("Run Feature Audit", "/api/feature-audit/run"),
                    Get("Feature Audit Status", "/api/feature-audit/status"),
                    Get("Version Public Readiness", "/api/update/check"),
                    Get("Recovery Incomplete Jobs", "/api/recovery/incomplete-jobs"),
                    Get("Action Log", "/api/action-log")),
                ["About"] = (
                    Get("Backend Version", "/api/version"),
                    Get("Backend Health", "/api/health"),
                    Get("Update Check", "/api/update/check"),
                    Get("Update Latest", "/api/update/latest"),
                    Get("Update Check", "/api/update/check")),
            };

            if (!actions.TryGetValue(pageKey, out var set))
                set = (Get("Backend Health", "/api/health"), Get("Product Storage", "/api/product/storage"), Get("Action Log", "/api/action-log"), Get("Backend Health", "/api/health"), Get("Action Log", "/api/action-log"));

            return actionKind switch
            {
                "preview" => set.Preview,
                "apply" => set.Apply,
                "undo" => set.Undo,
                "export" => set.Export,
                _ => set.Primary,
            };
        }

        private static FeatureAction Get(string name, string path) => new() { Name = name, Method = "GET", Path = path };
        private static FeatureAction Post(string name, string path, object payload) => new() { Name = name, Method = "POST", Path = path, Payload = payload };

        private static JToken NormalizeBackendResult(object result)
        {
            if (result == null)
                return JValue.CreateNull();

            if (result is JToken token)
                return token;

            return JToken.FromObject(result);
        }

        private static string BuildStatusText(FeatureAction action, JToken token)
        {
            if (token is JObject obj)
            {
                var status = obj.Value<string>("status")?.Trim();
                var safetyDetail = BuildSafetyStatusDetail(obj);

                if (string.Equals(status, "blocked", StringComparison.OrdinalIgnoreCase) || obj.Value<bool?>("blocked") == true)
                    return string.IsNullOrWhiteSpace(safetyDetail)
                        ? $"{action.Name}: blocked by Safety Guard"
                        : $"{action.Name}: blocked by Safety Guard - {safetyDetail}";
                if (obj.Value<bool?>("ok") == false)
                    return string.IsNullOrWhiteSpace(safetyDetail)
                        ? $"{action.Name}: stopped safely, review required"
                        : $"{action.Name}: stopped safely - {safetyDetail}";
                if (string.Equals(status, "preview", StringComparison.OrdinalIgnoreCase))
                    return $"{action.Name}: preview ready, approval required before apply";
                if (string.Equals(status, "partial", StringComparison.OrdinalIgnoreCase))
                    return $"{action.Name}: partial result loaded";
                if (string.Equals(status, "admin_required", StringComparison.OrdinalIgnoreCase))
                    return $"{action.Name}: admin required for this Windows action";
                if (obj.Value<bool?>("success") == false)
                    return $"{action.Name}: approval or review required";
                if (ContainsTrueFlag(obj, "requires_approval") || ContainsTrueFlag(obj, "requires_user_approval"))
                    return $"{action.Name}: preview ready, approval required before apply";
                if (obj["items"] is JArray items)
                    return $"{action.Name}: loaded {items.Count} item(s)";
                if (obj["safe_actions"] is JArray safeActions)
                    return $"{action.Name}: {safeActions.Count} safe action(s) ready";
            }

            return $"{action.Name}: complete";
        }

        private static string BuildSafetyStatusDetail(JObject obj)
        {
            if (obj["blocked_reasons"] is JArray blockedReasons && blockedReasons.Count > 0)
                return SensitiveTextRedactor.Redact(blockedReasons[0]?.ToString() ?? string.Empty);

            var message = obj.Value<string>("message");
            if (!string.IsNullOrWhiteSpace(message))
                return SensitiveTextRedactor.Redact(message);

            var reason = obj.Value<string>("reason");
            return SensitiveTextRedactor.Redact(reason ?? string.Empty);
        }

        private static bool ContainsTrueFlag(JToken token, string key)
        {
            if (token is JObject obj)
            {
                if (obj[key]?.Type == JTokenType.Boolean && obj.Value<bool>(key))
                    return true;

                return obj.Properties().Any(property => ContainsTrueFlag(property.Value, key));
            }

            if (token is JArray array)
                return array.Any(item => ContainsTrueFlag(item, key));

            return false;
        }

        private static string BuildReadableResult(FeatureAction action, JToken token)
        {
            var header = $"{action.Name}\n{action.Method} {action.Path}\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
            var summary = BuildSummary(token);
            var body = SensitiveTextRedactor.Redact(token.ToString(Formatting.Indented));
            if (body.Length > 14000)
                body = body[..14000] + "\n... output truncated in UI ...";
            return string.IsNullOrWhiteSpace(summary)
                ? $"{header}\nRaw JSON (redacted)\n{body}"
                : $"{header}\nSummary\n{summary}\n\nRaw JSON (redacted)\n{body}";
        }

        private static string BuildSummary(JToken token)
        {
            if (token is not JObject obj)
                return string.Empty;

            var lines = new List<string>();
            AddIfPresent(lines, obj, "status");
            AddIfPresent(lines, obj, "message");
            AddIfPresent(lines, obj, "disclaimer");
            AddCount(lines, obj, "items");
            AddCount(lines, obj, "safe_actions");
            AddCount(lines, obj, "blocked_risky_actions");
            AddCount(lines, obj, "recommendations");
            AddIfPresent(lines, obj, "current_version");
            AddIfPresent(lines, obj, "version");
            AddIfPresent(lines, obj, "creator_ready_score");
            AddIfPresent(lines, obj, "estimated_files");
            AddIfPresent(lines, obj, "estimated_size_mb");
            return string.Join(Environment.NewLine, lines.Take(10));
        }

        private static void AddIfPresent(ICollection<string> lines, JObject obj, string key)
        {
            var value = obj[key];
            if (value != null && value.Type != JTokenType.Object && value.Type != JTokenType.Array)
                lines.Add($"- {key}: {SensitiveTextRedactor.Redact(value.ToString())}");
        }

        private static void AddCount(ICollection<string> lines, JObject obj, string key)
        {
            if (obj[key] is JArray array)
                lines.Add($"- {key}: {array.Count}");
        }

        private static void UpdateMetricsFromResult(CyberPageViewModel page, JToken token, FeatureAction action)
        {
            if (page.Metrics.Count == 0)
                return;

            var first = page.Metrics[0];
            first.Value = "LIVE";
            first.Detail = action.Name;
            first.Score = 92;

            if (token is not JObject obj || page.Metrics.Count < 2)
                return;

            var second = page.Metrics[1];
            if (obj["items"] is JArray items)
            {
                second.Value = items.Count.ToString();
                second.Detail = "Backend items loaded";
                second.Score = Math.Clamp(60 + items.Count, 60, 100);
            }
            else if (obj["safe_actions"] is JArray safeActions)
            {
                second.Value = safeActions.Count.ToString();
                second.Detail = "Safe actions ready";
                second.Score = 95;
            }
            else if (obj.Value<int?>("creator_ready_score") is int creatorScore)
            {
                second.Value = creatorScore.ToString();
                second.Detail = "Creator readiness";
                second.Score = Math.Clamp(creatorScore, 0, 100);
            }
        }
    }
}
