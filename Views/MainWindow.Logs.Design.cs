using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
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

            PararTimerLogs();

            List<string> logFiles = DescobrirArquivosDeLog(folderPath, logTypeHint);
            _activeLogFilePath = logFiles.FirstOrDefault();

            Grid rootGrid = new Grid();
            RowDefinition rowTop = new RowDefinition(); rowTop.Height = GridLength.Auto;
            RowDefinition rowBreadcrumb = new RowDefinition(); rowBreadcrumb.Height = GridLength.Auto;
            RowDefinition rowMetrics = new RowDefinition(); rowMetrics.Height = GridLength.Auto;
            RowDefinition rowSearch = new RowDefinition(); rowSearch.Height = GridLength.Auto;
            RowDefinition rowLogTabs = new RowDefinition(); rowLogTabs.Height = GridLength.Auto;
            RowDefinition rowConsole = new RowDefinition(); rowConsole.Height = new GridLength(1, GridUnitType.Star);

            rootGrid.RowDefinitions.Add(rowTop);
            rootGrid.RowDefinitions.Add(rowBreadcrumb);
            rootGrid.RowDefinitions.Add(rowMetrics);
            rootGrid.RowDefinitions.Add(rowSearch);
            rootGrid.RowDefinitions.Add(rowLogTabs);
            rootGrid.RowDefinitions.Add(rowConsole);

            StackPanel titleLeft = CreateBrandHeaderWithTabs(_currentTab);
            Border topBar = BuildTopWindowBar(titleLeft, null);
            Grid.SetRow(topBar, 0);
            rootGrid.Children.Add(topBar);

            Border breadcrumbBar = new Border();
            breadcrumbBar.Background = UiTheme.BgHeader;
            breadcrumbBar.BorderBrush = UiTheme.BorderSubtle;
            breadcrumbBar.BorderThickness = new Thickness(0, 0, 0, 1);
            breadcrumbBar.Padding = new Thickness(20, 10, 20, 10);

            Grid headGrid = new Grid();
            ColumnDefinition cLeft = new ColumnDefinition(); cLeft.Width = GridLength.Auto;
            ColumnDefinition cSpacer = new ColumnDefinition(); cSpacer.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition cRight = new ColumnDefinition(); cRight.Width = GridLength.Auto;
            headGrid.ColumnDefinitions.Add(cLeft);
            headGrid.ColumnDefinitions.Add(cSpacer);
            headGrid.ColumnDefinitions.Add(cRight);

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

            StackPanel actionsStack = new StackPanel();
            actionsStack.Orientation = Orientation.Horizontal;
            actionsStack.VerticalAlignment = VerticalAlignment.Center;

            Button btnFolder = UiTheme.CreateActionButton(
                UiTheme.CreateFolderIcon(Brushes.White, 14),
                UiTheme.NeutralBtnHex,
                UiTheme.NeutralBtnHoverHex);
            btnFolder.Width = 32;
            btnFolder.Height = 28;
            btnFolder.Padding = new Thickness(0);
            btnFolder.Margin = new Thickness(0, 0, 6, 0);
            btnFolder.ToolTip = "Abrir pasta de logs no Windows Explorer: " + _activeLogFolderPath;
            btnFolder.Click += delegate
            {
                if (!string.IsNullOrEmpty(_activeLogFolderPath) && Directory.Exists(_activeLogFolderPath))
                    AbrirNoExplorer(_activeLogFolderPath);
            };
            actionsStack.Children.Add(btnFolder);

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
                ExportarArquivoLogAtual();
            };
            actionsStack.Children.Add(btnDownload);

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
                CopiarTextoLogParaClipboard();
            };
            actionsStack.Children.Add(btnCopyAll);

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
                AbrirLogNoEditorExterno();
            };
            actionsStack.Children.Add(btnOpenNotepad);

            Grid.SetColumn(actionsStack, 2);
            headGrid.Children.Add(actionsStack);

            breadcrumbBar.Child = headGrid;
            Grid.SetRow(breadcrumbBar, 1);
            rootGrid.Children.Add(breadcrumbBar);

            Border metricsBar = BuildLogMetricsCards();
            Grid.SetRow(metricsBar, 2);
            rootGrid.Children.Add(metricsBar);

            Border searchBar = new Border();
            searchBar.Background = UiTheme.BgWindow;
            searchBar.Padding = new Thickness(20, 2, 20, 8);

            Border searchContainer = new Border();
            searchContainer.Background = UiTheme.BgInput;
            searchContainer.BorderBrush = UiTheme.BorderSubtle;
            searchContainer.BorderThickness = new Thickness(1);
            searchContainer.CornerRadius = new CornerRadius(6);
            searchContainer.Padding = new Thickness(12, 4, 12, 4);
            searchContainer.Height = 30;

            DockPanel searchDock = new DockPanel();
            UIElement searchIcon = UiTheme.CreateSearchIcon();
            DockPanel.SetDock(searchIcon, Dock.Left);
            searchDock.Children.Add(searchIcon);

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
            searchDock.Children.Add(searchInner);
            searchContainer.Child = searchDock;
            searchBar.Child = searchContainer;

            Grid.SetRow(searchBar, 3);
            rootGrid.Children.Add(searchBar);

            Border logTabsBar = BuildLogFileTabs(logFiles);
            Grid.SetRow(logTabsBar, 4);
            rootGrid.Children.Add(logTabsBar);

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
            Grid.SetRow(consoleContainer, 5);
            rootGrid.Children.Add(consoleContainer);

            _pageHost.Content = rootGrid;

            RecarregarLogSelecionadoAsync();
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

            _logCardStatusValue = new TextBlock();
            _logCardStatusValue.Text = _activeLogPid > 0 ? "Ativo (PID " + _activeLogPid + ")" : "Parado / Offline";
            _logCardStatusValue.FontSize = 14;
            _logCardStatusValue.FontWeight = FontWeights.Bold;
            _logCardStatusValue.Foreground = _activeLogPid > 0 ? UiTheme.AccentGreen : UiTheme.TextMuted;
            Border cardStatus = CriarCardMetricaLog("🟢 Status / PID", _logCardStatusValue, "Identificador do processo no Windows");
            Grid.SetColumn(cardStatus, 4);
            grid.Children.Add(cardStatus);

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
                List<Action> updateTabVisuals = new List<Action>();

                foreach (string filePath in logFiles)
                {
                    string capturedPath = filePath;
                    string fileName = Path.GetFileName(capturedPath);

                    Border tabItem = new Border();
                    tabItem.Height = 32;
                    tabItem.Padding = new Thickness(14, 0, 14, 0);
                    tabItem.Margin = new Thickness(0, 0, 6, 0);
                    tabItem.CornerRadius = new CornerRadius(6, 6, 0, 0);
                    tabItem.Cursor = Cursors.Hand;
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
                    label.VerticalAlignment = VerticalAlignment.Center;
                    inner.Children.Add(label);

                    tabItem.Child = inner;

                    Action refreshTabState = delegate
                    {
                        bool isSelected = string.Equals(_activeLogFilePath, capturedPath, StringComparison.OrdinalIgnoreCase);
                        tabItem.Background = isSelected ? UiTheme.BrushFromHex("#060A10") : Brushes.Transparent;
                        tabItem.BorderBrush = isSelected ? UiTheme.BorderSubtle : Brushes.Transparent;
                        label.FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal;
                        label.Foreground = isSelected ? UiTheme.AccentLightBlue : UiTheme.TextSecondary;
                    };

                    refreshTabState();
                    updateTabVisuals.Add(refreshTabState);

                    tabItem.MouseLeftButtonUp += delegate
                    {
                        try
                        {
                            if (!string.Equals(_activeLogFilePath, capturedPath, StringComparison.OrdinalIgnoreCase))
                            {
                                _activeLogFilePath = capturedPath;
                                if (_logsBreadcrumbSub != null) _logsBreadcrumbSub.Text = fileName;
                                _activeLogLastFileLength = 0;
                                foreach (Action act in updateTabVisuals) act();
                                RecarregarLogSelecionadoAsync();
                            }
                        }
                        catch { }
                    };

                    tabsContainer.Children.Add(tabItem);
                }
            }

            bar.Child = tabsContainer;
            return bar;
        }
    }
}
