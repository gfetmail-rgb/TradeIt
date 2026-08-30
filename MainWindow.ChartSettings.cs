using System.Windows;
using System.Windows.Controls;
using TradeIt.Charts;

namespace TradeIt
{
    public partial class MainWindow
    {
        private void ChartSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChartTabs.SelectedItem is TabItem tab && tab.Content is ChartTabView chartView)
            {
                chartView.ShowSettings();
                return;
            }

            WpfMessageBox.Show(
                "ابتدا یک نمودار را انتخاب کنید.",
                "تنظیمات چارت",
                WpfMessageBoxButton.OK,
                WpfMessageBoxImage.Information);
        }
    }
}
