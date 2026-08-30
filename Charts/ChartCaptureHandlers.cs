using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using WpfPrintDialog = System.Windows.Controls.PrintDialog;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void ScreenshotChartAreaButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChartPrintArea.UpdateLayout();

                if (ChartPrintArea.ActualWidth <= 0 || ChartPrintArea.ActualHeight <= 0)
                    return;

                int width = Math.Max(1, (int)Math.Ceiling(ChartPrintArea.ActualWidth));
                int height = Math.Max(1, (int)Math.Ceiling(ChartPrintArea.ActualHeight));

                var bitmap = new RenderTargetBitmap(
                    width,
                    height,
                    96,
                    96,
                    PixelFormats.Pbgra32);

                bitmap.Render(ChartPrintArea);

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "ذخیره تصویر نمودار",
                    Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg",
                    FileName = $"{_symbol.Symbol}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };

                if (dialog.ShowDialog() != true)
                    return;

                BitmapEncoder encoder =
                    Path.GetExtension(dialog.FileName).Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                        ? new JpegBitmapEncoder()
                        : new PngBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using FileStream stream = new FileStream(
                    dialog.FileName,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None);

                encoder.Save(stream);
                BottomInfoTextBlock.Text = $"تصویر نمودار ذخیره شد: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(
                    $"خطا در گرفتن تصویر نمودار:\n{ex.Message}",
                    "Screenshot",
                    WpfMessageBoxButton.OK,
                    WpfMessageBoxImage.Error);
            }
        }

        private void PrintChartAreaButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChartPrintArea.UpdateLayout();

                var dialog = new WpfPrintDialog();
                if (dialog.ShowDialog() != true)
                    return;

                dialog.PrintVisual(
                    ChartPrintArea,
                    $"TradeIt - {_symbol.Symbol}");

                BottomInfoTextBlock.Text = "فقط محدوده نمودار برای چاپ ارسال شد.";
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(
                    $"خطا در چاپ نمودار:\n{ex.Message}",
                    "Print",
                    WpfMessageBoxButton.OK,
                    WpfMessageBoxImage.Error);
            }
        }
    }
}
