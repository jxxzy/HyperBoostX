using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using HyperBoostX.Windows;
using HyperBoostX.Core;

namespace HyperBoostX.App;

public partial class App:System.Windows.Application
{
    private Mutex? _mutex;
    private EventWaitHandle? _activationSignal;
    private CancellationTokenSource? _activationShutdown;
    private string? _root;
    private bool _ownsMutex;
    protected override void OnStartup(StartupEventArgs e)
    {
        System.Windows.Media.RenderOptions.ProcessRenderMode=System.Windows.Interop.RenderMode.SoftwareOnly;
        base.OnStartup(e);
        string? qaRoot=e.Args.Length==2&&e.Args[0]=="--data-dir"?Path.GetFullPath(e.Args[1]):null;
        var instanceSuffix=qaRoot is null?"":".QA."+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(qaRoot.ToUpperInvariant()))[..8]);
        var mutexName="Local\\HyperBoostX.v3.Desktop"+instanceSuffix;
        var activationName="Local\\HyperBoostX.v3.Activate"+instanceSuffix;
        _mutex=new Mutex(false,mutexName);
        try{_ownsMutex=_mutex.WaitOne(0);}
        catch(AbandonedMutexException){_ownsMutex=true;}
        if(!_ownsMutex)
        {
            if(!SignalExistingInstance(activationName))
                MessageBox.Show("HyperBoostX sudah berjalan, tetapi jendela aktif tidak dapat dijangkau. Tidak ada instans kedua yang dibuka.","HyperBoostX");
            Shutdown();return;
        }
        DispatcherUnhandledException+=(_,args)=>{Log(args.Exception);MessageBox.Show(ReportExport.Redact(args.Exception.Message),"HyperBoostX — operation failed");args.Handled=true;};
        try
        {
            _activationSignal=new EventWaitHandle(false,EventResetMode.AutoReset,activationName);
            _activationShutdown=new CancellationTokenSource();
            // Explicit isolated directory for QA; never changes Windows state by itself.
            var runtime=new Runtime(qaRoot);_root=runtime.Journal.Root;
            SetTheme(runtime.Journal.GetSetting("theme","Dark"));
            var vm=new ShellViewModel(runtime);var window=new MainWindow(vm);MainWindow=window;window.Show();
            _=Task.Run(()=>WaitForActivation(_activationShutdown.Token));
        }
        catch(Exception ex){Log(ex);MessageBox.Show("Aplikasi belum dapat dibuka. "+ReportExport.Redact(ex.Message)+"\nData recovery tetap dipertahankan. Gunakan hbx.exe untuk diagnosis.","HyperBoostX",MessageBoxButton.OK,MessageBoxImage.Error);Shutdown(1);}
    }
    private static bool SignalExistingInstance(string eventName)
    {
        var deadline=DateTime.UtcNow.AddSeconds(2);
        do
        {
            try{using var signal=EventWaitHandle.OpenExisting(eventName);signal.Set();return true;}
            catch(WaitHandleCannotBeOpenedException){Thread.Sleep(50);}
            catch(UnauthorizedAccessException){return false;}
        }while(DateTime.UtcNow<deadline);
        return false;
    }
    private void WaitForActivation(CancellationToken cancellationToken)
    {
        try
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                if(!_activationSignal!.WaitOne(250))continue;
                if(cancellationToken.IsCancellationRequested||Dispatcher.HasShutdownStarted)return;
                try{Dispatcher.BeginInvoke(new Action(ActivateMainWindow));}
                catch(InvalidOperationException)when(cancellationToken.IsCancellationRequested||Dispatcher.HasShutdownStarted){return;}
            }
        }
        catch(ObjectDisposedException)when(cancellationToken.IsCancellationRequested){}
    }
    private void ActivateMainWindow()
    {
        if(MainWindow is not Window window)return;
        if(window.WindowState==WindowState.Minimized)window.WindowState=WindowState.Normal;
        if(!window.IsVisible)window.Show();
        window.Activate();
        var handle=new WindowInteropHelper(window).Handle;
        if(handle!=IntPtr.Zero)SetForegroundWindow(handle);
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr handle);
    public static void SetTheme(string theme)
    {
        var r=Current.Resources;
        if(SystemParameters.HighContrast)
        {
            r["BackgroundBrush"]=SystemColors.WindowBrush;r["SurfaceBrush"]=SystemColors.WindowBrush;r["PanelBrush"]=SystemColors.WindowBrush;
            r["TextBrush"]=SystemColors.WindowTextBrush;r["MutedBrush"]=SystemColors.WindowTextBrush;r["BorderBrush"]=SystemColors.WindowTextBrush;
            r["AccentBrush"]=SystemColors.HighlightBrush;r["AccentForegroundBrush"]=SystemColors.HighlightTextBrush;
            r["AccentSurfaceBrush"]=SystemColors.HighlightBrush;r["BlueBrush"]=SystemColors.HighlightBrush;
            r["WarningBrush"]=SystemColors.WindowTextBrush;r["DangerBrush"]=SystemColors.WindowTextBrush;return;
        }
        var light=theme=="Light";
        void Set(string key,string color)=>r[key]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        Set("BackgroundBrush",light?"#EEF3F8":"#0B1017");Set("SurfaceBrush",light?"#FFFFFF":"#111923");Set("PanelBrush",light?"#F5F8FB":"#151F2B");
        Set("TextBrush",light?"#172333":"#E8F0F7");Set("MutedBrush",light?"#526679":"#8DA1B4");Set("BorderBrush",light?"#D8E2EC":"#273647");
        Set("AccentBrush",light?"#087E72":"#5EEAD4");Set("AccentForegroundBrush",light?"#FFFFFF":"#062622");Set("AccentSurfaceBrush",light?"#DEF6EF":"#173C3B");
        Set("BlueBrush",light?"#1769BC":"#67A9FF");Set("WarningBrush",light?"#98600A":"#F6C16B");Set("DangerBrush",light?"#B32642":"#FC8493");
    }
    private void Log(Exception e)
    {
        try{if(_root==null)return;var p=SafePaths.Under(_root,"application.log");if(File.Exists(p)&&new FileInfo(p).Length>1024*1024)File.Move(p,p+".previous",true);File.AppendAllText(p,$"{DateTimeOffset.UtcNow:O} HBX-APP-0001 {ReportExport.Redact(e.ToString())}\n");}catch(IOException){}
    }
    protected override void OnExit(ExitEventArgs e)
    {
        _activationShutdown?.Cancel();
        _activationSignal?.Dispose();
        _activationShutdown?.Dispose();
        if(_ownsMutex)_mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
