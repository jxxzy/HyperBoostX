using System.Windows;
using System.Windows.Controls;
namespace HyperBoostX.App;
public partial class IntegrationsPanel:System.Windows.Controls.UserControl
{
    public IntegrationsPanel()=>InitializeComponent();
    private void SaveAiKey(object sender,RoutedEventArgs e){if(DataContext is ShellViewModel vm)vm.SaveAiSecret(AiKeyBox.Password);AiKeyBox.Clear();}
    private void SaveWebhook(object sender,RoutedEventArgs e){if(DataContext is ShellViewModel vm)vm.SaveWebhookSecret(WebhookBox.Password);WebhookBox.Clear();}
}
