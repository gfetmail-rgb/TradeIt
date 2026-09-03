using System;
using System.Windows;
using WpfButton = System.Windows.Controls.Button;
using WpfButtonBase = System.Windows.Controls.Primitives.ButtonBase;
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
            EventManager.RegisterClassHandler(typeof(ChartTabView), System.Windows.Controls.ComboBox.SelectionChangedEvent, new WpfSelectionChangedEventHandler(DisplayStatePersistence_SelectionChanged), true);
            return true;
        }

        private static void DisplayStatePersistence_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.InitializeDisplayStatePersistence();
        }

        private void InitializeDisplayStatePersistence()
        {
            if (_displayStatePersistenceInitialized)
                return;

            ChartSettings settings = ChartSettingsManager.Current;
            _settings = settings;
            _gridVisible = settings.GridVisible;
            _crosshairVisible = settings.CrosshairVisible;
            _chartType = ParseChartDisplayType(settings.ChartType);

            int targetIndex = FindChartTypeIndex(settings.ChartType);
            if (ChartTypeComboBox.SelectedIndex != targetIndex)
                ChartTypeComboBox.SelectedIndex = targetIndex;

            // The guard is enabled only after all constructor-time control
            // initialization is complete, so a new chart can never overwrite
            // the persisted global state merely by setting its default selection.
            _displayStatePersistenceInitialized = true;

            ApplyStoredChartSettings();
            UpdateDisplayStateButtons();
        }

        private static void DisplayStatePersistence_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart || !chart._displayStatePersistenceInitialized)
                return;

            if (e.OriginalSource is WpfButton button &&
                (ReferenceEquals(button, chart.CrosshairButton) || ReferenceEquals(button, chart.GridButton)))
            {
                chart.SaveCurrentDisplayState();
            }
        }

        private static void DisplayStatePersistence_SelectionChanged(object sender, WpfSelectionChangedEventArgs e)
        {
            if (sender is not ChartTabView chart || !chart._displayStatePersistenceInitialized)
                return;
            if (!ReferenceEquals(e.OriginalSource, chart.ChartTypeComboBox))
                return;
            if (chart.ChartTypeComboBox.SelectedItem is not WpfComboBoxItem item)
                return;

            string type = item.Tag?.ToString() ?? "Candlestick";
            chart._chartType = ParseChartDisplayType(type);
            chart.SaveCurrentDisplayState();

            if (chart._bars.Count > 0)
                chart.DrawChart();
            else
                chart.ApplyChartVisualSettingsOnly();
        }

        private static ChartDisplayType ParseChartDisplayType(string? value) =>
            value?.Trim().ToLowerInvariant() switch
            {
                "line" => ChartDisplayType.Line,
                "bar" => ChartDisplayType.Bar,
                _ => ChartDisplayType.Candlestick
            };

        private int FindChartTypeIndex(string? value)
        {
            string persisted = string.IsNullOrWhiteSpace(value) ? "Candlestick" : value.Trim();
            for (int i = 0; i < ChartTypeComboBox.Items.Count; i++)
            {
                if (ChartTypeComboBox.Items[i] is WpfComboBoxItem item &&
                    string.Equals(item.Tag?.ToString(), persisted, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return 0;
        }

        private void SaveCurrentDisplayState()
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
                System.Diagnostics.Debug.WriteLine($"Chart display state save failed: {ex}");
            }
        }

        private void UpdateDisplayStateButtons()
        {
            CrosshairButton.Content = _crosshairVisible ? "Crosshair روشن" : "Crosshair خاموش";
            GridButton.Content = _gridVisible ? "GRID" : "GRID خاموش";
        }

        private void ApplyGridDisplayState() => SetGridVisibility(Chart, _gridVisible);

        private void ApplyCrosshairDisplayState()
        {
            if (_crosshair != null)
                _crosshair.IsVisible = _crosshairVisible && _chartVisible && _crosshairMouseInside;
        }
    }
}
