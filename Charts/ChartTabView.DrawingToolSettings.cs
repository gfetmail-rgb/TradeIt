using System;
using System.Collections.Generic;
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
            // Left-click is handled by the drawing-tool handlers themselves.
            // Settings are opened only with right-click so the tool activation cannot be disturbed.
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
            style.FibonacciLevels ??= new Dictionary<string, bool>();
            Dictionary<string, bool> defaults = key == "FibonacciRetracement"
                ? ChartSettings.CreateDefaultRetracementLevels()
                : key == "FibonacciExtension"
                    ? ChartSettings.CreateDefaultExtensionLevels()
                    : new Dictionary<string, bool>();
            foreach (var pair in defaults)
                if (!style.FibonacciLevels.ContainsKey(pair.Key)) style.FibonacciLevels[pair.Key] = pair.Value;

            if (key == "FibonacciRetracement")
            {
                style.FibonacciLevels.TryAdd("127.2", true);
                style.FibonacciLevels.TryAdd("161.8", true);
                style.FibonacciLevels.TryAdd("200.0", true);
            }
            else if (key == "FibonacciExtension")
            {
                style.FibonacciLevels.TryAdd("200.0", true);
            }
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
                Width = 400,
                Height = isFib ? 540 : isText ? 280 : 285,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                FlowDirection = System.Windows.FlowDirection.RightToLeft,
                Owner = Window.GetWindow(this)
            };

            var form = new WpfGrid { Margin = new Thickness(10) };
            int row = 0;
            WpfButton? backgroundButton = null;
            WpfButton? textColorButton = null;
            WpfButton? fontButton = null;

            if (isText)
            {
                form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                backgroundButton = CreateColorButton(current.BackgroundColor);
                AddSettingRow(form, row++, "رنگ زمینه", backgroundButton);

                form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                fontButton = new WpfButton
                {
                    Content = $"{current.FontFamily}  /  {current.FontSize:0}",
                    Width = 190, Height = 30, Margin = new Thickness(6)
                };
                AddSettingRow(form, row++, "فونت", fontButton);

                fontButton.Click += (_, _) =>
                {
                    using var dialog = new System.Windows.Forms.FontDialog
                    {
                        Font = CreateWinFormsFont(current.FontFamily, current.FontSize),
                        ShowColor = false
                    };
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        current.FontFamily = dialog.Font.FontFamily.Name;
                        current.FontSize = dialog.Font.Size;
                        fontButton.Content = $"{current.FontFamily}  /  {current.FontSize:0}";
                    }
                };

                form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                textColorButton = CreateColorButton(current.Color);
                AddSettingRow(form, row++, "رنگ متن", textColorButton);
            }
            else
            {
                form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var colorButton = CreateColorButton(current.Color);
                AddSettingRow(form, row++, "رنگ", colorButton);

                form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var widthBox = CreateWidthCombo(current.LineWidth);
                AddSettingRow(form, row++, "ضخامت", widthBox);

                form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var styleBox = CreateStyleCombo(current.LineStyle);
                AddSettingRow(form, row++, "استایل", styleBox);

                var levelChecks = new Dictionary<string, System.Windows.Controls.CheckBox>();
                if (isFib)
                {
                    form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    var levelPanel = new WpfStackPanel { Margin = new Thickness(4, 8, 4, 4) };
                    levelPanel.Children.Add(new WpfTextBlock { Text = "سطوح فیبوناچی رایج", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
                    string[] levels = key == "FibonacciRetracement"
                        ? new[] { "0.0", "23.6", "38.2", "50.0", "61.8", "78.6", "100.0", "127.2", "161.8", "200.0" }
                        : new[] { "0.0", "38.2", "61.8", "100.0", "127.2", "161.8", "200.0", "261.8" };
                    foreach (string level in levels)
                    {
                        bool checkedValue = current.FibonacciLevels.TryGetValue(level, out bool value) ? value : true;
                        var check = new System.Windows.Controls.CheckBox
                        {
                            Content = level switch
                            {
                                "127.2" => "1.272 (127.2%)",
                                "161.8" => "1.618 (161.8%)",
                                "200.0" => "2 (200%)",
                                "261.8" => "2.618 (261.8%)",
                                _ => $"{level}%"
                            },
                            IsChecked = checkedValue,
                            Margin = new Thickness(2)
                        };
                        levelChecks[level] = check;
                        levelPanel.Children.Add(check);
                    }
                    Grid.SetRow(levelPanel, row++);
                    form.Children.Add(levelPanel);
                }

                form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var buttons = CreateDialogButtons();
                Grid.SetRow(buttons.panel, row++);
                form.Children.Add(buttons.panel);
                window.Content = form;

                colorButton.Click += (_, _) =>
                {
                    using var dialog = new WinFormsColorDialog { FullOpen = true, Color = System.Drawing.ColorTranslator.FromHtml(current.Color) };
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        SetColorButton(colorButton, System.Drawing.ColorTranslator.ToHtml(dialog.Color));
                };

                buttons.defaultButton.Click += (_, _) =>
                {
                    SetColorButton(colorButton, defaults.Color);
                    widthBox.SelectedItem = FindNumericComboItem(widthBox, defaults.LineWidth);
                    styleBox.SelectedItem = defaults.LineStyle;
                    foreach (var pair in levelChecks)
                        pair.Value.IsChecked = defaults.FibonacciLevels.TryGetValue(pair.Key, out bool value) ? value : true;
                };

                buttons.applyButton.Click += (_, _) =>
                {
                    string color = GetColorButtonValue(colorButton);
                    DrawingToolStyle saved = GetDrawingToolStyle(key);
                    saved.Color = color;
                    if (widthBox.SelectedItem is double width) saved.LineWidth = width;
                    saved.LineStyle = styleBox.SelectedItem?.ToString() ?? "Solid";
                    if (isFib)
                    {
                        saved.FibonacciLevels = new Dictionary<string, bool>();
                        foreach (var pair in levelChecks) saved.FibonacciLevels[pair.Key] = pair.Value.IsChecked == true;
                    }
                    ChartSettingsManager.SaveDrawingToolStyles(_settings);
                    ApplyDrawingToolStyle(key);
                    window.DialogResult = true;
                };
                window.ShowDialog();
                return;
            }

            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var textButtons = CreateDialogButtons();
            Grid.SetRow(textButtons.panel, row++);
            form.Children.Add(textButtons.panel);
            window.Content = form;

            backgroundButton!.Click += (_, _) =>
            {
                using var dialog = new WinFormsColorDialog { FullOpen = true, Color = System.Drawing.ColorTranslator.FromHtml(current.BackgroundColor) };
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    SetColorButton(backgroundButton, System.Drawing.ColorTranslator.ToHtml(dialog.Color));
            };

            textColorButton!.Click += (_, _) =>
            {
                using var dialog = new WinFormsColorDialog { FullOpen = true, Color = System.Drawing.ColorTranslator.FromHtml(current.Color) };
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    SetColorButton(textColorButton, System.Drawing.ColorTranslator.ToHtml(dialog.Color));
            };

            textButtons.defaultButton.Click += (_, _) =>
            {
                SetColorButton(backgroundButton, defaults.BackgroundColor);
                SetColorButton(textColorButton, defaults.Color);
                current.FontFamily = defaults.FontFamily;
                current.FontSize = defaults.FontSize;
                fontButton!.Content = $"{current.FontFamily}  /  {current.FontSize:0}";
            };

            textButtons.applyButton.Click += (_, _) =>
            {
                DrawingToolStyle saved = GetDrawingToolStyle("Text");
                saved.BackgroundColor = GetColorButtonValue(backgroundButton);
                saved.Color = GetColorButtonValue(textColorButton);
                saved.FontFamily = current.FontFamily;
                saved.FontSize = current.FontSize;
                ChartSettingsManager.SaveDrawingToolStyles(_settings);
                ApplyDrawingToolStyle("Text");
                RenderTextDrawings();
                Chart.Refresh();
                window.DialogResult = true;
            };

            window.ShowDialog();
        }

        private static (WpfStackPanel panel, WpfButton defaultButton, WpfButton cancelButton, WpfButton applyButton) CreateDialogButtons()
        {
            var panel = new WpfStackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(4)
            };
            var defaultButton = new WpfButton { Content = "پیش‌فرض", Width = 85, Height = 30, Margin = new Thickness(4) };
            var cancelButton = new WpfButton { Content = "لغو", Width = 75, Height = 30, Margin = new Thickness(4), IsCancel = true };
            var applyButton = new WpfButton { Content = "اعمال", Width = 75, Height = 30, Margin = new Thickness(4), IsDefault = true };
            panel.Children.Add(defaultButton);
            panel.Children.Add(cancelButton);
            panel.Children.Add(applyButton);
            return (panel, defaultButton, cancelButton, applyButton);
        }

        private static System.Drawing.Font CreateWinFormsFont(string family, double size)
        {
            try { return new System.Drawing.Font(family, (float)Math.Max(1, size)); }
            catch { return new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont.FontFamily, (float)Math.Max(1, size)); }
        }

        private static WpfButton CreateColorButton(string color)
        {
            var button = new WpfButton
            {
                Width = 150, Height = 30,
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch,
                Margin = new Thickness(6)
            };
            SetColorButton(button, color);
            return button;
        }

        private static void SetColorButton(WpfButton button, string color)
        {
            button.Tag = color;
            button.Content = new Border
            {
                Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color)),
                BorderBrush = System.Windows.Media.Brushes.Gray,
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

        private static object? FindNumericComboItem(WpfComboBox combo, double value)
        {
            foreach (object item in combo.Items)
                if (item is double number && Math.Abs(number - value) < 0.0001) return item;
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
                case "TrendLine": foreach (var d in _trendLines) if (d.PlotLine != null) { d.PlotLine.LineColor = color; d.PlotLine.LineWidth = (float)style.LineWidth; d.PlotLine.LinePattern = pattern; } break;
                case "HorizontalLine": foreach (var d in _horizontalLines) if (d.PlotLine != null) { d.PlotLine.LineColor = color; d.PlotLine.LineWidth = (float)style.LineWidth; d.PlotLine.LinePattern = pattern; } break;
                case "VerticalLine": foreach (var d in _verticalLines) if (d.PlotLine != null) { d.PlotLine.LineColor = color; d.PlotLine.LineWidth = (float)style.LineWidth; d.PlotLine.LinePattern = pattern; } break;
                case "HorizontalRay": foreach (var d in _rays) if (d.PlotLine != null) { d.PlotLine.LineColor = color; d.PlotLine.LineWidth = (float)style.LineWidth; d.PlotLine.LinePattern = pattern; } break;
                case "ParallelChannel": foreach (var d in _parallelChannels) { if (d.BaseLine != null) { d.BaseLine.LineColor = color; d.BaseLine.LineWidth = (float)style.LineWidth; d.BaseLine.LinePattern = pattern; } if (d.ParallelLine != null) { d.ParallelLine.LineColor = color; d.ParallelLine.LineWidth = (float)style.LineWidth; d.ParallelLine.LinePattern = pattern; } } break;
                case "Rectangle": foreach (var d in _drawingRectangles) foreach (var line in d.Lines) { line.LineColor = color; line.LineWidth = (float)style.LineWidth; line.LinePattern = pattern; } break;
                case "Pitchfork": foreach (var d in _pitchforks) { if (d.MedianLine != null) { d.MedianLine.LineColor = color; d.MedianLine.LineWidth = (float)style.LineWidth; d.MedianLine.LinePattern = pattern; } if (d.UpperLine != null) { d.UpperLine.LineColor = color; d.UpperLine.LineWidth = (float)style.LineWidth; d.UpperLine.LinePattern = pattern; } if (d.LowerLine != null) { d.LowerLine.LineColor = color; d.LowerLine.LineWidth = (float)style.LineWidth; d.LowerLine.LinePattern = pattern; } } break;
            }
            Chart.Refresh();
        }
    }
}
