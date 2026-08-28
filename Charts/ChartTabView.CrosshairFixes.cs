using System;
using System.Collections.Generic;
using System.Linq;
using TradeIt.Models;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        /// <summary>
        /// Updates the bottom axis labels without assigning a generic TickGenerator
        /// to a DateTime axis. ScottPlot requires a DateTime-compatible generator
        /// whenever the bottom axis is a date axis.
        /// </summary>
        private void UpdateCrosshairAxisLabel(int barIndex)
        {
            if (_bars == null || _bars.Count == 0 || barIndex < 0 || barIndex >= _bars.Count)
                return;

            if (_crosshair == null)
                return;

            double x = barIndex;
            _crosshair.Position = new ScottPlot.Coordinates(x, _crosshair.Position.Y);

            string label = GetCrosshairXLabel(barIndex);

            // Use the crosshair's own annotation rather than replacing the axis
            // TickGenerator. This works for both numeric and DateTime axes.
            _crosshair.VerticalLine.Text = label;
        }

        private string GetCrosshairXLabel(int barIndex)
        {
            if (barIndex < 0 || barIndex >= _bars.Count)
                return string.Empty;

            MarketBar bar = _bars[barIndex];

            // Prefer an actual timestamp when the data contains one.
            DateTime? timestamp = TryGetBarTimestamp(bar);
            if (timestamp.HasValue)
                return timestamp.Value.ToString("yyyy/MM/dd");

            return $"کندل {barIndex + 1}";
        }

        /// <summary>
        /// Returns the bar timestamp only when the data actually contains one.
        /// No artificial time is created for date-less data.
        /// </summary>
        private static DateTime? TryGetBarTimestamp(MarketBar bar)
        {
            // MarketBar in the current project exposes its date/time through
            // the DateTime property. Keep the check centralized so callers do
            // not accidentally treat candle index as a timestamp.
            DateTime value = bar.DateTime;
            return value == default ? null : value;
        }

        /// <summary>
        /// Configure the bottom axis according to the actual data type.
        /// IMPORTANT: never assign a generic TickGenerator to a DateTime axis.
        /// </summary>
        private void ConfigureBottomAxisForCrosshair()
        {
            if (_bars == null || _bars.Count == 0)
                return;

            bool hasDateTime = _bars.Any(b => TryGetBarTimestamp(b).HasValue);

            if (hasDateTime)
            {
                Chart.Plot.Axes.DateTimeTicksBottom();
            }
            else
            {
                Chart.Plot.Axes.NumericTicksBottom();
            }
        }
    }
}
