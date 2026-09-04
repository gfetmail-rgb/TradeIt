namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool IsPointOnSelectedDrawing(ScottPlot.Coordinates point)
        {
            if (_selectedDrawing == null) return false;
            double tolerance = GetDrawingHitTolerance();

            return _selectedDrawingKind switch
            {
                DrawingSelectionKind.Fibonacci => DistanceToFibonacci(point, (FibonacciDrawing)_selectedDrawing) <= tolerance,
                DrawingSelectionKind.Pitchfork => DistanceToPitchfork(point, (PitchforkDrawing)_selectedDrawing) <= tolerance,
                DrawingSelectionKind.ParallelChannel => DistancePointToSegment(point,
                    ((ParallelChannelDrawing)_selectedDrawing).A, ((ParallelChannelDrawing)_selectedDrawing).B) <= tolerance,
                DrawingSelectionKind.Rectangle => IsPointOnRectangle(point, (RectangleDrawing)_selectedDrawing, tolerance),
                DrawingSelectionKind.Ray => DistanceToRay(point,
                    ((RayDrawing)_selectedDrawing).X1, ((RayDrawing)_selectedDrawing).Y1,
                    ((RayDrawing)_selectedDrawing).X2, ((RayDrawing)_selectedDrawing).Y2) <= tolerance,
                DrawingSelectionKind.TrendLine => DistancePointToSegment(point,
                    new ScottPlot.Coordinates(((TrendLineDrawing)_selectedDrawing).X1, ((TrendLineDrawing)_selectedDrawing).Y1),
                    new ScottPlot.Coordinates(((TrendLineDrawing)_selectedDrawing).X2, ((TrendLineDrawing)_selectedDrawing).Y2)) <= tolerance,
                DrawingSelectionKind.HorizontalLine => Math.Abs(point.Y - ((HorizontalLineDrawing)_selectedDrawing).Y) <= tolerance,
                DrawingSelectionKind.VerticalLine => Math.Abs(point.X - ((VerticalLineDrawing)_selectedDrawing).X) <= tolerance,
                _ => false
            };
        }
    }
}
