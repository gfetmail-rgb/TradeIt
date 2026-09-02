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
        private bool _volumeVisible = false;
        private const double VolumeScale = 1000.0;
        private bool _gridVisible = true;
        private ChartSettings _settings;
        private ScottPlot.Plottables.Crosshair? _crosshair;
        private bool _crosshairMouseInside;
        private bool _crosshairVisible = true;

        private enum AxisDragMode
        {
            None,
            TimeAxis,
            PriceAxis
        }

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
            var plottables = Chart.Plot.GetPlottables().ToList();
            foreach (var plottable in plottables)
            {
                if (ReferenceEquals(plottable, _crosshair)) continue;
                Chart.Plot.Remove(plottable);
            }
            if (_crosshair != null) _crosshair.IsVisible = false;
        }

        private void ClearVolumeChart()
        {
            var plottables = VolumeChart.Plot.GetPlottables().ToList();
            foreach (var plottable in plottables) VolumeChart.Plot.Remove(plottable);
        }

        private void UpdateMouseInformation(ScottPlot.Coordinates coordinates)
        {
            try
            {
                DateTime dateTime = DateTime.FromOADate(coordinates.X);
                string dateText = dateTime.TimeOfDay == TimeSpan.Zero
                    ? dateTime.ToString("yyyy/MM/dd")
                    : dateTime.ToString("yyyy/MM/dd HH:mm");
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
            if (_crosshair != null)
            {
                _crosshair.IsVisible = false;
                Chart.Refresh();
            }
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | {_bars.Count:N0} داده";
        }

        private void VolumeChart_MouseLeave(object sender, WpfMouseEventArgs e)
        {
            _crosshairMouseInside = false;
            if (_crosshair != null)
            {
                _crosshair.IsVisible = false;
                Chart.Refresh();
            }
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | {_bars.Count:N0} داده";
        }

        private void Chart_PreviewMouseWheel(object sender, WpfMouseWheelEventArgs e)
        {
            ZoomXAxis(e.Delta > 0 ? 0.80 : 1.25);
            e.Handled = true;
        }

        private void VolumeChart_PreviewMouseWheel(object sender, WpfMouseWheelEventArgs e)
        {
            ZoomXAxis(e.Delta > 0 ? 0.80 : 1.25);
            e.Handled = true;
        }

        private void ZoomXAxis(double factor)
        {
            if (!_hasInitialView) return;
            var limits = Chart.Plot.Axes.GetLimits();
            double xMin = limits.Left;
            double xMax = limits.Right;
            double yMin = limits.Bottom;
            double yMax = limits.Top;
            double range = xMax - xMin;
            if (range <= 0) return;
            double initialRange = _initialXMax - _initialXMin;
            if (initialRange <= 0) initialRange = range;
            double minimumRange = initialRange / 10000.0;
            double maximumRange = initialRange * 2.0;
            double newRange = Math.Max(minimumRange, Math.Min(maximumRange, range * factor));
            double right = xMax;
            double left = right - newRange;
            Chart.Plot.Axes.SetLimits(left, right, yMin, yMax);
            SyncVolumeXAxis();
            Chart.Refresh();
            if (_volumeVisible) VolumeChart.Refresh();
        }

        private void Chart_PreviewMouseLeftButtonDown(object sender, WpfMouseButtonEventArgs e)
        {
            WpfPoint p = e.GetPosition(Chart);
            AxisDragMode mode = GetAxisDragMode(p.X, p.Y);
            if (e.ClickCount == 2 && mode == AxisDragMode.PriceAxis)
            {
                AutoFitVisiblePriceRange();
                e.Handled = true;
                return;
            }
            if (mode == AxisDragMode.None) return;
            _axisDragMode = mode;
            _axisDragStartX = p.X;
            _axisDragStartY = p.Y;
            Chart.CaptureMouse();
            e.Handled = true;
        }

        private void Chart_PreviewMouseMove(object sender, WpfMouseEventArgs e)
        {
            WpfPoint p = e.GetPosition(Chart);
            UpdateCrosshair(p);
            if (_axisDragMode == AxisDragMode.None) return;
            if (e.LeftButton != WpfMouseButtonState.Pressed)
            {
                EndAxisDrag();
                return;
            }
            double deltaX = p.X - _axisDragStartX;
            double deltaY = p.Y - _axisDragStartY;
            if (_axisDragMode == AxisDragMode.TimeAxis)
            {
                if (Math.Abs(deltaX) >= 1)
                {
                    ApplyHorizontalAxisZoom(deltaX);
                    _axisDragStartX = p.X;
                }
            }
            else if (_axisDragMode == AxisDragMode.PriceAxis)
            {
                if (Math.Abs(deltaY) >= 1)
                {
                    ApplyVerticalAxisZoom(deltaY);
                    _axisDragStartY = p.Y;
                }
            }
            e.Handled = true;
        }

        private void VolumeChart_PreviewMouseMove(object sender, WpfMouseEventArgs e)
        {
            WpfPoint p = e.GetPosition(VolumeChart);
            UpdateCrosshairFromVolume(p);
        }

        private void Chart_PreviewMouseLeftButtonUp(object sender, WpfMouseButtonEventArgs e)
        {
            if (_axisDragMode == AxisDragMode.None) return;
            EndAxisDrag();
            e.Handled = true;
        }

        private void EndAxisDrag()
        {
            _axisDragMode = AxisDragMode.None;
            if (Chart.IsMouseCaptured) Chart.ReleaseMouseCapture();
            SyncVolumeXAxis();
            if (_volumeVisible) VolumeChart.Refresh();
        }

        private AxisDragMode GetAxisDragMode(double x, double y)
        {
            double width = Chart.ActualWidth;
            double height = Chart.ActualHeight;
            if (width <= 0 || height <= 0) return AxisDragMode.None;
            if (y >= height - BottomAxisHeight) return AxisDragMode.TimeAxis;
            if (x <= LeftAxisWidth || x >= width - RightAxisWidth) return AxisDragMode.PriceAxis;
            return AxisDragMode.None;
        }

        private void ApplyHorizontalAxisZoom(double deltaX)
        {
            var limits = Chart.Plot.Axes.GetLimits();
            double range = limits.Right - limits.Left;
            if (range <= 0) return;
            double factor = Math.Exp(-deltaX / 180.0);
            double newRange = range * factor;
            double initialRange = _initialXMax - _initialXMin;
            if (initialRange <= 0) initialRange = range;
            double minimumRange = initialRange / 10000.0;
            double maximumRange = initialRange * 2.0;
            newRange = Math.Max(minimumRange, Math.Min(maximumRange, newRange));
            double center = (limits.Left + limits.Right) / 2.0;
            double newXMin = center - newRange / 2.0;
            double newXMax = center + newRange / 2.0;
            Chart.Plot.Axes.SetLimits(newXMin, newXMax, limits.Bottom, limits.Top);
            SyncVolumeXAxis();
            Chart.Refresh();
            if (_volumeVisible) VolumeChart.Refresh();
        }

        private void ApplyVerticalAxisZoom(double deltaY)
        {
            var limits = Chart.Plot.Axes.GetLimits();
            double range = limits.Top - limits.Bottom;
            if (range <= 0) return;
            double factor = Math.Exp(deltaY / 180.0);
            double newRange = range * factor;
            double initialRange = _initialYMax - _initialYMin;
            if (initialRange <= 0) initialRange = range;
            double minimumRange = initialRange / 10000.0;
            double maximumRange = initialRange * 2.0;
            newRange = Math.Max(minimumRange, Math.Min(maximumRange, newRange));
            double center = (limits.Bottom + limits.Top) / 2.0;
            double newYMin = center - newRange / 2.0;
            double newYMax = center + newRange / 2.0;
            Chart.Plot.Axes.SetLimits(limits.Left, limits.Right, newYMin, newYMax);
            Chart.Refresh();
        }

        private void AutoFitVisiblePriceRange()
        {
            if (!_hasInitialView || _bars.Count == 0) return;
            var limits = Chart.Plot.Axes.GetLimits();
            double visibleXMin = limits.Left;
            double visibleXMax = limits.Right;
            double minPrice = double.MaxValue;
            double maxPrice = double.MinValue;
            for (int i = 0; i < _bars.Count; i++)
            {
                DateTime time = GetBarDateTime(_bars[i], i);
                double x = time.ToOADate();
                if (x < visibleXMin || x > visibleXMax) continue;
                minPrice = Math.Min(minPrice, _bars[i].Low);
                maxPrice = Math.Max(maxPrice, _bars[i].High);
            }
            if (minPrice == double.MaxValue || maxPrice == double.MinValue) return;
            double range = maxPrice - minPrice;
            if (range <= 0)
            {
                double padding = Math.Abs(maxPrice) * 0.01;
                if (padding <= 0) padding = 1;
                minPrice -= padding;
                maxPrice += padding;
            }
            else
            {
                double padding = range * 0.05;
                minPrice -= padding;
                maxPrice += padding;
            }
            Chart.Plot.Axes.SetLimits(visibleXMin, visibleXMax, minPrice, maxPrice);
            Chart.Refresh();
            SyncVolumeXAxis();
            if (_volumeVisible) VolumeChart.Refresh();
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
            bool preserveCurrentView = _hasInitialView && Chart.ActualWidth > 0 && Chart.ActualHeight > 0;
            ScottPlot.AxisLimits currentLimits = default;
            if (preserveCurrentView) currentLimits = Chart.Plot.Axes.GetLimits();
            ClearMainChart();
            switch (_chartType)
            {
                case ChartDisplayType.Candlestick: DrawCandlestick(); break;
                case ChartDisplayType.Line: DrawLine(); break;
                case ChartDisplayType.Bar: DrawBar(); break;
            }
            ApplySettings();
            DrawVolume();
            if (!preserveCurrentView)
            {
                Chart.Plot.Axes.AutoScale();
                SaveInitialView();
            }
            else
            {
                Chart.Plot.Axes.SetLimits(currentLimits.Left, currentLimits.Right, currentLimits.Bottom, currentLimits.Top);
            }
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | {_bars.Count:N0} داده";
            if (_volumeVisible) SyncVolumeXAxis();
            if (_crosshair != null)
            {
                _crosshair.IsVisible = _crosshairVisible && _chartVisible && _crosshairMouseInside;
            }
            Chart.Refresh();
            if (_volumeVisible) VolumeChart.Refresh();
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
            for (int i = 0; i < _bars.Count; i++)
            {
                MarketBar bar = _bars[i];
                DateTime time = GetBarDateTime(bar, i);
                candles.Add(new ScottPlot.OHLC(bar.Open, bar.High, bar.Low, bar.Close, time, TimeSpan.FromDays(1)));
            }
            var candlePlot = Chart.Plot.Add.Candlestick(candles);
            candlePlot.RisingColor = ScottPlot.Color.FromHtml(_settings.RisingColor);
            candlePlot.FallingColor = ScottPlot.Color.FromHtml(_settings.FallingColor);
            Chart.Plot.Axes.DateTimeTicksBottom();
        }

        private void DrawLine()
        {
            var xs = new DateTime[_bars.Count];
            var ys = new double[_bars.Count];
            for (int i = 0; i < _bars.Count; i++)
            {
                xs[i] = GetBarDateTime(_bars[i], i);
                ys[i] = _bars[i].Close;
            }
            var line = Chart.Plot.Add.Scatter(xs, ys);
            line.MarkerSize = 0;
            line.LineWidth = (float)_settings.LineWidth;
            line.Color = ScottPlot.Color.FromHtml(_settings.LineColor);
            Chart.Plot.Axes.DateTimeTicksBottom();
        }

        private void DrawBar()
        {
            var ohlcs = new List<ScottPlot.OHLC>();
            for (int i = 0; i < _bars.Count; i++)
            {
                MarketBar bar = _bars[i];
                DateTime time = GetBarDateTime(bar, i);
                ohlcs.Add(new ScottPlot.OHLC(bar.Open, bar.High, bar.Low, bar.Close, time, TimeSpan.FromDays(1)));
            }
            if (ohlcs.Count == 0) return;
            var ohlcPlot = Chart.Plot.Add.OHLC(ohlcs);
            ohlcPlot.RisingStyle.Color = ScottPlot.Color.FromHtml(_settings.RisingColor);
            ohlcPlot.FallingStyle.Color = ScottPlot.Color.FromHtml(_settings.FallingColor);
            Chart.Plot.Axes.DateTimeTicksBottom();
        }

        private void DrawVolume()
        {
            ClearVolumeChart();
            if (_bars.Count == 0) return;
            var bars = new List<ScottPlot.Bar>();
            for (int i = 0; i < _bars.Count; i++)
            {
                MarketBar marketBar = _bars[i];
                DateTime time = GetBarDateTime(marketBar, i);
                double volumeK = marketBar.Volume / VolumeScale;
                if (double.IsNaN(volumeK) || double.IsInfinity(volumeK) || volumeK < 0) volumeK = 0;
                bars.Add(new ScottPlot.Bar
                {
                    Position = time.ToOADate(),
                    Value = volumeK,
                    FillColor = ScottPlot.Color.FromHtml("#000000"),
                    LineColor = ScottPlot.Color.FromHtml("#000000"),
                    LineWidth = 0
                });
            }
            if (bars.Count == 0) return;
            VolumeChart.Plot.Add.Bars(bars);
            VolumeChart.Plot.Axes.DateTimeTicksBottom();
            ConfigureVolumeAxes();
            VolumeChart.Plot.Axes.Color(ScottPlot.Color.FromHtml(_settings.AxisColor));
            VolumeChart.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHtml(_settings.GridColor);
            VolumeChart.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml(_settings.FigureBackground);
            VolumeChart.Plot.DataBackground.Color = ScottPlot.Color.FromHtml(_settings.DataBackground);
            double maxVolume = bars.Max(x => x.Value);
            if (maxVolume <= 0) maxVolume = 1;
            var mainLimits = Chart.Plot.Axes.GetLimits();
            VolumeChart.Plot.Axes.SetLimits(mainLimits.Left, mainLimits.Right, 0, maxVolume * 1.05);
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

        private DateTime GetBarDateTime(MarketBar bar, int index)
        {
            if (bar.Timestamp.HasValue && bar.Timestamp.Value > DateTime.MinValue && bar.Timestamp.Value < DateTime.MaxValue)
                return bar.Timestamp.Value;
            return new DateTime(2000, 1, 1).AddDays(index);
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
            var limits = Chart.Plot.Axes.GetLimits();
            _initialXMin = limits.Left;
            _initialXMax = limits.Right;
            _initialYMin = limits.Bottom;
            _initialYMax = limits.Top;
            _hasInitialView = true;
        }

        private void SyncVolumeXAxis()
        {
            if (!_volumeVisible || _bars.Count == 0) return;

            var mainLimits = Chart.Plot.Axes.GetLimits();
            double left = mainLimits.Left;
            double right = mainLimits.Right;
            if (!double.IsFinite(left) || !double.IsFinite(right) || right <= left) return;

            double maxVisibleVolume = 0;
            for (int i = 0; i < _bars.Count; i++)
            {
                double x = GetBarDateTime(_bars[i], i).ToOADate();
                if (x < left || x > right) continue;
                double volume = _bars[i].Volume / VolumeScale;
                if (double.IsFinite(volume) && volume > maxVisibleVolume) maxVisibleVolume = volume;
            }

            if (maxVisibleVolume <= 0)
            {
                double maxVolume = 0;
                for (int i = 0; i < _bars.Count; i++)
                {
                    double volume = _bars[i].Volume / VolumeScale;
                    if (double.IsFinite(volume) && volume > maxVolume) maxVolume = volume;
                }
                maxVisibleVolume = maxVolume;
            }

            double volumeTop = maxVisibleVolume > 0 ? maxVisibleVolume * 1.05 : 1.0;
            VolumeChart.Plot.Axes.SetLimits(left, right, 0, volumeTop);
            ConfigureVolumeAxes();
        }

        private void VolumeButton_Click(object sender, RoutedEventArgs e)
        {
            SetVolumeVisible(!_volumeVisible, true);
        }

        private void SetVolumeVisible(bool visible, bool refresh)
        {
            _volumeVisible = visible;
            if (_volumeVisible)
            {
                MainChartRow.Height = new GridLength(3, GridUnitType.Star);
                VolumeChartRow.Height = new GridLength(1, GridUnitType.Star);
                VolumeContainer.Visibility = Visibility.Visible;
                SyncVolumeXAxis();
            }
            else
            {
                VolumeContainer.Visibility = Visibility.Collapsed;
                VolumeChartRow.Height = new GridLength(0);
                MainChartRow.Height = new GridLength(1, GridUnitType.Star);
            }
            if (refresh)
            {
                Chart.Refresh();
                VolumeChart.Refresh();
            }
        }
    }
}
