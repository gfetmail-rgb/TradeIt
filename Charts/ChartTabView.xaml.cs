using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TradeIt.Models;
using WpfPoint = System.Windows.Point;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfMouseWheelEventArgs = System.Windows.Input.MouseWheelEventArgs;
using WpfMouseButtonState = System.Windows.Input.MouseButtonState;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using WpfPrintDialog = System.Windows.Controls.PrintDialog;

namespace TradeIt.Charts
{
    public partial class ChartTabView : System.Windows.Controls.UserControl
    {
        private readonly SymbolInfo _symbol;
        private readonly List<MarketBar> _bars;
        private ChartDisplayType _chartType = ChartDisplayType.Candlestick;
        private bool _hasInitialView;
        private double _initialXMin;
        private double _initialXMax;
        private double _initialYMin;
        private double _initialYMax;
        private bool _chartVisible = true;
        private bool _toolsVisible = true;
        private bool _gridVisible = false;
        private ChartSettings _settings;
        private ScottPlot.Plottables.Crosshair? _crosshair;
        private bool _crosshairMouseInside;
        private bool _crosshairVisible = true;
        private enum AxisDragMode { None, TimeAxis, PriceAxis }
        private AxisDragMode _axisDragMode = AxisDragMode.None;
        private double _axisDragStartX;
        private double _axisDragStartY;
        private const double LeftAxisWidth = 75.0;
        private const double RightAxisWidth = 30.0;
        private const double BottomAxisHeight = 55.0;

        public ChartTabView(SymbolInfo symbol, List<MarketBar> bars)
        {
            InitializeComponent();
            _symbol = symbol;
            _bars = bars ?? new List<MarketBar>();
            _settings = ChartSettingsManager.Current;
            SubscribeToSettingsChanges();
            ChartTypeComboBox.SelectedIndex = 0;
            ConfigureInteraction();
            Chart.PreviewMouseWheel += Chart_PreviewMouseWheel;
            Chart.PreviewMouseLeftButtonDown += Chart_PreviewMouseLeftButtonDown;
            Chart.PreviewMouseMove += Chart_PreviewMouseMove;
            Chart.PreviewMouseLeftButtonUp += Chart_PreviewMouseLeftButtonUp;
            Chart.MouseLeave += Chart_MouseLeave;
            InitializeCrosshair();
            DrawChart();
            SetGridVisibility(Chart, _gridVisible);
            InitializeCrosshairAtInitialPosition();
            CrosshairButton.Content = "Crosshair روشن";
            GridButton.Content = "GRID خاموش";
            HideChartButton.Content = "پنهان کردن نمودار";
            HideToolsButton.Content = "پنهان کردن ابزارهای تکنیکال";
        }

        private void ConfigureInteraction()
        {
            var input = Chart.UserInputProcessor;
            input.IsEnabled = true;
            input.LeftClickDragPan(true, horizontal: true, vertical: true);
            input.RightClickDragZoom(true, horizontal: true, vertical: true);
            input.RemoveAll<ScottPlot.Interactivity.UserActionResponses.MouseWheelZoom>();
        }

        private void ClearMainChart()
        {
            var plottables = Chart.Plot.GetPlottables().ToList();
            foreach (var plottable in plottables)
            {
                if (ReferenceEquals(plottable, _crosshair)) continue;
                Chart.Plot.Remove(plottable);
            }
            if (_crosshair != null) _crosshair.IsVisible = false;
        }

        private void InitializeCrosshair()
        {
            if (_crosshair != null) return;
            _crosshair = Chart.Plot.Add.Crosshair(0, 0);
            _crosshair.IsVisible = false;
            _crosshair.LineColor = ScottPlot.Color.FromHtml("#707070");
            _crosshair.LineWidth = 1;
            _crosshair.LinePattern = ScottPlot.LinePattern.Dashed;
            _crosshair.MarkerSize = 7;
            _crosshair.MarkerColor = ScottPlot.Color.FromHtml("#202020");
            _crosshair.MarkerFillColor = ScottPlot.Color.FromHtml("#FFFFFF");
            _crosshair.MarkerLineColor = ScottPlot.Color.FromHtml("#202020");
            _crosshair.MarkerLineWidth = 1;
            _crosshair.TextColor = ScottPlot.Color.FromHtml("#FFFFFF");
            _crosshair.TextBackgroundColor = ScottPlot.Color.FromHtml("#202020");
            _crosshair.FontSize = 12;
            _crosshair.FontBold = true;
            _crosshair.HorizontalLine.LabelOppositeAxis = false;
            _crosshair.VerticalLine.LabelOppositeAxis = false;
            _crosshair.HorizontalLine.LabelAlignment = ScottPlot.Alignment.MiddleRight;
            _crosshair.VerticalLine.LabelAlignment = ScottPlot.Alignment.LowerCenter;
        }

        private bool TryGetChartCoordinates(ScottPlot.WPF.WpfPlot chart, WpfPoint mousePosition, out ScottPlot.Coordinates coordinates)
        {
            coordinates = default;
            double width = chart.ActualWidth;
            double height = chart.ActualHeight;
            if (width <= 0 || height <= 0 || mousePosition.X < 0 || mousePosition.Y < 0 || mousePosition.X > width || mousePosition.Y > height) return false;
            double scale = chart.DisplayScale;
            if (scale <= 0) scale = 1.0;
            try { coordinates = chart.Plot.GetCoordinates(new ScottPlot.Pixel(mousePosition.X * scale, mousePosition.Y * scale)); return true; }
            catch { return false; }
        }

        private void UpdateCrosshair(WpfPoint mousePosition)
        {
            if (_crosshair == null) return;
            if (!_chartVisible || !_crosshairVisible || !TryGetChartCoordinates(Chart, mousePosition, out ScottPlot.Coordinates coordinates))
            {
                _crosshair.IsVisible = false; Chart.Refresh(); return;
            }
            int barIndex = FindNearestBarIndex(coordinates.X);
            if (barIndex >= 0)
            {
                DateTime barTime = GetBarDateTime(_bars[barIndex], barIndex);
                _crosshair.Position = new ScottPlot.Coordinates(barTime.ToOADate(), coordinates.Y);
                UpdateCrosshairAxisLabel(barIndex);
            }
            else
            {
                _crosshair.Position = coordinates;
                try { _crosshair.VerticalLine.Text = DateTime.FromOADate(coordinates.X).ToString("yyyy/MM/dd"); } catch { _crosshair.VerticalLine.Text = string.Empty; }
            }
            _crosshair.HorizontalLine.Text = coordinates.Y.ToString("N2");
            _crosshair.IsVisible = true;
            _crosshairMouseInside = true;
            UpdateMouseInformation(coordinates, barIndex);
            Chart.Refresh();
        }

        private int FindNearestBarIndex(double x)
        {
            if (_bars.Count == 0) return -1;
            int best = -1; double bestDistance = double.MaxValue;
            for (int i = 0; i < _bars.Count; i++)
            {
                double bx = GetBarDateTime(_bars[i], i).ToOADate(); double distance = Math.Abs(bx - x);
                if (distance < bestDistance) { bestDistance = distance; best = i; }
            }
            return best;
        }

        private void UpdateMouseInformation(ScottPlot.Coordinates coordinates, int barIndex)
        {
            try
            {
                string dateText = barIndex >= 0 ? GetSourceDateLabel(barIndex) : string.Empty;
                if (string.IsNullOrWhiteSpace(dateText)) dateText = $"کندل {barIndex + 1}";
                string priceText = coordinates.Y.ToString("N2");
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | زمان: {dateText} | قیمت: {priceText}";
            }
            catch
            {
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | قیمت: {coordinates.Y:N2}";
            }
        }

        private void Chart_MouseLeave(object sender, WpfMouseEventArgs e)
        {
            _crosshairMouseInside = false;
            if (_crosshair != null) { _crosshair.IsVisible = false; Chart.Refresh(); }
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | {_bars.Count:N0} داده";
        }

        private void Chart_PreviewMouseWheel(object sender, WpfMouseWheelEventArgs e) { ZoomXAxis(e.Delta > 0 ? 0.80 : 1.25); e.Handled = true; }
        private void ZoomXAxis(double factor)
        {
            if (!_hasInitialView) return;
            var limits = Chart.Plot.Axes.GetLimits(); double range = limits.Right - limits.Left; if (range <= 0) return;
            double initialRange = _initialXMax - _initialXMin; if (initialRange <= 0) initialRange = range;
            double newRange = Math.Max(initialRange / 10000.0, Math.Min(initialRange * 2.0, range * factor));
            Chart.Plot.Axes.SetLimits(limits.Right - newRange, limits.Right, limits.Bottom, limits.Top); Chart.Refresh();
        }

        private void Chart_PreviewMouseLeftButtonDown(object sender, WpfMouseButtonEventArgs e)
        {
            WpfPoint p = e.GetPosition(Chart); AxisDragMode mode = GetAxisDragMode(p.X, p.Y);
            if (e.ClickCount == 2 && mode == AxisDragMode.PriceAxis) { AutoFitVisiblePriceRange(); e.Handled = true; return; }
            if (mode == AxisDragMode.None) return;
            _axisDragMode = mode; _axisDragStartX = p.X; _axisDragStartY = p.Y; Chart.CaptureMouse(); e.Handled = true;
        }
        private void Chart_PreviewMouseMove(object sender, WpfMouseEventArgs e)
        {
            WpfPoint p = e.GetPosition(Chart); UpdateCrosshair(p);
            if (_axisDragMode == AxisDragMode.None) return;
            if (e.LeftButton != WpfMouseButtonState.Pressed) { EndAxisDrag(); return; }
            double deltaX = p.X - _axisDragStartX; double deltaY = p.Y - _axisDragStartY;
            if (_axisDragMode == AxisDragMode.TimeAxis && Math.Abs(deltaX) >= 1) { ApplyHorizontalAxisZoom(deltaX); _axisDragStartX = p.X; }
            else if (_axisDragMode == AxisDragMode.PriceAxis && Math.Abs(deltaY) >= 1) { ApplyVerticalAxisZoom(deltaY); _axisDragStartY = p.Y; }
            e.Handled = true;
        }
        private void Chart_PreviewMouseLeftButtonUp(object sender, WpfMouseButtonEventArgs e) { if (_axisDragMode == AxisDragMode.None) return; EndAxisDrag(); e.Handled = true; }
        private void EndAxisDrag() { _axisDragMode = AxisDragMode.None; if (Chart.IsMouseCaptured) Chart.ReleaseMouseCapture(); }
        private AxisDragMode GetAxisDragMode(double x, double y)
        {
            double width = Chart.ActualWidth, height = Chart.ActualHeight; if (width <= 0 || height <= 0) return AxisDragMode.None;
            if (y >= height - BottomAxisHeight) return AxisDragMode.TimeAxis;
            if (x <= LeftAxisWidth || x >= width - RightAxisWidth) return AxisDragMode.PriceAxis;
            return AxisDragMode.None;
        }
        private void ApplyHorizontalAxisZoom(double deltaX)
        {
            var limits = Chart.Plot.Axes.GetLimits(); double range = limits.Right - limits.Left; if (range <= 0) return;
            double newRange = range * Math.Exp(-deltaX / 180.0); double initialRange = _initialXMax - _initialXMin; if (initialRange <= 0) initialRange = range;
            newRange = Math.Max(initialRange / 10000.0, Math.Min(initialRange * 2.0, newRange)); double center = (limits.Left + limits.Right) / 2.0;
            Chart.Plot.Axes.SetLimits(center - newRange / 2.0, center + newRange / 2.0, limits.Bottom, limits.Top); Chart.Refresh();
        }
        private void ApplyVerticalAxisZoom(double deltaY)
        {
            var limits = Chart.Plot.Axes.GetLimits(); double range = limits.Top - limits.Bottom; if (range <= 0) return;
            double newRange = range * Math.Exp(deltaY / 180.0); double initialRange = _initialYMax - _initialYMin; if (initialRange <= 0) initialRange = range;
            newRange = Math.Max(initialRange / 10000.0, Math.Min(initialRange * 2.0, newRange)); double center = (limits.Bottom + limits.Top) / 2.0;
            Chart.Plot.Axes.SetLimits(limits.Left, limits.Right, center - newRange / 2.0, center + newRange / 2.0); Chart.Refresh();
        }
        private void AutoFitVisiblePriceRange()
        {
            if (!_hasInitialView || _bars.Count == 0) return;
            var limits = Chart.Plot.Axes.GetLimits(); double minPrice = double.MaxValue, maxPrice = double.MinValue;
            for (int i = 0; i < _bars.Count; i++) { double x = GetBarDateTime(_bars[i], i).ToOADate(); if (x < limits.Left || x > limits.Right) continue; minPrice = Math.Min(minPrice, _bars[i].Low); maxPrice = Math.Max(maxPrice, _bars[i].High); }
            if (minPrice == double.MaxValue || maxPrice == double.MinValue) return;
            double range = maxPrice - minPrice; double padding = range > 0 ? range * 0.05 : Math.Max(Math.Abs(maxPrice) * 0.01, 1);
            Chart.Plot.Axes.SetLimits(limits.Left, limits.Right, minPrice - padding, maxPrice + padding); Chart.Refresh();
        }

        private void DrawChart()
        {
            if (_bars.Count == 0) { ClearMainChart(); _hasInitialView = false; ApplySettings(); Chart.Refresh(); return; }

            // The user's saved time-axis preference is authoritative for every redraw.
            // In continuous mode we use one synthetic X position per candle, so weekends
            // and other periods without data have zero visual width.
            if (!ChartSettingsManager.Current.ShowTimeGaps)
            {
                ApplyContinuousTimeAxis();
                return;
            }

            bool preserveCurrentView = _hasInitialView && Chart.ActualWidth > 0 && Chart.ActualHeight > 0;
            ScottPlot.AxisLimits currentLimits = default;
            if (preserveCurrentView) currentLimits = Chart.Plot.Axes.GetLimits();

            ClearMainChart();
            switch (_chartType) { case ChartDisplayType.Candlestick: DrawCandlestick(); break; case ChartDisplayType.Line: DrawLine(); break; case ChartDisplayType.Bar: DrawBar(); break; }
            ApplySettings();

            if (!preserveCurrentView)
            {
                Chart.Plot.Axes.AutoScale();
                ApplyInitial365ViewAfterAutoScale();
            }
            else
            {
                Chart.Plot.Axes.SetLimits(currentLimits.Left, currentLimits.Right, currentLimits.Bottom, currentLimits.Top);
            }

            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | {_bars.Count:N0} داده";
            if (_crosshair != null) _crosshair.IsVisible = _crosshairVisible && _chartVisible && (_crosshairMouseInside || !_hasInitialView);
            Chart.Refresh();
        }

        private void ApplyInitial365ViewAfterAutoScale()
        {
            const int visibleCount = 365;
            int firstIndex = Math.Max(0, _bars.Count - visibleCount);
            int lastIndex = _bars.Count - 1;

            double firstX = GetBarDateTime(_bars[firstIndex], firstIndex).ToOADate();
            double lastX = GetBarDateTime(_bars[lastIndex], lastIndex).ToOADate();
            if (!double.IsFinite(firstX) || !double.IsFinite(lastX) || lastX < firstX)
            {
                SaveInitialView();
                return;
            }

            var autoLimits = Chart.Plot.Axes.GetLimits();
            const double candleHalfWidthDays = 0.5;
            Chart.Plot.Axes.SetLimits(firstX - candleHalfWidthDays, lastX + candleHalfWidthDays, autoLimits.Bottom, autoLimits.Top);
            AutoFitVisiblePriceRange();
            SaveInitialView();
        }

        private void DrawCandlestick()
        {
            var candles = new List<ScottPlot.OHLC>(); for (int i = 0; i < _bars.Count; i++) { MarketBar bar = _bars[i]; DateTime time = GetBarDateTime(bar, i); candles.Add(new ScottPlot.OHLC(bar.Open, bar.High, bar.Low, bar.Close, time, TimeSpan.FromDays(1))); }
            var candlePlot = Chart.Plot.Add.Candlestick(candles);
            candlePlot.RisingColor = ScottPlot.Color.FromHtml(_settings.RisingColor); candlePlot.FallingColor = ScottPlot.Color.FromHtml(_settings.FallingColor);
        }

        private void DrawLine()
        {
            var xs = new double[_bars.Count]; var ys = new double[_bars.Count]; for (int i = 0; i < _bars.Count; i++) { xs[i] = GetBarDateTime(_bars[i], i).ToOADate(); ys[i] = _bars[i].Close; }
            if (_bars.Count == 0) return;
            var line = Chart.Plot.Add.ScatterLine(xs, ys); line.MarkerSize = 0; line.LineWidth = (float)Math.Max(0.01, _settings.LineWidth); line.LineColor = ScottPlot.Color.FromHtml(_settings.LineColor); line.ConnectStyle = ScottPlot.ConnectStyle.Straight; line.Smooth = false; line.PathStrategy = new ScottPlot.PathStrategies.Straight();
        }

        private void DrawBar()
        {
            var bars = new List<ScottPlot.OHLC>(); for (int i = 0; i < _bars.Count; i++) { MarketBar bar = _bars[i]; DateTime time = GetBarDateTime(bar, i); bars.Add(new ScottPlot.OHLC(bar.Open, bar.High, bar.Low, bar.Close, time, TimeSpan.FromDays(1))); }
            if (bars.Count == 0) return;
            var plot = Chart.Plot.Add.OHLC(bars); plot.RisingStyle.Color = ScottPlot.Color.FromHtml(_settings.RisingColor); plot.FallingStyle.Color = ScottPlot.Color.FromHtml(_settings.FallingColor);
        }

        private void SaveInitialView()
        {
            var limits = Chart.Plot.Axes.GetLimits(); _initialXMin = limits.Left; _initialXMax = limits.Right; _initialYMin = limits.Bottom; _initialYMax = limits.Top; _hasInitialView = true;
        }
    }
}
