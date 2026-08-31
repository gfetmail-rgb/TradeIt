using System.Windows;

namespace TradeIt
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // =========================================================
            // Startup window-state code intentionally disabled.
            // The application must start in WPF's normal default state.
            // No Maximized or Full Screen state is forced here.
            // =========================================================
            /*
            var mainWindow = new MainWindow
            {
                WindowStyle = WindowStyle.SingleBorderWindow,
                ResizeMode = ResizeMode.CanResize,
                WindowState = WindowState.Maximized
            };

            MainWindow = mainWindow;
            mainWindow.Show();
            */

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}