using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Path = System.IO.Path;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        public void ShowHomeOverviewView()
        {
            _isInDashboardView = false;
            _currentTab = HubActiveTab.HomeOverview;
            PararOverviewTimer();

            Grid rootGrid = new Grid();
            rootGrid.Background = UiTheme.BgWindow;
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            StackPanel titleLeft = CreateBrandHeaderWithTabs(HubActiveTab.HomeOverview);
            Border topBar = BuildTopWindowBar(titleLeft, null);
            Grid.SetRow(topBar, 0);
            rootGrid.Children.Add(topBar);

            Border headerBorder = new Border();
            headerBorder.Padding = new Thickness(20, 12, 20, 10);
            headerBorder.Background = UiTheme.BgHeader;
            headerBorder.BorderBrush = UiTheme.BorderSubtle;
            headerBorder.BorderThickness = new Thickness(0, 0, 0, 1);

            DockPanel headerDock = new DockPanel();

            StackPanel headerLeft = new StackPanel();
            headerLeft.VerticalAlignment = VerticalAlignment.Center;

            TextBlock titleMain = new TextBlock();
            titleMain.Text = "Monitoramento";
            titleMain.FontSize = 17;
            titleMain.FontWeight = FontWeights.Bold;
            titleMain.Foreground = UiTheme.TextPrimary;
            headerLeft.Children.Add(titleMain);

            TextBlock titleSub = new TextBlock();
            titleSub.Text = "Monitoramento consolidado de consumo e logs em tempo real dos serviços ativos";
            titleSub.FontSize = 11.5;
            titleSub.Foreground = UiTheme.TextMuted;
            titleSub.Margin = new Thickness(0, 2, 0, 0);
            headerLeft.Children.Add(titleSub);

            StackPanel headerRight = new StackPanel();
            headerRight.Orientation = Orientation.Horizontal;
            headerRight.VerticalAlignment = VerticalAlignment.Center;
            headerRight.HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(headerRight, Dock.Right);

            System.Windows.Shapes.Path spinIcon = (System.Windows.Shapes.Path)UiTheme.CreateThinRefreshIcon(UiTheme.AccentLightBlue);
            Button btnReload = UiTheme.CreateFramelessIconButton(spinIcon, "Atualizar dados e gráficos da Central");
            btnReload.HorizontalAlignment = HorizontalAlignment.Right;
            btnReload.Click += delegate
            {
                RotateTransform rt = spinIcon.RenderTransform as RotateTransform;
                if (rt == null) { rt = new RotateTransform(0); spinIcon.RenderTransform = rt; }
                DoubleAnimation spin = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(650));
                spin.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut };
                rt.BeginAnimation(RotateTransform.AngleProperty, spin);
                AtualizarListaOverview();
                CarregarLogOverviewAsync(true);
            };
            headerRight.Children.Add(btnReload);

            headerDock.Children.Add(headerRight);
            headerDock.Children.Add(headerLeft);
            headerBorder.Child = headerDock;

            Grid.SetRow(headerBorder, 1);
            rootGrid.Children.Add(headerBorder);

            Grid bodyGrid = new Grid();
            bodyGrid.Margin = new Thickness(20, 14, 20, 16);
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25, GridUnitType.Star) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25, GridUnitType.Star) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50, GridUnitType.Star) });

            ScrollViewer leftScroll = new ScrollViewer();
            leftScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            leftScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

            _overviewCardsStack = new StackPanel();
            leftScroll.Content = _overviewCardsStack;
            Grid.SetColumn(leftScroll, 0);
            bodyGrid.Children.Add(leftScroll);

            Border centerChartsPanel = CriarPainelConsumoGraficosOverview();
            Grid.SetColumn(centerChartsPanel, 2);
            bodyGrid.Children.Add(centerChartsPanel);

            Border rightLogPanel = CriarPainelLogsOverview();
            Grid.SetColumn(rightLogPanel, 4);
            bodyGrid.Children.Add(rightLogPanel);

            Grid.SetRow(bodyGrid, 2);
            rootGrid.Children.Add(bodyGrid);

            _pageHost.Content = rootGrid;

            AtualizarListaOverview();
            CarregarLogOverviewAsync(true);
            IniciarOverviewTimer();
        }

        private Border CriarPainelConsumoGraficosOverview()
        {
            Border panel = new Border();
            panel.Background = UiTheme.BgCard;
            panel.BorderBrush = UiTheme.BorderSubtle;
            panel.BorderThickness = new Thickness(1);
            panel.CornerRadius = new CornerRadius(10);
            panel.Padding = new Thickness(16, 14, 16, 14);

            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

            StackPanel sp = new StackPanel();

            TextBlock title = new TextBlock();
            title.Text = "Consumo Total";
            title.FontSize = 14;
            title.FontWeight = FontWeights.Bold;
            title.Foreground = UiTheme.TextPrimary;
            sp.Children.Add(title);

            TextBlock sub = new TextBlock();
            sub.Text = "Uso consolidado de hardware dos serviços em execução";
            sub.FontSize = 11;
            sub.Foreground = UiTheme.TextMuted;
            sub.Margin = new Thickness(0, 2, 0, 12);
            sp.Children.Add(sub);

            // Card 0: Gráficos de Pizza/Donut de Consumo Total
            Border cardDonuts = new Border();
            cardDonuts.Background = UiTheme.BrushFromHex("#0B0F19");
            cardDonuts.BorderBrush = UiTheme.BorderSubtle;
            cardDonuts.BorderThickness = new Thickness(1);
            cardDonuts.CornerRadius = new CornerRadius(8);
            cardDonuts.Padding = new Thickness(10, 10, 10, 10);
            cardDonuts.Margin = new Thickness(0, 0, 0, 10);

            Grid donutsGrid = new Grid();
            donutsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            donutsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            UIElement cpuDonut = CriarDonutChart(UiTheme.AccentLightBlue, out _overviewTotalCpuArc, out _overviewTotalCpuCenterText, "CPU Total", 100);
            Grid.SetColumn(cpuDonut as FrameworkElement, 0);
            donutsGrid.Children.Add(cpuDonut);

            UIElement ramDonut = CriarDonutChart(UiTheme.AccentGreen, out _overviewTotalRamArc, out _overviewTotalRamCenterText, "RAM Total", 100);
            Grid.SetColumn(ramDonut as FrameworkElement, 1);
            donutsGrid.Children.Add(ramDonut);

            cardDonuts.Child = donutsGrid;
            sp.Children.Add(cardDonuts);

            // Card 1: Consumo Total CPU
            Border cardCpu = new Border();
            cardCpu.Background = UiTheme.BrushFromHex("#0B0F19");
            cardCpu.BorderBrush = UiTheme.BorderSubtle;
            cardCpu.BorderThickness = new Thickness(1);
            cardCpu.CornerRadius = new CornerRadius(8);
            cardCpu.Padding = new Thickness(14, 12, 14, 14);
            cardCpu.Margin = new Thickness(0, 0, 0, 10);

            StackPanel spCpu = new StackPanel();

            DockPanel cpuTop = new DockPanel();
            cpuTop.Margin = new Thickness(0, 0, 0, 8);

            TextBlock lblCpu = new TextBlock();
            lblCpu.Text = "CPU CONSOLIDADA";
            lblCpu.FontSize = 10;
            lblCpu.FontWeight = FontWeights.Bold;
            lblCpu.Foreground = UiTheme.AccentLightBlue;
            DockPanel.SetDock(lblCpu, Dock.Left);
            cpuTop.Children.Add(lblCpu);

            _overviewTotalCpuSummaryText = new TextBlock();
            _overviewTotalCpuSummaryText.Text = "0.0%";
            _overviewTotalCpuSummaryText.FontSize = 16;
            _overviewTotalCpuSummaryText.FontWeight = FontWeights.Bold;
            _overviewTotalCpuSummaryText.Foreground = UiTheme.AccentLightBlue;
            _overviewTotalCpuSummaryText.HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(_overviewTotalCpuSummaryText, Dock.Right);
            cpuTop.Children.Add(_overviewTotalCpuSummaryText);
            spCpu.Children.Add(cpuTop);

            Border cpuTrack = new Border();
            cpuTrack.Height = 8;
            cpuTrack.Background = UiTheme.BrushFromHex("#1E293B");
            cpuTrack.CornerRadius = new CornerRadius(4);
            cpuTrack.Margin = new Thickness(0, 0, 0, 8);

            Grid cpuGrid = new Grid();
            _overviewTotalCpuColFill = new ColumnDefinition { Width = new GridLength(0.01, GridUnitType.Star) };
            _overviewTotalCpuColEmpty = new ColumnDefinition { Width = new GridLength(100, GridUnitType.Star) };
            cpuGrid.ColumnDefinitions.Add(_overviewTotalCpuColFill);
            cpuGrid.ColumnDefinitions.Add(_overviewTotalCpuColEmpty);

            Border cpuFill = new Border();
            cpuFill.Background = UiTheme.AccentLightBlue;
            cpuFill.CornerRadius = new CornerRadius(4);
            Grid.SetColumn(cpuFill, 0);
            cpuGrid.Children.Add(cpuFill);
            cpuTrack.Child = cpuGrid;
            spCpu.Children.Add(cpuTrack);

            _overviewTotalCpuDetailsText = new TextBlock();
            _overviewTotalCpuDetailsText.Text = "0 serviços ativos consumindo processamento";
            _overviewTotalCpuDetailsText.FontSize = 11;
            _overviewTotalCpuDetailsText.Foreground = UiTheme.TextMuted;
            spCpu.Children.Add(_overviewTotalCpuDetailsText);

            cardCpu.Child = spCpu;
            sp.Children.Add(cardCpu);

            // Card 2: Consumo Total RAM
            Border cardRam = new Border();
            cardRam.Background = UiTheme.BrushFromHex("#0B0F19");
            cardRam.BorderBrush = UiTheme.BorderSubtle;
            cardRam.BorderThickness = new Thickness(1);
            cardRam.CornerRadius = new CornerRadius(8);
            cardRam.Padding = new Thickness(14, 12, 14, 14);
            cardRam.Margin = new Thickness(0, 0, 0, 10);

            StackPanel spRam = new StackPanel();

            DockPanel ramTop = new DockPanel();
            ramTop.Margin = new Thickness(0, 0, 0, 8);

            TextBlock lblRam = new TextBlock();
            lblRam.Text = "MEMÓRIA RAM TOTAL";
            lblRam.FontSize = 10;
            lblRam.FontWeight = FontWeights.Bold;
            lblRam.Foreground = UiTheme.AccentGreen;
            DockPanel.SetDock(lblRam, Dock.Left);
            ramTop.Children.Add(lblRam);

            _overviewTotalRamSummaryText = new TextBlock();
            _overviewTotalRamSummaryText.Text = "0 MB";
            _overviewTotalRamSummaryText.FontSize = 16;
            _overviewTotalRamSummaryText.FontWeight = FontWeights.Bold;
            _overviewTotalRamSummaryText.Foreground = UiTheme.AccentGreen;
            _overviewTotalRamSummaryText.HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(_overviewTotalRamSummaryText, Dock.Right);
            ramTop.Children.Add(_overviewTotalRamSummaryText);
            spRam.Children.Add(ramTop);

            Border ramTrack = new Border();
            ramTrack.Height = 8;
            ramTrack.Background = UiTheme.BrushFromHex("#1E293B");
            ramTrack.CornerRadius = new CornerRadius(4);
            ramTrack.Margin = new Thickness(0, 0, 0, 8);

            Grid ramGrid = new Grid();
            _overviewTotalRamColFill = new ColumnDefinition { Width = new GridLength(0.01, GridUnitType.Star) };
            _overviewTotalRamColEmpty = new ColumnDefinition { Width = new GridLength(100, GridUnitType.Star) };
            ramGrid.ColumnDefinitions.Add(_overviewTotalRamColFill);
            ramGrid.ColumnDefinitions.Add(_overviewTotalRamColEmpty);

            Border ramFill = new Border();
            ramFill.Background = UiTheme.AccentGreen;
            ramFill.CornerRadius = new CornerRadius(4);
            Grid.SetColumn(ramFill, 0);
            ramGrid.Children.Add(ramFill);
            ramTrack.Child = ramGrid;
            spRam.Children.Add(ramTrack);

            _overviewTotalRamDetailsText = new TextBlock();
            _overviewTotalRamDetailsText.Text = "0 MB alocados entre os serviços ativos";
            _overviewTotalRamDetailsText.FontSize = 11;
            _overviewTotalRamDetailsText.Foreground = UiTheme.TextMuted;
            spRam.Children.Add(_overviewTotalRamDetailsText);

            cardRam.Child = spRam;
            sp.Children.Add(cardRam);

            // Card 3: Resumo dos Serviços Online
            Border cardSummary = new Border();
            cardSummary.Background = UiTheme.BrushFromHex("#0B0F19");
            cardSummary.BorderBrush = UiTheme.BorderSubtle;
            cardSummary.BorderThickness = new Thickness(1);
            cardSummary.CornerRadius = new CornerRadius(8);
            cardSummary.Padding = new Thickness(14, 12, 14, 14);

            StackPanel spSummary = new StackPanel();

            TextBlock lblSum = new TextBlock();
            lblSum.Text = "SERVIÇOS EM EXECUÇÃO";
            lblSum.FontSize = 10;
            lblSum.FontWeight = FontWeights.Bold;
            lblSum.Foreground = UiTheme.TextSecondary;
            lblSum.Margin = new Thickness(0, 0, 0, 10);
            spSummary.Children.Add(lblSum);

            Grid sumGrid = new Grid();
            sumGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sumGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sumGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel sumApp = new StackPanel();
            TextBlock lblAppTitle = new TextBlock { Text = "AppServer", FontSize = 10, Foreground = UiTheme.TextMuted };
            _overviewSummaryAppText = new TextBlock { Text = "0", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = UiTheme.AccentLightBlue, Margin = new Thickness(0, 2, 0, 0) };
            sumApp.Children.Add(lblAppTitle);
            sumApp.Children.Add(_overviewSummaryAppText);
            Grid.SetColumn(sumApp, 0);
            sumGrid.Children.Add(sumApp);

            StackPanel sumDb = new StackPanel();
            TextBlock lblDbTitle = new TextBlock { Text = "DBAccess", FontSize = 10, Foreground = UiTheme.TextMuted };
            _overviewSummaryDbText = new TextBlock { Text = "Inativo", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = UiTheme.TextSecondary, Margin = new Thickness(0, 2, 0, 0) };
            sumDb.Children.Add(lblDbTitle);
            sumDb.Children.Add(_overviewSummaryDbText);
            Grid.SetColumn(sumDb, 1);
            sumGrid.Children.Add(sumDb);

            StackPanel sumSv = new StackPanel();
            TextBlock lblSvTitle = new TextBlock { Text = "SmartView", FontSize = 10, Foreground = UiTheme.TextMuted };
            _overviewSummarySvText = new TextBlock { Text = "Inativo", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = UiTheme.TextSecondary, Margin = new Thickness(0, 2, 0, 0) };
            sumSv.Children.Add(lblSvTitle);
            sumSv.Children.Add(_overviewSummarySvText);
            Grid.SetColumn(sumSv, 2);
            sumGrid.Children.Add(sumSv);

            spSummary.Children.Add(sumGrid);
            cardSummary.Child = spSummary;
            sp.Children.Add(cardSummary);

            scroll.Content = sp;
            panel.Child = scroll;
            return panel;
        }

        private Border CriarPainelLogsOverview()
        {
            Border panel = new Border();
            panel.Background = UiTheme.BgCard;
            panel.BorderBrush = UiTheme.BorderSubtle;
            panel.BorderThickness = new Thickness(1);
            panel.CornerRadius = new CornerRadius(10);
            panel.Padding = new Thickness(14);

            Grid g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            DockPanel topDock = new DockPanel();
            topDock.Margin = new Thickness(0, 0, 0, 8);

            StackPanel titles = new StackPanel();
            titles.VerticalAlignment = VerticalAlignment.Center;

            _overviewLogTitle = new TextBlock();
            _overviewLogTitle.Text = "Log em Tempo Real";
            _overviewLogTitle.FontSize = 14;
            _overviewLogTitle.FontWeight = FontWeights.Bold;
            _overviewLogTitle.Foreground = UiTheme.TextPrimary;
            _overviewLogTitle.TextTrimming = TextTrimming.CharacterEllipsis;
            titles.Children.Add(_overviewLogTitle);

            _overviewLogPathText = new TextBlock();
            _overviewLogPathText.Text = "Selecione um serviço ativo para acompanhar o log";
            _overviewLogPathText.FontSize = 11;
            _overviewLogPathText.Foreground = UiTheme.TextMuted;
            _overviewLogPathText.Margin = new Thickness(0, 2, 0, 0);
            _overviewLogPathText.TextTrimming = TextTrimming.CharacterEllipsis;
            titles.Children.Add(_overviewLogPathText);

            StackPanel actions = new StackPanel();
            actions.Orientation = Orientation.Horizontal;
            actions.VerticalAlignment = VerticalAlignment.Center;
            actions.HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(actions, Dock.Right);

            _overviewAutoScrollBtn = UiTheme.CreateActionButton(
                UiTheme.CreateScrollIcon(Brushes.White, 13),
                _overviewAutoScroll ? "#0284C7" : UiTheme.NeutralBtnHex,
                _overviewAutoScroll ? "#0369A1" : UiTheme.NeutralBtnHoverHex);
            _overviewAutoScrollBtn.Width = 30;
            _overviewAutoScrollBtn.Height = 26;
            _overviewAutoScrollBtn.Padding = new Thickness(0);
            _overviewAutoScrollBtn.Margin = new Thickness(0, 0, 4, 0);
            _overviewAutoScrollBtn.ToolTip = _overviewAutoScroll ? "Auto-scroll: Ativado (Clique para pausar)" : "Auto-scroll: Pausado (Clique para ativar)";
            _overviewAutoScrollBtn.Click += delegate
            {
                _overviewAutoScroll = !_overviewAutoScroll;
                _overviewAutoScrollBtn.Background = UiTheme.BrushFromHex(_overviewAutoScroll ? "#0284C7" : UiTheme.NeutralBtnHex);
                _overviewAutoScrollBtn.ToolTip = _overviewAutoScroll ? "Auto-scroll: Ativado (Clique para pausar)" : "Auto-scroll: Pausado (Clique para ativar)";
                if (_overviewAutoScroll && _overviewLogConsole != null)
                {
                    double hOffset = _overviewLogConsole.HorizontalOffset;
                    _overviewLogConsole.ScrollToVerticalOffset(_overviewLogConsole.ExtentHeight);
                    if (hOffset > 0) _overviewLogConsole.ScrollToHorizontalOffset(hOffset);
                }
            };
            actions.Children.Add(_overviewAutoScrollBtn);

            Button btnCopy = UiTheme.CreateActionButton(
                UiTheme.CreateCopyIcon(Brushes.White, 13),
                UiTheme.NeutralBtnHex,
                UiTheme.NeutralBtnHoverHex);
            btnCopy.Width = 30;
            btnCopy.Height = 26;
            btnCopy.Padding = new Thickness(0);
            btnCopy.Margin = new Thickness(0, 0, 4, 0);
            btnCopy.ToolTip = "Copiar todo o log";
            btnCopy.Click += delegate
            {
                try
                {
                    if (_overviewLogConsole != null && !string.IsNullOrEmpty(_overviewLogConsole.Text))
                        Clipboard.SetText(_overviewLogConsole.Text);
                }
                catch { }
            };
            actions.Children.Add(btnCopy);

            Button btnOpenFolder = UiTheme.CreateActionButton(
                UiTheme.CreateFolderIcon(Brushes.White, 13),
                UiTheme.NeutralBtnHex,
                UiTheme.NeutralBtnHoverHex);
            btnOpenFolder.Width = 30;
            btnOpenFolder.Height = 26;
            btnOpenFolder.Padding = new Thickness(0);
            btnOpenFolder.ToolTip = "Abrir pasta de logs no Windows Explorer";
            btnOpenFolder.Click += delegate
            {
                if (!string.IsNullOrEmpty(_selectedOverviewLogPath) && Directory.Exists(_selectedOverviewLogPath))
                    AbrirNoExplorer(_selectedOverviewLogPath);
            };
            actions.Children.Add(btnOpenFolder);

            topDock.Children.Add(actions);
            topDock.Children.Add(titles);
            Grid.SetRow(topDock, 0);
            g.Children.Add(topDock);

            Border searchBorder = new Border();
            searchBorder.Background = UiTheme.BgInput;
            searchBorder.BorderBrush = UiTheme.BorderSubtle;
            searchBorder.BorderThickness = new Thickness(1);
            searchBorder.CornerRadius = new CornerRadius(6);
            searchBorder.Padding = new Thickness(10, 4, 10, 4);
            searchBorder.Height = 30;
            searchBorder.Margin = new Thickness(0, 0, 0, 10);

            DockPanel searchDock = new DockPanel();
            UIElement searchIcon = UiTheme.CreateSearchIcon();
            DockPanel.SetDock(searchIcon, Dock.Left);
            searchDock.Children.Add(searchIcon);

            Grid searchInner = new Grid();
            TextBlock searchPlaceholder = new TextBlock();
            searchPlaceholder.Text = "Digite aqui o trecho do log para procurar...";
            searchPlaceholder.Foreground = UiTheme.TextMuted;
            searchPlaceholder.FontSize = 12;
            searchPlaceholder.VerticalAlignment = VerticalAlignment.Center;
            searchPlaceholder.IsHitTestVisible = false;

            _overviewSearchBox = new TextBox();
            _overviewSearchBox.Background = Brushes.Transparent;
            _overviewSearchBox.Foreground = UiTheme.TextPrimary;
            _overviewSearchBox.CaretBrush = UiTheme.TextPrimary;
            _overviewSearchBox.BorderThickness = new Thickness(0);
            _overviewSearchBox.FontSize = 12;
            _overviewSearchBox.VerticalAlignment = VerticalAlignment.Center;
            _overviewSearchBox.ToolTip = "Digite aqui o trecho do log para procurar";
            _overviewSearchBox.TextChanged += delegate
            {
                searchPlaceholder.Visibility = string.IsNullOrEmpty(_overviewSearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
                _overviewFilterText = _overviewSearchBox.Text;
                FiltrarLinhasLogOverview();
            };

            searchInner.Children.Add(searchPlaceholder);
            searchInner.Children.Add(_overviewSearchBox);
            searchDock.Children.Add(searchInner);
            searchBorder.Child = searchDock;

            Grid.SetRow(searchBorder, 1);
            g.Children.Add(searchBorder);

            Border consoleBorder = new Border();
            consoleBorder.Background = UiTheme.BrushFromHex("#0B0F19");
            consoleBorder.BorderBrush = UiTheme.BorderSubtle;
            consoleBorder.BorderThickness = new Thickness(1);
            consoleBorder.CornerRadius = new CornerRadius(6);
            consoleBorder.Padding = new Thickness(10);

            _overviewLogConsole = new TextBox();
            _overviewLogConsole.Background = Brushes.Transparent;
            _overviewLogConsole.Foreground = UiTheme.BrushFromHex("#E2E8F0");
            _overviewLogConsole.CaretBrush = Brushes.White;
            _overviewLogConsole.BorderThickness = new Thickness(0);
            _overviewLogConsole.FontFamily = new FontFamily("Consolas, Courier New, monospace");
            _overviewLogConsole.FontSize = 12;
            _overviewLogConsole.IsReadOnly = true;
            _overviewLogConsole.TextWrapping = TextWrapping.NoWrap;
            _overviewLogConsole.AcceptsReturn = true;
            _overviewLogConsole.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _overviewLogConsole.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            consoleBorder.Child = _overviewLogConsole;
            Grid.SetRow(consoleBorder, 2);
            g.Children.Add(consoleBorder);

            panel.Child = g;
            return panel;
        }

        private Border CriarCardServicoOverview(OverviewServiceItem item)
        {
            Border card = new Border();
            card.Background = UiTheme.BgCard;
            card.BorderBrush = UiTheme.BorderSubtle;
            card.BorderThickness = new Thickness(1);
            card.CornerRadius = new CornerRadius(10);
            card.Padding = new Thickness(14, 12, 14, 12);
            card.Margin = new Thickness(0, 0, 0, 10);
            card.Cursor = Cursors.Hand;

            _overviewCardBorders[item.Id] = card;

            Grid g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            DockPanel topDock = new DockPanel();

            Border badgeRunning = new Border();
            badgeRunning.Background = UiTheme.BrushFromHex("#064E3B");
            badgeRunning.BorderBrush = UiTheme.AccentGreen;
            badgeRunning.BorderThickness = new Thickness(1);
            badgeRunning.CornerRadius = new CornerRadius(8);
            badgeRunning.Padding = new Thickness(6, 2, 6, 2);
            badgeRunning.Margin = new Thickness(0, 0, 8, 0);

            TextBlock badgeTxt = new TextBlock();
            badgeTxt.Text = "● RUNNING";
            badgeTxt.FontSize = 10;
            badgeTxt.FontWeight = FontWeights.Bold;
            badgeTxt.Foreground = UiTheme.AccentGreen;
            badgeRunning.Child = badgeTxt;
            DockPanel.SetDock(badgeRunning, Dock.Left);
            topDock.Children.Add(badgeRunning);

            if (!string.Equals(item.Name, item.TypeLabel, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.Name, item.TypeHint, StringComparison.OrdinalIgnoreCase))
            {
                Border typeBadge = new Border();
                typeBadge.Background = UiTheme.TagBg;
                typeBadge.BorderBrush = UiTheme.BorderSubtle;
                typeBadge.BorderThickness = new Thickness(1);
                typeBadge.CornerRadius = new CornerRadius(8);
                typeBadge.Padding = new Thickness(6, 2, 6, 2);

                TextBlock typeTxt = new TextBlock();
                typeTxt.Text = item.TypeLabel;
                typeTxt.FontSize = 10;
                typeTxt.FontWeight = FontWeights.SemiBold;
                typeTxt.Foreground = string.Equals(item.TypeHint, "dbaccess", StringComparison.OrdinalIgnoreCase)
                    ? UiTheme.AccentAmber
                    : string.Equals(item.TypeHint, "smartview", StringComparison.OrdinalIgnoreCase)
                        ? UiTheme.BrushFromHex("#A855F7")
                        : UiTheme.AccentLightBlue;
                typeBadge.Child = typeTxt;
                DockPanel.SetDock(typeBadge, Dock.Right);
                topDock.Children.Add(typeBadge);
            }

            TextBlock title = new TextBlock();
            title.Text = item.Name;
            title.FontSize = 14;
            title.FontWeight = FontWeights.Bold;
            title.Foreground = UiTheme.TextPrimary;
            title.VerticalAlignment = VerticalAlignment.Center;
            title.TextTrimming = TextTrimming.CharacterEllipsis;
            topDock.Children.Add(title);

            Grid.SetRow(topDock, 0);
            g.Children.Add(topDock);

            StackPanel metaRow = new StackPanel();
            metaRow.Orientation = Orientation.Horizontal;
            metaRow.Margin = new Thickness(0, 6, 0, 8);

            TextBlock pidTxt = new TextBlock();
            pidTxt.Text = string.Format("PID: {0}", item.Pid > 0 ? item.Pid.ToString() : "-");
            pidTxt.FontSize = 11;
            pidTxt.Foreground = UiTheme.TextMuted;
            pidTxt.Margin = new Thickness(0, 0, 12, 0);
            metaRow.Children.Add(pidTxt);

            if (!string.IsNullOrEmpty(item.Port))
            {
                TextBlock portTxt = new TextBlock();
                portTxt.Text = string.Format("Porta: {0}", item.Port);
                portTxt.FontSize = 11;
                portTxt.Foreground = UiTheme.TextMuted;
                metaRow.Children.Add(portTxt);
            }

            Grid.SetRow(metaRow, 1);
            g.Children.Add(metaRow);

            Grid metricsGrid = new Grid();
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel cpuStack = new StackPanel();
            DockPanel cpuHeader = new DockPanel();
            TextBlock cpuLbl = new TextBlock();
            cpuLbl.Text = "CPU";
            cpuLbl.FontSize = 11;
            cpuLbl.FontWeight = FontWeights.SemiBold;
            cpuLbl.Foreground = UiTheme.TextSecondary;
            DockPanel.SetDock(cpuLbl, Dock.Left);
            cpuHeader.Children.Add(cpuLbl);

            TextBlock cpuVal = new TextBlock();
            cpuVal.Text = string.Format("{0:0.0}%", item.CpuPercent);
            cpuVal.FontSize = 11;
            cpuVal.FontWeight = FontWeights.Bold;
            cpuVal.Foreground = UiTheme.AccentLightBlue;
            cpuVal.HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(cpuVal, Dock.Right);
            cpuHeader.Children.Add(cpuVal);
            cpuStack.Children.Add(cpuHeader);

            Border cpuBarTrack = new Border();
            cpuBarTrack.Height = 6;
            cpuBarTrack.Background = UiTheme.BrushFromHex("#0F172A");
            cpuBarTrack.CornerRadius = new CornerRadius(3);
            cpuBarTrack.Margin = new Thickness(0, 4, 0, 0);

            Border cpuBarFill = new Border();
            cpuBarFill.Height = 6;
            cpuBarFill.Background = UiTheme.AccentLightBlue;
            cpuBarFill.CornerRadius = new CornerRadius(3);
            cpuBarFill.HorizontalAlignment = HorizontalAlignment.Left;
            double cpuWidth = Math.Max(2, Math.Min(180, (item.CpuPercent / 100.0) * 180));
            cpuBarFill.Width = cpuWidth;
            cpuBarTrack.Child = cpuBarFill;
            cpuStack.Children.Add(cpuBarTrack);

            _overviewCpuTexts[item.Id] = cpuVal;
            _overviewCpuBars[item.Id] = cpuBarFill;

            Grid.SetColumn(cpuStack, 0);
            metricsGrid.Children.Add(cpuStack);

            StackPanel ramStack = new StackPanel();
            DockPanel ramHeader = new DockPanel();
            TextBlock ramLbl = new TextBlock();
            ramLbl.Text = "RAM";
            ramLbl.FontSize = 11;
            ramLbl.FontWeight = FontWeights.SemiBold;
            ramLbl.Foreground = UiTheme.TextSecondary;
            DockPanel.SetDock(ramLbl, Dock.Left);
            ramHeader.Children.Add(ramLbl);

            TextBlock ramVal = new TextBlock();
            ramVal.Text = string.Format("{0} MB", item.RamMb);
            ramVal.FontSize = 11;
            ramVal.FontWeight = FontWeights.Bold;
            ramVal.Foreground = UiTheme.AccentGreen;
            ramVal.HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(ramVal, Dock.Right);
            ramHeader.Children.Add(ramVal);
            ramStack.Children.Add(ramHeader);

            Border ramBarTrack = new Border();
            ramBarTrack.Height = 6;
            ramBarTrack.Background = UiTheme.BrushFromHex("#0F172A");
            ramBarTrack.CornerRadius = new CornerRadius(3);
            ramBarTrack.Margin = new Thickness(0, 4, 0, 0);

            Border ramBarFill = new Border();
            ramBarFill.Height = 6;
            ramBarFill.Background = UiTheme.AccentGreen;
            ramBarFill.CornerRadius = new CornerRadius(3);
            ramBarFill.HorizontalAlignment = HorizontalAlignment.Left;
            double ramWidth = Math.Max(2, Math.Min(180, (item.RamMb / 2048.0) * 180));
            ramBarFill.Width = ramWidth;
            ramBarTrack.Child = ramBarFill;
            ramStack.Children.Add(ramBarTrack);

            _overviewRamTexts[item.Id] = ramVal;
            _overviewRamBars[item.Id] = ramBarFill;

            Grid.SetColumn(ramStack, 2);
            metricsGrid.Children.Add(ramStack);

            Grid.SetRow(metricsGrid, 2);
            g.Children.Add(metricsGrid);

            card.Child = g;

            card.MouseLeftButtonUp += delegate
            {
                _selectedOverviewServiceId = item.Id;
                _selectedOverviewServiceName = item.Name;
                _selectedOverviewLogPath = item.FolderPath;
                _selectedOverviewPid = item.Pid;
                _selectedOverviewHint = item.TypeHint;
                AtualizarBordasSelecaoOverview();
                CarregarLogOverviewAsync(true);
            };

            return card;
        }

        public void ShowSkeletonLoadingView()
        {
            _isInDashboardView = false;
            _currentTab = HubActiveTab.HomeOverview;
            PararOverviewTimer();

            Grid rootGrid = new Grid();
            rootGrid.Background = UiTheme.BgWindow;
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            StackPanel titleLeft = CreateBrandHeaderWithTabs(HubActiveTab.HomeOverview);
            Border topBar = BuildTopWindowBar(titleLeft, null);
            Grid.SetRow(topBar, 0);
            rootGrid.Children.Add(topBar);

            Border headerBorder = new Border();
            headerBorder.Padding = new Thickness(20, 12, 20, 10);
            headerBorder.Background = UiTheme.BgHeader;
            headerBorder.BorderBrush = UiTheme.BorderSubtle;
            headerBorder.BorderThickness = new Thickness(0, 0, 0, 1);

            DockPanel headerDock = new DockPanel();

            StackPanel headerLeft = new StackPanel();
            headerLeft.VerticalAlignment = VerticalAlignment.Center;

            TextBlock titleMain = new TextBlock();
            titleMain.Text = "Monitoramento";
            titleMain.FontSize = 17;
            titleMain.FontWeight = FontWeights.Bold;
            titleMain.Foreground = UiTheme.TextPrimary;
            headerLeft.Children.Add(titleMain);

            TextBlock titleSub = new TextBlock();
            titleSub.Text = "Verificando serviços e processos ativos no sistema...";
            titleSub.FontSize = 11.5;
            titleSub.Foreground = UiTheme.AccentLightBlue;
            titleSub.Margin = new Thickness(0, 2, 0, 0);
            headerLeft.Children.Add(titleSub);

            headerDock.Children.Add(headerLeft);
            headerBorder.Child = headerDock;

            Grid.SetRow(headerBorder, 1);
            rootGrid.Children.Add(headerBorder);

            Grid bodyGrid = new Grid();
            bodyGrid.Margin = new Thickness(20, 14, 20, 16);
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25, GridUnitType.Star) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25, GridUnitType.Star) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50, GridUnitType.Star) });

            // Col 0: Skeleton Left Column (Service cards placeholders)
            StackPanel leftCol = new StackPanel();
            for (int i = 0; i < 3; i++)
            {
                leftCol.Children.Add(CriarSkeletonCardServico());
            }
            bodyGrid.Children.Add(leftCol);
            Grid.SetColumn(leftCol, 0);

            // Col 2: Skeleton Center Column (Consumo Total with donut placeholders and bars)
            Border centerSkeleton = CriarSkeletonPainelConsumo();
            Grid.SetColumn(centerSkeleton, 2);
            bodyGrid.Children.Add(centerSkeleton);

            // Col 4: Skeleton Right Column (Terminal / Log loading state with pulsing spinner and informative status)
            Border rightSkeleton = CriarSkeletonPainelLogs();
            Grid.SetColumn(rightSkeleton, 4);
            bodyGrid.Children.Add(rightSkeleton);

            Grid.SetRow(bodyGrid, 2);
            rootGrid.Children.Add(bodyGrid);

            DoubleAnimation pulse = new DoubleAnimation(0.35, 0.85, TimeSpan.FromMilliseconds(900));
            pulse.AutoReverse = true;
            pulse.RepeatBehavior = RepeatBehavior.Forever;
            pulse.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            bodyGrid.BeginAnimation(UIElement.OpacityProperty, pulse);

            _pageHost.Content = rootGrid;
        }

        private Border CriarSkeletonBar(double width, double height, double cornerRadius = 4, Brush fill = null)
        {
            Border b = new Border();
            if (!double.IsNaN(width)) b.Width = width;
            if (!double.IsNaN(height)) b.Height = height;
            b.CornerRadius = new CornerRadius(cornerRadius);
            b.Background = fill ?? UiTheme.BrushFromHex("#1E293B");
            return b;
        }

        private Border CriarSkeletonCardServico()
        {
            Border card = new Border();
            card.Background = UiTheme.BgCard;
            card.BorderBrush = UiTheme.BorderSubtle;
            card.BorderThickness = new Thickness(1);
            card.CornerRadius = new CornerRadius(8);
            card.Padding = new Thickness(12, 10, 12, 10);
            card.Margin = new Thickness(0, 0, 0, 8);

            StackPanel sp = new StackPanel();
            DockPanel top = new DockPanel();
            top.Margin = new Thickness(0, 0, 0, 8);

            StackPanel leftTitle = new StackPanel { Orientation = Orientation.Horizontal };
            leftTitle.Children.Add(CriarSkeletonBar(8, 8, 4, UiTheme.BrushFromHex("#334155")));
            leftTitle.Children.Add(new Border { Width = 8 });
            leftTitle.Children.Add(CriarSkeletonBar(90, 12, 4, UiTheme.BrushFromHex("#334155")));
            DockPanel.SetDock(leftTitle, Dock.Left);
            top.Children.Add(leftTitle);

            Border badgeSkel = CriarSkeletonBar(55, 14, 4, UiTheme.BrushFromHex("#334155"));
            badgeSkel.HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(badgeSkel, Dock.Right);
            top.Children.Add(badgeSkel);
            sp.Children.Add(top);

            Border pathSkel = CriarSkeletonBar(140, 9, 3, UiTheme.BrushFromHex("#1E293B"));
            pathSkel.Margin = new Thickness(0, 0, 0, 8);
            sp.Children.Add(pathSkel);

            Border bar1 = CriarSkeletonBar(double.NaN, 6, 3, UiTheme.BrushFromHex("#1E293B"));
            bar1.Margin = new Thickness(0, 0, 0, 4);
            sp.Children.Add(bar1);

            Border bar2 = CriarSkeletonBar(double.NaN, 6, 3, UiTheme.BrushFromHex("#1E293B"));
            sp.Children.Add(bar2);

            card.Child = sp;
            return card;
        }

        private Border CriarSkeletonPainelConsumo()
        {
            Border panel = new Border();
            panel.Background = UiTheme.BgCard;
            panel.BorderBrush = UiTheme.BorderSubtle;
            panel.BorderThickness = new Thickness(1);
            panel.CornerRadius = new CornerRadius(10);
            panel.Padding = new Thickness(16, 14, 16, 14);

            StackPanel sp = new StackPanel();
            sp.Children.Add(CriarSkeletonBar(120, 14, 4, UiTheme.BrushFromHex("#334155")));
            Border subBar = CriarSkeletonBar(200, 10, 3, UiTheme.BrushFromHex("#1E293B"));
            subBar.Margin = new Thickness(0, 4, 0, 12);
            sp.Children.Add(subBar);

            Border cardDonuts = new Border();
            cardDonuts.Background = UiTheme.BrushFromHex("#0B0F19");
            cardDonuts.BorderBrush = UiTheme.BorderSubtle;
            cardDonuts.BorderThickness = new Thickness(1);
            cardDonuts.CornerRadius = new CornerRadius(8);
            cardDonuts.Padding = new Thickness(10);
            cardDonuts.Margin = new Thickness(0, 0, 0, 10);

            Grid donutsGrid = new Grid();
            donutsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            donutsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            System.Windows.Shapes.Ellipse ring1 = new System.Windows.Shapes.Ellipse { Width = 88, Height = 88, Stroke = UiTheme.BrushFromHex("#1E293B"), StrokeThickness = 8 };
            Grid.SetColumn(ring1, 0);
            donutsGrid.Children.Add(ring1);

            System.Windows.Shapes.Ellipse ring2 = new System.Windows.Shapes.Ellipse { Width = 88, Height = 88, Stroke = UiTheme.BrushFromHex("#1E293B"), StrokeThickness = 8 };
            Grid.SetColumn(ring2, 1);
            donutsGrid.Children.Add(ring2);

            cardDonuts.Child = donutsGrid;
            sp.Children.Add(cardDonuts);

            for (int i = 0; i < 2; i++)
            {
                Border cardBar = new Border();
                cardBar.Background = UiTheme.BrushFromHex("#0B0F19");
                cardBar.BorderBrush = UiTheme.BorderSubtle;
                cardBar.BorderThickness = new Thickness(1);
                cardBar.CornerRadius = new CornerRadius(8);
                cardBar.Padding = new Thickness(14, 12, 14, 14);
                cardBar.Margin = new Thickness(0, 0, 0, 10);

                StackPanel spCard = new StackPanel();
                spCard.Children.Add(CriarSkeletonBar(100, 10, 3, UiTheme.BrushFromHex("#334155")));
                Border track = CriarSkeletonBar(double.NaN, 8, 4, UiTheme.BrushFromHex("#1E293B"));
                track.Margin = new Thickness(0, 8, 0, 8);
                spCard.Children.Add(track);
                spCard.Children.Add(CriarSkeletonBar(150, 9, 3, UiTheme.BrushFromHex("#1E293B")));

                cardBar.Child = spCard;
                sp.Children.Add(cardBar);
            }

            panel.Child = sp;
            return panel;
        }

        private Border CriarSkeletonPainelLogs()
        {
            Border panel = new Border();
            panel.Background = UiTheme.BgCard;
            panel.BorderBrush = UiTheme.BorderSubtle;
            panel.BorderThickness = new Thickness(1);
            panel.CornerRadius = new CornerRadius(10);
            panel.Padding = new Thickness(16, 14, 16, 14);

            Grid g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            DockPanel topDock = new DockPanel();
            topDock.Margin = new Thickness(0, 0, 0, 10);
            StackPanel titles = new StackPanel();
            titles.Children.Add(CriarSkeletonBar(160, 13, 4, UiTheme.BrushFromHex("#334155")));
            Border pathSkel = CriarSkeletonBar(240, 9, 3, UiTheme.BrushFromHex("#1E293B"));
            pathSkel.Margin = new Thickness(0, 4, 0, 0);
            titles.Children.Add(pathSkel);
            topDock.Children.Add(titles);
            Grid.SetRow(topDock, 0);
            g.Children.Add(topDock);

            Border searchSkel = CriarSkeletonBar(double.NaN, 28, 6, UiTheme.BrushFromHex("#0B0F19"));
            searchSkel.Margin = new Thickness(0, 0, 0, 10);
            Grid.SetRow(searchSkel, 1);
            g.Children.Add(searchSkel);

            Border consoleBg = new Border();
            consoleBg.Background = UiTheme.BrushFromHex("#050811");
            consoleBg.BorderBrush = UiTheme.BorderSubtle;
            consoleBg.BorderThickness = new Thickness(1);
            consoleBg.CornerRadius = new CornerRadius(8);

            StackPanel centerInfo = new StackPanel();
            centerInfo.HorizontalAlignment = HorizontalAlignment.Center;
            centerInfo.VerticalAlignment = VerticalAlignment.Center;

            Grid spinnerBox = new Grid();
            spinnerBox.Width = 44;
            spinnerBox.Height = 44;
            spinnerBox.HorizontalAlignment = HorizontalAlignment.Center;
            spinnerBox.Margin = new Thickness(0, 0, 0, 14);

            System.Windows.Shapes.Ellipse spinTrack = new System.Windows.Shapes.Ellipse();
            spinTrack.Width = 38;
            spinTrack.Height = 38;
            spinTrack.Stroke = UiTheme.BrushFromHex("#1E293B");
            spinTrack.StrokeThickness = 3;
            spinTrack.HorizontalAlignment = HorizontalAlignment.Center;
            spinTrack.VerticalAlignment = VerticalAlignment.Center;
            spinnerBox.Children.Add(spinTrack);

            System.Windows.Shapes.Path spinHead = new System.Windows.Shapes.Path();
            spinHead.Width = 38;
            spinHead.Height = 38;
            spinHead.Stroke = UiTheme.AccentLightBlue;
            spinHead.StrokeThickness = 3;
            spinHead.StrokeStartLineCap = PenLineCap.Round;
            spinHead.StrokeEndLineCap = PenLineCap.Round;

            PathFigure sfig = new PathFigure();
            sfig.StartPoint = new Point(19, 0);
            sfig.Segments.Add(new ArcSegment(new Point(38, 19), new Size(19, 19), 0, false, SweepDirection.Clockwise, true));
            PathGeometry sgeom = new PathGeometry();
            sgeom.Figures.Add(sfig);
            spinHead.Data = sgeom;

            RotateTransform spinRt = new RotateTransform(0, 19, 19);
            spinHead.RenderTransform = spinRt;
            DoubleAnimation spinAnim = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(850));
            spinAnim.RepeatBehavior = RepeatBehavior.Forever;
            spinRt.BeginAnimation(RotateTransform.AngleProperty, spinAnim);
            spinnerBox.Children.Add(spinHead);

            centerInfo.Children.Add(spinnerBox);

            TextBlock msgTitle = new TextBlock();
            msgTitle.Text = "Verificando ambiente e serviços...";
            msgTitle.FontSize = 13.5;
            msgTitle.FontWeight = FontWeights.Bold;
            msgTitle.Foreground = UiTheme.TextPrimary;
            msgTitle.HorizontalAlignment = HorizontalAlignment.Center;
            centerInfo.Children.Add(msgTitle);

            TextBlock msgDesc = new TextBlock();
            msgDesc.Text = "Escaneando diretórios configurados, portas TCP e processos em segundo plano";
            msgDesc.FontSize = 11;
            msgDesc.Foreground = UiTheme.TextMuted;
            msgDesc.Margin = new Thickness(0, 4, 0, 0);
            msgDesc.HorizontalAlignment = HorizontalAlignment.Center;
            centerInfo.Children.Add(msgDesc);

            consoleBg.Child = centerInfo;
            Grid.SetRow(consoleBg, 2);
            g.Children.Add(consoleBg);

            panel.Child = g;
            return panel;
        }

        private UIElement CriarDonutChart(Brush strokeBrush, out System.Windows.Shapes.Path arcPath, out TextBlock centerValText, string label, double size = 100)
        {
            Grid container = new Grid();
            container.Width = size;
            container.Height = size;
            container.HorizontalAlignment = HorizontalAlignment.Center;

            double strokeThickness = 8;
            double circleSize = size - strokeThickness;

            System.Windows.Shapes.Ellipse bgCircle = new System.Windows.Shapes.Ellipse();
            bgCircle.Width = circleSize;
            bgCircle.Height = circleSize;
            bgCircle.Stroke = UiTheme.BrushFromHex("#1E293B");
            bgCircle.StrokeThickness = strokeThickness;
            bgCircle.HorizontalAlignment = HorizontalAlignment.Center;
            bgCircle.VerticalAlignment = VerticalAlignment.Center;
            container.Children.Add(bgCircle);

            System.Windows.Shapes.Path arc = new System.Windows.Shapes.Path();
            arc.Width = size;
            arc.Height = size;
            arc.Stroke = strokeBrush;
            arc.StrokeThickness = strokeThickness;
            arc.StrokeStartLineCap = PenLineCap.Round;
            arc.StrokeEndLineCap = PenLineCap.Round;
            arc.HorizontalAlignment = HorizontalAlignment.Left;
            arc.VerticalAlignment = VerticalAlignment.Top;
            container.Children.Add(arc);
            arcPath = arc;

            StackPanel centerStack = new StackPanel();
            centerStack.HorizontalAlignment = HorizontalAlignment.Center;
            centerStack.VerticalAlignment = VerticalAlignment.Center;

            TextBlock val = new TextBlock();
            val.Text = "0%";
            val.FontSize = 13.5;
            val.FontWeight = FontWeights.Bold;
            val.Foreground = UiTheme.TextPrimary;
            val.HorizontalAlignment = HorizontalAlignment.Center;
            centerStack.Children.Add(val);
            centerValText = val;

            TextBlock lbl = new TextBlock();
            lbl.Text = label;
            lbl.FontSize = 9;
            lbl.FontWeight = FontWeights.SemiBold;
            lbl.Foreground = UiTheme.TextMuted;
            lbl.Margin = new Thickness(0, 1, 0, 0);
            lbl.HorizontalAlignment = HorizontalAlignment.Center;
            centerStack.Children.Add(lbl);

            container.Children.Add(centerStack);
            return container;
        }
    }
}
