using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _overviewTimer;
        private string _selectedOverviewServiceId = "";
        private string _selectedOverviewLogPath = "";
        private string _selectedOverviewServiceName = "";
        private string _selectedOverviewHint = "appserver";
        private int _selectedOverviewPid = 0;
        private string _selectedOverviewActiveLogFile = "";
        private bool _overviewAutoScroll = true;
        private string _cachedOverviewLogContent = "";
        private long _overviewLastFileLength = 0;
        private string _overviewFilterText = "";

        private StackPanel _overviewCardsStack;
        private TextBlock _overviewLogTitle;
        private TextBlock _overviewLogPathText;
        private TextBox _overviewLogConsole;
        private TextBox _overviewSearchBox;
        private Button _overviewAutoScrollBtn;
        private TextBlock _overviewTotalCpuSummaryText;
        private TextBlock _overviewTotalRamSummaryText;
        private TextBlock _overviewTotalCpuDetailsText;
        private TextBlock _overviewTotalRamDetailsText;
        private ColumnDefinition _overviewTotalCpuColFill;
        private ColumnDefinition _overviewTotalCpuColEmpty;
        private ColumnDefinition _overviewTotalRamColFill;
        private ColumnDefinition _overviewTotalRamColEmpty;
        private TextBlock _overviewSummaryAppText;
        private TextBlock _overviewSummaryDbText;
        private TextBlock _overviewSummarySvText;

        private readonly Dictionary<string, Border> _overviewCardBorders = new Dictionary<string, Border>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TextBlock> _overviewCpuTexts = new Dictionary<string, TextBlock>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TextBlock> _overviewRamTexts = new Dictionary<string, TextBlock>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Border> _overviewCpuBars = new Dictionary<string, Border>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Border> _overviewRamBars = new Dictionary<string, Border>(StringComparer.OrdinalIgnoreCase);

        private void IniciarOverviewTimer()
        {
            PararOverviewTimer();
            _overviewTimer = new DispatcherTimer();
            _overviewTimer.Interval = TimeSpan.FromMilliseconds(1600);
            _overviewTimer.Tick += delegate
            {
                AtualizarConsumosOverviewLive();
                CarregarLogOverviewAsync(false);
            };
            _overviewTimer.Start();
        }

        private void PararOverviewTimer()
        {
            if (_overviewTimer != null)
            {
                _overviewTimer.Stop();
                _overviewTimer = null;
            }
        }

        private void AtualizarListaOverview()
        {
            if (_overviewCardsStack == null) return;
            _overviewCardsStack.Children.Clear();
            _overviewCardBorders.Clear();
            _overviewCpuTexts.Clear();
            _overviewRamTexts.Clear();
            _overviewCpuBars.Clear();
            _overviewRamBars.Clear();

            List<OverviewServiceItem> ativos = ObterServicosAtivos();

            if (ativos.Count == 0)
            {
                _selectedOverviewServiceId = "";
                _selectedOverviewActiveLogFile = "";
                if (_overviewLogTitle != null) _overviewLogTitle.Text = "Nenhum Serviço Ativo";
                if (_overviewLogPathText != null) _overviewLogPathText.Text = "Inicie um ambiente ou DBAccess para visualizar consumo e logs em tempo real";
                if (_overviewLogConsole != null) _overviewLogConsole.Text = "Aguardando serviços ativos em execução...";

                Border emptyCard = new Border();
                emptyCard.Background = UiTheme.BgCard;
                emptyCard.BorderBrush = UiTheme.BorderSubtle;
                emptyCard.BorderThickness = new Thickness(1);
                emptyCard.CornerRadius = new CornerRadius(10);
                emptyCard.Padding = new Thickness(18);
                emptyCard.Margin = new Thickness(0, 0, 0, 10);

                StackPanel sp = new StackPanel();
                TextBlock txtEmptyTitle = new TextBlock();
                txtEmptyTitle.Text = "Nenhum serviço em execução";
                txtEmptyTitle.FontSize = 14;
                txtEmptyTitle.FontWeight = FontWeights.Bold;
                txtEmptyTitle.Foreground = UiTheme.TextSecondary;
                sp.Children.Add(txtEmptyTitle);

                TextBlock txtEmptyDesc = new TextBlock();
                txtEmptyDesc.Text = "Quando um ambiente do Protheus (AppServer), DBAccess ou SmartView estiver ligado, seu consumo de CPU/RAM e log aparecerão aqui automaticamente.";
                txtEmptyDesc.FontSize = 12;
                txtEmptyDesc.Foreground = UiTheme.TextMuted;
                txtEmptyDesc.TextWrapping = TextWrapping.Wrap;
                txtEmptyDesc.Margin = new Thickness(0, 6, 0, 0);
                sp.Children.Add(txtEmptyDesc);

                emptyCard.Child = sp;
                _overviewCardsStack.Children.Add(emptyCard);

                AtualizarGraficosTotais(ativos, 0, 0);
                return;
            }

            if (string.IsNullOrEmpty(_selectedOverviewServiceId) || !ativos.Any(a => string.Equals(a.Id, _selectedOverviewServiceId, StringComparison.OrdinalIgnoreCase)))
            {
                OverviewServiceItem first = ativos[0];
                _selectedOverviewServiceId = first.Id;
                _selectedOverviewServiceName = first.Name;
                _selectedOverviewLogPath = first.FolderPath;
                _selectedOverviewPid = first.Pid;
                _selectedOverviewHint = first.TypeHint;
            }

            double sumCpu = 0;
            long sumRam = 0;

            foreach (OverviewServiceItem item in ativos)
            {
                sumCpu += item.CpuPercent;
                sumRam += item.RamMb;
                Border card = CriarCardServicoOverview(item);
                _overviewCardsStack.Children.Add(card);
            }

            AtualizarBordasSelecaoOverview();
            AtualizarGraficosTotais(ativos, sumCpu, sumRam);
        }

        private void AtualizarBordasSelecaoOverview()
        {
            foreach (KeyValuePair<string, Border> kvp in _overviewCardBorders)
            {
                bool isSelected = string.Equals(kvp.Key, _selectedOverviewServiceId, StringComparison.OrdinalIgnoreCase);
                kvp.Value.BorderBrush = isSelected ? UiTheme.AccentLightBlue : UiTheme.BorderSubtle;
                kvp.Value.BorderThickness = isSelected ? new Thickness(2) : new Thickness(1);
                kvp.Value.Background = isSelected ? UiTheme.BrushFromHex("#111827") : UiTheme.BgCard;
            }
        }

        private void AtualizarConsumosOverviewLive()
        {
            List<OverviewServiceItem> ativos = ObterServicosAtivos();
            double sumCpu = 0;
            long sumRam = 0;

            foreach (OverviewServiceItem item in ativos)
            {
                sumCpu += item.CpuPercent;
                sumRam += item.RamMb;

                TextBlock cpuTxt;
                if (_overviewCpuTexts.TryGetValue(item.Id, out cpuTxt))
                    cpuTxt.Text = string.Format("{0:0.0}%", item.CpuPercent);

                Border cpuBar;
                if (_overviewCpuBars.TryGetValue(item.Id, out cpuBar))
                    cpuBar.Width = Math.Max(2, Math.Min(180, (item.CpuPercent / 100.0) * 180));

                TextBlock ramTxt;
                if (_overviewRamTexts.TryGetValue(item.Id, out ramTxt))
                    ramTxt.Text = string.Format("{0} MB", item.RamMb);

                Border ramBar;
                if (_overviewRamBars.TryGetValue(item.Id, out ramBar))
                    ramBar.Width = Math.Max(2, Math.Min(180, (item.RamMb / 2048.0) * 180));
            }

            AtualizarGraficosTotais(ativos, sumCpu, sumRam);
        }

        private void AtualizarGraficosTotais(List<OverviewServiceItem> ativos, double totalCpu, long totalRamMb)
        {
            if (_overviewTotalCpuSummaryText != null)
                _overviewTotalCpuSummaryText.Text = string.Format("{0:0.0}%", totalCpu);

            double cpuPct = Math.Max(0.5, Math.Min(100.0, totalCpu));
            if (_overviewTotalCpuColFill != null && _overviewTotalCpuColEmpty != null)
            {
                _overviewTotalCpuColFill.Width = new GridLength(cpuPct, GridUnitType.Star);
                _overviewTotalCpuColEmpty.Width = new GridLength(Math.Max(0.01, 100.0 - cpuPct), GridUnitType.Star);
            }

            int appCount = ativos.Count(a => string.Equals(a.TypeHint, "appserver", StringComparison.OrdinalIgnoreCase));
            int dbCount = ativos.Count(a => string.Equals(a.TypeHint, "dbaccess", StringComparison.OrdinalIgnoreCase));
            int svCount = ativos.Count(a => string.Equals(a.TypeHint, "smartview", StringComparison.OrdinalIgnoreCase));

            if (_overviewTotalCpuDetailsText != null)
            {
                _overviewTotalCpuDetailsText.Text = string.Format("{0} serviço{1} ativo{1} consumindo processamento",
                    ativos.Count, ativos.Count == 1 ? "" : "s");
            }

            double ramGb = totalRamMb / 1024.0;
            string ramStr = totalRamMb >= 1024 ? string.Format("{0:0.0} GB", ramGb) : string.Format("{0} MB", totalRamMb);

            if (_overviewTotalRamSummaryText != null)
                _overviewTotalRamSummaryText.Text = ramStr;

            double maxRamRef = Math.Max(4096.0, totalRamMb > 0 ? (double)totalRamMb : 4096.0);
            double ramPct = Math.Max(0.5, Math.Min(100.0, (totalRamMb / maxRamRef) * 100.0));
            if (_overviewTotalRamColFill != null && _overviewTotalRamColEmpty != null)
            {
                _overviewTotalRamColFill.Width = new GridLength(ramPct, GridUnitType.Star);
                _overviewTotalRamColEmpty.Width = new GridLength(Math.Max(0.01, 100.0 - ramPct), GridUnitType.Star);
            }

            if (_overviewTotalRamDetailsText != null)
            {
                _overviewTotalRamDetailsText.Text = string.Format("{0} alocado{1} entre os serviços em execução",
                    ramStr, totalRamMb == 0 ? "" : "s");
            }

            if (_overviewSummaryAppText != null)
            {
                _overviewSummaryAppText.Text = string.Format("{0} ativo{1}", appCount, appCount == 1 ? "" : "s");
                _overviewSummaryAppText.Foreground = appCount > 0 ? UiTheme.AccentLightBlue : UiTheme.TextSecondary;
            }

            if (_overviewSummaryDbText != null)
            {
                _overviewSummaryDbText.Text = dbCount > 0 ? "Ativo" : "Inativo";
                _overviewSummaryDbText.Foreground = dbCount > 0 ? UiTheme.AccentAmber : UiTheme.TextSecondary;
            }

            if (_overviewSummarySvText != null)
            {
                _overviewSummarySvText.Text = svCount > 0 ? string.Format("{0} porta{1}", svCount, svCount == 1 ? "" : "s") : "Inativo";
                _overviewSummarySvText.Foreground = svCount > 0 ? UiTheme.BrushFromHex("#A855F7") : UiTheme.TextSecondary;
            }
        }

        private void CarregarLogOverviewAsync(bool forceReload)
        {
            if (string.IsNullOrEmpty(_selectedOverviewServiceId) || string.IsNullOrEmpty(_selectedOverviewLogPath))
                return;

            string targetFolder = _selectedOverviewLogPath;
            string typeHint = _selectedOverviewHint;
            string serviceName = _selectedOverviewServiceName;

            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                List<string> logs = DescobrirArquivosDeLog(targetFolder, typeHint);
                string activeLog = logs.FirstOrDefault();

                if (string.IsNullOrEmpty(activeLog) || !File.Exists(activeLog))
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        if (_overviewLogTitle != null) _overviewLogTitle.Text = serviceName + " — Nenhum log encontrado";
                        if (_overviewLogPathText != null) _overviewLogPathText.Text = targetFolder;
                        if (_overviewLogConsole != null && forceReload)
                            _overviewLogConsole.Text = "Nenhum arquivo de log (.log) encontrado na pasta do serviço.";
                    }));
                    return;
                }

                long len = 0;
                try { len = new FileInfo(activeLog).Length; } catch { }

                if (!forceReload && string.Equals(activeLog, _selectedOverviewActiveLogFile, StringComparison.OrdinalIgnoreCase) && len == _overviewLastFileLength)
                    return;

                _selectedOverviewActiveLogFile = activeLog;
                _overviewLastFileLength = len;
                string content = LerUltimasLinhasArquivo(activeLog, 1200);

                Dispatcher.BeginInvoke(new Action(delegate
                {
                    _cachedOverviewLogContent = content;
                    if (_overviewLogTitle != null)
                        _overviewLogTitle.Text = string.Format("{0} — {1}", serviceName, Path.GetFileName(activeLog));
                    if (_overviewLogPathText != null)
                        _overviewLogPathText.Text = activeLog;

                    if (!string.IsNullOrEmpty(_overviewFilterText))
                        FiltrarLinhasLogOverview();
                    else if (_overviewLogConsole != null)
                    {
                        double hOffset = _overviewLogConsole.HorizontalOffset;
                        double vOffset = _overviewLogConsole.VerticalOffset;
                        _overviewLogConsole.Text = content;
                        if (_overviewAutoScroll)
                        {
                            _overviewLogConsole.ScrollToVerticalOffset(_overviewLogConsole.ExtentHeight);
                            if (hOffset > 0) _overviewLogConsole.ScrollToHorizontalOffset(hOffset);
                        }
                        else
                        {
                            _overviewLogConsole.ScrollToVerticalOffset(vOffset);
                            if (hOffset > 0) _overviewLogConsole.ScrollToHorizontalOffset(hOffset);
                        }
                    }
                }));
            });
        }

        private void FiltrarLinhasLogOverview()
        {
            if (_overviewLogConsole == null) return;
            string raw = _cachedOverviewLogContent ?? "";
            double hOffset = _overviewLogConsole.HorizontalOffset;
            double vOffset = _overviewLogConsole.VerticalOffset;

            if (string.IsNullOrEmpty(_overviewFilterText))
            {
                _overviewLogConsole.Text = raw;
                if (_overviewAutoScroll)
                {
                    _overviewLogConsole.ScrollToVerticalOffset(_overviewLogConsole.ExtentHeight);
                    if (hOffset > 0) _overviewLogConsole.ScrollToHorizontalOffset(hOffset);
                }
                else
                {
                    _overviewLogConsole.ScrollToVerticalOffset(vOffset);
                    if (hOffset > 0) _overviewLogConsole.ScrollToHorizontalOffset(hOffset);
                }
                return;
            }

            string[] lines = raw.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            List<string> filtered = new List<string>();
            string query = _overviewFilterText.Trim().ToLowerInvariant();
            foreach (string line in lines)
            {
                if (line.ToLowerInvariant().Contains(query))
                    filtered.Add(line);
            }
            _overviewLogConsole.Text = string.Join(Environment.NewLine, filtered.ToArray());
            if (_overviewAutoScroll)
            {
                _overviewLogConsole.ScrollToVerticalOffset(_overviewLogConsole.ExtentHeight);
                if (hOffset > 0) _overviewLogConsole.ScrollToHorizontalOffset(hOffset);
            }
            else
            {
                _overviewLogConsole.ScrollToVerticalOffset(vOffset);
                if (hOffset > 0) _overviewLogConsole.ScrollToHorizontalOffset(hOffset);
            }
        }

        private List<OverviewServiceItem> ObterServicosAtivos()
        {
            List<OverviewServiceItem> list = new List<OverviewServiceItem>();

            if (_ambientes != null)
            {
                foreach (AmbienteInfo amb in _ambientes)
                {
                    if (amb.State == EnvRunState.Active && !_hiddenDirs.Contains(amb.AppDir))
                    {
                        OverviewServiceItem item = new OverviewServiceItem();
                        item.Id = "appserver_" + amb.NomeIni;
                        item.Name = amb.NomeIni;
                        item.TypeLabel = "AppServer";
                        item.TypeHint = "appserver";
                        item.Port = !string.IsNullOrEmpty(amb.TcpPort) ? amb.TcpPort : amb.WebAppPort;
                        item.Pid = amb.Pid;
                        item.CpuPercent = amb.CpuPercent;
                        item.RamMb = amb.RamMb;
                        item.FolderPath = amb.AppDir;
                        list.Add(item);
                    }
                }
            }

            if (_dbAccessConfig != null && _dbAccessConfig.IsRunning)
            {
                OverviewServiceItem dbItem = new OverviewServiceItem();
                dbItem.Id = "dbaccess_main";
                dbItem.Name = "DBAccess";
                dbItem.TypeLabel = "DBAccess";
                dbItem.TypeHint = "dbaccess";
                dbItem.Port = (_dbAccessConfig.MonitorPort > 0 ? _dbAccessConfig.MonitorPort : 7890).ToString();
                dbItem.Pid = _dbAccessConfig.ProcessId;
                dbItem.CpuPercent = _dbAccessConfig.CpuPercent;
                dbItem.RamMb = _dbAccessConfig.RamMb;
                dbItem.FolderPath = _dbAccessConfig.FolderPath;
                list.Add(dbItem);
            }

            if (_smartViewPort7017 != null && _smartViewPort7019 != null && (_smartViewPort7017.IsOpen || _smartViewPort7019.IsOpen))
            {
                OverviewServiceItem svItem = new OverviewServiceItem();
                svItem.Id = "smartview_main";
                svItem.Name = "SmartView";
                svItem.TypeLabel = "SmartView";
                svItem.TypeHint = "smartview";
                svItem.Port = "7017 / 7019";
                svItem.Pid = _smartViewPid;
                svItem.CpuPercent = 0;
                svItem.RamMb = 0;
                svItem.FolderPath = ObterDiretorioLogsSmartView();

                if (_smartViewPid > 0)
                {
                    try
                    {
                        using (Process p = Process.GetProcessById(_smartViewPid))
                        {
                            if (!p.HasExited)
                            {
                                svItem.RamMb = p.WorkingSet64 / (1024 * 1024);
                            }
                        }
                    }
                    catch { }
                }

                list.Add(svItem);
            }

            return list;
        }

        private class OverviewServiceItem
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string TypeLabel { get; set; }
            public string TypeHint { get; set; }
            public string Port { get; set; }
            public int Pid { get; set; }
            public double CpuPercent { get; set; }
            public long RamMb { get; set; }
            public string FolderPath { get; set; }
        }
    }
}
