using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

using WpfButton = System.Windows.Controls.Button;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private sealed class TextDrawing
        {
            public string Text { get; init; } = string.Empty;
            public double X { get; set; }
            public double Y { get; set; }
            public ScottPlot.Plottables.Text? PlotText { get; set; }
        }

        private readonly List<TextDrawing> _textDrawings = new();
        private bool _textDrawingActive;
        private bool _textDrawingEventsAttached;

        private TextDrawing? _textSelection;
        private ScottPlot.Plottables.Marker? _textSelectionHandle;
        private bool _textSelectionDragging;

        private void InitializeTextDrawingHandling()
        {
            if (_textDrawingEventsAttached) return;
            _textDrawingEventsAttached = true;
            DrawingTextButton.Click += DrawingTextButton_Click;
            Chart.PreviewMouseLeftButtonDown += TextDrawing_MouseDown;
        }

        private void DrawingTextButton_Click(object? sender, RoutedEventArgs e)
        {
            RemoveTrendLinePreview();
            _trendLineStart = null;
            _activeDrawingTool = TechnicalDrawingTool.Select;
            _textDrawingActive = true;
            Chart.UserInputProcessor.IsEnabled = false;
            Chart.ReleaseMouseCapture();
            Chart.Focusable = true;
            Chart.Focus();
            UpdateTechnicalDrawingButtons();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | متن: محل درج متن را روی نمودار انتخاب کنید";
        }

        private void TextDrawing_MouseDown(object sender, WpfMouseButtonEventArgs e)
        {
            if (!_textDrawingActive || e.ChangedButton != MouseButton.Left) return;
            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates)) return;

            string? text = ShowTextInputDialog();
            if (string.IsNullOrWhiteSpace(text))
            {
                _textDrawingActive = false;
                Chart.UserInputProcessor.IsEnabled = true;
                UpdateTechnicalDrawingButtons();
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | متن لغو شد";
                e.Handled = true;
                return;
            }

            var drawing = new TextDrawing { Text = text.Trim(), X = coordinates.X, Y = coordinates.Y };
            _textDrawings.Add(drawing);
            AddTextToChart(drawing);
            _textDrawingActive = true;
            Chart.UserInputProcessor.IsEnabled = false;
            UpdateTechnicalDrawingButtons();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | متن درج شد؛ محل متن بعدی را کلیک کنید";
            Chart.Refresh();
            e.Handled = true;
        }

        private string? ShowTextInputDialog()
        {
            var window = new Window
            {
                Title = "درج متن روی نمودار",
                Width = 420,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                FlowDirection = System.Windows.FlowDirection.RightToLeft,
                Owner = Window.GetWindow(this)
            };

            var textBox = new WpfTextBox
            {
                Margin = new Thickness(12), Height = 55,
                VerticalContentAlignment = System.Windows.VerticalAlignment.Center,
                AcceptsReturn = true, TextWrapping = System.Windows.TextWrapping.Wrap,
                FlowDirection = System.Windows.FlowDirection.RightToLeft
            };

            var okButton = new WpfButton { Content = "تأیید", Width = 80, Height = 30, IsDefault = true, Margin = new Thickness(4, 0, 4, 10) };
            var cancelButton = new WpfButton { Content = "لغو", Width = 80, Height = 30, IsCancel = true, Margin = new Thickness(4, 0, 4, 10) };
            var buttons = new WpfStackPanel { Orientation = WpfOrientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
            buttons.Children.Add(cancelButton);
            buttons.Children.Add(okButton);
            var panel = new WpfStackPanel();
            panel.Children.Add(textBox);
            panel.Children.Add(buttons);
            window.Content = panel;
            okButton.Click += (_, _) => window.DialogResult = true;
            window.Loaded += (_, _) => { textBox.Focus(); Keyboard.Focus(textBox); };
            bool? result = window.ShowDialog();
            return result == true ? textBox.Text : null;
        }

        private void AddTextToChart(TextDrawing drawing)
        {
            var style = GetDrawingToolStyle("Text");
            var text = Chart.Plot.Add.Text(drawing.Text, drawing.X, drawing.Y);
            text.LabelFontSize = (float)style.FontSize;
            text.LabelFontName = style.FontFamily;
            text.LabelFontColor = ScottPlot.Color.FromHtml(style.Color);
            text.LabelBackgroundColor = ScottPlot.Color.FromHtml(style.BackgroundColor);
            text.LabelBorderColor = ScottPlot.Color.FromHtml(style.Color);
            text.LabelBorderWidth = 1;
            text.LabelPadding = 4;
            text.LabelAlignment = ScottPlot.Alignment.MiddleCenter;
            drawing.PlotText = text;
        }

        private void RenderTextDrawings()
        {
            foreach (TextDrawing drawing in _textDrawings)
            {
                if (drawing.PlotText != null) Chart.Plot.Remove(drawing.PlotText);
                AddTextToChart(drawing);
            }
        }

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
            double halfWidth = Math.Max(10.0, drawing.Text.Length * 4.2 + 4.0);
            double halfHeight = 14.0;
            return dx <= halfWidth && dy <= halfHeight;
        }

        private bool IsPointOnSelectedText(ScottPlot.Coordinates point)
            => _textSelection != null && IsPointOnTextDrawing(point, _textSelection);

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
