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
            public ScottPlot.Coordinates C { get; init; }
            public ScottPlot.Plottables.Scatter? BaseLine { get; set; }
            public ScottPlot.Plottables.Scatter? ParallelLine { get; set; }
        }

        private sealed class RectangleDrawing
        {
            public ScottPlot.Coordinates A { get; init; }
            public ScottPlot.Coordinates B { get; init; }
            public readonly List<ScottPlot.Plottables.Scatter> Lines = new();
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
            _horizontalRayStart = null;
            RemoveHorizontalRayPreview();
            _advancedDrawingP1 = null;
            _advancedDrawingP2 = null;
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
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates)) return false;
            int index = FindNearestDrawingBarIndex(coordinates.X);
            if (index < 0) return false;
            point = new ScottPlot.Coordinates(GetDrawingX(index), coordinates.Y);
            return true;
        }

        private bool TryGetRawChartPoint(WpfMouseEventArgs e, out ScottPlot.Coordinates point)
            => TryGetChartCoordinates(Chart, e.GetPosition(Chart), out point);

        private void AdvancedDrawing_MouseDown(object sender, WpfMouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || _textDrawingActive) return;

            // Ray is a one-click horizontal half-line from the clicked bar to the right edge.
            if (_activeDrawingTool == TechnicalDrawingTool.Ray)
            {
                if (!TryGetRawChartPoint(e, out ScottPlot.Coordinates p)) return;
                int index = FindNearestDrawingBarIndex(p.X);
                if (index < 0) return;
                p = new ScottPlot.Coordinates(GetDrawingX(index), p.Y);

                AddHorizontalRayToChart(p);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | نیم‌خط افقی رسم شد";
                e.Handled = true;
                Chart.Refresh();
                return;
            }

            if (!IsAdvancedDrawingTool) return;
            if (!TryGetAdvancedPoint(e, out ScottPlot.Coordinates point)) return;

            // Rectangle: exactly two clicks, opposite corners.
            if ((int)_activeDrawingTool == AdvancedToolRectangle)
            {
                if (_advancedDrawingP1 == null)
                {
                    _advancedDrawingP1 = point;
                    ChartInfoTextBlock.Text = $"{_symbol.Symbol} | مستطیل: گوشه اول انتخاب شد؛ گوشه دوم را کلیک کنید";
                }
                else
                {
                    var d = new RectangleDrawing { A = _advancedDrawingP1.Value, B = point };
                    _drawingRectangles.Add(d);
                    AddRectangleToChart(d);
                    _advancedDrawingP1 = null;
                    RemoveAdvancedPreview();
                    ChartInfoTextBlock.Text = $"{_symbol.Symbol} | مستطیل رسم شد";
                }
                e.Handled = true;
                Chart.Refresh();
                return;
            }

            // Parallel channel: A-B is the base line, C defines the parallel.
            // Andrews Pitchfork: A is the first pivot, B and C are the next two pivots.
            if (_advancedDrawingP1 == null)
            {
                _advancedDrawingP1 = point;
                ChartInfoTextBlock.Text = (int)_activeDrawingTool == AdvancedToolParallelChannel
                    ? $"{_symbol.Symbol} | کانال موازی: نقطه اول خط پایه انتخاب شد"
                    : $"{_symbol.Symbol} | چنگال اندروز: نقطه A انتخاب شد؛ نقطه B را کلیک کنید";
                e.Handled = true;
                return;
            }

            if (_advancedDrawingP2 == null)
            {
                _advancedDrawingP2 = point;
                ChartInfoTextBlock.Text = (int)_activeDrawingTool == AdvancedToolParallelChannel
                    ? $"{_symbol.Symbol} | کانال موازی: نقطه دوم خط پایه انتخاب شد؛ نقطه سوم را برای فاصله کلیک کنید"
                    : $"{_symbol.Symbol} | چنگال اندروز: نقطه B انتخاب شد؛ نقطه C را کلیک کنید";
                e.Handled = true;
                return;
            }

            if ((int)_activeDrawingTool == AdvancedToolParallelChannel)
            {
                var d = new ParallelChannelDrawing { A = _advancedDrawingP1.Value, B = _advancedDrawingP2.Value, C = point };
                _parallelChannels.Add(d);
                AddParallelChannelToChart(d);
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | کانال موازی رسم شد";
            }
            else
            {
                var d = new PitchforkDrawing { A = _advancedDrawingP1.Value, B = _advancedDrawingP2.Value, C = point };
                _pitchforks.Add(d);
                AddPitchforkToChart(d);
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

            // Ray preview is always horizontal and starts at the first click.
            if (_activeDrawingTool == TechnicalDrawingTool.Ray)
            {
                return;
            }

            if (!IsAdvancedDrawingTool || !TryGetAdvancedPoint(e, out ScottPlot.Coordinates point)) return;
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
                    _advancedDrawingPreview1 = AddLineThroughPoints(_advancedDrawingP1.Value, _advancedDrawingP2.Value);
                    _advancedDrawingPreview2 = AddParallelLineThroughPoint(_advancedDrawingP1.Value, _advancedDrawingP2.Value, point);
                }
            }
            else if ((int)_activeDrawingTool == AdvancedToolPitchfork && _advancedDrawingP1 != null)
            {
                if (_advancedDrawingP2 == null)
                    _advancedDrawingPreview1 = AddScatterLine(_advancedDrawingP1.Value.X, _advancedDrawingP1.Value.Y, point.X, point.Y);
                else
                {
                    var target = Midpoint(_advancedDrawingP2.Value, point);
                    _advancedDrawingPreview1 = AddRayThroughPoints(_advancedDrawingP1.Value, target);
                    _advancedDrawingPreview2 = AddRayThroughPoints(_advancedDrawingP2.Value, target);
                }
            }
            Chart.Refresh();
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

        private void AddHorizontalRayToChart(ScottPlot.Coordinates start)
        {
            var limits = Chart.Plot.Axes.GetLimits();
            AddScatterLine(start.X, start.Y, limits.Right, start.Y);
        }

        private ScottPlot.Plottables.Scatter AddLineThroughPoints(ScottPlot.Coordinates a, ScottPlot.Coordinates b)
        {
            var limits = Chart.Plot.Axes.GetLimits();
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            if (Math.Abs(dx) < 1e-12) return AddScatterLine(a.X, limits.Bottom, a.X, limits.Top);
            return AddScatterLine(limits.Left, a.Y + dy / dx * (limits.Left - a.X), limits.Right, a.Y + dy / dx * (limits.Right - a.X));
        }

        private ScottPlot.Plottables.Scatter AddParallelLineThroughPoint(ScottPlot.Coordinates a, ScottPlot.Coordinates b, ScottPlot.Coordinates c)
        {
            var limits = Chart.Plot.Axes.GetLimits();
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            if (Math.Abs(dx) < 1e-12) return AddScatterLine(c.X, limits.Bottom, c.X, limits.Top);
            return AddScatterLine(limits.Left, c.Y + dy / dx * (limits.Left - c.X), limits.Right, c.Y + dy / dx * (limits.Right - c.X));
        }

        private ScottPlot.Plottables.Scatter AddRayThroughPoints(ScottPlot.Coordinates start, ScottPlot.Coordinates through)
        {
            var limits = Chart.Plot.Axes.GetLimits();
            double dx = through.X - start.X;
            double dy = through.Y - start.Y;
            if (Math.Abs(dx) < 1e-12)
                return AddScatterLine(start.X, start.Y, start.X, dy >= 0 ? limits.Top : limits.Bottom);
            double endX = dx >= 0 ? limits.Right : limits.Left;
            double endY = start.Y + dy / dx * (endX - start.X);
            return AddScatterLine(start.X, start.Y, endX, endY);
        }

        private void AddRectangleToChart(RectangleDrawing d)
        {
            double left = Math.Min(d.A.X, d.B.X), right = Math.Max(d.A.X, d.B.X);
            double bottom = Math.Min(d.A.Y, d.B.Y), top = Math.Max(d.A.Y, d.B.Y);
            d.Lines.Add(AddScatterLine(left, bottom, right, bottom));
            d.Lines.Add(AddScatterLine(right, bottom, right, top));
            d.Lines.Add(AddScatterLine(right, top, left, top));
            d.Lines.Add(AddScatterLine(left, top, left, bottom));
        }

        private void AddParallelChannelToChart(ParallelChannelDrawing d)
        {
            d.BaseLine = AddLineThroughPoints(d.A, d.B);
            d.ParallelLine = AddParallelLineThroughPoint(d.A, d.B, d.C);
        }

        private static ScottPlot.Coordinates Midpoint(ScottPlot.Coordinates a, ScottPlot.Coordinates b) =>
            new((a.X + b.X) / 2.0, (a.Y + b.Y) / 2.0);

        private void AddPitchforkToChart(PitchforkDrawing d)
        {
            // Standard Andrews Pitchfork:
            // median = A -> midpoint(B,C); upper/lower tines are parallel through B/C.
            var target = Midpoint(d.B, d.C);
            d.MedianLine = AddRayThroughPoints(d.A, target);
            d.UpperLine = AddRayThroughPoints(d.B, target);
            d.LowerLine = AddRayThroughPoints(d.C, target);
        }
    }
}
