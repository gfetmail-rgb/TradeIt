using System;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void SubscribeToSettingsChanges()
        {
            ChartSettingsManager.SettingsChanged -= ChartSettingsManager_SettingsChanged;
            ChartSettingsManager.SettingsChanged += ChartSettingsManager_SettingsChanged;
        }

        private void ChartSettingsManager_SettingsChanged(object? sender, EventArgs e)
        {
            if (!IsLoaded) return;
            if (Dispatcher.CheckAccess())
                ApplyStoredChartSettings();
            else
                Dispatcher.InvokeAsync(ApplyStoredChartSettings);
        }

        private void ApplyStoredChartSettings()
        {
            try
            {
                _settings = ChartSettingsManager.Current;
                _gridVisible = _settings.GridVisible;
                ApplyGridStyle(Chart);
                ApplyGridStyle(VolumeChart);
                ApplyCrosshairStyle();

                Chart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml(_settings.FigureBackground);
                Chart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml(_settings.DataBackground);
                Chart.Plot.Axes.Color(ScottPlot.Color.FromHtml(_settings.AxisColor));
                VolumeChart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml(_settings.FigureBackground);
                VolumeChart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml(_settings.DataBackground);
                VolumeChart.Plot.Axes.Color(ScottPlot.Color.FromHtml(_settings.AxisColor));

                ApplySeriesThickness();
                Chart.Refresh();
                VolumeChart.Refresh();
            }
            catch { }
        }

        private void ApplySeriesThickness()
        {
            foreach (var plottable in Chart.Plot.GetPlottables())
            {
                if (plottable is ScottPlot.Plottables.CandlestickPlot candles)
                {
                    candles.RisingLineStyle.Width = (float)_settings.CandleLineWidth;
                    candles.FallingLineStyle.Width = (float)_settings.CandleLineWidth;
                }
                else if (plottable is ScottPlot.Plottables.OhlcPlot ohlc)
                {
                    ohlc.RisingStyle.Width = (float)_settings.BarLineWidth;
                    ohlc.FallingStyle.Width = (float)_settings.BarLineWidth;
                }
                else if (plottable is ScottPlot.Plottables.Scatter scatter)
                {
                    scatter.LineWidth = (float)_settings.LineWidth;
                }
            }
        }

        private void ApplyGridStyle(ScottPlot.WPF.WpfPlot plot)
        {
            plot.Plot.Grid.IsVisible = _settings.GridVisible;
            plot.Plot.Grid.LineColor = ScottPlot.Color.FromHtml(_settings.GridColor);
            plot.Plot.Grid.LinePattern = ParseLinePattern(_settings.GridPattern);
            plot.Plot.Grid.MajorLineWidth = (float)_settings.GridLineWidth;
            plot.Plot.Grid.MinorLineWidth = (float)_settings.GridLineWidth;
        }

        private void ApplyCrosshairStyle()
        {
            if (_crosshair == null) return;
            _crosshair.LineColor = ScottPlot.Color.FromHtml(_settings.CrosshairColor);
            _crosshair.LineWidth = (float)_settings.CrosshairLineWidth;
            _crosshair.LinePattern = ParseLinePattern(_settings.CrosshairPattern);
        }

        private static ScottPlot.LinePattern ParseLinePattern(string? value) => value switch
        {
            "Dotted" => ScottPlot.LinePattern.Dotted,
            "Dashed" => ScottPlot.LinePattern.Dashed,
            "DenselyDashed" => ScottPlot.LinePattern.DenselyDashed,
            _ => ScottPlot.LinePattern.Solid
        };
    }
}