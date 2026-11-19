using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace HyperBoostX.ViewModels
{
    public static class FeatureActionCatalog
    {
        private const string CatalogRelativePath = "Data/ui_action_map_v2_10.json";
        private static readonly object Gate = new();
        private static Dictionary<string, List<FeatureActionViewModel>> _cache;

        public static IReadOnlyList<FeatureActionViewModel> LoadFor(string featureKey)
        {
            var key = string.IsNullOrWhiteSpace(featureKey) ? "Dashboard" : featureKey.Trim();
            if (!FeatureVisibilityService.IsVisible(key))
                return Array.Empty<FeatureActionViewModel>();

            var all = LoadAll();
            if (all.TryGetValue(key, out var actions))
                return Clone(FilterForCurrentMode(actions));

            if (all.TryGetValue("Default", out var fallback))
                return Clone(FilterForCurrentMode(fallback));

            return Array.Empty<FeatureActionViewModel>();
        }

        public static IReadOnlyDictionary<string, IReadOnlyList<FeatureActionViewModel>> LoadVisibleForCurrentMode()
        {
            var visible = new Dictionary<string, IReadOnlyList<FeatureActionViewModel>>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in LoadAll())
            {
                if (!FeatureVisibilityService.IsVisible(pair.Key))
                    continue;

                var actions = Clone(FilterForCurrentMode(pair.Value));
                if (actions.Count > 0)
                    visible[pair.Key] = actions;
            }

            return visible;
        }

        private static Dictionary<string, List<FeatureActionViewModel>> LoadAll()
        {
            lock (Gate)
            {
                if (_cache != null)
                    return _cache;

                _cache = TryLoadCatalog() ?? BuildFallbackCatalog();
                return _cache;
            }
        }

        private static Dictionary<string, List<FeatureActionViewModel>> TryLoadCatalog()
        {
            foreach (var path in CandidatePaths())
            {
                try
                {
                    if (!File.Exists(path))
                        continue;

                    var root = JObject.Parse(File.ReadAllText(path));
                    var menus = root["menus"] as JArray;
                    if (menus == null)
                        continue;

                    var result = new Dictionary<string, List<FeatureActionViewModel>>(StringComparer.OrdinalIgnoreCase);
                    foreach (var menu in menus.OfType<JObject>())
                    {
                        var key = menu.Value<string>("key");
                        var actions = menu["actions"] as JArray;
                        if (string.IsNullOrWhiteSpace(key) || actions == null)
                            continue;

                        result[key] = actions
                            .OfType<JObject>()
                            .Select(action => ToAction(key, action))
                            .Where(action => !string.IsNullOrWhiteSpace(action.Path))
                            .ToList();
                    }

                    if (result.Count > 0)
                        return result;
                }
                catch
                {
                    // Keep UI bootable even if the action catalog is missing or malformed.
                }
            }

            return null;
        }

        private static IEnumerable<string> CandidatePaths()
        {
            yield return Path.Combine(AppContext.BaseDirectory, CatalogRelativePath);

            var current = new DirectoryInfo(AppContext.BaseDirectory);
            for (var i = 0; current != null && i < 6; i++, current = current.Parent)
                yield return Path.Combine(current.FullName, "wpf", CatalogRelativePath);
        }

        private static FeatureActionViewModel ToAction(string menuKey, JObject action)
        {
            return new FeatureActionViewModel
            {
                Id = action.Value<string>("id") ?? $"{menuKey}.action",
                MenuKey = menuKey,
                Label = action.Value<string>("label") ?? "Run action",
                Command = action.Value<string>("command") ?? "RunCatalogActionCommand",
                Method = (action.Value<string>("method") ?? "GET").ToUpperInvariant(),
                Path = action.Value<string>("path") ?? "/api/health",
                Payload = action["payload"] as JObject,
                RequiresAdmin = action.Value<bool?>("requires_admin") ?? false,
                PreviewRequired = action.Value<bool?>("preview_required") ?? true,
                ConfirmationRequired = action.Value<bool?>("confirmation_required") ?? false,
                SafetyGuard = action.Value<bool?>("safety_guard") ?? true,
                Restore = action.Value<bool?>("restore") ?? true,
                IsDestructive = action.Value<bool?>("is_destructive") ?? false,
                Partial = action.Value<bool?>("partial") ?? false,
                Status = action.Value<string>("status") ?? "Real",
                TestCoverage = action.Value<string>("test_coverage") ?? "tests/test_ui_action_map_v210.py",
                Tooltip = action.Value<string>("tooltip") ?? "Preview-first local backend action",
                SuccessState = action.Value<string>("success_state") ?? "Success state updates Live Result",
                ErrorState = action.Value<string>("error_state") ?? "Failure is rendered as a safe human-friendly message",
                LoadingState = action.Value<string>("loading_state") ?? "Buttons disabled while backend call is running",
            };
        }

        private static IEnumerable<FeatureActionViewModel> FilterForCurrentMode(IEnumerable<FeatureActionViewModel> actions)
        {
            if (!FeatureVisibilityService.IsStableMode)
                return actions;

            return actions.Where(action =>
                !action.Partial &&
                string.Equals(action.Status, "Real", StringComparison.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<FeatureActionViewModel> Clone(IEnumerable<FeatureActionViewModel> actions)
        {
            return actions.Select(action => new FeatureActionViewModel
            {
                Id = action.Id,
                MenuKey = action.MenuKey,
                Label = action.Label,
                Command = action.Command,
                Method = action.Method,
                Path = action.Path,
                Payload = action.Payload == null ? null : (JObject)action.Payload.DeepClone(),
                RequiresAdmin = action.RequiresAdmin,
                PreviewRequired = action.PreviewRequired,
                ConfirmationRequired = action.ConfirmationRequired,
                SafetyGuard = action.SafetyGuard,
                Restore = action.Restore,
                IsDestructive = action.IsDestructive,
                Partial = action.Partial,
                Status = action.Status,
                TestCoverage = action.TestCoverage,
                Tooltip = action.Tooltip,
                SuccessState = action.SuccessState,
                ErrorState = action.ErrorState,
                LoadingState = action.LoadingState,
                IsEnabled = action.IsEnabled,
            }).ToList();
        }

        private static Dictionary<string, List<FeatureActionViewModel>> BuildFallbackCatalog()
        {
            return new Dictionary<string, List<FeatureActionViewModel>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Default"] = new()
                {
                    new FeatureActionViewModel { Id = "default.health", Label = "Backend Health", Command = "FallbackHealthCommand", Method = "GET", Path = "/api/health", Restore = false, PreviewRequired = false },
                    new FeatureActionViewModel { Id = "default.readiness", Label = "Release Readiness", Command = "FallbackReadinessCommand", Method = "GET", Path = "/api/release/readiness", Restore = false, PreviewRequired = false },
                    new FeatureActionViewModel { Id = "default.audit", Label = "Feature Audit", Command = "FallbackAuditCommand", Method = "GET", Path = "/api/feature-audit/status", Restore = false, PreviewRequired = false },
                    new FeatureActionViewModel { Id = "default.log", Label = "Action Log", Command = "FallbackActionLogCommand", Method = "GET", Path = "/api/action-log", Restore = false, PreviewRequired = false },
                    new FeatureActionViewModel { Id = "default.restore", Label = "Restore Sessions", Command = "FallbackRestoreCommand", Method = "GET", Path = "/api/restore/sessions", Restore = true, PreviewRequired = false },
                    new FeatureActionViewModel { Id = "default.report", Label = "Latest Report", Command = "FallbackReportCommand", Method = "GET", Path = "/api/reports/latest", Restore = false, PreviewRequired = false },
                },
            };
        }
    }
}
