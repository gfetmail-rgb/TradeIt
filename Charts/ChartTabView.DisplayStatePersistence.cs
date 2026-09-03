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

            // A new chart inherits the last user-selected display state.
            _settings = ChartSettingsManager.Current;
            _gridVisible = _settings.GridVisible;
            _crosshairVisible = _settings.CrosshairVisible;
            _chartType = _settings.ChartType?.Trim().ToLowerInvariant() switch
            {
                "line" => ChartDisplayType.Line,
                "bar" => ChartDisplayType.Bar,
                _ => ChartDisplayType.Candlestick
            };

            int desiredIndex = _chartType switch
            {
                ChartDisplayType.Line => 1,
                ChartDisplayType.Bar => 2,
                _ => 0
            };

            if (ChartTypeComboBox.SelectedIndex != desiredIndex)
                ChartTypeComboBox.SelectedIndex = desiredIndex;

            _displayStatePersistenceInitialized = true;

            ApplyStoredChartSettings();
            InitializeCrosshairAtInitialPosition();
            ApplyGridDisplayState();
            ApplyCrosshairDisplayState();
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
                _crosshair.IsVisible = _crosshairVisible && _chartVisible && (_crosshairMouseInside || !_hasInitialView);
        }
    }
}