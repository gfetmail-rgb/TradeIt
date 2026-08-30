using System;
using System.Windows;
using System.Windows.Controls;

namespace TradeIt
{
    public partial class MainWindow
    {
        private bool _fullScreenInitialized;

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            EnterFullScreen();
        }

        private void FullScreenExitButton_Click(object sender, RoutedEventArgs e)
        {
            ExitFullScreen();
        }

        private void EnterFullScreen()
        {
            if (_isFullScreen)
                return;

            _previousWindowState = WindowState;
            _previousWindowStyle = WindowStyle;
            _previousResizeMode = ResizeMode;

            _isFullScreen = true;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;

            FullScreenButton.Visibility = Visibility.Collapsed;
            FullScreenExitButton.Visibility = Visibility.Visible;
        }

        private void ExitFullScreen()
        {
            if (!_isFullScreen)
                return;

            _isFullScreen = false;
            FullScreenExitButton.Visibility = Visibility.Collapsed;
            FullScreenButton.Visibility = Visibility.Visible;

            WindowState = _previousWindowState;
            WindowStyle = _previousWindowStyle;
            ResizeMode = _previousResizeMode;
        }
    }
}
