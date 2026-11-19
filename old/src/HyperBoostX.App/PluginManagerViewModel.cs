using System.IO;
using System.Windows.Input;
using HyperBoostX.Windows;

namespace HyperBoostX.App;

public sealed partial class ShellViewModel
{
    public bool IsPluginManager=>Title=="Plugin Manager";
    public string PluginDirectory=>Path.Combine(Runtime.Journal.Root,"Plugins");
    public ICommand ReviewPluginsCommand{get;private set;}=null!;
    private void InitializePluginManager()=>ReviewPluginsCommand=new Command(()=>Run(async token=>{var version=typeof(ShellViewModel).Assembly.GetName().Version??new Version(0,0);var report=await PluginManifestInspection.ScanAsync(PluginDirectory,version,token);Runtime.Journal.SaveReport(report);Show(report);Status="Plugin review completed. No plugin code was loaded or executed.";}),()=>!Busy&&IsPluginManager);
}
