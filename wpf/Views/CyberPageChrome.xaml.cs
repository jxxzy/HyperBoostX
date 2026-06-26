using System;
using System.Windows;
using System.Windows.Controls;
using HyperBoostX.Services;
using HyperBoostX.ViewModels;

namespace HyperBoostX.Views
{
    public partial class CyberPageChrome : UserControl
    {
        public CyberPageChrome()
        {
            InitializeComponent();
        }

        private async void RefreshBackendStatus_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not CyberPageViewModel page)
                return;

            try
            {
                page.Status = "Checking local backend...";
                using var client = new HyperBoostBackendClient();
                var online = await client.HealthCheckAsync();
                page.Status = online
                    ? "Backend online. Mutating routes still require approval/session token."
                    : "Backend offline. UI remains safe and responsive.";
            }
            catch (Exception ex)
            {
                page.Status = $"Backend check failed safely: {ex.Message}";
            }
        }
    }
}
