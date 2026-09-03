using System;
using System.Windows;
using System.Windows.Controls;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _displayStatePersistenceRegistered = RegisterDisplayStatePersistence();
        private bool _displayStatePersistenceInitialized;
        private bool _displayStatePersistenceHandlersAttached;

        private static bool RegisterDisplayStatePersistence()
        {
            // Initialization is handled at the class level. User-action
            // persistence is attached to each control after initialization so
            // it runs AFTER the XAML instance handlers have changed the state.
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DisplayStatePersistence_Loaded));
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

            // Enable persistence only after the persisted state has been loaded.
            // Constructor/XAML default values can therefore never overwrite it.
            _displayStatePersistenceInitialized = true;

            ApplyStoredChartSettings();
            UpdateDisplayStateButtons();
            AttachDisplayStatePersistenceHandlers();
        }

        private void AttachDisplayStatePersistenceHandlers()
        {
            if (_displayStatePersistenceHandlersAttached)
                return;

            // These handlers are attached after the XAML handlers. WPF invokes
            // them after the existing instance handlers, so they save the NEW
            // value rather than the value from immediately before the click.
            GridButton.Click += DisplayStateGridButton_ClickAfterStateChange;
            CrosshairButton.Click += DisplayStateCrosshairButton_ClickAfterStateChange;
            ChartTypeComboBox.SelectionChanged += DisplayStateChartType_SelectionChangedAfterStateChange;
            _displayStatePersistenceHandlersAttached = true;
        }

        private void DisplayStateGridButton_ClickAfterStateChange(object sender, RoutedEventArgs e)
        {
            SaveCurrentDisplayState();
        }

        private void DisplayStateCrosshairButton_ClickAfterStateChange(object sender, RoutedEventArgs e)
        {
            SaveCurrentDisplayState();
        }

        private void DisplayStateChartType_SelectionChangedAfterStateChange(object sender, SelectionChangedEventArgs e)
        {
            if (ReferenceEquals(e.OriginalSource, ChartTypeComboBox))
                SaveCurrentDisplayState();
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
                if (ChartTypeComboBox.Items[i] is ComboBoxItem item &&
                    string.Equals(item.Tag?.ToString(), persisted, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return 0;
        }

        private void SaveCurrentDisplayState()
        {
            if (!_displayStatePersistenceInitialized)
                return;

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
