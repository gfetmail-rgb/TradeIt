using System;
using System.Linq;
using System.Windows;

namespace TradeIt
{
    public partial class MainWindow
    {
        private static readonly bool _userOptionsStartupHandlerRegistered = RegisterUserOptionsStartupHandler();

        private static bool RegisterUserOptionsStartupHandler()
        {
            EventManager.RegisterClassHandler(typeof(MainWindow), FrameworkElement.LoadedEvent, new RoutedEventHandler(MainWindow_UserOptionsLoaded), true);
            return true;
        }

        private static void MainWindow_UserOptionsLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not MainWindow window)
                return;

            window.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (window.PortfolioComboBox.Items.Count > 0)
                    window.PortfolioComboBox.SelectedIndex = -1;

                window._selectedPortfolio = null;
                window._allSymbols.Clear();
                window.SymbolsDataGrid.ItemsSource = null;
                window.SymbolsDataGrid.SelectedItem = null;
                window.CloseAllChartTabs();
                window.StopAutoScroll();
                window.StatusTextBlock.Text = "یک سبد را انتخاب کنید.";
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
    }
}