using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private ScottPlot.Plottables.Crosshair? _volumeCrosshair;
        private bool _volumeCrosshairRegistered;

        private static readonly bool _performanceVolumeFixesRegistered = RegisterPerformanceVolumeFixes();

        private static bool RegisterPerformanceVolumeFixes()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(PerformanceVolumeFixes_Loaded));

            return true;
        }

        private static void PerformanceVolumeFixes_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            // The old UserRequestedFixes handler performs Enumerable.Range(...).OrderBy(...)
            // on every mouse move. With a large candle history this creates a large amount
            // of garbage and makes the entire chart feel delayed. DisplayFixes already owns
            // the current crosshair logic, so the duplicate handler must not remain attached.
            chart.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(() =>
                {
                    chart.Chart.MouseMove -= chart.UserFixesChart_MouseMove;
                }));

            chart.InitializeVolumeCrosshair();
            chart.VolumeContainer.IsVisibleChanged -= chart.VolumeContainer_IsVisibleChanged;
            chart.VolumeContainer.IsVisibleChanged += chart.VolumeContainer_IsVisibleChanged;

            chart.Chart.MouseMove -= chart.PerformanceVolumeFixes_ChartMouseMove;
            chart.Chart.MouseMove += chart.PerformanceVolumeFixes_ChartMouseMove;

            chart.VolumeChart.MouseMove -= chart.PerformanceVolumeFixes_VolumeMouseMove;
            chart.VolumeChart.MouseMove += chart.PerformanceVolumeFixes_VolumeMouseMove;

            chart.VolumeChart.MouseLeave -= chart.PerformanceVolumeFixes_VolumeMouseLeave;
            chart.VolumeChart.MouseLeave += chart.PerformanceVolumeFixes_VolumeMouseLeave;

            chart.ApplyVolumeSplitterState();
        }

        private void InitializeVolumeCrosshair()
        {
            if (_volumeCrosshair != null)
                return;

            _volumeCrosshair = VolumeChart.Plot.Add.Crosshair(0, 0);
            _volumeCrosshair.IsVisible = false;
            _volumeCrosshair.LineColor = ScottPlot.Color.FromHtml("#707070");
            _volumeCrosshair.LineWidth = 1;
            _volumeCrosshair.LinePattern = ScottPlot.LinePattern.Dashed;
            _volumeCrosshair.MarkerSize = 7;
            _volumeCrosshair.MarkerColor = ScottPlot.Color.FromHtml("#202020");
            _volumeCrosshair.MarkerFillColor = ScottPlot.Color.FromHtml("#FFFFFF");
            _volumeCrosshair.MarkerLineColor = ScottPlot.Color.FromHtml("#202020");
            _volumeCrosshair.MarkerLineWidth = 1;
            _volumeCrosshair.TextColor = ScottPlot.Color.FromHtml("#FFFFFF");
            _volumeCrosshair.TextBackgroundColor = ScottPlot.Color.FromHtml("#202020");
            _volumeCrosshair.FontSize = 12;
            _volumeCrosshair.FontBold = true;
            _volumeCrosshair.HorizontalLine.LabelOppositeAxis = false;
            _volumeCrosshair.VerticalLine.LabelOppositeAxis = false;
            _volumeCrosshair.HorizontalLine.LabelAlignment = ScottPlot.Alignment.MiddleRight;
            _volumeCrosshair.VerticalLine.LabelAlignment = ScottPlot.Alignment.LowerCenter;
            _volumeCrosshairRegistered = true;
        }

        private void ApplyVolumeSplitterState()
        {
            if (VolumeContainer.Visibility == Visibility.Visible)
            {
                VolumeSplitterRow.Height = new GridLength(6);
                VolumeSplitterRow.MinHeight = 6;
            }
            else
            {
                VolumeSplitterRow.Height = new GridLength(0);
                VolumeSplitterRow.MinHeight = 0;
            }
        }

        private void VolumeContainer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ApplyVolumeSplitterState();
        }

        private void PerformanceVolumeFixes_ChartMouseMove(object sender, MouseEventArgs e)
        {
            if (!_volumeVisible ||
                !_crosshairVisible ||
                !_chartVisible ||
                _volumeCrosshair == null ||
                _crosshair == null ||
                !_crosshair.IsVisible)
            {
                if (_volumeCrosshair != null)
                    _volumeCrosshair.IsVisible = false;
                return;
            }

            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out _))
                return;

            double x = _crosshair.Position.X;
            if (double.IsNaN(x) || double.IsInfinity(x))
                return;

            var limits = VolumeChart.Plot.Axes.GetLimits();
            double y = Math.Clamp(limits.Top * 0.5, limits.Bottom, limits.Top);

            _volumeCrosshair.Position = new ScottPlot.Coordinates(x, y);
            _volumeCrosshair.HorizontalLine.Text = "";
            _volumeCrosshair.VerticalLine.Text = _crosshair.VerticalLine.Text;
            _volumeCrosshair.IsVisible = true;
            VolumeChart.Refresh();
        }

        private void PerformanceVolumeFixes_VolumeMouseMove(object sender, MouseEventArgs e)
        {
            if (!_volumeVisible ||
                !_crosshairVisible ||
                !_chartVisible ||
                _volumeCrosshair == null)
            {
                if (_volumeCrosshair != null)
                    _volumeCrosshair.IsVisible = false;
                return;
            }

            if (!TryGetChartCoordinates(VolumeChart, e.GetPosition(VolumeChart), out ScottPlot.Coordinates coordinates))
                return;

            double x = coordinates.X;
            if (_crosshair != null && _crosshair.IsVisible)
                x = _crosshair.Position.X;

            _volumeCrosshair.Position = new ScottPlot.Coordinates(x, coordinates.Y);
            _volumeCrosshair.HorizontalLine.Text = (coordinates.Y * VolumeScale).ToString("N0");
            _volumeCrosshair.VerticalLine.Text = _crosshair?.VerticalLine.Text ?? "";
            _volumeCrosshair.IsVisible = true;
            VolumeChart.Refresh();
        }

        private void PerformanceVolumeFixes_VolumeMouseLeave(object sender, MouseEventArgs e)
        {
            if (_volumeCrosshair != null)
            {
                _volumeCrosshair.IsVisible = false;
                VolumeChart.Refresh();
            }
        }
    }
}
