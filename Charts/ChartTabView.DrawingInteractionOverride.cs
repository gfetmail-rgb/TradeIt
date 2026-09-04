using System;
using System.Collections.Generic;
using System.Windows;
using WpfButton = System.Windows.Controls.Button;
using System.Windows.Input;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using System.Windows.Threading;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private sealed class DrawingButtonVisualState
        {
            public double Opacity { get; init; }
            public WpfBrush? Background { get; init; }
            public WpfBrush? Foreground { get; init; }
            public WpfBrush? BorderBrush { get; init; }
            public Thickness BorderThickness { get; init; }
            public FontWeight FontWeight { get; init; }
        }

        private bool _drawingInteractionOverrideAttached;
        private readonly Dictionary<WpfButton, DrawingButtonVisualState> _drawingButtonOriginalVisuals = new();

        private static readonly bool _drawingInteractionOverrideRegistered = RegisterDrawingInteractionOverride();

        private static bool RegisterDrawingInteractionOverride()
        {
            EventManager.RegisterClassHandler(typeof(ChartTabView), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(DrawingInteractionOverride_Loaded));
            return true;
        }

        private static void DrawingInteractionOverride_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart) chart.AttachDrawingInteractionOverride();
        }

        private void AttachDrawingInteractionOverride()
        {
            if (_drawingInteractionOverrideAttached) return;
            _drawingInteractionOverrideAttached = true;

            CaptureDrawingButtonVisuals();
            InputManager.Current.PreProcessInput += DrawingInteractionOverride_PreProcessInput;

            DrawingSelectButton.Click += DrawingToolButtonVisual_Click;
            DrawingTrendLineButton.Click += DrawingToolButtonVisual_Click;
            DrawingHorizontalLineButton.Click += DrawingToolButtonVisual_Click;
            DrawingVerticalLineButton.Click += DrawingToolButtonVisual_Click;
            DrawingRayButton.Click += DrawingToolButtonVisual_Click;
            DrawingTextButton.Click += DrawingToolButtonVisual_Click;
            DrawingFibRetracementButton.Click += DrawingToolButtonVisual_Click;
            DrawingFibExtensionButton.Click += DrawingToolButtonVisual_Click;
            DrawingParallelChannelButton.Click += DrawingToolButtonVisual_Click;
            DrawingRectangleButton.Click += DrawingToolButtonVisual_Click;
            DrawingPitchforkButton.Click += DrawingToolButtonVisual_Click;

            SetAllDrawingButtonVisuals();
        }

        private void CaptureDrawingButtonVisuals()
        {
            WpfButton[] buttons =
            {
                DrawingSelectButton,
                DrawingTrendLineButton,
                DrawingHorizontalLineButton,
                DrawingVerticalLineButton,
                DrawingRayButton,
                DrawingTextButton,
                DrawingFibRetracementButton,
                DrawingFibExtensionButton,
                DrawingParallelChannelButton,
                DrawingRectangleButton,
                DrawingPitchforkButton
            };

            foreach (WpfButton button in buttons)
            {
                if (_drawingButtonOriginalVisuals.ContainsKey(button)) continue;
                _drawingButtonOriginalVisuals[button] = new DrawingButtonVisualState
                {
                    Opacity = button.Opacity,
                    Background = button.Background,
                    Foreground = button.Foreground,
                    BorderBrush = button.BorderBrush,
                    BorderThickness = button.BorderThickness,
                    FontWeight = button.FontWeight
                };
            }
        }

        private void DrawingToolButtonVisual_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(SetAllDrawingButtonVisuals), DispatcherPriority.Input);
        }

        private void DrawingInteractionOverride_PreProcessInput(object sender, PreProcessInputEventArgs e)
        {
            if (e.StagingItem.Input is System.Windows.Input.KeyEventArgs key)
            {
                if ((key.Key == Key.Escape || key.Key == Key.Cancel) &&
                    (_activeDrawingTool != TechnicalDrawingTool.Select || _textDrawingActive))
                {
                    Dispatcher.BeginInvoke(new Action(SetAllDrawingButtonVisuals), DispatcherPriority.Input);
                }
                return;
            }

            // Selection and editing are handled exclusively by DrawingSelection.cs
            // and DrawingSelectionBehaviorFix.cs. This class must never intercept a
            // chart mouse-down to move an entire drawing.
        }

        private void SetAllDrawingButtonVisuals()
        {
            RestoreAllDrawingButtonVisuals();

            SetDrawingButtonVisual(DrawingSelectButton, _activeDrawingTool == TechnicalDrawingTool.Select && !_textDrawingActive);
            SetDrawingButtonVisual(DrawingTrendLineButton, _activeDrawingTool == TechnicalDrawingTool.TrendLine);
            SetDrawingButtonVisual(DrawingHorizontalLineButton, _activeDrawingTool == TechnicalDrawingTool.HorizontalLine);
            SetDrawingButtonVisual(DrawingVerticalLineButton, _activeDrawingTool == TechnicalDrawingTool.VerticalLine);
            SetDrawingButtonVisual(DrawingRayButton, _activeDrawingTool == TechnicalDrawingTool.Ray);
            SetDrawingButtonVisual(DrawingTextButton, _textDrawingActive);
            SetDrawingButtonVisual(DrawingFibRetracementButton, (int)_activeDrawingTool == UnifiedFibRetracement);
            SetDrawingButtonVisual(DrawingFibExtensionButton, (int)_activeDrawingTool == UnifiedFibExtension);
            SetDrawingButtonVisual(DrawingParallelChannelButton, IsAdvancedDrawingTool && (int)_activeDrawingTool == 5);
            SetDrawingButtonVisual(DrawingRectangleButton, IsAdvancedDrawingTool && (int)_activeDrawingTool == 6);
            SetDrawingButtonVisual(DrawingPitchforkButton, IsAdvancedDrawingTool && (int)_activeDrawingTool == 7);
        }

        private void RestoreAllDrawingButtonVisuals()
        {
            foreach (var pair in _drawingButtonOriginalVisuals)
            {
                WpfButton button = pair.Key;
                DrawingButtonVisualState state = pair.Value;
                button.Opacity = state.Opacity;
                button.Background = state.Background;
                button.Foreground = state.Foreground;
                button.BorderBrush = state.BorderBrush;
                button.BorderThickness = state.BorderThickness;
                button.FontWeight = state.FontWeight;
            }
        }

        private static void SetDrawingButtonVisual(WpfButton button, bool active)
        {
            if (!active) return;

            button.Opacity = 1.0;
            button.Background = WpfBrushes.DodgerBlue;
            button.Foreground = WpfBrushes.White;
            button.BorderBrush = WpfBrushes.DodgerBlue;
            button.BorderThickness = new Thickness(2);
            button.FontWeight = FontWeights.Bold;
        }
    }
}
