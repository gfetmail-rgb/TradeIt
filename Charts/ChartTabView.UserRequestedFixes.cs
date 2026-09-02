using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _userRequestedFixesRegistered = RegisterUserRequestedFixes();

        private static bool RegisterUserRequestedFixes()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(UserRequestedChartLoaded));
            return true;
        }

        private static void UserRequestedChartLoaded(object sender, RoutedEventArgs e)
        {
            // Chart mouse handling is centralized in ChartTabView.DisplayFixes.cs.
            // Do not attach another MouseMove handler here: the old duplicate handler
            // could call Plot.GetCoordinates() before the timestamp-less axis was ready.
        }

        private void ScreenshotChartOnlyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chart.UpdateLayout();
                int width = Math.Max(1, (int)Math.Ceiling(Chart.ActualWidth));
                int height = Math.Max(1, (int)Math.Ceiling(Chart.ActualHeight));
                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(Chart);

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "ذخیره تصویر نمودار",
                    Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg",
                    FileName = $"{_symbol.Symbol}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };
                if (dialog.ShowDialog() != true)
                    return;

                BitmapEncoder encoder = Path.GetExtension(dialog.FileName).Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                    ? new JpegBitmapEncoder()
                    : new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using FileStream stream = new FileStream(dialog.FileName, FileMode.Create);
                encoder.Save(stream);
                BottomInfoTextBlock.Text = $"تصویر نمودار ذخیره شد: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطا در گرفتن تصویر نمودار:\n{ex.Message}", "Screenshot", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintChartOnlyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new System.Windows.Controls.PrintDialog();
                if (dialog.ShowDialog() != true)
                    return;
                dialog.PrintVisual(Chart, $"TradeIt - {_symbol.Symbol}");
                BottomInfoTextBlock.Text = "فقط ناحیه اصلی نمودار برای چاپ ارسال شد.";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطا در چاپ نمودار:\n{ex.Message}", "Print", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
