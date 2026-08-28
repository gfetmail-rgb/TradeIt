using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TradeIt.Charts;
using TradeIt.Models;

namespace TradeIt
{
    public partial class MainWindow
    {
        private static readonly bool _symbolListButtonHandlerRegistered = RegisterSymbolListButtonHandler();

        private static bool RegisterSymbolListButtonHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(Button),
                Button.ClickEvent,
                new RoutedEventHandler(PrepareSymbolListBeforeButtonAction));

            return true;
        }

        private static void PrepareSymbolListBeforeButtonAction(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Name != "DeleteSymbolsButton")
                return;

            if (Window.GetWindow(button) is not MainWindow window ||
                window._selectedPortfolio == null ||
                window._allSymbols == null ||
                window._allSymbols.Count == 0)
                return;

            window._selectedPortfolio.Symbols =
                window._allSymbols.Select(CloneSymbol).ToList();

            window._selectedPortfolio.UseExplicitSymbolList = true;
        }

        private static async void SymbolListTextBlockClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBlock textBlock ||
                textBlock.DataContext is not SymbolInfo symbol)
                return;

            if (Window.GetWindow(textBlock) is not MainWindow window ||
                window._selectedPortfolio == null ||
                !IsInsideSymbolsGrid(textBlock, window.SymbolsDataGrid))
                return;

            e.Handled = true;

            ChartSettings settings = ChartSettingsManager.Current;

            if (settings.OpenChartInNewTab)
            {
                await window.OpenChartTabAsync(
                    symbol,
                    window._selectedPortfolio,
                    false);
            }
            else
            {
                await window.OpenSharedChartTabAsync(
                    symbol,
                    window._selectedPortfolio);
            }
        }

        private static bool IsInsideSymbolsGrid(DependencyObject child, DependencyObject grid)
        {
            DependencyObject? current = child;

            while (current != null)
            {
                if (ReferenceEquals(current, grid))
                    return true;

                current = current is System.Windows.Media.Visual visual
                    ? System.Windows.Media.VisualTreeHelper.GetParent(visual)
                    : null;
            }

            return false;
        }

        private async Task OpenSharedChartTabAsync(SymbolInfo symbol, Portfolio portfolio)
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

                var chartView = new ChartTabView(symbol, bars);

                TabItem? sharedTab = ChartTabs.Items
                    .OfType<TabItem>()
                    .FirstOrDefault(x =>
                        string.Equals(
                            x.Tag?.ToString(),
                            "__SHARED_CHART__",
                            StringComparison.Ordinal));

                if (sharedTab == null)
                {
                    sharedTab = new TabItem { Tag = "__SHARED_CHART__" };
                    ChartTabs.Items.Add(sharedTab);
                }

                sharedTab.Header = CreateTabHeader(symbol);
                sharedTab.Content = chartView;
                ChartTabs.SelectedItem = sharedTab;

                StatusTextBlock.Text = $"{symbol.Symbol} — {bars.Count:N0} کندل";
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(
                    ex.ToString(),
                    "خطا در خواندن داده نماد",
                    WpfMessageBoxButton.OK,
                    WpfMessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private static SymbolInfo CloneSymbol(SymbolInfo source)
        {
            return new SymbolInfo
            {
                Symbol = source.Symbol,
                DisplayName = source.DisplayName,
                FilePath = source.FilePath,
                RowNumber = source.RowNumber,
                IsSelected = source.IsSelected,
                LastTradeDate = source.LastTradeDate,
                LastVolume = source.LastVolume,
                LastOpen = source.LastOpen,
                LastHigh = source.LastHigh,
                LastLow = source.LastLow,
                LastClose = source.LastClose,
                LastFinalFee = source.LastFinalFee
            };
        }
    }
}
