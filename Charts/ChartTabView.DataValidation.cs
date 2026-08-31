using System;
using System.Linq;
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

                if (!IsFinitePositive(bar.Open) ||
                    !IsFinitePositive(bar.High) ||
                    !IsFinitePositive(bar.Low) ||
                    !IsFinitePositive(bar.Close) ||
                    double.IsNaN(bar.Volume) ||
                    double.IsInfinity(bar.Volume) ||
                    bar.Volume < 0 ||
                    bar.High < bar.Low ||
                    bar.High < Math.Max(bar.Open, bar.Close) ||
                    bar.Low > Math.Min(bar.Open, bar.Close))
                {
                    ShowInvalidChartDataMessage();
                    return false;
                }
            }

            return true;
        }

        private void ShowInvalidChartDataMessage()
        {
            if (_chartDataInvalid)
                return;

            _chartDataInvalid = true;

            Chart.Plot.Clear();
            VolumeChart.Plot.Clear();
            Chart.Visibility = Visibility.Collapsed;
            VolumeContainer.Visibility = Visibility.Collapsed;

            ChartInfoTextBlock.Text = "";
            BottomInfoTextBlock.Text =
                $"چارت نماد «{_symbol.Symbol}» به دلیل خرابی یا ناقص بودن داده‌ها قابل نمایش نیست.";

            System.Windows.MessageBox.Show(
                $"چارت نماد «{_symbol.Symbol}» به دلیل خرابی یا ناقص بودن داده‌ها قابل نمایش نیست.\n\nداده‌های فایل منبع تغییر داده نشده‌اند.",
                "داده خراب",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }

        private static bool IsFinitePositive(double value)
        {
            return !double.IsNaN(value) &&
                   !double.IsInfinity(value) &&
                   value > 0;
        }
    }
}
