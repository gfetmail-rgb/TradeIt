using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TradeIt.Charts;
using TradeIt.Models;
using TradeIt.Services;

using WpfButton = System.Windows.Controls.Button;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace TradeIt
{
    public partial class MainWindow
    {
        private readonly AutoScrollController _autoScrollController = new();
        private bool _autoScrollLoading;
        private TabItem? _autoScrollTab;

        private async void AutoScrollButton_Order2_Click(object sender, RoutedEventArgs e)
        {
            if (_autoScrollController.IsRunning) { StopAutoScroll(); return; }
            await StartAutoScrollAsync();
        }

        private async Task StartAutoScrollAsync()
        {
            if (_selectedPortfolio == null || _allSymbols.Count == 0)
            {
                System.Windows.MessageBox.Show("هیچ نمادی برای Auto Scroll وجود ندارد.", "Auto Scroll", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!TryReadAutoScrollInterval(out int intervalMilliseconds)) return;

            int selectedIndex = SymbolsDataGrid.SelectedItem is SymbolInfo selected ? _allSymbols.IndexOf(selected) : -1;
            int initialIndex = selectedIndex >= 0 ? selectedIndex : 0;
            _autoScrollLoading = false;
            RefreshSymbolsButton.IsEnabled = false;
            DeleteSymbolsButton.IsEnabled = false;
            MakeWatchButton.IsEnabled = false;
            AutoScrollButton.Content = "Stop";
            EnsureAutoScrollTab();
            _autoScrollController.Start(_allSymbols.Count, initialIndex, intervalMilliseconds, ShowAutoScrollSymbolAsync);
            await ShowAutoScrollSymbolAsync();
        }

        private bool TryReadAutoScrollInterval(out int milliseconds)
        {
            milliseconds = 0;
            string text = AutoScrollIntervalTextBox.Text.Trim();
            if (!int.TryParse(text, out milliseconds) || milliseconds < 1)
            {
                System.Windows.MessageBox.Show("زمان Auto Scroll باید یک عدد صحیح بزرگ‌تر از صفر و بر حسب میلی‌ثانیه باشد.", "Auto Scroll", MessageBoxButton.OK, MessageBoxImage.Warning);
                AutoScrollIntervalTextBox.Focus();
                AutoScrollIntervalTextBox.SelectAll();
                return false;
            }
            return true;
        }

        private void EnsureAutoScrollTab()
        {
            if (_autoScrollTab != null && ChartTabs.Items.Contains(_autoScrollTab)) { ChartTabs.SelectedItem = _autoScrollTab; return; }
            _autoScrollTab = ChartTabs.Items.OfType<TabItem>().FirstOrDefault(x => x.Tag is string tag && tag == "__AUTO_SCROLL__");
            if (_autoScrollTab == null)
            {
                _autoScrollTab = new TabItem { Tag = "__AUTO_SCROLL__", Header = CreateAutoScrollTabHeader("Auto Scroll") };
                ChartTabs.Items.Add(_autoScrollTab);
            }
            ChartTabs.SelectedItem = _autoScrollTab;
        }

        private object CreateAutoScrollTabHeader(string title)
        {
            var panel = new WpfStackPanel { Orientation = WpfOrientation.Horizontal };
            var text = new WpfTextBlock { Text = title, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            var close = new WpfButton { Content = "×", Width = 22, Height = 22, Padding = new Thickness(0), FontWeight = FontWeights.Bold, ToolTip = "بستن نمودار Auto Scroll" };
            close.Click += (_, _) =>
            {
                StopAutoScroll();
                if (_autoScrollTab != null && ChartTabs.Items.Contains(_autoScrollTab)) ChartTabs.Items.Remove(_autoScrollTab);
                _autoScrollTab = null;
            };
            panel.Children.Add(text);
            panel.Children.Add(close);
            return panel;
        }

        private async Task ShowAutoScrollSymbolAsync()
        {
            if (!_autoScrollController.IsRunning || _autoScrollLoading || _selectedPortfolio == null) return;
            int index = _autoScrollController.CurrentIndex;
            if (index < 0 || index >= _allSymbols.Count) return;
            _autoScrollLoading = true;
            try
            {
                SymbolInfo symbol = _allSymbols[index];
                _suppressSymbolSelection = true;
                try { SymbolsDataGrid.SelectedItem = symbol; SymbolsDataGrid.ScrollIntoView(symbol); }
                finally { _suppressSymbolSelection = false; }

                List<MarketBar> bars = await Task.Run(() => _symbolDataService.LoadBars(symbol, _selectedPortfolio));
                if (!_autoScrollController.IsRunning) return;
                if (bars.Count == 0) { StatusTextBlock.Text = $"برای {symbol.Symbol} داده‌ای پیدا نشد."; return; }

                var chartView = new ChartTabView(symbol, bars);
                EnsureAutoScrollTab();
                _autoScrollTab!.Content = chartView;
                _autoScrollTab.Header = CreateAutoScrollTabHeader($"{symbol.DisplayName} — Auto Scroll");
                ChartTabs.SelectedItem = _autoScrollTab;
                StatusTextBlock.Text = $"{symbol.Symbol} — {bars.Count:N0} کندل";
            }
            catch (Exception ex)
            {
                StopAutoScroll();
                System.Windows.MessageBox.Show(ex.ToString(), "خطا در Auto Scroll", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { _autoScrollLoading = false; }
        }

        private void StopAutoScroll()
        {
            _autoScrollController.Stop();
            _autoScrollLoading = false;
            if (AutoScrollButton != null) AutoScrollButton.Content = "Auto Scroll";
            if (RefreshSymbolsButton != null) RefreshSymbolsButton.IsEnabled = true;
            if (DeleteSymbolsButton != null) DeleteSymbolsButton.IsEnabled = true;
            if (MakeWatchButton != null) MakeWatchButton.IsEnabled = true;
        }
    }
}