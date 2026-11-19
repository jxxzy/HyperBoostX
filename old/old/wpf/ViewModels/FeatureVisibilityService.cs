using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace HyperBoostX.ViewModels
{
    public enum HyperBoostAppMode
    {
        Stable,
        Dev,
    }

    public sealed class FeatureVisibilitySnapshot
    {
        public HyperBoostAppMode Mode { get; init; } = HyperBoostAppMode.Stable;
        public bool ShowExperimental { get; init; }
        public bool RequireRealFeatures { get; init; } = true;
        public bool BlockNonRealStableUi { get; init; } = true;
        public int TotalFeatures { get; init; }
        public int StableVisibleFeatures { get; init; }
        public int HiddenFromStable { get; init; }
        public int NonRealVisibleInStable { get; init; }
        public IReadOnlyDictionary<string, string> StatusByFeature { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyList<string> StableVisibleKeys { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> HiddenStableKeys { get; init; } = Array.Empty<string>();
    }

    public static class FeatureVisibilityService
    {
        private const string CatalogRelativePath = "Data/ui_action_map_v2_10.json";
        private static readonly object Gate = new();
        private static FeatureVisibilitySnapshot _snapshot;

        public static FeatureVisibilitySnapshot Current
        {
            get
            {
                lock (Gate)
                {
                    return _snapshot ??= BuildSnapshot();
                }
            }
        }

        public static void ResetForTests()
        {
            lock (Gate)
            {
                _snapshot = null;
            }
        }

        public static bool IsStableMode => Current.Mode == HyperBoostAppMode.Stable;
        public static string ModeLabel => IsStableMode ? "Stable" : "Dev";

        public static bool IsVisible(string featureKey)
        {
            if (string.IsNullOrWhiteSpace(featureKey))
                return true;

            var snapshot = Current;
            if (snapshot.Mode == HyperBoostAppMode.Dev || snapshot.ShowExperimental)
                return true;

            return snapshot.StableVisibleKeys.Contains(featureKey, StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsRealFeature(string featureKey)
        {
            if (string.IsNullOrWhiteSpace(featureKey))
                return true;

            return !Current.StatusByFeature.TryGetValue(featureKey, out var status) ||
                   string.Equals(status, "Real", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetStatus(string featureKey)
        {
            return Current.StatusByFeature.TryGetValue(featureKey ?? "", out var status) ? status : "Real";
        }

        private static FeatureVisibilitySnapshot BuildSnapshot()
        {
            var mode = ResolveMode();
            var showExperimental = ResolveBool("HYPERBOOSTX_SHOW_EXPERIMENTAL", false) || mode == HyperBoostAppMode.Dev;
            var requireReal = ResolveBool("HYPERBOOSTX_REQUIRE_REAL_FEATURES", true);
            var blockNonReal = ResolveBool("HYPERBOOSTX_BLOCK_NON_REAL_STABLE_UI", true);
            var statusByFeature = LoadFeatureStatuses();

            var stableVisible = statusByFeature
                .Where(pair => string.Equals(pair.Value, "Real", StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair.Key)
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var hidden = statusByFeature
                .Where(pair => !string.Equals(pair.Value, "Real", StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair.Key)
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new FeatureVisibilitySnapshot
            {
                Mode = mode,
                ShowExperimental = showExperimental,
                RequireRealFeatures = requireReal,
                BlockNonRealStableUi = blockNonReal,
                TotalFeatures = statusByFeature.Count,
                StableVisibleFeatures = stableVisible.Count,
                HiddenFromStable = hidden.Count,
                NonRealVisibleInStable = mode == HyperBoostAppMode.Stable && blockNonReal ? 0 : hidden.Count,
                StatusByFeature = statusByFeature,
                StableVisibleKeys = stableVisible,
                HiddenStableKeys = hidden,
            };
        }

        private static HyperBoostAppMode ResolveMode()
        {
            var raw = Environment.GetEnvironmentVariable("HYPERBOOSTX_MODE");
            if (string.Equals(raw, "dev", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(raw, "development", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(raw, "internal", StringComparison.OrdinalIgnoreCase))
                return HyperBoostAppMode.Dev;

            return HyperBoostAppMode.Stable;
        }

        private static bool ResolveBool(string name, bool fallback)
        {
            var raw = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            return raw.Trim().Equals("1", StringComparison.OrdinalIgnoreCase) ||
                   raw.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   raw.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, string> LoadFeatureStatuses()
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

                    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var menu in menus.OfType<JObject>())
                    {
                        var key = menu.Value<string>("key");
                        if (string.IsNullOrWhiteSpace(key) || string.Equals(key, "Default", StringComparison.OrdinalIgnoreCase))
                            continue;

                        result[key] = menu.Value<string>("status") ?? "Real";
                    }

                    if (result.Count > 0)
                        return result;
                }
                catch
                {
                    // Stable UI must remain bootable even if the action-map file is malformed.
                }
            }

            return BuildFallbackStatuses();
        }

        private static IEnumerable<string> CandidatePaths()
        {
            yield return Path.Combine(AppContext.BaseDirectory, CatalogRelativePath);

            var current = new DirectoryInfo(AppContext.BaseDirectory);
            for (var i = 0; current != null && i < 6; i++, current = current.Parent)
                yield return Path.Combine(current.FullName, "wpf", CatalogRelativePath);
        }

        private static Dictionary<string, string> BuildFallbackStatuses()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Dashboard"] = "Real",
                ["OneClickBoost"] = "Real",
                ["SmartScan"] = "Real",
                ["AIPerformanceAdvisor"] = "Real",
                ["AutoGamingMode"] = "Real",
                ["PerformanceBoost"] = "Real",
                ["StartupManager"] = "Real",
                ["Cleanup"] = "Real",
                ["Storage"] = "Real",
                ["GpuCenter"] = "Real",
                ["NetworkTools"] = "Real",
                ["RestoreBackup"] = "Real",
                ["Reports"] = "Real",
                ["Settings"] = "Real",
                ["About"] = "Real",
            };
        }
    }
}
