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

        private System.Windows.Shapes.Path _overviewTotalCpuArc;
        private TextBlock _overviewTotalCpuCenterText;
        private Border _overviewTotalCpuBarFill;
        private TextBlock _overviewTotalCpuSummaryText;

        private System.Windows.Shapes.Path _overviewTotalRamArc;
        private TextBlock _overviewTotalRamCenterText;
        private Border _overviewTotalRamBarFill;
        private TextBlock _overviewTotalRamSummaryText;

        private StackPanel _overviewBreakdownStack;

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
            AtualizarGraficoCircular(_overviewTotalCpuArc, _overviewTotalCpuCenterText, totalCpu, string.Format("{0:0.0}%", totalCpu));
            if (_overviewTotalCpuBarFill != null)
                _overviewTotalCpuBarFill.Width = Math.Max(2, Math.Min(220, (totalCpu / 100.0) * 220));

            if (_overviewTotalCpuSummaryText != null)
                _overviewTotalCpuSummaryText.Text = string.Format("{0:0.0}% de uso consolidado em {1} serviço{2}",
                    totalCpu, ativos.Count, ativos.Count == 1 ? "" : "s");

            double ramGb = totalRamMb / 1024.0;
            string ramStr = totalRamMb >= 1024 ? string.Format("{0:0.0} GB", ramGb) : string.Format("{0} MB", totalRamMb);
            double ramPercent = Math.Min(100.0, (totalRamMb / 8192.0) * 100.0);

            AtualizarGraficoCircular(_overviewTotalRamArc, _overviewTotalRamCenterText, ramPercent, ramStr);
            if (_overviewTotalRamBarFill != null)
                _overviewTotalRamBarFill.Width = Math.Max(2, Math.Min(220, (ramPercent / 100.0) * 220));

            if (_overviewTotalRamSummaryText != null)
                _overviewTotalRamSummaryText.Text = string.Format("{0} MB ({1:0.00} GB) alocados no total", totalRamMb, ramGb);

            AtualizarBreakdownServicos(ativos, totalCpu, totalRamMb);
        }

        private void AtualizarGraficoCircular(System.Windows.Shapes.Path path, TextBlock textBlock, double percent, string textValue)
        {
            if (textBlock != null) textBlock.Text = textValue;
            if (path == null) return;

            double p = Math.Max(0.01, Math.Min(99.99, percent));
            double angle = p * 3.6;
            double rad = (angle - 90.0) * (Math.PI / 180.0);
            double cx = 65, cy = 65, r = 50;
            double ex = cx + r * Math.Cos(rad);
            double ey = cy + r * Math.Sin(rad);

            PathFigure fig = new PathFigure();
            fig.StartPoint = new Point(cx, cy - r);
            fig.Segments.Add(new ArcSegment(new Point(ex, ey), new Size(r, r), 0, angle > 180.0, SweepDirection.Clockwise, true));
            PathGeometry geom = new PathGeometry();
            geom.Figures.Add(fig);
            path.Data = geom;
        }

        private void AtualizarBreakdownServicos(List<OverviewServiceItem> ativos, double totalCpu, long totalRamMb)
        {
            if (_overviewBreakdownStack == null) return;
            _overviewBreakdownStack.Children.Clear();

            if (ativos.Count == 0)
            {
                TextBlock empty = new TextBlock();
                empty.Text = "Nenhum serviço ativo no momento.";
                empty.FontSize = 11;
                empty.Foreground = UiTheme.TextMuted;
                empty.Margin = new Thickness(0, 4, 0, 0);
                _overviewBreakdownStack.Children.Add(empty);
                return;
            }

            foreach (OverviewServiceItem item in ativos)
            {
                Border row = new Border();
                row.Background = UiTheme.TagBg;
                row.BorderBrush = UiTheme.BorderSubtle;
                row.BorderThickness = new Thickness(1);
                row.CornerRadius = new CornerRadius(6);
                row.Padding = new Thickness(10, 7, 10, 7);
                row.Margin = new Thickness(0, 0, 0, 6);

                Grid g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                StackPanel spLeft = new StackPanel();
                spLeft.Orientation = Orientation.Horizontal;
                spLeft.VerticalAlignment = VerticalAlignment.Center;

                Border dot = new Border();
                dot.Width = 8;
                dot.Height = 8;
                dot.CornerRadius = new CornerRadius(4);
                dot.Background = string.Equals(item.TypeHint, "dbaccess", StringComparison.OrdinalIgnoreCase)
                    ? UiTheme.AccentAmber
                    : string.Equals(item.TypeHint, "smartview", StringComparison.OrdinalIgnoreCase)
                        ? UiTheme.BrushFromHex("#A855F7")
                        : UiTheme.AccentLightBlue;
                dot.Margin = new Thickness(0, 0, 6, 0);
                dot.VerticalAlignment = VerticalAlignment.Center;
                spLeft.Children.Add(dot);

                TextBlock nameTxt = new TextBlock();
                nameTxt.Text = item.Name;
                nameTxt.FontSize = 11;
                nameTxt.FontWeight = FontWeights.SemiBold;
                nameTxt.Foreground = UiTheme.TextPrimary;
                nameTxt.VerticalAlignment = VerticalAlignment.Center;
                spLeft.Children.Add(nameTxt);

                Grid.SetColumn(spLeft, 0);
                g.Children.Add(spLeft);

                StackPanel spRight = new StackPanel();
                spRight.Orientation = Orientation.Horizontal;
                spRight.VerticalAlignment = VerticalAlignment.Center;

                TextBlock cpuTxt = new TextBlock();
                cpuTxt.Text = string.Format("CPU: {0:0.0}%", item.CpuPercent);
                cpuTxt.FontSize = 10.5;
                cpuTxt.FontWeight = FontWeights.Bold;
                cpuTxt.Foreground = UiTheme.AccentLightBlue;
                cpuTxt.Margin = new Thickness(0, 0, 10, 0);
                spRight.Children.Add(cpuTxt);

                TextBlock ramTxt = new TextBlock();
                ramTxt.Text = string.Format("RAM: {0} MB", item.RamMb);
                ramTxt.FontSize = 10.5;
                ramTxt.FontWeight = FontWeights.Bold;
                ramTxt.Foreground = UiTheme.AccentGreen;
                spRight.Children.Add(ramTxt);

                Grid.SetColumn(spRight, 1);
                g.Children.Add(spRight);

                row.Child = g;
                _overviewBreakdownStack.Children.Add(row);
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
                        _overviewLogConsole.Text = content;
                        if (_overviewAutoScroll)
                            _overviewLogConsole.ScrollToEnd();
                    }
                }));
            });
        }

        private void FiltrarLinhasLogOverview()
        {
            if (_overviewLogConsole == null) return;
            string raw = _cachedOverviewLogContent ?? "";
            if (string.IsNullOrEmpty(_overviewFilterText))
            {
                _overviewLogConsole.Text = raw;
                if (_overviewAutoScroll) _overviewLogConsole.ScrollToEnd();
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
