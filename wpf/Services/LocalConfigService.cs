using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HyperBoostX.Services
{
    public sealed class LocalConfigService
    {
        public const int CurrentConfigSchemaVersion = 2;
        public string ConfigDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HyperBoost X", "config");
        public string UiSettingsFile => Path.Combine(ConfigDirectory, "ui_settings.json");
        public void EnsureReady() => Directory.CreateDirectory(ConfigDirectory);

        public UiSettings LoadUiSettings()
        {
            EnsureReady();
            if (!File.Exists(UiSettingsFile))
            {
                var created = NormalizeSettings(new UiSettings(), null, "created_default_config");
                SaveUiSettings(created);
                return created;
            }

            try
            {
                var json = File.ReadAllText(UiSettingsFile);
                var token = JObject.Parse(json);
                var settings = token.ToObject<UiSettings>() ?? new UiSettings();
                var originalSchema = settings.ConfigSchemaVersion;
                var originalStatus = settings.LastMigrationStatus;
                var normalized = NormalizeSettings(settings, token, "loaded_config");
                if (normalized.ConfigSchemaVersion != originalSchema || normalized.LastMigrationStatus != originalStatus)
                    SaveUiSettings(normalized);
                return normalized;
            }
            catch
            {
                var backup = Path.Combine(ConfigDirectory, $"ui_settings.corrupt.{DateTime.UtcNow:yyyyMMddHHmmss}.json");
                try { File.Copy(UiSettingsFile, backup, overwrite: true); } catch { }
                var recovered = NormalizeSettings(new UiSettings(), null, "recovered_corrupt_config");
                SaveUiSettings(recovered);
                return recovered;
            }
        }

        private static UiSettings NormalizeSettings(UiSettings settings, JObject legacyToken, string status)
        {
            settings ??= new UiSettings();

            if (legacyToken != null)
            {
                if (legacyToken.TryGetValue("reduce_motion", StringComparison.OrdinalIgnoreCase, out var reduceMotion))
                    settings.ReduceMotion = reduceMotion.Value<bool>();
                if (legacyToken.TryGetValue("enable_animations", StringComparison.OrdinalIgnoreCase, out var enableAnimations))
                    settings.EnableAnimations = enableAnimations.Value<bool>();
                if (legacyToken.TryGetValue("accent", StringComparison.OrdinalIgnoreCase, out var accent))
                    settings.AccentColor = SanitizeText(accent.Value<string>(), "Cyan");
                if (legacyToken.TryGetValue("mode", StringComparison.OrdinalIgnoreCase, out var mode))
                    settings.Mode = SanitizeText(mode.Value<string>(), "Beginner");
            }

            settings.ConfigSchemaVersion = CurrentConfigSchemaVersion;
            settings.AccentColor = SanitizeText(settings.AccentColor, "Cyan");
            settings.Mode = SanitizeText(settings.Mode, "Beginner");
            settings.LastMigrationStatus = status;
            settings.MigrationHistory ??= new List<string>();
            if (!settings.MigrationHistory.Contains(status))
                settings.MigrationHistory.Add(status);
            if (!settings.MigrationHistory.Contains($"schema_v{CurrentConfigSchemaVersion}"))
                settings.MigrationHistory.Add($"schema_v{CurrentConfigSchemaVersion}");

            return settings;
        }

        private static string SanitizeText(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        public void SaveUiSettings(UiSettings settings)
        {
            EnsureReady();
            var json = JsonConvert.SerializeObject(settings ?? new UiSettings(), Formatting.Indented);
            File.WriteAllText(UiSettingsFile, json);
        }
    }

    public sealed class UiSettings
    {
        public int ConfigSchemaVersion { get; set; } = LocalConfigService.CurrentConfigSchemaVersion;
        public List<string> MigrationHistory { get; set; } = new();
        public string LastMigrationStatus { get; set; } = "created_default_config";
        public bool EnableAnimations { get; set; } = true;
        public bool ReduceMotion { get; set; }
        public string AccentColor { get; set; } = "Cyan";
        public string Mode { get; set; } = "Beginner";
    }
}
