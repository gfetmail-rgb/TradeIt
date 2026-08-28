using System;
using System.Windows;
using System.Windows.Controls;
using WpfButton = System.Windows.Controls.Button;

namespace TradeIt.Charts
{
    public partial class ChartSettingsWindow
    {
        private static readonly bool _saveHandlerRegistered = RegisterSaveHandler();

        private static bool RegisterSaveHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartSettingsWindow),
                WpfButton.ClickEvent,
                new RoutedEventHandler(ChartSettingsSaveHandler),
                true);
            return true;
        }

        private static void ChartSettingsSaveHandler(object sender, RoutedEventArgs e)
        {
            if (sender is ChartSettingsWindow window &&
                e.OriginalSource is WpfButton button &&
                button.Content?.ToString() == "ذخیره" &&
                window.OpenChartInNewTabCheckBox != null)
            {
                window.Settings.OpenChartInNewTab =
                    window.OpenChartInNewTabCheckBox.IsChecked == true;
            }
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            OpenChartInNewTabCheckBox.IsChecked = Settings.OpenChartInNewTab;
        }
    }
}