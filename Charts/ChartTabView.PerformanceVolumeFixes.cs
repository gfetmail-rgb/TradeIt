using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private ScottPlot.Plottables.Crosshair? _volumeCrosshair;
        private static readonly bool _performanceVolumeFixesRegistered = RegisterPerformanceVolumeFixes();

        private static bool RegisterPerformanceVolumeFixes()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(PerformanceVolumeFixes_Loaded));
            return true;
        }

        private static void PerformanceVolumeFixes_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart) return;
            chart.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
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
            if (_volumeCrosshair != null) return;
            _volumeCrosshair = VolumeChart.Plot.Add.Crosshair(0, 0);
            ConfigureVolumeCrosshairStyle(_volumeCrosshair);
            _volumeCrosshair.IsVisible = false;
        }

        private static void ConfigureVolumeCrosshairStyle(ScottPlot.Plottables.Crosshair crosshair)
        {
            crosshair.LineColor = ScottPlot.Color.FromHtml("#707070");
            crosshair.LineWidth = 1;
            crosshair.LinePattern = ScottPlot.LinePattern.Dashed;
            crosshair.MarkerSize = 7;
            crosshair.MarkerColor = ScottPlot.Color.FromHtml("#202020");
            crosshair.MarkerFillColor = ScottPlot.Color.FromHtml("#FFFFFF");
            crosshair.MarkerLineColor = ScottPlot.Color.FromHtml("#202020");
            crosshair.MarkerLineWidth = 1;
            crosshair.TextColor = ScottPlot.Color.FromHtml("#FFFFFF");
            crosshair.TextBackgroundColor = ScottPlot.Color.FromHtml("#202020");
            crosshair.FontSize = 12;
            crosshair.FontBold = true;
            crosshair.HorizontalLine.LabelOppositeAxis = false;
            crosshair.VerticalLine.LabelOppositeAxis = false;
            crosshair.HorizontalLine.LabelAlignment = ScottPlot.Alignment.MiddleRight;
            crosshair.VerticalLine.LabelAlignment = ScottPlot.Alignment.LowerCenter;
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
            if (VolumeContainer.Visibility == Visibility.Visible) EnsureVolumeCrosshair();
        }

        private void PerformanceVolumeFixes_ChartMouseMove(object sender, WpfMouseEventArgs e)
        {
            if (!_volumeVisible || !_crosshairVisible || !_chartVisible || _crosshair == null)
            {
                if (_volumeCrosshair != null) _volumeCrosshair.IsVisible = false;
                return;
            }
            EnsureVolumeCrosshair();
            if (!_crosshair.IsVisible) return;
            double x = _crosshair.Position.X;
            if (double.IsNaN(x) || double.IsInfinity(x)) return;
            var limits = VolumeChart.Plot.Axes.GetLimits();
            double y = Math.Clamp(limits.Top * 0.5, limits.Bottom, limits.Top);
            _volumeCrosshair!.Position = new ScottPlot.Coordinates(x, y);
            _volumeCrosshair.HorizontalLine.Text = "";
            _volumeCrosshair.VerticalLine.Text = _crosshair.VerticalLine.Text;
            _volumeCrosshair.IsVisible = true;
            VolumeChart.Refresh();
        }

        private void PerformanceVolumeFixes_VolumeMouseMove(object sender, WpfMouseEventArgs e)
        {
            if (!_volumeVisible || !_crosshairVisible || !_chartVisible)
            {
                if (_volumeCrosshair != null) _volumeCrosshair.IsVisible = false;
                return;
            }
            EnsureVolumeCrosshair();
            if (!TryGetChartCoordinates(VolumeChart, e.GetPosition(VolumeChart), out ScottPlot.Coordinates coordinates)) return;
            double x = coordinates.X;
            if (_crosshair != null && _crosshair.IsVisible) x = _crosshair.Position.X;
            _volumeCrosshair!.Position = new ScottPlot.Coordinates(x, coordinates.Y);
            _volumeCrosshair.HorizontalLine.Text = (coordinates.Y * VolumeScale).ToString("N0");
            _volumeCrosshair.VerticalLine.Text = _crosshair?.VerticalLine.Text ?? "";
            _volumeCrosshair.IsVisible = true;
            VolumeChart.Refresh();
        }

        private void PerformanceVolumeFixes_VolumeMouseLeave(object sender, WpfMouseEventArgs e)
        {
            if (_volumeCrosshair != null)
            {
                _volumeCrosshair.IsVisible = false;
                VolumeChart.Refresh();
            }
        }
    }
}
