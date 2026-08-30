using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfCheckBox = System.Windows.Controls.CheckBox;

using TradeIt.Models;

namespace TradeIt
{
    public partial class MainWindow
    {
        private readonly SymbolFilterSettings _symbolFilterSettings = new();
        private readonly Dictionary<string, List<MarketBar>> _symbolFilterBars = new(StringComparer.OrdinalIgnoreCase);
        private CancellationTokenSource? _symbolFilterCts;
        private bool _symbolFiltersInitialized;
        private bool _symbolFiltersApplying;

        private WpfComboBox? _tradeStatusFilterComboBox;
        private WpfComboBox? _nameFilterComboBox;
        private WpfTextBox? _nameFilterTextBox;
        private WpfCheckBox? _daysWithoutTradeCheckBox;
        private WpfTextBox? _daysWithoutTradeTextBox;
        private WpfCheckBox? _volumeFilterCheckBox;
        private WpfTextBox? _volumeAverageDaysTextBox;
        private WpfTextBox? _volumeMultiplierTextBox;
        private readonly List<(WpfCheckBox Enabled, WpfComboBox Field, WpfComboBox Comparison, WpfTextBox Days)> _priceFilterControls = new();
        private TextBlock? _symbolFilterStatusTextBlock;

        private static readonly bool _symbolFiltersHandlerRegistered = RegisterSymbolFiltersHandler();

        private static bool RegisterSymbolFiltersHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(MainWindow),
                Window.LoadedEvent,
                new RoutedEventHandler(InitializeSymbolFiltersOnLoaded));
            return true;
        }

        private static void InitializeSymbolFiltersOnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is MainWindow window)
                window.InitializeSymbolFilters();
        }

        private void InitializeSymbolFilters()
        {
            if (_symbolFiltersInitialized)
                return;

            if (SymbolsPanel.Child is not Grid panelGrid)
                return;

            _symbolFiltersInitialized = true;

            panelGrid.RowDefinitions.Insert(2, new RowDefinition { Height = new GridLength(335) });

            foreach (UIElement child in panelGrid.Children)
            {
                int row = Grid.GetRow(child);
                if (row >= 2)
                    Grid.SetRow(child, row + 1);
            }

            Grid filterHostGrid = BuildFilterHost();
            Grid.SetRow(filterHostGrid, 2);
            panelGrid.Children.Add(filterHostGrid);

            SymbolSearchTextBox.TextChanged += SymbolFilterInputChanged;
            LoadFilterDefaults();
            _ = ApplyAllSymbolFiltersAsync();
        }

        private Grid BuildFilterHost()
        {
            var outer = new Grid();
            outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var title = new TextBlock
            {
                Text = "فیلتر سهام (همه فیلترها با AND)",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(title, 0);
            outer.Children.Add(title);

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            Grid.SetRow(scroll, 1);
            outer.Children.Add(scroll);

            var stack = new StackPanel();
            scroll.Content = stack;

            _tradeStatusFilterComboBox = new WpfComboBox { Height = 27, Margin = new Thickness(0, 1, 0, 3) };
            AddComboItems(_tradeStatusFilterComboBox,
                (TradeStatusFilter.All, "همه"),
                (TradeStatusFilter.TradedToday, "امروز معامله داشته"),
                (TradeStatusFilter.NotTradedToday, "امروز معامله نداشته"));
            stack.Children.Add(LabeledControl("وضعیت معامله امروز:", _tradeStatusFilterComboBox));

            _nameFilterComboBox = new WpfComboBox { Width = 115, Height = 27, Margin = new Thickness(0, 1, 4, 0) };
            AddComboItems(_nameFilterComboBox,
                (SymbolNameFilter.All, "همه"),
                (SymbolNameFilter.Contains, "دارای عبارت"),
                (SymbolNameFilter.StartsWith, "شروع با"),
                (SymbolNameFilter.EndsWith, "ختم با"),
                (SymbolNameFilter.Middle, "عبارت در وسط"),
                (SymbolNameFilter.DoesNotContain, "فاقد عبارت"));
            _nameFilterTextBox = new WpfTextBox { Height = 27, MinWidth = 70, Padding = new Thickness(5, 1, 5, 1) };
            _nameFilterTextBox.TextChanged += SymbolFilterInputChanged;
            _nameFilterComboBox.SelectionChanged += SymbolFilterInputChanged;
            stack.Children.Add(LabeledControl("نام سهم:", Inline(_nameFilterComboBox, _nameFilterTextBox)));

            _daysWithoutTradeCheckBox = new WpfCheckBox { Content = "فعال", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) };
            _daysWithoutTradeTextBox = new WpfTextBox { Width = 55, Height = 27, Text = "5", HorizontalContentAlignment = HorizontalAlignment.Center };
            _daysWithoutTradeCheckBox.Checked += SymbolFilterInputChanged;
            _daysWithoutTradeCheckBox.Unchecked += SymbolFilterInputChanged;
            _daysWithoutTradeTextBox.TextChanged += SymbolFilterInputChanged;
            stack.Children.Add(LabeledControl("فاقد معامله در X روز گذشته:", Inline(_daysWithoutTradeCheckBox, _daysWithoutTradeTextBox)));

            _volumeFilterCheckBox = new WpfCheckBox { Content = "فعال", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) };
            _volumeAverageDaysTextBox = new WpfTextBox { Width = 48, Height = 27, Text = "20", HorizontalContentAlignment = HorizontalAlignment.Center };
            _volumeMultiplierTextBox = new WpfTextBox { Width = 48, Height = 27, Text = "2", HorizontalContentAlignment = HorizontalAlignment.Center };
            _volumeFilterCheckBox.Checked += SymbolFilterInputChanged;
            _volumeFilterCheckBox.Unchecked += SymbolFilterInputChanged;
            _volumeAverageDaysTextBox.TextChanged += SymbolFilterInputChanged;
            _volumeMultiplierTextBox.TextChanged += SymbolFilterInputChanged;
            stack.Children.Add(LabeledControl("حجم آخر ≥ میانگین X روز × Y:", Inline(_volumeFilterCheckBox, _volumeAverageDaysTextBox, new TextBlock { Text = "×", Margin = new Thickness(4, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center }, _volumeMultiplierTextBox)));

            var priceHeader = new TextBlock
            {
                Text = "فیلترهای O/H/L/C/V/FINAL FEE (هر کدام مستقل)",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 4, 0, 2)
            };
            stack.Children.Add(priceHeader);

            for (int i = 0; i < 5; i++)
                stack.Children.Add(BuildPriceFilterRow(i));

            _symbolFilterStatusTextBlock = new TextBlock
            {
                Foreground = Brushes.Gray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 0)
            };
            stack.Children.Add(_symbolFilterStatusTextBlock);

            return outer;
        }

        private FrameworkElement BuildPriceFilterRow(int index)
        {
            var enabled = new WpfCheckBox { Content = $"{index + 1}", Width = 28, VerticalAlignment = VerticalAlignment.Center };
            var field = new WpfComboBox { Width = 72, Height = 26 };
            AddComboItems(field,
                (PriceField.Open, "O"),
                (PriceField.High, "H"),
                (PriceField.Low, "L"),
                (PriceField.Close, "C"),
                (PriceField.Volume, "V"),
                (PriceField.FinalFee, "FINAL FEE"));

            var comparison = new WpfComboBox { Width = 55, Height = 26, Margin = new Thickness(3, 0, 3, 0) };
            AddComboItems(comparison,
                (NumericComparison.GreaterThan, ">"),
                (NumericComparison.Equal, "="),
                (NumericComparison.NotEqual, "≠"),
                (NumericComparison.LessThan, "<"));

            var days = new WpfTextBox { Width = 42, Height = 26, Text = "1", HorizontalContentAlignment = HorizontalAlignment.Center };
            enabled.Checked += SymbolFilterInputChanged;
            enabled.Unchecked += SymbolFilterInputChanged;
            field.SelectionChanged += SymbolFilterInputChanged;
            comparison.SelectionChanged += SymbolFilterInputChanged;
            days.TextChanged += SymbolFilterInputChanged;

            _priceFilterControls.Add((enabled, field, comparison, days));

            return Inline(
                new TextBlock { Text = "فیلتر:", Width = 40, VerticalAlignment = VerticalAlignment.Center },
                enabled,
                field,
                comparison,
                new TextBlock { Text = "روز قبل:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 2, 0) },
                days);
        }

        private static StackPanel Inline(params UIElement[] children)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            foreach (UIElement child in children)
                panel.Children.Add(child);
            return panel;
        }

        private static StackPanel LabeledControl(string label, UIElement control)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 1, 0, 3) };
            panel.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(control);
            return panel;
        }

        private static void AddComboItems<T>(WpfComboBox combo, params (T Value, string Text)[] items)
        {
            foreach (var item in items)
                combo.Items.Add(new FilterComboItem<T>(item.Value, item.Text));
            combo.SelectedIndex = 0;
        }

        private sealed class FilterComboItem<T>
        {
            public T Value { get; }
            public string Text { get; }
            public FilterComboItem(T value, string text) { Value = value; Text = text; }
            public override string ToString() => Text;
        }

        private static T GetComboValue<T>(WpfComboBox combo)
        {
            return combo.SelectedItem is FilterComboItem<T> item ? item.Value : default!;
        }

        private void LoadFilterDefaults()
        {
            _tradeStatusFilterComboBox!.SelectedIndex = 0;
            _nameFilterComboBox!.SelectedIndex = 0;
            _nameFilterTextBox!.Text = "";
            _daysWithoutTradeCheckBox!.IsChecked = false;
            _volumeFilterCheckBox!.IsChecked = false;
            foreach (var row in _priceFilterControls)
                row.Enabled.IsChecked = false;
        }

        private void SymbolFilterInputChanged(object? sender, RoutedEventArgs e)
        {
            if (_symbolFiltersApplying)
                return;
            _ = ApplyAllSymbolFiltersAsync();
        }

        private void SymbolFilterInputChanged(object? sender, TextChangedEventArgs e)
        {
            if (_symbolFiltersApplying)
                return;
            _ = ApplyAllSymbolFiltersAsync();
        }

        private async Task ApplyAllSymbolFiltersAsync()
        {
            if (!_symbolFiltersInitialized || _selectedPortfolio == null)
                return;

            _symbolFilterCts?.Cancel();
            _symbolFilterCts?.Dispose();
            _symbolFilterCts = new CancellationTokenSource();
            CancellationToken token = _symbolFilterCts.Token;

            try
            {
                _symbolFiltersApplying = true;
                CaptureFilterSettings();
                _symbolFiltersApplying = false;

                IEnumerable<SymbolInfo> query = _allSymbols;
                SymbolFilterSettings settings = _symbolFilterSettings;

                DateTime? marketDate = _allSymbols
                    .Where(x => x.LastTradeDate.HasValue)
                    .Select(x => x.LastTradeDate!.Value.Date)
                    .OrderByDescending(x => x)
                    .FirstOrDefault();

                if (settings.TradeStatus != TradeStatusFilter.All && marketDate.HasValue)
                {
                    query = query.Where(x =>
                    {
                        bool traded = x.LastTradeDate.HasValue && x.LastTradeDate.Value.Date == marketDate.Value;
                        return settings.TradeStatus == TradeStatusFilter.TradedToday ? traded : !traded;
                    });
                }

                if (settings.NameFilter != SymbolNameFilter.All && !string.IsNullOrEmpty(settings.NameText))
                {
                    string text = settings.NameText;
                    query = query.Where(x => MatchName(x.DisplayName, text, settings.NameFilter));
                }

                if (settings.DaysWithoutTradeEnabled && settings.DaysWithoutTrade > 0 && marketDate.HasValue)
                {
                    DateTime cutoff = marketDate.Value.AddDays(-settings.DaysWithoutTrade);
                    query = query.Where(x => !x.LastTradeDate.HasValue || x.LastTradeDate.Value.Date < cutoff);
                }

                bool needsBars = settings.VolumeFilterEnabled || settings.PriceFilters.Any(x => x.Enabled);
                List<SymbolInfo> candidates = query.ToList();

                if (needsBars)
                {
                    await EnsureBarsLoadedAsync(candidates, token);
                    candidates = candidates.Where(x => PassHistoricalFilters(x, settings)).ToList();
                }

                token.ThrowIfCancellationRequested();
                RenumberFilteredSymbols(candidates);
                SymbolsDataGrid.ItemsSource = candidates;

                if (_symbolFilterStatusTextBlock != null)
                    _symbolFilterStatusTextBlock.Text = $"نتیجه: {candidates.Count:N0} نماد";
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (_symbolFilterStatusTextBlock != null)
                    _symbolFilterStatusTextBlock.Text = $"خطا در فیلتر: {ex.Message}";
            }
        }

        private void CaptureFilterSettings()
        {
            _symbolFilterSettings.TradeStatus = GetComboValue<TradeStatusFilter>(_tradeStatusFilterComboBox!);
            _symbolFilterSettings.NameFilter = GetComboValue<SymbolNameFilter>(_nameFilterComboBox!);
            _symbolFilterSettings.NameText = _nameFilterTextBox!.Text.Trim();
            _symbolFilterSettings.DaysWithoutTradeEnabled = _daysWithoutTradeCheckBox!.IsChecked == true;
            _symbolFilterSettings.DaysWithoutTrade = ParsePositiveInt(_daysWithoutTradeTextBox!.Text, 0);
            _symbolFilterSettings.VolumeFilterEnabled = _volumeFilterCheckBox!.IsChecked == true;
            _symbolFilterSettings.VolumeAverageDays = ParsePositiveInt(_volumeAverageDaysTextBox!.Text, 20);
            _symbolFilterSettings.VolumeMultiplier = ParsePositiveDouble(_volumeMultiplierTextBox!.Text, 2.0);

            for (int i = 0; i < 5; i++)
            {
                var ui = _priceFilterControls[i];
                var model = _symbolFilterSettings.PriceFilters[i];
                model.Enabled = ui.Enabled.IsChecked == true;
                model.Field = GetComboValue<PriceField>(ui.Field);
                model.Comparison = GetComboValue<NumericComparison>(ui.Comparison);
                model.Days = ParsePositiveInt(ui.Days.Text, 1);
            }
        }

        private static bool MatchName(string value, string text, SymbolNameFilter filter)
        {
            if (filter == SymbolNameFilter.All)
                return true;
            if (filter == SymbolNameFilter.Contains)
                return value.Contains(text, StringComparison.OrdinalIgnoreCase);
            if (filter == SymbolNameFilter.StartsWith)
                return value.StartsWith(text, StringComparison.OrdinalIgnoreCase);
            if (filter == SymbolNameFilter.EndsWith)
                return value.EndsWith(text, StringComparison.OrdinalIgnoreCase);
            if (filter == SymbolNameFilter.DoesNotContain)
                return !value.Contains(text, StringComparison.OrdinalIgnoreCase);
            int index = value.IndexOf(text, StringComparison.OrdinalIgnoreCase);
            return index > 0 && index + text.Length < value.Length;
        }

        private async Task EnsureBarsLoadedAsync(List<SymbolInfo> symbols, CancellationToken token)
        {
            if (_selectedPortfolio == null)
                return;

            List<SymbolInfo> missing = symbols
                .Where(x => !_symbolFilterBars.ContainsKey(x.FilePath))
                .ToList();

            if (missing.Count == 0)
                return;

            await Task.Run(() =>
            {
                foreach (SymbolInfo symbol in missing)
                {
                    token.ThrowIfCancellationRequested();
                    List<MarketBar> bars = _symbolDataService.LoadBars(symbol, _selectedPortfolio);
                    lock (_symbolFilterBars)
                        _symbolFilterBars[symbol.FilePath] = bars.OrderBy(x => x.Timestamp ?? DateTime.MinValue).ToList();
                }
            }, token);
        }

        private bool PassHistoricalFilters(SymbolInfo symbol, SymbolFilterSettings settings)
        {
            if (!_symbolFilterBars.TryGetValue(symbol.FilePath, out List<MarketBar>? bars) || bars.Count == 0)
                return false;

            if (settings.VolumeFilterEnabled)
            {
                int n = Math.Max(1, settings.VolumeAverageDays);
                if (bars.Count < n)
                    return false;
                double average = bars.TakeLast(n).Average(x => x.Volume);
                if (bars[^1].Volume < average * settings.VolumeMultiplier)
                    return false;
            }

            foreach (PriceFilter filter in settings.PriceFilters)
            {
                if (!filter.Enabled)
                    continue;

                int days = Math.Max(1, filter.Days);
                if (bars.Count <= days)
                    return false;

                double latest = GetPriceField(bars[^1], filter.Field);
                double previous = GetPriceField(bars[bars.Count - 1 - days], filter.Field);

                if (!Compare(latest, previous, filter.Comparison))
                    return false;
            }

            return true;
        }

        private static double GetPriceField(MarketBar bar, PriceField field)
        {
            return field switch
            {
                PriceField.Open => bar.Open,
                PriceField.High => bar.High,
                PriceField.Low => bar.Low,
                PriceField.Close => bar.Close,
                PriceField.Volume => bar.Volume,
                PriceField.FinalFee => bar.TSEClose,
                _ => double.NaN
            };
        }

        private static bool Compare(double left, double right, NumericComparison comparison)
        {
            if (double.IsNaN(left) || double.IsNaN(right))
                return false;

            const double epsilon = 1e-9;
            return comparison switch
            {
                NumericComparison.GreaterThan => left > right,
                NumericComparison.GreaterOrEqual => left >= right,
                NumericComparison.Equal => Math.Abs(left - right) <= epsilon,
                NumericComparison.NotEqual => Math.Abs(left - right) > epsilon,
                NumericComparison.LessOrEqual => left <= right,
                NumericComparison.LessThan => left < right,
                _ => true
            };
        }

        private static int ParsePositiveInt(string text, int fallback)
            => int.TryParse(text.Trim(), out int value) && value > 0 ? value : fallback;

        private static double ParsePositiveDouble(string text, double fallback)
            => double.TryParse(text.Trim(), out double value) && value > 0 ? value : fallback;

        private void RenumberFilteredSymbols(List<SymbolInfo> symbols)
        {
            for (int i = 0; i < symbols.Count; i++)
                symbols[i].RowNumber = i + 1;
        }
    }
}