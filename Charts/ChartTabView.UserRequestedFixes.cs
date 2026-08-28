using System;
using System.IO;
using System.Linq;
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
            if (sender is not ChartTabView chart)
                return;

            chart.Chart.MouseMove -= chart.UserFixesChart_MouseMove;
            chart.Chart.MouseMove += chart.UserFixesChart_MouseMove;
        }

        private void UserFixesChart_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_crosshair == null || !_crosshairVisible || !_chartVisible || _bars.Count == 0)
                return;

            var p = e.GetPosition(Chart);
            if (!TryGetChartCoordinates(Chart, p, out ScottPlot.Coordinates coordinates))
                return;

            int nearestIndex = Enumerable.Range(0, _bars.Count)
                .OrderBy(i => Math.Abs(GetBarDateTime(_bars[i], i).ToOADate() - coordinates.X))
                .First();

            DateTime barTime = GetBarDateTime(_bars[nearestIndex], nearestIndex);
            double x = barTime.ToOADate();
            var limits = Chart.Plot.Axes.GetLimits();
            double y = Math.Clamp(coordinates.Y, limits.Bottom, limits.Top);

            _crosshair.Position = new ScottPlot.Coordinates(x, y);
            _crosshair.IsVisible = true;
            _crosshairMouseInside = true;

            var bar = _bars[nearestIndex];
            bool hasTime = bar.Timestamp.HasValue && bar.Timestamp.Value > DateTime.MinValue && bar.Timestamp.Value < DateTime.MaxValue;
            string dateText = hasTime ? barTime.ToString("yyyy/MM/dd") : $"کندل {nearestIndex + 1}";
            string timeText = hasTime && barTime.TimeOfDay != TimeSpan.Zero ? $" {barTime:HH:mm}" : "";

            ChartInfoTextBlock.Text =
                $"{_symbol.Symbol}    O: {bar.Open:N2}  H: {bar.High:N2}  L: {bar.Low:N2}  C: {bar.Close:N2}  V: {bar.Volume:N0}    {dateText}{timeText}";

            Chart.Refresh();
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