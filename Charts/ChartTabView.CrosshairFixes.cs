using System;
using TradeIt.Models;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        /// <summary>
        /// Moves the crosshair to the actual X coordinate used by the chart.
        /// The chart currently uses DateTime/OADate coordinates, so the bar index
        /// must never be assigned directly as the X coordinate.
        /// </summary>
        private void UpdateCrosshairAxisLabel(int barIndex)
        {
            if (_bars == null || _bars.Count == 0 || barIndex < 0 || barIndex >= _bars.Count)
                return;

            if (_crosshair == null)
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

            if (bar.Timestamp.HasValue &&
                bar.Timestamp.Value > DateTime.MinValue &&
                bar.Timestamp.Value < DateTime.MaxValue)
            {
                return bar.Timestamp.Value.ToString("yyyy/MM/dd");
            }

            return $"کندل {barIndex + 1}";
        }

        /// <summary>
        /// The main chart is already configured as a DateTime axis by its
        /// Candlestick/Line/Bar drawing code. Do not replace its TickGenerator
        /// here. Replacing it while the axis is a DateTime axis causes the
        /// runtime exception: Date axis must have a ITickGenerator generator.
        /// </summary>
        private void ConfigureBottomAxisForCrosshair()
        {
            // Intentionally empty.
            // DrawChart() owns the axis type and TickGenerator.
        }
    }
}
