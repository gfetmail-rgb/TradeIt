using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private void AttachDrawingToolSettingsMenus()
        {
            AttachDrawingToolSettingsMenu(DrawingTrendLineButton, "TrendLine", "خط روند");
            AttachDrawingToolSettingsMenu(DrawingHorizontalLineButton, "HorizontalLine", "خط افقی");
            AttachDrawingToolSettingsMenu(DrawingVerticalLineButton, "VerticalLine", "خط عمودی");
            AttachDrawingToolSettingsMenu(DrawingRayButton, "HorizontalRay", "نیم‌خط افقی");
            AttachDrawingToolSettingsMenu(DrawingParallelChannelButton, "ParallelChannel", "کانال موازی");
            AttachDrawingToolSettingsMenu(DrawingRectangleButton, "Rectangle", "مستطیل");
            AttachDrawingToolSettingsMenu(DrawingPitchforkButton, "Pitchfork", "چنگال اندروز");
            AttachDrawingToolSettingsMenu(DrawingFibRetracementButton, "FibonacciRetracement", "فیبوناچی اصلاحی");
            AttachDrawingToolSettingsMenu(DrawingFibExtensionButton, "FibonacciExtension", "فیبوناچی اکستنشن");
            AttachDrawingToolSettingsMenu(DrawingTextButton, "Text", "متن");
        }

        private void AttachDrawingToolSettingsMenu(Button button, string key, string title)
        {
            button.PreviewMouseRightButtonDown += (_, e) =>
            {
                ShowDrawingToolSettings(key, title);
                e.Handled = true;
            };
        }

        private DrawingToolStyle GetDrawingToolStyle(string key)
        {
            if (!_settings.DrawingToolStyles.TryGetValue(key, out DrawingToolStyle? style))
            {
                style = ChartSettings.CreateDefaultDrawingToolStyles()[key];
                _settings.DrawingToolStyles[key] = style;
            }
            return style;
        }

        private static ScottPlot.LinePattern GetDrawingLinePattern(string style) => style switch
        {
            "Dash" => ScottPlot.LinePattern.Dashed,
            "Dot" => ScottPlot.LinePattern.Dotted,
            "DashDot" => ScottPlot.LinePattern.DashDot,
            _ => ScottPlot.LinePattern.Solid
        };

        private void ShowDrawingToolSettings(string key, string title)
        {
            DrawingToolStyle current = GetDrawingToolStyle(key).Clone();
            DrawingToolStyle defaults = ChartSettings.CreateDefaultDrawingToolStyles()[key];

            var window = new Window
            {
                Title = $"تنظیمات {title}",
                Width = 360,
                Height = 265,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                FlowDirection = FlowDirection.RightToLeft,
                Owner = Window.GetWindow(this)
            };

            var colorBox = new TextBox { Text = current.Color, Margin = new Thickness(6), Height = 30 };
            var widthBox = new TextBox { Text = current.LineWidth.ToString("0.##", CultureInfo.InvariantCulture), Margin = new Thickness(6), Height = 30 };
            var styleBox = new ComboBox { Margin = new Thickness(6), Height = 30 };
            styleBox.Items.Add("Solid");
            styleBox.Items.Add("Dash");
            styleBox.Items.Add("Dot");
            styleBox.Items.Add("DashDot");
            styleBox.SelectedItem = current.LineStyle;

            var form = new Grid { Margin = new Thickness(10) };
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            form.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddSettingRow(form, 0, "رنگ (HEX)", colorBox);
            AddSettingRow(form, 1, "ضخامت", widthBox);
            AddSettingRow(form, 2, "استایل", styleBox);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var defaultButton = new Button { Content = "پیش‌فرض", Width = 85, Height = 30, Margin = new Thickness(4) };
            var cancelButton = new Button { Content = "لغو", Width = 75, Height = 30, Margin = new Thickness(4), IsCancel = true };
            var applyButton = new Button { Content = "اعمال", Width = 75, Height = 30, Margin = new Thickness(4), IsDefault = true };
            buttons.Children.Add(defaultButton);
            buttons.Children.Add(cancelButton);
            buttons.Children.Add(applyButton);
            Grid.SetRow(buttons, 4);
            form.Children.Add(buttons);
            window.Content = form;

            defaultButton.Click += (_, _) =>
            {
                colorBox.Text = defaults.Color;
                widthBox.Text = defaults.LineWidth.ToString("0.##", CultureInfo.InvariantCulture);
                styleBox.SelectedItem = defaults.LineStyle;
            };

            applyButton.Click += (_, _) =>
            {
                string color = colorBox.Text.Trim();
                if (!color.StartsWith("#") || (color.Length != 7 && color.Length != 9))
                {
                    MessageBox.Show(window, "رنگ باید به صورت HEX مانند #1976D2 باشد.", "تنظیمات ابزار", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                double width;
                if (!double.TryParse(widthBox.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out width) || width < 0.5 || width > 10)
                {
                    MessageBox.Show(window, "ضخامت باید عددی بین 0.5 و 10 باشد.", "تنظیمات ابزار", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try { ScottPlot.Color.FromHtml(color); }
                catch
                {
                    MessageBox.Show(window, "کد رنگ معتبر نیست.", "تنظیمات ابزار", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DrawingToolStyle saved = GetDrawingToolStyle(key);
                saved.Color = color;
                saved.LineWidth = width;
                saved.LineStyle = styleBox.SelectedItem?.ToString() ?? "Solid";
                ChartSettingsManager.Save(_settings);
                ApplyDrawingToolStyle(key);
                window.DialogResult = true;
            };

            window.ShowDialog();
        }

        private static void AddSettingRow(Grid grid, int row, string label, Control control)
        {
            var panel = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 2, 0, 2) };
            var text = new TextBlock { Text = label, Width = 95, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4) };
            DockPanel.SetDock(text, Dock.Left);
            panel.Children.Add(text);
            panel.Children.Add(control);
            Grid.SetRow(panel, row);
            grid.Children.Add(panel);
        }

        private void ApplyDrawingToolStyle(string key)
        {
            DrawingToolStyle style = GetDrawingToolStyle(key);
            ScottPlot.Color color = ScottPlot.Color.FromHtml(style.Color);
            ScottPlot.LinePattern pattern = GetDrawingLinePattern(style.LineStyle);

            switch (key)
            {
                case "TrendLine":
                    foreach (var d in _trendLines) if (d.PlotLine != null) { d.PlotLine.LineColor = color; d.PlotLine.LineWidth = (float)style.LineWidth; d.PlotLine.LinePattern = pattern; }
                    break;
                case "HorizontalLine":
                    foreach (var d in _horizontalLines) if (d.PlotLine != null) { d.PlotLine.LineColor = color; d.PlotLine.LineWidth = (float)style.LineWidth; d.PlotLine.LinePattern = pattern; }
                    break;
                case "VerticalLine":
                    foreach (var d in _verticalLines) if (d.PlotLine != null) { d.PlotLine.LineColor = color; d.PlotLine.LineWidth = (float)style.LineWidth; d.PlotLine.LinePattern = pattern; }
                    break;
                case "HorizontalRay":
                    foreach (var d in _rays) if (d.PlotLine != null) { d.PlotLine.LineColor = color; d.PlotLine.LineWidth = (float)style.LineWidth; d.PlotLine.LinePattern = pattern; }
                    break;
                case "ParallelChannel":
                    foreach (var d in _parallelChannels)
                    {
                        if (d.BaseLine != null) { d.BaseLine.LineColor = color; d.BaseLine.LineWidth = (float)style.LineWidth; d.BaseLine.LinePattern = pattern; }
                        if (d.ParallelLine != null) { d.ParallelLine.LineColor = color; d.ParallelLine.LineWidth = (float)style.LineWidth; d.ParallelLine.LinePattern = pattern; }
                    }
                    break;
                case "Rectangle":
                    foreach (var d in _drawingRectangles) foreach (var line in d.Lines) { line.LineColor = color; line.LineWidth = (float)style.LineWidth; line.LinePattern = pattern; }
                    break;
                case "Pitchfork":
                    foreach (var d in _pitchforks)
                    {
                        if (d.MedianLine != null) { d.MedianLine.LineColor = color; d.MedianLine.LineWidth = (float)style.LineWidth; d.MedianLine.LinePattern = pattern; }
                        if (d.UpperLine != null) { d.UpperLine.LineColor = color; d.UpperLine.LineWidth = (float)style.LineWidth; d.UpperLine.LinePattern = pattern; }
                        if (d.LowerLine != null) { d.LowerLine.LineColor = color; d.LowerLine.LineWidth = (float)style.LineWidth; d.LowerLine.LinePattern = pattern; }
                    }
                    break;
                case "FibonacciRetracement":
                case "FibonacciExtension":
                    foreach (var d in _fibonacciDrawings)
                    {
                        if ((key == "FibonacciExtension") != d.IsExtension) continue;
                        foreach (var line in d.Lines) { line.LineColor = color; line.LineWidth = (float)style.LineWidth; line.LinePattern = pattern; }
                        foreach (var label in d.Labels) { label.LabelFontColor = color; label.LabelBorderColor = color; }
                    }
                    break;
                case "Text":
                    foreach (var d in _textDrawings) if (d.PlotText != null) { d.PlotText.LabelFontColor = color; d.PlotText.LabelBorderColor = color; }
                    break;
            }

            RenderDrawingSelectionOverlay();
            Chart.Refresh();
        }
    }
}
