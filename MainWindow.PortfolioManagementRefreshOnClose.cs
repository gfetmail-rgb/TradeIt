using System;
using System.Windows;
using TradeIt.Portfolios;

namespace TradeIt
{
    public partial class MainWindow
    {
        private static readonly bool _portfolioManagementRefreshOnCloseRegistered =
            RegisterPortfolioManagementRefreshOnClose();

        private static bool RegisterPortfolioManagementRefreshOnClose()
        {
            EventManager.RegisterClassHandler(
                typeof(PortfolioManagementWindow),
                Window.ClosedEvent,
                new EventHandler(PortfolioManagementWindow_Closed),
                true);

            return true;
        }

        private static void PortfolioManagementWindow_Closed(object? sender, EventArgs e)
        {
            if (sender is not PortfolioManagementWindow window ||
                window.Owner is not MainWindow mainWindow)
            {
                return;
            }

            // The management window may have deleted portfolios or symbols even
            // when the dialog result is not true. Always execute the same refresh
            // logic used by the top Refresh button after the window closes.
            mainWindow.RefreshPortfolioButton_Click(
                mainWindow,
                new RoutedEventArgs());
        }
    }
}
