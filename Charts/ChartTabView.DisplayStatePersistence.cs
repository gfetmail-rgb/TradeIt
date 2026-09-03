using System;
using System.Windows;
using WpfButton = System.Windows.Controls.Button;
using WpfButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;
using WpfSelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
using WpfSelectionChangedEventHandler = System.Windows.Controls.SelectionChangedEventHandler;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _displayStatePersistenceRegistered = RegisterDisplayStatePersistence();
        private bool _displayStatePersistenceInitialized;

        private static bool RegisterDisplayStatePersistence()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(DisplayStatePersistence_Loaded));
            EventManager.RegisterClassHandler(typeof(ChartTabView), WpfButtonBase.ClickEvent, new RoutedEventHandler(DisplayStatePersistence_Click), true);
            EventManager.RegisterClassHandler(typeof(ChartTabView), WpfComboBox.SelectionChangedEvent, new WpfSelectionChangedEventHandler(DisplayStatePersistence_SelectionChanged), true);
            return true;
        }

        private static void DisplayStatePersistence_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.InitializeDisplayStatePersistence();
        }

        private void InitializeDisplayStatePersistence()
        {
            if (_displayStatePersistenceInitialized) return;

            ChartSettings settings = ChartSettingsManager.Current;
            _settings = settings;
            _gridVisible = settings.GridVisible;
            _crosshairVisible = settings.CrosshairVisible;

            string persistedChartType = string.IsNullOrWhiteSpace(settings.ChartType) ? "Candlestick" : settings.ChartType;
            _chartType = persistedChartType.Trim().ToLowerInvariant() switch
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
                    ChartTypeComboBox.SelectedIndex = i;
                    break;
                }
            }

            _displayStatePersistenceInitialized = true;
            ApplyStoredChartSettings();
            CrosshairButton.Content = _crosshairVisible ? "Crosshair روشن" : "Crosshair خاموش";
            GridButton.Content = _gridVisible ? "GRID" : "GRID خاموش";
        }

        private static void DisplayStatePersistence_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart || !chart._displayStatePersistenceInitialized)
                return;

            if (e.OriginalSource is WpfButton button &&
                (button.Name == nameof(CrosshairButton) || button.Name == nameof(GridButton)))
            {
                chart.Dispatcher.BeginInvoke(new Action(chart.SaveCurrentDisplayState), System.Windows.Threading.DispatcherPriority.DataBind);
            }
        }

        private static void DisplayStatePersistence_SelectionChanged(object sender, WpfSelectionChangedEventArgs e)
        {
            if (sender is not ChartTabView chart || !chart._displayStatePersistenceInitialized)
                return;
            if (!ReferenceEquals(e.OriginalSource, chart.ChartTypeComboBox))
                return;

            chart.Dispatcher.BeginInvoke(new Action(chart.SaveCurrentDisplayState), System.Windows.Threading.DispatcherPriority.DataBind);
        }

        private void SaveCurrentDisplayState()
        {
            try
            {
                ChartSettings settings = ChartSettingsManager.Current;
                settings.GridVisible = _gridVisible;
                settings.CrosshairVisible = _crosshairVisible;
                settings.ChartType = ChartTypeComboBox.SelectedItem is WpfComboBoxItem item
                    ? item.Tag?.ToString() ?? _chartType.ToString()
                    : _chartType.ToString();
                ChartSettingsManager.Save(settings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chart display state save failed: {ex}");
            }
        }

        private void ApplyGridDisplayState() => SetGridVisibility(Chart, _gridVisible);

        private void ApplyCrosshairDisplayState()
        {
            if (_crosshair != null)
                _crosshair.IsVisible = _crosshairVisible && _chartVisible && _crosshairMouseInside;
        }
    }
}
