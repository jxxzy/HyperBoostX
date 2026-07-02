namespace HyperBoostX.ViewModels
{
    public sealed class PerformanceBoostViewModel : PlacementPageViewModel
    {
        public PerformanceBoostViewModel() : base("PerformanceBoost", "Performance Boost", "Scan CPU, RAM, disk, startup, and background pressure before a reversible fix plan.", "CPU and RAM pressure", "Preview the fix plan before apply.", "Use report output for before and after comparison.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Pressure", Value = "Ready", Detail = "Load local evidence first", Score = 80, Glyph = "PE" });
            Metrics.Add(new CyberMetricViewModel { Title = "Restore", Value = "Guarded", Detail = "Safety Guard active", Score = 100, Glyph = "SG" });
        }
    }

    public sealed class BackgroundAppsViewModel : PlacementPageViewModel
    {
        public BackgroundAppsViewModel() : base("BackgroundApps", "Background Apps", "Review running and background process pressure with protected-process guidance.", "Running apps", "Review high-memory apps manually before gaming.", "Browsers are treated as work/browser apps unless selected by the user.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Mode", Value = "Ready", Detail = "Load local evidence first", Score = 80, Glyph = "BA" });
            Metrics.Add(new CyberMetricViewModel { Title = "Protection", Value = "Guarded", Detail = "Safety Guard active", Score = 100, Glyph = "SG" });
        }
    }

    public sealed class StorageViewModel : PlacementPageViewModel
    {
        public StorageViewModel() : base("Storage", "Storage", "Review drive usage, disk pressure, large files, and cleanup guidance without destructive defaults.", "Drive usage", "Use storage scan before cleanup guidance.", "Duplicate cleanup remains review-only.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Storage", Value = "Ready", Detail = "Load local evidence first", Score = 80, Glyph = "ST" });
            Metrics.Add(new CyberMetricViewModel { Title = "Cleanup", Value = "Guarded", Detail = "Safety Guard active", Score = 100, Glyph = "SG" });
        }
    }

    public sealed class GamingBoosterViewModel : PlacementPageViewModel
    {
        public GamingBoosterViewModel() : base("GamingBooster", "Gaming Booster", "Build an instant gaming plan through safe boost endpoints and real game context.", "Detected game", "Do not apply a boost unless a real game is selected or detected.", "Chrome and browsers are not games by default.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Plan", Value = "Ready", Detail = "Load local evidence first", Score = 80, Glyph = "GA" });
            Metrics.Add(new CyberMetricViewModel { Title = "Undo", Value = "Guarded", Detail = "Safety Guard active", Score = 100, Glyph = "SG" });
        }
    }

    public sealed class NetworkBoosterViewModel : PlacementPageViewModel
    {
        public NetworkBoosterViewModel() : base("NetworkBooster", "Network Booster", "Run diagnostics, DNS checks, and approval-gated cache actions without fake ping claims.", "Network diagnostics", "Use DNS and latency tests before changing anything.", "No ping-lower guarantee is shown.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Network", Value = "Ready", Detail = "Load local evidence first", Score = 80, Glyph = "NE" });
            Metrics.Add(new CyberMetricViewModel { Title = "Risk", Value = "Guarded", Detail = "Safety Guard active", Score = 100, Glyph = "SG" });
        }
    }

    public sealed class DnsLatencyToolsViewModel : PlacementPageViewModel
    {
        public DnsLatencyToolsViewModel() : base("DnsLatencyTools", "DNS & Latency Tools", "Measure DNS and latency diagnostics, then export evidence-based reports.", "DNS test", "Run diagnostics to get real local results.", "DNS changes stay approval gated.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "DNS", Value = "Ready", Detail = "Load local evidence first", Score = 80, Glyph = "DN" });
            Metrics.Add(new CyberMetricViewModel { Title = "Latency", Value = "Guarded", Detail = "Safety Guard active", Score = 100, Glyph = "SG" });
        }
    }

    public sealed class PrivacyCenterViewModel : PlacementPageViewModel
    {
        public PrivacyCenterViewModel() : base("PrivacyCenter", "Privacy Center", "Review privacy controls, browser-session warnings, and personal-folder protection.", "Windows privacy controls", "Privacy cleanup is not a default performance action.", "Reports redact sensitive values.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Privacy", Value = "Ready", Detail = "Load local evidence first", Score = 80, Glyph = "PR" });
            Metrics.Add(new CyberMetricViewModel { Title = "Sessions", Value = "Guarded", Detail = "Safety Guard active", Score = 100, Glyph = "SG" });
        }
    }

    public sealed class SecurityHealthViewModel : PlacementPageViewModel
    {
        public SecurityHealthViewModel() : base("SecurityHealth", "Security & Health", "Collect read-only security evidence for Defender, Firewall, updates, protected apps, and blocked risky tweaks.", "Security overview", "HyperBoostX will not disable Defender, Firewall, or anti-cheat.", "Admin-required security states are clearly labeled.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Security", Value = "Ready", Detail = "Load local evidence first", Score = 80, Glyph = "SE" });
            Metrics.Add(new CyberMetricViewModel { Title = "Safety", Value = "Guarded", Detail = "Safety Guard active", Score = 100, Glyph = "SG" });
        }
    }

    public sealed class AppsManagerViewModel : PlacementPageViewModel
    {
        public AppsManagerViewModel() : base("AppsManager", "Apps Manager", "Inventory installed and running apps, explain impact, then hand off uninstall safely.", "Installed apps list", "Scan apps before uninstall guidance.", "Uninstall remains confirmation-first.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Apps", Value = "Ready", Detail = "Load local evidence first", Score = 80, Glyph = "AP" });
            Metrics.Add(new CyberMetricViewModel { Title = "Uninstall", Value = "Guarded", Detail = "Safety Guard active", Score = 100, Glyph = "SG" });
        }
    }

    public sealed class TweaksCenterViewModel : PlacementPageViewModel
    {
        public TweaksCenterViewModel() : base("TweaksCenter", "Tweaks Center", "Review allowlisted tweak categories, risk level, and selected tweak explanation before approval.", "Safe tweak filter", "No arbitrary shell command is exposed.", "Expert tweaks stay disabled by default.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Tweaks", Value = "Ready", Detail = "Load local evidence first", Score = 80, Glyph = "TW" });
            Metrics.Add(new CyberMetricViewModel { Title = "Apply", Value = "Guarded", Detail = "Safety Guard active", Score = 100, Glyph = "SG" });
        }
    }

    public sealed class WindowsFeaturesViewModel : PlacementPageViewModel
    {
        public WindowsFeaturesViewModel() : base("WindowsFeatures", "Windows Features", "Review optional Windows features, categories, status, admin needs, and restart warnings.", "Feature categories", "Feature changes can require restart.", "Final changes stay in Windows UI/admin path.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Features", Value = "Ready", Detail = "Load local evidence first", Score = 80, Glyph = "WI" });
            Metrics.Add(new CyberMetricViewModel { Title = "Admin", Value = "Guarded", Detail = "Safety Guard active", Score = 100, Glyph = "SG" });
        }
    }

    public sealed class UpdateControlViewModel : PlacementPageViewModel
    {
        public UpdateControlViewModel() : base("UpdateControl", "Update Control", "Show Windows Update status, pending updates, active hours, and temporary guidance without permanent disable.", "Update status", "Do not permanently disable Windows Update.", "Use Windows Update for final OS-managed decisions.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Updates", Value = "Ready", Detail = "Load local evidence first", Score = 80, Glyph = "UP" });
            Metrics.Add(new CyberMetricViewModel { Title = "Pause", Value = "Guarded", Detail = "Safety Guard active", Score = 100, Glyph = "SG" });
        }
    }

    public sealed class RepairToolsViewModel : PlacementPageViewModel
    {
        public RepairToolsViewModel() : base("RepairTools", "Repair Tools", "Review repair readiness, SFC/DISM modules, admin requirements, and reports before running approved repair.", "System file repair", "Run repair actions only after reading impact.", "Long-running repair actions are never silent.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Repair", Value = "Ready", Detail = "Load local evidence first", Score = 80, Glyph = "RE" });
            Metrics.Add(new CyberMetricViewModel { Title = "Report", Value = "Guarded", Detail = "Safety Guard active", Score = 100, Glyph = "SG" });
        }
    }

    public sealed class DriverUpdateCenterViewModel : PlacementPageViewModel
    {
        public DriverUpdateCenterViewModel() : base("DriverUpdateCenter", "Driver & Update Center", "Inventory device driver status and provide manual OEM/vendor update handoffs without silent installs.", "Driver overview", "Use vendor/OEM sources for downloads.", "HyperBoostX never disables driver services.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Driver", Value = "Ready", Detail = "Load local evidence first", Score = 80, Glyph = "DR" });
            Metrics.Add(new CyberMetricViewModel { Title = "Install", Value = "Guarded", Detail = "Safety Guard active", Score = 100, Glyph = "SG" });
        }
    }

    public sealed class AppUninstallerViewModel : PlacementPageViewModel
    {
        public AppUninstallerViewModel() : base("AppUninstaller", "App Uninstaller", "Review installed apps, selected app detail, uninstall preview, residual cleanup, and confirmation.", "Installed apps list", "Use Windows Apps Settings for final uninstall when needed.", "System apps are protected.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Mode", Value = "Ready", Detail = "Load local evidence first", Score = 80, Glyph = "AP" });
            Metrics.Add(new CyberMetricViewModel { Title = "Safety", Value = "Guarded", Detail = "Safety Guard active", Score = 100, Glyph = "SG" });
        }
    }

}
