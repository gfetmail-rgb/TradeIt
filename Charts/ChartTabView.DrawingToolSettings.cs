using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfControl = System.Windows.Controls.Control;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfGrid = System.Windows.Controls.Grid;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WinFormsColorDialog = System.Windows.Forms.ColorDialog;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _drawingToolSettingsRegistered = RegisterDrawingToolSettingsHandling();

        private static bool RegisterDrawingToolSettingsHandling()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DrawingToolSettings_Loaded));
            return true;
        }

        private static void DrawingToolSettings_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart) chart.AttachDrawingToolSettingsMenus();
        }

        private bool _drawingToolSettingsMenusAttached;

        private void AttachDrawingToolSettingsMenus()
        {
            if (_drawingToolSettingsMenusAttached) return;
            _drawingToolSettingsMenusAttached = true;
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

        private void AttachDrawingToolSettingsMenu(WpfButton button, string key, string title)
        {
            button.Click += (_, _) => ActivateDrawingToolStyle(key);
            button.AddHandler(UIElement.PreviewMouseRightButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler((_, e) =>
                {
                    ShowDrawingToolSettings(key, title);
                    e.Handled = true;
                }), true);
        }

        private void ActivateDrawingToolStyle(string key)
        {
            DrawingToolStyle style = GetDrawingToolStyle(key);
            _settings.LineColor = style.Color;
            _settings.LineWidth = style.LineWidth;
        }

        private DrawingToolStyle GetDrawingToolStyle(string key)
        {
            if (!_settings.DrawingToolStyles.TryGetValue(key, out DrawingToolStyle? style))
            {
                style = ChartSettings.CreateDefaultDrawingToolStyles()[key];
                _settings.DrawingToolStyles[key] = style;
            }
            EnsureFibonacciLevelDefaults(key, style);
            return style;
        }

        private static void EnsureFibonacciLevelDefaults(string key, DrawingToolStyle style)
        {
            if (key == "FibonacciRetracement" && style.FibonacciLevels.Count == 0)
                style.FibonacciLevels = ChartSettings.CreateDefaultRetracementLevels();
            else if (key == "FibonacciExtension" && style.FibonacciLevels.Count == 0)
                style.FibonacciLevels = ChartSettings.CreateDefaultExtensionLevels();
        }

        private static ScottPlot.LinePattern GetDrawingLinePattern(string style) => style switch
        {
            "Dash" => ScottPlot.LinePattern.Dashed,
            "Dot" => ScottPlot.LinePattern.Dotted,
            "DashDot" => ScottPlot.LinePattern.Dashed,
            _ => ScottPlot.LinePattern.Solid
        };

        private void ShowDrawingToolSettings(string key, string title)
        {
            DrawingToolStyle current = GetDrawingToolStyle(key).Clone();
            DrawingToolStyle defaults = ChartSettings.CreateDefaultDrawingToolStyles()[key];
            bool isText = key == "Text";
            bool isFib = key == "FibonacciRetracement" || key == "FibonacciExtension";

            var window = new Window
            {
                Title = $"تنظیمات {title}",
                Width = 390,
                Height = isFib ? 510 : isText ? 300 : 285,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                FlowDirection = FlowDirection.RightToLeft,
                Owner = Window.GetWindow(this)
            };

            var form = new WpfGrid { Margin = new Thickness(10) };
            int row = 0;
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var colorButton = CreateColorButton(current.Color);
            AddSettingRow(form, row++, "رنگ", colorButton);

            WpfComboBox? widthBox = null;
            WpfComboBox? styleBox = null;
            WpfComboBox? fontBox = null;
            WpfComboBox? fontSizeBox = null;

            if (!isText)
            {
                widthBox = CreateWidthCombo(current.LineWidth);
                AddSettingRow(form, row++, "ضخامت", widthBox);
                styleBox = CreateStyleCombo(current.LineStyle);
                AddSettingRow(form, row++, "استایل", styleBox);
            }
            else
            {
                fontBox = CreateFontCombo(current.FontFamily);
                AddSettingRow(form, row++, "فونت", fontBox);
                fontSizeBox = CreateFontSizeCombo(current.FontSize);
                AddSettingRow(form, row++, "سایز", fontSizeBox);
            }

            var levelChecks = new Dictionary<string, System.Windows.Controls.CheckBox>();
            if (isFib)
            {
                var levelPanel = new WpfStackPanel { Margin = new Thickness(4, 8, 4, 4) };
                levelPanel.Children.Add(new WpfTextBlock { Text = "سطوح فیبوناچی رایج", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
                string[] levels = key == "FibonacciRetracement"
                    ? new[] { "0.0", "23.6", "38.2", "50.0", "61.8", "78.6", "100.0" }
                    : new[] { "0.0", "38.2", "61.8", "100.0", "127.2", "161.8", "261.8" };
                foreach (string level in levels)
                {
                    bool isChecked = current.FibonacciLevels.TryGetValue(level, out bool value) ? value : true;
                    var check = new System.Windows.Controls.CheckBox { Content = $"{level}%", IsChecked = isChecked, Margin = new Thickness(2) };
                    levelChecks[level] = check;
                    levelPanel.Children.Add(check);
                }
                Grid.SetRow(levelPanel, row++);
                form.Children.Add(levelPanel);
            }

            var buttons = new WpfStackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(4)
            };
            var defaultButton = new WpfButton { Content = "پیش‌فرض", Width = 85, Height = 30, Margin = new Thickness(4) };
            var cancelButton = new WpfButton { Content = "لغو", Width = 75, Height = 30, Margin = new Thickness(4), IsCancel = true };
            var applyButton = new WpfButton { Content = "اعمال", Width = 75, Height = 30, Margin = new Thickness(4), IsDefault = true };
            buttons.Children.Add(defaultButton);
            buttons.Children.Add(cancelButton);
            buttons.Children.Add(applyButton);
            Grid.SetRow(buttons, row++);
            form.Children.Add(buttons);
            window.Content = form;

            colorButton.Click += (_, _) =>
            {
                using var dialog = new WinFormsColorDialog { FullOpen = true, Color = System.Drawing.ColorTranslator.FromHtml(current.Color) };
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    SetColorButton(colorButton, System.Drawing.ColorTranslator.ToHtml(dialog.Color));
            };

            defaultButton.Click += (_, _) =>
            {
                SetColorButton(colorButton, defaults.Color);
                if (!isText)
                {
                    widthBox!.SelectedItem = FindNumericComboItem(widthBox, defaults.LineWidth);
                    styleBox!.SelectedItem = defaults.LineStyle;
                }
                else
                {
                    fontBox!.SelectedItem = defaults.FontFamily;
                    fontSizeBox!.SelectedItem = FindNumericComboItem(fontSizeBox, defaults.FontSize);
                }
                foreach (var pair in levelChecks)
                    pair.Value.IsChecked = defaults.FibonacciLevels.TryGetValue(pair.Key, out bool value) ? value : true;
            };

            applyButton.Click += (_, _) =>
            {
                string color = GetColorButtonValue(colorButton);
                try { ScottPlot.Color.FromHtml(color); }
                catch
                {
                    System.Windows.MessageBox.Show(window, "رنگ انتخاب‌شده معتبر نیست.", "تنظیمات ابزار", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DrawingToolStyle saved = GetDrawingToolStyle(key);
                saved.Color = color;

                if (!isText)
                {
                    if (widthBox?.SelectedItem is not double width) return;
                    saved.LineWidth = width;
                    saved.LineStyle = styleBox?.SelectedItem?.ToString() ?? "Solid";
                }
                else
                {
                    saved.FontFamily = fontBox?.SelectedItem?.ToString() ?? "Segoe UI";
                    if (fontSizeBox?.SelectedItem is double fontSize)
                        saved.FontSize = fontSize;
                }

                if (isFib)
                {
                    saved.FibonacciLevels = new Dictionary<string, bool>();
                    foreach (var pair in levelChecks)
                        saved.FibonacciLevels[pair.Key] = pair.Value.IsChecked == true;
                }

                ChartSettingsManager.SaveDrawingToolStyles(_settings);
                ActivateDrawingToolStyle(key);
                ApplyDrawingToolStyle(key);
                window.DialogResult = true;
            };

            window.ShowDialog();
        }

        private static WpfButton CreateColorButton(string color)
        {
            var button = new WpfButton { Width = 150, Height = 30, HorizontalContentAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(6) };
            SetColorButton(button, color);
            return button;
        }

        private static void SetColorButton(WpfButton button, string color)
        {
            button.Tag = color;
            button.Content = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Height = 20
            };
        }

        private static string GetColorButtonValue(WpfButton button) => button.Tag?.ToString() ?? "#1976D2";

        private static WpfComboBox CreateWidthCombo(double value)
        {
            var combo = new WpfComboBox { Width = 150, Height = 30, Margin = new Thickness(6) };
            double[] values = { 0.5, 1.0, 1.25, 1.5, 2.0, 2.5, 3.0, 4.0, 5.0 };
            foreach (double item in values) combo.Items.Add(item);
            combo.SelectedItem = FindNumericComboItem(combo, value) ?? 1.5;
            return combo;
        }

        private static WpfComboBox CreateStyleCombo(string value)
        {
            var combo = new WpfComboBox { Width = 150, Height = 30, Margin = new Thickness(6) };
            combo.Items.Add("Solid"); combo.Items.Add("Dash"); combo.Items.Add("Dot"); combo.Items.Add("DashDot");
            combo.SelectedItem = value;
            if (combo.SelectedIndex < 0) combo.SelectedIndex = 0;
            return combo;
        }

        private static WpfComboBox CreateFontCombo(string value)
        {
            var combo = new WpfComboBox { Width = 150, Height = 30, Margin = new Thickness(6), IsEditable = false };
            foreach (FontFamily family in Fonts.SystemFontFamilies)
                combo.Items.Add(family.Source);
            combo.SelectedItem = value;
            if (combo.SelectedIndex < 0) combo.SelectedItem = "Segoe UI";
            return combo;
        }

        private static WpfComboBox CreateFontSizeCombo(double value)
        {
            var combo = new WpfComboBox { Width = 150, Height = 30, Margin = new Thickness(6) };
            for (int size = 8; size <= 32; size++) combo.Items.Add((double)size);
            combo.SelectedItem = FindNumericComboItem(combo, value) ?? 14.0;
            return combo;
        }

        private static object? FindNumericComboItem(WpfComboBox combo, double value)
        {
            foreach (object item in combo.Items)
                if (item is double number && Math.Abs(number - value) < 0.0001)
                    return item;
            return null;
        }

        private static void AddSettingRow(WpfGrid grid, int row, string label, WpfControl control)
        {
            var panel = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 2, 0, 2) };
            var text = new WpfTextBlock { Text = label, Width = 95, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4) };
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
                    foreach (var d in _parallelChannels) { if (d.BaseLine != null) { d.BaseLine.LineColor = color; d.BaseLine.LineWidth = (float)style.LineWidth; d.BaseLine.LinePattern = pattern; } if (d.ParallelLine != null) { d.ParallelLine.LineColor = color; d.ParallelLine.LineWidth = (float)style.LineWidth; d.ParallelLine.LinePattern = pattern; } }
                    break;
                case "Rectangle":
                    foreach (var d in _drawingRectangles) foreach (var line in d.Lines) { line.LineColor = color; line.LineWidth = (float)style.LineWidth; line.LinePattern = pattern; }
                    break;
                case "Pitchfork":
                    foreach (var d in _pitchforks) { if (d.MedianLine != null) { d.MedianLine.LineColor = color; d.MedianLine.LineWidth = (float)style.LineWidth; d.MedianLine.LinePattern = pattern; } if (d.UpperLine != null) { d.UpperLine.LineColor = color; d.UpperLine.LineWidth = (float)style.LineWidth; d.UpperLine.LinePattern = pattern; } if (d.LowerLine != null) { d.LowerLine.LineColor = color; d.LowerLine.LineWidth = (float)style.LineWidth; d.LowerLine.LinePattern = pattern; } }
                    break;
                case "FibonacciRetracement":
                case "FibonacciExtension":
                    foreach (var d in _fibonacciDrawings)
                    {
                        if ((key == "FibonacciExtension") != d.IsExtension) continue;
                        foreach (var line in d.Lines) { line.LineColor = color; line.LineWidth = (float)style.LineWidth; line.LinePattern = pattern; }
                        foreach (var label in d.Labels) { label.LabelFontColor = color; label.LabelBorderColor = color; }
                    }
                    RenderAllFibonacciDrawings();
                    break;
                case "Text":
                    foreach (var d in _textDrawings) if (d.PlotText != null)
                    {
                        d.PlotText.LabelFontColor = color;
                        d.PlotText.LabelBorderColor = color;
                        d.PlotText.LabelFontSize = (float)style.FontSize;
                        d.PlotText.LabelFontName = style.FontFamily;
                    }
                    break;
            }

            RenderDrawingSelectionOverlay();
            Chart.Refresh();
        }
    }
}
