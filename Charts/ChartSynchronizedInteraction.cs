using System;
using System.Windows;
using System.Windows.Media;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private ScottPlot.Plottables.Crosshair? _volumeCrosshair;
        private bool _sharedChartHandlersInstalled;

        private void InstallSharedChartInteraction()
        {
            if (_sharedChartHandlersInstalled)
                return;

            _sharedChartHandlersInstalled = true;
            VolumeChart.UserInputProcessor.IsEnabled = false;
            CompositionTarget.Rendering += SharedChartRendering;
            VolumeChart.MouseMove += SharedVolumeMouseMove;
            VolumeChart.MouseLeave += SharedVolumeMouseLeave;

            ConfigureVolumeAxisSpace();
            CreateVolumeCrosshair();
            SyncVolumeXAxis();
            ConfigureVolumeAxisSpace();
        }

        private void SharedChartRendering(object? sender, EventArgs e)
        {
            if (!_volumeVisible)
                return;

            SyncVolumeXAxis();
            ConfigureVolumeAxisSpace();
        }

        private void ConfigureVolumeAxisSpace()
        {
            // Reserve exactly the same horizontal space as the price plot, but
            // suppress volume tick labels. Thus candle and volume X coordinates
            // remain identical without showing a vertical volume scale.
            VolumeChart.Plot.Axes.Left.IsVisible = true;
            VolumeChart.Plot.Axes.Right.IsVisible = true;
            VolumeChart.Plot.Axes.Left.MinimumSize = 75;
            VolumeChart.Plot.Axes.Right.MinimumSize = 30;
            VolumeChart.Plot.Axes.Bottom.IsVisible = false;
            VolumeChart.Plot.Axes.Left.Label.Text = "";
            VolumeChart.Plot.Axes.Right.Label.Text = "";
            VolumeChart.Plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic { LabelFormatter = _ => "" };
            VolumeChart.Plot.Axes.Right.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic { LabelFormatter = _ => "" };
        }

        private void CreateVolumeCrosshair()
        {
            if (_volumeCrosshair != null)
                return;

            _volumeCrosshair = VolumeChart.Plot.Add.Crosshair(0, 0);
            _volumeCrosshair.IsVisible = false;
            _volumeCrosshair.VerticalLine.IsVisible = false;
            _volumeCrosshair.HorizontalLine.IsVisible = false;
            _volumeCrosshair.LineColor = ScottPlot.Color.FromHtml("#707070");
            _volumeCrosshair.LineWidth = 1;
            _volumeCrosshair.LinePattern = ScottPlot.LinePattern.Dashed;
            _volumeCrosshair.MarkerSize = 0;
            _volumeCrosshair.VerticalLine.Label.IsVisible = false;
            _volumeCrosshair.HorizontalLine.Label.IsVisible = false;
            _volumeCrosshair.VerticalLine.ExcludeFromLegend = true;
            _volumeCrosshair.HorizontalLine.ExcludeFromLegend = true;
        }

        private void SharedVolumeMouseMove(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_volumeVisible || _volumeCrosshair == null || !_crosshairVisible)
                return;

            var p = e.GetPosition(VolumeChart);
            if (!TryGetChartCoordinates(VolumeChart, p, out var c))
                return;

            var limits = Chart.Plot.Axes.GetLimits();
            if (c.X < limits.Left || c.X > limits.Right)
                return;

            double x = GetNearestCandleX(c.X);
            double volume = GetVolumeForX(x);

            _volumeCrosshair.Position = new ScottPlot.Coordinates(x, volume);
            _volumeCrosshair.IsVisible = true;
            _volumeCrosshair.VerticalLine.IsVisible = true;
            _volumeCrosshair.HorizontalLine.IsVisible = true;

            // When the pointer is over volume, the price crosshair is hidden and
            // the volume crosshair becomes the active one at the same candle.
            if (_crosshair != null)
            {
                _crosshair.IsVisible = false;
                _crosshair.VerticalLine.IsVisible = false;
                _crosshair.HorizontalLine.IsVisible = false;
                _crosshair.VerticalLine.Label.IsVisible = false;
                _crosshair.HorizontalLine.Label.IsVisible = false;
            }

            UpdateVolumeInfo(GetNearestCandleIndex(x) - 1);
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | زمان: {FormatCrosshairX(x)} | حجم: {volume:N0}";
            VolumeChart.Refresh();
            Chart.Refresh();
        }

        private void SharedVolumeMouseLeave(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_volumeCrosshair != null)
            {
                _volumeCrosshair.IsVisible = false;
                _volumeCrosshair.VerticalLine.IsVisible = false;
                _volumeCrosshair.HorizontalLine.IsVisible = false;
                VolumeChart.Refresh();
            }
        }

        private double GetVolumeForX(double x)
        {
            if (_bars.Count == 0)
                return 0;

            int index = GetNearestCandleIndex(x) - 1;
            if (index < 0 || index >= _bars.Count)
                return 0;

            double value = _bars[index].Volume / VolumeScale;
            return double.IsFinite(value) && value >= 0 ? value : 0;
        }
    }
}
