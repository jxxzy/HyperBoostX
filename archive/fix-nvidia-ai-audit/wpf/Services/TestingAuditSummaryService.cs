using System;

namespace HyperBoostX.Services
{
    public sealed class TestingCompatibilityContext
    {
        public string OsVersion { get; set; } = "";
        public bool Is64BitOperatingSystem { get; set; }
        public string CultureName { get; set; } = "";
        public string UiCultureName { get; set; } = "";
        public bool IsAdministrator { get; set; }
        public bool BackendHealthy { get; set; }
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }
    }

    public static class TestingAuditSummaryService
    {
        public static string BuildStrategySummary(string executionMode)
        {
            return
                $"Mode: {executionMode}{Environment.NewLine}" +
                "Testing strategy:" + Environment.NewLine +
                "- Mock mode for logic-only validation" + Environment.NewLine +
                "- Safe read-only for runtime smoke / integration checks" + Environment.NewLine +
                "- Live read-only for OS-aware checks without destructive apply";
        }

        public static string BuildLayerSummary()
        {
            return
                "Layer 1 - Core Logic: parser, score engine, validator, profile builder" + Environment.NewLine +
                "Layer 2 - System Action Layer: backend/API contract and safe system probes" + Environment.NewLine +
                "Layer 3 - App Service Layer: orchestration, config, automation, AI" + Environment.NewLine +
                "Layer 4 - UI Layer: refresh path, state binding, navigation flow" + Environment.NewLine +
                "Layer 5 - Full E2E: guided sequence and persistence validation";
        }

        public static string BuildSuiteMatrixText(string lastTestingSuite)
        {
            return
                "Available suites:" + Environment.NewLine +
                "- Unit" + Environment.NewLine +
                "- Integration" + Environment.NewLine +
                "- UI Flow" + Environment.NewLine +
                "- End-to-End" + Environment.NewLine +
                "- Regression" + Environment.NewLine +
                "- Performance" + Environment.NewLine +
                "- Stress" + Environment.NewLine +
                "- Stability" + Environment.NewLine +
                "- Security" + Environment.NewLine +
                "- Compatibility" + Environment.NewLine +
                $"Last suite: {lastTestingSuite}";
        }

        public static string BuildCompatibilitySummary(TestingCompatibilityContext context)
        {
            return
                $"OS: {context.OsVersion}{Environment.NewLine}" +
                $"Architecture: {(context.Is64BitOperatingSystem ? "64-bit OS" : "32-bit OS")}{Environment.NewLine}" +
                $"Culture: {context.CultureName}{Environment.NewLine}" +
                $"UI Culture: {context.UiCultureName}{Environment.NewLine}" +
                $"Admin mode: {(context.IsAdministrator ? "Yes" : "No")}{Environment.NewLine}" +
                $"Backend health: {(context.BackendHealthy ? "Healthy" : "Offline / degraded")}{Environment.NewLine}" +
                $"Window profile: {context.WindowWidth:0}x{context.WindowHeight:0}";
        }
    }
}
