using System;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartSettingsWindow
    {
        private static readonly bool _userOptionSaveHandlerRegistered = RegisterUserOptionSaveHandler();

        private static bool RegisterUserOptionSaveHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartSettingsWindow),
                System.Windows.Controls.Button.ClickEvent,
                new RoutedEventHandler(UserOptionSaveHandler));
            return true;
        }

        private static void UserOptionSaveHandler(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button ||
                button.Content?.ToString() != "ذخیره")
                return;

            if (Window.GetWindow(button) is ChartSettingsWindow window &&
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