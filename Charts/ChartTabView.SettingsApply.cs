using System;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        /// <summary>
        /// Applies the persisted chart appearance immediately to the currently displayed plots.
        /// Called by the settings window after saving changes; does not require changing portfolio.
        /// </summary>
        public void ApplySettingsImmediately()
        {
            var settings = ChartSettingsManager.Current;
            _settings = settings;
            _gridVisible = settings.GridVisible;

            ApplyGridAppearance(Chart);
            ApplyGridAppearance(VolumeChart);
            ApplyCrosshairAppearance();
            ApplyBackgroundAndAxesAppearance(Chart);
            ApplyBackgroundAndAxesAppearance(VolumeChart);

            Chart?.Refresh();
            VolumeChart?.Refresh();
        }

        private void ApplyCrosshairAppearance()
        {
            if (_crosshair == null)
                return;

            var settings = _settings;
            _crosshair.LineColor = ScottPlot.Color.FromHtml(settings.CrosshairColor);
            _crosshair.LineWidth = (float)Math.Max(0.1, settings.CrosshairLineWidth);
            _crosshair.LinePattern = ParseLinePattern(settings.CrosshairPattern);
            _crosshair.IsVisible = settings.CrosshairVisible;
        }

        private void ApplyBackgroundAndAxesAppearance(ScottPlot.WPF.WpfPlot? plot)
        {
            if (plot == null)
                return;

            var settings = _settings;
            plot.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml(settings.FigureBackground);
            plot.Plot.DataBackground.Color = ScottPlot.Color.FromHtml(settings.DataBackground);
            plot.Plot.Axes.Color(ScottPlot.Color.FromHtml(settings.AxisColor));
        }
    }
}
