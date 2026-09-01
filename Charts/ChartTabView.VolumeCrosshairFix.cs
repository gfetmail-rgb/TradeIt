using System;
using System.Windows;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private ScottPlot.Plottables.Crosshair? _volumeCrosshairFix;
        private bool _volumeCrosshairFixInitialized;

        private void EnsureVolumeCrosshairFix()
        {
            if (_volumeCrosshairFixInitialized)
                return;

            _volumeCrosshairFixInitialized = true;

            _volumeCrosshairFix = VolumeChart.Plot.Add.Crosshair(0, 0);
            _volumeCrosshairFix.IsVisible = false;

            CopyCrosshairStyleToVolume();

            _volumeCrosshairFix.HorizontalLine.IsVisible = false;
            _volumeCrosshairFix.VerticalLine.IsVisible = true;

            VolumeChart.PreviewMouseMove -= VolumeCrosshairFix_MouseMove;
            VolumeChart.PreviewMouseMove += VolumeCrosshairFix_MouseMove;

            VolumeChart.MouseLeave -= VolumeCrosshairFix_MouseLeave;
            VolumeChart.MouseLeave += VolumeCrosshairFix_MouseLeave;
        }

        private void CopyCrosshairStyleToVolume()
        {
            if (_volumeCrosshairFix == null || _crosshair == null)
                return;

            _volumeCrosshairFix.LineColor = _crosshair.LineColor;
            _volumeCrosshairFix.LineWidth = _crosshair.LineWidth;
            _volumeCrosshairFix.LinePattern = _crosshair.LinePattern;
            _volumeCrosshairFix.MarkerSize = _crosshair.MarkerSize;
            _volumeCrosshairFix.MarkerColor = _crosshair.MarkerColor;
            _volumeCrosshairFix.MarkerFillColor = _crosshair.MarkerFillColor;
            _volumeCrosshairFix.MarkerLineColor = _crosshair.MarkerLineColor;
            _volumeCrosshairFix.MarkerLineWidth = _crosshair.MarkerLineWidth;
            _volumeCrosshairFix.TextColor = _crosshair.TextColor;
            _volumeCrosshairFix.TextBackgroundColor = _crosshair.TextBackgroundColor;
            _volumeCrosshairFix.FontSize = _crosshair.FontSize;
            _volumeCrosshairFix.FontBold = _crosshair.FontBold;

            _volumeCrosshairFix.HorizontalLine.IsVisible = false;
            _volumeCrosshairFix.VerticalLine.IsVisible = true;
        }

        private void VolumeCrosshairFix_MouseMove(object sender, MouseEventArgs e)
        {
            EnsureVolumeCrosshairFix();

            if (_volumeCrosshairFix == null || !_volumeVisible || !_chartVisible || !_crosshairVisible)
                return;

            if (!TryGetChartCoordinates(VolumeChart, e.GetPosition(VolumeChart), out ScottPlot.Coordinates volumeCoordinates))
                return;

            var priceLimits = Chart.Plot.Axes.GetLimits();
            double x = Math.Max(priceLimits.Left, Math.Min(priceLimits.Right, volumeCoordinates.X));
            double y = (priceLimits.Bottom + priceLimits.Top) / 2.0;

            _volumeCrosshairFix.Position = new ScottPlot.Coordinates(x, volumeCoordinates.Y);
            _volumeCrosshairFix.IsVisible = true;
            _volumeCrosshairFix.VerticalLine.IsVisible = true;
            _volumeCrosshairFix.HorizontalLine.IsVisible = false;

            // While the pointer is over volume, the price horizontal crosshair must disappear.
            if (_crosshair != null)
            {
                _crosshair.IsVisible = false;
                Chart.Refresh();
            }

            CopyCrosshairStyleToVolume();
            VolumeChart.Refresh();
        }

        private void VolumeCrosshairFix_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_volumeCrosshairFix != null)
            {
                _volumeCrosshairFix.IsVisible = false;
                VolumeChart.Refresh();
            }

            if (_crosshair != null)
            {
                _crosshair.IsVisible = false;
                Chart.Refresh();
            }
        }

        private void VolumeCrosshairFix_InitializeAfterChart()
        {
            EnsureVolumeCrosshairFix();
        }
    }
}
