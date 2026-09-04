using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfMouseButtonEventHandler = System.Windows.Input.MouseButtonEventHandler;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private sealed class TextDrawing
        {
            public string Text { get; init; } = string.Empty;
            public double X { get; init; }
            public double Y { get; init; }
            public ScottPlot.Plottables.Text? PlotText { get; set; }
        }

        private readonly List<TextDrawing> _textDrawings = new();
        private bool _textDrawingActive;
        private bool _textDrawingEventsAttached;

        private static readonly bool _textDrawingRegistered = RegisterTextDrawingHandling();

        private static bool RegisterTextDrawingHandling()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(TextDrawing_Loaded));
            return true;
        }

        private static void TextDrawing_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ChartTabView chart || chart._textDrawingEventsAttached)
                return;

            chart._textDrawingEventsAttached = true;
            chart.DrawingTextButton.Click += chart.DrawingTextButton_Click;

            chart.Chart.AddHandler(
                UIElement.PreviewMouseLeftButtonDownEvent,
                new WpfMouseButtonEventHandler(chart.TextDrawing_MouseDown),
                true);
        }

        private void DrawingTextButton_Click(object sender, RoutedEventArgs e)
        {
            _textDrawingActive = true;
            Chart.UserInputProcessor.IsEnabled = false;
            Chart.Focus();
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | متن: محل درج متن را روی نمودار انتخاب کنید";
        }

        private void TextDrawing_MouseDown(object sender, WpfMouseButtonEventArgs e)
        {
            if (!_textDrawingActive || e.ChangedButton != MouseButton.Left)
                return;

            if (!TryGetChartCoordinates(Chart, e.GetPosition(Chart), out ScottPlot.Coordinates coordinates))
                return;

            string? text = ShowTextInputDialog();
            if (string.IsNullOrWhiteSpace(text))
            {
                ChartInfoTextBlock.Text = $"{_symbol.Symbol} | متن لغو شد";
                e.Handled = true;
                return;
            }

            var drawing = new TextDrawing
            {
                Text = text.Trim(),
                X = coordinates.X,
                Y = coordinates.Y
            };

            _textDrawings.Add(drawing);
            AddTextToChart(drawing);
            ChartInfoTextBlock.Text = $"{_symbol.Symbol} | متن درج شد";
            Chart.Refresh();
            e.Handled = true;
        }

        private string? ShowTextInputDialog()
        {
            var window = new Window
            {
                Title = "درج متن روی نمودار",
                Width = 420,
                Height = 170,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                FlowDirection = FlowDirection.RightToLeft,
                Owner = Window.GetWindow(this)
            };

            var textBox = new TextBox
            {
                Margin = new Thickness(12),
                Height = 45,
                VerticalContentAlignment = VerticalAlignment.Center,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                FlowDirection = FlowDirection.RightToLeft
            };

            var okButton = new Button
            {
                Content = "تأیید",
                Width = 80,
                Height = 30,
                IsDefault = true,
                Margin = new Thickness(0, 0, 8, 10)
            };

            var cancelButton = new Button
            {
                Content = "لغو",
                Width = 80,
                Height = 30,
                IsCancel = true,
                Margin = new Thickness(8, 0, 0, 10)
            };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            buttons.Children.Add(cancelButton);
            buttons.Children.Add(okButton);

            var panel = new StackPanel();
            panel.Children.Add(textBox);
            panel.Children.Add(buttons);
            window.Content = panel;

            okButton.Click += (_, _) => window.DialogResult = true;
            window.Loaded += (_, _) =>
            {
                textBox.Focus();
                Keyboard.Focus(textBox);
            };

            bool? result = window.ShowDialog();
            return result == true ? textBox.Text : null;
        }

        private void AddTextToChart(TextDrawing drawing)
        {
            var text = Chart.Plot.Add.Text(drawing.Text, drawing.X, drawing.Y);
            text.LabelFontSize = 14;
            text.LabelFontColor = ScottPlot.Color.FromHtml(_settings.LineColor);
            text.LabelBackgroundColor = ScottPlot.Colors.White.WithAlpha(0.85);
            text.LabelBorderColor = ScottPlot.Color.FromHtml(_settings.LineColor);
            text.LabelBorderWidth = 1;
            text.LabelPadding = 4;
            text.LabelAlignment = ScottPlot.Alignment.MiddleCenter;
            drawing.PlotText = text;
        }

        private void RenderTextDrawings()
        {
            foreach (TextDrawing drawing in _textDrawings)
            {
                if (drawing.PlotText != null)
                    Chart.Plot.Remove(drawing.PlotText);

                AddTextToChart(drawing);
            }
        }
    }
}
