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

            // Every newly opened chart always starts with the requested defaults:
            // Candlestick, GRID hidden, Crosshair hidden.
            _settings = ChartSettingsManager.Current;
            _gridVisible = false;
            _crosshairVisible = false;
            _chartType = ChartDisplayType.Candlestick;

            if (ChartTypeComboBox.SelectedIndex != 0)
                ChartTypeComboBox.SelectedIndex = 0;

            _displayStatePersistenceInitialized = true;

            ApplyStoredChartSettings();
            UpdateDisplayStateButtons();
            AttachDisplayStatePersistenceHandlers();
        }

        private void AttachDisplayStatePersistenceHandlers()
        {
            if (_displayStatePersistenceHandlersAttached)
                return;

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
