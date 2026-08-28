using System;
using System.Windows;
using System.Windows.Input;
using TradeIt.Charts;
using TradeIt.Models;

namespace TradeIt
{
    public partial class MainWindow
    {
        private static readonly bool _userOptionsStartupHandlerRegistered = RegisterUserOptionsStartupHandler();

        private static bool RegisterUserOptionsStartupHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(MainWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(MainWindow_UserOptionsLoaded),
                true);
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

        private async void SymbolNameTextBlock_ClickBySetting(object sender, MouseButtonEventArgs e)
        {
            await TaskProxy(sender, e);
        }

        private static async System.Threading.Tasks.Task TaskProxy(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.DataContext is not SymbolInfo symbol ||
                Window.GetWindow(element) is not MainWindow window ||
                window._selectedPortfolio == null)
                return;

            e.Handled = true;

            ChartSettings settings = ChartSettingsManager.Current;
            if (settings.OpenChartInNewTab)
            {
                await window.OpenChartTabAsync(symbol, window._selectedPortfolio, false);
            }
            else
            {
                await window.OpenSharedChartTabAsync(symbol, window._selectedPortfolio);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ChartSettingsWindow(ChartSettingsManager.Current)
            {
                Owner = this
            };

            if (window.ShowDialog() == true)
                ChartSettingsManager.SetDefaults(window.Settings);
        }
    }
}