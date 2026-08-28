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

namespace TradeIt
{
    public partial class MainWindow : Window
    {
        private readonly PortfolioManager _portfolioManager;

        private readonly SymbolDataService _symbolDataService;

        private List<Portfolio> _portfolios =
            new();

        private Portfolio? _selectedPortfolio;

        private List<SymbolInfo> _allSymbols =
            new();


        // =========================================================
        // Auto Scroll
        // =========================================================

        private DispatcherTimer? _autoScrollTimer;

        private bool _autoScrollRunning;

        private bool _autoScrollLoading;

        private int _autoScrollIndex = -1;

        private const int _autoScrollIntervalMilliseconds =
            1000;


        // =========================================================
        // Symbol Selection Control
        // =========================================================

        private bool _suppressSymbolSelection;


        // =========================================================
        // Full Screen
        // =========================================================

        private bool _isFullScreen;

        private WindowState _previousWindowState;

        private WindowStyle _previousWindowStyle;

        private ResizeMode _previousResizeMode;


        // =========================================================
        // Saved Main Layout
        // =========================================================

        private GridLength _previousRootRow0Height;

        private GridLength _previousRootRow1Height;

        private GridLength _previousRootRow2Height;


        // =========================================================
        // Saved MainContent
        // =========================================================

        private int _previousMainContentRow;

        private int _previousMainContentColumn;

        private int _previousMainContentRowSpan;

        private int _previousMainContentColumnSpan;


        // =========================================================
        // Saved MainContent Columns
        // =========================================================

        private GridLength _previousSymbolsColumnWidth;

        private GridLength _previousChartColumnWidth;


        // =========================================================
        // Constructor
        // =========================================================

        public MainWindow()
        {
            InitializeComponent();

            _portfolioManager =
                new PortfolioManager();

            _symbolDataService =
                new SymbolDataService();

            PreviewKeyDown +=
                MainWindow_PreviewKeyDown;

            Loaded +=
                MainWindow_Loaded;

            Closed +=
                MainWindow_Closed;
        }


        // =========================================================
        // Loaded
        // =========================================================

        private void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            LoadPortfolios();
        }


        // =========================================================
        // Closed
        // =========================================================

        private void MainWindow_Closed(
            object? sender,
            EventArgs e)
        {
            StopAutoScroll();
        }


        // =========================================================
        // ESC
        // =========================================================

        private void MainWindow_PreviewKeyDown(
            object sender,
            WpfKeyEventArgs e)
        {
            if (e.Key == Key.Escape &&
                _isFullScreen)
            {
                ExitFullScreen();

                e.Handled = true;
            }
        }


        // =========================================================
        // Load Portfolios
        // =========================================================

        private void LoadPortfolios()
        {
            try
            {
                StopAutoScroll();

                SetBusy(
                    true,
                    "در حال خواندن سبدها...");

                _portfolios =
                    _portfolioManager.LoadAll();

                PortfolioComboBox.ItemsSource =
                    null;

                PortfolioComboBox.ItemsSource =
                    _portfolios;

                CloseAllChartTabs();

                if (_portfolios.Count > 0)
                {
                    PortfolioComboBox.SelectedIndex =
                        0;
                }
                else
                {
                    SymbolsDataGrid.ItemsSource =
                        null;

                    _allSymbols.Clear();

                    _selectedPortfolio =
                        null;

                    StatusTextBlock.Text =
                        "هنوز سبدی تعریف نشده است.";
                }
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(
                    ex.ToString(),
                    "خطا",
                    WpfMessageBoxButton.OK,
                    WpfMessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }


        // =========================================================
        // New Portfolio
        // =========================================================

        private void NewPortfolioButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var window =
                new PortfolioEditorWindow
                {
                    Owner = this
                };

            bool? result =
                window.ShowDialog();

            if (result == true &&
                window.ResultPortfolio != null)
            {
                try
                {
                    _portfolioManager.Save(
                        window.ResultPortfolio);

                    LoadPortfolios();

                    PortfolioComboBox.SelectedItem =
                        _portfolios.FirstOrDefault(
                            x =>
                                x.Name ==
                                window.ResultPortfolio.Name);
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show(
                        ex.ToString(),
                        "خطا در ذخیره سبد",
                        WpfMessageBoxButton.OK,
                        WpfMessageBoxImage.Error);
                }
            }
        }


        // =========================================================
        // Manage Portfolio
        // =========================================================

        // Manage Portfolio
        // =========================================================

        private void ManagePortfolioButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var window =
                new PortfolioManagementWindow(this)
                {
                    Owner = this
                };

            bool? result =
                window.ShowDialog();

            if (result == true)
            {
                LoadPortfolios();
            }
        }


        // =========================================================
        // Top Refresh
        // =========================================================

        private void RefreshPortfolioButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            LoadPortfolios();
        }


        // =========================================================
        // Portfolio Selected
        // =========================================================

        private async void PortfolioComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (PortfolioComboBox.SelectedItem
                is not Portfolio portfolio)
            {
                return;
            }

            _selectedPortfolio =
                portfolio;

            StopAutoScroll();

            CloseAllChartTabs();

            await LoadSymbolsAsync(
                portfolio);
        }


        // =========================================================
        // Load Symbols
        // =========================================================

        private async Task LoadSymbolsAsync(
            Portfolio portfolio)
        {
            try
            {
                SetBusy(
                    true,
                    $"در حال خواندن فهرست نمادهای «{portfolio.Name}»...");

                _suppressSymbolSelection = true;

                SymbolsDataGrid.ItemsSource =
                    null;

                SymbolsDataGrid.SelectedItem =
                    null;

                _allSymbols =
                    await Task.Run(
                        () =>
                            _symbolDataService
                                .GetSymbols(
                                    portfolio));

                RenumberSymbols();

                SymbolsDataGrid.ItemsSource =
                    _allSymbols;

                StatusTextBlock.Text =
                    $"{_allSymbols.Count:N0} نماد در سبد «{portfolio.Name}»";
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(
                    ex.ToString(),
                    "خطا در خواندن نمادها",
                    WpfMessageBoxButton.OK,
                    WpfMessageBoxImage.Error);
            }
            finally
            {
                _suppressSymbolSelection = false;

                SetBusy(false);
            }
        }


        // =========================================================
        // Renumber Symbols
        // =========================================================

        private void RenumberSymbols()
        {
            for (int i = 0;
                 i < _allSymbols.Count;
                 i++)
            {
                _allSymbols[i].RowNumber =
                    i + 1;
            }
        }


        // =========================================================
        // Search
        // =========================================================

        private void SymbolSearchTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            ApplySymbolFilter();
        }


        // =========================================================
        // Apply Symbol Filter
        // =========================================================

        private void ApplySymbolFilter()
        {
            string search =
                SymbolSearchTextBox.Text.Trim();

            IEnumerable<SymbolInfo> query =
                _allSymbols;

            if (!string.IsNullOrWhiteSpace(search))
            {
                query =
                    query.Where(
                        x =>
                            x.Symbol.Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)
                            ||
                            x.DisplayName.Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase));
            }

            List<SymbolInfo> filtered =
                query.ToList();

            SymbolsDataGrid.ItemsSource =
                filtered;
        }


        // =========================================================
        // Symbol Selected
        //
        // عمداً کاری انجام نمی‌دهد.
        //
        // باز کردن نمودار فقط از طریق کلیک روی نام نماد انجام
        // می‌شود تا کلیک روی CheckBox باعث باز شدن نمودار نشود.
        // =========================================================

        private void SymbolsDataGrid_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_suppressSymbolSelection)
                return;

            // عمدی است.
        }


        // =========================================================
        // Symbol Name Click
        // =========================================================

        private async void SymbolNameTextBlock_MouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (_suppressSymbolSelection)
                return;

            if (sender is not FrameworkElement element)
                return;

            if (element.DataContext
                is not SymbolInfo symbol)
            {
                return;
            }

            if (_selectedPortfolio == null)
                return;

            await OpenChartTabAsync(
                symbol,
                _selectedPortfolio);

            e.Handled = true;
        }


        // =========================================================
        // Symbol CheckBox Click
        // =========================================================

        private void SymbolCheckBox_Click(
            object sender,
            RoutedEventArgs e)
        {
            e.Handled = true;
        }


        // =========================================================
        // Reset Selection Radio Buttons
        // =========================================================

        private void ResetSelectionRadioButtons()
        {
            _suppressSymbolSelection = true;

            try
            {
                AllRadioButton.IsChecked =
                    false;

                NoneRadioButton.IsChecked =
                    false;
            }
            finally
            {
                _suppressSymbolSelection = false;
            }
        }


        // =========================================================
        // Update Selection Radio Buttons
        // =========================================================

        private void UpdateSelectionRadioButtons()
        {
            if (_allSymbols.Count == 0)
            {
                AllRadioButton.IsChecked =
                    false;

                NoneRadioButton.IsChecked =
                    false;

                return;
            }

            bool allSelected =
                _allSymbols.All(
                    x => x.IsSelected);

            bool noneSelected =
                _allSymbols.All(
                    x => !x.IsSelected);

            _suppressSymbolSelection = true;

            try
            {
                AllRadioButton.IsChecked =
                    allSelected;

                NoneRadioButton.IsChecked =
                    noneSelected;
            }
            finally
            {
                _suppressSymbolSelection = false;
            }
        }


        // =========================================================
        // Open Chart
        // =========================================================

        public async Task OpenChartTabAsync(
            SymbolInfo symbol,
            Portfolio portfolio)
        {
            await OpenChartTabAsync(
                symbol,
                portfolio,
                false);
        }


        // =========================================================
        // Open Chart
        //
        // replaceCurrentTab:
        // false = تب جدید
        // true  = استفاده مجدد از تب Auto Scroll
        // =========================================================

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


        // =========================================================
        // Tab Header
        // =========================================================

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


        // =========================================================
        // Chart Tab Selection
        // =========================================================

        private void ChartTabs_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (ChartTabs.SelectedItem
                is TabItem tab &&
                tab.Tag is string symbol)
            {
                if (symbol ==
                    "__AUTO_SCROLL__")
                {
                    return;
                }

                StatusTextBlock.Text =
                    $"نمودار {symbol}";
            }
        }


        // =========================================================
        // Close All Tabs
        // =========================================================

        private void CloseAllChartTabs()
        {
            ChartTabs.Items.Clear();
        }


        // =========================================================
        // Close All Chart Tabs Button
        // =========================================================

        private void CloseAllChartsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            StopAutoScroll();

            CloseAllChartTabs();

            StatusTextBlock.Text =
                "همه نمودارها بسته شدند.";
        }


        // =========================================================
        // Refresh Symbols
        // =========================================================

        private async void RefreshSymbolsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_selectedPortfolio == null)
            {
                return;
            }

            await LoadSymbolsAsync(
                _selectedPortfolio);
        }


        // =========================================================
        // All
        // =========================================================

        private void AllRadioButton_Checked(
            object sender,
            RoutedEventArgs e)
        {
            if (_allSymbols == null)
            {
                return;
            }

            foreach (SymbolInfo symbol in _allSymbols)
            {
                symbol.IsSelected =
                    true;
            }

            SymbolsDataGrid.Items.Refresh();

            UpdateSelectionRadioButtons();
        }


        // =========================================================
        // None
        // =========================================================

        private void NoneRadioButton_Checked(
            object sender,
            RoutedEventArgs e)
        {
            if (_allSymbols == null)
            {
                return;
            }

            foreach (SymbolInfo symbol in _allSymbols)
            {
                symbol.IsSelected =
                    false;
            }

            SymbolsDataGrid.Items.Refresh();

            UpdateSelectionRadioButtons();
        }


        // =========================================================
        // Delete Selected Symbols
        // =========================================================

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
                $"آیا می‌خواهید {selected.Count:N0} نماد انتخاب‌شده از سبد «{_selectedPortfolio.Name}» حذف شود؟";

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


        // =========================================================
        // Make Watch
        // =========================================================

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


        // =========================================================
        // Auto Scroll Button
        // =========================================================

        private async void AutoScrollButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_autoScrollRunning)
            {
                StopAutoScroll();
                return;
            }

            await StartAutoScrollAsync();
        }


        // =========================================================
        // Start Auto Scroll
        // =========================================================

        private async Task StartAutoScrollAsync()
        {
            if (_allSymbols.Count == 0)
            {
                WpfMessageBox.Show(
                    "هیچ نمادی برای Auto Scroll وجود ندارد.",
                    "Auto Scroll",
                    WpfMessageBoxButton.OK,
                    WpfMessageBoxImage.Information);

                return;
            }

            if (_selectedPortfolio == null)
            {
                return;
            }

            int currentIndex =
                -1;

            if (SymbolsDataGrid.SelectedItem
                is SymbolInfo selected)
            {
                currentIndex =
                    _allSymbols.IndexOf(
                        selected);
            }

            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            _autoScrollIndex =
                currentIndex;

            _autoScrollRunning =
                true;

            _autoScrollLoading =
                false;

            AutoScrollButton.Content =
                "Stop Auto";

            EnsureAutoScrollTab();

            await OpenAutoScrollSymbolAsync();

            if (!_autoScrollRunning)
                return;

            _autoScrollTimer =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromMilliseconds(
                            _autoScrollIntervalMilliseconds)
                };

            _autoScrollTimer.Tick +=
                AutoScrollTimer_Tick;

            _autoScrollTimer.Start();
        }


        // =========================================================
        // Ensure Auto Scroll Tab
        // =========================================================

        private void EnsureAutoScrollTab()
        {
            TabItem? tab =
                ChartTabs.Items
                    .OfType<TabItem>()
                    .FirstOrDefault(
                        x =>
                            x.Tag is string tag &&
                            tag ==
                            "__AUTO_SCROLL__");

            if (tab != null)
            {
                ChartTabs.SelectedItem =
                    tab;

                return;
            }

            var header =
                new WpfTextBlock
                {
                    Text =
                        "Auto Scroll"
                };

            tab =
                new TabItem
                {
                    Tag =
                        "__AUTO_SCROLL__",

                    Header =
                        header
                };

            ChartTabs.Items.Add(
                tab);

            ChartTabs.SelectedItem =
                tab;
        }


        // =========================================================
        // Auto Scroll Tick
        // =========================================================

        private async void AutoScrollTimer_Tick(
            object? sender,
            EventArgs e)
        {
            if (!_autoScrollRunning ||
                _autoScrollLoading)
            {
                return;
            }

            _autoScrollIndex++;

            if (_autoScrollIndex >=
                _allSymbols.Count)
            {
                StopAutoScroll();

                return;
            }

            await OpenAutoScrollSymbolAsync();
        }


        // =========================================================
        // Open Auto Scroll Symbol
        // =========================================================

        private void OpenAutoScrollSymbol()
        {
            if (_autoScrollIndex < 0 ||
                _autoScrollIndex >=
                _allSymbols.Count)
            {
                return;
            }

            SymbolInfo symbol =
                _allSymbols[
                    _autoScrollIndex];

            _suppressSymbolSelection =
                true;

            try
            {
                SymbolsDataGrid.SelectedItem =
                    symbol;

                SymbolsDataGrid.ScrollIntoView(
                    symbol);
            }
            finally
            {
                _suppressSymbolSelection =
                    false;
            }
        }


        // =========================================================
        // Open Auto Scroll Symbol Async
        // =========================================================

        private async Task OpenAutoScrollSymbolAsync()
        {
            if (!_autoScrollRunning ||
                _autoScrollLoading)
            {
                return;
            }

            if (_autoScrollIndex < 0 ||
                _autoScrollIndex >=
                _allSymbols.Count ||
                _selectedPortfolio == null)
            {
                return;
            }

            _autoScrollLoading =
                true;

            try
            {
                SymbolInfo symbol =
                    _allSymbols[
                        _autoScrollIndex];

                _suppressSymbolSelection =
                    true;

                try
                {
                    SymbolsDataGrid.SelectedItem =
                        symbol;

                    SymbolsDataGrid.ScrollIntoView(
                        symbol);
                }
                finally
                {
                    _suppressSymbolSelection =
                        false;
                }

                await OpenChartTabAsync(
                    symbol,
                    _selectedPortfolio,
                    true);
            }
            finally
            {
                _autoScrollLoading =
                    false;
            }
        }


        // =========================================================
        // Stop Auto Scroll
        // =========================================================

        private void StopAutoScroll()
        {
            _autoScrollRunning =
                false;

            _autoScrollLoading =
                false;

            _autoScrollIndex =
                -1;

            if (_autoScrollTimer != null)
            {
                _autoScrollTimer.Stop();

                _autoScrollTimer.Tick -=
                    AutoScrollTimer_Tick;

                _autoScrollTimer =
                    null;
            }

            if (AutoScrollButton != null)
            {
                AutoScrollButton.Content =
                    "Auto Scroll";
            }
        }


        // =========================================================
        // Full Screen Button
        // =========================================================

        private void FullScreenButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isFullScreen)
            {
                ExitFullScreen();
            }
            else
            {
                EnterFullScreen();
            }
        }


        // =========================================================
        // Exit Button
        // =========================================================

        private void FullScreenExitButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ExitFullScreen();
        }


        // =========================================================
        // Enter Full Screen
        // =========================================================

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


        // =========================================================
        // Exit Full Screen
        // =========================================================

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


        // =========================================================
        // Busy
        // =========================================================

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
    }
}