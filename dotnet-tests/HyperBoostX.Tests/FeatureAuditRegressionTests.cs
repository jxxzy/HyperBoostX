using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
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
                var window = new HyperBoostX.MainWindow();

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
