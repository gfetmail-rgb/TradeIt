using WpfApplication = System.Windows.Application;
using WpfStartupEventArgs = System.Windows.StartupEventArgs;

namespace TradeIt
{
    public partial class App : WpfApplication
    {
        protected override void OnStartup(WpfStartupEventArgs e)
        {
            base.OnStartup(e);
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}
