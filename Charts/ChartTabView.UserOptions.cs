using System;
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
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.UnloadedEvent,
                new RoutedEventHandler(RemoveSettingsHandler),
                true);
            return true;
        }

        private static void ApplyUserChartSettings(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView view)
                return;

            ChartSettingsManager.SettingsChanged -= view.ChartSettingsManager_SettingsChanged;
            ChartSettingsManager.SettingsChanged += view.ChartSettingsManager_SettingsChanged;
            view.ApplyPersistedAppearance();
        }

        private static void RemoveSettingsHandler(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView view)
                ChartSettingsManager.SettingsChanged -= view.ChartSettingsManager_SettingsChanged;
        }

        private void ChartSettingsManager_SettingsChanged(object? sender, EventArgs e)
        {
            Dispatcher.InvokeAsync(ApplyPersistedAppearance);
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
                    _crosshair.LineWidth = (float)settings.CrosshairLineWidth;
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
            }
        }

        private void ApplyGridAppearance(ScottPlot.WPF.WpfPlot chart)
        {
            var settings = _settings;
            chart.Plot.Grid.IsVisible = settings.GridVisible;
            chart.Plot.Grid.LineColor = ScottPlot.Color.FromHtml(settings.GridColor);
            chart.Plot.Grid.LinePattern = ParseLinePattern(settings.GridPattern);
            chart.Plot.Grid.MajorLineWidth = (float)settings.GridLineWidth;
            chart.Plot.Grid.MinorLineWidth = (float)settings.GridLineWidth;
        }
    }
}
