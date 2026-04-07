using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HyperBoostX.Services
{
    public enum LocalizationMode
    {
        FollowSystem,
        ManualSelection,
        AutoSmartDetection
    }

    public sealed class LanguagePackInfo
    {
        public string LocaleCode { get; init; } = "en-US";
        public string NativeName { get; init; } = "English";
        public string EnglishName { get; init; } = "English";
        public double CoveragePercent { get; init; }
        public int MissingKeysCount { get; init; }
        public int OutdatedTranslationCount { get; init; }
        public DateTime LastUpdatedUtc { get; init; }
    }

    public sealed class LocalizationSettings
    {
        public string CurrentLanguage { get; set; } = "en-US";
        public string FallbackLanguage { get; set; } = "en-US";
        public string SystemLanguage { get; set; } = "en-US";
        public string RegionLocale { get; set; } = "en-US";
        public LocalizationMode Mode { get; set; } = LocalizationMode.FollowSystem;
        public bool FollowSystemUntilUserOverrides { get; set; } = true;
        public DateTime LastLanguageUpdateUtc { get; set; } = DateTime.UtcNow;
    }

    public sealed class LocalizationService
    {
        private const string DefaultLocale = "en-US";
        private readonly string _localizationRoot;
        private readonly string _configDirectory;
        private readonly string _configPath;
        private readonly Dictionary<string, string> _currentTranslations = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _fallbackTranslations = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _missingKeys = new(StringComparer.OrdinalIgnoreCase);

        public LocalizationSettings Settings { get; private set; } = new();
        public string CurrentLocale => Settings.CurrentLanguage;
        public string FallbackLocale => Settings.FallbackLanguage;
        public string SystemLocale => Settings.SystemLanguage;
        public string ActiveLanguagePack => ResolveLocale(CurrentLocale);
        public IReadOnlyCollection<string> MissingKeys => _missingKeys;

        public LocalizationService()
        {
            _localizationRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "localization");
            _configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HyperBoost X",
                "config");
            _configPath = Path.Combine(_configDirectory, "localization-settings.json");
        }

        public async Task InitializeAsync()
        {
            Directory.CreateDirectory(_configDirectory);
            Settings = await LoadSettingsAsync();
            Settings.SystemLanguage = DetectSystemLocale();
            Settings.RegionLocale = DetectRegionLocale();
            Settings.CurrentLanguage = ResolveStartupLocale(Settings);
            Settings.FallbackLanguage = ResolveLocale(Settings.FallbackLanguage);
            Settings.LastLanguageUpdateUtc = DateTime.UtcNow;
            await SaveSettingsAsync();
            await ReloadAsync();
        }

        public async Task SetModeAsync(LocalizationMode mode, string localeOverride = null)
        {
            Settings.Mode = mode;
            if (!string.IsNullOrWhiteSpace(localeOverride))
            {
                Settings.CurrentLanguage = ResolveLocale(localeOverride);
                Settings.FollowSystemUntilUserOverrides = false;
            }
            else if (mode == LocalizationMode.FollowSystem || mode == LocalizationMode.AutoSmartDetection)
            {
                Settings.CurrentLanguage = ResolveLocale(Settings.SystemLanguage);
                Settings.FollowSystemUntilUserOverrides = mode != LocalizationMode.ManualSelection;
            }

            Settings.LastLanguageUpdateUtc = DateTime.UtcNow;
            await SaveSettingsAsync();
            await ReloadAsync();
        }

        public async Task SetManualLanguageAsync(string locale)
        {
            Settings.Mode = LocalizationMode.ManualSelection;
            Settings.CurrentLanguage = ResolveLocale(locale);
            Settings.FollowSystemUntilUserOverrides = false;
            Settings.LastLanguageUpdateUtc = DateTime.UtcNow;
            await SaveSettingsAsync();
            await ReloadAsync();
        }

        public async Task SetFallbackLanguageAsync(string locale)
        {
            Settings.FallbackLanguage = ResolveLocale(locale);
            Settings.LastLanguageUpdateUtc = DateTime.UtcNow;
            await SaveSettingsAsync();
            await ReloadAsync();
        }

        public async Task ReloadAsync()
        {
            _currentTranslations.Clear();
            _fallbackTranslations.Clear();
            _missingKeys.Clear();

            foreach (var pair in LoadLocaleDictionary(ResolveLocale(Settings.FallbackLanguage)))
            {
                _fallbackTranslations[pair.Key] = pair.Value;
            }

            foreach (var pair in LoadLocaleDictionary(ResolveLocale(Settings.CurrentLanguage)))
            {
                _currentTranslations[pair.Key] = pair.Value;
            }

            ApplyCulture();
            await Task.CompletedTask;
        }

        public void ApplyCulture()
        {
            var culture = SafeCulture(ResolveLocale(Settings.CurrentLanguage));
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        public string T(string key, string fallback = null, IDictionary<string, object> variables = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                return fallback ?? string.Empty;

            string value = null;
            if (!_currentTranslations.TryGetValue(key, out value))
            {
                if (!_fallbackTranslations.TryGetValue(key, out value))
                {
                    _missingKeys.Add(key);
                    value = fallback ?? key;
                }
            }

            if (variables == null || variables.Count == 0)
                return value;

            return Regex.Replace(value, "\\{(?<name>[a-zA-Z0-9_]+)\\}", match =>
            {
                var name = match.Groups["name"].Value;
                return variables.TryGetValue(name, out var variableValue)
                    ? Convert.ToString(variableValue, CultureInfo.CurrentCulture) ?? string.Empty
                    : match.Value;
            });
        }

        public string FormatPlural(string keyPrefix, int count, string fallbackSingular, string fallbackPlural)
        {
            var key = count == 1 ? $"{keyPrefix}.one" : $"{keyPrefix}.other";
            var fallback = count == 1 ? fallbackSingular : fallbackPlural;
            return T(key, fallback, new Dictionary<string, object> { ["count"] = count });
        }

        public string FormatDateTime(DateTime value)
        {
            return value.ToString("g", CultureInfo.CurrentCulture);
        }

        public string FormatNumber(double value, string format = "N1")
        {
            return value.ToString(format, CultureInfo.CurrentCulture);
        }

        public string FormatStorageMb(double value)
        {
            return value.ToString("N1", CultureInfo.CurrentCulture) + " MB";
        }

        public IEnumerable<LanguagePackInfo> GetAvailableLanguagePacks()
        {
            var locales = GetAvailableLocales().Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var fallbackCount = LoadLocaleDictionary(DefaultLocale).Count;

            foreach (var locale in locales)
            {
                var resolvedLocale = ResolveLocale(locale);
                var translations = LoadLocaleDictionary(resolvedLocale);
                var culture = SafeCulture(resolvedLocale);
                var lastUpdated = GetLastWriteTimeUtc(resolvedLocale);
                var missingCount = Math.Max(0, fallbackCount - translations.Count);
                var coverage = fallbackCount == 0 ? 100 : Math.Round((translations.Count / (double)fallbackCount) * 100, 1);

                yield return new LanguagePackInfo
                {
                    LocaleCode = resolvedLocale,
                    NativeName = culture.NativeName,
                    EnglishName = culture.EnglishName,
                    CoveragePercent = coverage,
                    MissingKeysCount = missingCount,
                    OutdatedTranslationCount = 0,
                    LastUpdatedUtc = lastUpdated
                };
            }
        }

        public string BuildCoverageSummary()
        {
            var currentPack = GetAvailableLanguagePacks()
                .FirstOrDefault(pack => pack.LocaleCode.Equals(CurrentLocale, StringComparison.OrdinalIgnoreCase));

            if (currentPack == null)
                return "Language pack metadata unavailable.";

            return
                $"Current App Language: {currentPack.NativeName} ({currentPack.LocaleCode}){Environment.NewLine}" +
                $"Detected System Language: {SystemLocale}{Environment.NewLine}" +
                $"Region / Locale: {Settings.RegionLocale}{Environment.NewLine}" +
                $"Active Language Pack: {ActiveLanguagePack}{Environment.NewLine}" +
                $"Translation Coverage: {currentPack.CoveragePercent:0.#}% ({currentPack.MissingKeysCount} missing keys){Environment.NewLine}" +
                $"Last Language Update: {FormatDateTime(currentPack.LastUpdatedUtc.ToLocalTime())}{Environment.NewLine}" +
                $"Language Mode: {Settings.Mode}";
        }

        public async Task<string> ExportMissingKeysReportAsync()
        {
            Directory.CreateDirectory(_configDirectory);
            var path = Path.Combine(_configDirectory, $"localization-missing-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            var lines = new List<string>
            {
                $"Current language: {CurrentLocale}",
                $"Fallback language: {FallbackLocale}",
                $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                string.Empty,
                "Missing keys:"
            };

            lines.AddRange(_missingKeys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase));
            await File.WriteAllLinesAsync(path, lines);
            return path;
        }

        public string GetLocalizationRoot() => _localizationRoot;

        private async Task<LocalizationSettings> LoadSettingsAsync()
        {
            try
            {
                if (!File.Exists(_configPath))
                    return new LocalizationSettings();

                var json = await File.ReadAllTextAsync(_configPath);
                return JsonConvert.DeserializeObject<LocalizationSettings>(json) ?? new LocalizationSettings();
            }
            catch
            {
                return new LocalizationSettings();
            }
        }

        private async Task SaveSettingsAsync()
        {
            Directory.CreateDirectory(_configDirectory);
            var json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            await File.WriteAllTextAsync(_configPath, json);
        }

        private string ResolveStartupLocale(LocalizationSettings settings)
        {
            return settings.Mode switch
            {
                LocalizationMode.ManualSelection when !string.IsNullOrWhiteSpace(settings.CurrentLanguage) =>
                    ResolveLocale(settings.CurrentLanguage),
                LocalizationMode.AutoSmartDetection => ResolveLocale(settings.SystemLanguage),
                LocalizationMode.FollowSystem => ResolveLocale(settings.SystemLanguage),
                _ when settings.FollowSystemUntilUserOverrides => ResolveLocale(settings.SystemLanguage),
                _ => ResolveLocale(settings.CurrentLanguage)
            };
        }

        private string DetectSystemLocale()
        {
            try
            {
                return CultureInfo.InstalledUICulture.Name;
            }
            catch
            {
                return DefaultLocale;
            }
        }

        private string DetectRegionLocale()
        {
            try
            {
                return CultureInfo.CurrentCulture.Name;
            }
            catch
            {
                return DefaultLocale;
            }
        }

        private string ResolveLocale(string locale)
        {
            var available = GetAvailableLocales().ToList();
            if (!available.Any())
                return DefaultLocale;

            if (string.IsNullOrWhiteSpace(locale))
                return available.Contains(DefaultLocale, StringComparer.OrdinalIgnoreCase) ? DefaultLocale : available[0];

            var exact = available.FirstOrDefault(item => item.Equals(locale, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(exact))
                return exact;

            var language = locale.Split('-')[0];
            var baseMatch = available.FirstOrDefault(item => item.StartsWith(language + "-", StringComparison.OrdinalIgnoreCase) || item.Equals(language, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(baseMatch))
                return baseMatch;

            return available.Contains(DefaultLocale, StringComparer.OrdinalIgnoreCase) ? DefaultLocale : available[0];
        }

        private IEnumerable<string> GetAvailableLocales()
        {
            if (!Directory.Exists(_localizationRoot))
                return new[] { DefaultLocale };

            return Directory.GetDirectories(_localizationRoot)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
        }

        private Dictionary<string, string> LoadLocaleDictionary(string locale)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var localePath = Path.Combine(_localizationRoot, locale);

            if (!Directory.Exists(localePath))
                return result;

            foreach (var file in Directory.GetFiles(localePath, "*.json", SearchOption.TopDirectoryOnly).OrderBy(path => path))
            {
                try
                {
                    var text = File.ReadAllText(file);
                    var json = JsonConvert.DeserializeObject<JObject>(text);
                    if (json == null)
                        continue;

                    FlattenJson(json, result, null);
                }
                catch
                {
                    // Keep localization engine resilient to broken packs.
                }
            }

            return result;
        }

        private static void FlattenJson(JToken token, IDictionary<string, string> output, string prefix)
        {
            if (token is JObject obj)
            {
                foreach (var property in obj.Properties())
                {
                    var key = string.IsNullOrWhiteSpace(prefix) ? property.Name : $"{prefix}.{property.Name}";
                    FlattenJson(property.Value, output, key);
                }

                return;
            }

            if (token is JValue value)
            {
                output[prefix ?? string.Empty] = Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
        }

        private DateTime GetLastWriteTimeUtc(string locale)
        {
            try
            {
                var localePath = Path.Combine(_localizationRoot, locale);
                var files = Directory.GetFiles(localePath, "*.json", SearchOption.TopDirectoryOnly);
                return files.Length == 0
                    ? DateTime.UtcNow
                    : files.Max(File.GetLastWriteTimeUtc);
            }
            catch
            {
                return DateTime.UtcNow;
            }
        }

        private static CultureInfo SafeCulture(string locale)
        {
            try
            {
                return CultureInfo.GetCultureInfo(locale);
            }
            catch
            {
                return CultureInfo.GetCultureInfo(DefaultLocale);
            }
        }
    }
}
