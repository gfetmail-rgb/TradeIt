using System.Windows;
using System.Windows.Controls;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private static readonly bool _drawingToolSettingsApplyCloseRegistered = RegisterDrawingToolSettingsApplyClose();

        private static bool RegisterDrawingToolSettingsApplyClose()
        {
            EventManager.RegisterClassHandler(
                typeof(Button),
                Button.ClickEvent,
                new RoutedEventHandler(DrawingToolSettingsApplyClose_Click),
                true);
            return true;
        }

        private static void DrawingToolSettingsApplyClose_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button ||
                !string.Equals(button.Content?.ToString(), "اعمال", System.StringComparison.Ordinal))
                return;

            Window? window = Window.GetWindow(button);
            if (window?.Title.StartsWith("تنظیمات ", System.StringComparison.Ordinal) == true)
                window.DialogResult = true;
        }
    }
}
