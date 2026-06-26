using System;
using System.Windows;
using System.Windows.Controls;
using HyperBoostX.Services;
using HyperBoostX.ViewModels;

namespace HyperBoostX.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly LocalConfigService _configService = new();

        public SettingsView()
        {
            InitializeComponent();
        }

        private SettingsViewModel ViewModel => DataContext as SettingsViewModel;

        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void ReloadSettings_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
                return;

            try
            {
                _configService.SaveUiSettings(new UiSettings
                {
                    EnableAnimations = ViewModel.EnableAnimations,
                    ReduceMotion = ViewModel.ReduceMotion,
                    AccentColor = ViewModel.AccentColor,
                    Mode = ViewModel.Mode
                });
                ViewModel.Status = "Saved to ui_settings.json. Changes apply across the cyber shell on next launch where required.";
            }
            catch (Exception ex)
            {
                ViewModel.Status = $"Save failed safely: {ex.Message}";
            }
        }

        private void LoadSettings()
        {
            if (ViewModel == null)
                return;

            try
            {
                var settings = _configService.LoadUiSettings();
                ViewModel.EnableAnimations = settings.EnableAnimations;
                ViewModel.ReduceMotion = settings.ReduceMotion;
                ViewModel.AccentColor = settings.AccentColor;
                ViewModel.Mode = settings.Mode;
                ViewModel.Status = "Loaded from ui_settings.json.";
            }
            catch (Exception ex)
            {
                ViewModel.Status = $"Load failed safely: {ex.Message}";
            }
        }
    }
}
