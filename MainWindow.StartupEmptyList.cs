using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TradeIt
{
    public partial class MainWindow
    {
        private bool _startupEmptyListPending;

        private static readonly bool _startupHandlerRegistered = RegisterStartupHandler();

        private static bool RegisterStartupHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(MainWindow),
                LoadedEvent,
                new RoutedEventHandler(MainWindow_StartupEmptyListLoaded),
                true);

            return true;
        }

        private static void MainWindow_StartupEmptyListLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not MainWindow window)
                return;

            window._startupEmptyListPending = true;

            window.Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new System.Action(() => window.ClearStartupPortfolioSelection()));
        }

        private void ClearStartupPortfolioSelection()
        {
            if (!_startupEmptyListPending)
                return;

            _startupEmptyListPending = false;

            PortfolioComboBox.SelectedIndex = -1;
            _selectedPortfolio = null;
            _allSymbols.Clear();
            SymbolsDataGrid.ItemsSource = null;
            SymbolsDataGrid.SelectedItem = null;
            SymbolSearchTextBox.Clear();
            CloseAllChartTabs();
            StatusTextBlock.Text = "برای شروع، یک سبد را انتخاب کنید.";
        }
    }
}
