using System.Windows;

namespace TradeIt
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Startup must always be the normal application window.
            // Chart fullscreen is never entered automatically.
            var mainWindow = new MainWindow
            {
                WindowStyle = WindowStyle.SingleBorderWindow,
                ResizeMode = ResizeMode.CanResize,
                WindowState = WindowState.Maximized
            };

            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}