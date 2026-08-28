using System.Windows;
using System.Windows.Threading;

namespace TradeIt
{
    public partial class MainWindow
    {
        private bool _startupEmptyListPending;
        private int _startupSelectionEvents;

        private static readonly bool _startupHandlerRegistered = RegisterStartupHandler();

        private static bool RegisterStartupHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(MainWindow),
                LoadedEvent,
                new RoutedEventHandler(MainWindow_StartupEmptyListLoaded),
                true);

            EventManager.RegisterClassHandler(
                typeof(MainWindow),
                SelectionChangedEvent,
                new System.Windows.Controls.SelectionChangedEventHandler(MainWindow_StartupSelectionChanged),
                true);

            return true;
        }

        private static void MainWindow_StartupEmptyListLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not MainWindow window)
                return;

            window._startupEmptyListPending = true;
            window._startupSelectionEvents = 0;

            window.Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new System.Action(() => window.ClearStartupPortfolioSelection()));
        }

        private static void MainWindow_StartupSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (sender is not MainWindow window || !window._startupEmptyListPending)
                return;

            if (e.OriginalSource == window.PortfolioComboBox)
                window._startupSelectionEvents++;
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
