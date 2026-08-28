using System;
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
                new RoutedEventHandler(MainWindow_StartupFixLoaded));
        }

        private static void MainWindow_StartupFixLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not MainWindow window)
                return;

            window.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(window.ClearStartupSelection));
        }

        private void ClearStartupSelection()
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
