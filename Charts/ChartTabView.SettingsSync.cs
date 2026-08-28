using System;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _settingsSyncHandlerRegistered = RegisterSettingsSyncHandler();

        private static bool RegisterSettingsSyncHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(ChartTabView_Loaded));
            return true;
        }

        private static void ChartTabView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            ChartSettingsManager.SettingsChanged -= chart.ChartSettingsManager_SettingsChanged;
            ChartSettingsManager.SettingsChanged += chart.ChartSettingsManager_SettingsChanged;
            chart.ApplyPersistedAppearance();
        }
    }
}
