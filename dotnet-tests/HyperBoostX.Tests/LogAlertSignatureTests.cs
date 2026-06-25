using System.Reflection;
using HyperBoostX;
using Xunit;

namespace HyperBoostX.Tests;

public class LogAlertSignatureTests
{
    [Fact]
    public void BuildLogAlertSignature_NormalizesStructuredLogTimestamp()
    {
        var first = BuildLogAlertSignature(
            "hyperboost.log",
            "warning",
            "2026-05-30 02:47:28,186 - utils.shell - WARNING - Admin command blocked without elevation: powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
        var second = BuildLogAlertSignature(
            "hyperboost.log",
            "warning",
            "2026-05-30 02:48:06,935 - utils.shell - WARNING - Admin command blocked without elevation: powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e");

        Assert.Equal(first, second);
        Assert.Contains("utils.shell|WARNING|Admin command blocked without elevation", first);
    }

    [Fact]
    public void BuildLogAlertSignature_NormalizesWpfLogTimestamp()
    {
        var first = BuildLogAlertSignature(
            "hyperboost-wpf.log",
            "error",
            "[2026-05-30 02:47:28] DispatcherUnhandledException: boom");
        var second = BuildLogAlertSignature(
            "hyperboost-wpf.log",
            "error",
            "[2026-05-30 02:49:28] DispatcherUnhandledException: boom");

        Assert.Equal(first, second);
    }

    private static string BuildLogAlertSignature(string sourceLog, string severity, string entry)
    {
        var method = typeof(App).GetMethod(
            "BuildLogAlertSignature",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(method);
        return (string)method!.Invoke(null, new object[] { sourceLog, severity, entry })!;
    }
}
