using System;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private ScottPlot.Plottables.Crosshair? _volumeCrosshairFix;
        private bool _volumeCrosshairFixInitialized;

        private void EnsureVolumeCrosshairFix()
        {
            if (_volumeCrosshairFixInitialized) return;

            _volumeCrosshairFixInitialized = true;
            _volumeCrosshairFix = VolumeChart.Plot.Add.Crosshair(0, 0);
            _volumeCrosshairFix.IsVisible = false;
            ApplyVolumeCrosshairSettings();

            VolumeChart.PreviewMouseMove -= VolumeCrosshairFix_MouseMove;
            VolumeChart.PreviewMouseMove += VolumeCrosshairFix_MouseMove;
            VolumeChart.MouseLeave -= VolumeCrosshairFix_MouseLeave;
            VolumeChart.MouseLeave += VolumeCrosshairFix_MouseLeave;
        }

        private void ApplyVolumeCrosshairSettings()
        {
            if (_volumeCrosshairFix == null) return;

            // Use the same user-configured crosshair settings as the price chart.
            object? color = TryCreateScottPlotColor(_settings?.CrosshairColor ?? "#909090");
            if (color != null)
                _volumeCrosshairFix.LineColor = color;

            _volumeCrosshairFix.LineWidth = (float)Math.Max(0.1, _settings?.CrosshairLineWidth ?? 1.0);

            if (Enum.TryParse<ScottPlot.LinePattern>(_settings?.CrosshairPattern ?? "Dotted", true, out var pattern))
                _volumeCrosshairFix.LinePattern = pattern;

            _volumeCrosshairFix.HorizontalLine.IsVisible = false;
            _volumeCrosshairFix.VerticalLine.IsVisible = true;
        }

        private void VolumeCrosshairFix_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            EnsureVolumeCrosshairFix();
            if (_volumeCrosshairFix == null || !_volumeVisible || !_chartVisible || !_crosshairVisible) return;
            if (!TryGetChartCoordinates(VolumeChart, e.GetPosition(VolumeChart), out ScottPlot.Coordinates volumeCoordinates)) return;

            var priceLimits = Chart.Plot.Axes.GetLimits();
            double x = Math.Max(priceLimits.Left, Math.Min(priceLimits.Right, volumeCoordinates.X));

            _volumeCrosshairFix.Position = new ScottPlot.Coordinates(x, volumeCoordinates.Y);
            _volumeCrosshairFix.IsVisible = true;
            _volumeCrosshairFix.VerticalLine.IsVisible = true;
            _volumeCrosshairFix.HorizontalLine.IsVisible = false;

            // While the pointer is over Volume, hide the price-chart crosshair so its
            // horizontal line does not remain frozen on the price chart.
            if (_crosshair != null)
            {
                _crosshair.IsVisible = false;
                Chart.Refresh();
            }

            ApplyVolumeCrosshairSettings();
            VolumeChart.Refresh();
        }

        private void VolumeCrosshairFix_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
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

        private static object? TryCreateScottPlotColor(string hex)
        {
            try
            {
                Type? colorType = Type.GetType("ScottPlot.Color, ScottPlot");
                if (colorType == null) return null;
                var method = colorType.GetMethod("FromHex", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                return method?.Invoke(null, new object[] { hex });
            }
            catch
            {
                return null;
            }
        }
    }
}