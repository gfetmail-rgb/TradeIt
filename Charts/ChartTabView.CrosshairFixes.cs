using System;
using TradeIt.Models;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void UpdateCrosshairAxisLabel(int barIndex)
        {
            if (_bars == null || _bars.Count == 0 || barIndex < 0 || barIndex >= _bars.Count || _crosshair == null)
                return;

            DateTime barTime = GetBarDateTime(_bars[barIndex], barIndex);
            _crosshair.Position = new ScottPlot.Coordinates(
                barTime.ToOADate(),
                _crosshair.Position.Y);
            _crosshair.VerticalLine.Text = GetCrosshairXLabel(barIndex);
        }

        private string GetCrosshairXLabel(int barIndex)
        {
            if (barIndex < 0 || barIndex >= _bars.Count)
                return string.Empty;

            MarketBar bar = _bars[barIndex];
            string sourceDate = NormalizeSourceDate(bar.JalaliDate);

            // Never manufacture a date for a source which has no date.
            if (string.IsNullOrWhiteSpace(sourceDate))
                return $"کندل {barIndex + 1}";

            string time = NormalizeSourceDate(bar.Time);
            if (!string.IsNullOrWhiteSpace(time))
                return $"{sourceDate} {time}";

            return sourceDate;
        }

        private string GetSourceDateLabel(int barIndex)
        {
            if (barIndex < 0 || barIndex >= _bars.Count)
                return string.Empty;

            return GetCrosshairXLabel(barIndex);
        }

        private static string NormalizeSourceDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.Trim()
                .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
                .Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9')
                .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
                .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9');
        }

        private void InitializeCrosshairAtInitialPosition()
        {
            if (_crosshair == null || !_chartVisible || _bars.Count == 0)
                return;

            int index = _bars.Count - 1;
            DateTime time = GetBarDateTime(_bars[index], index);
            double y = _bars[index].Close;

            _crosshair.Position = new ScottPlot.Coordinates(time.ToOADate(), y);
            _crosshair.HorizontalLine.Text = y.ToString("N2");
            _crosshair.VerticalLine.Text = GetCrosshairXLabel(index);
            _crosshairVisible = true;
            _crosshairMouseInside = true;
            _crosshair.IsVisible = true;
        }

        private void ConfigureBottomAxisForCrosshair()
        {
            // DrawChart() owns the axis type and TickGenerator.
        }
    }
}