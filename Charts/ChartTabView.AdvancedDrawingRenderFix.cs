using System;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _advancedDrawingRenderFixAttached;

        private void AttachAdvancedDrawingRenderFix()
        {
            if (_advancedDrawingRenderFixAttached)
                return;

            _advancedDrawingRenderFixAttached = true;
            Chart.Plot.RenderManager.RenderStarting += AdvancedDrawingRenderFix_RenderStarting;
        }

        private void AdvancedDrawingRenderFix_RenderStarting(object? sender, ScottPlot.RenderPack e)
        {
            if (!_allDrawingsVisible)
                return;

            RenderAdvancedDrawingsAfterChartRebuild();
        }

        private void RenderAdvancedDrawingsAfterChartRebuild()
        {
            foreach (var drawing in _parallelChannels)
            {
                if (drawing.BaseLine != null) Chart.Plot.Remove(drawing.BaseLine);
                if (drawing.ParallelLine != null) Chart.Plot.Remove(drawing.ParallelLine);
                drawing.BaseLine = null;
                drawing.ParallelLine = null;
                AddParallelChannelToChart(drawing);
            }

            foreach (var drawing in _drawingRectangles)
            {
                foreach (var line in drawing.Lines)
                    Chart.Plot.Remove(line);
                drawing.Lines.Clear();
                AddRectangleToChart(drawing);
            }

            foreach (var drawing in _pitchforks)
            {
                if (drawing.MedianLine != null) Chart.Plot.Remove(drawing.MedianLine);
                if (drawing.UpperLine != null) Chart.Plot.Remove(drawing.UpperLine);
                if (drawing.LowerLine != null) Chart.Plot.Remove(drawing.LowerLine);
                drawing.MedianLine = null;
                drawing.UpperLine = null;
                drawing.LowerLine = null;
                AddPitchforkToChart(drawing);
            }
        }
    }
}
