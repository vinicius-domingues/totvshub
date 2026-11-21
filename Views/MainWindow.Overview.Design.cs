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
            titleMain.Text = "Central de Visão Geral";
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

            DockPanel.SetDock(headerLeft, Dock.Left);
            headerDock.Children.Add(headerLeft);

            StackPanel headerRight = new StackPanel();
            headerRight.Orientation = Orientation.Horizontal;
            headerRight.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(headerRight, Dock.Right);

            System.Windows.Shapes.Path spinIcon = (System.Windows.Shapes.Path)UiTheme.CreateThinRefreshIcon(UiTheme.AccentLightBlue);
            Button btnReload = UiTheme.CreateFramelessIconButton(spinIcon, "Atualizar dados e gráficos da Central");
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
            headerBorder.Child = headerDock;

            Grid.SetRow(headerBorder, 1);
            rootGrid.Children.Add(headerBorder);

            Grid bodyGrid = new Grid();
            bodyGrid.Margin = new Thickness(20, 14, 20, 16);
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(33, GridUnitType.Star) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34, GridUnitType.Star) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(33, GridUnitType.Star) });

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
            title.Text = "Consumo Consolidado";
            title.FontSize = 14;
            title.FontWeight = FontWeights.Bold;
            title.Foreground = UiTheme.TextPrimary;
            sp.Children.Add(title);

            TextBlock sub = new TextBlock();
            sub.Text = "Somatória de recursos de todos os serviços rodando";
            sub.FontSize = 11;
            sub.Foreground = UiTheme.TextMuted;
            sub.Margin = new Thickness(0, 2, 0, 12);
            sp.Children.Add(sub);

            Border cardCpu = new Border();
            cardCpu.Background = UiTheme.BrushFromHex("#0B0F19");
            cardCpu.BorderBrush = UiTheme.BorderSubtle;
            cardCpu.BorderThickness = new Thickness(1);
            cardCpu.CornerRadius = new CornerRadius(8);
            cardCpu.Padding = new Thickness(12, 10, 12, 12);
            cardCpu.Margin = new Thickness(0, 0, 0, 10);

            StackPanel spCpu = new StackPanel();
            TextBlock lblCpuHeader = new TextBlock();
            lblCpuHeader.Text = "CPU CONSOLIDADA";
            lblCpuHeader.FontSize = 10;
            lblCpuHeader.FontWeight = FontWeights.Bold;
            lblCpuHeader.Foreground = UiTheme.AccentLightBlue;
            lblCpuHeader.Margin = new Thickness(0, 0, 0, 6);
            spCpu.Children.Add(lblCpuHeader);

            UIElement donutCpu = CriarDonutChart(UiTheme.AccentLightBlue, out _overviewTotalCpuArc, out _overviewTotalCpuCenterText, "TOTAL CPU");
            spCpu.Children.Add(donutCpu);

            Border cpuBarTrack = new Border();
            cpuBarTrack.Height = 7;
            cpuBarTrack.Background = UiTheme.BrushFromHex("#1E293B");
            cpuBarTrack.CornerRadius = new CornerRadius(3.5);
            cpuBarTrack.Margin = new Thickness(0, 8, 0, 4);

            _overviewTotalCpuBarFill = new Border();
            _overviewTotalCpuBarFill.Height = 7;
            _overviewTotalCpuBarFill.Background = UiTheme.AccentLightBlue;
            _overviewTotalCpuBarFill.CornerRadius = new CornerRadius(3.5);
            _overviewTotalCpuBarFill.HorizontalAlignment = HorizontalAlignment.Left;
            _overviewTotalCpuBarFill.Width = 2;
            cpuBarTrack.Child = _overviewTotalCpuBarFill;
            spCpu.Children.Add(cpuBarTrack);

            _overviewTotalCpuSummaryText = new TextBlock();
            _overviewTotalCpuSummaryText.Text = "0.0% de uso total";
            _overviewTotalCpuSummaryText.FontSize = 10.5;
            _overviewTotalCpuSummaryText.Foreground = UiTheme.TextMuted;
            _overviewTotalCpuSummaryText.HorizontalAlignment = HorizontalAlignment.Center;
            spCpu.Children.Add(_overviewTotalCpuSummaryText);

            cardCpu.Child = spCpu;
            sp.Children.Add(cardCpu);

            Border cardRam = new Border();
            cardRam.Background = UiTheme.BrushFromHex("#0B0F19");
            cardRam.BorderBrush = UiTheme.BorderSubtle;
            cardRam.BorderThickness = new Thickness(1);
            cardRam.CornerRadius = new CornerRadius(8);
            cardRam.Padding = new Thickness(12, 10, 12, 12);
            cardRam.Margin = new Thickness(0, 0, 0, 12);

            StackPanel spRam = new StackPanel();
            TextBlock lblRamHeader = new TextBlock();
            lblRamHeader.Text = "MEMÓRIA RAM CONSOLIDADA";
            lblRamHeader.FontSize = 10;
            lblRamHeader.FontWeight = FontWeights.Bold;
            lblRamHeader.Foreground = UiTheme.AccentGreen;
            lblRamHeader.Margin = new Thickness(0, 0, 0, 6);
            spRam.Children.Add(lblRamHeader);

            UIElement donutRam = CriarDonutChart(UiTheme.AccentGreen, out _overviewTotalRamArc, out _overviewTotalRamCenterText, "TOTAL RAM");
            spRam.Children.Add(donutRam);

            Border ramBarTrack = new Border();
            ramBarTrack.Height = 7;
            ramBarTrack.Background = UiTheme.BrushFromHex("#1E293B");
            ramBarTrack.CornerRadius = new CornerRadius(3.5);
            ramBarTrack.Margin = new Thickness(0, 8, 0, 4);

            _overviewTotalRamBarFill = new Border();
            _overviewTotalRamBarFill.Height = 7;
            _overviewTotalRamBarFill.Background = UiTheme.AccentGreen;
            _overviewTotalRamBarFill.CornerRadius = new CornerRadius(3.5);
            _overviewTotalRamBarFill.HorizontalAlignment = HorizontalAlignment.Left;
            _overviewTotalRamBarFill.Width = 2;
            ramBarTrack.Child = _overviewTotalRamBarFill;
            spRam.Children.Add(ramBarTrack);

            _overviewTotalRamSummaryText = new TextBlock();
            _overviewTotalRamSummaryText.Text = "0 MB alocados";
            _overviewTotalRamSummaryText.FontSize = 10.5;
            _overviewTotalRamSummaryText.Foreground = UiTheme.TextMuted;
            _overviewTotalRamSummaryText.HorizontalAlignment = HorizontalAlignment.Center;
            spRam.Children.Add(_overviewTotalRamSummaryText);

            cardRam.Child = spRam;
            sp.Children.Add(cardRam);

            TextBlock lblDistTitle = new TextBlock();
            lblDistTitle.Text = "Distribuição por Serviço";
            lblDistTitle.FontSize = 12;
            lblDistTitle.FontWeight = FontWeights.SemiBold;
            lblDistTitle.Foreground = UiTheme.TextSecondary;
            lblDistTitle.Margin = new Thickness(0, 0, 0, 6);
            sp.Children.Add(lblDistTitle);

            _overviewBreakdownStack = new StackPanel();
            sp.Children.Add(_overviewBreakdownStack);

            scroll.Content = sp;
            panel.Child = scroll;
            return panel;
        }

        private UIElement CriarDonutChart(Brush strokeBrush, out System.Windows.Shapes.Path arcPath, out TextBlock centerValText, string label)
        {
            Grid container = new Grid();
            container.Width = 130;
            container.Height = 130;
            container.HorizontalAlignment = HorizontalAlignment.Center;

            System.Windows.Shapes.Ellipse bgCircle = new System.Windows.Shapes.Ellipse();
            bgCircle.Width = 100;
            bgCircle.Height = 100;
            bgCircle.Stroke = UiTheme.BrushFromHex("#1E293B");
            bgCircle.StrokeThickness = 10;
            bgCircle.HorizontalAlignment = HorizontalAlignment.Center;
            bgCircle.VerticalAlignment = VerticalAlignment.Center;
            container.Children.Add(bgCircle);

            System.Windows.Shapes.Path arc = new System.Windows.Shapes.Path();
            arc.Width = 130;
            arc.Height = 130;
            arc.Stroke = strokeBrush;
            arc.StrokeThickness = 10;
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
            val.FontSize = 17;
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
            lbl.HorizontalAlignment = HorizontalAlignment.Center;
            centerStack.Children.Add(lbl);

            container.Children.Add(centerStack);
            return container;
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

            DockPanel.SetDock(titles, Dock.Left);
            topDock.Children.Add(titles);

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
                    _overviewLogConsole.ScrollToEnd();
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
            Grid.SetRow(topDock, 0);
            g.Children.Add(topDock);

            Border searchBorder = new Border();
            searchBorder.Background = UiTheme.BgInput;
            searchBorder.BorderBrush = UiTheme.BorderSubtle;
            searchBorder.BorderThickness = new Thickness(1);
            searchBorder.CornerRadius = new CornerRadius(6);
            searchBorder.Padding = new Thickness(8, 4, 8, 4);
            searchBorder.Margin = new Thickness(0, 0, 0, 10);

            _overviewSearchBox = new TextBox();
            _overviewSearchBox.Background = Brushes.Transparent;
            _overviewSearchBox.Foreground = UiTheme.TextPrimary;
            _overviewSearchBox.CaretBrush = UiTheme.TextPrimary;
            _overviewSearchBox.BorderThickness = new Thickness(0);
            _overviewSearchBox.FontSize = 12;
            _overviewSearchBox.ToolTip = "Filtrar linhas do log atual";
            _overviewSearchBox.TextChanged += delegate
            {
                _overviewFilterText = _overviewSearchBox.Text;
                FiltrarLinhasLogOverview();
            };
            searchBorder.Child = _overviewSearchBox;

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
            typeTxt.Foreground = UiTheme.AccentLightBlue;
            typeBadge.Child = typeTxt;
            DockPanel.SetDock(typeBadge, Dock.Right);
            topDock.Children.Add(typeBadge);

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
    }
}
