using System.Windows;
using System.Windows.Controls;
using TradeIt.Charts;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;

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
