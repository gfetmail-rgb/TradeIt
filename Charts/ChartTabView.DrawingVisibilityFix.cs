using System;
using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _drawingVisibilityFixAttached;
        private static readonly bool _drawingVisibilityFixRegistered = RegisterDrawingVisibilityFix();

        private static bool RegisterDrawingVisibilityFix()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DrawingVisibilityFix_Loaded));
            return true;
        }

        private static void DrawingVisibilityFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachDrawingVisibilityFix();
        }

        private void AttachDrawingVisibilityFix()
        {
            if (_drawingVisibilityFixAttached)
                return;

            _drawingVisibilityFixAttached = true;
            Chart.Plot.RenderManager.RenderStarting += DrawingVisibilityFix_RenderStarting;
        }

        private void DrawingVisibilityFix_RenderStarting(object? sender, ScottPlot.RenderPack e)
        {
            if (_allDrawingsVisible)
                return;

            SetAllPersistentDrawingPlottablesVisible(false);
        }

        private void SetAllPersistentDrawingPlottablesVisible(bool visible)
        {
            foreach (var drawing in _trendLines)
                if (drawing.PlotLine != null) drawing.PlotLine.IsVisible = visible;

            foreach (var drawing in _horizontalLines)
                if (drawing.PlotLine != null) drawing.PlotLine.IsVisible = visible;

            foreach (var drawing in _verticalLines)
                if (drawing.PlotLine != null) drawing.PlotLine.IsVisible = visible;

            foreach (var drawing in _rays)
                if (drawing.PlotLine != null) drawing.PlotLine.IsVisible = visible;

            foreach (var drawing in _parallelChannels)
            {
                if (drawing.BaseLine != null) drawing.BaseLine.IsVisible = visible;
                if (drawing.ParallelLine != null) drawing.ParallelLine.IsVisible = visible;
            }

            foreach (var drawing in _drawingRectangles)
                foreach (var line in drawing.Lines)
                    line.IsVisible = visible;

            foreach (var drawing in _pitchforks)
            {
                if (drawing.MedianLine != null) drawing.MedianLine.IsVisible = visible;
                if (drawing.UpperLine != null) drawing.UpperLine.IsVisible = visible;
                if (drawing.LowerLine != null) drawing.LowerLine.IsVisible = visible;
            }

            foreach (var drawing in _fibonacciDrawings)
            {
                foreach (var line in drawing.Lines)
                    line.IsVisible = visible;
                foreach (var label in drawing.Labels)
                    label.IsVisible = visible;
            }

            foreach (var drawing in _textDrawings)
                if (drawing.PlotText != null) drawing.PlotText.IsVisible = visible;

            foreach (var drawing in _arrowDrawings)
                if (drawing.PlotArrow != null) drawing.PlotArrow.IsVisible = visible && _arrowsVisible;

            foreach (var handle in _drawingSelectionHandles)
                handle.IsVisible = visible;
            foreach (var overlay in _drawingSelectionOverlays)
                overlay.IsVisible = visible;
            if (_textSelectionHandle != null)
                _textSelectionHandle.IsVisible = visible;
            if (_selectedArrowOverlay != null)
                _selectedArrowOverlay.IsVisible = visible;
            if (_selectedArrowHandle1 != null)
                _selectedArrowHandle1.IsVisible = visible;
            if (_selectedArrowHandle2 != null)
                _selectedArrowHandle2.IsVisible = visible;

            if (_trendLinePreview != null) _trendLinePreview.IsVisible = visible;
            if (_unifiedFibPreview != null) _unifiedFibPreview.IsVisible = visible;
            if (_advancedDrawingPreview1 != null) _advancedDrawingPreview1.IsVisible = visible;
            if (_advancedDrawingPreview2 != null) _advancedDrawingPreview2.IsVisible = visible;
            if (_horizontalRayPreview != null) _horizontalRayPreview.IsVisible = visible;
            if (_arrowPreview != null) _arrowPreview.IsVisible = visible;
        }
    }
}
