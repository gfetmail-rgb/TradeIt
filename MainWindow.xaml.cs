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
            StopAutoScrollController();
            _autoScrollController.Dispose();
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
                StopAutoScrollController();

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

            StopAutoScrollController();

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




        // =========================================================
        // Tab Header
        // =========================================================




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
            StopAutoScrollController();

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




        // =========================================================
        // Make Watch
        // =========================================================




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




        // =========================================================
        // Exit Full Screen
        // =========================================================




        // =========================================================
        // Busy
        // =========================================================


    }
}