using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TradeIt.Models;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfMouseEventHandler = System.Windows.Input.MouseEventHandler;
using WpfPoint = System.Windows.Point;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _crosshairRestoreRegistered = RegisterCrosshairRestore();
        private bool _crosshairRestoreMouseHooked;

        private static bool RegisterCrosshairRestore()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(CrosshairRestore_Loaded));
            return true;
        }

        private static void CrosshairRestore_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart) return;
            chart.Dispatcher.BeginInvoke(new Action(chart.RestoreCrosshairAndDateAxis), DispatcherPriority.SystemIdle);
        }

        private void RestoreCrosshairAndDateAxis()
        {
            if (_bars.Count == 0) return;
            try
            {
                if (_continuousTimeAxisApplied) ConfigureContinuousDateAxis();
                else ConfigureFinalDateAxis();

                if (!_crosshairRestoreMouseHooked)
                {
                    _crosshairRestoreMouseHooked = true;
                    Chart.PreviewMouseMove += CrosshairRestore_MouseMove;
                }

                if (_crosshair == null || !_chartVisible || !_crosshairVisible)
                {
                    if (_crosshair != null) _crosshair.IsVisible = false;
                    Chart.Refresh();
                    return;
                }

                int index = _bars.Count - 1;
                double x = _continuousTimeAxisApplied ? ContinuousX(index) : GetBarDateTime(_bars[index], index).ToOADate();
                double y = _bars[index].Close;
                _crosshair.Position = new ScottPlot.Coordinates(x, y);
                _crosshair.HorizontalLine.Text = y.ToString("N2");
                _crosshair.VerticalLine.Text = GetCrosshairXLabel(index);
                _crosshairMouseInside = true;
                _crosshair.IsVisible = true;
                Chart.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Crosshair/date-axis restore failed: {ex}");
            }
        }

        private void CrosshairRestore_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_continuousTimeAxisApplied || _crosshair == null || !_crosshairVisible || !_chartVisible) return;
            try { ApplyContinuousCrosshair(e); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Continuous crosshair update failed: {ex}"); }
        }

        private bool _mouseOhlcvInfoFixAttached;
        private static readonly bool _mouseOhlcvInfoFixRegistered = RegisterMouseOhlcvInfoFix();

        private static bool RegisterMouseOhlcvInfoFix()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(MouseOhlcvInfoFix_Loaded));
            return true;
        }

        private static void MouseOhlcvInfoFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart) chart.AttachMouseOhlcvInfoFix();
        }

        private void AttachMouseOhlcvInfoFix()
        {
            if (_mouseOhlcvInfoFixAttached) return;
            _mouseOhlcvInfoFixAttached = true;
            Chart.AddHandler(UIElement.MouseMoveEvent, new WpfMouseEventHandler(MouseOhlcvInfoFix_MouseMove), true);
        }

        private void MouseOhlcvInfoFix_MouseMove(object sender, WpfMouseEventArgs e)
        {
            if (!_chartVisible || _bars.Count == 0 || _continuousTimeAxisApplied) return;
            WpfPoint mousePosition = e.GetPosition(Chart);
            if (!TryGetChartCoordinates(Chart, mousePosition, out ScottPlot.Coordinates coordinates)) return;
            int barIndex = FindNearestBarIndex(coordinates.X);
            if (barIndex < 0 || barIndex >= _bars.Count) return;
            UpdateOHLCVInfo(barIndex);
        }

        private static readonly bool _ohlcvFinalFixRegistered = RegisterOHLCVFinalFix();
        private bool _ohlcvFinalFixInitialized;

        private static bool RegisterOHLCVFinalFix()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(OHLCVFinalFix_Loaded));
            return true;
        }

        private static void OHLCVFinalFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart || chart._ohlcvFinalFixInitialized) return;
            chart._ohlcvFinalFixInitialized = true;
            chart.Dispatcher.BeginInvoke(new Action(chart.AttachOHLCVFinalFix), DispatcherPriority.ApplicationIdle);
        }

        private void AttachOHLCVFinalFix()
        {
            Chart.MouseMove -= OHLCVFinalFix_MouseMove;
            Chart.MouseMove += OHLCVFinalFix_MouseMove;
            Chart.MouseLeave -= OHLCVFinalFix_MouseLeave;
            Chart.MouseLeave += OHLCVFinalFix_MouseLeave;
            if (_bars.Count > 0) UpdateOHLCVInfo(_bars.Count - 1);
        }

        private void OHLCVFinalFix_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                if (_bars.Count == 0 || !TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates)) return;
                int index = FindNearestBarIndex(coordinates.X);
                if (index >= 0) UpdateOHLCVInfo(index);
            }
            catch { }
        }

        private void OHLCVFinalFix_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_bars.Count > 0) UpdateOHLCVInfo(_bars.Count - 1);
        }

        private static readonly bool _gridCrosshairToolbarRegistered = RegisterGridCrosshairToolbar();
        private bool _gridCrosshairToolbarHandlersAttached;

        private static bool RegisterGridCrosshairToolbar()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(GridCrosshairToolbar_Loaded));
            return true;
        }

        private static void GridCrosshairToolbar_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart) return;
            chart.CrosshairButton.Content = "✚";
            chart.GridButton.Content = "▦";
            chart.CrosshairButton.IsChecked = chart._crosshairVisible;
            chart.GridButton.IsChecked = chart._gridVisible;
            if (!chart._gridCrosshairToolbarHandlersAttached)
            {
                chart._gridCrosshairToolbarHandlersAttached = true;
                chart.CrosshairButton.Click += chart.GridCrosshairToolbar_Click;
                chart.GridButton.Click += chart.GridCrosshairToolbar_Click;
                chart.Chart.PreviewMouseMove += chart.GridCrosshairToolbar_MouseMove;
            }
        }

        private void GridCrosshairToolbar_Click(object sender, RoutedEventArgs e)
        {
            if (ReferenceEquals(sender, CrosshairButton)) { CrosshairButton.Content = "✚"; CrosshairButton.IsChecked = _crosshairVisible; }
            else if (ReferenceEquals(sender, GridButton)) { GridButton.Content = "▦"; GridButton.IsChecked = _gridVisible; }
        }

        private void GridCrosshairToolbar_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            CrosshairButton.IsChecked = _crosshairVisible;
            GridButton.IsChecked = _gridVisible;
        }

        private static readonly bool _unifiedBordersRegistered = RegisterUnifiedBorders();

        private static bool RegisterUnifiedBorders()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(UnifiedBorders_Loaded));
            return true;
        }

        private static void UnifiedBorders_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart) return;
            chart.ApplyUnifiedPlotBorders();
            ChartSettingsManager.SettingsChanged -= chart.UnifiedBorders_SettingsChanged;
            ChartSettingsManager.SettingsChanged += chart.UnifiedBorders_SettingsChanged;
        }

        private void UnifiedBorders_SettingsChanged(object? sender, EventArgs e)
        {
            if (Dispatcher.CheckAccess()) ApplyUnifiedPlotBorders();
            else Dispatcher.InvokeAsync(ApplyUnifiedPlotBorders);
        }

        private void ApplyUnifiedPlotBorders()
        {
            try
            {
                _settings = ChartSettingsManager.Current;
                ScottPlot.Color borderColor = ScottPlot.Color.FromHtml(_settings.AxisColor);
                Chart.Plot.DataBorder = new ScottPlot.LineStyle { Color = borderColor, Width = 1, Pattern = ScottPlot.LinePattern.Solid };
                Chart.Plot.FigureBorder = new ScottPlot.LineStyle { Color = borderColor, Width = 1, Pattern = ScottPlot.LinePattern.Solid };
                Chart.Plot.Axes.Frame(false);
                Chart.Refresh();
            }
            catch { }
        }

        // Price-axis, arrow input, horizontal zoom, and visible date tick handling.
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
                first = Math.Max(0, (int)Math.Ceiling(limits.Left - ContinuousChartBaseDate));
                last = Math.Min(_bars.Count - 1, (int)Math.Floor(limits.Right - ContinuousChartBaseDate));
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
                positions[n] = _continuousTimeAxisApplied ? ContinuousChartBaseDate + index : GetBarDateTime(_bars[index], index).ToOADate();
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
                double x = _continuousTimeAxisApplied ? ContinuousChartBaseDate + i : GetBarDateTime(_bars[i], i).ToOADate();
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