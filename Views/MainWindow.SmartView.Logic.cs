using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Path = System.IO.Path;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        private SmartViewPortStatus _smartViewPort7017 = new SmartViewPortStatus(7017, "SmartView Home", "Serviço central de execução e processamento de relatórios SmartView");
        private SmartViewPortStatus _smartViewPort7019 = new SmartViewPortStatus(7019, "SmartView Diagnostic", "Interface web do SmartView e APIs REST de diagnóstico");

        private ScrollViewer _svScrollViewer;
        private Panel _svCardsContainer;
        private Border _svCard7017;
        private Border _svCard7019;
        private TextBlock _svCard7017StatusText;
        private TextBlock _svCard7019StatusText;
        private Border _svCard7017Badge;
        private Border _svCard7019Badge;
        private TextBlock _svCard7017LatencyText;
        private TextBlock _svCard7019LatencyText;
        private TextBlock _svFolderLabel;
        private TextBlock _svLastUpdateLabel;
        private string _smartViewDetectedFolder;
        private int _smartViewPid = 0;
        private string _smartViewSavedFolderPrefPath;
        private string _smartViewNotesPrefPath;
        private Dictionary<int, string> _smartViewNotes = new Dictionary<int, string>();
        private string _svFilterText = "";

        private bool _isCheckingSmartView = false;

        private void CarregarNotasSmartViewAppData()
        {
            if (string.IsNullOrEmpty(_smartViewNotesPrefPath))
                _smartViewNotesPrefPath = Path.Combine(_appDataDir, "smartview_notas.txt");

            _smartViewNotes.Clear();
            if (File.Exists(_smartViewNotesPrefPath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(_smartViewNotesPrefPath, Encoding.UTF8);
                    foreach (string line in lines)
                    {
                        int eq = line.IndexOf('=');
                        if (eq > 0)
                        {
                            int port;
                            if (int.TryParse(line.Substring(0, eq).Trim(), out port))
                            {
                                _smartViewNotes[port] = line.Substring(eq + 1).Trim();
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private void SalvarNotasSmartViewAppData()
        {
            if (string.IsNullOrEmpty(_smartViewNotesPrefPath))
                _smartViewNotesPrefPath = Path.Combine(_appDataDir, "smartview_notas.txt");

            try
            {
                List<string> lines = new List<string>();
                foreach (KeyValuePair<int, string> kvp in _smartViewNotes)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Value))
                        lines.Add(string.Format("{0}={1}", kvp.Key, kvp.Value.Trim()));
                }
                File.WriteAllLines(_smartViewNotesPrefPath, lines.ToArray(), Encoding.UTF8);
            }
            catch { }
        }

        private void CarregarSmartViewPrefFolder()
        {
            if (string.IsNullOrEmpty(_smartViewSavedFolderPrefPath))
                _smartViewSavedFolderPrefPath = Path.Combine(_appDataDir, "smartview_path.txt");

            if (File.Exists(_smartViewSavedFolderPrefPath))
            {
                try
                {
                    string txt = File.ReadAllText(_smartViewSavedFolderPrefPath).Trim();
                    if (!string.IsNullOrEmpty(txt) && Directory.Exists(txt))
                        _smartViewDetectedFolder = txt;
                }
                catch { }
            }
        }

        private void SalvarSmartViewPrefFolder(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(_smartViewSavedFolderPrefPath))
                    _smartViewSavedFolderPrefPath = Path.Combine(_appDataDir, "smartview_path.txt");
                File.WriteAllText(_smartViewSavedFolderPrefPath, path ?? "");
            }
            catch { }
        }

        private string ObterDiretorioLogsSmartView()
        {
            string root = _smartViewDetectedFolder;
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                if (Directory.Exists(@"C:\TOTVS\smart-view"))
                    root = @"C:\TOTVS\smart-view";
            }

            if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
            {
                string directLogs = Path.Combine(root, "logs");
                if (Directory.Exists(directLogs)) return directLogs;

                string deploysDir = Path.Combine(root, "deployments");
                if (Directory.Exists(deploysDir))
                {
                    try
                    {
                        foreach (string sub in Directory.GetDirectories(deploysDir))
                        {
                            string logSub = Path.Combine(sub, "logs");
                            if (Directory.Exists(logSub)) return logSub;
                            if (Directory.GetFiles(sub, "*.log").Length > 0) return sub;
                        }
                    }
                    catch { }
                }

                return root;
            }

            return _smartViewDetectedFolder;
        }

        private void VerificarPortasSmartViewAsync()
        {
            if (_isCheckingSmartView) return;
            _isCheckingSmartView = true;

            ThreadPool.QueueUserWorkItem(delegate
            {
                TestarPortaTcp(_smartViewPort7017);
                TestarPortaTcp(_smartViewPort7019);

                int pid = NativeWin32.GetProcessIdByTcpPort(7017);
                if (pid == 0) pid = NativeWin32.GetProcessIdByTcpPort(7019);
                _smartViewPid = pid;
                if (pid > 0 && string.IsNullOrEmpty(_smartViewDetectedFolder))
                {
                    string exePath = NativeWin32.GetProcessImagePathSafe(pid);
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        _smartViewDetectedFolder = Path.GetDirectoryName(exePath);
                        SalvarSmartViewPrefFolder(_smartViewDetectedFolder);
                    }
                }

                _isCheckingSmartView = false;

                Dispatcher.BeginInvoke(new Action(delegate
                {
                    AtualizarVisualPortasPadrao();
                    if (_svFolderLabel != null)
                    {
                        if (string.IsNullOrEmpty(_smartViewDetectedFolder))
                        {
                            _svFolderLabel.Text = "Pasta: Não foi possível localizar pelas portas. Selecione a pasta acima";
                            _svFolderLabel.ToolTip = "Clique para selecionar a pasta de instalação do SmartView";
                        }
                        else
                        {
                            _svFolderLabel.Text = "Pasta: " + _smartViewDetectedFolder;
                            _svFolderLabel.ToolTip = "Clique para abrir no Windows Explorer";
                        }
                    }
                    if (_svLastUpdateLabel != null)
                    {
                        _svLastUpdateLabel.Text = string.Format("Atualizado às {0:HH:mm:ss}", DateTime.Now);
                    }
                }));
            });
        }

        private void SelecionarPastaSmartViewDialog()
        {
            using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.Description = "Selecione a pasta de instalação do SmartView";
                if (!string.IsNullOrEmpty(_smartViewDetectedFolder) && Directory.Exists(_smartViewDetectedFolder))
                    fbd.SelectedPath = _smartViewDetectedFolder;

                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    _smartViewDetectedFolder = fbd.SelectedPath;
                    SalvarSmartViewPrefFolder(_smartViewDetectedFolder);
                    if (_svFolderLabel != null)
                    {
                        _svFolderLabel.Text = "Pasta: " + _smartViewDetectedFolder;
                        _svFolderLabel.ToolTip = "Clique para abrir no Windows Explorer";
                    }
                    VerificarPortasSmartViewAsync();
                }
            }
        }

        private void TestarPortaTcp(SmartViewPortStatus port)
        {
            string host = string.IsNullOrEmpty(port.Host) ? "127.0.0.1" : port.Host;
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
                host = "127.0.0.1";

            Stopwatch sw = Stopwatch.StartNew();
            bool connected = false;
            string err = "";

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    IAsyncResult ar = client.BeginConnect(host, port.Port, null, null);
                    bool success = ar.AsyncWaitHandle.WaitOne(800, false);
                    if (success && client.Connected)
                    {
                        client.EndConnect(ar);
                        connected = true;
                    }
                    else
                    {
                        connected = false;
                        err = "Timeout (800ms)";
                    }
                }
            }
            catch (Exception ex)
            {
                connected = false;
                err = ex.Message;
            }
            finally
            {
                sw.Stop();
            }

            port.IsOpen = connected;
            port.LatencyMs = connected ? sw.ElapsedMilliseconds : -1;
            port.ErrorMessage = err;
            port.LastChecked = DateTime.Now;
        }
    }
}
