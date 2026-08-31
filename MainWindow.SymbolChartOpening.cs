using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TradeIt.Charts;
using TradeIt.Models;

namespace TradeIt
{
    public partial class MainWindow
    {
        private async void SymbolNameTextBlock_ClickBySetting(object sender, MouseButtonEventArgs e)
        {
            if (_suppressSymbolSelection)
                return;

            if (sender is not FrameworkElement element ||
                element.DataContext is not SymbolInfo symbol ||
                _selectedPortfolio == null)
                return;

            e.Handled = true;

            ChartSettings settings = ChartSettingsManager.Current;

            if (settings.OpenChartInNewTab)
            {
                await OpenChartTabAsync(symbol, _selectedPortfolio, false);
            }
            else
            {
                await OpenSharedChartTabAsync(symbol, _selectedPortfolio);
            }
        }
    }
}
