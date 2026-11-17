using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace TOTVSHub
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args != null && args.Length > 0 && string.Equals(args[0], "--generate-ico", StringComparison.OrdinalIgnoreCase))
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TOTVSHub");
                if (!Directory.Exists(appData)) Directory.CreateDirectory(appData);
                string svgPath = TotvsLogo.EnsureTotvsWhiteSvgInAppData(appData);
                string icoOut = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "totvs_white.ico");
                TotvsLogo.WriteMultiSizeIcoFile(svgPath, icoOut);
                return;
            }

            try
            {
                EventWaitHandle existingSignal;
                if (EventWaitHandle.TryOpenExisting("Global\\TOTVSHub_Overwrite_Signal_2026", out existingSignal))
                {
                    existingSignal.Set();
                    existingSignal.Close();
                    Thread.Sleep(350);
                }
            }
            catch { }

            Application app = new Application();
            UiTheme.InstallCustomBlueScrollBar(app);
            MainWindow win = new MainWindow();
            app.Run(win);
        }
    }
}
