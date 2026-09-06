using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using TradeIt.Models;
using TradeIt.Portfolios;
using TradeIt.Services;
using TradeIt.Charts;

using WpfButton = System.Windows.Controls.Button;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfPanel = System.Windows.Controls.Panel;
using WpfCursors = System.Windows.Input.Cursors;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TradeIt.Models;
using TradeIt.Portfolios;
using TradeIt.Services;

namespace TradeIt
{
    public partial class MainWindow
    {
        // AUTO-REFACTORED-METHODS-V2

        public async Task OpenChartTabAsync(
            SymbolInfo symbol,
            Portfolio portfolio,
            bool replaceCurrentTab)
        {
            if (symbol == null ||
                portfolio == null)
            {
                return;
            }

            try
            {
                SetBusy(
                    true,
                    $"در حال خواندن داده‌های {symbol.Symbol} ... لطفاً صبر کنید");

                TabItem? autoScrollTab =
                    ChartTabs.Items
                        .OfType<TabItem>()
                        .FirstOrDefault(
                            x =>
                                x.Tag is string tag &&
                                tag == "__AUTO_SCROLL__");

                if (!replaceCurrentTab)
                {
                    foreach (TabItem tab in ChartTabs.Items)
                    {
                        if (tab.Tag is string existingSymbol &&
                            existingSymbol !=
                            "__AUTO_SCROLL__" &&
                            existingSymbol ==
                            symbol.Symbol)
                        {
                            ChartTabs.SelectedItem =
                                tab;

                            return;
                        }
                    }
                }

                List<MarketBar> bars =
                    await Task.Run(
                        () =>
                            _symbolDataService
                                .LoadBars(
                                    symbol,
                                    portfolio));

                if (bars.Count == 0)
                {
                    StatusTextBlock.Text =
                        $"برای {symbol.Symbol} داده‌ای پیدا نشد.";

                    return;
                }

                var chartView =
                    new ChartTabView(
                        symbol,
                        bars);

                if (replaceCurrentTab &&
                    autoScrollTab != null)
                {
                    autoScrollTab.Content =
                        chartView;

                    autoScrollTab.Header =
                        CreateTabHeader(
                            symbol);

                    ChartTabs.SelectedItem =
                        autoScrollTab;

                    StatusTextBlock.Text =
                        $"{symbol.Symbol} — {bars.Count:N0} کندل";

                    return;
                }

                var tabItem =
                    new TabItem
                    {
                        Tag =
                            symbol.Symbol,

                        Header =
                            CreateTabHeader(
                                symbol),

                        Content =
                            chartView
                    };

                ChartTabs.Items.Add(
                    tabItem);

                ChartTabs.SelectedItem =
                    tabItem;

                StatusTextBlock.Text =
                    $"{symbol.Symbol} — {bars.Count:N0} کندل";
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

        private object CreateTabHeader(
            SymbolInfo symbol)
        {
            var panel =
                new WpfStackPanel
                {
                    Orientation =
                        WpfOrientation.Horizontal
                };

            var text =
                new WpfTextBlock
                {
                    Text =
                        symbol.DisplayName,

                    VerticalAlignment =
                        VerticalAlignment.Center,

                    Margin =
                        new Thickness(
                            0,
                            0,
                            8,
                            0)
                };

            var close =
                new WpfButton
                {
                    Content =
                        "×",

                    Width =
                        22,

                    Height =
                        22,

                    Padding =
                        new Thickness(0),

                    FontWeight =
                        FontWeights.Bold,

                    ToolTip =
                        "بستن نمودار"
                };

            close.Click +=
                (_, _) =>
                {
                    if (close.Parent
                        is WpfStackPanel headerPanel)
                    {
                        TabItem? tabToRemove =
                            ChartTabs.Items
                                .OfType<TabItem>()
                                .FirstOrDefault(
                                    tab =>
                                        tab.Header ==
                                        headerPanel);

                        if (tabToRemove != null)
                        {
                            ChartTabs.Items.Remove(
                                tabToRemove);
                        }
                    }
                };

            panel.Children.Add(
                text);

            panel.Children.Add(
                close);

            return panel;
        }

        private void SetBusy(
            bool busy,
            string? message = null)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                StatusTextBlock.Text =
                    message;
            }

            ProgressBar.Visibility =
                busy
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            Mouse.OverrideCursor =
                busy
                    ? WpfCursors.Wait
                    : null;

            PortfolioComboBox.IsEnabled =
                !busy;

            NewPortfolioButton.IsEnabled =
                !busy;

            RefreshPortfolioButton.IsEnabled =
                !busy;

            SymbolsDataGrid.IsEnabled =
                !busy;

            RefreshSymbolsButton.IsEnabled =
                !busy;

            DeleteSymbolsButton.IsEnabled =
                !busy;

            MakeWatchButton.IsEnabled =
                !busy;

            AutoScrollButton.IsEnabled =
                !busy;
        }

        private void DeleteSymbolsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_selectedPortfolio == null)
            {
                return;
            }

            List<SymbolInfo> selected =
                _allSymbols
                    .Where(
                        x => x.IsSelected)
                    .ToList();

            if (selected.Count == 0)
            {
                WpfMessageBox.Show(
                    "هیچ نمادی انتخاب نشده است.",
                    "Delete",
                    WpfMessageBoxButton.OK,
                    WpfMessageBoxImage.Information);

                return;
            }

            string message =
                $"آیا میخواهید {selected.Count:N0} نماد انتخاب‌شده از سبد «{_selectedPortfolio.Name}» حذف شود؟";

            MessageBoxResult answer =
                WpfMessageBox.Show(
                    message,
                    "حذف نمادها",
                    WpfMessageBoxButton.YesNo,
                    WpfMessageBoxImage.Warning);

            if (answer !=
                MessageBoxResult.Yes)
            {
                return;
            }

            HashSet<string> selectedPaths =
                selected
                    .Select(
                        x => x.FilePath)
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);

            if (_selectedPortfolio.Symbols != null)
            {
                _selectedPortfolio.Symbols =
                    _selectedPortfolio.Symbols
                        .Where(
                            x =>
                                !selectedPaths.Contains(
                                    x.FilePath))
                        .ToList();
            }

            try
            {
                _portfolioManager.Save(
                    _selectedPortfolio);

                _allSymbols =
                    _allSymbols
                        .Where(
                            x =>
                                !x.IsSelected)
                        .ToList();

                RenumberSymbols();

                ApplySymbolFilter();

                ResetSelectionRadioButtons();

                StatusTextBlock.Text =
                    $"{selected.Count:N0} نماد حذف شد.";
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(
                    ex.ToString(),
                    "خطا در حذف نمادها",
                    WpfMessageBoxButton.OK,
                    WpfMessageBoxImage.Error);
            }
        }

        private void MakeWatchButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_selectedPortfolio == null)
            {
                return;
            }

            List<SymbolInfo> selected =
                _allSymbols
                    .Where(
                        x => x.IsSelected)
                    .ToList();

            if (selected.Count == 0)
            {
                WpfMessageBox.Show(
                    "هیچ نمادی انتخاب نشده است.",
                    "Make Watch",
                    WpfMessageBoxButton.OK,
                    WpfMessageBoxImage.Information);

                return;
            }

            string timestamp =
                DateTime.Now.ToString(
                    "yyyyMMdd_HHmmss");

            string newName =
                $"{_selectedPortfolio.Name}_{timestamp}";

            var newPortfolio =
                new Portfolio
                {
                    Name =
                        newName,

                    DataSource =
                        new DataSource
                        {
                            SourceType =
                                _selectedPortfolio.DataSource.SourceType,

                            Path =
                                _selectedPortfolio.DataSource.Path,

                            Delimiter =
                                _selectedPortfolio.DataSource.Delimiter,

                            HasHeader =
                                _selectedPortfolio.DataSource.HasHeader,

                            SymbolSource =
                                _selectedPortfolio.DataSource.SymbolSource,

                            DataType =
                                _selectedPortfolio.DataSource.DataType,

                            HasDateTime =
                                _selectedPortfolio.DataSource.HasDateTime,

                            Calendar =
                                _selectedPortfolio.DataSource.Calendar,

                            DateFormat =
                                _selectedPortfolio.DataSource.DateFormat,

                            TimeFormat =
                                _selectedPortfolio.DataSource.TimeFormat,

                            SymbolColumn =
                                _selectedPortfolio.DataSource.SymbolColumn,

                            DateColumn =
                                _selectedPortfolio.DataSource.DateColumn,

                            TimeColumn =
                                _selectedPortfolio.DataSource.TimeColumn,

                            OpenColumn =
                                _selectedPortfolio.DataSource.OpenColumn,

                            HighColumn =
                                _selectedPortfolio.DataSource.HighColumn,

                            LowColumn =
                                _selectedPortfolio.DataSource.LowColumn,

                            CloseColumn =
                                _selectedPortfolio.DataSource.CloseColumn,

                            VolumeColumn =
                                _selectedPortfolio.DataSource.VolumeColumn,

                            TSECloseColumn =
                                _selectedPortfolio.DataSource.TSECloseColumn,

                            PreviousColumn =
                                _selectedPortfolio.DataSource.PreviousColumn,

                            ValueColumn =
                                _selectedPortfolio.DataSource.ValueColumn,

                            TradeCountColumn =
                                _selectedPortfolio.DataSource.TradeCountColumn,

                            EnglishTickerColumn =
                                _selectedPortfolio.DataSource.EnglishTickerColumn,

                            ShareCountColumn =
                                _selectedPortfolio.DataSource.ShareCountColumn,

                            MarketValueColumn =
                                _selectedPortfolio.DataSource.MarketValueColumn
                        }
                };

            // =====================================================
            // نگهداری نمادهای انتخاب‌شده
            // =====================================================

            newPortfolio.Symbols =
                selected
                    .Select(
                        x =>
                            new SymbolInfo
                            {
                                Symbol =
                                    x.Symbol,

                                DisplayName =
                                    x.DisplayName,

                                FilePath =
                                    x.FilePath,

                                RowNumber =
                                    0,

                                LastTradeDate =
                                    x.LastTradeDate,

                                LastVolume =
                                    x.LastVolume,

                                LastOpen =
                                    x.LastOpen,

                                LastHigh =
                                    x.LastHigh,

                                LastLow =
                                    x.LastLow,

                                LastClose =
                                    x.LastClose,

                                LastFinalFee =
                                    x.LastFinalFee,

                                IsSelected =
                                    false
                            })
                    .ToList();

            try
            {
                _portfolioManager.Save(
                    newPortfolio);

                LoadPortfolios();

                PortfolioComboBox.SelectedItem =
                    _portfolios.FirstOrDefault(
                        x =>
                            x.Name ==
                            newName);

                StatusTextBlock.Text =
                    $"سبد جدید «{newName}» ساخته شد.";
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(
                    ex.ToString(),
                    "خطا در ساخت سبد جدید",
                    WpfMessageBoxButton.OK,
                    WpfMessageBoxImage.Error);
            }
        }

        private void EnterFullScreen()
        {
            if (_isFullScreen)
            {
                return;
            }

            _previousWindowState =
                WindowState;

            _previousWindowStyle =
                WindowStyle;

            _previousResizeMode =
                ResizeMode;

            _previousRootRow0Height =
                RootLayout.RowDefinitions[0].Height;

            _previousRootRow1Height =
                RootLayout.RowDefinitions[1].Height;

            _previousRootRow2Height =
                RootLayout.RowDefinitions[2].Height;

            _previousMainContentRow =
                Grid.GetRow(
                    MainContent);

            _previousMainContentColumn =
                Grid.GetColumn(
                    MainContent);

            _previousMainContentRowSpan =
                Grid.GetRowSpan(
                    MainContent);

            _previousMainContentColumnSpan =
                Grid.GetColumnSpan(
                    MainContent);

            _previousSymbolsColumnWidth =
                MainContent.ColumnDefinitions[0].Width;

            _previousChartColumnWidth =
                MainContent.ColumnDefinitions[1].Width;

            WindowState =
                WindowState.Maximized;

            ResizeMode =
                ResizeMode.NoResize;

            TopToolbar.Visibility =
                Visibility.Collapsed;

            SymbolsPanel.Visibility =
                Visibility.Collapsed;

            StatusBar.Visibility =
                Visibility.Collapsed;

            RootLayout.RowDefinitions[0].Height =
                new GridLength(0);

            RootLayout.RowDefinitions[1].Height =
                new GridLength(
                    1,
                    GridUnitType.Star);

            RootLayout.RowDefinitions[2].Height =
                new GridLength(0);

            Grid.SetRow(
                MainContent,
                0);

            Grid.SetRowSpan(
                MainContent,
                3);

            Grid.SetColumn(
                MainContent,
                0);

            Grid.SetColumnSpan(
                MainContent,
                1);

            MainContent.ColumnDefinitions[0].Width =
                new GridLength(0);

            MainContent.ColumnDefinitions[1].Width =
                new GridLength(
                    1,
                    GridUnitType.Star);

            _isFullScreen =
                true;

            FullScreenButton.Content =
                "↙ خروج از تمام صفحه";

            FullScreenExitButton.Visibility =
                Visibility.Visible;

            WpfPanel.SetZIndex(
                FullScreenExitButton,
                10000);

            UpdateLayout();

            MainContent.UpdateLayout();

            ChartArea.UpdateLayout();

            ChartTabs.UpdateLayout();

            Dispatcher.BeginInvoke(
                new Action(
                    () =>
                    {
                        FullScreenExitButton.Visibility =
                            Visibility.Visible;

                        WpfPanel.SetZIndex(
                            FullScreenExitButton,
                            100);

                        UpdateLayout();
                    }),
                DispatcherPriority.Loaded);
        }

        private void ExitFullScreen()
        {
            if (!_isFullScreen)
            {
                return;
            }

            FullScreenExitButton.Visibility =
                Visibility.Collapsed;

            Grid.SetRow(
                MainContent,
                _previousMainContentRow);

            Grid.SetColumn(
                MainContent,
                _previousMainContentColumn);

            Grid.SetRowSpan(
                MainContent,
                _previousMainContentRowSpan);

            Grid.SetColumnSpan(
                MainContent,
                _previousMainContentColumnSpan);

            MainContent.ColumnDefinitions[0].Width =
                _previousSymbolsColumnWidth;

            MainContent.ColumnDefinitions[1].Width =
                _previousChartColumnWidth;

            RootLayout.RowDefinitions[0].Height =
                _previousRootRow0Height;

            RootLayout.RowDefinitions[1].Height =
                _previousRootRow1Height;

            RootLayout.RowDefinitions[2].Height =
                _previousRootRow2Height;

            TopToolbar.Visibility =
                Visibility.Visible;

            SymbolsPanel.Visibility =
                Visibility.Visible;

            StatusBar.Visibility =
                Visibility.Visible;

            ResizeMode =
                _previousResizeMode;

            WindowStyle =
                _previousWindowStyle;

            WindowState =
                _previousWindowState;

            _isFullScreen =
                false;

            FullScreenButton.Content =
                "⛶ تمام صفحه";

            UpdateLayout();

            MainContent.UpdateLayout();

            ChartArea.UpdateLayout();

            ChartTabs.UpdateLayout();
        }

        private async Task ApplySymbolFiltersThroughEngineAsync()
        {
            if (!_symbolFilterArchitectureAttached || !_symbolFiltersInitialized || _selectedPortfolio == null)
                return;

            if (!ReferenceEquals(_symbolFilterEnginePortfolio, _selectedPortfolio))
            {
                _symbolFilterEngine.ClearCache();
                _symbolFilterEnginePortfolio = _selectedPortfolio;
            }

            _symbolFilterCts?.Cancel();
            _symbolFilterCts?.Dispose();
            _symbolFilterCts = new CancellationTokenSource();
            CancellationToken token = _symbolFilterCts.Token;

            try
            {
                _symbolFiltersApplying = true;
                CaptureFilterSettings();
                _symbolFiltersApplying = false;

                var results = await _symbolFilterEngine.ApplyAsync(
                    _allSymbols,
                    SymbolSearchTextBox.Text,
                    _symbolFilterSettings,
                    token,
                    symbol => _symbolDataService.LoadBars(symbol, _selectedPortfolio!));

                token.ThrowIfCancellationRequested();
                SymbolsDataGrid.ItemsSource = results;

                if (_symbolFilterStatusTextBlock != null)
                    _symbolFilterStatusTextBlock.Text = $"نتیجه: {results.Count:N0} نماد";
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

        private void AttachSymbolFilterArchitecture()
        {
            if (_symbolFilterArchitectureAttached || !_symbolFiltersInitialized)
                return;

            DetachLegacySymbolFilterHandlers();

            SymbolSearchTextBox.TextChanged += SymbolFilterArchitecture_TextChanged;
            _nameFilterTextBox!.TextChanged += SymbolFilterArchitecture_TextChanged;
            _nameFilterComboBox!.SelectionChanged += SymbolFilterArchitecture_SelectionChanged;
            _daysWithoutTradeCheckBox!.Checked += SymbolFilterArchitecture_RoutedChanged;
            _daysWithoutTradeCheckBox.Unchecked += SymbolFilterArchitecture_RoutedChanged;
            _daysWithoutTradeTextBox!.TextChanged += SymbolFilterArchitecture_TextChanged;
            _daysWithTradeCheckBox!.Checked += SymbolFilterArchitecture_RoutedChanged;
            _daysWithTradeCheckBox.Unchecked += SymbolFilterArchitecture_RoutedChanged;
            _daysWithTradeTextBox!.TextChanged += SymbolFilterArchitecture_TextChanged;
            _volumeFilterCheckBox!.Checked += SymbolFilterArchitecture_RoutedChanged;
            _volumeFilterCheckBox.Unchecked += SymbolFilterArchitecture_RoutedChanged;
            _volumeAverageDaysTextBox!.TextChanged += SymbolFilterArchitecture_TextChanged;
            _volumeMultiplierTextBox!.TextChanged += SymbolFilterArchitecture_TextChanged;

            foreach (var row in _priceFilterControls)
            {
                row.Enabled.Checked += SymbolFilterArchitecture_RoutedChanged;
                row.Enabled.Unchecked += SymbolFilterArchitecture_RoutedChanged;
                row.LeftField.SelectionChanged += SymbolFilterArchitecture_SelectionChanged;
                row.LeftDays.TextChanged += SymbolFilterArchitecture_TextChanged;
                row.Comparison.SelectionChanged += SymbolFilterArchitecture_SelectionChanged;
                row.RightField.SelectionChanged += SymbolFilterArchitecture_SelectionChanged;
                row.RightDays.TextChanged += SymbolFilterArchitecture_TextChanged;
            }

            _symbolFilterArchitectureAttached = true;
            _ = ApplySymbolFiltersThroughEngineAsync();
        }
    }
}
