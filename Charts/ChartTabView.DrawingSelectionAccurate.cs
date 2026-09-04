using System;
using System.Collections.Generic;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool TrySelectDrawingAccurate(ScottPlot.Coordinates point)
        {
            var candidates = GetAccurateDrawingCandidates(point);
            if (candidates.Count == 0) return false;

            double cycleTolerance = GetHandleHitTolerance() * 1.5;
            bool sameLocation = !double.IsNaN(_lastSelectionX) &&
                Math.Abs(point.X - _lastSelectionX) <= cycleTolerance &&
                Math.Abs(point.Y - _lastSelectionY) <= cycleTolerance &&
                _selectionCycleCount == candidates.Count;

            _selectionCycleIndex = sameLocation
                ? (_selectionCycleIndex + 1) % candidates.Count
                : 0;

            _lastSelectionX = point.X;
            _lastSelectionY = point.Y;
            _selectionCycleCount = candidates.Count;

            var candidate = candidates[_selectionCycleIndex];
            ClearTextSelection();
            SelectDrawing(candidate.Kind, candidate.Drawing);
            return true;
        }

        private List<DrawingCandidate> GetAccurateDrawingCandidates(ScottPlot.Coordinates point)
        {
            double tolerance = GetDrawingHitTolerance();
            var candidates = new List<DrawingCandidate>();

            // Newest first. Fibonacci levels are constrained to their actual
            // horizontal drawing span, so a level cannot select from the other
            // side of the chart.
            for (int i = _fibonacciDrawings.Count - 1; i >= 0; i--)
            {
                if (IsPointOnFibonacciDrawing(point, _fibonacciDrawings[i], tolerance))
                    candidates.Add(new DrawingCandidate { Kind = DrawingSelectionKind.Fibonacci, Drawing = _fibonacciDrawings[i] });
            }

            for (int i = _pitchforks.Count - 1; i >= 0; i--)
            {
                if (DistanceToPitchfork(point, _pitchforks[i]) <= tolerance)
                    candidates.Add(new DrawingCandidate { Kind = DrawingSelectionKind.Pitchfork, Drawing = _pitchforks[i] });
            }

            for (int i = _parallelChannels.Count - 1; i >= 0; i--)
            {
                var d = _parallelChannels[i];
                var parallelEnd = new ScottPlot.Coordinates(d.C.X + d.B.X - d.A.X, d.C.Y + d.B.Y - d.A.Y);
                if (DistancePointToSegment(point, d.A, d.B) <= tolerance ||
                    DistancePointToSegment(point, d.C, parallelEnd) <= tolerance)
                    candidates.Add(new DrawingCandidate { Kind = DrawingSelectionKind.ParallelChannel, Drawing = d });
            }

            for (int i = _drawingRectangles.Count - 1; i >= 0; i--)
            {
                var d = _drawingRectangles[i];
                if (IsPointOnRectangleAccurate(point, d, tolerance))
                    candidates.Add(new DrawingCandidate { Kind = DrawingSelectionKind.Rectangle, Drawing = d });
            }

            for (int i = _rays.Count - 1; i >= 0; i--)
            {
                var d = _rays[i];
                if (DistanceToRay(point, d.X1, d.Y1, d.X2, d.Y2) <= tolerance)
                    candidates.Add(new DrawingCandidate { Kind = DrawingSelectionKind.Ray, Drawing = d });
            }

            for (int i = _trendLines.Count - 1; i >= 0; i--)
            {
                var d = _trendLines[i];
                if (DistancePointToSegment(point,
                    new ScottPlot.Coordinates(d.X1, d.Y1),
                    new ScottPlot.Coordinates(d.X2, d.Y2)) <= tolerance)
                    candidates.Add(new DrawingCandidate { Kind = DrawingSelectionKind.TrendLine, Drawing = d });
            }

            for (int i = _horizontalLines.Count - 1; i >= 0; i--)
                if (Math.Abs(point.Y - _horizontalLines[i].Y) <= tolerance)
                    candidates.Add(new DrawingCandidate { Kind = DrawingSelectionKind.HorizontalLine, Drawing = _horizontalLines[i] });

            for (int i = _verticalLines.Count - 1; i >= 0; i--)
                if (Math.Abs(point.X - _verticalLines[i].X) <= tolerance)
                    candidates.Add(new DrawingCandidate { Kind = DrawingSelectionKind.VerticalLine, Drawing = _verticalLines[i] });

            return candidates;
        }

        private bool IsPointOnFibonacciDrawing(ScottPlot.Coordinates point, FibonacciDrawing drawing, double tolerance)
        {
            double ab = drawing.B.Y - drawing.A.Y;
            double left = drawing.IsExtension ? Math.Min(drawing.A.X, drawing.C.X) : Math.Min(drawing.A.X, drawing.B.X);
            double right = drawing.IsExtension ? Math.Max(drawing.A.X, drawing.C.X) : Math.Max(drawing.A.X, drawing.B.X);

            if (point.X < left - tolerance || point.X > right + tolerance)
                return false;

            double[] ratios = drawing.IsExtension
                ? new[] { 0.0, 0.382, 0.618, 1.0, 1.618, 2.618 }
                : new[] { 0.0, 0.236, 0.382, 0.5, 0.618, 0.786, 1.0 };

            foreach (double ratio in ratios)
            {
                double y = drawing.IsExtension
                    ? drawing.C.Y + ab * ratio
                    : drawing.B.Y - ab * ratio;
                if (Math.Abs(point.Y - y) <= tolerance)
                    return true;
            }
            return false;
        }

        private static bool IsPointOnRectangleAccurate(ScottPlot.Coordinates point, RectangleDrawing d, double tolerance)
        {
            double left = Math.Min(d.A.X, d.B.X);
            double right = Math.Max(d.A.X, d.B.X);
            double bottom = Math.Min(d.A.Y, d.B.Y);
            double top = Math.Max(d.A.Y, d.B.Y);

            // The rectangle is a selectable shape, not only four selectable lines.
            return point.X >= left - tolerance && point.X <= right + tolerance &&
                   point.Y >= bottom - tolerance && point.Y <= top + tolerance;
        }
    }
}
