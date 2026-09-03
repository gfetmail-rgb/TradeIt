using System;
using System.Windows;
using WpfButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _displayStatePersistenceRegistered = RegisterDisplayStatePersistence();
        private bool _displayStatePersistenceInitialized;

        private static bool RegisterDisplayStatePersistence()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(DisplayStatePersistence_Loaded));
            EventManager.RegisterClassHandler(typeof(ChartTabView), WpfButtonBase.ClickEvent, new RoutedEventHandler(DisplayStatePersistence_Click));
            EventManager.RegisterClassHandler(typeof(ChartTabView), WpfComboBox.SelectionChangedEvent, new System.Windows.Controls.SelectionChangedEventHandler(DisplayStatePersistence_SelectionChanged));
            return true;
        }

        private static void DisplayStatePersistence_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart) return;
            chart.InitializeDisplayStatePersistence();
        }

        private void InitializeDisplayStatePersistence()
        {
            if (_displayStatePersistenceInitialized) return;
            _displayStatePersistenceInitialized = true;

            ChartSettings settings = ChartSettingsManager.Current;
            _settings = settings;
            _gridVisible = settings.GridVisible;
            _crosshairVisible = settings.CrosshairVisible;

            if (!string.IsNullOrWhiteSpace(settings.ChartType))
            {
                for (int i = 0; i < ChartTypeComboBox.Items.Count; i++)
                {
                    if (ChartTypeComboBox.Items[i] is WpfComboBoxItem item &&
                        string.Equals(item.Tag?.ToString(), settings.ChartType, StringComparison.OrdinalIgnoreCase))
                    {
                        ChartTypeComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }

            ApplyStoredChartSettings();
            CrosshairButton.Content = _crosshairVisible ? "Crosshair روشن" : "Crosshair خاموش";
            GridButton.Content = _gridVisible ? "GRID" : "GRID خاموش";
        }

        private static void DisplayStatePersistence_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (sender is not ChartTabView chart || e.OriginalSource != chart.ChartTypeComboBox || chart.ChartTypeComboBox.SelectedItem is not WpfComboBoxItem item)
                return;

            string chartType = item.Tag?.ToString() ?? "Candlestick";
            if (!chart._displayStatePersistenceInitialized) return;

            try
            {
                ChartSettings settings = ChartSettingsManager.Current;
                settings.ChartType = chartType;
                settings.HasUserSavedSettings = true;
                ChartSettingsManager.Save(settings);
            }
            catch { }
        }

        private static void DisplayStatePersistence_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not System.Windows.Controls.Button button) return;
            if (button.Name != nameof(CrosshairButton) && button.Name != nameof(GridButton)) return;
            if (sender is ChartTabView chart) chart.SaveCurrentDisplayState();
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
            catch { }
        }

        private void ApplyGridDisplayState()
        {
            SetGridVisibility(Chart, _gridVisible);
        }

        private void ApplyCrosshairDisplayState()
        {
            if (_crosshair != null)
                _crosshair.IsVisible = _crosshairVisible && _chartVisible && _crosshairMouseInside;
        }
    }
}
