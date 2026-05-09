using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace HyperBoostX
{
    public partial class CameraTrackingWindow : System.Windows.Window
    {
        private readonly int _cameraIndex;
        private readonly string _targetMode;
        private readonly string _framingMode;
        private readonly double _smoothnessPercent;
        private readonly double _deadZonePercent;
        private readonly double _brightnessPercent;
        private readonly double _contrastPercent;
        private readonly double _sharpnessPercent;
        private readonly double _exposureEv;
        private readonly double _fpsTarget;
        private CancellationTokenSource _trackingCancellation;
        private Task _trackingTask;
        private volatile bool _isStopping;
        private volatile bool _uiFramePending;

        public CameraTrackingWindow(
            int cameraIndex,
            string targetMode,
            string framingMode,
            double smoothnessPercent,
            double deadZonePercent,
            double brightnessPercent,
            double contrastPercent,
            double sharpnessPercent,
            double exposureEv,
            double fpsTarget)
        {
            InitializeComponent();
            _cameraIndex = Math.Max(0, cameraIndex);
            _targetMode = string.IsNullOrWhiteSpace(targetMode) ? "Motion Center" : targetMode;
            _framingMode = string.IsNullOrWhiteSpace(framingMode) ? "Medium" : framingMode;
            _smoothnessPercent = Math.Max(0, Math.Min(100, smoothnessPercent));
            _deadZonePercent = Math.Max(0, Math.Min(35, deadZonePercent));
            _brightnessPercent = Math.Max(-50, Math.Min(50, brightnessPercent));
            _contrastPercent = Math.Max(-50, Math.Min(50, contrastPercent));
            _sharpnessPercent = Math.Max(0, Math.Min(100, sharpnessPercent));
            _exposureEv = Math.Max(-3, Math.Min(3, exposureEv));
            _fpsTarget = Math.Max(24, Math.Min(60, fpsTarget));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TrackingHeaderText.Text = $"Camera {_cameraIndex} | target {_targetMode} | framing {_framingMode}";
            TrackingGuideText.Text =
                "Real-time tracker: motion detection, UI-safe preview throttle, smoothed center point, and live bounding box." + Environment.NewLine +
                "Camera color properties are applied only from Camera Studio, because some webcam drivers use unsafe OpenCV ranges.";
            StartTracking();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopTracking();
        }

        private void StartTracking_Click(object sender, RoutedEventArgs e)
        {
            StartTracking();
        }

        private void StopTracking_Click(object sender, RoutedEventArgs e)
        {
            StopTracking();
        }

        private void StartTracking()
        {
            if (_trackingTask != null && !_trackingTask.IsCompleted)
                return;

            _isStopping = false;
            _trackingCancellation = new CancellationTokenSource();
            CameraPreviewPlaceholderText.Visibility = Visibility.Visible;
            TrackingStatusText.Text = "Status: starting camera...";
            _trackingTask = Task.Run(() => RunTrackingLoop(_trackingCancellation.Token));
        }

        private void StopTracking()
        {
            try
            {
                _isStopping = true;
                _trackingCancellation?.Cancel();
            }
            catch
            {
                // The camera may already be closed during app shutdown.
            }
        }

        private void RunTrackingLoop(CancellationToken token)
        {
            try
            {
                using var capture = new VideoCapture(_cameraIndex);
                if (!capture.IsOpened())
                {
                    SafeUi(() =>
                    {
                        CameraPreviewPlaceholderText.Visibility = Visibility.Visible;
                        TrackingStatusText.Text = $"Status: camera {_cameraIndex} could not be opened.";
                    });
                    return;
                }

                // Keep camera driver state untouched. Some webcams persist property writes
                // such as focus/exposure/color across apps, so the tracker only reads frames.

                using var frame = new Mat();
                using var gray = new Mat();
                using var previousGray = new Mat();
                using var diff = new Mat();
                using var threshold = new Mat();
                using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(7, 7));

                Point2d smoothedCenter = default;
                var hasSmoothedCenter = false;
                var frameCount = 0;
                var lastStatusUpdate = DateTime.MinValue;
                var lastPreviewUpdate = DateTime.MinValue;
                var previewInterval = TimeSpan.FromMilliseconds(66);

                while (!token.IsCancellationRequested)
                {
                    if (!capture.Read(frame) || frame.Empty())
                    {
                        Thread.Sleep(8);
                        continue;
                    }

                    Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
                    Cv2.GaussianBlur(gray, gray, new OpenCvSharp.Size(21, 21), 0);

                    OpenCvSharp.Rect? trackingRect = null;
                    if (!previousGray.Empty())
                    {
                        Cv2.Absdiff(previousGray, gray, diff);
                        Cv2.Threshold(diff, threshold, 24, 255, ThresholdTypes.Binary);
                        Cv2.Dilate(threshold, threshold, kernel, iterations: 2);
                        Cv2.FindContours(threshold, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                        var minArea = Math.Max(800, frame.Width * frame.Height * 0.003);
                        var best = new OpenCvSharp.Rect();
                        var bestArea = 0d;
                        foreach (var contour in contours)
                        {
                            var area = Cv2.ContourArea(contour);
                            if (area < minArea || area <= bestArea)
                                continue;

                            best = Cv2.BoundingRect(contour);
                            bestArea = area;
                        }

                        if (bestArea > 0)
                            trackingRect = best;
                    }

                    gray.CopyTo(previousGray);

                    if (trackingRect.HasValue)
                    {
                        var rect = trackingRect.Value;
                        var center = new Point2d(rect.X + rect.Width / 2d, rect.Y + rect.Height / 2d);
                        var alpha = 1d - (_smoothnessPercent / 100d);
                        alpha = Math.Max(0.08, Math.Min(0.75, alpha));
                        var deadZonePixels = Math.Min(frame.Width, frame.Height) * (_deadZonePercent / 100d);

                        if (!hasSmoothedCenter)
                        {
                            smoothedCenter = center;
                            hasSmoothedCenter = true;
                        }
                        else
                        {
                            var distance = Math.Sqrt(Math.Pow(center.X - smoothedCenter.X, 2) + Math.Pow(center.Y - smoothedCenter.Y, 2));
                            if (distance > deadZonePixels)
                            {
                                smoothedCenter = new Point2d(
                                    smoothedCenter.X + ((center.X - smoothedCenter.X) * alpha),
                                    smoothedCenter.Y + ((center.Y - smoothedCenter.Y) * alpha));
                            }
                        }

                        Cv2.Rectangle(frame, rect, Scalar.DeepSkyBlue, 3);
                        Cv2.Circle(frame, new OpenCvSharp.Point((int)smoothedCenter.X, (int)smoothedCenter.Y), 8, Scalar.LimeGreen, -1);
                        Cv2.Line(frame, new OpenCvSharp.Point(frame.Width / 2, 0), new OpenCvSharp.Point(frame.Width / 2, frame.Height), Scalar.DarkGreen, 1);
                        Cv2.Line(frame, new OpenCvSharp.Point(0, frame.Height / 2), new OpenCvSharp.Point(frame.Width, frame.Height / 2), Scalar.DarkGreen, 1);
                        Cv2.PutText(frame, $"{_targetMode} | {_framingMode}", new OpenCvSharp.Point(18, 34), HersheyFonts.HersheySimplex, 0.8, Scalar.White, 2);
                        Cv2.PutText(frame, $"tracking x:{smoothedCenter.X:0} y:{smoothedCenter.Y:0}", new OpenCvSharp.Point(18, 66), HersheyFonts.HersheySimplex, 0.65, Scalar.LimeGreen, 2);
                    }
                    else
                    {
                        Cv2.PutText(frame, "waiting for motion...", new OpenCvSharp.Point(18, 34), HersheyFonts.HersheySimplex, 0.8, Scalar.White, 2);
                    }

                    frameCount++;
                    if ((DateTime.Now - lastPreviewUpdate) < previewInterval || _uiFramePending)
                    {
                        Thread.Sleep(4);
                        continue;
                    }

                    var bitmapSource = frame.ToBitmapSource();
                    bitmapSource.Freeze();
                    lastPreviewUpdate = DateTime.Now;
                    _uiFramePending = true;
                    SafeUi(() =>
                    {
                        CameraPreviewPlaceholderText.Visibility = Visibility.Collapsed;
                        CameraPreviewImage.Source = bitmapSource;
                        if ((DateTime.Now - lastStatusUpdate).TotalMilliseconds > 300)
                        {
                            TrackingStatusText.Text =
                                $"Status: live | frames {frameCount} | camera {_cameraIndex}{Environment.NewLine}" +
                                $"Target: {_targetMode} | Framing: {_framingMode}{Environment.NewLine}" +
                                $"Smoothness: {_smoothnessPercent:0}% | Dead-zone: {_deadZonePercent:0}%{Environment.NewLine}" +
                                $"Camera profile: brightness {_brightnessPercent:+0;-0;0}% | contrast {_contrastPercent:+0;-0;0}% | sharpness {_sharpnessPercent:0}% | exposure {_exposureEv:+0.0;-0.0;0.0} EV | fps {_fpsTarget:0}";
                            lastStatusUpdate = DateTime.Now;
                        }
                    });
                    Thread.Sleep(2);
                }

                SafeUi(() => TrackingStatusText.Text = "Status: stopped");
            }
            catch (Exception ex)
            {
                SafeUi(() =>
                {
                    CameraPreviewPlaceholderText.Visibility = Visibility.Visible;
                    TrackingStatusText.Text = $"Status: tracker error: {ex.Message}";
                });
            }
        }

        private void SafeUi(Action action)
        {
            if (_isStopping || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
                return;

            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        action();
                    }
                    finally
                    {
                        _uiFramePending = false;
                    }
                }));
            }
            catch
            {
                // The window can close while a frame is being processed.
            }
        }
    }
}
