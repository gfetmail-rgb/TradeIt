using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _settingsHandlerRegistered = RegisterSettingsHandler();

        private static bool RegisterSettingsHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(ApplyUserChartSettings),
                true);
            return true;
        }

        private static void ApplyUserChartSettings(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView view)
                return;

            view.ApplyPersistedAppearance();
        }

        private void ApplyPersistedAppearance()
        {
            try
            {
                var settings = ChartSettingsManager.Current;
                _settings = settings;
                _gridVisible = settings.GridVisible;

                ApplyGridAppearance(Chart);
                ApplyGridAppearance(VolumeChart);

                if (_crosshair != null)
                {
                    _crosshair.LineColor = ScottPlot.Color.FromHtml(settings.CrosshairColor);
                    _crosshair.LineWidth = settings.CrosshairLineWidth;
                    _crosshair.LinePattern = ParseLinePattern(settings.CrosshairPattern);
                }

                Chart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml(settings.FigureBackground);
                Chart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml(settings.DataBackground);
                Chart.Plot.Axes.Color(ScottPlot.Color.FromHtml(settings.AxisColor));

                VolumeChart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml(settings.FigureBackground);
                VolumeChart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml(settings.DataBackground);
                VolumeChart.Plot.Axes.Color(ScottPlot.Color.FromHtml(settings.AxisColor));

                Chart.Refresh();
                VolumeChart.Refresh();
            }
            catch
            {
                // User preferences must never prevent a chart from opening.
            }
        }

        private void ApplyGridAppearance(ScottPlot.WPF.WpfPlot chart)
        {
            var settings = _settings;
            chart.Plot.Grid.IsVisible = settings.GridVisible;
            chart.Plot.Grid.LineColor = ScottPlot.Color.FromHtml(settings.GridColor);
            chart.Plot.Grid.LinePattern = ParseLinePattern(settings.GridPattern);
            chart.Plot.Grid.MajorLineWidth = settings.GridLineWidth;
            chart.Plot.Grid.MinorLineWidth = settings.GridLineWidth;
        }

        private static ScottPlot.LinePattern ParseLinePattern(string? value)
        {
            return value?.ToLowerInvariant() switch
            {
                "dotted" => ScottPlot.LinePattern.Dotted,
                "dashed" => ScottPlot.LinePattern.Dashed,
                "denselydashed" => ScottPlot.LinePattern.DenselyDashed,
                _ => ScottPlot.LinePattern.Solid
            };
        }
    }
}