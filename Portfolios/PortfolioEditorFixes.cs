using System;
using System.IO;
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
                System.Windows.Controls.Button.ClickEvent,
                new RoutedEventHandler(PortfolioEditor_ButtonClicked));

            EventManager.RegisterClassHandler(
                typeof(PortfolioEditorWindow),
                System.Windows.Controls.TextBox.TextChangedEvent,
                new TextChangedEventHandler(PortfolioEditor_PathChanged));
        }

        private static void PortfolioEditor_ButtonClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button || button.Name != "BrowseButton")
                return;

            if (Window.GetWindow(button) is not PortfolioEditorWindow window)
                return;

            QueuePreview(window);
        }

        private static void PortfolioEditor_PathChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not System.Windows.Controls.TextBox textBox || textBox.Name != "PathTextBox")
                return;

            if (Window.GetWindow(textBox) is not PortfolioEditorWindow window)
                return;

            QueuePreview(window);
        }

        private static void QueuePreview(PortfolioEditorWindow window)
        {
            window.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(() =>
                {
                    string path = window.PathTextBox.Text.Trim();

                    if (string.IsNullOrWhiteSpace(path))
                        return;

                    if (!File.Exists(path) && !Directory.Exists(path))
                        return;

                    window.LoadPreviewButton_Click(
                        window,
                        new RoutedEventArgs());
                }));
        }
    }
}
