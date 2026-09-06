using System;
using System.Windows;
using TradeIt.Models;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _chartDataInvalid;

        private bool ValidateChartData()
        {
            if (_bars == null || _bars.Count == 0)
                return false;

            for (int i = 0; i < _bars.Count; i++)
            {
                MarketBar bar = _bars[i];
                string? reason = GetInvalidBarReason(bar);
                if (reason != null)
                {
                    ShowInvalidChartDataMessage(i, reason);
                    return false;
                }
            }

            return true;
        }

        private static string? GetInvalidBarReason(MarketBar bar)
        {
            if (!IsFinitePositive(bar.Open) || !IsFinitePositive(bar.High) ||
                !IsFinitePositive(bar.Low) || !IsFinitePositive(bar.Close))
                return "مقادیر قیمت نامعتبر یا غیرعددی هستند";

            if (double.IsNaN(bar.Volume) || double.IsInfinity(bar.Volume) || bar.Volume < 0)
                return "حجم نامعتبر است";

            if (bar.High < bar.Low)
                return "بیشینه قیمت از کمینه قیمت کمتر است";

            if (bar.High < Math.Max(bar.Open, bar.Close))
                return "بیشینه قیمت از قیمت باز یا پایانی کمتر است";

            if (bar.Low > Math.Min(bar.Open, bar.Close))
                return "کمینه قیمت از قیمت باز یا پایانی بیشتر است";

            return null;
        }

        private void ShowInvalidChartDataMessage(int barIndex, string reason)
        {
            if (_chartDataInvalid)
                return;

            _chartDataInvalid = true;

            Chart.Plot.Clear();
            Chart.Visibility = Visibility.Collapsed;

            ChartInfoTextBlock.Text = "";
            BottomInfoTextBlock.Text =
                $"چارت نماد «{_symbol.Symbol}» به دلیل خرابی یا ناقص بودن داده‌ها قابل نمایش نیست.";

            System.Windows.MessageBox.Show(
                $"چارت نماد «{_symbol.Symbol}» به دلیل خرابی یا ناقص بودن داده‌ها قابل نمایش نیست.\n\n" +
                $"ردیف داده: {barIndex + 1:N0}\nعلت: {reason}\n\nداده‌های فایل منبع تغییر داده نشده‌اند.",
                "داده خراب",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private static bool IsFinitePositive(double value)
        {
            return !double.IsNaN(value) &&
                   !double.IsInfinity(value) &&
                   value > 0;
        }
    }
}
