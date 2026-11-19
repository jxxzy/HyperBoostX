using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using HyperBoostX.Core;
using HyperBoostX.Windows;
using Microsoft.Win32;

namespace HyperBoostX.App;

public sealed partial class ShellViewModel
{
    private string _updatePackagePath="";
    public bool IsUpdateCenter=>Title=="Update Center";
    public string UpdatePackagePath{get=>_updatePackagePath;set{_updatePackagePath=value??"";Notify();CommandManager.InvalidateRequerySuggested();}}
    public ObservableCollection<DiagnosticReport> UpdateInspectionHistory {get;}=[];
    public ICommand InspectCurrentPackageCommand{get;private set;}=null!;
    public ICommand SelectUpdatePackageCommand{get;private set;}=null!;
    public ICommand InspectUpdatePackageCommand{get;private set;}=null!;
    private void InitializeUpdateCenter()
    {
        InspectCurrentPackageCommand=new Command(()=>Run(async token=>{var report=await ReleasePackageInspection.InspectCurrentAsync(token);Runtime.Journal.SaveReport(report);Show(report);RefreshUpdateHistory();Status="Current executable trust and integrity inspection completed."; }),()=>!Busy&&IsUpdateCenter);
        SelectUpdatePackageCommand=new Command(()=>{var dialog=new OpenFileDialog{Title="Select a local release package to inspect",Filter="Release packages|*.exe;*.msi;*.msix;*.msixbundle;*.appx;*.appxbundle"};if(dialog.ShowDialog()==true)UpdatePackagePath=dialog.FileName;},()=>!Busy&&IsUpdateCenter);
        InspectUpdatePackageCommand=new Command(()=>Run(async token=>{var current=typeof(ShellViewModel).Assembly.GetName().Version??new Version(0,0);var report=await ReleasePackageInspection.InspectAsync(UpdatePackagePath,current,token);Runtime.Journal.SaveReport(report);Show(report);RefreshUpdateHistory();Status="Candidate package inspection completed. The package was not executed or installed."; }),()=>!Busy&&IsUpdateCenter&&File.Exists(UpdatePackagePath));
    }
    private void LoadUpdateCenterPage(){if(IsUpdateCenter)RefreshUpdateHistory();}
    private void RefreshUpdateHistory(){UpdateInspectionHistory.Clear();foreach(var report in Runtime.Journal.Reports().Where(report=>report.Center=="Update Center").Take(100))UpdateInspectionHistory.Add(report);}
}
