using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;
using TradeIt.Models;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

namespace TradeIt
{
    public partial class MainWindow
    {
        private const string SharedChartTabTag = "__SHARED_CHART__";

        // Register a class-level Preview handler so the shared mode can take
        // control before the existing TextBlock MouseLeftButtonUp handler.
        // The handler is restricted to this window's symbol grid.
        static MainWindow()
        {
            EventManager.RegisterClassHandler(
                typeof(DataGrid),
                UIElement.PreviewMouseLeftButtonUpEvent,
                new MouseButtonEventHandler(SymbolNameSharedModePreviewMouseLeftButtonUp));
        }

        private static async void SymbolNameSharedModePreviewMouseLeftButtonUp(
            object sender,
            WpfMouseButtonEventArgs e)
        {
            if (sender is not DataGrid grid ||
                grid.Name != "SymbolsDataGrid" ||
                grid.DataContext is not MainWindow window)
            {
                return;
            }

            if (!ChartSettingsManager.Current.OpenSymbolsInSharedChart)
                return;

            DependencyObject? source = e.OriginalSource as DependencyObject;
            SymbolInfo? symbol = FindSymbolFromVisualTree(source);
            if (symbol == null || window._selectedPortfolio == null)
                return;

            e.Handled = true;
            await window.OpenSharedChartAsync(symbol, window._selectedPortfolio);
        }

        private static SymbolInfo? FindSymbolFromVisualTree(DependencyObject? source)
        {
            DependencyObject? current = source;
            while (current != null)
            {
                if (current is FrameworkElement fe && fe.DataContext is SymbolInfo symbol)
                    return symbol;

                current = LogicalTreeHelper.GetParent(current);
                if (current == null && source != null)
                    current = System.Windows.Media.VisualTreeHelper.GetParent(source);
            }

            return null;
        }

        private async Task OpenSharedChartAsync(SymbolInfo symbol, Portfolio portfolio)
        {
            try
            {
                SetBusy(true, $"در حال خواندن داده‌های {symbol.Symbol} ... لطفاً صبر کنید");

                List<MarketBar> bars = await Task.Run(
                    () => _symbolDataService.LoadBars(symbol, portfolio));

                if (bars.Count == 0)
                {
                    StatusTextBlock.Text = $"برای {symbol.Symbol} داده‌ای پیدا نشد.";
                    return;
                }

                var chartView = new Charts.ChartTabView(symbol, bars);

                TabItem? sharedTab = ChartTabs.Items
                    .OfType<TabItem>()
                    .FirstOrDefault(x => x.Tag is string tag && tag == SharedChartTabTag);

                if (sharedTab == null)
                {
                    sharedTab = new TabItem
                    {
                        Tag = SharedChartTabTag,
                        Header = CreateTabHeader(symbol),
                        Content = chartView
                    };
                    ChartTabs.Items.Add(sharedTab);
                }
                else
                {
                    sharedTab.Header = CreateTabHeader(symbol);
                    sharedTab.Content = chartView;
                }

                ChartTabs.SelectedItem = sharedTab;
                StatusTextBlock.Text = $"{symbol.Symbol} — {bars.Count:N0} کندل";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "خطا در خواندن داده نماد",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }
    }
}