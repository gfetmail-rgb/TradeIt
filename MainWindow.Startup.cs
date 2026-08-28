using System.Windows;
using System.Windows.Threading;

namespace TradeIt
{
    public partial class MainWindow
    {
        static MainWindow()
        {
            EventManager.RegisterClassHandler(
                typeof(MainWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(MainWindow_ClassLoaded));
        }

        private static void MainWindow_ClassLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not MainWindow window)
                return;

            // LoadPortfolios() runs from the instance Loaded handler. Queue this
            // cleanup so it executes after that handler has finished populating
            // the ComboBox and symbol list.
            window.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(window.ClearStartupPortfolioSelection));
        }

        private void ClearStartupPortfolioSelection()
        {
            if (PortfolioComboBox == null || SymbolsDataGrid == null)
                return;

            PortfolioComboBox.SelectedItem = null;
            PortfolioComboBox.SelectedIndex = -1;

            _selectedPortfolio = null;
            _allSymbols.Clear();
            SymbolsDataGrid.ItemsSource = null;
            SymbolsDataGrid.SelectedItem = null;

            StatusTextBlock.Text = _portfolios.Count > 0
                ? "یک سبد را انتخاب کنید."
                : "هنوز سبدی تعریف نشده است.";
        }
    }
}
