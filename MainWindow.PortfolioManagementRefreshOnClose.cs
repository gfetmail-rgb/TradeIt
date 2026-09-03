using System;
using System.Windows;
using System.Windows.Controls;
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
                FrameworkElement.UnloadedEvent,
                new RoutedEventHandler(PortfolioManagementWindow_Unloaded),
                true);

            return true;
        }

        private static void PortfolioManagementWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is not PortfolioManagementWindow window ||
                window.Owner is not MainWindow mainWindow)
            {
                return;
            }

            // Execute the exact same refresh logic as the top Refresh button
            // whenever Portfolio Management is closed.
            mainWindow.RefreshPortfolioButton_Click(
                mainWindow,
                new RoutedEventArgs());
        }
    }
}
