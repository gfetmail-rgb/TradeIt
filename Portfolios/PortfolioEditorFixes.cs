using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TradeIt.Portfolios
{
    public partial class PortfolioEditorWindow
    {
        static PortfolioEditorWindow()
        {
            EventManager.RegisterClassHandler(
                typeof(PortfolioEditorWindow),
                Button.ClickEvent,
                new RoutedEventHandler(PortfolioEditor_ButtonClicked));
        }

        private static void PortfolioEditor_ButtonClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Name != "BrowseButton")
                return;

            if (Window.GetWindow(button) is not PortfolioEditorWindow window)
                return;

            // BrowseButton_Click sets PathTextBox after the file/folder dialog closes.
            // Queue preview loading so the column mapping combos are populated
            // immediately after the selected source has been assigned.
            window.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(() =>
                {
                    if (!string.IsNullOrWhiteSpace(window.PathTextBox.Text))
                        window.LoadPreviewButton_Click(button, new RoutedEventArgs());
                }));
        }
    }
}
