using System.Windows;

namespace TradeIt.Charts
{
    public partial class ChartTabView
    {
        private bool _mouseWheelFinalFixAttached;
        private static readonly bool _mouseWheelFinalFixRegistered = RegisterMouseWheelFinalFix();

        private static bool RegisterMouseWheelFinalFix()
        {
            EventManager.RegisterClassHandler(
                typeof(ChartTabView),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(MouseWheelFinalFix_Loaded));
            return true;
        }

        private static void MouseWheelFinalFix_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ChartTabView chart)
                chart.AttachMouseWheelFinalFix();
        }

        private void AttachMouseWheelFinalFix()
        {
            if (_mouseWheelFinalFixAttached)
                return;

            _mouseWheelFinalFixAttached = true;
        }
    }
}
