using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using TradeIt.Models;
using TradeIt.Services;
using WpfCursors = System.Windows.Input.Cursors;
namespace TradeIt.Portfolios
{
    public partial class PortfolioManagementWindow : Window
    {
        // =========================================================
        // Services
        // =========================================================

        private readonly PortfolioManager _portfolioManager;

        private readonly SymbolDataService _symbolDataService;


        // =========================================================
        // MainWindow
        // =========================================================

        private readonly MainWindow _mainWindow;


        // =========================================================
        // Data
        // =========================================================

        private List<Portfolio> _portfolios =
            new();

        private Portfolio? _selectedPortfolio;

        private List<SymbolInfo> _symbols =
            new();


        // =========================================================
        // Loading Control
        // =========================================================

        private CancellationTokenSource?
            _symbolsLoadingCancellation;

        private int _loadGeneration;


        // =========================================================
        // Constructor
        // =========================================================

        public PortfolioManagementWindow(
            MainWindow mainWindow)
        {
            InitializeComponent();

            _mainWindow =
                mainWindow;

            _portfolioManager =
                new PortfolioManager();

            _symbolDataService =
                new SymbolDataService();

            Loaded +=
                PortfolioManagementWindow_Loaded;

            Closed +=
                PortfolioManagementWindow_Closed;
        }


        // =========================================================
        // Loaded
        // =========================================================

        private void PortfolioManagementWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            LoadPortfolios();
        }


        // =========================================================
        // Closed
        // =========================================================

        private void PortfolioManagementWindow_Closed(
            object? sender,
            EventArgs e)
        {
            CancelCurrentSymbolLoading();
        }


        // =========================================================
        // Load Portfolios
        // =========================================================

        private void LoadPortfolios()
        {
            try
            {
                CancelCurrentSymbolLoading();

                _portfolios =
                    _portfolioManager
                        .LoadAll()
                        .OrderBy(
                            x => x.Name,
                            StringComparer.OrdinalIgnoreCase)
                        .ToList();

                PortfolioListBox.ItemsSource =
                    null;

                PortfolioListBox.ItemsSource =
                    _portfolios;

                SymbolsDataGrid.ItemsSource =
                    null;

                _symbols.Clear();

                _selectedPortfolio =
                    null;

                ClearPortfolioDetails();

                if (_portfolios.Count > 0)
                {
                    PortfolioListBox.SelectedIndex =
                        0;
                }
                else
                {
                    StatusTextBlock.Text =
                        "هیچ سبدی وجود ندارد.";
                }
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(
                    ex.ToString(),
                    "خطا در بارگذاری سبدها",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // =========================================================
        // Refresh
        // =========================================================

        private void RefreshButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            LoadPortfolios();
        }


        // =========================================================
        // Portfolio Selection
        // =========================================================

        private async void PortfolioListBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            /*
             * وقتی چند سبد انتخاب شده‌اند، فقط اولین سبد
             * برای نمایش جزئیات استفاده می‌شود.
             *
             * اما SelectedItems همچنان برای حذف چندتایی
             * قابل استفاده است.
             */

            if (PortfolioListBox.SelectedItems.Count == 0)
            {
                _selectedPortfolio =
                    null;

                ClearPortfolioDetails();

                return;
            }


            Portfolio? portfolio =
                PortfolioListBox.SelectedItems[0]
                    as Portfolio;

            if (portfolio == null)
            {
                return;
            }

            _selectedPortfolio =
                portfolio;


            await LoadSelectedPortfolioAsync(
                portfolio);
        }


        // =========================================================
        // Load Selected Portfolio Async
        // =========================================================

        private async Task LoadSelectedPortfolioAsync(
            Portfolio portfolio)
        {
            int generation =
                Interlocked.Increment(
                    ref _loadGeneration);

            CancelCurrentSymbolLoading();

            _symbolsLoadingCancellation =
                new CancellationTokenSource();

            CancellationToken cancellationToken =
                _symbolsLoadingCancellation.Token;


            Mouse.OverrideCursor =
                WpfCursors.Wait;


            ShowPortfolioDetails(
                portfolio);


            SymbolsDataGrid.ItemsSource =
                null;

            _symbols.Clear();


            int declaredSymbolCount =
                portfolio.Symbols?.Count ?? 0;


            SelectedPortfolioInfoTextBlock.Text =
                $"تعداد نمادها: {declaredSymbolCount:N0}";


            StatusTextBlock.Text =
                $"در حال بارگذاری نمادهای سبد «{portfolio.Name}» ...";


            try
            {
                List<SymbolInfo> symbols =
                    await _symbolDataService
                        .GetSymbolsAsync(
                            portfolio,
                            cancellationToken);


                cancellationToken.ThrowIfCancellationRequested();


                if (generation != _loadGeneration ||
                    !ReferenceEquals(
                        _selectedPortfolio,
                        portfolio))
                {
                    return;
                }


                _symbols =
                    symbols;


                for (int i = 0;
                     i < _symbols.Count;
                     i++)
                {
                    _symbols[i].RowNumber =
                        i + 1;
                }


                SymbolsDataGrid.ItemsSource =
                    _symbols;


                SelectedPortfolioInfoTextBlock.Text =
                    $"تعداد نمادها: {_symbols.Count:N0}";


                StatusTextBlock.Text =
                    $"سبد «{portfolio.Name}» — {_symbols.Count:N0} نماد";
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (generation != _loadGeneration)
                {
                    return;
                }

                System.Windows.MessageBox.Show(
                    ex.ToString(),
                    "خطا در خواندن نمادها",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                StatusTextBlock.Text =
                    "خطا در بارگذاری نمادها.";
            }
            finally
            {
                Mouse.OverrideCursor =
                    null;
            }
        }


        // =========================================================
        // Cancel Current Loading
        // =========================================================

        private void CancelCurrentSymbolLoading()
        {
            if (_symbolsLoadingCancellation == null)
            {
                return;
            }

            try
            {
                _symbolsLoadingCancellation.Cancel();
            }
            catch
            {
            }

            _symbolsLoadingCancellation.Dispose();

            _symbolsLoadingCancellation =
                null;
        }


        // =========================================================
        // Portfolio Details
        // =========================================================

        private void ShowPortfolioDetails(
            Portfolio portfolio)
        {
            PortfolioNameTextBox.Text =
                portfolio.Name ?? "";


            if (portfolio.DataSource != null)
            {
                SourceTypeTextBox.Text =
                    portfolio.DataSource.SourceType ?? "";

                DataPathTextBox.Text =
                    portfolio.DataSource.Path ?? "";

                SymbolSourceTextBox.Text =
                    portfolio.DataSource.SymbolSource ?? "";

                DataTypeTextBox.Text =
                    portfolio.DataSource.DataType ?? "";

                DelimiterTextBox.Text =
                    portfolio.DataSource.Delimiter.ToString();

                HeaderTextBox.Text =
                    portfolio.DataSource.HasHeader
                        ? "بله"
                        : "خیر";
            }
            else
            {
                SourceTypeTextBox.Text =
                    "";

                DataPathTextBox.Text =
                    "";

                SymbolSourceTextBox.Text =
                    "";

                DataTypeTextBox.Text =
                    "";

                DelimiterTextBox.Text =
                    "";

                HeaderTextBox.Text =
                    "";
            }


            int symbolCount =
                portfolio.Symbols?.Count ?? 0;


            SymbolCountTextBox.Text =
                symbolCount.ToString("N0");


            SelectedPortfolioInfoTextBlock.Text =
                $"تعداد نمادها: {symbolCount:N0}";


            StatusTextBlock.Text =
                $"سبد «{portfolio.Name}»";
        }


        // =========================================================
        // Portfolio Double Click
        // =========================================================

        private async void PortfolioListBox_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            /*
             * دابل کلیک فقط اولین سبد انتخاب‌شده را
             * برای نمایش جزئیات فعال می‌کند.
             */

            if (PortfolioListBox.SelectedItems.Count == 0)
            {
                return;
            }


            if (PortfolioListBox.SelectedItems[0]
                is not Portfolio portfolio)
            {
                return;
            }


            _selectedPortfolio =
                portfolio;


            await LoadSelectedPortfolioAsync(
                portfolio);
        }


        // =========================================================
        // Delete Multiple Portfolios
        // =========================================================

        private void DeletePortfolioButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            List<Portfolio> selectedPortfolios =
                PortfolioListBox.SelectedItems
                    .OfType<Portfolio>()
                    .ToList();


            if (selectedPortfolios.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "ابتدا حداقل یک سبد را انتخاب کنید.",
                    "حذف سبد",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }


            string names =
                string.Join(
                    "\n",
                    selectedPortfolios
                        .Select(
                            x => $"• {x.Name}"));


            string message;


            if (selectedPortfolios.Count == 1)
            {
                message =
                    $"آیا مطمئن هستید که سبد «{selectedPortfolios[0].Name}» به طور کامل حذف شود؟";
            }
            else
            {
                message =
                    $"آیا مطمئن هستید که {selectedPortfolios.Count:N0} سبد زیر حذف شوند؟\n\n" +
                    names;
            }


            message +=
                "\n\nفقط اطلاعات سبدها حذف می‌شود و فایل‌های دیتای واقعی سهم‌ها حذف نخواهند شد.";


            MessageBoxResult answer =
                System.Windows.MessageBox.Show(
                    message,
                    "حذف سبدها",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);


            if (answer !=
                MessageBoxResult.Yes)
            {
                return;
            }


            try
            {
                CancelCurrentSymbolLoading();


                foreach (Portfolio portfolio
                         in selectedPortfolios)
                {
                    _portfolioManager.Delete(
                        portfolio.Name);
                }


                _selectedPortfolio =
                    null;


                _symbols.Clear();


                SymbolsDataGrid.ItemsSource =
                    null;


                ClearPortfolioDetails();


                LoadPortfolios();


                StatusTextBlock.Text =
                    $"{selectedPortfolios.Count:N0} سبد حذف شد.";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    ex.ToString(),
                    "خطا در حذف سبدها",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // =========================================================
        // Edit Portfolio
        // =========================================================

        private void EditPortfolioButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            /*
             * ویرایش فقط زمانی انجام می‌شود که دقیقاً
             * یک سبد انتخاب شده باشد.
             */

            List<Portfolio> selectedPortfolios =
                PortfolioListBox.SelectedItems
                    .OfType<Portfolio>()
                    .ToList();


            if (selectedPortfolios.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "ابتدا یک سبد را انتخاب کنید.",
                    "ویرایش سبد",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }


            if (selectedPortfolios.Count > 1)
            {
                System.Windows.MessageBox.Show(
                    "برای ویرایش، فقط یک سبد را انتخاب کنید.",
                    "ویرایش سبد",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }


            Portfolio selectedPortfolio =
                selectedPortfolios[0];


            try
            {
                var editor =
                    new PortfolioEditorWindow
                    {
                        Owner = this
                    };


                bool? result =
                    editor.ShowDialog();


                if (result == true &&
                    editor.ResultPortfolio != null)
                {
                    _portfolioManager.Save(
                        editor.ResultPortfolio);


                    LoadPortfolios();


                    Portfolio? savedPortfolio =
                        _portfolios.FirstOrDefault(
                            x =>
                                x.Name ==
                                editor.ResultPortfolio.Name);


                    if (savedPortfolio != null)
                    {
                        PortfolioListBox.SelectedItem =
                            savedPortfolio;
                    }


                    StatusTextBlock.Text =
                        $"سبد «{editor.ResultPortfolio.Name}» ذخیره شد.";
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    ex.ToString(),
                    "خطا در ویرایش سبد",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // =========================================================
        // Click Symbol Name -> Open Chart
        // =========================================================

        private async void SymbolTextBlock_MouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not TextBlock textBlock)
            {
                return;
            }


            if (textBlock.DataContext
                is not SymbolInfo symbol)
            {
                return;
            }


            if (_selectedPortfolio == null)
            {
                System.Windows.MessageBox.Show(
                    "ابتدا یک سبد را انتخاب کنید.",
                    "باز کردن چارت",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }


            try
            {
                e.Handled =
                    true;


                await _mainWindow.OpenChartTabAsync(
                    symbol,
                    _selectedPortfolio);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    ex.ToString(),
                    "خطا در باز کردن چارت",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // =========================================================
        // Open Symbol Data File
        // =========================================================

        private void OpenSymbolDataFile(
            SymbolInfo symbol)
        {
            if (string.IsNullOrWhiteSpace(
                symbol.FilePath))
            {
                System.Windows.MessageBox.Show(
                    "مسیر فایل دیتای این نماد مشخص نیست.",
                    "فایل داده",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }


            if (!File.Exists(
                symbol.FilePath))
            {
                System.Windows.MessageBox.Show(
                    $"فایل زیر پیدا نشد:\n\n{symbol.FilePath}",
                    "فایل داده",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName =
                            symbol.FilePath,

                        UseShellExecute =
                            true
                    });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    ex.ToString(),
                    "خطا در باز کردن فایل",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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
               System.Windows.MessageBox.Show(
                    "ابتدا یک سبد را انتخاب کنید.",
                    "حذف نماد",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }


            /*
             * اول CheckBoxهای انتخاب‌شده را بررسی می‌کنیم.
             */

            List<SymbolInfo> selected =
                _symbols
                    .Where(
                        x => x.IsSelected)
                    .ToList();


            /*
             * اگر CheckBox انتخاب نشده باشد،
             * از Selection خود DataGrid استفاده می‌کنیم.
             */

            if (selected.Count == 0)
            {
                selected =
                    SymbolsDataGrid.SelectedItems
                        .OfType<SymbolInfo>()
                        .ToList();
            }


            if (selected.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "هیچ نمادی انتخاب نشده است.",
                    "حذف نماد",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }


            MessageBoxResult answer =
                System.Windows.MessageBox.Show(
                    $"آیا می‌خواهید {selected.Count:N0} نماد از سبد «{_selectedPortfolio.Name}» حذف شود؟\n\n" +
                    "فقط نمادها از سبد حذف می‌شوند و فایل‌های واقعی دیتای آنها پاک نخواهند شد.",
                    "حذف نمادها",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);


            if (answer !=
                MessageBoxResult.Yes)
            {
                return;
            }


            try
            {
                HashSet<string> paths =
                    selected
                        .Select(
                            x => x.FilePath)
                        .Where(
                            x =>
                                !string.IsNullOrWhiteSpace(x))
                        .ToHashSet(
                            StringComparer.OrdinalIgnoreCase);


                if (_selectedPortfolio.Symbols != null)
                {
                    _selectedPortfolio.Symbols =
                        _selectedPortfolio.Symbols
                            .Where(
                                x =>
                                    x == null ||
                                    !paths.Contains(
                                        x.FilePath))
                            .ToList();
                }


                _portfolioManager.Save(
                    _selectedPortfolio);


                Portfolio currentPortfolio =
                    _selectedPortfolio;


                _ = LoadSelectedPortfolioAsync(
                    currentPortfolio);


                StatusTextBlock.Text =
                    $"{selected.Count:N0} نماد حذف شد.";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    ex.ToString(),
                    "خطا در حذف نمادها",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        // =========================================================
        // Clear Details
        // =========================================================

        private void ClearPortfolioDetails()
        {
            PortfolioNameTextBox.Text =
                "";

            SourceTypeTextBox.Text =
                "";

            DataPathTextBox.Text =
                "";

            SymbolSourceTextBox.Text =
                "";

            DataTypeTextBox.Text =
                "";

            CalendarTextBox.Text =
                "";

            DelimiterTextBox.Text =
                "";

            DateFormatTextBox.Text =
                "";

            TimeFormatTextBox.Text =
                "";

            SymbolCountTextBox.Text =
                "";

            HeaderTextBox.Text =
                "";

            SelectedPortfolioInfoTextBlock.Text =
                "";

            StatusTextBlock.Text =
                "آماده";
        }


        // =========================================================
        // Close
        // =========================================================

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}