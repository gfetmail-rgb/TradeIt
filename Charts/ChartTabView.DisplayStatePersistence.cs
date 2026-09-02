using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

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

            // Click bubbles from the three display-state buttons to ChartTabView.
            // This class handler runs after the button's own Click handler, so the
            // new in-memory state is saved, not the previous state.
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                ButtonBase.ClickEvent,
                new RoutedEventHandler(DisplayStatePersistence_Click));

            return true;
        }

        private static void DisplayStatePersistence_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            chart.InitializeDisplayStatePersistence();
            chart.AlignVolumeDataRectToPrice();
            if (chart._volumeVisible)
                chart.VolumeChart.Refresh();
        }

        private void InitializeDisplayStatePersistence()
        {
            if (_displayStatePersistenceInitialized)
                return;

            _displayStatePersistenceInitialized = true;

            // ChartSettingsManager.Current is the single global source of truth.
            // Every newly opened chart reads the last state saved by the user.
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
        }

        private static void DisplayStatePersistence_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not Button button)
                return;

            if (button.Name != nameof(CrosshairButton) &&
                button.Name != nameof(VolumeButton) &&
                button.Name != nameof(GridButton))
                return;

            if (sender is ChartTabView chart)
                chart.SaveCurrentDisplayState();
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
