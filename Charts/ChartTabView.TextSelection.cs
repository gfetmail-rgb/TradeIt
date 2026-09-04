using System;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private TextDrawing? _textSelection;
        private ScottPlot.Plottables.Marker? _textSelectionHandle;
        private bool _textSelectionDragging;

        private bool TrySelectTextDrawing(ScottPlot.Coordinates point)
        {
            for (int i = _textDrawings.Count - 1; i >= 0; i--)
            {
                if (!IsPointOnTextDrawing(point, _textDrawings[i])) continue;

                ClearDrawingSelection();
                ClearTextSelection();
                _textSelection = _textDrawings[i];
                RenderTextSelectionVisuals();
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | متن انتخاب شد؛ نقطه کنترل را جابه‌جا کنید | حذف: Delete";
                return true;
            }
            return false;
        }

        private bool IsPointOnTextDrawing(ScottPlot.Coordinates point, TextDrawing drawing)
        {
            ScottPlot.Pixel mousePixel = Chart.Plot.GetPixel(point);
            ScottPlot.Pixel textPixel = Chart.Plot.GetPixel(new ScottPlot.Coordinates(drawing.X, drawing.Y));
            double dx = Math.Abs(mousePixel.X - textPixel.X);
            double dy = Math.Abs(mousePixel.Y - textPixel.Y);

            // The text is centered on its anchor. Use a compact bounding box based on
            // the rendered font size and character count instead of a large radial hit area.
            double halfWidth = Math.Max(10.0, drawing.Text.Length * 4.2 + 4.0);
            double halfHeight = 14.0;
            return dx <= halfWidth && dy <= halfHeight;
        }

        private bool IsPointOnSelectedText(ScottPlot.Coordinates point)
        {
            return _textSelection != null && IsPointOnTextDrawing(point, _textSelection);
        }

        private double GetTextHitTolerance() => 10.0;

        private bool TryGetTextSelectionHandle(ScottPlot.Coordinates point, out bool hit)
        {
            hit = false;
            if (_textSelection == null) return false;
            hit = IsPointOnSelectedText(point);
            return hit;
        }

        private void BeginTextSelectionDrag(ScottPlot.Coordinates point)
        {
            if (_textSelection == null) return;
            _textSelectionDragging = true;
            Chart.CaptureMouse();
            Chart.UserInputProcessor.IsEnabled = false;
        }

        private bool MoveSelectedText(ScottPlot.Coordinates point)
        {
            if (_textSelection == null) return false;
            _textSelection.X = point.X;
            _textSelection.Y = point.Y;
            if (_textSelection.PlotText != null)
                Chart.Plot.Remove(_textSelection.PlotText);
            AddTextToChart(_textSelection);
            RenderTextSelectionVisuals();
            return true;
        }

        private void EndTextSelectionDrag()
        {
            _textSelectionDragging = false;
            Chart.ReleaseMouseCapture();
            Chart.UserInputProcessor.IsEnabled = true;
        }

        private void RenderTextSelectionVisuals()
        {
            ClearTextSelectionVisualsOnly();
            if (_textSelection == null) return;

            if (_textSelection.PlotText != null)
            {
                _textSelection.PlotText.LabelFontColor = ScottPlot.Color.FromHtml("#00BFFF");
                _textSelection.PlotText.LabelBorderColor = ScottPlot.Color.FromHtml("#00BFFF");
            }

            _textSelectionHandle = Chart.Plot.Add.Marker(_textSelection.X, _textSelection.Y, ScottPlot.MarkerShape.FilledCircle);
            _textSelectionHandle.MarkerSize = 12;
            _textSelectionHandle.MarkerFillColor = ScottPlot.Color.FromHtml("#00BFFF");
            _textSelectionHandle.MarkerLineColor = ScottPlot.Color.FromHtml("#FFFFFF");
            _textSelectionHandle.LineWidth = 1.5f;
        }

        private void ClearTextSelectionVisualsOnly()
        {
            if (_textSelectionHandle != null)
            {
                Chart.Plot.Remove(_textSelectionHandle);
                _textSelectionHandle = null;
            }
        }

        private void ClearTextSelection()
        {
            ClearTextSelectionVisualsOnly();
            if (_textSelection != null)
            {
                if (_textSelection.PlotText != null)
                    Chart.Plot.Remove(_textSelection.PlotText);
                AddTextToChart(_textSelection);
            }
            _textSelection = null;
            _textSelectionDragging = false;
            if (Chart.IsMouseCaptured) Chart.ReleaseMouseCapture();
        }

        private void DeleteSelectedText()
        {
            if (_textSelection == null) return;
            TextDrawing drawing = _textSelection;
            ClearTextSelectionVisualsOnly();
            if (drawing.PlotText != null) Chart.Plot.Remove(drawing.PlotText);
            _textDrawings.Remove(drawing);
            _textSelection = null;
            _textSelectionDragging = false;
            Chart.ReleaseMouseCapture();
            Chart.UserInputProcessor.IsEnabled = true;
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | متن حذف شد";
            Chart.Refresh();
        }

        private void ShowTextSelectionContextMenu()
        {
            var menu = new System.Windows.Controls.ContextMenu();
            var deleteItem = new System.Windows.Controls.MenuItem { Header = "حذف" };
            deleteItem.Click += (_, _) => DeleteSelectedText();
            menu.Items.Add(deleteItem);
            menu.IsOpen = true;
        }
    }
}
