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

        private void InicializarBandejaWindows()
        {
            _trayMenu = new System.Windows.Forms.ContextMenuStrip();
            _trayMenu.Renderer = new DarkTrayRenderer();
            _trayMenu.ForeColor = Color.FromArgb(248, 250, 252);

            _trayIcon = new System.Windows.Forms.NotifyIcon();
            _trayIcon.Icon = TotvsLogo.CreateTotvsTrayIcon(_cachedSvgPath);
            _trayIcon.Text = "TOTVSHub";
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += delegate { RestaurarJanelaPrincipal(); };

            _trayIcon.MouseUp += delegate(object s, System.Windows.Forms.MouseEventArgs e)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    ReconstruirMenuBandeja();
                    if (_trayMenu.Handle != IntPtr.Zero)
                        NativeWin32.SetForegroundWindow(_trayMenu.Handle);
                    _trayMenu.Show(System.Windows.Forms.Cursor.Position);
                }
            };
        }

        private void ReconstruirMenuBandeja()
        {
            if (_trayMenu == null) return;
            _trayMenu.Items.Clear();

            var openItem = new System.Windows.Forms.ToolStripMenuItem("Abrir TOTVSHub");
            openItem.ForeColor = Color.White;
            openItem.Click += delegate { RestaurarJanelaPrincipal(); };
            _trayMenu.Items.Add(openItem);

            _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var protheusMenu = new System.Windows.Forms.ToolStripMenuItem("Protheus");
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

            var desativarMenu = new System.Windows.Forms.ToolStripMenuItem("Desativar");
            desativarMenu.ForeColor = Color.White;
            var basesAtivas = _ambientes.Where(a => a.State == EnvRunState.Active).ToList();
            if (basesAtivas.Count == 0)
            {
                var nenhumaAtiva = new System.Windows.Forms.ToolStripMenuItem("Nenhuma base ativa no momento");
                nenhumaAtiva.Enabled = false;
                desativarMenu.DropDownItems.Add(nenhumaAtiva);
            }
            else
            {
                foreach (AmbienteInfo amb in basesAtivas)
                {
                    AmbienteInfo captured = amb;
                    string modo = captured.RunningAsService ? "Serviço" : "Console";
                    var subStop = new System.Windows.Forms.ToolStripMenuItem(string.Format("Parar {0} ({1})", captured.NomeIni, modo));
                    subStop.ForeColor = Color.FromArgb(248, 113, 113);
                    subStop.Click += delegate { EncerrarAmbienteAsync(captured); };
                    desativarMenu.DropDownItems.Add(subStop);
                }
            }
            _trayMenu.Items.Add(desativarMenu);

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
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                }
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
