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
        private double _initialXMin, _initialXMax, _initialYMin, _initialYMax;
        private bool _chartVisible = true, _toolsVisible = true, _volumeVisible;
        private const double VolumeScale = 1000.0;
        private bool _gridVisible = true;
        private ChartSettings _settings;
        private ScottPlot.Plottables.Crosshair? _crosshair;
        private bool _crosshairMouseInside, _crosshairVisible = true;

        private enum AxisDragMode { None, TimeAxis, PriceAxis }
        private AxisDragMode _axisDragMode;
        private double _axisDragStartX, _axisDragStartY;
        private const double LeftAxisWidth = 75, RightAxisWidth = 30, BottomAxisHeight = 55;

        private bool ShowNonTradingDays => _settings.ShowNonTradingDays;

        public ChartTabView(SymbolInfo symbol, List<MarketBar> bars)
        {
            InitializeComponent();
            _symbol = symbol;
            _bars = bars ?? new List<MarketBar>();
            _settings = ChartSettingsManager.Current;
            _gridVisible = _settings.GridVisible;

            ChartTypeComboBox.SelectedIndex = 0;
            ConfigureInteraction();

            Chart.PreviewMouseWheel += Chart_PreviewMouseWheel;
            Chart.PreviewMouseLeftButtonDown += Chart_PreviewMouseLeftButtonDown;
            Chart.PreviewMouseMove += Chart_PreviewMouseMove;
            Chart.PreviewMouseLeftButtonUp += Chart_PreviewMouseLeftButtonUp;
            Chart.MouseLeave += Chart_MouseLeave;

            VolumeChart.PreviewMouseWheel += VolumeChart_PreviewMouseWheel;
            VolumeChart.PreviewMouseMove += VolumeChart_PreviewMouseMove;
            VolumeChart.MouseLeave += VolumeChart_MouseLeave;

            VolumeContainer.Visibility = Visibility.Collapsed;
            VolumeChartRow.Height = new GridLength(0);

            InitializeCrosshair();
            DrawChart();

            SetGridVisibility(Chart, _gridVisible);
            SetGridVisibility(VolumeChart, _gridVisible);

            CrosshairButton.Content = "Crosshair روشن";
            VolumeButton.Content = "نمایش حجم";
            GridButton.Content = "GRID";
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
            foreach (var p in Chart.Plot.GetPlottables().ToList())
            {
                if (!ReferenceEquals(p, _crosshair))
                    Chart.Plot.Remove(p);
            }

            if (_crosshair != null)
            {
                _crosshair.IsVisible = false;
                _crosshair.VerticalLine.IsVisible = false;
                _crosshair.HorizontalLine.IsVisible = false;
                _crosshair.VerticalLine.Label.IsVisible = false;
                _crosshair.HorizontalLine.Label.IsVisible = false;
            }
        }

        private void ClearVolumeChart()
        {
            foreach (var p in VolumeChart.Plot.GetPlottables().ToList())
                VolumeChart.Plot.Remove(p);
        }

        private void InitializeCrosshair()
        {
            if (_crosshair != null)
                return;

            _crosshair = Chart.Plot.Add.Crosshair(0, 0);

            _crosshair.IsVisible = false;
            _crosshair.VerticalLine.IsVisible = false;
            _crosshair.HorizontalLine.IsVisible = false;

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

            _crosshair.VerticalLine.LabelOppositeAxis = false;
            _crosshair.HorizontalLine.LabelOppositeAxis = false;
            _crosshair.VerticalLine.LabelRotation = 0;
            _crosshair.HorizontalLine.LabelRotation = 0;
            _crosshair.VerticalLine.LabelAlignment = ScottPlot.Alignment.LowerCenter;
            _crosshair.HorizontalLine.LabelAlignment = ScottPlot.Alignment.MiddleRight;

            _crosshair.VerticalLine.Label.BackgroundColor = ScottPlot.Color.FromHtml("#202020");
            _crosshair.VerticalLine.Label.ForeColor = ScottPlot.Color.FromHtml("#FFFFFF");
            _crosshair.HorizontalLine.Label.BackgroundColor = ScottPlot.Color.FromHtml("#202020");
            _crosshair.HorizontalLine.Label.ForeColor = ScottPlot.Color.FromHtml("#FFFFFF");

            _crosshair.VerticalLine.ExcludeFromLegend = true;
            _crosshair.HorizontalLine.ExcludeFromLegend = true;

            Chart.Plot.Axes.Bottom.IsVisible = true;
            Chart.Plot.Axes.Bottom.MinimumSize = 55;
            Chart.Plot.Axes.Left.MinimumSize = 75;
        }

        private bool TryGetChartCoordinates(ScottPlot.WPF.WpfPlot chart, WpfPoint pos, out ScottPlot.Coordinates c)
        {
            c = default;
            double w = chart.ActualWidth;
            double h = chart.ActualHeight;
            if (w <= 0 || h <= 0 || pos.X < 0 || pos.Y < 0 || pos.X > w || pos.Y > h)
                return false;

            double s = chart.DisplayScale <= 0 ? 1 : chart.DisplayScale;
            try
            {
                c = chart.Plot.GetCoordinates(new ScottPlot.Pixel(pos.X * s, pos.Y * s));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ShowCrosshair(double x, double y)
        {
            if (_crosshair == null || !_chartVisible || !_crosshairVisible)
                return;

            _crosshair.Position = new ScottPlot.Coordinates(x, y);
            _crosshair.IsVisible = true;
            _crosshair.VerticalLine.IsVisible = true;
            _crosshair.HorizontalLine.IsVisible = true;

            string xText = FormatCrosshairX(x);
            string yText = y.ToString("N2");

            _crosshair.VerticalLine.Text = xText;
            _crosshair.HorizontalLine.Text = yText;
            _crosshair.VerticalLine.Label.Text = xText;
            _crosshair.HorizontalLine.Label.Text = yText;

            _crosshair.VerticalLine.Label.IsVisible = true;
            _crosshair.HorizontalLine.Label.IsVisible = true;

            UpdateMouseInformation(new ScottPlot.Coordinates(x, y));
        }

        private void UpdateCrosshair(WpfPoint pos)
        {
            if (_crosshair == null || !_chartVisible || !_crosshairVisible)
                return;

            if (!TryGetChartCoordinates(Chart, pos, out var m))
                return;

            double x = GetNearestCandleX(m.X);
            double y = m.Y;

            _crosshairMouseInside = true;
            ShowCrosshair(x, y);
            Chart.Refresh();
        }

        private void UpdateCrosshairFromVolume(WpfPoint pos)
        {
            if (_crosshair == null || !_chartVisible || !_crosshairVisible)
                return;

            if (!TryGetChartCoordinates(VolumeChart, pos, out var v))
                return;

            var l = Chart.Plot.Axes.GetLimits();
            if (v.X < l.Left || v.X > l.Right)
                return;

            double x = GetNearestCandleX(v.X);
            double y = (l.Bottom + l.Top) / 2.0;

            _crosshairMouseInside = true;
            ShowCrosshair(x, y);
            Chart.Refresh();
        }

        private void UpdateMouseInformation(ScottPlot.Coordinates c)
        {
            try
            {
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | زمان: {FormatCrosshairX(c.X)} | قیمت: {c.Y:N2}";
            }
            catch
            {
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | قیمت: {c.Y:N2}";
            }
        }

        private void HideCrosshair()
        {
            _crosshairMouseInside = false;
            if (_crosshair != null)
            {
                _crosshair.IsVisible = false;
                _crosshair.VerticalLine.IsVisible = false;
                _crosshair.HorizontalLine.IsVisible = false;
                _crosshair.VerticalLine.Label.IsVisible = false;
                _crosshair.HorizontalLine.Label.IsVisible = false;
                Chart.Refresh();
            }
        }

        private void Chart_MouseLeave(object s, WpfMouseEventArgs e)
        {
            HideCrosshair();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | {_bars.Count:N0} داده";
        }

        private void VolumeChart_MouseLeave(object s, WpfMouseEventArgs e)
        {
            HideCrosshair();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | {_bars.Count:N0} داده";
        }

        private void Chart_PreviewMouseWheel(object s, WpfMouseWheelEventArgs e)
        {
            ZoomXAxis(e.Delta > 0 ? .80 : 1.25);
            e.Handled = true;
        }

        private void VolumeChart_PreviewMouseWheel(object s, WpfMouseWheelEventArgs e)
        {
            ZoomXAxis(e.Delta > 0 ? .80 : 1.25);
            e.Handled = true;
        }

        private void ZoomXAxis(double f)
        {
            if (!_hasInitialView)
                return;

            var l = Chart.Plot.Axes.GetLimits();
            double r = l.Right - l.Left;
            if (r <= 0)
                return;

            double initial = _initialXMax - _initialXMin;
            if (initial <= 0)
                initial = r;

            double nr = Math.Max(initial / 10000, Math.Min(initial * 2, r * f));
            Chart.Plot.Axes.SetLimits(l.Right - nr, l.Right, l.Bottom, l.Top);
            SyncVolumeXAxis();
            Chart.Refresh();
            if (_volumeVisible)
                VolumeChart.Refresh();
        }

        private void Chart_PreviewMouseLeftButtonDown(object s, WpfMouseButtonEventArgs e)
        {
            var p = e.GetPosition(Chart);
            var mode = GetAxisDragMode(p.X, p.Y);
            if (e.ClickCount == 2 && mode == AxisDragMode.PriceAxis)
            {
                AutoFitVisiblePriceRange();
                e.Handled = true;
                return;
            }
            if (mode == AxisDragMode.None)
                return;

            _axisDragMode = mode;
            _axisDragStartX = p.X;
            _axisDragStartY = p.Y;
            Chart.CaptureMouse();
            e.Handled = true;
        }

        private void Chart_PreviewMouseMove(object s, WpfMouseEventArgs e)
        {
            var p = e.GetPosition(Chart);
            UpdateCrosshair(p);

            if (_axisDragMode == AxisDragMode.None)
                return;

            if (e.LeftButton != WpfMouseButtonState.Pressed)
            {
                EndAxisDrag();
                return;
            }

            double dx = p.X - _axisDragStartX;
            double dy = p.Y - _axisDragStartY;

            if (_axisDragMode == AxisDragMode.TimeAxis && Math.Abs(dx) >= 1)
            {
                ApplyHorizontalAxisZoom(dx);
                _axisDragStartX = p.X;
            }
            else if (_axisDragMode == AxisDragMode.PriceAxis && Math.Abs(dy) >= 1)
            {
                ApplyVerticalAxisZoom(dy);
                _axisDragStartY = p.Y;
            }

            e.Handled = true;
        }

        private void VolumeChart_PreviewMouseMove(object s, WpfMouseEventArgs e) =>
            UpdateCrosshairFromVolume(e.GetPosition(VolumeChart));

        private void Chart_PreviewMouseLeftButtonUp(object s, WpfMouseButtonEventArgs e)
        {
            if (_axisDragMode != AxisDragMode.None)
            {
                EndAxisDrag();
                e.Handled = true;
            }
        }

        private void EndAxisDrag()
        {
            _axisDragMode = AxisDragMode.None;
            if (Chart.IsMouseCaptured)
                Chart.ReleaseMouseCapture();
        }

        private AxisDragMode GetAxisDragMode(double x, double y)
        {
            double w = Chart.ActualWidth, h = Chart.ActualHeight;
            if (w <= 0 || h <= 0)
                return AxisDragMode.None;
            if (y >= h - BottomAxisHeight)
                return AxisDragMode.TimeAxis;
            if (x <= LeftAxisWidth || x >= w - RightAxisWidth)
                return AxisDragMode.PriceAxis;
            return AxisDragMode.None;
        }

        private void ApplyHorizontalAxisZoom(double dx)
        {
            var l = Chart.Plot.Axes.GetLimits();
            double r = l.Right - l.Left;
            if (r <= 0)
                return;
            double initial = _initialXMax - _initialXMin;
            if (initial <= 0)
                initial = r;
            double nr = Math.Max(initial / 10000, Math.Min(initial * 2, r * Math.Exp(-dx / 180)));
            double c = (l.Left + l.Right) / 2;
            Chart.Plot.Axes.SetLimits(c - nr / 2, c + nr / 2, l.Bottom, l.Top);
            SyncVolumeXAxis();
            Chart.Refresh();
            if (_volumeVisible)
                VolumeChart.Refresh();
        }

        private void ApplyVerticalAxisZoom(double dy)
        {
            var l = Chart.Plot.Axes.GetLimits();
            double r = l.Top - l.Bottom;
            if (r <= 0)
                return;
            double initial = _initialYMax - _initialYMin;
            if (initial <= 0)
                initial = r;
            double nr = Math.Max(initial / 10000, Math.Min(initial * 2, r * Math.Exp(dy / 180)));
            double c = (l.Bottom + l.Top) / 2;
            Chart.Plot.Axes.SetLimits(l.Left, l.Right, c - nr / 2, c + nr / 2);
            Chart.Refresh();
        }

        private void AutoFitVisiblePriceRange()
        {
            if (!_hasInitialView || _bars.Count == 0)
                return;

            var l = Chart.Plot.Axes.GetLimits();
            double min = double.MaxValue, max = double.MinValue;
            for (int i = 0; i < _bars.Count; i++)
            {
                double x = GetBarX(_bars[i], i);
                if (x < l.Left || x > l.Right)
                    continue;
                if (double.IsFinite(_bars[i].Low)) min = Math.Min(min, _bars[i].Low);
                if (double.IsFinite(_bars[i].High)) max = Math.Max(max, _bars[i].High);
            }
            if (min == double.MaxValue || max == double.MinValue)
                return;

            double r = max - min;
            double pad = r <= 0 ? Math.Max(Math.Abs(max) * .01, 1) : r * .05;
            Chart.Plot.Axes.SetLimits(l.Left, l.Right, min - pad, max + pad);
            Chart.Refresh();
        }

        private void DrawChart()
        {
            if (_bars.Count == 0)
            {
                ClearMainChart();
                ClearVolumeChart();
                _hasInitialView = false;
                Chart.Refresh();
                VolumeChart.Refresh();
                return;
            }

            bool preserve = _hasInitialView && Chart.ActualWidth > 0 && Chart.ActualHeight > 0;
            ScottPlot.AxisLimits old = default;
            if (preserve)
                old = Chart.Plot.Axes.GetLimits();

            ClearMainChart();

            switch (_chartType)
            {
                case ChartDisplayType.Candlestick: DrawCandlestick(); break;
                case ChartDisplayType.Line: DrawLine(); break;
                case ChartDisplayType.Bar: DrawBar(); break;
            }

            ApplySettings();
            DrawVolume();

            if (!preserve)
            {
                Chart.Plot.Axes.AutoScale();
                SaveInitialView();
            }
            else
            {
                Chart.Plot.Axes.SetLimits(old.Left, old.Right, old.Bottom, old.Top);
            }

            ConfigureBottomAxis();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | {_bars.Count:N0} داده";

            if (_volumeVisible)
                SyncVolumeXAxis();

            if (_crosshair != null)
            {
                _crosshair.IsVisible = _crosshairVisible && _chartVisible && _crosshairMouseInside;
                _crosshair.VerticalLine.IsVisible = _crosshair.IsVisible;
                _crosshair.HorizontalLine.IsVisible = _crosshair.IsVisible;
                _crosshair.VerticalLine.Label.IsVisible = _crosshair.IsVisible;
                _crosshair.HorizontalLine.Label.IsVisible = _crosshair.IsVisible;
            }

            Chart.Refresh();
            if (_volumeVisible)
                VolumeChart.Refresh();

            if (!_volumeVisible)
            {
                VolumeContainer.Visibility = Visibility.Collapsed;
                VolumeChartRow.Height = new GridLength(0);
                MainChartRow.Height = new GridLength(1, GridUnitType.Star);
            }
        }

        private void DrawCandlestick()
        {
            var candles = new List<ScottPlot.OHLC>();
            var invalid = new List<DateTime>();

            for (int i = 0; i < _bars.Count; i++)
            {
                var b = _bars[i];
                DateTime t = GetBarDateTime(b, i);
                bool badTime = t == DateTime.MinValue || t == DateTime.MaxValue || !double.IsFinite(t.ToOADate());

                if (badTime || !IsValidOhlc(b))
                {
                    if (!badTime) invalid.Add(t);
                    continue;
                }

                candles.Add(new ScottPlot.OHLC(b.Open, b.High, b.Low, b.Close, t, TimeSpan.FromDays(1)));
            }

            if (candles.Count > 0)
            {
                var p = Chart.Plot.Add.Candlestick(candles);
                p.RisingColor = ScottPlot.Color.FromHtml(_settings.RisingColor);
                p.FallingColor = ScottPlot.Color.FromHtml(_settings.FallingColor);
                p.Sequential = !ShowNonTradingDays;
            }

            double min = double.PositiveInfinity, max = double.NegativeInfinity;
            foreach (var c in candles)
            {
                min = Math.Min(min, c.Low);
                max = Math.Max(max, c.High);
            }

            if (!double.IsFinite(min) || !double.IsFinite(max))
            {
                foreach (var b in _bars)
                {
                    foreach (double v in new[] { b.Open, b.High, b.Low, b.Close })
                    {
                        if (double.IsFinite(v))
                        {
                            min = Math.Min(min, v);
                            max = Math.Max(max, v);
                        }
                    }
                }
            }

            if (double.IsFinite(min) && double.IsFinite(max))
            {
                if (Math.Abs(max - min) < double.Epsilon)
                {
                    double pad = Math.Max(Math.Abs(min) * .01, 1);
                    min -= pad;
                    max += pad;
                }

                foreach (var t in invalid)
                {
                    var line = Chart.Plot.Add.Line(
                        new ScottPlot.Coordinates(t.ToOADate(), min),
                        new ScottPlot.Coordinates(t.ToOADate(), max));
                    line.Color = ScottPlot.Color.FromHtml("#FF0000");
                    line.LineWidth = 2;
                }
            }

            if (InvalidDataWarningTextBlock != null)
                InvalidDataWarningTextBlock.Visibility = invalid.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            ConfigureBottomAxis();
        }

        private void DrawLine()
        {
            double[] xs = new double[_bars.Count];
            double[] ys = new double[_bars.Count];
            for (int i = 0; i < _bars.Count; i++)
            {
                xs[i] = GetBarX(_bars[i], i);
                ys[i] = _bars[i].Close;
            }

            var p = Chart.Plot.Add.Scatter(xs, ys);
            p.MarkerSize = 0;
            p.LineWidth = (float)_settings.LineWidth;
            p.Color = ScottPlot.Color.FromHtml(_settings.LineColor);
            ConfigureBottomAxis();
        }

        private void DrawBar()
        {
            var list = new List<ScottPlot.OHLC>();
            for (int i = 0; i < _bars.Count; i++)
            {
                var b = _bars[i];
                if (IsValidOhlc(b))
                    list.Add(new ScottPlot.OHLC(b.Open, b.High, b.Low, b.Close, GetBarDateTime(b, i), TimeSpan.FromDays(1)));
            }

            if (list.Count == 0)
                return;

            var p = Chart.Plot.Add.OHLC(list);
            p.RisingStyle.Color = ScottPlot.Color.FromHtml(_settings.RisingColor);
            p.FallingStyle.Color = ScottPlot.Color.FromHtml(_settings.FallingColor);
            p.Sequential = !ShowNonTradingDays;
            ConfigureBottomAxis();
        }

        private void DrawVolume()
        {
            ClearVolumeChart();
            if (_bars.Count == 0)
                return;

            var list = new List<ScottPlot.Bar>();
            for (int i = 0; i < _bars.Count; i++)
            {
                var b = _bars[i];
                double v = b.Volume / VolumeScale;
                if (!double.IsFinite(v) || v < 0) v = 0;

                list.Add(new ScottPlot.Bar
                {
                    Position = GetBarX(b, i),
                    Value = v,
                    FillColor = b.Close >= b.Open ? ScottPlot.Color.FromHtml(_settings.RisingColor) : ScottPlot.Color.FromHtml(_settings.FallingColor),
                    LineColor = ScottPlot.Color.FromHtml(_settings.AxisColor),
                    LineWidth = 0
                });
            }

            if (list.Count == 0)
                return;

            VolumeChart.Plot.Add.Bars(list);
            ConfigureVolumeAxes();

            double max = list.Max(x => x.Value);
            if (max <= 0) max = 1;

            var l = Chart.Plot.Axes.GetLimits();
            VolumeChart.Plot.Axes.SetLimits(l.Left, l.Right, 0, max * 1.1);
            VolumeChart.Plot.Axes.Color(ScottPlot.Color.FromHtml(_settings.AxisColor));
            VolumeChart.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHtml(_settings.GridColor);
            VolumeChart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml(_settings.FigureBackground);
            VolumeChart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml(_settings.DataBackground);
            SetGridVisibility(VolumeChart, _gridVisible);
        }

        private void ConfigureVolumeAxes()
        {
            VolumeChart.Plot.Axes.Bottom.IsVisible = false;
            VolumeChart.Plot.Axes.Right.IsVisible = false;
            VolumeChart.Plot.Axes.Left.IsVisible = false;
            VolumeChart.Plot.Axes.Left.Label.Text = "";
            VolumeChart.Plot.Axes.Left.Label.IsVisible = false;
        }

        private DateTime GetBarDateTime(MarketBar b, int i)
        {
            if (b.Timestamp.HasValue && b.Timestamp.Value > DateTime.MinValue && b.Timestamp.Value < DateTime.MaxValue)
                return b.Timestamp.Value;
            return new DateTime(2000, 1, 1).AddDays(i);
        }

        private double GetBarX(MarketBar b, int i) => ShowNonTradingDays ? GetBarDateTime(b, i).ToOADate() : i;

        private bool HasRealDates => _bars.Any(x => x.Timestamp.HasValue && x.Timestamp.Value > DateTime.MinValue && x.Timestamp.Value < DateTime.MaxValue);

        private bool IsValidOhlc(MarketBar b) =>
            double.IsFinite(b.Open) && double.IsFinite(b.High) && double.IsFinite(b.Low) && double.IsFinite(b.Close) &&
            b.High >= b.Low && b.Low <= b.Open && b.Low <= b.Close && b.High >= b.Open && b.High >= b.Close;

        private void ConfigureBottomAxis()
        {
            Chart.Plot.Axes.Bottom.IsVisible = true;
            Chart.Plot.Axes.Bottom.MinimumSize = 55;

            if (ShowNonTradingDays && HasRealDates)
            {
                Chart.Plot.Axes.DateTimeTicksBottom();
                return;
            }

            var ticks = new ScottPlot.TickGenerators.NumericAutomatic
            {
                IntegerTicksOnly = true,
                LabelFormatter = v =>
                {
                    int i = (int)Math.Round(v);
                    if (i < 0 || i >= _bars.Count)
                        return "";

                    if (HasRealDates)
                    {
                        DateTime t = GetBarDateTime(_bars[i], i);
                        return t.TimeOfDay == TimeSpan.Zero
                            ? t.ToString("yyyy/MM/dd")
                            : t.ToString("yyyy/MM/dd HH:mm");
                    }

                    return (i + 1).ToString();
                }
            };

            Chart.Plot.Axes.Bottom.TickGenerator = ticks;
        }

        private string FormatCrosshairX(double x)
        {
            int index = GetNearestCandleIndex(x);
            if (_bars.Count == 0)
                return "";

            if (HasRealDates)
            {
                try
                {
                    DateTime t = GetBarDateTime(_bars[index - 1], index - 1);
                    return t.TimeOfDay == TimeSpan.Zero ? t.ToString("yyyy/MM/dd") : t.ToString("yyyy/MM/dd HH:mm");
                }
                catch { }
            }

            return $"کندل {index}";
        }

        private double GetNearestCandleX(double x)
        {
            if (_bars.Count == 0)
                return x;

            double best = GetBarX(_bars[0], 0);
            double d = Math.Abs(best - x);

            for (int i = 1; i < _bars.Count; i++)
            {
                double bx = GetBarX(_bars[i], i);
                double bd = Math.Abs(bx - x);
                if (bd < d)
                {
                    d = bd;
                    best = bx;
                }
            }

            return best;
        }

        private int GetNearestCandleIndex(double x)
        {
            if (_bars.Count == 0)
                return 1;

            int best = 0;
            double d = Math.Abs(GetBarX(_bars[0], 0) - x);

            for (int i = 1; i < _bars.Count; i++)
            {
                double nd = Math.Abs(GetBarX(_bars[i], i) - x);
                if (nd < d)
                {
                    d = nd;
                    best = i;
                }
            }

            return best + 1;
        }

        private void ApplySettings()
        {
            Chart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml(_settings.FigureBackground);
            Chart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml(_settings.DataBackground);
            Chart.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHtml(_settings.GridColor);
            Chart.Plot.Axes.Color(ScottPlot.Color.FromHtml(_settings.AxisColor));

            VolumeChart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml(_settings.FigureBackground);
            VolumeChart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml(_settings.DataBackground);
            VolumeChart.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHtml(_settings.GridColor);
            VolumeChart.Plot.Axes.Color(ScottPlot.Color.FromHtml(_settings.AxisColor));

            SetGridVisibility(Chart, _gridVisible);
            SetGridVisibility(VolumeChart, _gridVisible);
        }

        private void SaveInitialView()
        {
            var l = Chart.Plot.Axes.GetLimits();
            _initialXMin = l.Left;
            _initialXMax = l.Right;
            _initialYMin = l.Bottom;
            _initialYMax = l.Top;
            _hasInitialView = true;
        }

        private void SyncVolumeXAxis()
        {
            if (!_volumeVisible)
                return;

            var l = Chart.Plot.Axes.GetLimits();
            var vl = VolumeChart.Plot.Axes.GetLimits();
            double top = vl.Top;
            if (top <= 0)
                top = Math.Max(1, (_bars.Count > 0 ? _bars.Max(x => x.Volume / VolumeScale) : 1) * 1.1);

            VolumeChart.Plot.Axes.SetLimits(l.Left, l.Right, 0, top);
            ConfigureVolumeAxes();
        }

        private void VolumeButton_Click(object s, RoutedEventArgs e) => SetVolumeVisible(!_volumeVisible, true);

        private void SetVolumeVisible(bool visible, bool refresh)
        {
            _volumeVisible = visible;
            if (visible)
            {
                MainChartRow.Height = new GridLength(3, GridUnitType.Star);
                VolumeChartRow.Height = new GridLength(1, GridUnitType.Star);
                VolumeContainer.Visibility = Visibility.Visible;
                DrawVolume();
                SyncVolumeXAxis();
                VolumeButton.Content = "پنهان کردن حجم";
            }
            else
            {
                MainChartRow.Height = new GridLength(1, GridUnitType.Star);
                VolumeChartRow.Height = new GridLength(0);
                VolumeContainer.Visibility = Visibility.Collapsed;
                VolumeButton.Content = "نمایش حجم";
            }

            if (refresh)
            {
                Chart.Refresh();
                if (visible) VolumeChart.Refresh();
            }
        }

        private void GridButton_Click(object s, RoutedEventArgs e)
        {
            _gridVisible = !_gridVisible;
            SetGridVisibility(Chart, _gridVisible);
            SetGridVisibility(VolumeChart, _gridVisible);
            GridButton.Content = _gridVisible ? "GRID" : "GRID خاموش";
            Chart.Refresh();
            if (_volumeVisible) VolumeChart.Refresh();
        }

        private void SetGridVisibility(ScottPlot.WPF.WpfPlot p, bool v) => p.Plot.Grid.IsVisible = v;

        private void CrosshairButton_Click(object s, RoutedEventArgs e)
        {
            _crosshairVisible = !_crosshairVisible;
            if (_crosshair != null)
            {
                _crosshair.IsVisible = _crosshairVisible && _chartVisible && _crosshairMouseInside;
                _crosshair.VerticalLine.IsVisible = _crosshair.IsVisible;
                _crosshair.HorizontalLine.IsVisible = _crosshair.IsVisible;
                _crosshair.VerticalLine.Label.IsVisible = _crosshair.IsVisible;
                _crosshair.HorizontalLine.Label.IsVisible = _crosshair.IsVisible;
            }
            CrosshairButton.Content = _crosshairVisible ? "Crosshair روشن" : "Crosshair خاموش";
            Chart.Refresh();
        }

        private void ScreenshotButton_Click(object s, RoutedEventArgs e)
        {
            try
            {
                int w = (int)Math.Max(1, ActualWidth), h = (int)Math.Max(1, ActualHeight);
                var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(w, h, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                bitmap.Render(this);

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "ذخیره تصویر نمودار",
                    Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg",
                    FileName = $"{_symbol.Symbol}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };
                if (dialog.ShowDialog() != true) return;

                System.Windows.Media.Imaging.BitmapEncoder encoder =
                    Path.GetExtension(dialog.FileName).Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                        ? new System.Windows.Media.Imaging.JpegBitmapEncoder()
                        : new System.Windows.Media.Imaging.PngBitmapEncoder();

                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                using FileStream stream = new FileStream(dialog.FileName, FileMode.Create);
                encoder.Save(stream);
                BottomInfoTextBlock.Text = $"تصویر ذخیره شد: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"خطا در گرفتن تصویر نمودار:\n{ex.Message}", "Screenshot", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
            }
        }

        private void PrintButton_Click(object s, RoutedEventArgs e)
        {
            try
            {
                var d = new WpfPrintDialog();
                if (d.ShowDialog() != true) return;
                d.PrintVisual(this, $"TradeIt - {_symbol.Symbol}");
                BottomInfoTextBlock.Text = "نمودار برای چاپ ارسال شد.";
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"خطا در چاپ نمودار:\n{ex.Message}", "Print", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
            }
        }

        private void ChartTypeComboBox_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            if (ChartTypeComboBox.SelectedItem is not ComboBoxItem item) return;
            _chartType = item.Tag?.ToString() switch
            {
                "Line" => ChartDisplayType.Line,
                "Bar" => ChartDisplayType.Bar,
                _ => ChartDisplayType.Candlestick
            };
            if (IsLoaded) DrawChart();
        }

        private void SettingsButton_Click(object s, RoutedEventArgs e) => OpenSettings();

        private void OpenSettings()
        {
            var w = new ChartSettingsWindow(_settings) { Owner = Window.GetWindow(this) };
            if (w.ShowDialog() == true)
            {
                _settings = ChartSettingsManager.Clone(w.Settings);
                ChartSettingsManager.SetDefaults(_settings);
                _gridVisible = _settings.GridVisible;
                DrawChart();
            }
        }

        private void HideChartButton_Click(object s, RoutedEventArgs e)
        {
            _chartVisible = !_chartVisible;
            foreach (var p in Chart.Plot.GetPlottables())
            {
                if (!ReferenceEquals(p, _crosshair))
                    p.IsVisible = _chartVisible;
            }

            if (_crosshair != null)
            {
                _crosshair.IsVisible = _chartVisible && _crosshairVisible && _crosshairMouseInside;
                _crosshair.VerticalLine.IsVisible = _crosshair.IsVisible;
                _crosshair.HorizontalLine.IsVisible = _crosshair.IsVisible;
                _crosshair.VerticalLine.Label.IsVisible = _crosshair.IsVisible;
                _crosshair.HorizontalLine.Label.IsVisible = _crosshair.IsVisible;
            }

            Chart.Refresh();
            HideChartButton.Content = _chartVisible ? "پنهان کردن نمودار" : "نمایش نمودار";
        }

        private void HideToolsButton_Click(object s, RoutedEventArgs e)
        {
            _toolsVisible = !_toolsVisible;
            HideToolsButton.Content = _toolsVisible ? "پنهان کردن ابزارهای تکنیکال" : "نمایش ابزارهای تکنیکال";
        }

        private void ZoomInButton_Click(object s, RoutedEventArgs e) => ZoomXAxis(.8);
        private void ZoomOutButton_Click(object s, RoutedEventArgs e) => ZoomXAxis(1.25);

        private void ResetZoomButton_Click(object s, RoutedEventArgs e)
        {
            if (!_hasInitialView) return;
            Chart.Plot.Axes.SetLimits(_initialXMin, _initialXMax, _initialYMin, _initialYMax);
            SyncVolumeXAxis();
            Chart.Refresh();
            if (_volumeVisible) VolumeChart.Refresh();
        }

        private void FullViewButton_Click(object s, RoutedEventArgs e)
        {
            if (_bars.Count == 0) return;
            Chart.Plot.Axes.AutoScale();
            SaveInitialView();
            SyncVolumeXAxis();
            Chart.Refresh();
            if (_volumeVisible) VolumeChart.Refresh();
        }
    }

    public enum ChartDisplayType { Candlestick, Line, Bar }
}
