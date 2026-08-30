using System.Windows;
using TradeIt.Charts;

namespace TradeIt
{
    public partial class MainWindow
    {
        private void ChartSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ChartSettingsWindow(ChartSettingsManager.Current)
            {
                Owner = this
            };

            if (window.ShowDialog() == true)
            {
                ChartSettingsManager.SetDefaults(window.Settings);
                ChartSettingsStore.Update(window.Settings);
                StatusTextBlock.Text = "تنظیمات چارت ذخیره شد.";
            }
        }
    }
}
