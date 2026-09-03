using System.Windows;
using TradeIt.Charts;

namespace TradeIt
{
    public partial class MainWindow
    {
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ChartSettingsWindow(ChartSettingsManager.Current)
            {
                Owner = this
            };

            // ChartSettingsWindow persists the settings itself and notifies all
            // open charts through ChartSettingsManager.SettingsChanged.
            window.ShowDialog();
        }
    }
}
