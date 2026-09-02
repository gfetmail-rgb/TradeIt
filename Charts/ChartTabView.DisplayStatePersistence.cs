using System;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _displayStatePersistenceRegistered = RegisterDisplayStatePersistence();
        private bool _displayStatePersistenceInitialized;

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
            if (sender is not ChartTabView chart)
                return;

            chart.InitializeDisplayStatePersistence();
        }

        private void InitializeDisplayStatePersistence()
        {
            if (_displayStatePersistenceInitialized)
                return;

            _displayStatePersistenceInitialized = true;

            ChartSettings settings = ChartSettingsManager.Current;
            _settings = settings;
            _gridVisible = settings.GridVisible;
            _crosshairVisible = settings.CrosshairVisible;

            ApplyGridDisplayState();
            ApplyCrosshairDisplayState();
            SetVolumeVisible(settings.VolumeVisible, false);

            CrosshairButton.Content = _crosshairVisible
                ? "Crosshair روشن"
                : "Crosshair خاموش";
            VolumeButton.Content = _volumeVisible
                ? "پنهان کردن حجم"
                : "نمایش حجم";
            GridButton.Content = _gridVisible
                ? "GRID"
                : "GRID خاموش";

            // These handlers run after the XAML Click handlers, therefore the
            // in-memory state has already been toggled when it is saved.
            CrosshairButton.Click -= DisplayStatePersistence_ButtonClick;
            VolumeButton.Click -= DisplayStatePersistence_ButtonClick;
            GridButton.Click -= DisplayStatePersistence_ButtonClick;
            CrosshairButton.Click += DisplayStatePersistence_ButtonClick;
            VolumeButton.Click += DisplayStatePersistence_ButtonClick;
            GridButton.Click += DisplayStatePersistence_ButtonClick;
        }

        private void DisplayStatePersistence_ButtonClick(object sender, RoutedEventArgs e)
        {
            SaveCurrentDisplayState();
        }

        private void SaveCurrentDisplayState()
        {
            try
            {
                ChartSettings settings = ChartSettingsManager.Current;
                settings.GridVisible = _gridVisible;
                settings.CrosshairVisible = _crosshairVisible;
                settings.VolumeVisible = _volumeVisible;
                ChartSettingsManager.Save(settings);
            }
            catch
            {
                // Display-state persistence must never prevent chart interaction.
            }
        }

        private void ApplyGridDisplayState()
        {
            SetGridVisibility(Chart, _gridVisible);
            SetGridVisibility(VolumeChart, _gridVisible);
        }

        private void ApplyCrosshairDisplayState()
        {
            if (_crosshair != null)
                _crosshair.IsVisible = _crosshairVisible && _chartVisible && _crosshairMouseInside;
        }
    }
}
