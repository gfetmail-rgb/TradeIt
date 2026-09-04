using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _allDrawingsVisible = true;

        private void HideChartButton_Click(object sender, RoutedEventArgs e)
        {
            _chartVisible = !_chartVisible;

            foreach (var plottable in Chart.Plot.GetPlottables())
            {
                if (ReferenceEquals(plottable, _crosshair))
                    continue;

                plottable.IsVisible = _chartVisible;
            }

            if (_crosshair != null)
                _crosshair.IsVisible = _chartVisible && _crosshairVisible && (_crosshairMouseInside || !_hasInitialView);

            HideChartButton.Content = _chartVisible ? "پنهان کردن نمودار" : "نمایش نمودار";
            Chart.Refresh();
        }

        private void HideToolsButton_Click(object sender, RoutedEventArgs e)
        {
            _toolsVisible = !_toolsVisible;
            TechnicalDrawingToolbarHost.Visibility = _toolsVisible
                ? Visibility.Visible
                : Visibility.Collapsed;

            HideToolsButton.Content = _toolsVisible
                ? "پنهان کردن ابزارهای تکنیکال"
                : "نمایش ابزارهای تکنیکال";
        }

        private void HideAllDrawingsButton_Click(object sender, RoutedEventArgs e)
        {
            _allDrawingsVisible = !_allDrawingsVisible;

            foreach (var drawing in _trendLines)
                if (drawing.PlotLine != null) drawing.PlotLine.IsVisible = _allDrawingsVisible;

            foreach (var drawing in _horizontalLines)
                if (drawing.PlotLine != null) drawing.PlotLine.IsVisible = _allDrawingsVisible;

            foreach (var drawing in _verticalLines)
                if (drawing.PlotLine != null) drawing.PlotLine.IsVisible = _allDrawingsVisible;

            foreach (var drawing in _rays)
                if (drawing.PlotLine != null) drawing.PlotLine.IsVisible = _allDrawingsVisible;

            foreach (var drawing in _parallelChannels)
            {
                if (drawing.BaseLine != null) drawing.BaseLine.IsVisible = _allDrawingsVisible;
                if (drawing.ParallelLine != null) drawing.ParallelLine.IsVisible = _allDrawingsVisible;
            }

            foreach (var drawing in _drawingRectangles)
                foreach (var line in drawing.Lines)
                    line.IsVisible = _allDrawingsVisible;

            foreach (var drawing in _pitchforks)
            {
                if (drawing.MedianLine != null) drawing.MedianLine.IsVisible = _allDrawingsVisible;
                if (drawing.UpperLine != null) drawing.UpperLine.IsVisible = _allDrawingsVisible;
                if (drawing.LowerLine != null) drawing.LowerLine.IsVisible = _allDrawingsVisible;
            }

            foreach (var drawing in _fibonacciDrawings)
            {
                foreach (var line in drawing.Lines)
                    line.IsVisible = _allDrawingsVisible;
                foreach (var label in drawing.Labels)
                    label.IsVisible = _allDrawingsVisible;
            }

            foreach (var drawing in _textDrawings)
                if (drawing.PlotText != null) drawing.PlotText.IsVisible = _allDrawingsVisible;

            foreach (var handle in _drawingSelectionHandles)
                handle.IsVisible = _allDrawingsVisible;

            foreach (var overlay in _drawingSelectionOverlays)
                overlay.IsVisible = _allDrawingsVisible;

            if (_textSelectionHandle != null)
                _textSelectionHandle.IsVisible = _allDrawingsVisible;

            HideAllDrawingsButton.Content = _allDrawingsVisible ? "پنهان کردن همه" : "نمایش همه";
            Chart.Refresh();
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomXAxis(0.80);
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomXAxis(1.25);
        }
    }
}
