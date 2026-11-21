using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        public void ShowSmartView()
        {
            _isInDashboardView = false;
            _currentTab = HubActiveTab.SmartView;
            CarregarSmartViewPrefFolder();
            CarregarNotasSmartViewAppData();

            Grid rootGrid = new Grid();
            RowDefinition rowTop = new RowDefinition(); rowTop.Height = GridLength.Auto;
            RowDefinition rowToolbar = new RowDefinition(); rowToolbar.Height = GridLength.Auto;
            RowDefinition rowInfoBar = new RowDefinition(); rowInfoBar.Height = GridLength.Auto;
            RowDefinition rowBody = new RowDefinition(); rowBody.Height = new GridLength(1, GridUnitType.Star);
            rootGrid.RowDefinitions.Add(rowTop);
            rootGrid.RowDefinitions.Add(rowToolbar);
            rootGrid.RowDefinitions.Add(rowInfoBar);
            rootGrid.RowDefinitions.Add(rowBody);

            StackPanel topLeft = CreateBrandHeaderWithTabs(HubActiveTab.SmartView);
            Border topBar = BuildTopWindowBar(topLeft, null);
            Grid.SetRow(topBar, 0);
            rootGrid.Children.Add(topBar);

            Border toolbar = BuildSmartViewToolbar();
            Grid.SetRow(toolbar, 1);
            rootGrid.Children.Add(toolbar);

            Border infoBar = BuildSmartViewInfoBar();
            Grid.SetRow(infoBar, 2);
            rootGrid.Children.Add(infoBar);

            _svScrollViewer = new ScrollViewer();
            _svScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _svScrollViewer.Padding = new Thickness(20, 14, 20, 20);

            _svCardsContainer = new StackPanel();
            _svScrollViewer.Content = _svCardsContainer;

            Grid.SetRow(_svScrollViewer, 3);
            rootGrid.Children.Add(_svScrollViewer);

            _pageHost.Content = rootGrid;

            RenderizarCardsSmartView();
            VerificarPortasSmartViewAsync();
        }

        private Border BuildSmartViewToolbar()
        {
            Border toolbar = new Border();
            toolbar.Padding = new Thickness(20, 14, 20, 10);
            toolbar.Background = UiTheme.BgWindow;

            Grid toolGrid = new Grid();
            ColumnDefinition cLeft = new ColumnDefinition(); cLeft.Width = GridLength.Auto;
            ColumnDefinition cSearch = new ColumnDefinition(); cSearch.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition cRight = new ColumnDefinition(); cRight.Width = GridLength.Auto;
            toolGrid.ColumnDefinitions.Add(cLeft);
            toolGrid.ColumnDefinitions.Add(cSearch);
            toolGrid.ColumnDefinitions.Add(cRight);

            Button btnFolder = UiTheme.CreateActionButton(
                UiTheme.CreateFolderIcon(Brushes.White, 16),
                UiTheme.NeutralBtnHex,
                UiTheme.NeutralBtnHoverHex);
            btnFolder.Width = 38;
            btnFolder.Height = 34;
            btnFolder.Padding = new Thickness(0);
            btnFolder.Margin = new Thickness(0, 0, 10, 0);
            btnFolder.ToolTip = "Configurar pasta de instalação do SmartView";
            btnFolder.Click += delegate
            {
                using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog())
                {
                    fbd.Description = "Selecione a pasta de instalação do SmartView";
                    if (!string.IsNullOrEmpty(_smartViewDetectedFolder) && Directory.Exists(_smartViewDetectedFolder))
                        fbd.SelectedPath = _smartViewDetectedFolder;

                    if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        _smartViewDetectedFolder = fbd.SelectedPath;
                        SalvarSmartViewPrefFolder(fbd.SelectedPath);
                        if (_svFolderLabel != null) _svFolderLabel.Text = "Pasta: " + fbd.SelectedPath;
                    }
                }
            };
            Grid.SetColumn(btnFolder, 0);
            toolGrid.Children.Add(btnFolder);

            Border searchContainer = new Border();
            searchContainer.Background = UiTheme.BgHeader;
            searchContainer.BorderBrush = UiTheme.BorderSubtle;
            searchContainer.BorderThickness = new Thickness(1);
            searchContainer.CornerRadius = new CornerRadius(8);
            searchContainer.Padding = new Thickness(12, 7, 12, 7);
            searchContainer.Margin = new Thickness(0, 0, 10, 0);

            DockPanel searchDock = new DockPanel();
            UIElement lupaIcon = UiTheme.CreateSearchIcon();
            DockPanel.SetDock(lupaIcon, Dock.Left);
            searchDock.Children.Add(lupaIcon);

            Grid searchInner = new Grid();
            TextBlock searchPlaceholder = new TextBlock();
            searchPlaceholder.Text = "Filtrar portas, URLs ou anotações do SmartView...";
            searchPlaceholder.Foreground = UiTheme.TextMuted;
            searchPlaceholder.FontSize = 13;
            searchPlaceholder.VerticalAlignment = VerticalAlignment.Center;
            searchPlaceholder.IsHitTestVisible = false;

            TextBox txtSearch = new TextBox();
            txtSearch.Background = Brushes.Transparent;
            txtSearch.Foreground = UiTheme.TextPrimary;
            txtSearch.CaretBrush = UiTheme.TextPrimary;
            txtSearch.BorderThickness = new Thickness(0);
            txtSearch.FontSize = 13;
            txtSearch.VerticalAlignment = VerticalAlignment.Center;
            txtSearch.TextChanged += delegate
            {
                searchPlaceholder.Visibility = string.IsNullOrEmpty(txtSearch.Text) ? Visibility.Visible : Visibility.Collapsed;
                _svFilterText = txtSearch.Text;
                RenderizarCardsSmartView();
            };

            searchInner.Children.Add(searchPlaceholder);
            searchInner.Children.Add(txtSearch);
            searchDock.Children.Add(searchInner);
            searchContainer.Child = searchDock;
            Grid.SetColumn(searchContainer, 1);
            toolGrid.Children.Add(searchContainer);

            StackPanel rightTools = new StackPanel();
            rightTools.Orientation = Orientation.Horizontal;
            rightTools.VerticalAlignment = VerticalAlignment.Center;

            System.Windows.Shapes.Path spinIcon = (System.Windows.Shapes.Path)UiTheme.CreateThinRefreshIcon(UiTheme.AccentLightBlue);
            Button btnReload = UiTheme.CreateFramelessIconButton(spinIcon, "Recarregar e testar portas 7017 e 7019");
            btnReload.Click += delegate
            {
                RotateTransform rt = spinIcon.RenderTransform as RotateTransform;
                if (rt == null) { rt = new RotateTransform(0); spinIcon.RenderTransform = rt; }
                DoubleAnimation spin = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(650));
                spin.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut };
                rt.BeginAnimation(RotateTransform.AngleProperty, spin);
                VerificarPortasSmartViewAsync();
            };
            rightTools.Children.Add(btnReload);

            Grid.SetColumn(rightTools, 2);
            toolGrid.Children.Add(rightTools);

            toolbar.Child = toolGrid;
            return toolbar;
        }

        private Border BuildSmartViewInfoBar()
        {
            Border infoBar = new Border();
            infoBar.Padding = new Thickness(24, 8, 24, 0);

            DockPanel dp = new DockPanel();

            StackPanel leftStack = new StackPanel();
            leftStack.Orientation = Orientation.Horizontal;
            leftStack.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(leftStack, Dock.Left);

            _svFolderLabel = new TextBlock();
            string folderTxt = string.IsNullOrEmpty(_smartViewDetectedFolder) ? "Pasta: Não foi possível localizar pelas portas. Selecione a pasta acima" : "Pasta: " + _smartViewDetectedFolder;
            _svFolderLabel.Text = folderTxt;
            _svFolderLabel.Foreground = UiTheme.TextMuted;
            _svFolderLabel.FontSize = 12;
            _svFolderLabel.Cursor = Cursors.Hand;
            _svFolderLabel.ToolTip = string.IsNullOrEmpty(_smartViewDetectedFolder) ? "Clique para selecionar a pasta de instalação do SmartView" : "Clique para abrir no Windows Explorer";
            _svFolderLabel.MouseLeftButtonUp += delegate
            {
                if (!string.IsNullOrEmpty(_smartViewDetectedFolder) && Directory.Exists(_smartViewDetectedFolder))
                    AbrirNoExplorer(_smartViewDetectedFolder);
                else
                    SelecionarPastaSmartViewDialog();
            };
            leftStack.Children.Add(_svFolderLabel);

            Button btnCopyFolder = UiTheme.CreateFramelessIconButton(UiTheme.CreateCopyIcon(UiTheme.TextMuted, 12), "Copiar caminho da pasta");
            btnCopyFolder.Margin = new Thickness(6, 0, 0, 0);
            btnCopyFolder.Click += delegate
            {
                try { Clipboard.SetText(_smartViewDetectedFolder ?? ""); } catch { }
            };
            leftStack.Children.Add(btnCopyFolder);
            dp.Children.Add(leftStack);

            _svLastUpdateLabel = new TextBlock();
            _svLastUpdateLabel.Text = string.Format("Atualizado às {0:HH:mm:ss}", DateTime.Now);
            _svLastUpdateLabel.Foreground = UiTheme.TextMuted;
            _svLastUpdateLabel.FontSize = 12;
            _svLastUpdateLabel.VerticalAlignment = VerticalAlignment.Center;
            _svLastUpdateLabel.HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(_svLastUpdateLabel, Dock.Right);
            dp.Children.Add(_svLastUpdateLabel);

            infoBar.Child = dp;
            return infoBar;
        }

        private void RenderizarCardsSmartView()
        {
            if (_svScrollViewer == null) return;

            string filtro = (_svFilterText ?? "").Trim().ToLowerInvariant();

            string nota7017 = "";
            _smartViewNotes.TryGetValue(7017, out nota7017);
            bool match7017 = string.IsNullOrEmpty(filtro) ||
                             "7017".Contains(filtro) ||
                             "smartview home".Contains(filtro) ||
                             (!string.IsNullOrEmpty(nota7017) && nota7017.ToLowerInvariant().Contains(filtro));

            string nota7019 = "";
            _smartViewNotes.TryGetValue(7019, out nota7019);
            bool match7019 = string.IsNullOrEmpty(filtro) ||
                             "7019".Contains(filtro) ||
                             "smartview diagnostic".Contains(filtro) ||
                             (!string.IsNullOrEmpty(nota7019) && nota7019.ToLowerInvariant().Contains(filtro));

            _svCard7017 = BuildPortCard(
                _smartViewPort7017,
                "SmartView Home",
                "http://localhost:7017",
                "Serviço central de execução e processamento de relatórios SmartView",
                ref _svCard7017Badge,
                ref _svCard7017StatusText,
                ref _svCard7017LatencyText,
                "http://localhost:7017");

            _svCard7019 = BuildPortCard(
                _smartViewPort7019,
                "SmartView Diagnostic",
                "http://localhost:7019/diagnostic",
                "Diagnóstico e status interno da infraestrutura SmartView",
                ref _svCard7019Badge,
                ref _svCard7019StatusText,
                ref _svCard7019LatencyText,
                "http://localhost:7019/diagnostic");

            _svCard7017.Visibility = match7017 ? Visibility.Visible : Visibility.Collapsed;
            _svCard7019.Visibility = match7019 ? Visibility.Visible : Visibility.Collapsed;

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(_svCard7017, 0);
            grid.Children.Add(_svCard7017);

            Grid.SetColumn(_svCard7019, 2);
            grid.Children.Add(_svCard7019);

            _svCardsContainer = grid;
            _svScrollViewer.Content = _svCardsContainer;

            AtualizarVisualPortasPadrao();
        }

        private Border BuildPortCard(
            SmartViewPortStatus portStatus,
            string customTitle,
            string urlPath,
            string description,
            ref Border badgeRef,
            ref TextBlock statusTextRef,
            ref TextBlock latencyTextRef,
            string targetUrl)
        {
            Border card = new Border();
            card.Background = UiTheme.BgCard;
            card.BorderBrush = UiTheme.BorderSubtle;
            card.BorderThickness = new Thickness(1);
            card.CornerRadius = new CornerRadius(12);
            card.Padding = new Thickness(16, 14, 16, 14);

            Grid cardGrid = new Grid();
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel leftStack = new StackPanel();
            leftStack.VerticalAlignment = VerticalAlignment.Center;
            leftStack.HorizontalAlignment = HorizontalAlignment.Center;
            leftStack.Width = 96;
            leftStack.Margin = new Thickness(0, 0, 14, 0);

            Border statusBadge = new Border();
            statusBadge.Width = 96;
            statusBadge.Height = 26;
            statusBadge.CornerRadius = new CornerRadius(10);
            statusBadge.Padding = new Thickness(6, 3, 6, 3);
            statusBadge.Background = portStatus.IsOpen ? UiTheme.AccentGreen : UiTheme.BrushFromHex("#475569");
            statusBadge.HorizontalAlignment = HorizontalAlignment.Center;

            TextBlock statusText = new TextBlock();
            statusText.Text = portStatus.IsOpen ? "RUNNING" : "STOPPED";
            statusText.FontSize = 10;
            statusText.FontWeight = FontWeights.Bold;
            statusText.Foreground = Brushes.White;
            statusBadge.Child = statusText;
            leftStack.Children.Add(statusBadge);

            badgeRef = statusBadge;
            statusTextRef = statusText;

            StackPanel actionRow = new StackPanel();
            actionRow.Orientation = Orientation.Horizontal;
            actionRow.HorizontalAlignment = HorizontalAlignment.Center;
            actionRow.Margin = new Thickness(0, 8, 0, 0);

            Button btnLogsSquare = UiTheme.CreateActionButton(
                UiTheme.CreateHistoryIcon(UiTheme.AccentLightBlue, 13),
                UiTheme.NeutralBtnHex,
                UiTheme.NeutralBtnHoverHex);
            btnLogsSquare.Width = 34;
            btnLogsSquare.Height = 26;
            btnLogsSquare.Padding = new Thickness(0);
            btnLogsSquare.Margin = new Thickness(0, 0, 4, 0);
            btnLogsSquare.ToolTip = "Logs e Monitoramento do SmartView";
            btnLogsSquare.Click += delegate
            {
                string logsDir = ObterDiretorioLogsSmartView();
                AbrirTelaCompletaLogs("SmartView", logsDir, _smartViewPid, "smartview");
            };

            Button btnCommentSquare = UiTheme.CreateActionButton(
                UiTheme.CreateCommentBubbleIcon(UiTheme.TextSecondary, 13),
                UiTheme.NeutralBtnHex,
                UiTheme.NeutralBtnHoverHex);
            btnCommentSquare.Width = 34;
            btnCommentSquare.Height = 26;
            btnCommentSquare.Padding = new Thickness(0);
            btnCommentSquare.ToolTip = "Adicionar / editar anotação";
            btnCommentSquare.Click += delegate
            {
                EditarNotaSmartView(portStatus.Port, customTitle);
            };

            actionRow.Children.Add(btnLogsSquare);
            actionRow.Children.Add(btnCommentSquare);
            leftStack.Children.Add(actionRow);

            Grid.SetRow(leftStack, 0);
            Grid.SetColumn(leftStack, 0);
            cardGrid.Children.Add(leftStack);

            StackPanel infoStack = new StackPanel();
            infoStack.VerticalAlignment = VerticalAlignment.Center;
            infoStack.Margin = new Thickness(0, 0, 12, 0);

            WrapPanel titleWrap = new WrapPanel();
            titleWrap.VerticalAlignment = VerticalAlignment.Center;

            TextBlock titleLbl = new TextBlock();
            titleLbl.Text = customTitle ?? portStatus.Label;
            titleLbl.FontSize = 14;
            titleLbl.FontWeight = FontWeights.Bold;
            titleLbl.Foreground = UiTheme.TextPrimary;
            titleLbl.VerticalAlignment = VerticalAlignment.Center;
            titleWrap.Children.Add(titleLbl);

            Button btnCopyTitle = UiTheme.CreateFramelessIconButton(
                UiTheme.CreateCopyIcon(UiTheme.TextMuted, 13),
                "Copiar endereço");
            btnCopyTitle.Margin = new Thickness(6, 0, 0, 0);
            btnCopyTitle.Click += delegate
            {
                try { Clipboard.SetText(targetUrl ?? urlPath ?? ""); } catch { }
            };
            titleWrap.Children.Add(btnCopyTitle);

            infoStack.Children.Add(titleWrap);

            TextBlock lblUrl = new TextBlock();
            lblUrl.Text = urlPath ?? ("http://localhost:" + portStatus.Port);
            lblUrl.FontSize = 11;
            lblUrl.Foreground = UiTheme.AccentLightBlue;
            lblUrl.Cursor = Cursors.Hand;
            lblUrl.Margin = new Thickness(0, 3, 0, 4);
            lblUrl.ToolTip = "Clique para abrir: " + lblUrl.Text;
            lblUrl.MouseLeftButtonUp += delegate
            {
                try { Process.Start(lblUrl.Text); } catch { }
            };
            infoStack.Children.Add(lblUrl);

            TextBlock desc = new TextBlock();
            desc.Text = description ?? portStatus.Description;
            desc.FontSize = 12;
            desc.Foreground = UiTheme.TextSecondary;
            desc.Margin = new Thickness(0, 0, 0, 4);
            infoStack.Children.Add(desc);

            TextBlock latencyText = new TextBlock();
            latencyText.Text = portStatus.IsOpen
                ? string.Format("Latência: {0}ms • Protocolo TCP", portStatus.LatencyMs)
                : "Serviço não detectado na porta";
            latencyText.FontSize = 11;
            latencyText.Foreground = portStatus.IsOpen ? UiTheme.AccentLightBlue : UiTheme.TextMuted;
            infoStack.Children.Add(latencyText);
            latencyTextRef = latencyText;

            Grid.SetRow(infoStack, 0);
            Grid.SetColumn(infoStack, 1);
            cardGrid.Children.Add(infoStack);

            StackPanel accessStack = new StackPanel();
            accessStack.Orientation = Orientation.Horizontal;
            accessStack.VerticalAlignment = VerticalAlignment.Center;
            accessStack.Margin = new Thickness(0, 0, 10, 0);

            Button btnLink = UiTheme.CreateWebLinkButton("Abrir", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
            btnLink.ToolTip = "Abrir " + (targetUrl ?? urlPath);
            btnLink.Click += delegate
            {
                try { Process.Start(targetUrl ?? urlPath ?? string.Format("http://localhost:{0}", portStatus.Port)); } catch { }
            };
            accessStack.Children.Add(btnLink);

            Grid.SetRow(accessStack, 0);
            Grid.SetColumn(accessStack, 2);
            cardGrid.Children.Add(accessStack);

            Button btnDots = UiTheme.CreateFramelessIconButton(
                UiTheme.CreateThreeDotsIcon(UiTheme.TextSecondary, 16),
                "Mais opções do SmartView");
            btnDots.VerticalAlignment = VerticalAlignment.Top;
            btnDots.HorizontalAlignment = HorizontalAlignment.Right;
            btnDots.Margin = new Thickness(4, -4, 0, 0);

            ContextMenu menuDots = UiTheme.CreateDarkContextMenu();

            menuDots.Items.Add(UiTheme.CreateDarkMenuItem("Comentário", UiTheme.CreateCommentBubbleIcon(UiTheme.TextSecondary, 14), delegate
            {
                EditarNotaSmartView(portStatus.Port, customTitle);
            }));

            menuDots.Items.Add(UiTheme.CreateDarkMenuItem("Logs e Monitoramento", UiTheme.CreateCopyIcon(UiTheme.AccentLightBlue, 14), delegate
            {
                string logsDir = ObterDiretorioLogsSmartView();
                AbrirTelaCompletaLogs("SmartView", logsDir, _smartViewPid, "smartview");
            }));

            menuDots.Items.Add(new Separator { Background = UiTheme.BorderSubtle, Margin = new Thickness(4, 2, 4, 2) });

            menuDots.Items.Add(UiTheme.CreateDarkMenuItem("Abrir pasta no Explorer", UiTheme.CreateFolderIcon(UiTheme.TextSecondary, 14), delegate
            {
                if (!string.IsNullOrEmpty(_smartViewDetectedFolder) && Directory.Exists(_smartViewDetectedFolder))
                    AbrirNoExplorer(_smartViewDetectedFolder);
                else
                    SelecionarPastaSmartViewDialog();
            }));

            btnDots.ContextMenu = menuDots;
            btnDots.Click += delegate
            {
                menuDots.PlacementTarget = btnDots;
                menuDots.Placement = PlacementMode.Bottom;
                menuDots.IsOpen = true;
            };

            Grid.SetRow(btnDots, 0);
            Grid.SetColumn(btnDots, 3);
            cardGrid.Children.Add(btnDots);

            string noteText = "";
            if (_smartViewNotes.TryGetValue(portStatus.Port, out noteText) && !string.IsNullOrWhiteSpace(noteText))
            {
                Border noteBanner = new Border();
                noteBanner.Background = UiTheme.NoteBannerBg;
                noteBanner.BorderBrush = UiTheme.BorderSubtle;
                noteBanner.BorderThickness = new Thickness(1);
                noteBanner.CornerRadius = new CornerRadius(7);
                noteBanner.Padding = new Thickness(10, 6, 10, 6);
                noteBanner.Margin = new Thickness(0, 10, 0, 0);
                noteBanner.Cursor = Cursors.Hand;
                noteBanner.ToolTip = "Clique para editar anotação";
                noteBanner.MouseLeftButtonUp += delegate { EditarNotaSmartView(portStatus.Port, customTitle); };

                TextBlock noteLbl = new TextBlock();
                noteLbl.Text = "💬 " + noteText;
                noteLbl.Foreground = UiTheme.TextPrimary;
                noteLbl.FontSize = 12;
                noteLbl.TextWrapping = TextWrapping.Wrap;
                noteBanner.Child = noteLbl;

                Grid.SetRow(noteBanner, 1);
                Grid.SetColumn(noteBanner, 0);
                Grid.SetColumnSpan(noteBanner, 4);
                cardGrid.Children.Add(noteBanner);
            }

            card.Child = cardGrid;
            return card;
        }

        private void EditarNotaSmartView(int port, string label)
        {
            string atual = "";
            _smartViewNotes.TryGetValue(port, out atual);

            Window dlg = new Window();
            dlg.Title = "Anotação — " + label;
            dlg.Icon = TotvsLogo.CreateTotvsWindowImageSource(_cachedSvgPath);
            dlg.Owner = this;
            dlg.Width = 440;
            dlg.SizeToContent = SizeToContent.Height;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.WindowStyle = WindowStyle.None;
            dlg.ResizeMode = ResizeMode.NoResize;
            dlg.Background = UiTheme.BgHeader;
            dlg.FontFamily = new FontFamily("Segoe UI");
            UiTheme.ApplyWindowChromeState(dlg, false);

            Border outer = new Border();
            outer.Background = UiTheme.BgHeader;
            outer.BorderBrush = UiTheme.BorderSubtle;
            outer.BorderThickness = new Thickness(1);
            outer.CornerRadius = new CornerRadius(12);
            outer.Padding = new Thickness(20, 16, 20, 18);
            outer.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ClickCount == 1)
                {
                    try { dlg.DragMove(); } catch { }
                }
            };

            StackPanel stack = new StackPanel();

            DockPanel topRow = new DockPanel();
            topRow.Margin = new Thickness(0, 0, 0, 10);
            Button btnClose = UiTheme.CreateWindowButton("✕", "#DC2626");
            btnClose.Width = 28;
            btnClose.Height = 26;
            DockPanel.SetDock(btnClose, Dock.Right);
            btnClose.Click += delegate { dlg.Close(); };
            topRow.Children.Add(btnClose);

            TextBlock lblTitle = new TextBlock();
            lblTitle.Text = "Anotação para " + label + " (:" + port + ")";
            lblTitle.Foreground = UiTheme.TextPrimary;
            lblTitle.FontSize = 14;
            lblTitle.FontWeight = FontWeights.Bold;
            lblTitle.VerticalAlignment = VerticalAlignment.Center;
            topRow.Children.Add(lblTitle);
            stack.Children.Add(topRow);

            TextBox txtNote = new TextBox();
            txtNote.Text = atual ?? "";
            txtNote.MaxLength = 100;
            txtNote.AcceptsReturn = true;
            txtNote.TextWrapping = TextWrapping.Wrap;
            txtNote.Height = 84;
            txtNote.Foreground = UiTheme.TextPrimary;
            txtNote.Background = UiTheme.BgInput;
            txtNote.CaretBrush = UiTheme.TextPrimary;
            txtNote.BorderBrush = UiTheme.BorderSubtle;
            txtNote.BorderThickness = new Thickness(1);
            txtNote.Padding = new Thickness(8, 6, 8, 6);
            txtNote.FontSize = 12;
            txtNote.Margin = new Thickness(0, 0, 0, 8);
            stack.Children.Add(txtNote);

            DockPanel footerRow = new DockPanel();
            footerRow.Margin = new Thickness(0, 0, 0, 6);

            TextBlock lblCount = new TextBlock();
            lblCount.Text = string.Format("{0}/100 caracteres", (txtNote.Text ?? "").Length);
            lblCount.Foreground = UiTheme.TextMuted;
            lblCount.FontSize = 11;
            lblCount.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(lblCount, Dock.Left);
            footerRow.Children.Add(lblCount);

            txtNote.TextChanged += delegate
            {
                lblCount.Text = string.Format("{0}/100 caracteres", (txtNote.Text ?? "").Length);
            };

            StackPanel btnsRow = new StackPanel();
            btnsRow.Orientation = Orientation.Horizontal;
            btnsRow.HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(btnsRow, Dock.Right);

            Button btnClear = UiTheme.CreateActionButton("Limpar", UiTheme.NeutralBtnHex, "#DC2626");
            btnClear.Margin = new Thickness(0, 0, 8, 0);
            btnClear.Click += delegate
            {
                _smartViewNotes.Remove(port);
                SalvarNotasSmartViewAppData();
                dlg.Close();
                RenderizarCardsSmartView();
            };

            Button btnSave = UiTheme.CreateActionButton("Salvar", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
            btnSave.Click += delegate
            {
                string val = (txtNote.Text ?? "").Trim();
                if (string.IsNullOrEmpty(val))
                    _smartViewNotes.Remove(port);
                else
                    _smartViewNotes[port] = val;

                SalvarNotasSmartViewAppData();
                dlg.Close();
                RenderizarCardsSmartView();
            };

            btnsRow.Children.Add(btnClear);
            btnsRow.Children.Add(btnSave);
            footerRow.Children.Add(btnsRow);
            stack.Children.Add(footerRow);

            outer.Child = stack;
            dlg.Content = outer;
            dlg.ShowDialog();
        }

        private void AtualizarVisualPortasPadrao()
        {
            AtualizarBadgeEStatus(_smartViewPort7017, _svCard7017Badge, _svCard7017StatusText, _svCard7017LatencyText);
            AtualizarBadgeEStatus(_smartViewPort7019, _svCard7019Badge, _svCard7019StatusText, _svCard7019LatencyText);
        }

        private void AtualizarBadgeEStatus(SmartViewPortStatus port, Border badge, TextBlock statusText, TextBlock latencyText)
        {
            if (badge != null)
                badge.Background = port.IsOpen ? UiTheme.AccentGreen : UiTheme.BrushFromHex("#475569");

            if (statusText != null)
                statusText.Text = port.IsOpen ? "RUNNING" : "STOPPED";

            if (latencyText != null)
            {
                latencyText.Text = port.IsOpen
                    ? string.Format("Latência: {0}ms • Protocolo TCP", port.LatencyMs)
                    : "Serviço não detectado na porta";
                latencyText.Foreground = port.IsOpen ? UiTheme.AccentLightBlue : UiTheme.TextMuted;
            }
        }
    }
}
