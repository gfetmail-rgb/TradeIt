using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfCursors = System.Windows.Input.Cursors;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;

namespace TradeIt.Portfolios
{
    public partial class PortfolioManagementWindow
    {
        private static readonly bool _dataPathHandlerRegistered = RegisterDataPathHandler();

        private static bool RegisterDataPathHandler()
        {
            EventManager.RegisterClassHandler(typeof(PortfolioManagementWindow), Window.LoadedEvent, new RoutedEventHandler(DataPathLoaded));
            return true;
        }

        private static void DataPathLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is PortfolioManagementWindow window && window.DataPathTextBox != null)
            {
                window.DataPathTextBox.Cursor = WpfCursors.Hand;
                window.DataPathTextBox.ToolTip = "برای باز کردن فایل داده کلیک کنید";
                window.DataPathTextBox.MouseLeftButtonUp -= window.OpenDataPathTextFile;
                window.DataPathTextBox.MouseLeftButtonUp += window.OpenDataPathTextFile;
            }
        }

        private void OpenDataPathTextFile(object sender, MouseButtonEventArgs e)
        {
            string path = DataPathTextBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                if (File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                    e.Handled = true;
                    return;
                }

                if (Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{path}\"", UseShellExecute = true });
                    e.Handled = true;
                    return;
                }

                WpfMessageBox.Show("فایل یا پوشه داده در مسیر ثبت‌شده پیدا نشد.", "مسیر داده", WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"باز کردن مسیر داده امکان‌پذیر نیست.\n\n{ex.Message}", "خطا", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
            }
        }

        private bool _detailsFixHandlerAttached;
        private static readonly bool _detailsFixClassHandlerRegistered = RegisterDetailsFixClassHandler();

        private static bool RegisterDetailsFixClassHandler()
        {
            EventManager.RegisterClassHandler(typeof(PortfolioManagementWindow), Window.LoadedEvent, new RoutedEventHandler(DetailsFixLoadedClassHandler));
            return true;
        }

        private static void DetailsFixLoadedClassHandler(object sender, RoutedEventArgs e)
        {
            if (sender is PortfolioManagementWindow window)
                window.AttachPortfolioDetailsFix();
        }

        private void AttachPortfolioDetailsFix()
        {
            if (_detailsFixHandlerAttached)
                return;

            _detailsFixHandlerAttached = true;
            PortfolioListBox.SelectionChanged += PortfolioDetailsFix_SelectionChanged;
        }

        private void PortfolioDetailsFix_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedPortfolio == null)
                return;

            ShowCalendarAndDateTimeDetails(_selectedPortfolio);
        }

        private void ShowCalendarAndDateTimeDetails(Models.Portfolio portfolio)
        {
            var dataSource = portfolio.DataSource;

            CalendarTextBox.Text = dataSource?.Calendar switch
            {
                "Persian" => "شمسی",
                "Gregorian" => "میلادی",
                string value when !string.IsNullOrWhiteSpace(value) => value,
                _ => ""
            };

            DateFormatTextBox.Text = dataSource?.DateFormat ?? "";
            TimeFormatTextBox.Text = dataSource?.TimeFormat ?? "";
        }

        private static readonly bool _startupSelectionHandlerRegistered = RegisterStartupSelectionHandler();

        private static bool RegisterStartupSelectionHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(PortfolioManagementWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(PortfolioManagementWindow_UserOptionsLoaded),
                true);
            return true;
        }

        private static void PortfolioManagementWindow_UserOptionsLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not PortfolioManagementWindow window)
                return;

            window.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (window.PortfolioListBox.Items.Count == 0)
                    return;

                window.PortfolioListBox.SelectedIndex = -1;
                window._selectedPortfolio = null;
                window._symbols.Clear();
                window.SymbolsDataGrid.ItemsSource = null;
                window.ClearPortfolioDetails();
                window.StatusTextBlock.Text = "یک سبد را انتخاب کنید.";
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
    }
}
