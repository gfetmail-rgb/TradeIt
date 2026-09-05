using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TradeIt.Models;
using TradeIt.Services;

using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfBrushes = System.Windows.Media.Brushes;

namespace TradeIt
{
    public partial class MainWindow
    {
        // UI adapter state. Filtering rules themselves live in SymbolFilterEngine.
        private readonly SymbolFilterSettings _symbolFilterSettings = new();
        private CancellationTokenSource? _symbolFilterCts;
        private bool _symbolFiltersInitialized;
        private bool _symbolFiltersApplying;

        private WpfComboBox? _tradeStatusFilterComboBox;
        private WpfComboBox? _nameFilterComboBox;
        private WpfTextBox? _nameFilterTextBox;
        private WpfCheckBox? _daysWithoutTradeCheckBox;
        private WpfTextBox? _daysWithoutTradeTextBox;
        private WpfCheckBox? _daysWithTradeCheckBox;
        private WpfTextBox? _daysWithTradeTextBox;
        private WpfCheckBox? _volumeFilterCheckBox;
        private WpfTextBox? _volumeAverageDaysTextBox;
        private WpfTextBox? _volumeMultiplierTextBox;
        private readonly List<(WpfCheckBox Enabled, WpfComboBox LeftField, WpfTextBox LeftDays, WpfComboBox Comparison, WpfComboBox RightField, WpfTextBox RightDays)> _priceFilterControls = new();
        private TextBlock? _symbolFilterStatusTextBlock;

        private static readonly bool _symbolFiltersHandlerRegistered = RegisterSymbolFiltersHandler();

        private static bool RegisterSymbolFiltersHandler()
        {
            EventManager.RegisterClassHandler(typeof(MainWindow), Window.LoadedEvent, new RoutedEventHandler(InitializeSymbolFiltersOnLoaded));
            return true;
        }

        private static void InitializeSymbolFiltersOnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is MainWindow window)
                window.InitializeSymbolFilters();
        }

        private void InitializeSymbolFilters()
        {
            if (_symbolFiltersInitialized || SymbolFilterHost == null)
                return;

            _symbolFiltersInitialized = true;
            SymbolFilterHost.Children.Clear();
            SymbolFilterHost.Children.Add(BuildFilterHost());
            SymbolSearchTextBox.TextChanged += SymbolFilterInputChanged;
            LoadFilterDefaults();
        }

        private Grid BuildFilterHost()
        {
            var outer = new Grid();
            outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var title = new TextBlock
            {
                Text = "فیلتر سهام — همه فیلترهای فعال با AND اعمال می‌شوند",
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
                (SymbolNameFilter.DoesNotContain, "فاقد عبارت"),
                (SymbolNameFilter.ContainsAny, "شامل یکی از عبارات"),
                (SymbolNameFilter.DoesNotContainAny, "شامل هیچ‌یک از عبارات"));
            _nameFilterTextBox = new WpfTextBox { Width = 300, Height = 27, Padding = new Thickness(5, 1, 5, 1) };
            _nameFilterTextBox.TextChanged += SymbolFilterInputChanged;
            _nameFilterComboBox.SelectionChanged += SymbolFilterInputChanged;
            stack.Children.Add(LabeledControl("نام سهم:", Inline(_nameFilterComboBox, _nameFilterTextBox)));

            _daysWithoutTradeCheckBox = new WpfCheckBox { Content = "فعال", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) };
            _daysWithoutTradeTextBox = new WpfTextBox { Width = 55, Height = 27, Text = "5", HorizontalContentAlignment = WpfHorizontalAlignment.Center };
            _daysWithoutTradeCheckBox.Checked += SymbolFilterInputChanged;
            _daysWithoutTradeCheckBox.Unchecked += SymbolFilterInputChanged;
            _daysWithoutTradeTextBox.TextChanged += SymbolFilterInputChanged;
            stack.Children.Add(LabeledControl("فاقد معامله در X روز گذشته:", Inline(_daysWithoutTradeCheckBox, _daysWithoutTradeTextBox)));

            _daysWithTradeCheckBox = new WpfCheckBox { Content = "فعال", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) };
            _daysWithTradeTextBox = new WpfTextBox { Width = 55, Height = 27, Text = "5", HorizontalContentAlignment = WpfHorizontalAlignment.Center };
            _daysWithTradeCheckBox.Checked += SymbolFilterInputChanged;
            _daysWithTradeCheckBox.Unchecked += SymbolFilterInputChanged;
            _daysWithTradeTextBox.TextChanged += SymbolFilterInputChanged;
            stack.Children.Add(LabeledControl("دارای معامله در X روز گذشته:", Inline(_daysWithTradeCheckBox, _daysWithTradeTextBox)));

            _volumeFilterCheckBox = new WpfCheckBox { Content = "فعال", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) };
            _volumeAverageDaysTextBox = new WpfTextBox { Width = 48, Height = 27, Text = "20", HorizontalContentAlignment = WpfHorizontalAlignment.Center };
            _volumeMultiplierTextBox = new WpfTextBox { Width = 48, Height = 27, Text = "2", HorizontalContentAlignment = WpfHorizontalAlignment.Center };
            _volumeFilterCheckBox.Checked += SymbolFilterInputChanged;
            _volumeFilterCheckBox.Unchecked += SymbolFilterInputChanged;
            _volumeAverageDaysTextBox.TextChanged += SymbolFilterInputChanged;
            _volumeMultiplierTextBox.TextChanged += SymbolFilterInputChanged;
            stack.Children.Add(LabeledControl("حجم آخر ≥ میانگین X روز × Y:", Inline(
                _volumeFilterCheckBox,
                _volumeAverageDaysTextBox,
                new TextBlock { Text = "×", Margin = new Thickness(4, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center },
                _volumeMultiplierTextBox)));

            stack.Children.Add(new TextBlock
            {
                Text = "فیلترهای O/H/L/C/V/FINAL FEE — مقایسه دو طرفه بین کندل‌ها",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 4, 0, 2)
            });

            for (int i = 0; i < 5; i++)
                stack.Children.Add(BuildPriceFilterRow(i));

            _symbolFilterStatusTextBlock = new TextBlock
            {
                Foreground = WpfBrushes.Gray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 0)
            };
            stack.Children.Add(_symbolFilterStatusTextBlock);
            return outer;
        }

        private FrameworkElement BuildPriceFilterRow(int index)
        {
            var enabled = new WpfCheckBox { Content = $"{index + 1}", Width = 28, VerticalAlignment = VerticalAlignment.Center };
            var leftField = new WpfComboBox { Width = 72, Height = 26 };
            AddComboItems(leftField,
                (PriceField.Open, "O"), (PriceField.High, "H"), (PriceField.Low, "L"),
                (PriceField.Close, "C"), (PriceField.Volume, "V"), (PriceField.FinalFee, "FINAL FEE"));
            var leftDays = new WpfTextBox { Width = 42, Height = 26, Text = "0", HorizontalContentAlignment = WpfHorizontalAlignment.Center };
            var comparison = new WpfComboBox { Width = 55, Height = 26, Margin = new Thickness(3, 0, 3, 0) };
            AddComboItems(comparison,
                (NumericComparison.GreaterThan, ">"), (NumericComparison.GreaterOrEqual, ">="),
                (NumericComparison.Equal, "="), (NumericComparison.NotEqual, "≠"),
                (NumericComparison.LessOrEqual, "<="), (NumericComparison.LessThan, "<"));
            var rightField = new WpfComboBox { Width = 72, Height = 26 };
            AddComboItems(rightField,
                (PriceField.Open, "O"), (PriceField.High, "H"), (PriceField.Low, "L"),
                (PriceField.Close, "C"), (PriceField.Volume, "V"), (PriceField.FinalFee, "FINAL FEE"));
            var rightDays = new WpfTextBox { Width = 42, Height = 26, Text = "1", HorizontalContentAlignment = WpfHorizontalAlignment.Center };

            enabled.Checked += SymbolFilterInputChanged;
            enabled.Unchecked += SymbolFilterInputChanged;
            leftField.SelectionChanged += SymbolFilterInputChanged;
            leftDays.TextChanged += SymbolFilterInputChanged;
            comparison.SelectionChanged += SymbolFilterInputChanged;
            rightField.SelectionChanged += SymbolFilterInputChanged;
            rightDays.TextChanged += SymbolFilterInputChanged;

            _priceFilterControls.Add((enabled, leftField, leftDays, comparison, rightField, rightDays));

            return Inline(
                enabled,
                leftField,
                new TextBlock { Text = "[", VerticalAlignment = VerticalAlignment.Center },
                leftDays,
                new TextBlock { Text = "]", VerticalAlignment = VerticalAlignment.Center },
                comparison,
                rightField,
                new TextBlock { Text = "[", VerticalAlignment = VerticalAlignment.Center },
                rightDays,
                new TextBlock { Text = "]", VerticalAlignment = VerticalAlignment.Center });
        }

        private static StackPanel Inline(params UIElement[] children)
        {
            var panel = new StackPanel { Orientation = WpfOrientation.Horizontal };
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

        private static T GetComboValue<T>(WpfComboBox combo) =>
            combo.SelectedItem is FilterComboItem<T> item ? item.Value : default!;

        private void LoadFilterDefaults()
        {
            _tradeStatusFilterComboBox!.SelectedIndex = 0;
            _nameFilterComboBox!.SelectedIndex = 0;
            _nameFilterTextBox!.Text = "";
            _daysWithoutTradeCheckBox!.IsChecked = false;
            _daysWithTradeCheckBox!.IsChecked = false;
            _volumeFilterCheckBox!.IsChecked = false;
            foreach (var row in _priceFilterControls)
                row.Enabled.IsChecked = false;
        }

        private void SymbolFilterInputChanged(object? sender, RoutedEventArgs e)
        {
            if (!_symbolFiltersApplying)
                _ = ApplyAllSymbolFiltersAsync();
        }

        private void SymbolFilterInputChanged(object? sender, TextChangedEventArgs e)
        {
            if (!_symbolFiltersApplying)
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

                List<SymbolInfo> candidates = await _symbolFilterEngine.ApplyAsync(
                    _allSymbols,
                    SymbolSearchTextBox.Text,
                    _symbolFilterSettings,
                    token,
                    symbol => _symbolDataService.LoadBars(symbol, _selectedPortfolio));

                token.ThrowIfCancellationRequested();
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
            finally
            {
                _symbolFiltersApplying = false;
            }
        }

        private void CaptureFilterSettings()
        {
            _symbolFilterSettings.TradeStatus = GetComboValue<TradeStatusFilter>(_tradeStatusFilterComboBox!);
            _symbolFilterSettings.NameFilter = GetComboValue<SymbolNameFilter>(_nameFilterComboBox!);
            _symbolFilterSettings.NameText = _nameFilterTextBox!.Text.Trim();
            _symbolFilterSettings.DaysWithoutTradeEnabled = _daysWithoutTradeCheckBox!.IsChecked == true;
            _symbolFilterSettings.DaysWithoutTrade = ParsePositiveInt(_daysWithoutTradeTextBox!.Text, 0);
            _symbolFilterSettings.DaysWithTradeEnabled = _daysWithTradeCheckBox!.IsChecked == true;
            _symbolFilterSettings.DaysWithTrade = ParsePositiveInt(_daysWithTradeTextBox!.Text, 0);
            _symbolFilterSettings.VolumeFilterEnabled = _volumeFilterCheckBox!.IsChecked == true;
            _symbolFilterSettings.VolumeAverageDays = ParsePositiveInt(_volumeAverageDaysTextBox!.Text, 20);
            _symbolFilterSettings.VolumeMultiplier = ParsePositiveDouble(_volumeMultiplierTextBox!.Text, 2.0);

            for (int i = 0; i < 5; i++)
            {
                var ui = _priceFilterControls[i];
                var model = _symbolFilterSettings.PriceFilters[i];
                model.Enabled = ui.Enabled.IsChecked == true;
                model.LeftField = GetComboValue<PriceField>(ui.LeftField);
                model.LeftDayOffset = ParseNonNegativeInt(ui.LeftDays.Text, 0);
                model.Comparison = GetComboValue<NumericComparison>(ui.Comparison);
                model.RightField = GetComboValue<PriceField>(ui.RightField);
                model.RightDayOffset = ParseNonNegativeInt(ui.RightDays.Text, 0);
            }
        }

        private static int ParsePositiveInt(string text, int fallback) =>
            int.TryParse(text.Trim(), out int value) && value > 0 ? value : fallback;

        private static int ParseNonNegativeInt(string text, int fallback) =>
            int.TryParse(text.Trim(), out int value) && value >= 0 ? value : fallback;

        private static double ParsePositiveDouble(string text, double fallback) =>
            double.TryParse(text.Trim(), out double value) && value > 0 ? value : fallback;
    }
}