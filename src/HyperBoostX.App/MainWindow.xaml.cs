using System.ComponentModel;
using Drawing=System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using HyperBoostX.Core;
using WinForms=System.Windows.Forms;

namespace HyperBoostX.App;

public sealed class InverseBoolConverter:IValueConverter
{
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)=>value is not true;
    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)=>value is not true;
}
public partial class MainWindow:Window
{
    private readonly ShellViewModel _vm;
    private readonly WinForms.ContextMenuStrip _trayMenu=new();
    private readonly Drawing.Icon _trayImage;
    private readonly WinForms.NotifyIcon _trayIcon;
    public MainWindow(ShellViewModel vm,bool initializeOnLoad=true)
    {
        _vm=vm;InitializeComponent();DataContext=vm;
        _trayImage=Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath??throw new InvalidOperationException("The application executable path is unavailable."))
            ??throw new InvalidOperationException("The application icon could not be loaded for the system tray.");
        _trayMenu.Items.Add(vm.Localization["TrayShow"],null,(_,_)=>RestoreFromTray());
        _trayMenu.Items.Add(new WinForms.ToolStripSeparator());
        _trayMenu.Items.Add(vm.Localization["TrayExit"],null,(_,_)=>Close());
        _trayIcon=new WinForms.NotifyIcon{Icon=_trayImage,Text="HyperBoostX V3",ContextMenuStrip=_trayMenu,Visible=true};
        _trayIcon.DoubleClick+=(_,_)=>RestoreFromTray();
        vm.Localization.PropertyChanged+=RefreshTrayLabels;
        try
        {
            var state=vm.Runtime.Journal.GetSetting("window");
            if(state!="")
            {
                var saved=Json.Read<WindowPosition>(state);
                var desktop=new Rect(SystemParameters.VirtualScreenLeft,SystemParameters.VirtualScreenTop,SystemParameters.VirtualScreenWidth,SystemParameters.VirtualScreenHeight);
                if(saved.Width>=MinWidth&&saved.Height>=MinHeight&&saved.Width<=desktop.Width&&saved.Height<=desktop.Height&&desktop.Contains(new Rect(saved.Left,saved.Top,saved.Width,saved.Height)))
                {WindowStartupLocation=WindowStartupLocation.Manual;Left=saved.Left;Top=saved.Top;Width=saved.Width;Height=saved.Height;if(saved.Maximized)WindowState=WindowState.Maximized;}
            }
        }catch(Exception e)when(e is System.Text.Json.JsonException or ArgumentException){}
        if(initializeOnLoad)Loaded+=(_,_)=>{vm.Initialize();vm.StartTelemetry();};
        Closed+=(_,_)=>{vm.StopTelemetry();vm.Localization.PropertyChanged-=RefreshTrayLabels;_trayIcon.Visible=false;_trayIcon.Dispose();_trayMenu.Dispose();_trayImage.Dispose();};
        PreviewKeyDown+=(_,e)=>
        {
            if(e.Key==Key.K&&Keyboard.Modifiers.HasFlag(ModifierKeys.Control)){_vm.OpenCommandPaletteCommand.Execute(null);e.Handled=true;return;}
            if(e.Key==Key.Escape&&_vm.IsCommandPaletteOpen){_vm.CloseCommandPaletteCommand.Execute(null);e.Handled=true;return;}
            if(e.Key==Key.Enter&&_vm.IsCommandPaletteOpen&&_vm.ExecuteCommandPaletteCommand.CanExecute(null)){_vm.ExecuteCommandPaletteCommand.Execute(null);e.Handled=true;}
        };
        Closing+=(_,e)=>
        {
            if(vm.Busy){e.Cancel=true;vm.Stop();vm.Status="Menunggu operasi berhenti pada batas aman. Tutup kembali setelah operasi selesai.";return;}
            if(vm.HasGamingSession&&MessageBox.Show("Sesi gaming belum dipulihkan. Menutup aplikasi menghentikan auto-restore. Pilih No lalu Game Library > Restore session untuk memulihkan sekarang.\n\nTetap tutup dan simpan recovery untuk sesi berikutnya?","Gaming session",MessageBoxButton.YesNo,MessageBoxImage.Warning,MessageBoxResult.No)!=MessageBoxResult.Yes){e.Cancel=true;return;}
            var b=RestoreBounds;
            try{vm.Runtime.Journal.SetSetting("window",Json.Write(new WindowPosition(b.Left,b.Top,b.Width,b.Height,WindowState==WindowState.Maximized)));}
            catch(Exception){/* Closing must remain available if settings storage fails. */}
        };
    }
    private void RestoreFromTray()
    {
        if(WindowState==WindowState.Minimized)WindowState=WindowState.Normal;
        Show();Activate();
    }
    private void RefreshTrayLabels(object? sender,PropertyChangedEventArgs e)
    {
        if(e.PropertyName is not ("Language" or "Item[]"))return;
        _trayMenu.Items[0].Text=_vm.Localization["TrayShow"];
        _trayMenu.Items[2].Text=_vm.Localization["TrayExit"];
    }
    private sealed record WindowPosition(double Left,double Top,double Width,double Height,bool Maximized);
}
