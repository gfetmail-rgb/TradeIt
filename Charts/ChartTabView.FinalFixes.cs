using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using TradeIt.Models;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _finalFixesRegistered = RegisterFinalFixes();

        private static bool RegisterFinalFixes()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(FinalFixes_Loaded));

            return true;
        }

        private static void FinalFixes_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart)
                return;

            chart.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(chart.ApplyFinalFixes));
        }

        private void ApplyFinalFixes()
        {
            try
            {
                // Timestamps which are outside the normal Gregorian market-data
                // range are not usable chart dates. Treat them as missing data.
                foreach (MarketBar bar in _bars)
                {
                    if (bar.Timestamp.HasValue &&
                        (bar.Timestamp.Value.Year < 1900 ||
                         bar.Timestamp.Value.Year > 2100))
                    {
                        bar.Timestamp = null;
                    }
                }

                bool hasRealTimestamp = _bars.Any(HasUsableTimestamp);

                ConfigureFinalDateAxis(Chart, hasRealTimestamp);
                ConfigureFinalDateAxis(VolumeChart, hasRealTimestamp);

                if (_bars.Count > 0 && Chart.ActualWidth > 0 && Chart.ActualHeight > 0)
                {
                    int visibleCount = Math.Min(365, _bars.Count);
                    int firstIndex = _bars.Count - visibleCount;
                    int lastIndex = _bars.Count - 1;

                    double firstX = GetBarDateTime(_bars[firstIndex], firstIndex).ToOADate();
                    double lastX = GetBarDateTime(_bars[lastIndex], lastIndex).ToOADate();

                    double halfStep = visibleCount > 1
                        ? Math.Abs(lastX - firstX) / (visibleCount - 1) * 0.5
                        : 0.5;

                    if (halfStep <= 0 || double.IsNaN(halfStep) || double.IsInfinity(halfStep))
                        halfStep = 0.5;

                    double xMin = firstX - halfStep;
                    double xMax = lastX + halfStep;

                    double minPrice = double.MaxValue;
                    double maxPrice = double.MinValue;

                    for (int i = firstIndex; i <= lastIndex; i++)
                    {
                        minPrice = Math.Min(minPrice, _bars[i].Low);
                        maxPrice = Math.Max(maxPrice, _bars[i].High);
                    }

                    if (minPrice == double.MaxValue || maxPrice == double.MinValue)
                        return;

                    double priceRange = maxPrice - minPrice;
                    double padding = priceRange > 0
                        ? priceRange * 0.05
                        : Math.Max(Math.Abs(maxPrice) * 0.01, 1.0);

                    Chart.Plot.Axes.SetLimits(
                        xMin,
                        xMax,
                        minPrice - padding,
                        maxPrice + padding);

                    SaveInitialView();
                    SyncVolumeXAxis();
                }

                SetFinalLastBarInfo();

                Chart.Refresh();
                VolumeChart.Refresh();
            }
            catch
            {
                // Display corrections must never prevent the chart from opening.
            }
        }

        private static bool HasUsableTimestamp(MarketBar bar)
        {
            return bar.Timestamp.HasValue &&
                   bar.Timestamp.Value.Year >= 1900 &&
                   bar.Timestamp.Value.Year <= 2100 &&
                   bar.Timestamp.Value > DateTime.MinValue &&
                   bar.Timestamp.Value < DateTime.MaxValue;
        }

        private void ConfigureFinalDateAxis(
            ScottPlot.WPF.WpfPlot plot,
            bool hasRealTimestamp)
        {
            if (hasRealTimestamp)
            {
                var axis = plot.Plot.Axes.DateTimeTicksBottom();

                if (axis.TickGenerator is ScottPlot.TickGenerators.DateTimeAutomatic auto)
                {
                    auto.LabelFormatter = dt =>
                        dt.TimeOfDay == TimeSpan.Zero
                            ? dt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)
                            : dt.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);
                }

                return;
            }

            // No real date exists: the X axis is numeric and is labelled by
            // candle number. Synthetic dates are used internally only because
            // ScottPlot OHLC requires DateTime X values.
            plot.Plot.Axes.NumericTicksBottom();

            var manualTicks = new ScottPlot.TickGenerators.NumericManual();
            int count = _bars.Count;
            int step = Math.Max(1, count / 8);

            for (int i = 0; i < count; i += step)
            {
                double x = GetBarDateTime(_bars[i], i).ToOADate();
                manualTicks.AddMajor(x, $"کندل {i + 1}");
            }

            if (count > 0)
            {
                double x = GetBarDateTime(_bars[count - 1], count - 1).ToOADate();
                manualTicks.AddMajor(x, $"کندل {count}");
            }

            plot.Plot.Axes.Bottom.TickGenerator = manualTicks;
        }

        private void SetFinalLastBarInfo()
        {
            if (_bars.Count == 0)
            {
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | بدون داده";
                BottomInfoTextBlock.Text = string.Empty;
                return;
            }

            MarketBar bar = _bars[_bars.Count - 1];

            ChartInfoTextBlock.Text =
                $"{_symbol.Symbol} | O: {bar.Open:N2}  H: {bar.High:N2}  L: {bar.Low:N2}  C: {bar.Close:N2}  V: {bar.Volume:N0}";

            if (HasUsableTimestamp(bar))
            {
                DateTime dt = bar.Timestamp!.Value;
                BottomInfoTextBlock.Text =
                    dt.TimeOfDay == TimeSpan.Zero
                        ? dt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)
                        : dt.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);
            }
            else
            {
                BottomInfoTextBlock.Text = $"کندل {_bars.Count}";
            }
        }

        private void FinalFixes_ChartMouseLeave(object? sender, System.Windows.Input.MouseEventArgs e)
        {
            SetFinalLastBarInfo();
        }

        public ChartTabView CreateFullScreenClone()
        {
            ChartTabView clone = new ChartTabView(
                _symbol,
                new List<MarketBar>(_bars));

            clone._chartType = _chartType;
            clone._chartVisible = _chartVisible;
            clone._toolsVisible = _toolsVisible;
            clone._volumeVisible = _volumeVisible;
            clone._gridVisible = _gridVisible;
            clone._crosshairVisible = _crosshairVisible;
            clone._settings = ChartSettingsManager.Current;

            switch (_chartType)
            {
                case ChartDisplayType.Line:
                    clone.ChartTypeComboBox.SelectedIndex = 1;
                    break;
                case ChartDisplayType.Bar:
                    clone.ChartTypeComboBox.SelectedIndex = 2;
                    break;
                default:
                    clone.ChartTypeComboBox.SelectedIndex = 0;
                    break;
            }

            if (_volumeVisible)
                clone.SetVolumeVisible(true, false);

            clone.SetGridVisibility(clone.Chart, clone._gridVisible);
            clone.SetGridVisibility(clone.VolumeChart, clone._gridVisible);

            var limits = Chart.Plot.Axes.GetLimits();
            clone.Chart.Plot.Axes.SetLimits(
                limits.Left,
                limits.Right,
                limits.Bottom,
                limits.Top);

            clone._initialXMin = _initialXMin;
            clone._initialXMax = _initialXMax;
            clone._initialYMin = _initialYMin;
            clone._initialYMax = _initialYMax;
            clone._hasInitialView = _hasInitialView;

            clone.SetFinalLastBarInfo();
            clone.SyncVolumeXAxis();
            clone.Chart.Refresh();
            clone.VolumeChart.Refresh();

            return clone;
        }
    }
}
