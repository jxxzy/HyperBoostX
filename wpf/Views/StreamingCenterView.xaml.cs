using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HyperBoostX.Views
{
    public partial class StreamingCenterView : UserControl
    {
        private string _voicemeeterPath = "";

        public StreamingCenterView()
        {
            InitializeComponent();
            RefreshMicProfileText();
            RefreshCameraProfileText();
        }

        private void CheckStreaming_Click(object sender, RoutedEventArgs e)
        {
            MicStatusText.Text = "Streaming check: OBS/Discord should stay protected; review duplicate overlays before streaming.";
            VoicemeeterStatusText.Text = "Voicemeeter status: run detection if you use virtual audio routing.";
            StreamingProfileText.Text = BuildStreamingProfile();
        }

        private void RefreshMicrophones_Click(object sender, RoutedEventArgs e)
        {
            MicStatusText.Text = "Microphone refresh requested. Use Windows Sound Settings to pick the active communications microphone, then return to HyperBoostX for guidance.";
        }

        private void MicDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            RefreshMicProfileText();
            MicStatusText.Text += " Clipping check: if your voice peaks or distorts, lower gain before applying noise gate/compressor in your streaming app.";
        }

        private void OpenMicPrivacy_Click(object sender, RoutedEventArgs e) => LaunchUri("ms-settings:privacy-microphone");
        private void OpenSoundSettings_Click(object sender, RoutedEventArgs e) => LaunchUri("ms-settings:sound");
        private void OpenVolumeMixer_Click(object sender, RoutedEventArgs e) => LaunchUri("ms-settings:apps-volume");

        private void DetectVoicemeeter_Click(object sender, RoutedEventArgs e)
        {
            _voicemeeterPath = FindVoicemeeterPath();
            VoicemeeterStatusText.Text = string.IsNullOrWhiteSpace(_voicemeeterPath)
                ? "Voicemeeter status: not installed or not found in standard VB-Audio folders."
                : $"Voicemeeter status: found {Path.GetFileName(_voicemeeterPath)}";
        }

        private void OpenVoicemeeter_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_voicemeeterPath))
                _voicemeeterPath = FindVoicemeeterPath();

            if (string.IsNullOrWhiteSpace(_voicemeeterPath))
            {
                VoicemeeterStatusText.Text = "Voicemeeter status: not found. Use Official Download for setup guidance.";
                return;
            }

            LaunchFile(_voicemeeterPath);
            VoicemeeterStatusText.Text = "Voicemeeter launch requested. HyperBoostX did not change routing automatically.";
        }

        private void OpenVoicemeeterDownload_Click(object sender, RoutedEventArgs e) => LaunchUri("https://vb-audio.com/Voicemeeter/");

        private void MicProfile_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => RefreshMicProfileText();

        private void CameraProfile_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => RefreshCameraProfileText();

        private void ApplyStreamingCameraPreset_Click(object sender, RoutedEventArgs e)
        {
            SetCameraProfile(10, 15, 40, 0, 30);
        }

        private void ApplyLowLightCameraPreset_Click(object sender, RoutedEventArgs e)
        {
            SetCameraProfile(24, 8, 28, 1.2, 30);
        }

        private void ApplySharpFaceCameraPreset_Click(object sender, RoutedEventArgs e)
        {
            SetCameraProfile(6, 20, 58, -0.2, 60);
        }

        private void ResetCameraProfile_Click(object sender, RoutedEventArgs e)
        {
            SetCameraProfile(0, 0, 25, 0, 30);
        }

        private void ScanCameras_Click(object sender, RoutedEventArgs e)
        {
            CameraStatusText.Text = "Camera scan requested. If no camera appears in OBS/TikTok/Discord, open Camera Privacy and allow desktop apps.";
        }

        private void CameraDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            RefreshCameraProfileText();
            CameraStatusText.Text += " Diagnostics: if preview is black, close other camera apps and verify privacy permissions.";
        }

        private void OpenCameraSettings_Click(object sender, RoutedEventArgs e) => LaunchUri("ms-settings:camera");
        private void OpenCameraPrivacy_Click(object sender, RoutedEventArgs e) => LaunchUri("ms-settings:privacy-webcam");
        private void OpenCameraApp_Click(object sender, RoutedEventArgs e) => LaunchUri("microsoft.windows.camera:");
        private void OpenDeviceManager_Click(object sender, RoutedEventArgs e) => LaunchFile("devmgmt.msc");

        private void ExportStreamingProfile_Click(object sender, RoutedEventArgs e)
        {
            StreamingProfileText.Text = BuildStreamingProfile();
        }

        private void SetCameraProfile(double brightness, double contrast, double sharpness, double exposure, double fps)
        {
            CameraBrightnessSlider.Value = brightness;
            CameraContrastSlider.Value = contrast;
            CameraSharpnessSlider.Value = sharpness;
            CameraExposureSlider.Value = exposure;
            CameraFpsSlider.Value = fps;
            RefreshCameraProfileText();
            StreamingProfileText.Text = BuildStreamingProfile();
        }

        private void RefreshMicProfileText()
        {
            if (MicStatusText == null || MicGainSlider == null)
                return;

            MicStatusText.Text = $"Preview Only: gain {MicGainSlider.Value:0} | gate {MicGateSlider.Value:0} | compressor {MicCompressorSlider.Value:0}. Endpoint changes require approved action support.";
        }

        private void RefreshCameraProfileText()
        {
            if (CameraStatusText == null || CameraBrightnessSlider == null)
                return;

            CameraStatusText.Text = $"Preview Only: brightness {CameraBrightnessSlider.Value:+0;-0;0} | contrast {CameraContrastSlider.Value:+0;-0;0} | sharpness {CameraSharpnessSlider.Value:0} | exposure {CameraExposureSlider.Value:+0.0;-0.0;0.0} EV | target {CameraFpsSlider.Value:0} FPS.";
        }

        private string BuildStreamingProfile()
        {
            return "Streaming profile output:" + Environment.NewLine +
                   $"- OBS: use current mic profile, keep OBS protected, target camera {CameraFpsSlider.Value:0} FPS." + Environment.NewLine +
                   "- TikTok LIVE Studio: verify camera privacy permission and avoid duplicate capture overlays." + Environment.NewLine +
                   "- Discord: use communications microphone, review noise suppression, and avoid forced driver changes." + Environment.NewLine +
                   "- Safety: HyperBoostX did not install, rewire, or write camera driver settings automatically.";
        }

        private static string FindVoicemeeterPath()
        {
            var roots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            }.Where(path => !string.IsNullOrWhiteSpace(path));

            var names = new[] { "voicemeeter8.exe", "voicemeeterpro.exe", "voicemeeter.exe" };
            foreach (var root in roots)
            {
                foreach (var name in names)
                {
                    var path = Path.Combine(root, "VB", "Voicemeeter", name);
                    if (File.Exists(path))
                        return path;
                }
            }

            return "";
        }

        private static void LaunchUri(string uri)
        {
            try
            {
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            }
            catch
            {
                // The page stays usable if Windows blocks a settings URI.
            }
        }

        private static void LaunchFile(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch
            {
                // The page stays usable if the tool cannot be launched.
            }
        }
    }
}
