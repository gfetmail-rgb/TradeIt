using System;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _unifiedBordersRegistered = RegisterUnifiedBorders();

        private static bool RegisterUnifiedBorders()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(UnifiedBorders_Loaded));
            return true;
        }

        private static void UnifiedBorders_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            chart.ApplyUnifiedPlotBorders();
            ChartSettingsManager.SettingsChanged -= chart.UnifiedBorders_SettingsChanged;
            ChartSettingsManager.SettingsChanged += chart.UnifiedBorders_SettingsChanged;
        }

        private void UnifiedBorders_SettingsChanged(object? sender, EventArgs e)
        {
            if (Dispatcher.CheckAccess())
                ApplyUnifiedPlotBorders();
            else
                Dispatcher.InvokeAsync(ApplyUnifiedPlotBorders);
        }

        /// <summary>
        /// Price and volume use exactly the same DataBorder and FigureBorder.
        /// Axis frame lines are disabled so the DataBorder is the single
        /// visible border around the data area.
        /// </summary>
        private void ApplyUnifiedPlotBorders()
        {
            try
            {
                _settings = ChartSettingsManager.Current;
                ScottPlot.Color borderColor = ScottPlot.Color.FromHtml(_settings.AxisColor);

                ApplyUnifiedPlotBorders(Chart, borderColor);
                ApplyUnifiedPlotBorders(VolumeChart, borderColor);

                Chart.Refresh();
                VolumeChart.Refresh();
            }
            catch
            {
                // Border styling must never prevent the chart from loading.
            }
        }

        private static void ApplyUnifiedPlotBorders(
            ScottPlot.WPF.WpfPlot plot,
            ScottPlot.Color borderColor)
        {
            plot.Plot.DataBorder = new ScottPlot.LineStyle
            {
                Color = borderColor,
                Width = 1,
                Pattern = ScottPlot.LinePattern.Solid
            };

            plot.Plot.FigureBorder = new ScottPlot.LineStyle
            {
                Color = borderColor,
                Width = 1,
                Pattern = ScottPlot.LinePattern.Solid
            };

            // DataBorder replaces the axis frame around the data rectangle.
            plot.Plot.Axes.Frame(false);
        }
    }
}
