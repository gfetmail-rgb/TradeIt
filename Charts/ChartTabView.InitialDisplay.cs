using System;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private const int InitialVisibleCandleCount = 200;
        private const double InitialRightBlankFraction = 0.25;
        private bool _initialDisplayApplied;

        private void ApplyInitialDisplayRange()
        {
            if (_initialDisplayApplied || _bars.Count == 0 || !IsLoaded)
                return;

            try
            {
                int visibleCount = Math.Min(InitialVisibleCandleCount, _bars.Count);
                int firstIndex = _bars.Count - visibleCount;
                int lastIndex = _bars.Count - 1;

                double firstX;
                double lastX;

                if (_continuousTimeAxisApplied)
                {
                    firstX = ContinuousX(firstIndex);
                    lastX = ContinuousX(lastIndex);
                }
                else
                {
                    firstX = GetBarDateTime(_bars[firstIndex], firstIndex).ToOADate();
                    lastX = GetBarDateTime(_bars[lastIndex], lastIndex).ToOADate();
                }

                if (!double.IsFinite(firstX) || !double.IsFinite(lastX))
                    return;

                double slotWidth = visibleCount > 1
                    ? (lastX - firstX) / (visibleCount - 1)
                    : 1.0;

                if (!(slotWidth > 0) || !double.IsFinite(slotWidth))
                    slotWidth = _continuousTimeAxisApplied ? 1.0 : 1.0 / 24.0;

                // The requested 25% is the blank portion of the actual X-axis.
                // Therefore the 200-candle data span occupies exactly 75% of it.
                double candleHalfWidth = slotWidth * 0.5;
                double dataSpan = Math.Max(slotWidth, (lastX - firstX) + slotWidth);
                double axisSpan = dataSpan / (1.0 - InitialRightBlankFraction);
                double left = firstX - candleHalfWidth;
                double right = left + axisSpan;

                Chart.Plot.Axes.SetLimitsX(left, right);
                _initialDisplayApplied = true;
                Chart.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initial chart display range failed: {ex}");
            }
        }
    }
}
