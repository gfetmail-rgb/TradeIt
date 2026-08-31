using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private ScottPlot.Plottables.Crosshair? _volumeCrosshair;
        private static readonly bool _registered = RegisterFixes();

        private static bool RegisterFixes()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnLoaded));
            return true;
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart) return;
            chart.ConfigureVolumeInteraction();
            chart.InitializeVolumeCrosshair();
            chart.ApplyVolumeSplitterState();
            chart.VolumeContainer.IsVisibleChanged -= chart.VolumeContainer_IsVisibleChanged;
            chart.VolumeContainer.IsVisibleChanged += chart.VolumeContainer_IsVisibleChanged;
            chart.Chart.MouseMove -= chart.FixChartMouseMove;
            chart.Chart.MouseMove += chart.FixChartMouseMove;
            chart.VolumeChart.MouseMove -= chart.FixVolumeMouseMove;
            chart.VolumeChart.MouseMove += chart.FixVolumeMouseMove;
            chart.VolumeChart.MouseLeave -= chart.FixVolumeMouseLeave;
            chart.VolumeChart.MouseLeave += chart.FixVolumeMouseLeave;
            chart.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(chart.SyncVolumeXAxisAndScale));
        }

        private void ConfigureVolumeInteraction()
        {
            // Volume is a projection of the price chart. It must never pan or zoom independently.
            VolumeChart.UserInputProcessor.IsEnabled = false;
        }

        private void InitializeVolumeCrosshair()
        {
            if (_volumeCrosshair != null) return;
            _volumeCrosshair = VolumeChart.Plot.Add.Crosshair(0, 0);
            ConfigureVolumeCrosshairStyle(_volumeCrosshair);
            _volumeCrosshair.IsVisible = false;
        }

        private static void ConfigureVolumeCrosshairStyle(ScottPlot.Plottables.Crosshair c)
        {
            c.LineColor = ScottPlot.Color.FromHtml("#707070");
            c.LineWidth = 1;
            c.LinePattern = ScottPlot.LinePattern.Dashed;
            c.MarkerSize = 7;
            c.MarkerColor = ScottPlot.Color.FromHtml("#202020");
            c.MarkerFillColor = ScottPlot.Color.FromHtml("#FFFFFF");
            c.MarkerLineColor = ScottPlot.Color.FromHtml("#202020");
            c.MarkerLineWidth = 1;
            c.TextColor = ScottPlot.Color.FromHtml("#FFFFFF");
            c.TextBackgroundColor = ScottPlot.Color.FromHtml("#202020");
            c.FontSize = 12;
            c.FontBold = true;
            c.HorizontalLine.LabelOppositeAxis = false;
            c.VerticalLine.LabelOppositeAxis = false;
            c.HorizontalLine.LabelAlignment = ScottPlot.Alignment.MiddleRight;
            c.VerticalLine.LabelAlignment = ScottPlot.Alignment.LowerCenter;
        }

        private void EnsureVolumeCrosshair()
        {
            if (_volumeCrosshair == null) InitializeVolumeCrosshair();
            if (_volumeCrosshair == null) return;
            if (VolumeChart.Plot.GetPlottables().Contains(_volumeCrosshair)) return;
            _volumeCrosshair = VolumeChart.Plot.Add.Crosshair(0, 0);
            ConfigureVolumeCrosshairStyle(_volumeCrosshair);
            _volumeCrosshair.IsVisible = false;
        }

        private void ApplyVolumeSplitterState()
        {
            if (VolumeContainer.Visibility == Visibility.Visible)
            {
                VolumeSplitterRow.Height = new GridLength(6, GridUnitType.Pixel);
                VolumeSplitterRow.MinHeight = 6;
                if (MainChartRow.MinHeight < 120) MainChartRow.MinHeight = 120;
                if (VolumeChartRow.MinHeight < 100) VolumeChartRow.MinHeight = 100;
                ConfigureVolumeInteraction();
                EnsureVolumeCrosshair();
                SyncVolumeXAxisAndScale();
            }
            else
            {
                VolumeSplitterRow.Height = new GridLength(0, GridUnitType.Pixel);
                VolumeSplitterRow.MinHeight = 0;
                VolumeChartRow.MinHeight = 0;
                VolumeChartRow.Height = new GridLength(0, GridUnitType.Pixel);
            }
        }

        private void VolumeContainer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) => ApplyVolumeSplitterState();

        private double BarX(int i) => _bars[i].Timestamp.HasValue ? _bars[i].Timestamp.Value.ToOADate() : i;

        private int NearestBar(double x)
        {
            if (_bars.Count == 0 || double.IsNaN(x) || double.IsInfinity(x)) return -1;
            int lo = 0, hi = _bars.Count - 1;
            while (lo < hi)
            {
                int mid = lo + (hi - lo) / 2;
                if (BarX(mid) < x) lo = mid + 1; else hi = mid;
            }
            if (lo == 0) return 0;
            return Math.Abs(BarX(lo - 1) - x) <= Math.Abs(BarX(lo) - x) ? lo - 1 : lo;
        }

        private double VisibleMaxVolume(double left, double right)
        {
            double max = 0;
            for (int i = 0; i < _bars.Count; i++)
            {
                double x = BarX(i);
                if (x < left || x > right) continue;
                double v = _bars[i].Volume;
                if (!double.IsNaN(v) && !double.IsInfinity(v) && v > 0) max = Math.Max(max, v / VolumeScale);
            }
            return max > 0 ? max : 1;
        }

        private void SyncVolumeXAxisAndScale()
        {
            if (!_volumeVisible || VolumeContainer.Visibility != Visibility.Visible) return;
            try
            {
                var main = Chart.Plot.Axes.GetLimits();
                double max = VisibleMaxVolume(main.Left, main.Right);
                VolumeChart.Plot.Axes.SetLimits(main.Left, main.Right, 0, max);
            }
            catch { }
        }

        private void SetCrosshairAtX(double x)
        {
            int i = NearestBar(x);
            if (i < 0) return;
            double sx = BarX(i);
            var main = Chart.Plot.Axes.GetLimits();
            if (_crosshair != null)
            {
                double y = _crosshair.Position.Y;
                if (double.IsNaN(y) || double.IsInfinity(y)) y = (main.Bottom + main.Top) / 2;
                y = Math.Max(main.Bottom, Math.Min(main.Top, y));
                _crosshair.Position = new ScottPlot.Coordinates(sx, y);
                _crosshair.IsVisible = true;
            }
            EnsureVolumeCrosshair();
            if (_volumeCrosshair == null) return;
            double v = _bars[i].Volume;
            if (double.IsNaN(v) || double.IsInfinity(v) || v < 0) v = 0;
            double vk = v / VolumeScale;
            _volumeCrosshair.Position = new ScottPlot.Coordinates(sx, vk);
            _volumeCrosshair.HorizontalLine.Text = vk.ToString("N0");
            _volumeCrosshair.VerticalLine.Text = _crosshair?.VerticalLine.Text ?? "";
            _volumeCrosshair.IsVisible = true;
        }

        private void FixChartMouseMove(object sender, MouseEventArgs e)
        {
            if (!_volumeVisible || !_crosshairVisible || !_chartVisible) return;
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if (!_volumeVisible || VolumeContainer.Visibility != Visibility.Visible) return;
                SyncVolumeXAxisAndScale();
                if (_crosshair != null && _crosshair.IsVisible)
                {
                    SetCrosshairAtX(_crosshair.Position.X);
                    Chart.Refresh();
                    VolumeChart.Refresh();
                }
            }));
        }

        private void FixVolumeMouseMove(object sender, MouseEventArgs e)
        {
            if (!_volumeVisible || !_crosshairVisible || !_chartVisible) return;
            EnsureVolumeCrosshair();
            if (!TryGetChartCoordinates(VolumeChart, e.GetPosition(VolumeChart), out ScottPlot.Coordinates p)) return;
            var main = Chart.Plot.Axes.GetLimits();
            if (p.X < main.Left || p.X > main.Right) return;
            int i = NearestBar(p.X);
            if (i < 0) return;
            double x = BarX(i);
            double y = (main.Bottom + main.Top) / 2;
            if (_crosshair != null)
            {
                _crosshair.Position = new ScottPlot.Coordinates(x, y);
                _crosshair.IsVisible = true;
            }
            SetCrosshairAtX(x);
            UpdateMouseInformation(new ScottPlot.Coordinates(x, y));
            Chart.Refresh();
            VolumeChart.Refresh();
        }

        private void FixVolumeMouseLeave(object sender, MouseEventArgs e)
        {
            if (_volumeCrosshair != null)
            {
                _volumeCrosshair.IsVisible = false;
                VolumeChart.Refresh();
            }
        }
    }
}
