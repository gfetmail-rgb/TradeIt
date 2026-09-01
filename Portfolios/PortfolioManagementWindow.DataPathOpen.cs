using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

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
                window.DataPathTextBox.Cursor = Cursors.Hand;
                window.DataPathTextBox.ToolTip = "برای باز کردن فایل داده کلیک کنید";
                window.DataPathTextBox.MouseLeftButtonUp -= window.OpenDataPathTextFile;
                window.DataPathTextBox.MouseLeftButtonUp += window.OpenDataPathTextFile;
            }
        }

        private void OpenDataPathTextFile(object sender, MouseButtonEventArgs e)
        {
            string path = DataPathTextBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                if (File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                    e.Handled = true;
                    return;
                }

                if (Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{path}\"",
                        UseShellExecute = true
                    });
                    e.Handled = true;
                    return;
                }

                MessageBox.Show("فایل یا پوشه داده در مسیر ثبت‌شده پیدا نشد.", "مسیر داده", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"باز کردن مسیر داده امکان‌پذیر نیست.\n\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
