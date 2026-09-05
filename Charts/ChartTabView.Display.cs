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
        private static readonly bool _displayFixesRegistered = RegisterDisplayFixes();

        private static bool RegisterDisplayFixes()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(DisplayFixes_Loaded));
            return true;
        }

        private static void DisplayFixes_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart) return;
            chart.ApplyDisplayFixesNow();
            ChartSettingsManager.SettingsChanged -= chart.DisplayFixes_SettingsChanged;
            ChartSettingsManager.SettingsChanged += chart.DisplayFixes_SettingsChanged;
        }

        private void DisplayFixes_SettingsChanged(object? sender, EventArgs e)
        {
            if (Dispatcher.CheckAccess()) ApplyDisplayFixesNow();
            else Dispatcher.InvokeAsync(ApplyDisplayFixesNow);
        }

        private void ApplyDisplayFixesNow()
        {
            try
            {
                _settings = ChartSettingsManager.Current;
                _chartType = _settings.ChartType?.Trim().ToLowerInvariant() switch
                {
                    "line" => ChartDisplayType.Line,
                    "bar" => ChartDisplayType.Bar,
                    _ => ChartDisplayType.Candlestick
                };
                _gridVisible = _settings.GridVisible;
                _crosshairVisible = _settings.CrosshairVisible;
                int desiredIndex = _chartType switch
                {
                    ChartDisplayType.Line => 1,
                    ChartDisplayType.Bar => 2,
                    _ => 0
                };
                if (ChartTypeComboBox.SelectedIndex != desiredIndex)
                    ChartTypeComboBox.SelectedIndex = desiredIndex;
                ApplyDisplayCrosshairSettings();
                ApplyDisplayCrosshairLayout();
                ApplyDisplayGridSettings(Chart);
                ApplyDisplayBackgroundAndAxes(Chart);
                ApplyDisplaySeriesWidths();
                ConfigureDisplayDateAxis(Chart);
                ApplyUnifiedPlotBorders();
                ApplyGridDisplayState();
                ApplyCrosshairDisplayState();
                UpdateDisplayStateButtons();
                Chart.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Display fixes failed: {ex}");
            }
        }

        private void ApplyDisplayCrosshairSettings()
        {
            if (_crosshair == null) return;
            _crosshair.LineColor = ScottPlot.Color.FromHtml(_settings.CrosshairColor);
            _crosshair.LineWidth = (float)Math.Max(0.01, _settings.CrosshairLineWidth);
            _crosshair.LinePattern = ParseDisplayPattern(_settings.CrosshairPattern);
            _crosshair.HorizontalLine.LabelOppositeAxis = false;
            _crosshair.VerticalLine.LabelOppositeAxis = false;
            _crosshair.HorizontalLine.LabelAlignment = ScottPlot.Alignment.MiddleRight;
            _crosshair.VerticalLine.LabelAlignment = ScottPlot.Alignment.LowerCenter;
        }

        private void ApplyDisplayCrosshairLayout()
        {
            float leftWidth = Math.Max(85, Chart.Plot.Axes.Left.MinimumSize);
            float rightWidth = Math.Max(0, Chart.Plot.Axes.Right.MinimumSize);
            Chart.Plot.Axes.Left.MinimumSize = leftWidth;
            Chart.Plot.Axes.Right.MinimumSize = rightWidth;
            Chart.Plot.Axes.Bottom.MinimumSize = Math.Max(38, Chart.Plot.Axes.Bottom.MinimumSize);
        }

        private static ScottPlot.LinePattern ParseDisplayPattern(string? value) => value?.Trim().ToLowerInvariant() switch
        {
            "dotted" => ScottPlot.LinePattern.Dotted,
            "dashed" => ScottPlot.LinePattern.Dashed,
            "denselydashed" => ScottPlot.LinePattern.DenselyDashed,
            _ => ScottPlot.LinePattern.Solid
        };

        private void ApplyDisplayGridSettings(ScottPlot.WPF.WpfPlot plot)
        {
            plot.Plot.Grid.IsVisible = _settings.GridVisible;
            plot.Plot.Grid.LineColor = ScottPlot.Color.FromHtml(_settings.GridColor);
            plot.Plot.Grid.LinePattern = ParseDisplayPattern(_settings.GridPattern);
            plot.Plot.Grid.MajorLineWidth = (float)Math.Max(0.01, _settings.GridLineWidth);
            plot.Plot.Grid.MinorLineWidth = (float)Math.Max(0.01, _settings.GridLineWidth);
        }

        private void ApplyDisplayBackgroundAndAxes(ScottPlot.WPF.WpfPlot plot)
        {
            plot.Plot.FigureBackground.Color = ScottPlot.Color.FromHtml(_settings.FigureBackground);
            plot.Plot.DataBackground.Color = ScottPlot.Color.FromHtml(_settings.DataBackground);
            plot.Plot.Axes.Color(ScottPlot.Color.FromHtml(_settings.AxisColor));
        }

        private void ApplyDisplaySeriesWidths()
        {
            foreach (var plottable in Chart.Plot.GetPlottables())
            {
                if (plottable is ScottPlot.Plottables.CandlestickPlot candles)
                {
                    candles.RisingLineStyle.Width = (float)Math.Max(0.01, _settings.CandleLineWidth);
                    candles.FallingLineStyle.Width = (float)Math.Max(0.01, _settings.CandleLineWidth);
                }
                else if (plottable is ScottPlot.Plottables.OhlcPlot ohlc)
                {
                    ohlc.RisingStyle.Width = (float)Math.Max(0.01, _settings.BarLineWidth);
                    ohlc.FallingStyle.Width = (float)Math.Max(0.01, _settings.BarLineWidth);
                }
                else if (plottable is ScottPlot.Plottables.Scatter scatter)
                {
                    scatter.LineWidth = (float)Math.Max(0.01, _settings.LineWidth);
                }
            }
        }

        private void ConfigureDisplayDateAxis(ScottPlot.WPF.WpfPlot plot)
        {
            try
            {
                if (!ChartSettingsManager.Current.ShowTimeGaps && _bars.Count > 0)
                {
                    ConfigureContinuousDateAxis();
                    return;
                }
                bool hasSourceDate = _bars.Any(b => !string.IsNullOrWhiteSpace(b.Calendar) && !string.IsNullOrWhiteSpace(b.JalaliDate));
                if (hasSourceDate)
                {
                    var axis = plot.Plot.Axes.DateTimeTicksBottom();
                    if (axis.TickGenerator is ScottPlot.TickGenerators.DateTimeAutomatic auto)
                        auto.LabelFormatter = dt => dt.TimeOfDay == TimeSpan.Zero
                            ? dt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)
                            : dt.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);
                }
                else
                {
                    var axis = plot.Plot.Axes.NumericTicksBottom();
                    int count = _bars.Count;
                    if (count == 0) return;
                    int step = Math.Max(1, count / 8);
                    var positions = new List<double>();
                    var labels = new List<string>();
                    for (int i = 0; i < count; i += step)
                    {
                        positions.Add(i);
                        labels.Add($"کندل {i + 1}");
                    }
                    int lastIndex = count - 1;
                    if (positions.Count == 0 || Math.Abs(positions[^1] - lastIndex) > 1e-12)
                    {
                        positions.Add(lastIndex);
                        labels.Add($"کندل {count}");
                    }
                    axis.TickGenerator = new ScottPlot.TickGenerators.NumericManual(positions.ToArray(), labels.ToArray());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Display date axis failed: {ex}");
            }
        }

        private static readonly bool _displayStatePersistenceRegistered = RegisterDisplayStatePersistence();
        private bool _displayStatePersistenceInitialized;
        private bool _displayStatePersistenceHandlersAttached;

        private static bool RegisterDisplayStatePersistence()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(DisplayStatePersistence_Loaded));
            return true;
        }

        private static void DisplayStatePersistence_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart) chart.InitializeDisplayStatePersistence();
        }

        private void InitializeDisplayStatePersistence()
        {
            if (_displayStatePersistenceInitialized) return;
            _settings = ChartSettingsManager.Current;
            _gridVisible = _settings.GridVisible;
            _crosshairVisible = _settings.CrosshairVisible;
            _chartType = _settings.ChartType?.Trim().ToLowerInvariant() switch
            {
                "line" => ChartDisplayType.Line,
                "bar" => ChartDisplayType.Bar,
                _ => ChartDisplayType.Candlestick
            };
            int desiredIndex = _chartType switch
            {
                ChartDisplayType.Line => 1,
                ChartDisplayType.Bar => 2,
                _ => 0
            };
            if (ChartTypeComboBox.SelectedIndex != desiredIndex)
                ChartTypeComboBox.SelectedIndex = desiredIndex;
            _displayStatePersistenceInitialized = true;
            ApplyStoredChartSettings();
            InitializeCrosshairAtInitialPosition();
            ApplyGridDisplayState();
            ApplyCrosshairDisplayState();
            UpdateDisplayStateButtons();
            AttachDisplayStatePersistenceHandlers();
        }

        private void AttachDisplayStatePersistenceHandlers()
        {
            if (_displayStatePersistenceHandlersAttached) return;
            GridButton.Click += DisplayStateGridButton_ClickAfterStateChange;
            CrosshairButton.Click += DisplayStateCrosshairButton_ClickAfterStateChange;
            ChartTypeComboBox.SelectionChanged += DisplayStateChartType_SelectionChangedAfterStateChange;
            _displayStatePersistenceHandlersAttached = true;
        }

        private void DisplayStateGridButton_ClickAfterStateChange(object sender, RoutedEventArgs e) => SaveCurrentDisplayState();
        private void DisplayStateCrosshairButton_ClickAfterStateChange(object sender, RoutedEventArgs e) => SaveCurrentDisplayState();

        private void DisplayStateChartType_SelectionChangedAfterStateChange(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ReferenceEquals(e.OriginalSource, ChartTypeComboBox)) SaveCurrentDisplayState();
        }

        private void SaveCurrentDisplayState()
        {
            if (!_displayStatePersistenceInitialized) return;
            try
            {
                ChartSettings settings = ChartSettingsManager.Current;
                settings.GridVisible = _gridVisible;
                settings.CrosshairVisible = _crosshairVisible;
                settings.ChartType = _chartType switch
                {
                    ChartDisplayType.Line => "Line",
                    ChartDisplayType.Bar => "Bar",
                    _ => "Candlestick"
                };
                ChartSettingsManager.Save(settings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Chart display state save failed: {ex}");
            }
        }

        private void UpdateDisplayStateButtons()
        {
            CrosshairButton.Content = _crosshairVisible ? "Crosshair روشن" : "Crosshair خاموش";
            GridButton.Content = _gridVisible ? "GRID" : "GRID خاموش";
        }

        private void ApplyGridDisplayState() => SetGridVisibility(Chart, _gridVisible);

        private void ApplyCrosshairDisplayState()
        {
            if (_crosshair != null)
                _crosshair.IsVisible = _crosshairVisible && _chartVisible && (_crosshairMouseInside || !_hasInitialView);
        }

        private static readonly bool _finalChartFixesRegistered = RegisterFinalChartFixes();
        private bool _finalChartFixesInitialized;
        private bool _finalChartMouseMoveAttached;

        private static bool RegisterFinalChartFixes()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent, new RoutedEventHandler(FinalChartFixes_Loaded));
            return true;
        }

        private static void FinalChartFixes_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart || chart._finalChartFixesInitialized) return;
            chart._finalChartFixesInitialized = true;
            chart.Dispatcher.BeginInvoke(new Action(chart.ApplyFinalChartFixes), DispatcherPriority.ContextIdle);
        }

        private void ApplyFinalChartFixes()
        {
            try
            {
                bool showTimeGaps = ChartSettingsManager.Current.ShowTimeGaps;
                if (showTimeGaps)
                {
                    _continuousTimeAxisApplied = false;
                    ConfigureFinalDateAxis();
                    ForceInitial365CandleRange();
                }
                else
                {
                    ApplyContinuousTimeAxis();
                }
                _settings = ChartSettingsManager.Current;
                _gridVisible = _settings.GridVisible;
                _crosshairVisible = _settings.CrosshairVisible;
                ApplyGridDisplayState();
                ApplyCrosshairDisplayState();
                UpdateDisplayStateButtons();
                UpdateInitialOHLCVInfo();
                Chart.Refresh();
                if (!_finalChartMouseMoveAttached)
                {
                    _finalChartMouseMoveAttached = true;
                    Chart.AddHandler(UIElement.MouseMoveEvent, new System.Windows.Input.MouseEventHandler(FinalChartFixes_MouseMove), true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Final chart fixes failed: {ex}");
            }
        }

        private bool HasSourceDate(int index)
        {
            if (index < 0 || index >= _bars.Count) return false;
            MarketBar bar = _bars[index];
            return !string.IsNullOrWhiteSpace(bar.Calendar) && !string.IsNullOrWhiteSpace(bar.JalaliDate);
        }

        private void ConfigureFinalDateAxis()
        {
            int count = _bars.Count;
            if (count == 0) return;
            var axis = Chart.Plot.Axes.NumericTicksBottom();
            int tickCount = Math.Min(9, count);
            var positions = new List<double>(tickCount);
            var labels = new List<string>(tickCount);
            for (int n = 0; n < tickCount; n++)
            {
                int index = tickCount == 1 ? 0 : (int)Math.Round(n * (count - 1.0) / (tickCount - 1.0));
                positions.Add(GetBarDateTime(_bars[index], index).ToOADate());
                string label = HasSourceDate(index) ? GetSourceDateLabel(index) : $"کندل {index + 1}";
                labels.Add(string.IsNullOrWhiteSpace(label) ? $"کندل {index + 1}" : label);
            }
            axis.TickGenerator = new ScottPlot.TickGenerators.NumericManual(positions.ToArray(), labels.ToArray());
        }

        private void ForceInitial365CandleRange()
        {
            if (_bars.Count == 0) return;
            const int visibleCount = 365;
            int firstIndex = Math.Max(0, _bars.Count - visibleCount);
            int lastIndex = _bars.Count - 1;
            double firstX = GetBarDateTime(_bars[firstIndex], firstIndex).ToOADate();
            double lastX = GetBarDateTime(_bars[lastIndex], lastIndex).ToOADate();
            if (!double.IsFinite(firstX) || !double.IsFinite(lastX)) return;
            const double xPadding = 0.5;
            double minPrice = double.MaxValue;
            double maxPrice = double.MinValue;
            for (int i = firstIndex; i <= lastIndex; i++)
            {
                minPrice = Math.Min(minPrice, _bars[i].Low);
                maxPrice = Math.Max(maxPrice, _bars[i].High);
            }
            double rightMargin = Math.Max(1.0, lastX - firstX) * InitialRightMarginFraction / (1.0 - InitialRightMarginFraction);
            if (double.IsFinite(minPrice) && double.IsFinite(maxPrice))
            {
                double range = maxPrice - minPrice;
                double padding = range > 0 ? range * 0.05 : Math.Max(Math.Abs(maxPrice) * 0.01, 1);
                Chart.Plot.Axes.SetLimits(firstX - xPadding, lastX + xPadding + rightMargin, minPrice - padding, maxPrice + padding);
            }
            else
            {
                var current = Chart.Plot.Axes.GetLimits();
                Chart.Plot.Axes.SetLimits(firstX - xPadding, lastX + xPadding + rightMargin, current.Bottom, current.Top);
            }
            _initialCandleRangeApplied = true;
            SaveInitialView();
        }

        private void FinalChartFixes_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                if (_bars.Count == 0 || _continuousTimeAxisApplied) return;
                if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates)) return;
                int index = FindNearestBarIndex(coordinates.X);
                if (index >= 0) UpdateOHLCVInfo(index);
            }
            catch { }
        }

        private void UpdateInitialOHLCVInfo()
        {
            if (_bars.Count > 0) UpdateOHLCVInfo(_bars.Count - 1);
        }

        private void UpdateOHLCVInfo(int index)
        {
            if (index < 0 || index >= _bars.Count) return;
            MarketBar bar = _bars[index];
            string date = HasSourceDate(index) ? GetSourceDateLabel(index) : $"کندل {index + 1}";
            if (string.IsNullOrWhiteSpace(date)) date = $"کندل {index + 1}";
            string time = !string.IsNullOrWhiteSpace(bar.Time) ? bar.Time : GetBarDateTime(bar, index).ToString("HH:mm:ss");
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | تاریخ: {date} | ساعت: {time} | O: {bar.Open:N2}  H: {bar.High:N2}  L: {bar.Low:N2}  C: {bar.Close:N2}  V: {bar.Volume:N0}";
        }
    }
}