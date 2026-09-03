using System;
using System.Windows;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;
using WpfSelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
using WpfSelectionChangedEventHandler = System.Windows.Controls.SelectionChangedEventHandler;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _finalDisplayStateFixRegistered = RegisterFinalDisplayStateFix();

        private static bool RegisterFinalDisplayStateFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(FinalDisplayStateFix_Loaded),
                true);

            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                WpfComboBox.SelectionChangedEvent,
                new WpfSelectionChangedEventHandler(FinalDisplayStateFix_SelectionChanged),
                true);

            return true;
        }

        private static void FinalDisplayStateFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            // Run after the other Loaded handlers. This makes the persisted state
            // the final source of truth for every newly-created chart instance.
            chart.Dispatcher.BeginInvoke(new Action(chart.ApplyFinalPersistedDisplayState),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private static void FinalDisplayStateFix_SelectionChanged(object sender, WpfSelectionChangedEventArgs e)
        {
            if (sender is not ChartTabView chart || !ReferenceEquals(e.OriginalSource, chart.ChartTypeComboBox))
                return;

            chart.Dispatcher.BeginInvoke(new Action(chart.EnforceLinePlotConfiguration),
                System.Windows.Threading.DispatcherPriority.DataBind);
        }

        private void ApplyFinalPersistedDisplayState()
        {
            ChartSettings settings = ChartSettingsManager.Current;
            _settings = settings;
            _gridVisible = settings.GridVisible;
            _crosshairVisible = settings.CrosshairVisible;

            string persistedChartType = string.IsNullOrWhiteSpace(settings.ChartType)
                ? "Candlestick"
                : settings.ChartType.Trim();

            _chartType = persistedChartType.ToLowerInvariant() switch
            {
                "line" => ChartDisplayType.Line,
                "bar" => ChartDisplayType.Bar,
                _ => ChartDisplayType.Candlestick
            };

            for (int i = 0; i < ChartTypeComboBox.Items.Count; i++)
            {
                if (ChartTypeComboBox.Items[i] is WpfComboBoxItem item &&
                    string.Equals(item.Tag?.ToString(), persistedChartType, StringComparison.OrdinalIgnoreCase))
                {
                    if (ChartTypeComboBox.SelectedIndex != i)
                        ChartTypeComboBox.SelectedIndex = i;
                    break;
                }
            }

            if (_bars.Count > 0)
                DrawChart();
            else
                ApplyChartVisualSettingsOnly();

            ApplyGridStyle(Chart);
            ApplyCrosshairStyle();
            ApplyGridDisplayState();
            ApplyCrosshairDisplayState();
            EnforceLinePlotConfiguration();

            GridButton.Content = _gridVisible ? "GRID" : "GRID خاموش";
            CrosshairButton.Content = _crosshairVisible ? "Crosshair روشن" : "Crosshair خاموش";
            Chart.Refresh();
        }

        private void EnforceLinePlotConfiguration()
        {
            foreach (var plottable in Chart.Plot.GetPlottables())
            {
                if (plottable is ScottPlot.Plottables.Scatter scatter)
                {
                    // A normal price line must connect only the supplied data points.
                    // Explicitly disable smoothing and use the straight path strategy.
                    scatter.ConnectStyle = ScottPlot.ConnectStyle.Straight;
                    scatter.Smooth = false;
                    scatter.PathStrategy = new ScottPlot.PathStrategies.Straight();
                }
            }

            Chart.Refresh();
        }
    }
}
