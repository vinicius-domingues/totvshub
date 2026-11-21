using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
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

        public void AbrirTelaCompletaLogs(string serviceOrigin, string folderPath, int pid, string logTypeHint)
        {
            _isInDashboardView = false;
            _previousTabBeforeLogs = _currentTab;

            _activeLogServiceOrigin = !string.IsNullOrEmpty(serviceOrigin) ? serviceOrigin : "Serviço";
            _activeLogFolderPath = folderPath;
            _activeLogPid = pid;
            _activeLogAutoScroll = true;
            _activeLogLastFileLength = 0;
            _activeLogLastCpuTime = TimeSpan.Zero;
            _activeLogLastCpuSample = DateTime.MinValue;
            _cpuHistory.Clear();
            _ramHistory.Clear();

            // Para qualquer timer anterior de logs
            PararTimerLogs();

            // Localiza os arquivos de log disponíveis na pasta
            List<string> logFiles = DescobrirArquivosDeLog(folderPath, logTypeHint);
            _activeLogFilePath = logFiles.FirstOrDefault();

            Grid rootGrid = new Grid();
            RowDefinition rowTop = new RowDefinition(); rowTop.Height = GridLength.Auto;
            RowDefinition rowHeader = new RowDefinition(); rowHeader.Height = GridLength.Auto;
            RowDefinition rowMetrics = new RowDefinition(); rowMetrics.Height = GridLength.Auto;
            RowDefinition rowLogTabs = new RowDefinition(); rowLogTabs.Height = GridLength.Auto;
            RowDefinition rowConsole = new RowDefinition(); rowConsole.Height = new GridLength(1, GridUnitType.Star);

            rootGrid.RowDefinitions.Add(rowTop);
            rootGrid.RowDefinitions.Add(rowHeader);
            rootGrid.RowDefinitions.Add(rowMetrics);
            rootGrid.RowDefinitions.Add(rowLogTabs);
            rootGrid.RowDefinitions.Add(rowConsole);

            // 1. Barra Superior Principal do App
            StackPanel titleLeft = CreateBrandHeaderWithTabs(_currentTab);
            Border topBar = BuildTopWindowBar(titleLeft, null);
            Grid.SetRow(topBar, 0);
            rootGrid.Children.Add(topBar);

            // 2. Header Breadcrumb + Busca na esquerda + Acoes icones a direita
            Border breadcrumbBar = new Border();
            breadcrumbBar.Background = UiTheme.BgHeader;
            breadcrumbBar.BorderBrush = UiTheme.BorderSubtle;
            breadcrumbBar.BorderThickness = new Thickness(0, 0, 0, 1);
            breadcrumbBar.Padding = new Thickness(20, 10, 20, 10);

            Grid headGrid = new Grid();
            ColumnDefinition cLeft = new ColumnDefinition(); cLeft.Width = GridLength.Auto;
            ColumnDefinition cSearch = new ColumnDefinition(); cSearch.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition cRight = new ColumnDefinition(); cRight.Width = GridLength.Auto;
            headGrid.ColumnDefinitions.Add(cLeft);
            headGrid.ColumnDefinitions.Add(cSearch);
            headGrid.ColumnDefinitions.Add(cRight);

            // Esquerda: [ ← Voltar ] <base> > <log x>
            StackPanel breadcrumbStack = new StackPanel();
            breadcrumbStack.Orientation = Orientation.Horizontal;
            breadcrumbStack.VerticalAlignment = VerticalAlignment.Center;
            breadcrumbStack.Margin = new Thickness(0, 0, 16, 0);

            Button btnBack = UiTheme.CreateActionButton("← Voltar", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
            btnBack.Height = 28;
            btnBack.Padding = new Thickness(10, 0, 10, 0);
            btnBack.Margin = new Thickness(0, 0, 14, 0);
            btnBack.FontSize = 11;
            btnBack.FontWeight = FontWeights.SemiBold;
            btnBack.ToolTip = "Voltar para a tela anterior";
            btnBack.Click += delegate
            {
                PararTimerLogs();
                SwitchToTab(_previousTabBeforeLogs);
            };
            breadcrumbStack.Children.Add(btnBack);

            TextBlock txtBaseName = new TextBlock();
            txtBaseName.Text = _activeLogServiceOrigin;
            txtBaseName.Foreground = UiTheme.TextPrimary;
            txtBaseName.FontSize = 14;
            txtBaseName.FontWeight = FontWeights.Bold;
            txtBaseName.VerticalAlignment = VerticalAlignment.Center;
            breadcrumbStack.Children.Add(txtBaseName);

            TextBlock txtSep = new TextBlock();
            txtSep.Text = "  ›  ";
            txtSep.Foreground = UiTheme.TextMuted;
            txtSep.FontSize = 14;
            txtSep.FontWeight = FontWeights.Bold;
            txtSep.VerticalAlignment = VerticalAlignment.Center;
            breadcrumbStack.Children.Add(txtSep);

            _logsBreadcrumbSub = new TextBlock();
            _logsBreadcrumbSub.Text = !string.IsNullOrEmpty(_activeLogFilePath) ? Path.GetFileName(_activeLogFilePath) : "Sem logs disponíveis";
            _logsBreadcrumbSub.Foreground = UiTheme.AccentLightBlue;
            _logsBreadcrumbSub.FontSize = 14;
            _logsBreadcrumbSub.FontWeight = FontWeights.SemiBold;
            _logsBreadcrumbSub.VerticalAlignment = VerticalAlignment.Center;
            breadcrumbStack.Children.Add(_logsBreadcrumbSub);

            Grid.SetColumn(breadcrumbStack, 0);
            headGrid.Children.Add(breadcrumbStack);

            // Centro/Esquerda: Campo de busca de texto no log + Atalho da pasta
            DockPanel searchDock = new DockPanel();
            searchDock.VerticalAlignment = VerticalAlignment.Center;
            searchDock.Margin = new Thickness(0, 0, 16, 0);

            Button btnFolder = UiTheme.CreateActionButton(
                UiTheme.CreateFolderIcon(Brushes.White, 14),
                UiTheme.NeutralBtnHex,
                UiTheme.NeutralBtnHoverHex);
            btnFolder.Width = 32;
            btnFolder.Height = 28;
            btnFolder.Padding = new Thickness(0);
            btnFolder.Margin = new Thickness(0, 0, 8, 0);
            btnFolder.ToolTip = "Abrir pasta de logs no Windows Explorer: " + _activeLogFolderPath;
            btnFolder.Click += delegate
            {
                if (!string.IsNullOrEmpty(_activeLogFolderPath) && Directory.Exists(_activeLogFolderPath))
                    AbrirNoExplorer(_activeLogFolderPath);
            };
            DockPanel.SetDock(btnFolder, Dock.Left);
            searchDock.Children.Add(btnFolder);

            Border searchContainer = new Border();
            searchContainer.Background = UiTheme.BgInput;
            searchContainer.BorderBrush = UiTheme.BorderSubtle;
            searchContainer.BorderThickness = new Thickness(1);
            searchContainer.CornerRadius = new CornerRadius(6);
            searchContainer.Padding = new Thickness(10, 4, 10, 4);
            searchContainer.Height = 28;

            Grid searchInner = new Grid();
            TextBlock searchPlaceholder = new TextBlock();
            searchPlaceholder.Text = "Filtrar conteúdo do log...";
            searchPlaceholder.Foreground = UiTheme.TextMuted;
            searchPlaceholder.FontSize = 12;
            searchPlaceholder.VerticalAlignment = VerticalAlignment.Center;
            searchPlaceholder.IsHitTestVisible = false;

            _logSearchBox = new TextBox();
            _logSearchBox.Background = Brushes.Transparent;
            _logSearchBox.Foreground = UiTheme.TextPrimary;
            _logSearchBox.CaretBrush = UiTheme.TextPrimary;
            _logSearchBox.BorderThickness = new Thickness(0);
            _logSearchBox.FontSize = 12;
            _logSearchBox.VerticalAlignment = VerticalAlignment.Center;
            _logSearchBox.TextChanged += delegate
            {
                searchPlaceholder.Visibility = string.IsNullOrEmpty(_logSearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
                FiltrarLinhasLogVisiveis(_logSearchBox.Text);
            };

            searchInner.Children.Add(searchPlaceholder);
            searchInner.Children.Add(_logSearchBox);
            searchContainer.Child = searchInner;
            searchDock.Children.Add(searchContainer);

            Grid.SetColumn(searchDock, 1);
            headGrid.Children.Add(searchDock);

            // Direita: Ações puras com ÍCONE e TOOLTIP (sem texto, sem pausa, sem pasta)
            StackPanel actionsStack = new StackPanel();
            actionsStack.Orientation = Orientation.Horizontal;
            actionsStack.VerticalAlignment = VerticalAlignment.Center;

            // 1. Auto-Scroll Toggle (Ícone de rolagem)
            Button btnAutoScroll = UiTheme.CreateActionButton(
                UiTheme.CreateScrollIcon(UiTheme.AccentLightBlue, 14),
                UiTheme.NeutralBtnHex,
                UiTheme.NeutralBtnHoverHex);
            btnAutoScroll.Width = 32;
            btnAutoScroll.Height = 28;
            btnAutoScroll.Padding = new Thickness(0);
            btnAutoScroll.Margin = new Thickness(0, 0, 6, 0);
            btnAutoScroll.ToolTip = "Rolagem automática para novas linhas (Ativa)";
            btnAutoScroll.Click += delegate
            {
                _activeLogAutoScroll = !_activeLogAutoScroll;
                btnAutoScroll.ToolTip = _activeLogAutoScroll ? "Rolagem automática (Ativa)" : "Rolagem automática (Pausada)";
                if (_activeLogAutoScroll && _logsConsoleTextBox != null)
                {
                    _logsConsoleTextBox.ScrollToEnd();
                }
            };
            actionsStack.Children.Add(btnAutoScroll);

            // 2. Download / Exportar Log
            Button btnDownload = UiTheme.CreateActionButton(
                UiTheme.CreateDownloadIcon(Brushes.White, 14),
                UiTheme.NeutralBtnHex,
                UiTheme.NeutralBtnHoverHex);
            btnDownload.Width = 32;
            btnDownload.Height = 28;
            btnDownload.Padding = new Thickness(0);
            btnDownload.Margin = new Thickness(0, 0, 6, 0);
            btnDownload.ToolTip = "Baixar / Salvar log em arquivo";
            btnDownload.Click += delegate
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
            };
            actionsStack.Children.Add(btnDownload);

            // 3. Copiar
            Button btnCopyAll = UiTheme.CreateActionButton(
                UiTheme.CreateCopyIcon(Brushes.White, 14),
                UiTheme.NeutralBtnHex,
                UiTheme.NeutralBtnHoverHex);
            btnCopyAll.Width = 32;
            btnCopyAll.Height = 28;
            btnCopyAll.Padding = new Thickness(0);
            btnCopyAll.Margin = new Thickness(0, 0, 6, 0);
            btnCopyAll.ToolTip = "Copiar todas as linhas visíveis para a área de transferência";
            btnCopyAll.Click += delegate
            {
                if (_logsConsoleTextBox != null)
                {
                    try { Clipboard.SetText(_logsConsoleTextBox.Text); } catch { }
                }
            };
            actionsStack.Children.Add(btnCopyAll);

            // 4. Abrir no Notepad
            Button btnOpenNotepad = UiTheme.CreateActionButton(
                UiTheme.CreatePencilIcon(Brushes.White, 14),
                UiTheme.NeutralBtnHex,
                UiTheme.NeutralBtnHoverHex);
            btnOpenNotepad.Width = 32;
            btnOpenNotepad.Height = 28;
            btnOpenNotepad.Padding = new Thickness(0);
            btnOpenNotepad.ToolTip = "Abrir arquivo completo no editor de texto padrão";
            btnOpenNotepad.Click += delegate
            {
                if (!string.IsNullOrEmpty(_activeLogFilePath) && File.Exists(_activeLogFilePath))
                {
                    try { Process.Start(_activeLogFilePath); } catch { }
                }
            };
            actionsStack.Children.Add(btnOpenNotepad);

            Grid.SetColumn(actionsStack, 2);
            headGrid.Children.Add(actionsStack);

            breadcrumbBar.Child = headGrid;
            Grid.SetRow(breadcrumbBar, 1);
            rootGrid.Children.Add(breadcrumbBar);

            // 3. Cards de Consumo e Gráficos de Performance
            Border metricsBar = BuildLogMetricsCards();
            Grid.SetRow(metricsBar, 2);
            rootGrid.Children.Add(metricsBar);

            // 4. Sub-abas de Arquivos de Log
            Border logTabsBar = BuildLogFileTabs(logFiles);
            Grid.SetRow(logTabsBar, 3);
            rootGrid.Children.Add(logTabsBar);

            // 5. Console Viewer Dark Mode
            Border consoleContainer = new Border();
            consoleContainer.Background = UiTheme.BrushFromHex("#060A10");
            consoleContainer.BorderBrush = UiTheme.BorderSubtle;
            consoleContainer.BorderThickness = new Thickness(0, 1, 0, 0);
            consoleContainer.Padding = new Thickness(14, 10, 14, 10);

            _logsConsoleTextBox = new TextBox();
            _logsConsoleTextBox.Background = Brushes.Transparent;
            _logsConsoleTextBox.Foreground = UiTheme.BrushFromHex("#E2E8F0");
            _logsConsoleTextBox.CaretBrush = UiTheme.AccentLightBlue;
            _logsConsoleTextBox.BorderThickness = new Thickness(0);
            _logsConsoleTextBox.FontFamily = new FontFamily("Consolas, Courier New, monospace");
            _logsConsoleTextBox.FontSize = 12;
            _logsConsoleTextBox.IsReadOnly = true;
            _logsConsoleTextBox.TextWrapping = TextWrapping.NoWrap;
            _logsConsoleTextBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _logsConsoleTextBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            _logsConsoleTextBox.AcceptsReturn = true;

            consoleContainer.Child = _logsConsoleTextBox;
            Grid.SetRow(consoleContainer, 4);
            rootGrid.Children.Add(consoleContainer);

            // Define o conteúdo da página no container principal sem quebrar o layout da janela
            _pageHost.Content = rootGrid;

            // Carregamento inicial assíncrono das linhas do log selecionado
            RecarregarLogSelecionadoAsync();

            // Inicia streaming contínuo apenas da aba selecionada
            IniciarTimerLogs();
        }

        private Border BuildLogMetricsCards()
        {
            Border bar = new Border();
            bar.Background = UiTheme.BgWindow;
            bar.Padding = new Thickness(20, 10, 20, 8);

            Grid grid = new Grid();
            for (int i = 0; i < 4; i++)
            {
                ColumnDefinition c = new ColumnDefinition();
                c.Width = new GridLength(1, GridUnitType.Star);
                grid.ColumnDefinitions.Add(c);
                if (i < 3)
                {
                    ColumnDefinition spacer = new ColumnDefinition();
                    spacer.Width = new GridLength(12);
                    grid.ColumnDefinitions.Add(spacer);
                }
            }

            // Card 1: Consumo de CPU com Gráfico Sparkline
            _logCardCpuValue = new TextBlock();
            _logCardCpuValue.Text = "0.0%";
            _logCardCpuValue.FontSize = 17;
            _logCardCpuValue.FontWeight = FontWeights.Bold;
            _logCardCpuValue.Foreground = UiTheme.AccentLightBlue;

            _cpuSparklineCanvas = new Canvas();
            _cpuSparklineCanvas.Height = 22;
            _cpuSparklineCanvas.ClipToBounds = true;
            _cpuSparklineCanvas.Margin = new Thickness(0, 4, 0, 0);

            Border cardCpu = CriarCardMetricaComGrafico("CPU", _logCardCpuValue, _cpuSparklineCanvas, "Uso do processador pelo serviço em tempo real");
            Grid.SetColumn(cardCpu, 0);
            grid.Children.Add(cardCpu);

            // Card 2: Consumo de RAM com Gráfico Sparkline
            _logCardRamValue = new TextBlock();
            _logCardRamValue.Text = "0 MB";
            _logCardRamValue.FontSize = 17;
            _logCardRamValue.FontWeight = FontWeights.Bold;
            _logCardRamValue.Foreground = UiTheme.AccentGreen;

            _ramSparklineCanvas = new Canvas();
            _ramSparklineCanvas.Height = 22;
            _ramSparklineCanvas.ClipToBounds = true;
            _ramSparklineCanvas.Margin = new Thickness(0, 4, 0, 0);

            Border cardRam = CriarCardMetricaComGrafico("RAM", _logCardRamValue, _ramSparklineCanvas, "Memória física de trabalho (Working Set)");
            Grid.SetColumn(cardRam, 2);
            grid.Children.Add(cardRam);

            // Card 3: Status / PID
            _logCardStatusValue = new TextBlock();
            _logCardStatusValue.Text = _activeLogPid > 0 ? "Ativo (PID " + _activeLogPid + ")" : "Parado / Offline";
            _logCardStatusValue.FontSize = 14;
            _logCardStatusValue.FontWeight = FontWeights.Bold;
            _logCardStatusValue.Foreground = _activeLogPid > 0 ? UiTheme.AccentGreen : UiTheme.TextMuted;
            Border cardStatus = CriarCardMetricaLog("🟢 Status / PID", _logCardStatusValue, "Identificador do processo no Windows");
            Grid.SetColumn(cardStatus, 4);
            grid.Children.Add(cardStatus);

            // Card 4: Tamanho do Arquivo
            _logCardSizeValue = new TextBlock();
            _logCardSizeValue.Text = "0 KB";
            _logCardSizeValue.FontSize = 15;
            _logCardSizeValue.FontWeight = FontWeights.Bold;
            _logCardSizeValue.Foreground = UiTheme.TextPrimary;
            Border cardSize = CriarCardMetricaLog("📄 Tamanho do Arquivo", _logCardSizeValue, "Tamanho no disco e volume do log");
            Grid.SetColumn(cardSize, 6);
            grid.Children.Add(cardSize);

            bar.Child = grid;
            return bar;
        }

        private Border CriarCardMetricaComGrafico(string title, TextBlock valBlock, Canvas sparkline, string tooltip)
        {
            Border card = new Border();
            card.Background = UiTheme.BgCard;
            card.BorderBrush = UiTheme.BorderSubtle;
            card.BorderThickness = new Thickness(1);
            card.CornerRadius = new CornerRadius(8);
            card.Padding = new Thickness(12, 8, 12, 6);
            card.ToolTip = tooltip;

            StackPanel sp = new StackPanel();

            DockPanel top = new DockPanel();
            TextBlock lblTitle = new TextBlock();
            lblTitle.Text = title;
            lblTitle.FontSize = 11;
            lblTitle.FontWeight = FontWeights.SemiBold;
            lblTitle.Foreground = UiTheme.TextSecondary;
            DockPanel.SetDock(lblTitle, Dock.Left);
            top.Children.Add(lblTitle);

            valBlock.HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(valBlock, Dock.Right);
            top.Children.Add(valBlock);
            sp.Children.Add(top);

            sp.Children.Add(sparkline);

            card.Child = sp;
            return card;
        }

        private Border CriarCardMetricaLog(string title, TextBlock valBlock, string tooltip)
        {
            Border card = new Border();
            card.Background = UiTheme.BgCard;
            card.BorderBrush = UiTheme.BorderSubtle;
            card.BorderThickness = new Thickness(1);
            card.CornerRadius = new CornerRadius(8);
            card.Padding = new Thickness(12, 10, 12, 10);
            card.ToolTip = tooltip;

            StackPanel sp = new StackPanel();
            TextBlock lblTitle = new TextBlock();
            lblTitle.Text = title;
            lblTitle.FontSize = 11;
            lblTitle.FontWeight = FontWeights.SemiBold;
            lblTitle.Foreground = UiTheme.TextSecondary;
            lblTitle.Margin = new Thickness(0, 0, 0, 4);
            sp.Children.Add(lblTitle);

            valBlock.VerticalAlignment = VerticalAlignment.Center;
            sp.Children.Add(valBlock);

            card.Child = sp;
            return card;
        }

        private void DesenharSparkline(Canvas canvas, List<double> history, Brush strokeBrush)
        {
            if (canvas == null) return;
            canvas.Children.Clear();
            if (history.Count < 2) return;

            double width = canvas.ActualWidth > 0 ? canvas.ActualWidth : 140;
            double height = canvas.ActualHeight > 0 ? canvas.ActualHeight : 22;

            double max = history.Max();
            if (max <= 0) max = 1.0;

            Polyline poly = new Polyline();
            poly.Stroke = strokeBrush;
            poly.StrokeThickness = 1.5;
            poly.StrokeLineJoin = PenLineJoin.Round;

            double step = width / (MaxHistorySamples - 1);
            int startIdx = Math.Max(0, history.Count - MaxHistorySamples);
            int sampleCount = history.Count - startIdx;
            double xOffset = width - (sampleCount - 1) * step;

            for (int i = 0; i < sampleCount; i++)
            {
                double val = history[startIdx + i];
                double x = xOffset + (i * step);
                double y = height - (val / max * (height - 4)) - 2;
                poly.Points.Add(new Point(x, y));
            }

            canvas.Children.Add(poly);
        }

        private Border BuildLogFileTabs(List<string> logFiles)
        {
            Border bar = new Border();
            bar.Background = UiTheme.BgHeader;
            bar.BorderBrush = UiTheme.BorderSubtle;
            bar.BorderThickness = new Thickness(0, 1, 0, 0);
            bar.Padding = new Thickness(20, 6, 20, 0);

            StackPanel tabsContainer = new StackPanel();
            tabsContainer.Orientation = Orientation.Horizontal;

            if (logFiles.Count == 0)
            {
                TextBlock emptyTxt = new TextBlock();
                emptyTxt.Text = "Nenhum arquivo .log encontrado na pasta: " + _activeLogFolderPath;
                emptyTxt.Foreground = UiTheme.TextMuted;
                emptyTxt.FontSize = 12;
                emptyTxt.Margin = new Thickness(0, 6, 0, 8);
                tabsContainer.Children.Add(emptyTxt);
            }
            else
            {
                foreach (string filePath in logFiles)
                {
                    string capturedPath = filePath;
                    string fileName = Path.GetFileName(capturedPath);
                    bool isSelected = string.Equals(_activeLogFilePath, capturedPath, StringComparison.OrdinalIgnoreCase);

                    Border tabItem = new Border();
                    tabItem.Height = 32;
                    tabItem.Padding = new Thickness(14, 0, 14, 0);
                    tabItem.Margin = new Thickness(0, 0, 6, 0);
                    tabItem.CornerRadius = new CornerRadius(6, 6, 0, 0);
                    tabItem.Cursor = Cursors.Hand;
                    tabItem.Background = isSelected ? UiTheme.BrushFromHex("#060A10") : Brushes.Transparent;
                    tabItem.BorderBrush = isSelected ? UiTheme.BorderSubtle : Brushes.Transparent;
                    tabItem.BorderThickness = new Thickness(1, 1, 1, 0);

                    StackPanel inner = new StackPanel();
                    inner.Orientation = Orientation.Horizontal;
                    inner.VerticalAlignment = VerticalAlignment.Center;

                    TextBlock icon = new TextBlock();
                    icon.Text = "📄 ";
                    icon.FontSize = 11;
                    icon.VerticalAlignment = VerticalAlignment.Center;
                    inner.Children.Add(icon);

                    TextBlock label = new TextBlock();
                    label.Text = fileName;
                    label.FontSize = 12;
                    label.FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal;
                    label.Foreground = isSelected ? UiTheme.AccentLightBlue : UiTheme.TextSecondary;
                    label.VerticalAlignment = VerticalAlignment.Center;
                    inner.Children.Add(label);

                    tabItem.Child = inner;

                    tabItem.MouseLeftButtonUp += delegate
                    {
                        if (!string.Equals(_activeLogFilePath, capturedPath, StringComparison.OrdinalIgnoreCase))
                        {
                            _activeLogFilePath = capturedPath;
                            if (_logsBreadcrumbSub != null) _logsBreadcrumbSub.Text = fileName;
                            _activeLogLastFileLength = 0;
                            bar.Child = BuildLogFileTabs(logFiles).Child;
                            RecarregarLogSelecionadoAsync();
                        }
                    };

                    tabsContainer.Children.Add(tabItem);
                }
            }

            bar.Child = tabsContainer;
            return bar;
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

        private string _cachedFullLogContent = "";

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
            // 1. Atualiza métricas de CPU e RAM
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

            // 2. Atualiza apenas o arquivo de log atualmente selecionado ("cada aba só é atualizada ao olhar ela")
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