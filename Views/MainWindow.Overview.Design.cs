using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
            headerBorder.Padding = new Thickness(24, 14, 24, 12);
            headerBorder.Background = UiTheme.BgHeader;
            headerBorder.BorderBrush = UiTheme.BorderSubtle;
            headerBorder.BorderThickness = new Thickness(0, 0, 0, 1);

            DockPanel headerDock = new DockPanel();

            StackPanel headerLeft = new StackPanel();
            headerLeft.VerticalAlignment = VerticalAlignment.Center;

            TextBlock titleMain = new TextBlock();
            titleMain.Text = "Central de Visão Geral";
            titleMain.FontSize = 18;
            titleMain.FontWeight = FontWeights.Bold;
            titleMain.Foreground = UiTheme.TextPrimary;
            headerLeft.Children.Add(titleMain);

            TextBlock titleSub = new TextBlock();
            titleSub.Text = "Monitoramento de consumo e streaming de logs em tempo real dos serviços ativos";
            titleSub.FontSize = 12;
            titleSub.Foreground = UiTheme.TextMuted;
            titleSub.Margin = new Thickness(0, 2, 0, 0);
            headerLeft.Children.Add(titleSub);

            DockPanel.SetDock(headerLeft, Dock.Left);
            headerDock.Children.Add(headerLeft);

            StackPanel headerRight = new StackPanel();
            headerRight.Orientation = Orientation.Horizontal;
            headerRight.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(headerRight, Dock.Right);

            Border badgeCount = new Border();
            badgeCount.Background = UiTheme.BrushFromHex("#064E3B");
            badgeCount.BorderBrush = UiTheme.AccentGreen;
            badgeCount.BorderThickness = new Thickness(1);
            badgeCount.CornerRadius = new CornerRadius(12);
            badgeCount.Padding = new Thickness(10, 4, 10, 4);
            badgeCount.Margin = new Thickness(0, 0, 10, 0);

            _overviewActiveCountText = new TextBlock();
            _overviewActiveCountText.Text = "Calculando ativos...";
            _overviewActiveCountText.FontSize = 11;
            _overviewActiveCountText.FontWeight = FontWeights.Bold;
            _overviewActiveCountText.Foreground = UiTheme.AccentGreen;
            badgeCount.Child = _overviewActiveCountText;
            headerRight.Children.Add(badgeCount);

            Button btnRefresh = UiTheme.CreateActionButton("Atualizar", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
            btnRefresh.Height = 28;
            btnRefresh.FontSize = 11;
            btnRefresh.Padding = new Thickness(12, 0, 12, 0);
            btnRefresh.Click += delegate
            {
                AtualizarListaOverview();
                CarregarLogOverviewAsync(true);
            };
            headerRight.Children.Add(btnRefresh);

            headerDock.Children.Add(headerRight);
            headerBorder.Child = headerDock;

            Grid.SetRow(headerBorder, 1);
            rootGrid.Children.Add(headerBorder);

            Grid bodyGrid = new Grid();
            bodyGrid.Margin = new Thickness(24, 14, 24, 16);
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(420) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            ScrollViewer leftScroll = new ScrollViewer();
            leftScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            leftScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

            _overviewCardsStack = new StackPanel();
            leftScroll.Content = _overviewCardsStack;
            Grid.SetColumn(leftScroll, 0);
            bodyGrid.Children.Add(leftScroll);

            Border rightLogPanel = CriarPainelLogsOverview();
            Grid.SetColumn(rightLogPanel, 2);
            bodyGrid.Children.Add(rightLogPanel);

            Grid.SetRow(bodyGrid, 2);
            rootGrid.Children.Add(bodyGrid);

            _pageHost.Content = rootGrid;

            AtualizarListaOverview();
            CarregarLogOverviewAsync(true);
            IniciarOverviewTimer();
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
            DockPanel.SetDock(actions, Dock.Right);

            _overviewAutoScrollBtn = UiTheme.CreateActionButton(_overviewAutoScroll ? "Auto-scroll: ON" : "Auto-scroll: OFF", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
            _overviewAutoScrollBtn.Height = 26;
            _overviewAutoScrollBtn.FontSize = 11;
            _overviewAutoScrollBtn.Padding = new Thickness(10, 0, 10, 0);
            _overviewAutoScrollBtn.Margin = new Thickness(0, 0, 6, 0);
            _overviewAutoScrollBtn.Click += delegate
            {
                _overviewAutoScroll = !_overviewAutoScroll;
                _overviewAutoScrollBtn.Content = _overviewAutoScroll ? "Auto-scroll: ON" : "Auto-scroll: OFF";
                if (_overviewAutoScroll && _overviewLogConsole != null)
                    _overviewLogConsole.ScrollToEnd();
            };
            actions.Children.Add(_overviewAutoScrollBtn);

            Button btnCopy = UiTheme.CreateActionButton("Copiar", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
            btnCopy.Height = 26;
            btnCopy.FontSize = 11;
            btnCopy.Padding = new Thickness(10, 0, 10, 0);
            btnCopy.Margin = new Thickness(0, 0, 6, 0);
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

            Button btnOpenFolder = UiTheme.CreateActionButton("Abrir Pasta", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
            btnOpenFolder.Height = 26;
            btnOpenFolder.FontSize = 11;
            btnOpenFolder.Padding = new Thickness(10, 0, 10, 0);
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
