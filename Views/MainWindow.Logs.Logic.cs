using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        private HubActiveTab _previousTabBeforeLogs = HubActiveTab.Ambientes;
        private DispatcherTimer _logsRefreshTimer;
        private string _activeLogFilePath;
        private string _activeLogFolderPath;
        private string _activeLogServiceOrigin;
        private int _activeLogPid;
        private bool _activeLogAutoScroll = true;
        private long _activeLogLastFileLength = 0;
        private TextBox _logsConsoleTextBox;
        private TextBlock _logsBreadcrumbSub;
        private TextBlock _logCardCpuValue;
        private TextBlock _logCardRamValue;
        private TextBlock _logCardStatusValue;
        private TextBlock _logCardSizeValue;
        private Canvas _cpuSparklineCanvas;
        private Canvas _ramSparklineCanvas;
        private TextBox _logSearchBox;

        private List<double> _cpuHistory = new List<double>();
        private List<double> _ramHistory = new List<double>();
        private const int MaxHistorySamples = 30;

        private TimeSpan _activeLogLastCpuTime = TimeSpan.Zero;
        private DateTime _activeLogLastCpuSample = DateTime.MinValue;
        private string _cachedFullLogContent = "";

        private void ExportarArquivoLogAtual()
        {
            if (string.IsNullOrEmpty(_activeLogFilePath) || !File.Exists(_activeLogFilePath))
            {
                MessageBox.Show(this, "Nenhum arquivo de log para salvar.", "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = Path.GetFileName(_activeLogFilePath);
            sfd.Filter = "Arquivos de Log (*.log)|*.log|Todos os Arquivos (*.*)|*.*";
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    File.Copy(_activeLogFilePath, sfd.FileName, true);
                    MessageBox.Show(this, "Log exportado com sucesso!", "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Erro ao salvar arquivo: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CopiarTextoLogParaClipboard()
        {
            if (_logsConsoleTextBox != null)
            {
                try { Clipboard.SetText(_logsConsoleTextBox.Text); } catch { }
            }
        }

        private void AbrirLogNoEditorExterno()
        {
            if (!string.IsNullOrEmpty(_activeLogFilePath) && File.Exists(_activeLogFilePath))
            {
                try { Process.Start(_activeLogFilePath); } catch { }
            }
        }

        private List<string> DescobrirArquivosDeLog(string folderPath, string logTypeHint)
        {
            List<string> result = new List<string>();
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return result;

            try
            {
                List<string> priorityNames = new List<string>();
                if (string.Equals(logTypeHint, "dbaccess", StringComparison.OrdinalIgnoreCase))
                {
                    priorityNames.Add("dbconsole.log");
                    priorityNames.Add("dbaccess.log");
                }
                else if (string.Equals(logTypeHint, "smartview", StringComparison.OrdinalIgnoreCase))
                {
                    priorityNames.Add("smartview.log");
                    priorityNames.Add("backend.log");
                    priorityNames.Add("web.log");
                }
                else
                {
                    priorityNames.Add("console.log");
                    priorityNames.Add("appserver.log");
                }

                foreach (string name in priorityNames)
                {
                    string candidate = Path.Combine(folderPath, name);
                    if (File.Exists(candidate))
                    {
                        result.Add(candidate);
                    }
                }

                string[] files = Directory.GetFiles(folderPath, "*.log", SearchOption.TopDirectoryOnly);
                foreach (string f in files)
                {
                    if (!result.Any(r => string.Equals(r, f, StringComparison.OrdinalIgnoreCase)))
                    {
                        result.Add(f);
                    }
                }
            }
            catch { }

            return result;
        }

        private void RecarregarLogSelecionadoAsync()
        {
            if (string.IsNullOrEmpty(_activeLogFilePath) || !File.Exists(_activeLogFilePath))
            {
                if (_logsConsoleTextBox != null)
                {
                    _logsConsoleTextBox.Text = string.Format("[{0:HH:mm:ss}] Arquivo de log não encontrado em: {1}", DateTime.Now, _activeLogFilePath ?? "<vazio>");
                }
                if (_logCardSizeValue != null) _logCardSizeValue.Text = "0 KB";
                return;
            }

            string targetFile = _activeLogFilePath;

            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                string content = "";
                long fileLength = 0;

                try
                {
                    FileInfo fi = new FileInfo(targetFile);
                    fileLength = fi.Length;
                    content = LerUltimasLinhasArquivo(targetFile, 1000);
                }
                catch (Exception ex)
                {
                    content = "Erro ao ler arquivo de log: " + ex.Message;
                }

                Dispatcher.BeginInvoke(new Action(delegate
                {
                    _activeLogLastFileLength = fileLength;
                    _cachedFullLogContent = content;

                    if (_logSearchBox != null && !string.IsNullOrEmpty(_logSearchBox.Text))
                    {
                        FiltrarLinhasLogVisiveis(_logSearchBox.Text);
                    }
                    else if (_logsConsoleTextBox != null)
                    {
                        _logsConsoleTextBox.Text = content;
                        if (_activeLogAutoScroll)
                        {
                            _logsConsoleTextBox.ScrollToEnd();
                        }
                    }

                    if (_logCardSizeValue != null)
                    {
                        _logCardSizeValue.Text = FormatarTamanhoArquivo(fileLength);
                    }
                }));
            });
        }

        private void FiltrarLinhasLogVisiveis(string query)
        {
            if (_logsConsoleTextBox == null) return;
            if (string.IsNullOrEmpty(query))
            {
                _logsConsoleTextBox.Text = _cachedFullLogContent;
                if (_activeLogAutoScroll) _logsConsoleTextBox.ScrollToEnd();
                return;
            }

            string[] lines = _cachedFullLogContent.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            var filtered = lines.Where(l => l.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
            _logsConsoleTextBox.Text = string.Join(Environment.NewLine, filtered.ToArray());
        }

        private static string LerUltimasLinhasArquivo(string filePath, int maxLines)
        {
            if (!File.Exists(filePath)) return "";

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    long length = fs.Length;
                    long bufferSize = Math.Min(length, 512 * 1024);
                    fs.Seek(length - bufferSize, SeekOrigin.Begin);

                    byte[] buffer = new byte[bufferSize];
                    int bytesRead = fs.Read(buffer, 0, (int)bufferSize);

                    string text = Encoding.Default.GetString(buffer, 0, bytesRead);
                    string[] lines = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

                    if (lines.Length <= maxLines)
                        return text;

                    int skip = lines.Length - maxLines;
                    return string.Join(Environment.NewLine, lines.Skip(skip).ToArray());
                }
            }
            catch (Exception ex)
            {
                return "Falha ao acessar o log: " + ex.Message;
            }
        }

        private void IniciarTimerLogs()
        {
            if (_logsRefreshTimer != null)
                _logsRefreshTimer.Stop();

            _logsRefreshTimer = new DispatcherTimer();
            _logsRefreshTimer.Interval = TimeSpan.FromMilliseconds(1500);
            _logsRefreshTimer.Tick += delegate
            {
                AtualizarMetricasETailLog();
            };
            _logsRefreshTimer.Start();
        }

        private void PararTimerLogs()
        {
            if (_logsRefreshTimer != null)
            {
                _logsRefreshTimer.Stop();
                _logsRefreshTimer = null;
            }
        }

        private void AtualizarMetricasETailLog()
        {
            int currentPid = _activeLogPid;
            if (currentPid > 0)
            {
                double cpu;
                long ramMb;
                NativeWin32.GetProcessCpuAndRam(currentPid, ref _activeLogLastCpuTime, ref _activeLogLastCpuSample, out cpu, out ramMb);

                if (_logCardCpuValue != null) _logCardCpuValue.Text = string.Format("{0:0.0}%", cpu);
                if (_logCardRamValue != null) _logCardRamValue.Text = string.Format("{0} MB", ramMb);
                if (_logCardStatusValue != null)
                {
                    _logCardStatusValue.Text = "Ativo (PID " + currentPid + ")";
                    _logCardStatusValue.Foreground = UiTheme.AccentGreen;
                }

                _cpuHistory.Add(cpu);
                if (_cpuHistory.Count > MaxHistorySamples) _cpuHistory.RemoveAt(0);

                _ramHistory.Add((double)ramMb);
                if (_ramHistory.Count > MaxHistorySamples) _ramHistory.RemoveAt(0);

                DesenharSparkline(_cpuSparklineCanvas, _cpuHistory, UiTheme.AccentLightBlue);
                DesenharSparkline(_ramSparklineCanvas, _ramHistory, UiTheme.AccentGreen);
            }
            else
            {
                if (_logCardCpuValue != null) _logCardCpuValue.Text = "0.0%";
                if (_logCardRamValue != null) _logCardRamValue.Text = "0 MB";
                if (_logCardStatusValue != null)
                {
                    _logCardStatusValue.Text = "Parado / Offline";
                    _logCardStatusValue.Foreground = UiTheme.TextMuted;
                }
            }

            if (string.IsNullOrEmpty(_activeLogFilePath) || !File.Exists(_activeLogFilePath)) return;

            string targetFile = _activeLogFilePath;
            long prevLen = _activeLogLastFileLength;

            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    FileInfo fi = new FileInfo(targetFile);
                    long newLen = fi.Length;
                    if (newLen != prevLen)
                    {
                        string fresh = LerUltimasLinhasArquivo(targetFile, 1000);
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            _activeLogLastFileLength = newLen;
                            _cachedFullLogContent = fresh;
                            if (_logSearchBox != null && !string.IsNullOrEmpty(_logSearchBox.Text))
                            {
                                FiltrarLinhasLogVisiveis(_logSearchBox.Text);
                            }
                            else if (_logsConsoleTextBox != null)
                            {
                                _logsConsoleTextBox.Text = fresh;
                                if (_activeLogAutoScroll)
                                {
                                    _logsConsoleTextBox.ScrollToEnd();
                                }
                            }
                            if (_logCardSizeValue != null)
                            {
                                _logCardSizeValue.Text = FormatarTamanhoArquivo(newLen);
                            }
                        }));
                    }
                }
                catch { }
            });
        }

        private static string FormatarTamanhoArquivo(long bytes)
        {
            if (bytes <= 0) return "0 KB";
            if (bytes < 1024 * 1024)
                return string.Format("{0:0.0} KB", bytes / 1024.0);
            return string.Format("{0:0.00} MB", bytes / (1024.0 * 1024.0));
        }
    }
}
