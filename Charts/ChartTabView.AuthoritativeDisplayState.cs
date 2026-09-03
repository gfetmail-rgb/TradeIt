using System;
using System.Windows;
using System.Windows.Controls;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;

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
                Button.ClickEvent,
                new RoutedEventHandler(AuthoritativeDisplayState_ButtonClick),
                true);

            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                ComboBox.SelectionChangedEvent,
                new SelectionChangedEventHandler(AuthoritativeDisplayState_SelectionChanged),
                true);

            return true;
        }

        private static void AuthoritativeDisplayState_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            chart.Dispatcher.BeginInvoke(
                new Action(chart.ApplyAuthoritativeDisplayState),
                System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private static void AuthoritativeDisplayState_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            if (e.OriginalSource is Button button &&
                (ReferenceEquals(button, chart.GridButton) ||
                 ReferenceEquals(button, chart.CrosshairButton)))
            {
                chart.SaveAuthoritativeDisplayState();
            }
        }

        private static void AuthoritativeDisplayState_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ChartTabView chart ||
                !ReferenceEquals(e.OriginalSource, chart.ChartTypeComboBox))
                return;

            if (chart.ChartTypeComboBox.SelectedItem is WpfComboBoxItem item)
            {
                string type = item.Tag?.ToString() ?? string.Empty;
                chart._chartType = type.ToLowerInvariant() switch
                {
                    "line" => ChartDisplayType.Line,
                    "bar" => ChartDisplayType.Bar,
                    _ => ChartDisplayType.Candlestick
                };
            }

            chart.SaveAuthoritativeDisplayState();
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

            _chartType = type.ToLowerInvariant() switch
            {
                "line" => ChartDisplayType.Line,
                "bar" => ChartDisplayType.Bar,
                _ => ChartDisplayType.Candlestick
            };

            int targetIndex = 0;
            for (int i = 0; i < ChartTypeComboBox.Items.Count; i++)
            {
                if (ChartTypeComboBox.Items[i] is WpfComboBoxItem item &&
                    string.Equals(item.Tag?.ToString(), type, StringComparison.OrdinalIgnoreCase))
                {
                    targetIndex = i;
                    break;
                }
            }

            if (ChartTypeComboBox.SelectedIndex != targetIndex)
                ChartTypeComboBox.SelectedIndex = targetIndex;

            if (_bars.Count > 0)
                DrawChart();

            SetGridVisibility(Chart, _gridVisible);
            if (_crosshair != null)
                _crosshair.IsVisible = _crosshairVisible && _chartVisible && _crosshairMouseInside;

            GridButton.Content = _gridVisible ? "GRID" : "GRID خاموش";
            CrosshairButton.Content = _crosshairVisible ? "Crosshair روشن" : "Crosshair خاموش";

            EnforceAuthoritativeLineRendering();
            Chart.Refresh();
        }

        private void EnforceAuthoritativeLineRendering()
        {
            if (_chartType != ChartDisplayType.Line)
                return;

            foreach (var plottable in Chart.Plot.GetPlottables())
            {
                if (plottable is ScottPlot.Plottables.Scatter scatter)
                {
                    scatter.ConnectStyle = ScottPlot.ConnectStyle.Straight;
                    scatter.Smooth = false;
                    scatter.PathStrategy = new ScottPlot.PathStrategies.Straight();
                    scatter.MarkerSize = 0;
                    scatter.LineWidth = (float)Math.Max(0.01, _settings.LineWidth);
                    scatter.Color = ScottPlot.Color.FromHtml(_settings.LineColor);
                }
            }
        }

        private void SaveAuthoritativeDisplayState()
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
    }
}
