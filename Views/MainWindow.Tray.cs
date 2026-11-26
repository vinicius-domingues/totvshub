using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Interop;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        private class DarkTrayRenderer : System.Windows.Forms.ToolStripProfessionalRenderer
        {
            protected override void OnRenderMenuItemBackground(System.Windows.Forms.ToolStripItemRenderEventArgs e)
            {
                Rectangle rc = new Rectangle(System.Drawing.Point.Empty, e.Item.Size);
                Color bg = e.Item.Selected && e.Item.Enabled
                    ? Color.FromArgb(30, 41, 59)
                    : Color.FromArgb(15, 23, 42);
                using (SolidBrush b = new SolidBrush(bg))
                {
                    e.Graphics.FillRectangle(b, rc);
                }
            }

            protected override void OnRenderToolStripBackground(System.Windows.Forms.ToolStripRenderEventArgs e)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    e.Graphics.FillRectangle(b, e.AffectedBounds);
                }
            }

            protected override void OnRenderToolStripBorder(System.Windows.Forms.ToolStripRenderEventArgs e)
            {
                using (Pen p = new Pen(Color.FromArgb(51, 65, 85)))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
                }
            }

            protected override void OnRenderSeparator(System.Windows.Forms.ToolStripSeparatorRenderEventArgs e)
            {
                Rectangle rc = new Rectangle(System.Drawing.Point.Empty, e.Item.Size);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    e.Graphics.FillRectangle(b, rc);
                }
                int y = rc.Height / 2;
                using (Pen p = new Pen(Color.FromArgb(51, 65, 85)))
                {
                    e.Graphics.DrawLine(p, 8, y, rc.Width - 8, y);
                }
            }

            protected override void OnRenderImageMargin(System.Windows.Forms.ToolStripRenderEventArgs e)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(15, 23, 42)))
                {
                    e.Graphics.FillRectangle(b, e.AffectedBounds);
                }
            }

            protected override void OnRenderItemText(System.Windows.Forms.ToolStripItemTextRenderEventArgs e)
            {
                Color textColor = e.Item.Enabled
                    ? (e.Item.ForeColor != Color.Empty ? e.Item.ForeColor : Color.FromArgb(248, 250, 252))
                    : Color.FromArgb(100, 116, 139);

                System.Windows.Forms.TextRenderer.DrawText(
                    e.Graphics,
                    e.Text,
                    e.TextFont,
                    e.TextRectangle,
                    textColor,
                    e.TextFormat
                );
            }
        }

        private System.Threading.EventWaitHandle _bringToFrontEvent;
        private System.Threading.RegisteredWaitHandle _registeredWait;

        private void InicializarBandejaWindows()
        {
            RefreshSystemTrayArea();

            _trayMenu = new System.Windows.Forms.ContextMenuStrip();
            _trayMenu.BackColor = Color.FromArgb(15, 23, 42);
            _trayMenu.ShowImageMargin = false;
            _trayMenu.ShowCheckMargin = false;
            _trayMenu.Renderer = new DarkTrayRenderer();
            _trayMenu.ForeColor = Color.FromArgb(248, 250, 252);

            _trayIcon = new System.Windows.Forms.NotifyIcon();
            _trayIcon.Icon = TotvsLogo.CreateTotvsTrayIcon(_cachedSvgPath);
#if CORP
            _trayIcon.Text = "TOTVSHub Corp";
#else
            _trayIcon.Text = "TOTVSHub";
#endif
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += delegate { RestaurarJanelaPrincipal(); };

            _trayIcon.MouseUp += delegate(object s, System.Windows.Forms.MouseEventArgs e)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    RestaurarJanelaPrincipal();
                }
                else if (e.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    ReconstruirMenuBandeja();
                    var helper = new WindowInteropHelper(this);
                    if (helper.Handle != IntPtr.Zero)
                        NativeWin32.SetForegroundWindow(helper.Handle);
                    _trayMenu.Show(System.Windows.Forms.Cursor.Position);
                }
            };

            InicializarSinalInstanciaUnica();

            AppDomain.CurrentDomain.ProcessExit += delegate { DescartarBandeja(); };
            if (Application.Current != null)
            {
                Application.Current.Exit += delegate { DescartarBandeja(); };
            }
            Closed += delegate { DescartarBandeja(); };
        }

        private void InicializarSinalInstanciaUnica()
        {
            try
            {
                _bringToFrontEvent = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, Program.BringToFrontSignalName);
                _registeredWait = System.Threading.ThreadPool.RegisterWaitForSingleObject(_bringToFrontEvent, delegate(object state, bool timedOut)
                {
                    if (!timedOut)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            RestaurarJanelaPrincipal();
                        }));
                    }
                }, null, -1, false);
            }
            catch { }
        }

        public void DescartarBandeja()
        {
            try
            {
                if (_registeredWait != null)
                {
                    _registeredWait.Unregister(null);
                    _registeredWait = null;
                }
                if (_bringToFrontEvent != null)
                {
                    _bringToFrontEvent.Close();
                    _bringToFrontEvent = null;
                }
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }
            }
            catch { }
        }

        private static void SendMouseMoveAcrossWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;
            NativeWin32.RECT rect;
            if (NativeWin32.GetClientRect(hWnd, out rect))
            {
                for (int x = 1; x < rect.Right; x += 8)
                {
                    for (int y = 1; y < rect.Bottom; y += 8)
                    {
                        IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
                        NativeWin32.PostMessage(hWnd, 0x0200, IntPtr.Zero, lParam);
                    }
                }
            }
        }

        public static void RefreshSystemTrayArea()
        {
            try
            {
                IntPtr trayWnd = NativeWin32.FindWindow("Shell_TrayWnd", null);
                if (trayWnd != IntPtr.Zero)
                {
                    IntPtr trayNotifyWnd = NativeWin32.FindWindowEx(trayWnd, IntPtr.Zero, "TrayNotifyWnd", null);
                    if (trayNotifyWnd != IntPtr.Zero)
                    {
                        IntPtr sysPager = NativeWin32.FindWindowEx(trayNotifyWnd, IntPtr.Zero, "SysPager", null);
                        IntPtr toolbar = (sysPager != IntPtr.Zero)
                            ? NativeWin32.FindWindowEx(sysPager, IntPtr.Zero, "ToolbarWindow32", null)
                            : NativeWin32.FindWindowEx(trayNotifyWnd, IntPtr.Zero, "ToolbarWindow32", null);

                        if (toolbar != IntPtr.Zero)
                            SendMouseMoveAcrossWindow(toolbar);
                    }
                }

                IntPtr overflowWnd = NativeWin32.FindWindow("NotifyIconOverflowWindow", null);
                if (overflowWnd != IntPtr.Zero)
                {
                    IntPtr overflowToolbar = NativeWin32.FindWindowEx(overflowWnd, IntPtr.Zero, "ToolbarWindow32", null);
                    if (overflowToolbar != IntPtr.Zero)
                        SendMouseMoveAcrossWindow(overflowToolbar);
                }
            }
            catch { }
        }

        private void IniciarProtheusAmbiente(AmbienteInfo amb)
        {
            if (amb == null) return;
            if (amb.State == EnvRunState.Active || amb.State == EnvRunState.Initializing)
            {
                if (!string.IsNullOrEmpty(amb.WebAppPort))
                    AbrirNavegador(amb);
                return;
            }

            if (!string.IsNullOrEmpty(amb.WebAppPort))
            {
                string url = string.Format("http://localhost:{0}", amb.WebAppPort);
                int delay = _browserDelaySeconds;
                IniciarAmbienteAsync(amb, delegate { AbrirUrlNoNavegador(url, delay); });
            }
            else
            {
                IniciarAmbienteAsync(amb, null);
            }
        }

        private void ReconstruirMenuBandeja()
        {
            if (_trayMenu == null) return;
            _trayMenu.Items.Clear();

#if CORP
            var openItem = new System.Windows.Forms.ToolStripMenuItem("Abrir TOTVSHub Corp");
#else
            var openItem = new System.Windows.Forms.ToolStripMenuItem("Abrir TOTVSHub");
#endif
            openItem.Font = new System.Drawing.Font(_trayMenu.Font, System.Drawing.FontStyle.Bold);
            openItem.ForeColor = Color.White;
            openItem.Click += delegate { RestaurarJanelaPrincipal(); };
            _trayMenu.Items.Add(openItem);

            _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var listaAmbientes = _ambientes.Where(a => !_hiddenDirs.Contains(a.AppDir)).ToList();
            if (listaAmbientes.Count == 0 && _ambientes.Count > 0)
            {
                listaAmbientes = _ambientes.ToList();
            }

            listaAmbientes = listaAmbientes
                .OrderByDescending(a => a.State == EnvRunState.Active || a.State == EnvRunState.Initializing)
                .ThenBy(a => a.NomeIni)
                .ToList();

            if (listaAmbientes.Count > 0)
            {
                for (int i = 0; i < listaAmbientes.Count; i++)
                {
                    if (i > 0)
                    {
                        _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
                    }

                    AmbienteInfo captured = listaAmbientes[i];

                    if (captured.State == EnvRunState.Active)
                    {
                        string portDisplay = !string.IsNullOrEmpty(captured.WebAppPort)
                            ? " (:" + captured.WebAppPort + ")"
                            : (!string.IsNullOrEmpty(captured.RestPort) ? " (:" + captured.RestPort + ")" : "");

                        var envItem = new System.Windows.Forms.ToolStripMenuItem(string.Format("🟢 {0}{1}", captured.NomeIni, portDisplay));
                        envItem.Font = new System.Drawing.Font(_trayMenu.Font, System.Drawing.FontStyle.Bold);
                        envItem.ForeColor = Color.FromArgb(74, 222, 128);
                        envItem.ToolTipText = "Clique para abrir o Protheus";
                        envItem.Click += delegate
                        {
                            if (!string.IsNullOrEmpty(captured.WebAppPort))
                                AbrirNavegador(captured);
                            else if (!string.IsNullOrEmpty(captured.RestPort))
                                AbrirRestNavegador(captured);
                            else
                                RestaurarJanelaPrincipal();
                        };
                        _trayMenu.Items.Add(envItem);

                        if (!string.IsNullOrEmpty(captured.RestPort) && !string.IsNullOrEmpty(captured.WebAppPort))
                        {
                            var restItem = new System.Windows.Forms.ToolStripMenuItem(string.Format("    REST (:{0}/rest)", captured.RestPort));
                            restItem.ForeColor = Color.FromArgb(56, 189, 248);
                            restItem.Click += delegate { AbrirRestNavegador(captured); };
                            _trayMenu.Items.Add(restItem);
                        }

                        var stopItem = new System.Windows.Forms.ToolStripMenuItem(string.Format("    Parar {0}", captured.NomeIni));
                        stopItem.ForeColor = Color.FromArgb(248, 113, 113);
                        stopItem.Click += delegate { EncerrarAmbienteAsync(captured); };
                        _trayMenu.Items.Add(stopItem);
                    }
                    else if (captured.State == EnvRunState.Initializing)
                    {
                        var envItem = new System.Windows.Forms.ToolStripMenuItem(string.Format("🟡 {0} (Iniciando...)", captured.NomeIni));
                        envItem.Font = new System.Drawing.Font(_trayMenu.Font, System.Drawing.FontStyle.Italic);
                        envItem.ForeColor = Color.FromArgb(251, 191, 36);
                        envItem.Click += delegate { RestaurarJanelaPrincipal(); };
                        _trayMenu.Items.Add(envItem);
                    }
                    else if (captured.State == EnvRunState.Stopping)
                    {
                        var envItem = new System.Windows.Forms.ToolStripMenuItem(string.Format("🟠 {0} (Parando...)", captured.NomeIni));
                        envItem.Font = new System.Drawing.Font(_trayMenu.Font, System.Drawing.FontStyle.Italic);
                        envItem.ForeColor = Color.FromArgb(251, 146, 60);
                        envItem.Click += delegate { RestaurarJanelaPrincipal(); };
                        _trayMenu.Items.Add(envItem);
                    }
                    else // Stopped
                    {
                        var envItem = new System.Windows.Forms.ToolStripMenuItem(string.Format("⚪ {0}", captured.NomeIni));
                        envItem.Font = new System.Drawing.Font(_trayMenu.Font, System.Drawing.FontStyle.Regular);
                        envItem.ForeColor = Color.FromArgb(148, 163, 184);
                        envItem.ToolTipText = "Clique para iniciar o Protheus";
                        envItem.Click += delegate
                        {
                            IniciarProtheusAmbiente(captured);
                        };
                        _trayMenu.Items.Add(envItem);

                        string startLabel = !string.IsNullOrEmpty(captured.WebAppPort)
                            ? "    Iniciar Protheus"
                            : string.Format("    Iniciar {0}", captured.NomeIni);
                        var startItem = new System.Windows.Forms.ToolStripMenuItem(startLabel);
                        startItem.ForeColor = Color.FromArgb(74, 222, 128);
                        startItem.Click += delegate
                        {
                            IniciarProtheusAmbiente(captured);
                        };
                        _trayMenu.Items.Add(startItem);
                    }
                }
            }
            else
            {
                var semAmbientes = new System.Windows.Forms.ToolStripMenuItem("Nenhum ambiente encontrado");
                semAmbientes.ForeColor = Color.FromArgb(148, 163, 184);
                semAmbientes.Enabled = false;
                _trayMenu.Items.Add(semAmbientes);
            }

            _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var checkUpdateItem = new System.Windows.Forms.ToolStripMenuItem("Verificar atualizações");
            checkUpdateItem.ForeColor = Color.FromArgb(56, 189, 248);
            checkUpdateItem.Click += delegate { AbrirDriveAtualizacoes(); };
            _trayMenu.Items.Add(checkUpdateItem);

            _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var aboutItem = new System.Windows.Forms.ToolStripMenuItem("Sobre (" + UiTheme.AppVersion + ")");
            aboutItem.ForeColor = Color.FromArgb(148, 163, 184);
            aboutItem.Click += delegate
            {
                RestaurarJanelaPrincipal();
                ShowSettingsView();
            };
            _trayMenu.Items.Add(aboutItem);

            var exitItem = new System.Windows.Forms.ToolStripMenuItem("Encerrar");
            exitItem.ForeColor = Color.FromArgb(248, 113, 113);
            exitItem.Click += delegate
            {
                _isRealExit = true;
                DescartarBandeja();
                Application.Current.Shutdown();
            };
            _trayMenu.Items.Add(exitItem);
        }

        private void AbrirDriveAtualizacoes()
        {
            try
            {
                string url = string.IsNullOrWhiteSpace(_updateDriveUrl) ? UiTheme.DefaultUpdateDriveUrl : _updateDriveUrl.Trim();
                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    url = "https://" + url;
                }

                Uri uriResult;
                if (Uri.TryCreate(url, UriKind.Absolute, out uriResult) &&
                    (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = uriResult.AbsoluteUri;
                    psi.UseShellExecute = true;
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                StyledDialog.Info(this, "Erro ao Abrir Drive", "Não foi possível abrir o link de atualizações.", ex.Message);
            }
        }

        private void RestaurarJanelaPrincipal()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(RestaurarJanelaPrincipal));
                return;
            }

            try
            {
                if (!IsVisible)
                {
                    Show();
                }
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }

                Activate();
                Topmost = true;
                Topmost = false;
                Focus();

                var helper = new WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    NativeWin32.ShowWindow(helper.Handle, 9);
                    NativeWin32.SetForegroundWindow(helper.Handle);
                }
            }
            catch { }
        }
    }
}
