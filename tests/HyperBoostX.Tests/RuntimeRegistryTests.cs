using System.IO;
using HyperBoostX.Core;
using HyperBoostX.Windows;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class RuntimeRegistryTests
{
    private sealed record Catalog(ActionInfo[] Actions);

    [Fact]
    public void RuntimeActionRegistryMatchesTheExecutableActionHandlers()
    {
        var parent=Path.Combine(Path.GetTempPath(),"HyperBoostX-tests");
        var root=Path.Combine(parent,Guid.NewGuid().ToString("N"));
        try
        {
            var runtime=new Runtime(root);
            var catalogPath=Path.Combine(AppContext.BaseDirectory,"docs","registry","runtime.json");
            var catalog=Json.Read<Catalog>(File.ReadAllText(catalogPath));
            var documented=catalog.Actions.OrderBy(action=>action.Id,StringComparer.Ordinal).ToArray();
            var actual=runtime.Engine.Actions.OrderBy(action=>action.Id,StringComparer.Ordinal).ToArray();

            Assert.Equal(documented.Select(action=>action.Id).Distinct().Count(),documented.Length);
            Assert.Equal(actual,documented);
        }
        finally
        {
            var allowed=Path.GetFullPath(parent)+Path.DirectorySeparatorChar;
            if(Path.GetFullPath(root).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(root))Directory.Delete(root,true);
        }
    }
}
