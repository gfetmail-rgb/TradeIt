using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
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
    }
}
