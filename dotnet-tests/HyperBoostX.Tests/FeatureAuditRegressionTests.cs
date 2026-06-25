using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using HyperBoostX.Services;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HyperBoostX.Tests;

public class FeatureAuditRegressionTests
{
    [Fact]
    public async Task Critical_feature_audit_suites_complete_without_failures()
    {
        var result = await RunOnStaThreadAsync(async () =>
        {
            Application? app = null;
            try
            {
                app = new Application();
                var window = new HyperBoostX.MainWindow(new FeatureAuditBackendClientStub());

                await InvokePrivateTaskAsync(window, "RunTestingSuiteAsync", "Integration");
                var integrationFailures = GetFailureCount(window);

                await InvokePrivateTaskAsync(window, "RunTestingSuiteAsync", "UI Flow");
                var uiFlowFailures = GetFailureCount(window);

                await InvokePrivateTaskAsync(window, "RunTestingSuiteAsync", "Performance");
                var performanceFailures = GetFailureCount(window);

                return (integrationFailures, uiFlowFailures, performanceFailures);
            }
            finally
            {
                app?.Shutdown();
            }
        });

        Assert.True(
            result.integrationFailures == 0 && result.uiFlowFailures == 0 && result.performanceFailures == 0,
            $"Expected no failures, got Integration={result.integrationFailures}, UI Flow={result.uiFlowFailures}, Performance={result.performanceFailures}.");
    }

    private sealed class FeatureAuditBackendClientStub : IHyperBoostBackendClient
    {
        public Task<bool> HealthCheckAsync() => Task.FromResult(true);

        public Task<dynamic> GetSystemInfoAsync() => Json(new JObject
        {
            ["os"] = "Windows test host",
            ["cpu"] = "Test CPU",
            ["memory_gb"] = 16,
            ["disk"] = "SSD"
        });

        public Task<dynamic> GetSystemStatsAsync() => Json(new JObject
        {
            ["cpu"] = 12,
            ["cpu_percent"] = 12,
            ["memory"] = 38,
            ["memory_percent"] = 38,
            ["disk"] = 42,
            ["disk_percent"] = 42,
            ["process_count"] = 64
        });

        public Task<dynamic> GetTweaksAsync() => Json(new JObject { ["tweaks"] = new JArray() });

        public Task<dynamic> ApplyTweakAsync(string tweakId, bool expertMode = false, bool confirmed = false) =>
            Json(SuccessPayload("tweak", tweakId));

        public Task<dynamic> GetBoosterProfilesAsync() => Json(new JObject { ["profiles"] = new JArray() });

        public Task<dynamic> ApplyBoosterAsync(string profile) => Json(SuccessPayload("profile", profile));

        public Task<dynamic> GetDriversAsync() => Json(new JObject { ["drivers"] = new JArray() });

        public Task<dynamic> CheckDriverUpdatesAsync() => Json(new JObject { ["updates"] = new JArray() });

        public Task<dynamic> RunSfcAsync() => Json(SuccessPayload("action", "sfc"));

        public Task<dynamic> CleanupAsync(string scope = "") => Json(new JObject
        {
            ["success"] = true,
            ["scope"] = scope ?? "safe",
            ["freed_bytes"] = 0
        });

        public Task<dynamic> RunDismAsync() => Json(SuccessPayload("action", "dism"));

        public Task<dynamic> GetStartupItemsAsync() => Json(new JObject
        {
            ["startup_items"] = new JArray(),
            ["items"] = new JArray()
        });

        public Task<dynamic> GetProcessesAsync() => Json(new JObject
        {
            ["processes"] = new JArray
            {
                new JObject
                {
                    ["name"] = "explorer.exe",
                    ["memory_mb"] = 128
                }
            }
        });

        public Task<dynamic> TestDnsAsync() => Json(new JObject { ["success"] = true, ["latency_ms"] = 12 });

        public Task<dynamic> FlushDnsAsync() => Json(SuccessPayload("action", "flush_dns"));

        public Task<dynamic> OptimizeTcpAsync() => Json(SuccessPayload("action", "optimize_tcp"));

        public Task<dynamic> ResetNetworkAsync() => Json(SuccessPayload("action", "reset_network"));

        public Task<dynamic> RunTripleAiFlowAsync(string userGoal = "gaming", string game = "") => Json(new JObject
        {
            ["assistant"] = new JObject
            {
                ["message"] = "Triple AI test summary",
                ["risk_level"] = "Low"
            },
            ["analysis"] = new JObject
            {
                ["issues"] = new JArray(),
                ["recommendations"] = new JArray()
            },
            ["safety"] = new JObject
            {
                ["approved"] = new JArray(),
                ["warnings"] = new JArray(),
                ["blocked"] = new JArray()
            },
            ["report"] = new JObject
            {
                ["pc_health_score"] = 92,
                ["gaming_readiness_score"] = 88
            }
        });

        public Task<dynamic> ApplyTripleAiTweaksAsync(dynamic approvedTweaks, bool userApproved) =>
            Json(new JObject { ["success"] = userApproved });

        public Task<dynamic> RevertTripleAiTweaksAsync(string backupId, object tweakIds) =>
            Json(new JObject { ["success"] = true, ["backup_id"] = backupId ?? "" });

        private static JObject SuccessPayload(string key, string value) => new()
        {
            ["success"] = true,
            [key] = value
        };

        private static Task<dynamic> Json(JToken token) => Task.FromResult<dynamic>(token);
    }

    private static async Task InvokePrivateTaskAsync(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var task = method!.Invoke(instance, args) as Task;
        Assert.NotNull(task);
        await task!;
    }

    private static int GetFailureCount(object window)
    {
        var field = window.GetType().GetField("_lastFeatureAuditResults", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        var results = field!.GetValue(window) as System.Collections.IEnumerable;
        Assert.NotNull(results);

        var failures = 0;
        foreach (var item in results!)
        {
            var successProperty = item.GetType().GetProperty("Success");
            if (successProperty?.GetValue(item) is bool success && !success)
                failures++;
        }

        return failures;
    }

    private static Task<T> RunOnStaThreadAsync<T>(Func<Task<T>> callback)
    {
        var tcs = new TaskCompletionSource<T>();

        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));

            _ = RunCallbackAsync();
            Dispatcher.Run();

            async Task RunCallbackAsync()
            {
                try
                {
                    var result = await callback();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
                finally
                {
                    dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                }
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return tcs.Task;
    }
}
