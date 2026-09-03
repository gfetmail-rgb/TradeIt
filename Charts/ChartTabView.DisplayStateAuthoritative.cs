using System;
using System.Linq;
using System.Windows;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;
using WpfSelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
using WpfSelectionChangedEventHandler = System.Windows.Controls.SelectionChangedEventHandler;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _authoritativeDisplayStateRegistered = RegisterAuthoritativeDisplayState();

        private static bool RegisterAuthoritativeDisplayState()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(AuthoritativeDisplayState_Loaded),
                true);

            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                WpfButton.ClickEvent,
                new RoutedEventHandler(AuthoritativeDisplayState_Click),
                true);

            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                WpfComboBox.SelectionChangedEvent,
                new WpfSelectionChangedEventHandler(AuthoritativeDisplayState_SelectionChanged),
                true);

            return true;
        }

        private static void AuthoritativeDisplayState_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            // Apply the global state after every constructor/Loaded handler has run.
            chart.Dispatcher.BeginInvoke(new Action(chart.ApplyAuthoritativeDisplayState),
                System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private static void AuthoritativeDisplayState_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            if (e.OriginalSource is not WpfButton button)
                return;

            if (button.Name != nameof(CrosshairButton) && button.Name != nameof(GridButton))
                return;

            // Save only after the actual instance Click handler has toggled the field.
            chart.Dispatcher.BeginInvoke(new Action(chart.SaveAuthoritativeDisplayState),
                System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private static void AuthoritativeDisplayState_SelectionChanged(object sender, WpfSelectionChangedEventArgs e)
        {
            if (sender is not ChartTabView chart || !ReferenceEquals(e.OriginalSource, chart.ChartTypeComboBox))
                return;

            // Save only after ChartTypeComboBox_SelectionChanged has updated _chartType.
            chart.Dispatcher.BeginInvoke(new Action(chart.SaveAuthoritativeDisplayState),
                System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        private void ApplyAuthoritativeDisplayState()
        {
            ChartSettings settings = ChartSettingsManager.Current;
            _settings = settings;
            _gridVisible = settings.GridVisible;
            _crosshairVisible = settings.CrosshairVisible;

            string type = string.IsNullOrWhiteSpace(settings.ChartType)
                ? "Candlestick"
                : settings.ChartType.Trim();

            _chartType = type.Equals("Line", StringComparison.OrdinalIgnoreCase)
                ? ChartDisplayType.Line
                : type.Equals("Bar", StringComparison.OrdinalIgnoreCase)
                    ? ChartDisplayType.Bar
                    : ChartDisplayType.Candlestick;

            for (int i = 0; i < ChartTypeComboBox.Items.Count; i++)
            {
                if (ChartTypeComboBox.Items[i] is WpfComboBoxItem item &&
                    string.Equals(item.Tag?.ToString(), type, StringComparison.OrdinalIgnoreCase))
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
            EnforceAuthoritativeLinePlot();

            GridButton.Content = _gridVisible ? "GRID" : "GRID خاموش";
            CrosshairButton.Content = _crosshairVisible ? "Crosshair روشن" : "Crosshair خاموش";
            Chart.Refresh();
        }

        private void SaveAuthoritativeDisplayState()
        {
            try
            {
                ChartSettings settings = ChartSettingsManager.Current;
                settings.GridVisible = _gridVisible;
                settings.CrosshairVisible = _crosshairVisible;
                settings.ChartType = _chartType switch
                {
                    ChartDisplayType.Line => "Line",
                    ChartDisplayType.Bar => "Bar",
                    _ => "Candlestick"
                };
                ChartSettingsManager.Save(settings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Authoritative chart display state save failed: {ex}");
            }
        }

        private void EnforceAuthoritativeLinePlot()
        {
            if (_chartType != ChartDisplayType.Line)
                return;

            foreach (var plottable in Chart.Plot.GetPlottables().ToList())
            {
                if (plottable is ScottPlot.Plottables.Scatter scatter)
                {
                    scatter.ConnectStyle = ScottPlot.ConnectStyle.Straight;
                    scatter.Smooth = false;
                    scatter.PathStrategy = new ScottPlot.PathStrategies.Straight();
                }
            }

            Chart.Refresh();
        }
    }
}
