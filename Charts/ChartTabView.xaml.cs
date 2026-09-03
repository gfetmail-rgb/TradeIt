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
        private bool _crosshairVisible = false;
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
            CrosshairButton.Content = "Crosshair خاموش";
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
                DateTime dateTime = barIndex >= 0 ? GetBarDateTime(_bars[barIndex], barIndex) : DateTime.FromOADate(coordinates.X);
                string dateText = dateTime.TimeOfDay == TimeSpan.Zero ? dateTime.ToString("yyyy/MM/dd") : dateTime.ToString("yyyy/MM/dd HH:mm");
                string priceText = coordinates.Y.ToString("N2");
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | زمان: {dateText} | قیمت: {priceText}";
            }
            catch { ChartInfoTextBlock.Text = $"{_symbol.Symbol} | قیمت: {coordinates.Y:N2}"; }
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
            bool preserveCurrentView = _hasInitialView && Chart.ActualWidth > 0 && Chart.ActualHeight > 0; ScottPlot.AxisLimits currentLimits = default;
            if (preserveCurrentView) currentLimits = Chart.Plot.Axes.GetLimits();
            ClearMainChart();
            switch (_chartType) { case ChartDisplayType.Candlestick: DrawCandlestick(); break; case ChartDisplayType.Line: DrawLine(); break; case ChartDisplayType.Bar: DrawBar(); break; }
            ApplySettings();
            if (!preserveCurrentView) { Chart.Plot.Axes.AutoScale(); SaveInitialView(); } else Chart.Plot.Axes.SetLimits(currentLimits.Left, currentLimits.Right, currentLimits.Bottom, currentLimits.Top);
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | {_bars.Count:N0} داده";
            if (_crosshair != null) _crosshair.IsVisible = _crosshairVisible && _chartVisible && _crosshairMouseInside;
            Chart.Refresh();
        }
        private void DrawCandlestick()
        {
            var candles = new List<ScottPlot.OHLC>(); for (int i = 0; i < _bars.Count; i++) { MarketBar bar = _bars[i]; DateTime time = GetBarDateTime(bar, i); candles.Add(new ScottPlot.OHLC(bar.Open, bar.High, bar.Low, bar.Close, time, TimeSpan.FromDays(1))); }
            var candlePlot = Chart.Plot.Add.Candlestick(candles); candlePlot.RisingColor = ScottPlot.Color.FromHtml(_settings.RisingColor); candlePlot.FallingColor = ScottPlot.Color.FromHtml(_settings.FallingColor); Chart.Plot.Axes.DateTimeTicksBottom();
        }
        private void DrawLine()
        {
            var points = new List<(double X, double Y)>(); for (int i = 0; i < _bars.Count; i++) { MarketBar bar = _bars[i]; if (!bar.Timestamp.HasValue || bar.Timestamp.Value <= DateTime.MinValue || bar.Timestamp.Value >= DateTime.MaxValue) continue; double x = bar.Timestamp.Value.ToOADate(), y = bar.Close; if (!double.IsFinite(x) || !double.IsFinite(y)) continue; points.Add((x, y)); }
            if (points.Count == 0) return; points.Sort((a, b) => a.X.CompareTo(b.X)); var cleanPoints = new List<(double X, double Y)>(points.Count);
            foreach (var point in points) { if (cleanPoints.Count == 0) cleanPoints.Add(point); else if (point.X == cleanPoints[^1].X) cleanPoints[^1] = point; else cleanPoints.Add(point); }
            if (cleanPoints.Count == 0) return; double[] xs = cleanPoints.Select(p => p.X).ToArray(), ys = cleanPoints.Select(p => p.Y).ToArray();
            var line = Chart.Plot.Add.ScatterLine(xs, ys); line.MarkerSize = 0; line.LineWidth = (float)Math.Max(0.01, _settings.LineWidth); line.LineColor = ScottPlot.Color.FromHtml(_settings.LineColor); line.ConnectStyle = ScottPlot.ConnectStyle.Straight; line.Smooth = false; line.PathStrategy = new ScottPlot.PathStrategies.Straight(); line.MinRenderIndex = 0; line.MaxRenderIndex = xs.Length - 1; Chart.Plot.Axes.DateTimeTicksBottom();
        }
        private void DrawBar()
        {
            var ohlcs = new List<ScottPlot.OHLC>(); for (int i = 0; i < _bars.Count; i++) { MarketBar bar = _bars[i]; DateTime time = GetBarDateTime(bar, i); ohlcs.Add(new ScottPlot.OHLC(bar.Open, bar.High, bar.Low, bar.Close, time, TimeSpan.FromDays(1))); }
            if (ohlcs.Count == 0) return; var ohlcPlot = Chart.Plot.Add.OHLC(ohlcs); ohlcPlot.RisingStyle.Color = ScottPlot.Color.FromHtml(_settings.RisingColor); ohlcPlot.FallingStyle.Color = ScottPlot.Color.FromHtml(_settings.FallingColor); Chart.Plot.Axes.DateTimeTicksBottom();
        }
        private DateTime GetBarDateTime(MarketBar bar, int index) => bar.Timestamp.HasValue && bar.Timestamp.Value > DateTime.MinValue && bar.Timestamp.Value < DateTime.MaxValue ? bar.Timestamp.Value : new DateTime(2000, 1, 1).AddDays(index);
        private void ApplySettings()
        {
            Chart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml(_settings.FigureBackground); Chart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml(_settings.DataBackground); Chart.Plot.Grid.LineColor = ScottPlot.Color.FromHtml(_settings.GridColor); Chart.Plot.Grid.LinePattern = ParseLinePattern(_settings.GridPattern); Chart.Plot.Grid.MajorLineWidth = (float)Math.Max(0.01, _settings.GridLineWidth); Chart.Plot.Grid.MinorLineWidth = (float)Math.Max(0.01, _settings.GridLineWidth); Chart.Plot.Axes.Color(ScottPlot.Color.FromHtml(_settings.AxisColor)); SetGridVisibility(Chart, _gridVisible);
            foreach (var plottable in Chart.Plot.GetPlottables()) { if (plottable is ScottPlot.Plottables.CandlestickPlot candles) { candles.RisingLineStyle.Width = (float)Math.Max(0.01, _settings.CandleLineWidth); candles.FallingLineStyle.Width = (float)Math.Max(0.01, _settings.CandleLineWidth); } else if (plottable is ScottPlot.Plottables.OhlcPlot ohlc) { ohlc.RisingStyle.Width = (float)Math.Max(0.01, _settings.BarLineWidth); ohlc.FallingStyle.Width = (float)Math.Max(0.01, _settings.BarLineWidth); } else if (plottable is ScottPlot.Plottables.Scatter scatter) scatter.LineWidth = (float)Math.Max(0.01, _settings.LineWidth); }
            if (_crosshair != null) { _crosshair.LineColor = ScottPlot.Color.FromHtml(_settings.CrosshairColor); _crosshair.LineWidth = (float)Math.Max(0.01, _settings.CrosshairLineWidth); _crosshair.LinePattern = ParseLinePattern(_settings.CrosshairPattern); _crosshair.HorizontalLine.LabelOppositeAxis = false; _crosshair.VerticalLine.LabelOppositeAxis = false; _crosshair.HorizontalLine.LabelAlignment = ScottPlot.Alignment.MiddleRight; _crosshair.VerticalLine.LabelAlignment = ScottPlot.Alignment.LowerCenter; }
        }
        private static ScottPlot.LinePattern ParseLinePattern(string? value) => value?.Trim().ToLowerInvariant() switch { "dotted" => ScottPlot.LinePattern.Dotted, "dashed" => ScottPlot.LinePattern.Dashed, "denselydashed" => ScottPlot.LinePattern.DenselyDashed, _ => ScottPlot.LinePattern.Solid };
        private void SaveInitialView() { var limits = Chart.Plot.Axes.GetLimits(); _initialXMin = limits.Left; _initialXMax = limits.Right; _initialYMin = limits.Bottom; _initialYMax = limits.Top; _hasInitialView = true; }
        private void GridButton_Click(object sender, RoutedEventArgs e) { _gridVisible = !_gridVisible; SetGridVisibility(Chart, _gridVisible); GridButton.Content = _gridVisible ? "GRID" : "GRID خاموش"; Chart.Refresh(); }
        private void SetGridVisibility(ScottPlot.WPF.WpfPlot plot, bool visible) => plot.Plot.Grid.IsVisible = visible;
        private void CrosshairButton_Click(object sender, RoutedEventArgs e) { _crosshairVisible = !_crosshairVisible; if (_crosshair != null) _crosshair.IsVisible = _crosshairVisible && _chartVisible && _crosshairMouseInside; CrosshairButton.Content = _crosshairVisible ? "Crosshair روشن" : "Crosshair خاموش"; Chart.Refresh(); }
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e) { try { int width = (int)Math.Max(1, ActualWidth), height = (int)Math.Max(1, ActualHeight); var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32); bitmap.Render(this); var dialog = new Microsoft.Win32.SaveFileDialog { Title = "ذخیره تصویر نمودار", Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg", FileName = $"{_symbol.Symbol}_{DateTime.Now:yyyyMMdd_HHmmss}.png" }; if (dialog.ShowDialog() != true) return; System.Windows.Media.Imaging.BitmapEncoder encoder = Path.GetExtension(dialog.FileName).Equals(".jpg", StringComparison.OrdinalIgnoreCase) ? new System.Windows.Media.Imaging.JpegBitmapEncoder() : new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap)); using FileStream stream = new FileStream(dialog.FileName, FileMode.Create); encoder.Save(stream); BottomInfoTextBlock.Text = $"تصویر ذخیره شد: {dialog.FileName}"; } catch (Exception ex) { WpfMessageBox.Show($"خطا در گرفتن تصویر نمودار:\n{ex.Message}", "Screenshot", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error); } }
        private void PrintButton_Click(object sender, RoutedEventArgs e) { try { var dialog = new WpfPrintDialog(); if (dialog.ShowDialog() != true) return; dialog.PrintVisual(this, $"TradeIt - {_symbol.Symbol}"); BottomInfoTextBlock.Text = "نمودار برای چاپ ارسال شد."; } catch (Exception ex) { WpfMessageBox.Show($"خطا در چاپ نمودار:\n{ex.Message}", "Print", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error); } }
        private void ChartTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (ChartTypeComboBox.SelectedItem is not ComboBoxItem item) return; string type = item.Tag?.ToString() ?? ""; _chartType = type switch { "Line" => ChartDisplayType.Line, "Bar" => ChartDisplayType.Bar, _ => ChartDisplayType.Candlestick }; if (IsLoaded) DrawChart(); }
        private void SettingsButton_Click(object sender, RoutedEventArgs e) => OpenSettings();
        private void OpenSettings() { var window = new ChartSettingsWindow(ChartSettingsManager.Current) { Owner = Window.GetWindow(this) }; if (window.ShowDialog() == true) { _settings = ChartSettingsManager.Current; DrawChart(); } }
        private void HideChartButton_Click(object sender, RoutedEventArgs e) { _chartVisible = !_chartVisible; foreach (var plottable in Chart.Plot.GetPlottables()) { if (ReferenceEquals(plottable, _crosshair)) continue; plottable.IsVisible = _chartVisible; } if (_crosshair != null) _crosshair.IsVisible = _chartVisible && _crosshairVisible && _crosshairMouseInside; HideChartButton.Content = _chartVisible ? "پنهان کردن نمودار" : "نمایش نمودار"; Chart.Refresh(); }
        private void HideToolsButton_Click(object sender, RoutedEventArgs e) { _toolsVisible = !_toolsVisible; HideToolsButton.Content = _toolsVisible ? "پنهان کردن ابزارهای تکنیکال" : "نمایش ابزارهای تکنیکال"; }
        private void ZoomInButton_Click(object sender, RoutedEventArgs e) => ZoomXAxis(0.80);
        private void ZoomOutButton_Click(object sender, RoutedEventArgs e) => ZoomXAxis(1.25);
    }
}