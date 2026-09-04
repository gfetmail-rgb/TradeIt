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
            text.LabelBackgroundColor = ScottPlot.Colors.White.WithAlpha(0.85);
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
    }
}
