using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        // =====================================================================
        // BANDEJA DO WINDOWS (SYSTEM TRAY)
        // =====================================================================
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
        }

        private System.Threading.EventWaitHandle _bringToFrontEvent;
        private System.Threading.RegisteredWaitHandle _registeredWait;

        private void InicializarBandejaWindows()
        {
            // Limpa icones fantasmas da bandeja do Windows antes de registrar o novo icone
            RefreshSystemTrayArea();

            _trayMenu = new System.Windows.Forms.ContextMenuStrip();
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
                    if (_trayMenu.Handle != IntPtr.Zero)
                        NativeWin32.SetForegroundWindow(_trayMenu.Handle);
                    _trayMenu.Show(System.Windows.Forms.Cursor.Position);
                }
            };

            // Escuta chamadas de nova instancia para restaurar janela sem duplicar na bandeja
            InicializarSinalInstanciaUnica();

            // Garante o descarte completo do icone da bandeja em qualquer encerramento do processo
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

        // Remove icones orfaos de processos encerrados passando mensagem de mouse sobre a barra de notificacao
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
                        NativeWin32.PostMessage(hWnd, 0x0200 /* WM_MOUSEMOVE */, IntPtr.Zero, lParam);
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

            // Facilitador: Ambientes Ativos com atalhos de Protheus, REST e Parar
            var basesAtivas = _ambientes.Where(a => a.State == EnvRunState.Active).ToList();
            if (basesAtivas.Count > 0)
            {
                var headerAtivos = new System.Windows.Forms.ToolStripMenuItem(string.Format("🟢 Ambientes Ativos ({0})", basesAtivas.Count));
                headerAtivos.ForeColor = Color.FromArgb(74, 222, 128);
                headerAtivos.Font = new System.Drawing.Font(_trayMenu.Font, System.Drawing.FontStyle.Bold);

                foreach (AmbienteInfo amb in basesAtivas)
                {
                    AmbienteInfo captured = amb;
                    string execMode = captured.RunningAsService ? "Serviço" : "Console";
                    string titleAmb = captured.Pid > 0
                        ? string.Format("{0} ({1} • PID {2})", captured.NomeIni, execMode, captured.Pid)
                        : string.Format("{0} ({1})", captured.NomeIni, execMode);

                    var envItem = new System.Windows.Forms.ToolStripMenuItem(titleAmb);
                    envItem.ForeColor = Color.White;

                    if (!string.IsNullOrEmpty(captured.WebAppPort))
                    {
                        var subWeb = new System.Windows.Forms.ToolStripMenuItem(string.Format("🌐 Protheus (:{0})", captured.WebAppPort));
                        subWeb.ForeColor = Color.FromArgb(56, 189, 248);
                        subWeb.Click += delegate { AbrirNavegador(captured); };
                        envItem.DropDownItems.Add(subWeb);
                    }

                    if (!string.IsNullOrEmpty(captured.RestPort))
                    {
                        var subRest = new System.Windows.Forms.ToolStripMenuItem(string.Format("🌐 REST (:{0}/rest)", captured.RestPort));
                        subRest.ForeColor = Color.FromArgb(56, 189, 248);
                        subRest.Click += delegate { AbrirRestNavegador(captured); };
                        envItem.DropDownItems.Add(subRest);
                    }

                    var cfgSub = new System.Windows.Forms.ToolStripMenuItem("⚙️ Configurar (.ini)");
                    cfgSub.ForeColor = Color.FromArgb(226, 232, 240);
                    cfgSub.Click += delegate
                    {
                        RestaurarJanelaPrincipal();
                        ShowIniEditorView(captured);
                    };
                    envItem.DropDownItems.Add(cfgSub);

                    var stopSub = new System.Windows.Forms.ToolStripMenuItem("⏹️ Parar Ambiente");
                    stopSub.ForeColor = Color.FromArgb(248, 113, 113);
                    stopSub.Click += delegate { EncerrarAmbienteAsync(captured); };
                    envItem.DropDownItems.Add(stopSub);

                    headerAtivos.DropDownItems.Add(envItem);
                }

                _trayMenu.Items.Add(headerAtivos);
                _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            }
            else
            {
                var semAtivos = new System.Windows.Forms.ToolStripMenuItem("⚪ Nenhum ambiente ativo no momento");
                semAtivos.ForeColor = Color.FromArgb(148, 163, 184);
                semAtivos.Enabled = false;
                _trayMenu.Items.Add(semAtivos);
                _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            }

            // Submenu com todas as bases registradas
            var protheusMenu = new System.Windows.Forms.ToolStripMenuItem("Todos os Ambientes");
            protheusMenu.ForeColor = Color.White;
            var basesComPorta = _ambientes
                .Where(a => !_hiddenDirs.Contains(a.AppDir) && (!string.IsNullOrEmpty(a.WebAppPort) || !string.IsNullOrEmpty(a.RestPort)))
                .ToList();

            if (basesComPorta.Count == 0)
            {
                var vazio = new System.Windows.Forms.ToolStripMenuItem("Nenhuma base configurada");
                vazio.Enabled = false;
                protheusMenu.DropDownItems.Add(vazio);
            }
            else
            {
                foreach (AmbienteInfo amb in basesComPorta)
                {
                    AmbienteInfo captured = amb;
                    if (!string.IsNullOrEmpty(captured.WebAppPort))
                    {
                        var subWeb = new System.Windows.Forms.ToolStripMenuItem(string.Format("{0} — Protheus (:{1})", captured.NomeIni, captured.WebAppPort));
                        subWeb.ForeColor = Color.White;
                        subWeb.Click += delegate { AbrirNavegador(captured); };
                        protheusMenu.DropDownItems.Add(subWeb);
                    }
                    if (!string.IsNullOrEmpty(captured.RestPort))
                    {
                        var subRest = new System.Windows.Forms.ToolStripMenuItem(string.Format("{0} — REST (:{1}/rest)", captured.NomeIni, captured.RestPort));
                        subRest.ForeColor = Color.White;
                        subRest.Click += delegate { AbrirRestNavegador(captured); };
                        protheusMenu.DropDownItems.Add(subRest);
                    }
                }
            }
            _trayMenu.Items.Add(protheusMenu);

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
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = url;
                psi.UseShellExecute = true;
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                StyledDialog.Info(this, "Erro ao Abrir Drive", "Não foi possível abrir o link de atualizações.", ex.Message);
            }
        }

        private void RestaurarJanelaPrincipal()
        {
            Show();
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            Activate();
        }
    }
}
