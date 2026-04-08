using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HyperBoostX.Services
{
    public sealed class PersistedSettingsState
    {
        public string Theme { get; set; } = "Auto";
        public string Density { get; set; } = "Comfortable";
        public string LanguageMode { get; set; } = "Follow System";
        public string Language { get; set; } = "en-US";
        public string SidebarMode { get; set; } = "Full";
        public string UserMode { get; set; } = "Beginner";
        public string PerformanceLevel { get; set; } = "Balanced";
        public string RiskMode { get; set; } = "Safe mode";
        public string AutomationMode { get; set; } = "Smart Autonomous";
        public string AutomationPolicyProfile { get; set; } = "Balanced automation";
        public bool EngineEnabled { get; set; } = true;
        public bool SafetyEnabled { get; set; } = true;
        public bool MonitoringEnabled { get; set; } = true;
        public bool LearningEnabled { get; set; } = true;
        public bool AutonomousEnabled { get; set; } = true;
        public bool AutoBackupEnabled { get; set; } = true;
        public bool AutoRestorePointEnabled { get; set; } = true;
        public string LoggingLevel { get; set; } = "Advanced";
        public string UpdateChannel { get; set; } = "Stable";
        public bool BackgroundExecutionEnabled { get; set; } = true;
        public bool SilentExecutionEnabled { get; set; } = true;
        public bool DiscordWebhookEnabled { get; set; }
        public string DiscordWebhookUrl { get; set; } = "";
        public string DiscordWebhookMinimumLevel { get; set; } = "Error";
        public int DiscordWebhookCooldownSeconds { get; set; } = 120;
        public bool OpenAiEnabled { get; set; }
        public string OpenAiApiKey { get; set; } = "";
        public string OpenAiModel { get; set; } = "gpt-4.1-mini";
        public string OpenAiMode { get; set; } = "Assistant";
        public string OpenAiPermissionLevel { get; set; } = "Ask";
        public string LastOpenAiConnectionTestStatus { get; set; } = "No AI connection test run yet.";
        public bool AutoCheckAppUpdates { get; set; } = true;
        public bool AutoInstallAppUpdates { get; set; }
        public string LastKnownLatestVersion { get; set; } = "";
        public string LastKnownReleaseUrl { get; set; } = "";
        public string LastKnownReleaseChannel { get; set; } = "";
        public string LastKnownReleasePublishedUtc { get; set; } = "";
        public string LastAppUpdateSummary { get; set; } = "Update status has not been checked yet.";
        public DateTime? LastAppUpdateCheckUtc { get; set; }
        public int CpuThreshold { get; set; } = 80;
        public int RamThreshold { get; set; } = 80;
        public int DiskThreshold { get; set; } = 85;
        public int TemperatureThreshold { get; set; } = 85;
        public DateTime LastSavedUtc { get; set; } = DateTime.UtcNow;
    }

    public sealed class AutomationRuleDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "Unnamed Rule";
        public string Goal { get; set; } = "Keep PC Fast";
        public string Scenario { get; set; } = "Idle Maintenance";
        public string TriggerType { get; set; } = "idle";
        public string ActionType { get; set; } = "cleanup_light";
        public string SafeLevel { get; set; } = "Safe";
        public bool Enabled { get; set; } = true;
        public bool RequiresIdle { get; set; }
        public int MaxCpuPercent { get; set; } = 70;
        public int MaxRamPercent { get; set; } = 85;
        public int MaxDiskPercent { get; set; } = 90;
        public int MaxTemperatureC { get; set; } = 85;
        public int MinimumMinutesBetweenRuns { get; set; } = 20;
        public DateTime? LastRunUtc { get; set; }
    }

    public sealed class AutomationTaskRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string RuleId { get; set; } = "";
        public string Name { get; set; } = "Unnamed Task";
        public string Status { get; set; } = "Queued";
        public string TriggerReason { get; set; } = "";
        public string ResultSummary { get; set; } = "";
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ScheduledForUtc { get; set; }
        public DateTime? LastTriedUtc { get; set; }
        public DateTime? CompletedUtc { get; set; }
        public int RetryCount { get; set; }
    }

    public sealed class AutomationAuditEntry
    {
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string Level { get; set; } = "Info";
        public string Message { get; set; } = "";
        public string Source { get; set; } = "Automation";
    }

    public sealed class PersistedAutomationState
    {
        public string Goal { get; set; } = "Keep PC Fast";
        public string Mode { get; set; } = "Smart Autonomous";
        public string PolicyProfile { get; set; } = "Balanced automation";
        public bool Enabled { get; set; } = true;
        public bool LearningEnabled { get; set; } = true;
        public bool Paused { get; set; }
        public int MaxConcurrentTasks { get; set; } = 2;
        public int RetryLimit { get; set; } = 2;
        public int EvaluationIntervalSeconds { get; set; } = 15;
        public int IdleCpuThreshold { get; set; } = 15;
        public int HighRamThreshold { get; set; } = 80;
        public int LowStorageThreshold { get; set; } = 85;
        public int HighTemperatureThreshold { get; set; } = 85;
        public List<AutomationRuleDefinition> Rules { get; set; } = new();
        public List<AutomationTaskRecord> Tasks { get; set; } = new();
        public List<AutomationAuditEntry> AuditTrail { get; set; } = new();
    }

    public sealed class PersistedAiState
    {
        public string LastPrompt { get; set; } = "";
        public string LastIntent { get; set; } = "general_help";
        public double LastConfidence { get; set; } = 0.5;
        public string LastReply { get; set; } = "";
        public string LastContext { get; set; } = "";
        public string LastReasoningSummary { get; set; } = "";
        public string LastAutomationSummary { get; set; } = "";
        public string LastWhySummary { get; set; } = "";
        public List<string> LastSafeActions { get; set; } = new();
        public List<string> MemoryEntries { get; set; } = new();
        public int TotalRequests { get; set; }
        public int ApprovedPlans { get; set; }
        public int RejectedPlans { get; set; }
        public int CreatedAutomations { get; set; }
        public string PreferredScenario { get; set; } = "General Assistance";
        public string PreferredAction { get; set; } = "scan_only";
        public string PreferredRiskStyle { get; set; } = "Ask";
        public string LastOutcomeSummary { get; set; } = "No AI outcome recorded yet.";
        public Dictionary<string, int> IntentCounters { get; set; } = new();
        public Dictionary<string, int> ActionCounters { get; set; } = new();
    }

    public sealed class PersistedAppConfig
    {
        public PersistedSettingsState Settings { get; set; } = new();
        public PersistedAutomationState Automation { get; set; } = new();
        public PersistedAiState Ai { get; set; } = new();
    }

    public sealed class AppConfigService
    {
        private readonly string _configDirectory;
        private readonly string _configPath;

        public AppConfigService() : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HyperBoost X",
            "config"))
        {
        }

        public AppConfigService(string configDirectory)
        {
            _configDirectory = string.IsNullOrWhiteSpace(configDirectory)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "HyperBoost X",
                    "config")
                : configDirectory;
            _configPath = Path.Combine(_configDirectory, "app-state.json");
        }

        public async Task<PersistedAppConfig> LoadAsync()
        {
            try
            {
                if (!File.Exists(_configPath))
                    return new PersistedAppConfig();

                var json = await File.ReadAllTextAsync(_configPath);
                return JsonConvert.DeserializeObject<PersistedAppConfig>(json) ?? new PersistedAppConfig();
            }
            catch
            {
                return new PersistedAppConfig();
            }
        }

        public async Task SaveAsync(PersistedAppConfig config)
        {
            Directory.CreateDirectory(_configDirectory);
            config.Settings.LastSavedUtc = DateTime.UtcNow;
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            await File.WriteAllTextAsync(_configPath, json);
        }

        public string GetConfigPath()
        {
            Directory.CreateDirectory(_configDirectory);
            return _configPath;
        }
    }
}
