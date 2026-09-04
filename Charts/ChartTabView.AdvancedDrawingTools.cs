using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private const int AdvancedToolParallelChannel = 5;
        private const int AdvancedToolRectangle = 6;
        private const int AdvancedToolPitchfork = 7;

        private sealed class ParallelChannelDrawing
        {
            public ScottPlot.Coordinates A { get; init; }
            public ScottPlot.Coordinates B { get; init; }
            public double OffsetY { get; init; }
            public ScottPlot.Plottables.Scatter? BaseLine { get; set; }
            public ScottPlot.Plottables.Scatter? ParallelLine { get; set; }
        }

        private sealed class RectangleDrawing
        {
            public ScottPlot.Coordinates A { get; init; }
            public ScottPlot.Coordinates B { get; init; }
            public ScottPlot.Plottables.Scatter? PlotLine { get; set; }
        }

        private sealed class PitchforkDrawing
        {
            public ScottPlot.Coordinates A { get; init; }
            public ScottPlot.Coordinates B { get; init; }
            public ScottPlot.Coordinates C { get; init; }
            public ScottPlot.Plottables.Scatter? MedianLine { get; set; }
            public ScottPlot.Plottables.Scatter? UpperLine { get; set; }
            public ScottPlot.Plottables.Scatter? LowerLine { get; set; }
        }

        private readonly List<ParallelChannelDrawing> _parallelChannels = new();
        private readonly List<RectangleDrawing> _drawingRectangles = new();
        private readonly List<PitchforkDrawing> _pitchforks = new();
        private ScottPlot.Coordinates? _advancedDrawingP1;
        private ScottPlot.Coordinates? _advancedDrawingP2;
        private ScottPlot.Coordinates? _horizontalRayStart;
        private ScottPlot.Plottables.Scatter? _advancedDrawingPreview1;
        private ScottPlot.Plottables.Scatter? _advancedDrawingPreview2;
        private ScottPlot.Plottables.Scatter? _horizontalRayPreview;
        private bool _advancedDrawingToolsAttached;

        private void AttachAdvancedDrawingTools()
        {
            if (_advancedDrawingToolsAttached) return;
            _advancedDrawingToolsAttached = true;

            DrawingParallelChannelButton.Click += DrawingParallelChannelButton_Click_Advanced;
            DrawingRectangleButton.Click += DrawingRectangleButton_Click_Advanced;
            DrawingPitchforkButton.Click += DrawingPitchforkButton_Click_Advanced;

            // Replace the original left-click/move subscriptions so Ray can be
            // implemented as a horizontal half-line without changing the
            // already-working Trend/Horizontal/Vertical tools.
            Chart.PreviewMouseLeftButtonDown -= TechnicalDrawing_MouseDown;
            Chart.PreviewMouseMove -= TechnicalDrawing_MouseMove;
            Chart.PreviewMouseLeftButtonDown += AdvancedDrawing_MouseDown;
            Chart.PreviewMouseMove += AdvancedDrawing_MouseMove;
            Chart.PreviewMouseRightButtonDown += AdvancedDrawing_RightMouseDown;
            Chart.PreviewKeyDown += AdvancedDrawing_KeyDown;
        }

        private void DrawingParallelChannelButton_Click_Advanced(object sender, RoutedEventArgs e)
        {
            _textDrawingActive = false;
            SetAdvancedDrawingTool(AdvancedToolParallelChannel);
        }

        private void DrawingRectangleButton_Click_Advanced(object sender, RoutedEventArgs e)
        {
            _textDrawingActive = false;
            SetAdvancedDrawingTool(AdvancedToolRectangle);
        }

        private void DrawingPitchforkButton_Click_Advanced(object sender, RoutedEventArgs e)
        {
            _textDrawingActive = false;
            SetAdvancedDrawingTool(AdvancedToolPitchfork);
        }

        private void SetAdvancedDrawingTool(int tool)
        {
            RemoveAdvancedPreview();
            _advancedDrawingP1 = null;
            _advancedDrawingP2 = null;
            _horizontalRayStart = null;
            RemoveHorizontalRayPreview();
            _activeDrawingTool = (TechnicalDrawingTool)tool;
            Chart.UserInputProcessor.IsEnabled = false;
            DrawingParallelChannelButton.Opacity = tool == AdvancedToolParallelChannel ? 1.0 : 0.55;
            DrawingRectangleButton.Opacity = tool == AdvancedToolRectangle ? 1.0 : 0.55;
            DrawingPitchforkButton.Opacity = tool == AdvancedToolPitchfork ? 1.0 : 0.55;
            Chart.Focusable = true;
            Chart.Focus();
            Focus();
            Chart.Refresh();
        }

        private bool IsAdvancedDrawingTool =>
            (int)_activeDrawingTool == AdvancedToolParallelChannel ||
            (int)_activeDrawingTool == AdvancedToolRectangle ||
            (int)_activeDrawingTool == AdvancedToolPitchfork;

        private bool TryGetAdvancedPoint(WpfMouseEventArgs e, out ScottPlot.Coordinates point)
        {
            point = default;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates))
                return false;
            int index = FindNearestDrawingBarIndex(coordinates.X);
            if (index < 0) return false;
            point = new ScottPlot.Coordinates(GetDrawingX(index), coordinates.Y);
            return true;
        }

        private bool TryGetRawChartPoint(WpfMouseEventArgs e, out ScottPlot.Coordinates point)
        {
            point = default;
            return TryGetChartCoordinates(Chart, e.GetPosition(Chart), out point);
        }

        private void AdvancedDrawing_MouseDown(object sender, WpfMouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || _textDrawingActive) return;

            // Horizontal half-line (Ray): click 1 fixes the start point;
            // click 2 fixes only the direction. The final line is horizontal
            // and extends from the start point to the corresponding chart edge.
            if (_activeDrawingTool == TechnicalDrawingTool.Ray)
            {
                if (!TryGetRawChartPoint(e, out ScottPlot.Coordinates rayPoint)) return;
                int rayIndex = FindNearestDrawingBarIndex(rayPoint.X);
                if (rayIndex < 0) return;
                rayPoint = new ScottPlot.Coordinates(GetDrawingX(rayIndex), rayPoint.Y);

                if (_horizontalRayStart == null)
                {
                    _horizontalRayStart = rayPoint;
                    ChartInfoTextBlock.Text = $"{_symbol.Symbol} | نیم‌خط افقی: نقطه شروع انتخاب شد؛ جهت را با کلیک دوم مشخص کنید";
                }
                else
                {
                    double dx = rayPoint.X - _horizontalRayStart.Value.X;
                    if (Math.Abs(dx) < 1e-12) return;
                    AddHorizontalRayToChart(_horizontalRayStart.Value, dx > 0);
                    _horizontalRayStart = null;
                    RemoveHorizontalRayPreview();
                    ChartInfoTextBlock.Text = $"{_symbol.Symbol} | نیم‌خط افقی رسم شد";
                }

                e.Handled = true;
                Chart.Refresh();
                return;
            }

            // Existing tools keep their original implementation.
            if (!IsAdvancedDrawingTool)
            {
                TechnicalDrawing_MouseDown(sender, e);
                return;
            }

            if (!TryGetAdvancedPoint(e, out ScottPlot.Coordinates point)) return;

            if ((int)_activeDrawingTool == AdvancedToolRectangle)
            {
                if (_advancedDrawingP1 == null)
                {
                    _advancedDrawingP1 = point;
                    ChartInfoTextBlock.Text = $"{_symbol.Symbol} | مستطیل: گوشه اول انتخاب شد؛ گوشه دوم را کلیک کنید";
                }
                else
                {
                    var drawing = new RectangleDrawing { A = _advancedDrawingP1.Value, B = point };
                    _drawingRectangles.Add(drawing);
                    AddRectangleToChart(drawing);
                    _advancedDrawingP1 = null;
                    RemoveAdvancedPreview();
                    ChartInfoTextBlock.Text = $"{_symbol.Symbol} | مستطیل رسم شد";
                }
                e.Handled = true;
                Chart.Refresh();
                return;
            }

            if (_advancedDrawingP1 == null)
            {
                _advancedDrawingP1 = point;
                ChartInfoTextBlock.Text = (int)_activeDrawingTool == AdvancedToolParallelChannel
                    ? $"{_symbol.Symbol} | کانال موازی: نقطه اول خط پایه انتخاب شد"
                    : $"{_symbol.Symbol} | چنگال: نقطه اول انتخاب شد؛ نقطه دوم را کلیک کنید";
                e.Handled = true;
                return;
            }

            if (_advancedDrawingP2 == null)
            {
                _advancedDrawingP2 = point;
                ChartInfoTextBlock.Text = (int)_activeDrawingTool == AdvancedToolParallelChannel
                    ? $"{_symbol.Symbol} | کانال موازی: نقطه دوم خط پایه انتخاب شد؛ نقطه سوم را برای فاصله کانال کلیک کنید"
                    : $"{_symbol.Symbol} | چنگال: نقطه دوم انتخاب شد؛ نقطه سوم را کلیک کنید";
                e.Handled = true;
                return;
            }

            if ((int)_activeDrawingTool == AdvancedToolParallelChannel)
            {
                double offset = point.Y - _advancedDrawingP2.Value.Y;
                var drawing = new ParallelChannelDrawing
                {
                    A = _advancedDrawingP1.Value,
                    B = _advancedDrawingP2.Value,
                    OffsetY = offset
                };
                _parallelChannels.Add(drawing);
                AddParallelChannelToChart(drawing);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | کانال موازی رسم شد";
            }
            else
            {
                var drawing = new PitchforkDrawing
                {
                    A = _advancedDrawingP1.Value,
                    B = _advancedDrawingP2.Value,
                    C = point
                };
                _pitchforks.Add(drawing);
                AddPitchforkToChart(drawing);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | چنگال اندروز رسم شد";
            }

            _advancedDrawingP1 = null;
            _advancedDrawingP2 = null;
            RemoveAdvancedPreview();
            e.Handled = true;
            Chart.Refresh();
        }

        private void AdvancedDrawing_MouseMove(object sender, WpfMouseEventArgs e)
        {
            if (_textDrawingActive) return;

            if (_activeDrawingTool == TechnicalDrawingTool.Ray)
            {
                if (_horizontalRayStart == null || !TryGetRawChartPoint(e, out ScottPlot.Coordinates rayPoint)) return;
                double dx = rayPoint.X - _horizontalRayStart.Value.X;
                if (Math.Abs(dx) < 1e-12) return;
                RemoveHorizontalRayPreview();
                var limits = Chart.Plot.Axes.GetLimits();
                double endX = dx > 0 ? limits.Right : limits.Left;
                _horizontalRayPreview = AddScatterLine(_horizontalRayStart.Value.X, _horizontalRayStart.Value.Y, endX, _horizontalRayStart.Value.Y);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | نیم‌خط افقی: جهت را انتخاب کنید";
                Chart.Refresh();
                return;
            }

            if (!IsAdvancedDrawingTool)
            {
                TechnicalDrawing_MouseMove(sender, e);
                return;
            }

            if (!TryGetAdvancedPoint(e, out ScottPlot.Coordinates point)) return;

            RemoveAdvancedPreview();
            if ((int)_activeDrawingTool == AdvancedToolRectangle && _advancedDrawingP1 != null)
            {
                double left = Math.Min(_advancedDrawingP1.Value.X, point.X);
                double right = Math.Max(_advancedDrawingP1.Value.X, point.X);
                double bottom = Math.Min(_advancedDrawingP1.Value.Y, point.Y);
                double top = Math.Max(_advancedDrawingP1.Value.Y, point.Y);
                _advancedDrawingPreview1 = AddScatterLine(left, bottom, right, bottom);
                _advancedDrawingPreview2 = AddScatterLine(left, top, right, top);
            }
            else if ((int)_activeDrawingTool == AdvancedToolParallelChannel && _advancedDrawingP1 != null)
            {
                if (_advancedDrawingP2 == null)
                    _advancedDrawingPreview1 = AddScatterLine(_advancedDrawingP1.Value.X, _advancedDrawingP1.Value.Y, point.X, point.Y);
                else
                {
                    double offset = point.Y - _advancedDrawingP2.Value.Y;
                    _advancedDrawingPreview1 = AddInfiniteDirectionLine(_advancedDrawingP1.Value, _advancedDrawingP2.Value, 0);
                    _advancedDrawingPreview2 = AddInfiniteDirectionLine(_advancedDrawingP1.Value, _advancedDrawingP2.Value, offset);
                }
            }
            else if ((int)_activeDrawingTool == AdvancedToolPitchfork && _advancedDrawingP1 != null)
            {
                if (_advancedDrawingP2 == null)
                    _advancedDrawingPreview1 = AddScatterLine(_advancedDrawingP1.Value.X, _advancedDrawingP1.Value.Y, point.X, point.Y);
                else
                {
                    var median = Midpoint(_advancedDrawingP2.Value, point);
                    _advancedDrawingPreview1 = AddInfiniteDirectionLine(_advancedDrawingP1.Value, median, 0);
                    _advancedDrawingPreview2 = AddInfiniteDirectionLine(_advancedDrawingP2.Value, median, 0);
                }
            }
            Chart.Refresh();
        }

        private void AdvancedDrawing_RightMouseDown(object sender, WpfMouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right) return;
            if (_activeDrawingTool == TechnicalDrawingTool.Ray || IsAdvancedDrawingTool)
            {
                _horizontalRayStart = null;
                RemoveHorizontalRayPreview();
                RemoveAdvancedPreview();
            }
        }

        private void AdvancedDrawing_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            if (_activeDrawingTool == TechnicalDrawingTool.Ray || IsAdvancedDrawingTool)
            {
                _horizontalRayStart = null;
                RemoveHorizontalRayPreview();
                RemoveAdvancedPreview();
            }
        }

        private void RemoveAdvancedPreview()
        {
            if (_advancedDrawingPreview1 != null) Chart.Plot.Remove(_advancedDrawingPreview1);
            if (_advancedDrawingPreview2 != null) Chart.Plot.Remove(_advancedDrawingPreview2);
            _advancedDrawingPreview1 = null;
            _advancedDrawingPreview2 = null;
        }

        private void RemoveHorizontalRayPreview()
        {
            if (_horizontalRayPreview != null) Chart.Plot.Remove(_horizontalRayPreview);
            _horizontalRayPreview = null;
        }

        private ScottPlot.Plottables.Scatter AddScatterLine(double x1, double y1, double x2, double y2)
        {
            var line = Chart.Plot.Add.ScatterLine(new[] { x1, x2 }, new[] { y1, y2 });
            line.MarkerSize = 0;
            line.LineWidth = (float)Math.Max(1.0, _settings.LineWidth);
            line.LineColor = ScottPlot.Color.FromHtml(_settings.LineColor);
            return line;
        }

        private void AddHorizontalRayToChart(ScottPlot.Coordinates start, bool toRight)
        {
            var limits = Chart.Plot.Axes.GetLimits();
            double endX = toRight ? limits.Right : limits.Left;
            AddScatterLine(start.X, start.Y, endX, start.Y);
        }

        private ScottPlot.Plottables.Scatter AddInfiniteDirectionLine(ScottPlot.Coordinates a, ScottPlot.Coordinates b, double offsetY)
        {
            var limits = Chart.Plot.Axes.GetLimits();
            double dx = b.X - a.X;
            if (Math.Abs(dx) < 1e-12)
                return AddScatterLine(a.X, a.Y + offsetY, a.X, limits.Top);
            double y1 = a.Y + offsetY;
            double y2 = y1 + (b.Y - a.Y) / dx * (limits.Right - a.X);
            return AddScatterLine(a.X, y1, limits.Right, y2);
        }

        private void AddRectangleToChart(RectangleDrawing d)
        {
            double left = Math.Min(d.A.X, d.B.X), right = Math.Max(d.A.X, d.B.X);
            double bottom = Math.Min(d.A.Y, d.B.Y), top = Math.Max(d.A.Y, d.B.Y);
            d.PlotLine = AddScatterLine(left, bottom, right, bottom);
            AddScatterLine(right, bottom, right, top);
            AddScatterLine(right, top, left, top);
            AddScatterLine(left, top, left, bottom);
        }

        private void AddParallelChannelToChart(ParallelChannelDrawing d)
        {
            d.BaseLine = AddInfiniteDirectionLine(d.A, d.B, 0);
            d.ParallelLine = AddInfiniteDirectionLine(d.A, d.B, d.OffsetY);
        }

        private static ScottPlot.Coordinates Midpoint(ScottPlot.Coordinates a, ScottPlot.Coordinates b) =>
            new((a.X + b.X) / 2.0, (a.Y + b.Y) / 2.0);

        private void AddPitchforkToChart(PitchforkDrawing d)
        {
            var median = Midpoint(d.B, d.C);
            d.MedianLine = AddInfiniteDirectionLine(d.A, median, 0);
            d.UpperLine = AddInfiniteDirectionLine(d.B, median, 0);
            d.LowerLine = AddInfiniteDirectionLine(d.C, median, 0);
        }
    }
}
