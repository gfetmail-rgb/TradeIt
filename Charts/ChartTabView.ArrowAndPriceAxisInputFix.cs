using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _arrowAndPriceAxisInputFixAttached;
        private static readonly bool _arrowAndPriceAxisInputFixRegistered = RegisterArrowAndPriceAxisInputFix();

        private static bool RegisterArrowAndPriceAxisInputFix()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(ArrowAndPriceAxisInputFix_Loaded));
            EventManager.RegisterClassHandler(typeof(ChartTabView), UIElement.PreviewMouseLeftButtonDownEvent, new System.Windows.Input.MouseButtonEventHandler(ArrowAndPriceAxisInputFix_ClassMouseDown));
            EventManager.RegisterClassHandler(typeof(ChartTabView), UIElement.PreviewMouseWheelEvent, new System.Windows.Input.MouseWheelEventHandler(ArrowAndPriceAxisInputFix_ClassMouseWheel));
            EventManager.RegisterClassHandler(typeof(ChartTabView), UIElement.PreviewMouseMoveEvent, new System.Windows.Input.MouseEventHandler(ArrowAndPriceAxisInputFix_ClassMouseMove));
            return true;
        }

        private static void ArrowAndPriceAxisInputFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
            {
                chart.AttachArrowAndPriceAxisInputFix();
                chart.Dispatcher.BeginInvoke(new Action(chart.UpdateVisibleDateAxisTicks), DispatcherPriority.ApplicationIdle);
            }
        }

        private void AttachArrowAndPriceAxisInputFix()
        {
            if (_arrowAndPriceAxisInputFixAttached) return;
            _arrowAndPriceAxisInputFixAttached = true;
            Chart.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, new System.Windows.Input.MouseButtonEventHandler(ArrowAndPriceAxisInputFix_MouseDown), true);
        }

        private static void ArrowAndPriceAxisInputFix_ClassMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not ChartTabView chart || e.ChangedButton != MouseButton.Left) return;
            if (chart._arrowDrawingActive && (int)chart._activeDrawingTool == 10)
            {
                chart.ArrowDrawing_MouseDown(chart.Chart, e);
                e.Handled = true;
                return;
            }
            if (e.ClickCount == 2)
            {
                System.Windows.Point p = e.GetPosition(chart.Chart);
                if (chart.IsPriceAxisPoint(p.X, p.Y))
                {
                    chart.AutoFitVisiblePriceRangeFixed();
                    e.Handled = true;
                }
            }
        }

        private static void ArrowAndPriceAxisInputFix_ClassMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (sender is not ChartTabView chart || !chart._hasInitialView) return;
            double range = chart.Chart.Plot.Axes.GetLimits().Right - chart.Chart.Plot.Axes.GetLimits().Left;
            if (range <= 0) return;
            double initialRange = chart._initialXMax - chart._initialXMin;
            if (initialRange <= 0) initialRange = range;
            double factor = e.Delta > 0 ? 0.80 : 1.25;
            double newRange = Math.Max(initialRange / 10000.0, Math.Min(initialRange * 2.0, range * factor));
            var limits = chart.Chart.Plot.Axes.GetLimits();
            chart.Chart.Plot.Axes.SetLimits(limits.Right - newRange, limits.Right, limits.Bottom, limits.Top);
            chart.UpdateVisibleDateAxisTicks();
            chart.Chart.Refresh();
            e.Handled = true;
        }

        private static void ArrowAndPriceAxisInputFix_ClassMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is not ChartTabView chart || chart._axisDragMode != AxisDragMode.TimeAxis) return;
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                chart.EndAxisDrag();
                return;
            }
            System.Windows.Point p = e.GetPosition(chart.Chart);
            double deltaX = p.X - chart._axisDragStartX;
            if (Math.Abs(deltaX) < 1.0) return;
            var limits = chart.Chart.Plot.Axes.GetLimits();
            double range = limits.Right - limits.Left;
            if (range <= 0) return;
            double newRange = range * Math.Exp(-deltaX / 180.0);
            double initialRange = chart._initialXMax - chart._initialXMin;
            if (initialRange <= 0) initialRange = range;
            newRange = Math.Max(initialRange / 10000.0, Math.Min(initialRange * 2.0, newRange));
            chart.Chart.Plot.Axes.SetLimits(limits.Right - newRange, limits.Right, limits.Bottom, limits.Top);
            chart._axisDragStartX = p.X;
            chart.UpdateVisibleDateAxisTicks();
            chart.Chart.Refresh();
            e.Handled = true;
        }

        private void ArrowAndPriceAxisInputFix_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            if (_arrowDrawingActive && (int)_activeDrawingTool == 10)
            {
                ArrowDrawing_MouseDown(sender, e);
                e.Handled = true;
                return;
            }
            if (e.ClickCount == 2)
            {
                System.Windows.Point p = e.GetPosition(Chart);
                if (IsPriceAxisPoint(p.X, p.Y))
                {
                    AutoFitVisiblePriceRangeFixed();
                    e.Handled = true;
                }
            }
        }

        private bool IsPriceAxisPoint(double x, double y)
        {
            double width = Chart.ActualWidth;
            double height = Chart.ActualHeight;
            if (width <= 0 || height <= 0) return false;
            const double leftAxisWidth = 75.0;
            const double rightAxisWidth = 30.0;
            const double bottomAxisHeight = 55.0;
            if (y >= height - bottomAxisHeight) return false;
            return x <= leftAxisWidth || x >= width - rightAxisWidth;
        }

        private void UpdateVisibleDateAxisTicks()
        {
            if (_bars.Count == 0 || !IsLoaded) return;
            var limits = Chart.Plot.Axes.GetLimits();
            int first = 0;
            int last = _bars.Count - 1;
            if (_continuousTimeAxisApplied)
            {
                first = Math.Max(0, (int)Math.Ceiling(limits.Left - 2000.0));
                last = Math.Min(_bars.Count - 1, (int)Math.Floor(limits.Right - 2000.0));
            }
            else
            {
                double left = limits.Left;
                double right = limits.Right;
                while (first < _bars.Count && GetBarDateTime(_bars[first], first).ToOADate() < left) first++;
                while (last >= 0 && GetBarDateTime(_bars[last], last).ToOADate() > right) last--;
            }
            if (first > last) return;
            int visibleCount = last - first + 1;
            int tickCount = Math.Min(9, visibleCount);
            var positions = new double[tickCount];
            var labels = new string[tickCount];
            for (int n = 0; n < tickCount; n++)
            {
                int index = tickCount == 1 ? first : first + (int)Math.Round(n * (visibleCount - 1.0) / (tickCount - 1.0));
                positions[n] = _continuousTimeAxisApplied ? 2000.0 + index : GetBarDateTime(_bars[index], index).ToOADate();
                string label = HasSourceDate(index) ? GetSourceDateLabel(index) : GetBarDateTime(_bars[index], index).ToString("yyyy/MM/dd");
                labels[n] = string.IsNullOrWhiteSpace(label) ? $"کندل {index + 1}" : label;
            }
            var axis = Chart.Plot.Axes.NumericTicksBottom();
            axis.TickGenerator = new ScottPlot.TickGenerators.NumericManual(positions, labels);
        }

        private void AutoFitVisiblePriceRangeFixed()
        {
            if (_bars.Count == 0) return;
            ScottPlot.AxisLimits limits = Chart.Plot.Axes.GetLimits();
            double minPrice = double.MaxValue;
            double maxPrice = double.MinValue;
            for (int i = 0; i < _bars.Count; i++)
            {
                double x = _continuousTimeAxisApplied ? 2000.0 + i : GetBarDateTime(_bars[i], i).ToOADate();
                if (x < limits.Left || x > limits.Right) continue;
                if (double.IsFinite(_bars[i].Low)) minPrice = Math.Min(minPrice, _bars[i].Low);
                if (double.IsFinite(_bars[i].High)) maxPrice = Math.Max(maxPrice, _bars[i].High);
            }
            if (minPrice == double.MaxValue || maxPrice == double.MinValue) return;
            double range = maxPrice - minPrice;
            double padding = range > 0 ? range * 0.05 : Math.Max(Math.Abs(maxPrice) * 0.01, 1.0);
            Chart.Plot.Axes.SetLimits(limits.Left, limits.Right, minPrice - padding, maxPrice + padding);
            Chart.Refresh();
        }
    }
}
