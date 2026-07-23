using System;
using System.Linq;
using System.Windows;

namespace CtrlAltStand
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Headless self-test path — never shows a window; returns an exit code.
            if (e.Args.Any(a => string.Equals(a, "--self-test", StringComparison.OrdinalIgnoreCase)))
            {
                int code = SelfTests.Run() ? 0 : 1;
                Shutdown(code);
                return;
            }

            base.OnStartup(e);

            bool smoke = e.Args.Any(a => string.Equals(a, "--smoke-test", StringComparison.OrdinalIgnoreCase));
            MainWindow window = new MainWindow(smoke);
            window.Show();
        }
    }
}
