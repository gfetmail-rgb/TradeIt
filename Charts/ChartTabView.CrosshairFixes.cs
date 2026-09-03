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
            string sourceDate = bar.JalaliDate?.Trim() ?? string.Empty;

            // The chart must never invent a date. If the source has no date,
            // identify the bar by its candle number instead.
            if (string.IsNullOrWhiteSpace(sourceDate))
                return $"کندل {barIndex + 1}";

            string time = bar.Time?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(time))
                return $"{sourceDate} {time}";

            // Preserve the source date text exactly (including its calendar
            // and digit style) rather than converting it to another format.
            return sourceDate;
        }

        private string GetSourceDateLabel(int barIndex)
        {
            if (barIndex < 0 || barIndex >= _bars.Count)
                return string.Empty;

            return GetCrosshairXLabel(barIndex);
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
            // DrawChart() and the final chart-fix pass own the bottom-axis tick generator.
        }
    }
}