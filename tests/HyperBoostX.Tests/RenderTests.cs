using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HyperBoostX.App;
using HyperBoostX.Windows;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class RenderTests
{
    [Fact]public void AllPagesRenderWithoutBindingErrors()
    {
        Exception? failure=null;
        var thread=new Thread(()=>
        {
            try
            {
                var directory=new DirectoryInfo(AppContext.BaseDirectory);
                while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"HyperBoostX.sln")))directory=directory.Parent;
                Assert.NotNull(directory);
                var output=Path.Combine(directory.FullName,"artifacts","wpf-render");Directory.CreateDirectory(output);
                var app=new HyperBoostX.App.App();app.InitializeComponent();
                var vm=new ShellViewModel(new Runtime(Path.Combine(output,"data")));
                var writer=new StringWriter();var listener=new TextWriterTraceListener(writer);
                PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
                PresentationTraceSources.DataBindingSource.Switch.Level=SourceLevels.Error;
                try
                {
                    var window=new MainWindow(vm,initializeOnLoad:false);
                    var content=(FrameworkElement)window.Content;
                    content.Measure(new Size(1280,850));content.Arrange(new Rect(0,0,1280,850));content.UpdateLayout();
                    var searchBox=(System.Windows.Controls.TextBox)window.FindName("SearchBox")!;
                    var languageSelector=(System.Windows.Controls.ComboBox)window.FindName("LanguageSelector")!;
                    Assert.Equal(6,languageSelector.Items.Count);
                    languageSelector.SelectedValue="en-US";content.UpdateLayout();
                    Assert.Equal("en-US",vm.Language);Assert.Equal("Search modules and aliases",searchBox.ToolTip);
                    languageSelector.SelectedValue="ja-JP";content.UpdateLayout();
                    Assert.Equal("ja-JP",vm.Language);Assert.Equal("モジュールと別名を検索",searchBox.ToolTip);
                    languageSelector.SelectedValue="id-ID";content.UpdateLayout();
                    ((System.Windows.Controls.Grid)content).Background=window.Background;
                    foreach(var page in ShellViewModel.AllNavigation)
                    {
                        vm.Navigate(page.Name);
                        content.Measure(new Size(1280,850));content.Arrange(new Rect(0,0,1280,850));content.UpdateLayout();
                        var bitmap=new RenderTargetBitmap(1280,850,96,96,PixelFormats.Pbgra32);bitmap.Render(content);
                        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
                        using var file=File.Create(Path.Combine(output,page.FeatureId+".png"));encoder.Save(file);
                    }
                    window.Close();listener.Flush();
                    File.WriteAllText(Path.Combine(output,"binding-errors.txt"),writer.ToString());
                    Assert.True(string.IsNullOrWhiteSpace(writer.ToString()),writer.ToString());
                }
                finally{PresentationTraceSources.DataBindingSource.Listeners.Remove(listener);listener.Dispose();app.Shutdown();}
            }
            catch(Exception e){failure=e;}
        });
        thread.SetApartmentState(ApartmentState.STA);thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(120)),"WPF rendering timed out.");
        if(failure!=null)ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
