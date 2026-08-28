using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TradeIt.Charts;
using TradeIt.Models;

namespace TradeIt
{
    public partial class MainWindow
    {
        public static readonly DependencyProperty IsAutoScrollActiveProperty =
            DependencyProperty.Register(nameof(IsAutoScrollActive), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool IsAutoScrollActive
        {
            get => (bool)GetValue(IsAutoScrollActiveProperty);
            set => SetValue(IsAutoScrollActiveProperty, value);
        }

        private DispatcherTimer? _order2AutoScrollTimer;
        private bool _order2AutoScrollRunning;
        private bool _order2AutoScrollLoading;
        private int _order2AutoScrollIndex = -1;
        private TabItem? _order2AutoScrollTab;

        private async void AutoScrollButton_Order2_Click(object sender, RoutedEventArgs e)
        {
            if (_order2AutoScrollRunning) { StopOrder2AutoScroll(); return; }
            await StartOrder2AutoScrollAsync();
        }

        private async Task StartOrder2AutoScrollAsync()
        {
            if (_selectedPortfolio == null || _allSymbols.Count == 0)
            {
                System.Windows.MessageBox.Show("هیچ نمادی برای Auto Scroll وجود ندارد.", "Auto Scroll", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!TryReadAutoScrollInterval(out int intervalMilliseconds)) return;

            int selectedIndex = -1;
            if (SymbolsDataGrid.SelectedItem is SymbolInfo selected) selectedIndex = _allSymbols.IndexOf(selected);
            _order2AutoScrollIndex = selectedIndex >= 0 ? selectedIndex : 0;
            _order2AutoScrollRunning = true;
            _order2AutoScrollLoading = false;
            IsAutoScrollActive = true;
            RefreshSymbolsButton.IsEnabled = false;
            DeleteSymbolsButton.IsEnabled = false;
            MakeWatchButton.IsEnabled = false;
            AutoScrollButton.Content = "Stop";

            EnsureOrder2AutoScrollTab();
            await ShowOrder2AutoScrollSymbolAsync();
            if (!_order2AutoScrollRunning) return;

            _order2AutoScrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(intervalMilliseconds) };
            _order2AutoScrollTimer.Tick += Order2AutoScrollTimer_Tick;
            _order2AutoScrollTimer.Start();
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

        private void EnsureOrder2AutoScrollTab()
        {
            if (_order2AutoScrollTab != null && ChartTabs.Items.Contains(_order2AutoScrollTab))
            {
                ChartTabs.SelectedItem = _order2AutoScrollTab;
                return;
            }
            _order2AutoScrollTab = ChartTabs.Items.OfType<TabItem>().FirstOrDefault(x => x.Tag is string tag && tag == "__AUTO_SCROLL__");
            if (_order2AutoScrollTab == null)
            {
                _order2AutoScrollTab = new TabItem { Tag = "__AUTO_SCROLL__", Header = CreateAutoScrollTabHeader("Auto Scroll") };
                ChartTabs.Items.Add(_order2AutoScrollTab);
            }
            ChartTabs.SelectedItem = _order2AutoScrollTab;
        }

        private object CreateAutoScrollTabHeader(string title)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var text = new TextBlock { Text = title, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            var close = new Button { Content = "×", Width = 22, Height = 22, Padding = new Thickness(0), FontWeight = FontWeights.Bold, ToolTip = "بستن نمودار Auto Scroll" };
            close.Click += (_, _) =>
            {
                StopOrder2AutoScroll();
                if (_order2AutoScrollTab != null && ChartTabs.Items.Contains(_order2AutoScrollTab)) ChartTabs.Items.Remove(_order2AutoScrollTab);
                _order2AutoScrollTab = null;
            };
            panel.Children.Add(text);
            panel.Children.Add(close);
            return panel;
        }

        private async void Order2AutoScrollTimer_Tick(object? sender, EventArgs e)
        {
            if (!_order2AutoScrollRunning || _order2AutoScrollLoading) return;
            _order2AutoScrollIndex++;
            if (_order2AutoScrollIndex >= _allSymbols.Count) { StopOrder2AutoScroll(); return; }
            await ShowOrder2AutoScrollSymbolAsync();
        }

        private async Task ShowOrder2AutoScrollSymbolAsync()
        {
            if (!_order2AutoScrollRunning || _order2AutoScrollLoading || _selectedPortfolio == null || _order2AutoScrollIndex < 0 || _order2AutoScrollIndex >= _allSymbols.Count) return;
            _order2AutoScrollLoading = true;
            try
            {
                SymbolInfo symbol = _allSymbols[_order2AutoScrollIndex];
                _suppressSymbolSelection = true;
                try { SymbolsDataGrid.SelectedItem = symbol; SymbolsDataGrid.ScrollIntoView(symbol); }
                finally { _suppressSymbolSelection = false; }

                List<MarketBar> bars = await Task.Run(() => _symbolDataService.LoadBars(symbol, _selectedPortfolio));
                if (!_order2AutoScrollRunning) return;
                if (bars.Count == 0) { StatusTextBlock.Text = $"برای {symbol.Symbol} داده‌ای پیدا نشد."; return; }

                var chartView = new ChartTabView(symbol, bars);
                EnsureOrder2AutoScrollTab();
                _order2AutoScrollTab!.Content = chartView;
                _order2AutoScrollTab.Header = CreateAutoScrollTabHeader($"{symbol.DisplayName} — Auto Scroll");
                ChartTabs.SelectedItem = _order2AutoScrollTab;
                StatusTextBlock.Text = $"{symbol.Symbol} — {bars.Count:N0} کندل";
            }
            catch (Exception ex)
            {
                StopOrder2AutoScroll();
                System.Windows.MessageBox.Show(ex.ToString(), "خطا در Auto Scroll", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { _order2AutoScrollLoading = false; }
        }

        private void StopOrder2AutoScroll()
        {
            _order2AutoScrollRunning = false;
            _order2AutoScrollLoading = false;
            _order2AutoScrollIndex = -1;
            IsAutoScrollActive = false;
            if (_order2AutoScrollTimer != null)
            {
                _order2AutoScrollTimer.Stop();
                _order2AutoScrollTimer.Tick -= Order2AutoScrollTimer_Tick;
                _order2AutoScrollTimer = null;
            }
            if (AutoScrollButton != null) AutoScrollButton.Content = "Auto Scroll";
            if (RefreshSymbolsButton != null) RefreshSymbolsButton.IsEnabled = true;
            if (DeleteSymbolsButton != null) DeleteSymbolsButton.IsEnabled = true;
            if (MakeWatchButton != null) MakeWatchButton.IsEnabled = true;
        }
    }
}