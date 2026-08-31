using System;
using System.Windows;

namespace TradeIt.Portfolios
{
    public partial class PortfolioManagementWindow
    {
        private static readonly bool _startupSelectionHandlerRegistered = RegisterStartupSelectionHandler();

        private static bool RegisterStartupSelectionHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(PortfolioManagementWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(PortfolioManagementWindow_UserOptionsLoaded),
                true);
            return true;
        }

        private static void PortfolioManagementWindow_UserOptionsLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not PortfolioManagementWindow window)
                return;

            window.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (window.PortfolioListBox.Items.Count == 0)
                    return;

                window.PortfolioListBox.SelectedIndex = -1;
                window._selectedPortfolio = null;
                window._symbols.Clear();
                window.SymbolsDataGrid.ItemsSource = null;
                window.ClearPortfolioDetails();
                window.StatusTextBlock.Text = "یک سبد را انتخاب کنید.";
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
    }
}
