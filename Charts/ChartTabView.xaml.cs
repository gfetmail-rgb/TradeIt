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

        private ChartDisplayType _chartType =
            ChartDisplayType.Candlestick;

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


        // =========================================================
        // Axis Drag
        // =========================================================

        private enum AxisDragMode
        {
            None,
            TimeAxis,
            PriceAxis
        }

        private AxisDragMode _axisDragMode =
            AxisDragMode.None;

        private double _axisDragStartX;
        private double _axisDragStartY;


        private const double LeftAxisWidth = 75.0;

        private const double RightAxisWidth = 30.0;

        private const double BottomAxisHeight = 55.0;


        // =========================================================
        // Constructor
        // =========================================================

        public ChartTabView(
            SymbolInfo symbol,
            List<MarketBar> bars)
        {
            InitializeComponent();

            _symbol = symbol;

            _bars =
                bars ?? new List<MarketBar>();

            _settings =
                ChartSettingsManager.Current;


            // -----------------------------------------------------
            // Default chart type
            // -----------------------------------------------------

            ChartTypeComboBox.SelectedIndex = 0;


            // -----------------------------------------------------
            // Interaction
            // -----------------------------------------------------

            ConfigureInteraction();


            // -----------------------------------------------------
            // Main chart events
            // -----------------------------------------------------

            Chart.PreviewMouseWheel +=
                Chart_PreviewMouseWheel;

            Chart.PreviewMouseLeftButtonDown +=
                Chart_PreviewMouseLeftButtonDown;

            Chart.PreviewMouseMove +=
                Chart_PreviewMouseMove;

            Chart.PreviewMouseLeftButtonUp +=
                Chart_PreviewMouseLeftButtonUp;

            Chart.MouseLeave +=
                Chart_MouseLeave;


            // -----------------------------------------------------
            // Volume chart events
            // -----------------------------------------------------

            VolumeChart.PreviewMouseWheel +=
                VolumeChart_PreviewMouseWheel;

            VolumeChart.PreviewMouseMove +=
                VolumeChart_PreviewMouseMove;

            VolumeChart.MouseLeave +=
                VolumeChart_MouseLeave;


            // -----------------------------------------------------
            // Initial volume state
            // -----------------------------------------------------

            VolumeContainer.Visibility =
                Visibility.Collapsed;

            VolumeChartRow.Height =
                new GridLength(0);


            // -----------------------------------------------------
            // Crosshair
            // -----------------------------------------------------

            InitializeCrosshair();


            // -----------------------------------------------------
            // Draw
            // -----------------------------------------------------

            DrawChart();


            SetGridVisibility(
                Chart,
                _gridVisible);

            SetGridVisibility(
                VolumeChart,
                _gridVisible);


            CrosshairButton.Content =
                "Crosshair روشن";

            VolumeButton.Content =
                "نمایش حجم";

            GridButton.Content =
                "GRID";

            HideChartButton.Content =
                "پنهان کردن نمودار";

            HideToolsButton.Content =
                "پنهان کردن ابزارهای تکنیکال";
        }


        // =========================================================
        // Configure Interaction
        // =========================================================

        private void ConfigureInteraction()
        {
            var input =
                Chart.UserInputProcessor;

            input.IsEnabled = true;

            input.LeftClickDragPan(
                true,
                horizontal: true,
                vertical: true);

            input.RightClickDragZoom(
                true,
                horizontal: true,
                vertical: true);

            input.RemoveAll<
                ScottPlot.Interactivity.UserActionResponses.MouseWheelZoom>();
        }


        // =========================================================
        // Clear Main Chart
        // =========================================================

        private void ClearMainChart()
        {
            var plottables =
                Chart.Plot
                    .GetPlottables()
                    .ToList();

            foreach (var plottable in plottables)
            {
                if (ReferenceEquals(
                    plottable,
                    _crosshair))
                {
                    continue;
                }

                Chart.Plot.Remove(
                    plottable);
            }

            if (_crosshair != null)
            {
                _crosshair.IsVisible = false;
            }
        }


        // =========================================================
        // Clear Volume Chart
        // =========================================================

        private void ClearVolumeChart()
        {
            var plottables =
                VolumeChart.Plot
                    .GetPlottables()
                    .ToList();

            foreach (var plottable in plottables)
            {
                VolumeChart.Plot.Remove(
                    plottable);
            }
        }


        // =========================================================
        // Crosshair
        // =========================================================

        private void InitializeCrosshair()
        {
            if (_crosshair != null)
                return;

            _crosshair =
                Chart.Plot.Add.Crosshair(
                    0,
                    0);

            _crosshair.IsVisible =
                false;

            _crosshair.LineColor =
                ScottPlot.Color.FromHtml(
                    "#707070");

            _crosshair.LineWidth =
                1;

            _crosshair.LinePattern =
                ScottPlot.LinePattern.Dashed;

            _crosshair.MarkerSize =
                7;

            _crosshair.MarkerColor =
                ScottPlot.Color.FromHtml(
                    "#202020");

            _crosshair.MarkerFillColor =
                ScottPlot.Color.FromHtml(
                    "#FFFFFF");

            _crosshair.MarkerLineColor =
                ScottPlot.Color.FromHtml(
                    "#202020");

            _crosshair.MarkerLineWidth =
                1;

            _crosshair.TextColor =
                ScottPlot.Color.FromHtml(
                    "#FFFFFF");

            _crosshair.TextBackgroundColor =
                ScottPlot.Color.FromHtml(
                    "#202020");

            _crosshair.FontSize =
                12;

            _crosshair.FontBold =
                true;
        }


        // =========================================================
        // Try Get Chart Coordinates
        // =========================================================

        private bool TryGetChartCoordinates(
            ScottPlot.WPF.WpfPlot chart,
            WpfPoint mousePosition,
            out ScottPlot.Coordinates coordinates)
        {
            coordinates =
                default;

            double width =
                chart.ActualWidth;

            double height =
                chart.ActualHeight;

            if (width <= 0 ||
                height <= 0)
            {
                return false;
            }

            if (mousePosition.X < 0 ||
                mousePosition.Y < 0 ||
                mousePosition.X > width ||
                mousePosition.Y > height)
            {
                return false;
            }

            double scale =
                chart.DisplayScale;

            if (scale <= 0)
            {
                scale = 1.0;
            }

            try
            {
                var pixel =
                    new ScottPlot.Pixel(
                        mousePosition.X * scale,
                        mousePosition.Y * scale);

                coordinates =
                    chart.Plot.GetCoordinates(
                        pixel);

                return true;
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // Update Crosshair
        // =========================================================

        private void UpdateCrosshair(
            WpfPoint mousePosition)
        {
            if (_crosshair == null)
                return;

            if (!_chartVisible ||
                !_crosshairVisible)
            {
                _crosshair.IsVisible =
                    false;

                Chart.Refresh();

                return;
            }

            if (!TryGetChartCoordinates(
                    Chart,
                    mousePosition,
                    out ScottPlot.Coordinates coordinates))
            {
                _crosshair.IsVisible =
                    false;

                Chart.Refresh();

                return;
            }

            _crosshair.Position =
                coordinates;

            _crosshair.IsVisible =
                true;

            _crosshairMouseInside =
                true;

            UpdateMouseInformation(
                coordinates);

            Chart.Refresh();
        }


        // =========================================================
        // Update Crosshair From Volume
        // =========================================================

        private void UpdateCrosshairFromVolume(
            WpfPoint mousePosition)
        {
            if (_crosshair == null)
                return;

            if (!_chartVisible ||
                !_crosshairVisible)
            {
                _crosshair.IsVisible =
                    false;

                Chart.Refresh();

                return;
            }

            if (!TryGetChartCoordinates(
                    VolumeChart,
                    mousePosition,
                    out ScottPlot.Coordinates volumeCoordinates))
            {
                return;
            }

            var mainLimits =
                Chart.Plot.Axes.GetLimits();

            double x =
                volumeCoordinates.X;

            if (x < mainLimits.Left ||
                x > mainLimits.Right)
            {
                return;
            }

            double y =
                (mainLimits.Bottom +
                 mainLimits.Top) / 2.0;

            _crosshair.Position =
                new ScottPlot.Coordinates(
                    x,
                    y);

            _crosshair.IsVisible =
                true;

            _crosshairMouseInside =
                true;

            UpdateMouseInformation(
                new ScottPlot.Coordinates(
                    x,
                    y));

            Chart.Refresh();
        }


        // =========================================================
        // Mouse Information
        // =========================================================

        private void UpdateMouseInformation(
            ScottPlot.Coordinates coordinates)
        {
            try
            {
                DateTime dateTime =
                    DateTime.FromOADate(
                        coordinates.X);

                string dateText;

                if (dateTime.TimeOfDay ==
                    TimeSpan.Zero)
                {
                    dateText =
                        dateTime.ToString(
                            "yyyy/MM/dd");
                }
                else
                {
                    dateText =
                        dateTime.ToString(
                            "yyyy/MM/dd HH:mm");
                }

                string priceText =
                    coordinates.Y.ToString(
                        "N2");

                ChartInfoTextBlock.Text =
                    $"{_symbol.Symbol} | زمان: {dateText} | قیمت: {priceText}";
            }
            catch
            {
                ChartInfoTextBlock.Text =
                    $"{_symbol.Symbol} | قیمت: {coordinates.Y:N2}";
            }
        }


        // =========================================================
        // Mouse Leave - Main
        // =========================================================

        private void Chart_MouseLeave(
            object sender,
            WpfMouseEventArgs e)
        {
            _crosshairMouseInside =
                false;

            if (_crosshair != null)
            {
                _crosshair.IsVisible =
                    false;

                Chart.Refresh();
            }

            ChartInfoTextBlock.Text =
                $"{_symbol.Symbol} | {_bars.Count:N0} داده";
        }


        // =========================================================
        // Mouse Leave - Volume
        // =========================================================

        private void VolumeChart_MouseLeave(
            object sender,
            WpfMouseEventArgs e)
        {
            _crosshairMouseInside =
                false;

            if (_crosshair != null)
            {
                _crosshair.IsVisible =
                    false;

                Chart.Refresh();
            }

            ChartInfoTextBlock.Text =
                $"{_symbol.Symbol} | {_bars.Count:N0} داده";
        }


        // =========================================================
        // Mouse Wheel - Main
        // =========================================================

        private void Chart_PreviewMouseWheel(
            object sender,
            WpfMouseWheelEventArgs e)
        {
            ZoomXAxis(
                e.Delta > 0
                    ? 0.80
                    : 1.25);

            e.Handled =
                true;
        }


        // =========================================================
        // Mouse Wheel - Volume
        // =========================================================

        private void VolumeChart_PreviewMouseWheel(
            object sender,
            WpfMouseWheelEventArgs e)
        {
            ZoomXAxis(
                e.Delta > 0
                    ? 0.80
                    : 1.25);

            e.Handled =
                true;
        }


        // =========================================================
        // Zoom X
        // =========================================================

        private void ZoomXAxis(
            double factor)
        {
            if (!_hasInitialView)
                return;

            var limits =
                Chart.Plot.Axes.GetLimits();

            double xMin =
                limits.Left;

            double xMax =
                limits.Right;

            double yMin =
                limits.Bottom;

            double yMax =
                limits.Top;

            double range =
                xMax - xMin;

            if (range <= 0)
                return;

            double initialRange =
                _initialXMax -
                _initialXMin;

            if (initialRange <= 0)
                initialRange =
                    range;

            double minimumRange =
                initialRange / 10000.0;

            double maximumRange =
                initialRange * 2.0;

            double newRange =
                range * factor;

            newRange =
                Math.Max(
                    minimumRange,
                    Math.Min(
                        maximumRange,
                        newRange));

            double right =
                xMax;

            double left =
                right - newRange;

            Chart.Plot.Axes.SetLimits(
                left,
                right,
                yMin,
                yMax);

            SyncVolumeXAxis();

            Chart.Refresh();

            if (_volumeVisible)
            {
                VolumeChart.Refresh();
            }
        }


        // =========================================================
        // Main Chart Mouse Down
        // =========================================================

        private void Chart_PreviewMouseLeftButtonDown(
            object sender,
            WpfMouseButtonEventArgs e)
        {
            WpfPoint p =
                e.GetPosition(Chart);

            AxisDragMode mode =
                GetAxisDragMode(
                    p.X,
                    p.Y);

            if (e.ClickCount == 2 &&
                mode == AxisDragMode.PriceAxis)
            {
                AutoFitVisiblePriceRange();

                e.Handled =
                    true;

                return;
            }

            if (mode ==
                AxisDragMode.None)
            {
                return;
            }

            _axisDragMode =
                mode;

            _axisDragStartX =
                p.X;

            _axisDragStartY =
                p.Y;

            Chart.CaptureMouse();

            e.Handled =
                true;
        }


        // =========================================================
        // Main Chart Mouse Move
        // =========================================================

        private void Chart_PreviewMouseMove(
            object sender,
            WpfMouseEventArgs e)
        {
            WpfPoint p =
                e.GetPosition(Chart);

            UpdateCrosshair(p);

            if (_axisDragMode ==
                AxisDragMode.None)
            {
                return;
            }

            if (e.LeftButton !=
                WpfMouseButtonState.Pressed)
            {
                EndAxisDrag();

                return;
            }

            double deltaX =
                p.X -
                _axisDragStartX;

            double deltaY =
                p.Y -
                _axisDragStartY;

            if (_axisDragMode ==
                AxisDragMode.TimeAxis)
            {
                if (Math.Abs(deltaX) >= 1)
                {
                    ApplyHorizontalAxisZoom(
                        deltaX);

                    _axisDragStartX =
                        p.X;
                }
            }
            else if (_axisDragMode ==
                     AxisDragMode.PriceAxis)
            {
                if (Math.Abs(deltaY) >= 1)
                {
                    ApplyVerticalAxisZoom(
                        deltaY);

                    _axisDragStartY =
                        p.Y;
                }
            }

            e.Handled =
                true;
        }


        // =========================================================
        // Volume Mouse Move
        // =========================================================

        private void VolumeChart_PreviewMouseMove(
            object sender,
            WpfMouseEventArgs e)
        {
            WpfPoint p =
                e.GetPosition(
                    VolumeChart);

            UpdateCrosshairFromVolume(p);
        }


        // =========================================================
        // Main Chart Mouse Up
        // =========================================================

        private void Chart_PreviewMouseLeftButtonUp(
            object sender,
            WpfMouseButtonEventArgs e)
        {
            if (_axisDragMode ==
                AxisDragMode.None)
            {
                return;
            }

            EndAxisDrag();

            e.Handled =
                true;
        }


        // =========================================================
        // End Axis Drag
        // =========================================================

        private void EndAxisDrag()
        {
            _axisDragMode =
                AxisDragMode.None;

            if (Chart.IsMouseCaptured)
            {
                Chart.ReleaseMouseCapture();
            }
        }


        // =========================================================
        // Get Axis Drag Mode
        // =========================================================

        private AxisDragMode GetAxisDragMode(
            double x,
            double y)
        {
            double width =
                Chart.ActualWidth;

            double height =
                Chart.ActualHeight;

            if (width <= 0 ||
                height <= 0)
            {
                return AxisDragMode.None;
            }

            if (y >=
                height -
                BottomAxisHeight)
            {
                return AxisDragMode.TimeAxis;
            }

            if (x <= LeftAxisWidth ||
                x >=
                width -
                RightAxisWidth)
            {
                return AxisDragMode.PriceAxis;
            }

            return AxisDragMode.None;
        }


        // =========================================================
        // Horizontal Axis Zoom
        // =========================================================

        private void ApplyHorizontalAxisZoom(
            double deltaX)
        {
            var limits =
                Chart.Plot.Axes.GetLimits();

            double range =
                limits.Right -
                limits.Left;

            if (range <= 0)
                return;

            double factor =
                Math.Exp(
                    -deltaX / 180.0);

            double newRange =
                range * factor;

            double initialRange =
                _initialXMax -
                _initialXMin;

            if (initialRange <= 0)
                initialRange =
                    range;

            double minimumRange =
                initialRange / 10000.0;

            double maximumRange =
                initialRange * 2.0;

            newRange =
                Math.Max(
                    minimumRange,
                    Math.Min(
                        maximumRange,
                        newRange));

            double center =
                (limits.Left +
                 limits.Right) /
                2.0;

            double newXMin =
                center -
                newRange / 2.0;

            double newXMax =
                center +
                newRange / 2.0;

            Chart.Plot.Axes.SetLimits(
                newXMin,
                newXMax,
                limits.Bottom,
                limits.Top);

            SyncVolumeXAxis();

            Chart.Refresh();

            if (_volumeVisible)
            {
                VolumeChart.Refresh();
            }
        }


        // =========================================================
        // Vertical Axis Zoom
        // =========================================================

        private void ApplyVerticalAxisZoom(
            double deltaY)
        {
            var limits =
                Chart.Plot.Axes.GetLimits();

            double range =
                limits.Top -
                limits.Bottom;

            if (range <= 0)
                return;

            double factor =
                Math.Exp(
                    deltaY / 180.0);

            double newRange =
                range * factor;

            double initialRange =
                _initialYMax -
                _initialYMin;

            if (initialRange <= 0)
                initialRange =
                    range;

            double minimumRange =
                initialRange / 10000.0;

            double maximumRange =
                initialRange * 2.0;

            newRange =
                Math.Max(
                    minimumRange,
                    Math.Min(
                        maximumRange,
                        newRange));

            double center =
                (limits.Bottom +
                 limits.Top) /
                2.0;

            double newYMin =
                center -
                newRange / 2.0;

            double newYMax =
                center +
                newRange / 2.0;

            Chart.Plot.Axes.SetLimits(
                limits.Left,
                limits.Right,
                newYMin,
                newYMax);

            Chart.Refresh();
        }


        // =========================================================
        // Auto Fit Price
        // =========================================================

        private void AutoFitVisiblePriceRange()
        {
            if (!_hasInitialView ||
                _bars.Count == 0)
            {
                return;
            }

            var limits =
                Chart.Plot.Axes.GetLimits();

            double visibleXMin =
                limits.Left;

            double visibleXMax =
                limits.Right;

            double minPrice =
                double.MaxValue;

            double maxPrice =
                double.MinValue;

            for (int i = 0;
                 i < _bars.Count;
                 i++)
            {
                DateTime time =
                    GetBarDateTime(
                        _bars[i],
                        i);

                double x =
                    time.ToOADate();

                if (x < visibleXMin ||
                    x > visibleXMax)
                {
                    continue;
                }

                minPrice =
                    Math.Min(
                        minPrice,
                        _bars[i].Low);

                maxPrice =
                    Math.Max(
                        maxPrice,
                        _bars[i].High);
            }

            if (minPrice ==
                    double.MaxValue ||
                maxPrice ==
                    double.MinValue)
            {
                return;
            }

            double range =
                maxPrice -
                minPrice;

            if (range <= 0)
            {
                double padding =
                    Math.Abs(maxPrice) *
                    0.01;

                if (padding <= 0)
                    padding = 1;

                minPrice -= padding;
                maxPrice += padding;
            }
            else
            {
                double padding =
                    range * 0.05;

                minPrice -= padding;
                maxPrice += padding;
            }

            Chart.Plot.Axes.SetLimits(
                visibleXMin,
                visibleXMax,
                minPrice,
                maxPrice);

            Chart.Refresh();
        }


        // =========================================================
        // Draw Chart
        // =========================================================

        private void DrawChart()
        {
            if (_bars.Count == 0)
            {
                ClearMainChart();

                ClearVolumeChart();

                _hasInitialView =
                    false;

                Chart.Refresh();

                VolumeChart.Refresh();

                return;
            }

            bool preserveCurrentView =
                _hasInitialView &&
                Chart.ActualWidth > 0 &&
                Chart.ActualHeight > 0;

            ScottPlot.AxisLimits currentLimits =
                default;

            if (preserveCurrentView)
            {
                currentLimits =
                    Chart.Plot.Axes.GetLimits();
            }

            ClearMainChart();

            switch (_chartType)
            {
                case ChartDisplayType.Candlestick:

                    DrawCandlestick();

                    break;

                case ChartDisplayType.Line:

                    DrawLine();

                    break;

                case ChartDisplayType.Bar:

                    DrawBar();

                    break;
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
                Chart.Plot.Axes.SetLimits(
                    currentLimits.Left,
                    currentLimits.Right,
                    currentLimits.Bottom,
                    currentLimits.Top);
            }

            ChartInfoTextBlock.Text =
                $"{_symbol.Symbol} | {_bars.Count:N0} داده";

            if (_volumeVisible)
            {
                SyncVolumeXAxis();
            }

            if (_crosshair != null)
            {
                _crosshair.IsVisible =
                    _crosshairVisible &&
                    _chartVisible &&
                    _crosshairMouseInside;
            }

            Chart.Refresh();

            if (_volumeVisible)
            {
                VolumeChart.Refresh();
            }

            if (!_volumeVisible)
            {
                VolumeContainer.Visibility =
                    Visibility.Collapsed;

                VolumeChartRow.Height =
                    new GridLength(0);

                MainChartRow.Height =
                    new GridLength(
                        1,
                        GridUnitType.Star);
            }
        }


        // =========================================================
        // Candlestick
        // =========================================================

        private void DrawCandlestick()
        {
            var candles =
                new List<ScottPlot.OHLC>();

            for (int i = 0;
                 i < _bars.Count;
                 i++)
            {
                MarketBar bar =
                    _bars[i];

                DateTime time =
                    GetBarDateTime(
                        bar,
                        i);

                candles.Add(
                    new ScottPlot.OHLC(
                        bar.Open,
                        bar.High,
                        bar.Low,
                        bar.Close,
                        time,
                        TimeSpan.FromDays(1)));
            }

            var candlePlot =
                Chart.Plot.Add.Candlestick(
                    candles);

            candlePlot.RisingColor =
                ScottPlot.Color.FromHtml(
                    _settings.RisingColor);

            candlePlot.FallingColor =
                ScottPlot.Color.FromHtml(
                    _settings.FallingColor);

            Chart.Plot.Axes.DateTimeTicksBottom();
        }


        // =========================================================
        // Line
        // =========================================================

        private void DrawLine()
        {
            var xs =
                new DateTime[_bars.Count];

            var ys =
                new double[_bars.Count];

            for (int i = 0;
                 i < _bars.Count;
                 i++)
            {
                xs[i] =
                    GetBarDateTime(
                        _bars[i],
                        i);

                ys[i] =
                    _bars[i].Close;
            }

            var line =
                Chart.Plot.Add.Scatter(
                    xs,
                    ys);

            line.MarkerSize =
                0;

            line.LineWidth =
                (float)_settings.LineWidth;

            line.Color =
                ScottPlot.Color.FromHtml(
                    _settings.LineColor);

            Chart.Plot.Axes.DateTimeTicksBottom();
        }


        // =========================================================
        // Bar
        // =========================================================

        private void DrawBar()
        {
            var ohlcs =
                new List<ScottPlot.OHLC>();

            for (int i = 0;
                 i < _bars.Count;
                 i++)
            {
                MarketBar bar =
                    _bars[i];

                DateTime time =
                    GetBarDateTime(
                        bar,
                        i);

                ohlcs.Add(
                    new ScottPlot.OHLC(
                        bar.Open,
                        bar.High,
                        bar.Low,
                        bar.Close,
                        time,
                        TimeSpan.FromDays(1)));
            }

            if (ohlcs.Count == 0)
                return;

            var ohlcPlot =
                Chart.Plot.Add.OHLC(
                    ohlcs);

            ohlcPlot.RisingStyle.Color =
                ScottPlot.Color.FromHtml(
                    _settings.RisingColor);

            ohlcPlot.FallingStyle.Color =
                ScottPlot.Color.FromHtml(
                    _settings.FallingColor);

            Chart.Plot.Axes.DateTimeTicksBottom();
        }


        // =========================================================
        // Volume
        // =========================================================

        private void DrawVolume()
        {
            ClearVolumeChart();

            if (_bars.Count == 0)
                return;

            var bars =
                new List<ScottPlot.Bar>();

            for (int i = 0;
                 i < _bars.Count;
                 i++)
            {
                MarketBar marketBar =
                    _bars[i];

                DateTime time =
                    GetBarDateTime(
                        marketBar,
                        i);

                double volumeK =
                    marketBar.Volume /
                    VolumeScale;

                if (double.IsNaN(volumeK) ||
                    double.IsInfinity(volumeK) ||
                    volumeK < 0)
                {
                    volumeK = 0;
                }

                bars.Add(
                    new ScottPlot.Bar
                    {
                        Position =
                            time.ToOADate(),

                        Value =
                            volumeK,

                        FillColor =
                            marketBar.Close >=
                            marketBar.Open
                                ? ScottPlot.Color.FromHtml(
                                    _settings.RisingColor)
                                : ScottPlot.Color.FromHtml(
                                    _settings.FallingColor),

                        LineColor =
                            ScottPlot.Color.FromHtml(
                                _settings.AxisColor),

                        LineWidth =
                            0
                    });
            }

            if (bars.Count == 0)
                return;

            VolumeChart.Plot.Add.Bars(
                bars);

            VolumeChart.Plot.Axes
                .DateTimeTicksBottom();

            ConfigureVolumeAxes();

            VolumeChart.Plot.Axes.Color(
                ScottPlot.Color.FromHtml(
                    _settings.AxisColor));

            VolumeChart.Plot.Grid
                .MajorLineColor =
                ScottPlot.Color.FromHtml(
                    _settings.GridColor);

            VolumeChart.Plot
                .FigureBackground.Color =
                ScottPlot.Color.FromHtml(
                    _settings.FigureBackground);

            VolumeChart.Plot
                .DataBackground.Color =
                ScottPlot.Color.FromHtml(
                    _settings.DataBackground);

            double maxVolume =
                bars.Max(
                    x => x.Value);

            if (maxVolume <= 0)
                maxVolume = 1;

            var mainLimits =
                Chart.Plot.Axes.GetLimits();

            VolumeChart.Plot.Axes.SetLimits(
                mainLimits.Left,
                mainLimits.Right,
                0,
                maxVolume * 1.10);

            SetGridVisibility(
                VolumeChart,
                _gridVisible);
        }


        // =========================================================
        // Volume Axes
        // =========================================================

        private void ConfigureVolumeAxes()
        {
            VolumeChart.Plot.Axes.Bottom
                .IsVisible = false;

            VolumeChart.Plot.Axes.Right
                .IsVisible = false;

            VolumeChart.Plot.Axes.Left
                .IsVisible = false;

            VolumeChart.Plot.Axes.Left.Label.Text =
                "";

            VolumeChart.Plot.Axes.Left.Label
                .IsVisible = false;
        }


        // =========================================================
        // Get Bar DateTime
        // =========================================================

        private DateTime GetBarDateTime(
            MarketBar bar,
            int index)
        {
            if (bar.Timestamp.HasValue &&
                bar.Timestamp.Value >
                    DateTime.MinValue &&
                bar.Timestamp.Value <
                    DateTime.MaxValue)
            {
                return bar.Timestamp.Value;
            }

            return new DateTime(
                2000,
                1,
                1).AddDays(index);
        }


        // =========================================================
        // Apply Settings
        // =========================================================

        private void ApplySettings()
        {
            Chart.Plot.FigureBackground.Color =
                ScottPlot.Color.FromHtml(
                    _settings.FigureBackground);

            Chart.Plot.DataBackground.Color =
                ScottPlot.Color.FromHtml(
                    _settings.DataBackground);

            Chart.Plot.Grid.MajorLineColor =
                ScottPlot.Color.FromHtml(
                    _settings.GridColor);

            Chart.Plot.Axes.Color(
                ScottPlot.Color.FromHtml(
                    _settings.AxisColor));


            VolumeChart.Plot.FigureBackground.Color =
                ScottPlot.Color.FromHtml(
                    _settings.FigureBackground);

            VolumeChart.Plot.DataBackground.Color =
                ScottPlot.Color.FromHtml(
                    _settings.DataBackground);

            VolumeChart.Plot.Grid.MajorLineColor =
                ScottPlot.Color.FromHtml(
                    _settings.GridColor);

            VolumeChart.Plot.Axes.Color(
                ScottPlot.Color.FromHtml(
                    _settings.AxisColor));


            SetGridVisibility(
                Chart,
                _gridVisible);

            SetGridVisibility(
                VolumeChart,
                _gridVisible);
        }


        // =========================================================
        // Save Initial View
        // =========================================================

        private void SaveInitialView()
        {
            var limits =
                Chart.Plot.Axes.GetLimits();

            _initialXMin =
                limits.Left;

            _initialXMax =
                limits.Right;

            _initialYMin =
                limits.Bottom;

            _initialYMax =
                limits.Top;

            _hasInitialView =
                true;
        }


        // =========================================================
        // Sync Volume X Axis
        // =========================================================

        private void SyncVolumeXAxis()
        {
            if (!_volumeVisible)
                return;

            var mainLimits =
                Chart.Plot.Axes.GetLimits();

            var volumeLimits =
                VolumeChart.Plot.Axes.GetLimits();

            double volumeTop =
                volumeLimits.Top;

            if (volumeTop <= 0)
            {
                double maxVolume =
                    _bars.Count > 0
                        ? _bars.Max(
                            x =>
                                x.Volume /
                                VolumeScale)
                        : 1;

                volumeTop =
                    Math.Max(
                        1,
                        maxVolume * 1.10);
            }

            VolumeChart.Plot.Axes.SetLimits(
                mainLimits.Left,
                mainLimits.Right,
                0,
                volumeTop);

            ConfigureVolumeAxes();
        }


        // =========================================================
        // Volume Button
        // =========================================================

        private void VolumeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            SetVolumeVisible(
                !_volumeVisible,
                true);
        }


        // =========================================================
        // Set Volume Visible
        // =========================================================

        private void SetVolumeVisible(
            bool visible,
            bool refresh)
        {
            _volumeVisible =
                visible;

            if (_volumeVisible)
            {
                MainChartRow.Height =
                    new GridLength(
                        3,
                        GridUnitType.Star);

                VolumeChartRow.Height =
                    new GridLength(
                        1,
                        GridUnitType.Star);

                VolumeContainer.Visibility =
                    Visibility.Visible;

                DrawVolume();

                SyncVolumeXAxis();

                VolumeButton.Content =
                    "پنهان کردن حجم";
            }
            else
            {
                MainChartRow.Height =
                    new GridLength(
                        1,
                        GridUnitType.Star);

                VolumeChartRow.Height =
                    new GridLength(0);

                VolumeContainer.Visibility =
                    Visibility.Collapsed;

                VolumeButton.Content =
                    "نمایش حجم";
            }

            if (refresh)
            {
                Chart.Refresh();

                if (_volumeVisible)
                {
                    VolumeChart.Refresh();
                }
            }
        }


        // =========================================================
        // Grid Button
        // =========================================================

        private void GridButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _gridVisible =
                !_gridVisible;

            SetGridVisibility(
                Chart,
                _gridVisible);

            SetGridVisibility(
                VolumeChart,
                _gridVisible);

            GridButton.Content =
                _gridVisible
                    ? "GRID"
                    : "GRID خاموش";

            Chart.Refresh();

            if (_volumeVisible)
            {
                VolumeChart.Refresh();
            }
        }


        // =========================================================
        // Grid Visibility
        // =========================================================

        private void SetGridVisibility(
            ScottPlot.WPF.WpfPlot plot,
            bool visible)
        {
            plot.Plot.Grid.IsVisible =
                visible;
        }


        // =========================================================
        // Crosshair Button
        // =========================================================

        private void CrosshairButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _crosshairVisible =
                !_crosshairVisible;

            if (_crosshair != null)
            {
                _crosshair.IsVisible =
                    _crosshairVisible &&
                    _chartVisible &&
                    _crosshairMouseInside;
            }

            CrosshairButton.Content =
                _crosshairVisible
                    ? "Crosshair روشن"
                    : "Crosshair خاموش";

            Chart.Refresh();
        }


        // =========================================================
        // Screenshot
        // =========================================================

        private void ScreenshotButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                int width =
                    (int)Math.Max(
                        1,
                        ActualWidth);

                int height =
                    (int)Math.Max(
                        1,
                        ActualHeight);

                var bitmap =
                    new System.Windows.Media.Imaging
                        .RenderTargetBitmap(
                            width,
                            height,
                            96,
                            96,
                            System.Windows.Media
                                .PixelFormats.Pbgra32);

                bitmap.Render(this);

                var dialog =
                    new Microsoft.Win32
                        .SaveFileDialog
                    {
                        Title =
                            "ذخیره تصویر نمودار",

                        Filter =
                            "PNG Image (*.png)|*.png|" +
                            "JPEG Image (*.jpg)|*.jpg",

                        FileName =
                            $"{_symbol.Symbol}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                    };

                if (dialog.ShowDialog() != true)
                    return;

                System.Windows.Media.Imaging
                    .BitmapEncoder encoder;

                if (
                    Path.GetExtension(
                        dialog.FileName)
                    .Equals(
                        ".jpg",
                        StringComparison
                            .OrdinalIgnoreCase))
                {
                    encoder =
                        new System.Windows.Media.Imaging
                            .JpegBitmapEncoder();
                }
                else
                {
                    encoder =
                        new System.Windows.Media.Imaging
                            .PngBitmapEncoder();
                }

                encoder.Frames.Add(
                    System.Windows.Media.Imaging
                        .BitmapFrame.Create(
                            bitmap));

                using FileStream stream =
                    new FileStream(
                        dialog.FileName,
                        FileMode.Create);

                encoder.Save(stream);

                BottomInfoTextBlock.Text =
                    $"تصویر ذخیره شد: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(
                    $"خطا در گرفتن تصویر نمودار:\n{ex.Message}",
                    "Screenshot",
                    WpfMessageBoxButton.OK,
                    WpfMessageBoxImage.Error);
            }
        }


        // =========================================================
        // Print
        // =========================================================

        private void PrintButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                var dialog =
                    new WpfPrintDialog();

                if (dialog.ShowDialog() != true)
                    return;

                dialog.PrintVisual(
                    this,
                    $"TradeIt - {_symbol.Symbol}");

                BottomInfoTextBlock.Text =
                    "نمودار برای چاپ ارسال شد.";
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show(
                    $"خطا در چاپ نمودار:\n{ex.Message}",
                    "Print",
                    WpfMessageBoxButton.OK,
                    WpfMessageBoxImage.Error);
            }
        }


        // =========================================================
        // Chart Type
        // =========================================================

        private void ChartTypeComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (ChartTypeComboBox.SelectedItem
                is not ComboBoxItem item)
            {
                return;
            }

            string type =
                item.Tag?.ToString() ??
                "";

            _chartType =
                type switch
                {
                    "Line" =>
                        ChartDisplayType.Line,

                    "Bar" =>
                        ChartDisplayType.Bar,

                    _ =>
                        ChartDisplayType.Candlestick
                };

            if (IsLoaded)
            {
                DrawChart();
            }
        }


        // =========================================================
        // Settings
        // =========================================================

        private void SettingsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenSettings();
        }


        private void OpenSettings()
        {
            var window =
                new ChartSettingsWindow(
                    _settings)
                {
                    Owner =
                        Window.GetWindow(this)
                };

            if (window.ShowDialog() == true)
            {
                _settings =
                    ChartSettingsManager.Clone(
                        window.Settings);

                ChartSettingsManager.SetDefaults(
                    _settings);

                DrawChart();
            }
        }


        // =========================================================
        // Hide Chart
        // =========================================================

        private void HideChartButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _chartVisible =
                !_chartVisible;

            foreach (var plottable in
                     Chart.Plot.GetPlottables())
            {
                if (ReferenceEquals(
                    plottable,
                    _crosshair))
                {
                    continue;
                }

                plottable.IsVisible =
                    _chartVisible;
            }

            if (_crosshair != null)
            {
                _crosshair.IsVisible =
                    _chartVisible &&
                    _crosshairVisible &&
                    _crosshairMouseInside;
            }

            Chart.Refresh();

            HideChartButton.Content =
                _chartVisible
                    ? "پنهان کردن نمودار"
                    : "نمایش نمودار";
        }


        // =========================================================
        // Hide Tools
        // =========================================================

        private void HideToolsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _toolsVisible =
                !_toolsVisible;

            HideToolsButton.Content =
                _toolsVisible
                    ? "پنهان کردن ابزارهای تکنیکال"
                    : "نمایش ابزارهای تکنیکال";
        }


        // =========================================================
        // Zoom In
        // =========================================================

        private void ZoomInButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ZoomXAxis(0.80);
        }


        // =========================================================
        // Zoom Out
        // =========================================================

        private void ZoomOutButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ZoomXAxis(1.25);
        }


        // =========================================================
        // Reset Zoom
        // =========================================================

        private void ResetZoomButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!_hasInitialView)
                return;

            Chart.Plot.Axes.SetLimits(
                _initialXMin,
                _initialXMax,
                _initialYMin,
                _initialYMax);

            SyncVolumeXAxis();

            Chart.Refresh();

            if (_volumeVisible)
            {
                VolumeChart.Refresh();
            }
        }


        // =========================================================
        // Full View
        // =========================================================

        private void FullViewButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_bars.Count == 0)
                return;

            Chart.Plot.Axes.AutoScale();

            SaveInitialView();

            SyncVolumeXAxis();

            Chart.Refresh();

            if (_volumeVisible)
            {
                VolumeChart.Refresh();
            }
        }
    }


    // =============================================================
    // Chart Display Type
    // =============================================================

    public enum ChartDisplayType
    {
        Candlestick,
        Line,
        Bar
    }
}