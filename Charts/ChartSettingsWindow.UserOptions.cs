using System;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartSettingsWindow
    {
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            OpenChartInNewTabCheckBox.IsChecked = Settings.OpenChartInNewTab;
        }

        private void OpenChartInNewTab_Changed(object sender, RoutedEventArgs e)
        {
            if (OpenChartInNewTabCheckBox != null)
                Settings.OpenChartInNewTab = OpenChartInNewTabCheckBox.IsChecked == true;
        }
    }
}