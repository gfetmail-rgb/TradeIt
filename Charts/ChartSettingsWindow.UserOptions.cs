using System;
using System.Windows;
using System.Windows.Controls;

namespace TradeIt.Charts
{
    public partial class ChartSettingsWindow
    {
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (OpenChartInNewTabCheckBox != null)
                OpenChartInNewTabCheckBox.IsChecked = Settings.OpenChartInNewTab;
        }

        private void SaveChartOpeningOption()
        {
            if (OpenChartInNewTabCheckBox != null)
                Settings.OpenChartInNewTab = OpenChartInNewTabCheckBox.IsChecked == true;
        }
    }
}