using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfMouseWheelEventArgs = System.Windows.Input.MouseWheelEventArgs;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private ScottPlot.Plottables.Crosshair? _volumeCrosshair;
        private DispatcherTimer? _volumeSyncTimer;
        private bool _volumeSyncBusy;
        private const double VolumeTopPaddingFactor = 1.05;

        private void ChartTabView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_volumeSyncTimer != null) return;
            VolumeChart.UserInputProcessor.IsEnabled = false;
            VolumeSplitter.MinHeight = 6;
            VolumeSplitterRow.MinHeight = 6;
            VolumeChart.PreviewMouseMove += VolumeSync_MouseMove;
            VolumeChart.PreviewMouseWheel += VolumeSync_MouseWheel;
            Chart.PreviewMouseMove += VolumeSync_MainMouseMove;
            VolumeButton.Click += VolumeSync_VolumeButtonClick;
            _volumeSyncTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
            _volumeSyncTimer.Tick += VolumeSyncTimer_Tick;
            _volumeSyncTimer.Start();
            ApplyVolumeLayoutAndSync();
        }

        private void VolumeSync_VolumeButtonClick(object? sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(ApplyVolumeLayoutAndSync), DispatcherPriority.Render);
        }

        private void VolumeSync_MouseWheel(object? sender, WpfMouseWheelEventArgs e)
        {
            ZoomXAxis(e.Delta > 0 ? 0.80 : 1.25);
            e.Handled = true;
        }

        private void VolumeSync_MainMouseMove(object? sender, WpfMouseEventArgs e)
        {
            if (!_volumeVisible || !_crosshairVisible || !_chartVisible) return;
            SnapMainCrosshairToCandle();
        }

        private void VolumeSync_MouseMove(object? sender, WpfMouseEventArgs e)
        {
            if (!_volumeVisible || !_crosshairVisible || !_chartVisible) return;
            if (!TryGetChartCoordinates(VolumeChart, e.GetPosition(VolumeChart), out var c)) return;
            int index = NearestVisibleBarIndex(c.X);
            if (index < 0) return;

            double x = GetBarDateTime(_bars[index], index).ToOADate();
            double volumeY = _bars[index].Volume / VolumeScale;
            if (double.IsNaN(volumeY) || double.IsInfinity(volumeY) || volumeY < 0) volumeY = 0;

            EnsureVolumeCrosshair();
            _volumeCrosshair!.Position = new ScottPlot.Coordinates(x, volumeY);
            _volumeCrosshair.IsVisible = true;
            _volumeCrosshair.VerticalLine.IsVisible = true;
            _volumeCrosshair.HorizontalLine.IsVisible = false;
            ApplyVolumeCrosshairSettings();

            if (_crosshair != null)
            {
                _crosshair.IsVisible = false;
                Chart.Refresh();
            }

            _crosshairMouseInside = true;
            UpdateMouseInformation(new ScottPlot.Coordinates(x, volumeY));
            VolumeChart.Refresh();
        }

        private void VolumeSyncTimer_Tick(object? sender, EventArgs e)
        {
            if (_volumeSyncBusy || !_chartVisible || !_volumeVisible) return;
            ApplyVolumeLayoutAndSync();
        }

        private void ApplyVolumeLayoutAndSync()
        {
            if (_volumeSyncBusy) return;
            _volumeSyncBusy = true;
            try
            {
                if (!_volumeVisible)
                {
                    VolumeSplitterRow.Height = new GridLength(0);
                    VolumeChartRow.Height = new GridLength(0);
                    VolumeContainer.Visibility = Visibility.Collapsed;
                    return;
                }

                VolumeContainer.Visibility = Visibility.Visible;
                VolumeSplitterRow.Height = new GridLength(6);
                VolumeChart.UserInputProcessor.IsEnabled = false;

                // First synchronize the X coordinate limits, then force the
                // Volume data rectangle to occupy exactly the same pixel
                // rectangle as the Price chart. This fixes the right-edge
                // overhang caused by different axis-panel widths.
                SyncVolumeFromPriceLimits();
                AlignVolumeDataRectToPrice();
                EnsureVolumeCrosshair();
                VolumeChart.Refresh();
            }
            finally { _volumeSyncBusy = false; }
        }

        private void SyncVolumeFromPriceLimits()
        {
            if (!_volumeVisible || _bars.Count == 0) return;
            var priceLimits = Chart.Plot.Axes.GetLimits();
            double left = priceLimits.Left;
            double right = priceLimits.Right;
            if (right <= left) return;

            double maxVisibleVolume = 0;
            for (int i = 0; i < _bars.Count; i++)
            {
                double x = GetBarDateTime(_bars[i], i).ToOADate();
                if (x < left || x > right) continue;
                double v = _bars[i].Volume / VolumeScale;
                if (!double.IsNaN(v) && !double.IsInfinity(v) && v > maxVisibleVolume) maxVisibleVolume = v;
            }
            if (maxVisibleVolume <= 0) maxVisibleVolume = 1;

            VolumeChart.Plot.Axes.SetLimits(left, right, 0, maxVisibleVolume * VolumeTopPaddingFactor);
            ConfigureVolumeAxes();
            EnsureVolumeCrosshair();
            VolumeChart.Refresh();
        }

        private void EnsureVolumeCrosshair()
        {
            if (!_volumeVisible) return;
            if (_volumeCrosshair != null && VolumeChart.Plot.GetPlottables().Contains(_volumeCrosshair))
            {
                ApplyVolumeCrosshairSettings();
                return;
            }

            _volumeCrosshair = VolumeChart.Plot.Add.Crosshair(0, 0);
            _volumeCrosshair.IsVisible = false;
            ApplyVolumeCrosshairSettings();
        }

        private void SnapMainCrosshairToCandle()
        {
            if (_crosshair == null || _bars.Count == 0) return;
            if (!TryGetChartCoordinates(Chart, System.Windows.Input.Mouse.GetPosition(Chart), out var c)) return;
            int index = NearestVisibleBarIndex(c.X);
            if (index < 0) return;

            double x = GetBarDateTime(_bars[index], index).ToOADate();
            _crosshair.Position = new ScottPlot.Coordinates(x, c.Y);
            _crosshair.IsVisible = true;
            _crosshairMouseInside = true;

            if (_volumeVisible)
            {
                EnsureVolumeCrosshair();
                double volumeY = _bars[index].Volume / VolumeScale;
                if (double.IsNaN(volumeY) || double.IsInfinity(volumeY) || volumeY < 0) volumeY = 0;
                _volumeCrosshair!.Position = new ScottPlot.Coordinates(x, volumeY);
                _volumeCrosshair.IsVisible = true;
                ApplyVolumeCrosshairSettings();
                VolumeChart.Refresh();
            }
        }

        private int NearestVisibleBarIndex(double x)
        {
            if (_bars.Count == 0) return -1;
            var limits = Chart.Plot.Axes.GetLimits();
            int best = -1;
            double bestDistance = double.MaxValue;
            for (int i = 0; i < _bars.Count; i++)
            {
                double bx = GetBarDateTime(_bars[i], i).ToOADate();
                if (bx < limits.Left || bx > limits.Right) continue;
                double distance = Math.Abs(bx - x);
                if (distance < bestDistance) { bestDistance = distance; best = i; }
            }
            return best;
        }
    }
}
