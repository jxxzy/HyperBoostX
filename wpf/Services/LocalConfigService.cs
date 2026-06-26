using System;
using System.IO;
using Newtonsoft.Json;

namespace HyperBoostX.Services
{
    public sealed class LocalConfigService
    {
        public string ConfigDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HyperBoost X", "config");
        public string UiSettingsFile => Path.Combine(ConfigDirectory, "ui_settings.json");
        public void EnsureReady() => Directory.CreateDirectory(ConfigDirectory);

        public UiSettings LoadUiSettings()
        {
            EnsureReady();
            if (!File.Exists(UiSettingsFile))
                return new UiSettings();

            try
            {
                var json = File.ReadAllText(UiSettingsFile);
                return JsonConvert.DeserializeObject<UiSettings>(json) ?? new UiSettings();
            }
            catch
            {
                var backup = Path.Combine(ConfigDirectory, $"ui_settings.corrupt.{DateTime.UtcNow:yyyyMMddHHmmss}.json");
                try { File.Copy(UiSettingsFile, backup, overwrite: true); } catch { }
                return new UiSettings();
            }
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
        public bool EnableAnimations { get; set; } = true;
        public bool ReduceMotion { get; set; }
        public string AccentColor { get; set; } = "Cyan";
        public string Mode { get; set; } = "Beginner";
    }
}
