using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace TOTVSHub
{
    public static class Program
    {
        public const string SingleInstanceMutexName = "Global\\TOTVSHub_SingleInstance_Mutex_2026";
        public const string BringToFrontSignalName = "Global\\TOTVSHub_Show_Signal_2026";

        [STAThread]
        public static void Main(string[] args)
        {
            // Força renderização por software para compatibilidade total com sessões RDP e evitar UCEERR_RENDERTHREADFAILURE (0x88980406)
            try
            {
                System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
            }
            catch { }

            if (args != null && args.Length > 0 && string.Equals(args[0], "--generate-ico", StringComparison.OrdinalIgnoreCase))
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TOTVSHub");
                if (!Directory.Exists(appData)) Directory.CreateDirectory(appData);
                string svgPath = TotvsLogo.EnsureTotvsWhiteSvgInAppData(appData);
                string icoOut = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "totvs_white.ico");
                TotvsLogo.WriteMultiSizeIcoFile(svgPath, icoOut);
                return;
            }

            bool isFirstInstance = false;
            Mutex singleInstanceMutex = null;
            try
            {
                singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out isFirstInstance);
            }
            catch
            {
                isFirstInstance = true;
            }

            if (!isFirstInstance)
            {

                try
                {
                    EventWaitHandle showSignal;
                    if (EventWaitHandle.TryOpenExisting(BringToFrontSignalName, out showSignal))
                    {
                        showSignal.Set();
                        showSignal.Close();
                    }
                    else
                    {
                        IntPtr hWnd = NativeWin32.FindWindow(null, "TOTVSHub");
                        if (hWnd == IntPtr.Zero)
                            hWnd = NativeWin32.FindWindow(null, "TOTVSHub Corp");
                        if (hWnd != IntPtr.Zero)
                        {
                            NativeWin32.ShowWindow(hWnd, 9);
                            NativeWin32.SetForegroundWindow(hWnd);
                        }
                    }
                }
                catch { }

                return;
            }

            try
            {
                Application app = new Application();
                app.DispatcherUnhandledException += delegate(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
                {
                    System.Runtime.InteropServices.COMException comEx = e.Exception as System.Runtime.InteropServices.COMException;
                    if (comEx != null && (uint)comEx.ErrorCode == 0x88980406)
                    {
                        e.Handled = true;
                    }
                };
                UiTheme.InstallCustomBlueScrollBar(app);
                MainWindow win = new MainWindow();
                app.Run(win);
            }
            finally
            {
                if (singleInstanceMutex != null)
                {
                    try { singleInstanceMutex.ReleaseMutex(); } catch { }
                    singleInstanceMutex.Close();
                }
            }
        }
    }
}
