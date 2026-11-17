using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
        private void ShowDashboardView()
        {
            _isInDashboardView = true;

            Grid rootGrid = new Grid();
            RowDefinition rowTop = new RowDefinition(); rowTop.Height = GridLength.Auto;
            RowDefinition rowToolbar = new RowDefinition(); rowToolbar.Height = GridLength.Auto;
            RowDefinition rowLoader = new RowDefinition(); rowLoader.Height = GridLength.Auto;
            RowDefinition rowBody = new RowDefinition(); rowBody.Height = new GridLength(1, GridUnitType.Star);

            rootGrid.RowDefinitions.Add(rowTop);
            rootGrid.RowDefinitions.Add(rowToolbar);
            rootGrid.RowDefinitions.Add(rowLoader);
            rootGrid.RowDefinitions.Add(rowBody);

            StackPanel titleLeft = CreateBrandTitleHeader();

            Border topBar = BuildTopWindowBar(titleLeft, null);
            Grid.SetRow(topBar, 0);
            rootGrid.Children.Add(topBar);

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
            btnFolder.ToolTip = "Selecionar Pasta Base";
            btnFolder.Click += delegate { SelecionarPastaBase(); };
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
            _placeholderText = new TextBlock();
            _placeholderText.Text = "Filtrar ambientes por nome, pasta, porta, DBAlias ou comentário...";
            _placeholderText.Foreground = UiTheme.TextMuted;
            _placeholderText.FontSize = 13;
            _placeholderText.VerticalAlignment = VerticalAlignment.Center;
            _placeholderText.IsHitTestVisible = false;

            _searchBox = new TextBox();
            _searchBox.Background = Brushes.Transparent;
            _searchBox.Foreground = UiTheme.TextPrimary;
            _searchBox.CaretBrush = UiTheme.TextPrimary;
            _searchBox.BorderThickness = new Thickness(0);
            _searchBox.FontSize = 13;
            _searchBox.VerticalAlignment = VerticalAlignment.Center;
            _searchBox.TextChanged += delegate
            {
                _placeholderText.Visibility = string.IsNullOrEmpty(_searchBox.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                RenderizarCards();
            };

            searchInner.Children.Add(_placeholderText);
            searchInner.Children.Add(_searchBox);
            searchDock.Children.Add(searchInner);
            searchContainer.Child = searchDock;
            Grid.SetColumn(searchContainer, 1);
            toolGrid.Children.Add(searchContainer);

            StackPanel rightTools = new StackPanel();
            rightTools.Orientation = Orientation.Horizontal;
            rightTools.VerticalAlignment = VerticalAlignment.Center;

            _btnToggleHiddenView = UiTheme.CreateActionButton("👁 Ocultos", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
            _btnToggleHiddenView.Width = 104;
            _btnToggleHiddenView.Height = 34;
            _btnToggleHiddenView.Margin = new Thickness(0, 0, 10, 0);
            _btnToggleHiddenView.ToolTip = "Alternar entre bases ativas e ocultas";
            _btnToggleHiddenView.Click += delegate
            {
                _showingHidden = !_showingHidden;
                RenderizarCards();
            };

            Border homeToggleGroup = new Border();
            homeToggleGroup.Background = UiTheme.BgHeader;
            homeToggleGroup.BorderBrush = UiTheme.BorderSubtle;
            homeToggleGroup.BorderThickness = new Thickness(1);
            homeToggleGroup.CornerRadius = new CornerRadius(8);
            homeToggleGroup.Padding = new Thickness(2);
            homeToggleGroup.Margin = new Thickness(0, 0, 10, 0);

            StackPanel homeToggleBtns = new StackPanel();
            homeToggleBtns.Orientation = Orientation.Horizontal;

            _btnViewCardsHome = new Button();
            _btnViewCardsHome.ToolTip = "Modo Cards (2 por linha)";
            _btnViewCardsHome.Padding = new Thickness(8, 6, 8, 6);
            _btnViewCardsHome.Cursor = System.Windows.Input.Cursors.Hand;
            _btnViewCardsHome.BorderThickness = new Thickness(0);

            _btnViewListHome = new Button();
            _btnViewListHome.ToolTip = "Modo Lista (Largura total)";
            _btnViewListHome.Padding = new Thickness(8, 6, 8, 6);
            _btnViewListHome.Cursor = System.Windows.Input.Cursors.Hand;
            _btnViewListHome.BorderThickness = new Thickness(0);

            homeToggleBtns.Children.Add(_btnViewCardsHome);
            homeToggleBtns.Children.Add(_btnViewListHome);
            homeToggleGroup.Child = homeToggleBtns;

            Action updateHomeViewToggleVisual = delegate
            {
                _btnViewCardsHome.Background = _homeCardMode ? UiTheme.PrimaryBlueBrush : Brushes.Transparent;
                _btnViewCardsHome.Content = UiTheme.CreateGridCardIcon(_homeCardMode ? Brushes.White : UiTheme.TextSecondary, 15);

                _btnViewListHome.Background = !_homeCardMode ? UiTheme.PrimaryBlueBrush : Brushes.Transparent;
                _btnViewListHome.Content = UiTheme.CreateListIcon(!_homeCardMode ? Brushes.White : UiTheme.TextSecondary, 15);
            };
            updateHomeViewToggleVisual();

            Action<bool> switchHomeView = delegate(bool cardMode)
            {
                if (_homeCardMode == cardMode) return;
                _homeCardMode = cardMode;
                try
                {
                    File.WriteAllText(_homeCardModePrefPath, _homeCardMode ? "1" : "0", Encoding.UTF8);
                }
                catch { }
                updateHomeViewToggleVisual();
                RenderizarCards();
            };

            _btnViewCardsHome.Click += delegate { switchHomeView(true); };
            _btnViewListHome.Click += delegate { switchHomeView(false); };

            _refreshIconPath = UiTheme.CreateThinRefreshIcon(UiTheme.AccentLightBlue);
            Button btnReload = UiTheme.CreateFramelessIconButton(
                _refreshIconPath,
                "Atualizar bases e status agora");
            btnReload.Click += delegate
            {
                GirarIconeAtualizacao1x();
                CarregarAmbientesAsync(true);
            };

            rightTools.Children.Add(_btnToggleHiddenView);
            rightTools.Children.Add(homeToggleGroup);
            rightTools.Children.Add(btnReload);
            Grid.SetColumn(rightTools, 2);
            toolGrid.Children.Add(rightTools);

            StackPanel toolbarWrapper = new StackPanel();
            toolbarWrapper.Children.Add(toolGrid);

            DockPanel infoBar = new DockPanel();
            infoBar.Margin = new Thickness(2, 10, 2, 0);

            StackPanel rightStatsStack = new StackPanel();
            rightStatsStack.Orientation = Orientation.Horizontal;
            DockPanel.SetDock(rightStatsStack, Dock.Right);

            _lastUpdateLabel = new TextBlock();
            _lastUpdateLabel.Foreground = UiTheme.TextMuted;
            _lastUpdateLabel.FontSize = 12;
            _lastUpdateLabel.Margin = new Thickness(0, 0, 10, 0);
            _lastUpdateLabel.Text = string.Format("Atualizado em {0:dd/MM/yyyy HH:mm:ss}  •", _lastUpdatedTime);

            _statsLabel = new TextBlock();
            _statsLabel.Foreground = UiTheme.TextPrimary;
            _statsLabel.FontSize = 12;
            _statsLabel.FontWeight = FontWeights.SemiBold;

            rightStatsStack.Children.Add(_lastUpdateLabel);
            rightStatsStack.Children.Add(_statsLabel);

            _rootPathLabel = new TextBlock();
            _rootPathLabel.Foreground = UiTheme.TextMuted;
            _rootPathLabel.FontSize = 12;
            _rootPathLabel.Text = "Diretório: " + _rootPath;

            infoBar.Children.Add(rightStatsStack);
            infoBar.Children.Add(_rootPathLabel);
            toolbarWrapper.Children.Add(infoBar);

            toolbar.Child = toolbarWrapper;
            Grid.SetRow(toolbar, 1);
            rootGrid.Children.Add(toolbar);

            _loadingTrackGrid = new Grid();
            _loadingTrackGrid.Height = 3;
            _loadingTrackGrid.Background = UiTheme.BorderSubtle;
            _loadingTrackGrid.ClipToBounds = true;
            _loadingTrackGrid.Margin = new Thickness(20, 0, 20, 6);

            _loadingBeam = new Border();
            _loadingBeam.Width = 240;
            _loadingBeam.Height = 3;
            _loadingBeam.CornerRadius = new CornerRadius(1.5);
            _loadingBeam.HorizontalAlignment = HorizontalAlignment.Left;
            _loadingBeam.Opacity = 0;

            LinearGradientBrush beamBrush = new LinearGradientBrush();
            beamBrush.StartPoint = new Point(0, 0);
            beamBrush.EndPoint = new Point(1, 0);
            beamBrush.GradientStops.Add(new GradientStop(Colors.Transparent, 0.0));
            beamBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#0284C7"), 0.5));
            beamBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#38BDF8"), 0.85));
            beamBrush.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));
            _loadingBeam.Background = beamBrush;

            _loadingBeamTransform = new TranslateTransform(-240, 0);
            _loadingBeam.RenderTransform = _loadingBeamTransform;
            _loadingTrackGrid.Children.Add(_loadingBeam);

            Grid.SetRow(_loadingTrackGrid, 2);
            rootGrid.Children.Add(_loadingTrackGrid);

            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.Padding = new Thickness(20, 4, 20, 18);
            _cardsScrollViewer = scroll;

            if (_homeCardMode)
            {
                Grid homeGrid = new Grid();
                homeGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
                _cardsContainer = homeGrid;
            }
            else
            {
                StackPanel sp = new StackPanel();
                sp.HorizontalAlignment = HorizontalAlignment.Stretch;
                _cardsContainer = sp;
            }

            scroll.Content = _cardsContainer;
            Grid.SetRow(scroll, 3);
            rootGrid.Children.Add(scroll);

            scroll.SizeChanged += delegate
            {
                if (_homeCardMode && _cardsContainer is Grid)
                {
                    AtualizarColunasGridMaximizado();
                }
            };

            _pageHost.Content = rootGrid;
            RenderizarCards();
        }

        private void AtualizarColunasGridMaximizado()
        {
            if (_homeCardMode)
            {
                RenderizarCards();
            }
        }

        private void GirarIconeAtualizacao1x()
        {
            if (_refreshIconPath == null) return;
            RotateTransform rt = _refreshIconPath.RenderTransform as RotateTransform;
            if (rt == null)
            {
                rt = new RotateTransform(0);
                _refreshIconPath.RenderTransform = rt;
            }

            DoubleAnimation spin = new DoubleAnimation();
            spin.From = 0;
            spin.To = 360;
            spin.Duration = TimeSpan.FromMilliseconds(650);
            spin.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut };
            rt.BeginAnimation(RotateTransform.AngleProperty, spin);
        }

        private void DispararAnimacaoLoadingHorizontal()
        {
            if (_loadingTrackGrid == null || _loadingBeam == null || _loadingBeamTransform == null)
                return;

            double trackWidth = _loadingTrackGrid.ActualWidth > 0 ? _loadingTrackGrid.ActualWidth : 960;
            _loadingBeam.Opacity = 1.0;

            DoubleAnimation slide = new DoubleAnimation();
            slide.From = -240;
            slide.To = trackWidth + 40;
            slide.Duration = TimeSpan.FromMilliseconds(850);
            slide.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut };
            slide.Completed += delegate
            {
                _loadingBeam.Opacity = 0;
            };

            _loadingBeamTransform.BeginAnimation(TranslateTransform.XProperty, slide);
        }

        private void AtualizarBotaoOcultos()
        {
            if (_btnToggleHiddenView == null) return;
            _btnToggleHiddenView.Content = _showingHidden ? "👁 Ativos" : "👁 Ocultos";
            _btnToggleHiddenView.Background = UiTheme.BrushFromHex(UiTheme.NeutralBtnHex);
        }

        private void RenderizarCards()
        {
            if (_cardsScrollViewer == null) return;

            AtualizarBotaoOcultos();

            if (_homeCardMode)
            {
                if (!(_cardsContainer is Grid))
                {
                    Grid homeGrid = new Grid();
                    homeGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
                    _cardsContainer = homeGrid;
                    _cardsScrollViewer.Content = _cardsContainer;
                }
            }
            else
            {
                if (!(_cardsContainer is StackPanel))
                {
                    StackPanel spNew = new StackPanel();
                    spNew.HorizontalAlignment = HorizontalAlignment.Stretch;
                    _cardsContainer = spNew;
                    _cardsScrollViewer.Content = _cardsContainer;
                }
            }

            _cardsContainer.Children.Clear();

            if (!_isInitialScanCompleted)
            {
                _cardsContainer.Children.Add(UiTheme.CreateVectorLoadingScreen(_cachedSvgPath));
                AtualizarResumoEVisualCards();
                return;
            }

            string filtro = (_searchBox != null && _searchBox.Text != null)
                ? _searchBox.Text.Trim().ToLowerInvariant()
                : "";

            List<Tuple<AmbienteInfo, bool>> listaAmbientesParaExibir = new List<Tuple<AmbienteInfo, bool>>();

            foreach (AmbienteInfo amb in _ambientes)
            {
                bool isHidden = _hiddenDirs.Contains(amb.AppDir);
                if (_showingHidden != isHidden)
                    continue;

                string notaCard = null;
                _cardNotes.TryGetValue(amb.AppDir, out notaCard);

                if (!string.IsNullOrEmpty(filtro))
                {
                    bool match = (amb.NomeIni != null && amb.NomeIni.ToLowerInvariant().Contains(filtro))
                              || (amb.PastaBase != null && amb.PastaBase.ToLowerInvariant().Contains(filtro))
                              || (amb.AppDir != null && amb.AppDir.ToLowerInvariant().Contains(filtro))
                              || (amb.WebAppPort != null && amb.WebAppPort.Contains(filtro))
                              || (amb.TcpPort != null && amb.TcpPort.Contains(filtro))
                              || (amb.RestPort != null && amb.RestPort.Contains(filtro))
                              || (amb.WebServicesPort != null && amb.WebServicesPort.Contains(filtro))
                              || (amb.SpecialKey != null && amb.SpecialKey.ToLowerInvariant().Contains(filtro))
                              || (amb.DBAlias != null && amb.DBAlias.ToLowerInvariant().Contains(filtro))
                              || (amb.DBPort != null && amb.DBPort.Contains(filtro))
                              || (amb.ProcessUser != null && amb.ProcessUser.ToLowerInvariant().Contains(filtro))
                              || (amb.ServiceName != null && amb.ServiceName.ToLowerInvariant().Contains(filtro))
                              || (!string.IsNullOrEmpty(notaCard) && notaCard.ToLowerInvariant().Contains(filtro));
                    if (!match) continue;
                }

                listaAmbientesParaExibir.Add(new Tuple<AmbienteInfo, bool>(amb, isHidden));
            }

            if (listaAmbientesParaExibir.Count == 0)
            {
                Border emptyBox = new Border();
                emptyBox.Background = UiTheme.BgCard;
                emptyBox.BorderBrush = UiTheme.BorderSubtle;
                emptyBox.BorderThickness = new Thickness(1);
                emptyBox.CornerRadius = new CornerRadius(12);
                emptyBox.Padding = new Thickness(30);
                emptyBox.Margin = new Thickness(0, 10, 0, 0);

                TextBlock emptyTxt = new TextBlock();
                if (_showingHidden)
                    emptyTxt.Text = "Nenhum ambiente oculto no momento.";
                else
                    emptyTxt.Text = Directory.Exists(_rootPath)
                        ? "Nenhum ambiente ativo encontrado."
                        : "A pasta configurada não existe: " + _rootPath;

                emptyTxt.Foreground = UiTheme.TextPrimary;
                emptyTxt.FontSize = 14;
                emptyTxt.HorizontalAlignment = HorizontalAlignment.Center;
                emptyBox.Child = emptyTxt;

                _cardsContainer.Children.Add(emptyBox);
                AtualizarResumoEVisualCards();
                return;
            }

            if (!_homeCardMode)
            {

                foreach (var item in listaAmbientesParaExibir)
                {
                    Border cardUi = CriarCardAmbiente(item.Item1, item.Item2);
                    _cardsContainer.Children.Add(cardUi);
                }
            }
            else
            {

                Grid gridContainer = _cardsContainer as Grid;
                if (gridContainer != null)
                {
                    gridContainer.Children.Clear();
                    gridContainer.ColumnDefinitions.Clear();

                    double availW = _cardsScrollViewer.ActualWidth > 0 ? _cardsScrollViewer.ActualWidth : ActualWidth;

                    int numCols = availW < 780 ? 1 : 2;

                    List<StackPanel> colPanels = new List<StackPanel>();
                    List<double> colHeights = new List<double>();

                    for (int c = 0; c < numCols; c++)
                    {
                        ColumnDefinition cd = new ColumnDefinition();
                        cd.Width = new GridLength(1, GridUnitType.Star);
                        gridContainer.ColumnDefinitions.Add(cd);

                        StackPanel colStack = new StackPanel();
                        colStack.HorizontalAlignment = HorizontalAlignment.Stretch;
                        colStack.Margin = new Thickness(0, 0, c < numCols - 1 ? 14 : 0, 0);

                        Grid.SetColumn(colStack, c);
                        gridContainer.Children.Add(colStack);
                        colPanels.Add(colStack);
                        colHeights.Add(0.0);
                    }

                    foreach (var item in listaAmbientesParaExibir)
                    {
                        AmbienteInfo amb = item.Item1;
                        bool isHidden = item.Item2;
                        Border cardUi = CriarCardAmbiente(amb, isHidden);

                        string nText;
                        bool hasNote = _cardNotes.TryGetValue(amb.AppDir, out nText) && !string.IsNullOrWhiteSpace(nText);
                        double estimatedHeight = 110 + (hasNote ? 48 : 0);

                        int bestCol = 0;
                        double minH = colHeights[0];
                        for (int c = 1; c < numCols; c++)
                        {
                            if (colHeights[c] < minH)
                            {
                                minH = colHeights[c];
                                bestCol = c;
                            }
                        }

                        colPanels[bestCol].Children.Add(cardUi);
                        colHeights[bestCol] += estimatedHeight;
                    }
                }
            }

            AtualizarResumoEVisualCards();
        }

        private Border CreateMetaPill(string label, string value)
        {
            Border pill = new Border();
            pill.Background = UiTheme.TagBg;
            pill.BorderBrush = UiTheme.BorderSubtle;
            pill.BorderThickness = new Thickness(1);
            pill.CornerRadius = new CornerRadius(5);
            pill.Padding = new Thickness(7, 2, 7, 2);
            pill.Margin = new Thickness(0, 0, 6, 4);

            StackPanel row = new StackPanel();
            row.Orientation = Orientation.Horizontal;
            row.VerticalAlignment = VerticalAlignment.Center;

            TextBlock lbl = new TextBlock();
            lbl.Text = label + ": ";
            lbl.Foreground = UiTheme.TextMuted;
            lbl.FontSize = 11;
            lbl.FontWeight = FontWeights.SemiBold;

            TextBlock val = new TextBlock();
            val.Text = string.IsNullOrWhiteSpace(value) ? "-" : value;
            val.Foreground = UiTheme.TextPrimary;
            val.FontSize = 11;
            val.FontWeight = FontWeights.Bold;

            row.Children.Add(lbl);
            row.Children.Add(val);
            pill.Child = row;
            return pill;
        }

        private Border CriarCardAmbiente(AmbienteInfo amb, bool isHidden)
        {
            Border card = new Border();
            card.Background = UiTheme.BgCard;
            card.BorderBrush = UiTheme.BorderSubtle;
            card.BorderThickness = new Thickness(1);
            card.CornerRadius = new CornerRadius(12);
            card.Padding = new Thickness(16, 14, 16, 14);
            card.Margin = new Thickness(0, 0, 0, 10);
            amb.CardBorder = card;

            Grid cardGrid = new Grid();
            RowDefinition rMain = new RowDefinition(); rMain.Height = GridLength.Auto;
            RowDefinition rNote = new RowDefinition(); rNote.Height = GridLength.Auto;
            cardGrid.RowDefinitions.Add(rMain);
            cardGrid.RowDefinitions.Add(rNote);

            ColumnDefinition colLeftTools = new ColumnDefinition(); colLeftTools.Width = GridLength.Auto;
            ColumnDefinition colInfo = new ColumnDefinition(); colInfo.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition colRightActions = new ColumnDefinition(); colRightActions.Width = GridLength.Auto;
            cardGrid.ColumnDefinitions.Add(colLeftTools);
            cardGrid.ColumnDefinitions.Add(colInfo);
            cardGrid.ColumnDefinitions.Add(colRightActions);

            StackPanel leftToolsColumn = new StackPanel();
            leftToolsColumn.Orientation = Orientation.Vertical;
            leftToolsColumn.VerticalAlignment = VerticalAlignment.Center;
            leftToolsColumn.HorizontalAlignment = HorizontalAlignment.Center;
            leftToolsColumn.Margin = new Thickness(0, 0, 16, 0);

            StackPanel toolsIconsRow = new StackPanel();
            toolsIconsRow.Orientation = Orientation.Horizontal;
            toolsIconsRow.HorizontalAlignment = HorizontalAlignment.Center;
            toolsIconsRow.VerticalAlignment = VerticalAlignment.Center;

            Button btnConfig = UiTheme.CreateFramelessIconButton(
                UiTheme.CreatePencilIcon(UiTheme.TextSecondary),
                "Configurar appserver.ini");
            btnConfig.Click += delegate { ShowIniEditorView(amb); };

            Button btnHide = UiTheme.CreateFramelessIconButton(
                isHidden ? UiTheme.CreateEyeOffIcon(UiTheme.AccentAmber) : UiTheme.CreateEyeIcon(UiTheme.TextSecondary),
                isHidden ? "Desocultar ambiente" : "Ocultar ambiente");
            btnHide.Margin = new Thickness(4, 0, 0, 0);
            btnHide.Click += delegate
            {
                if (_hiddenDirs.Contains(amb.AppDir))
                    _hiddenDirs.Remove(amb.AppDir);
                else
                    _hiddenDirs.Add(amb.AppDir);

                SalvarOcultosAppData();
                RecalcularAlertas();
                RenderizarCards();
            };

            Button btnNote = UiTheme.CreateFramelessIconButton(
                UiTheme.CreateCommentBubbleIcon(UiTheme.TextSecondary, 15),
                "Adicionar ou editar anotação deste ambiente");
            btnNote.Margin = new Thickness(4, 0, 0, 0);
            btnNote.Click += delegate { EditarNotaCard(amb); };

            toolsIconsRow.Children.Add(btnConfig);
            toolsIconsRow.Children.Add(btnHide);
            toolsIconsRow.Children.Add(btnNote);
            leftToolsColumn.Children.Add(toolsIconsRow);

            Border statusBadge = new Border();
            statusBadge.CornerRadius = new CornerRadius(10);
            statusBadge.Padding = new Thickness(8, 3, 8, 3);
            statusBadge.Margin = new Thickness(0, 6, 0, 0);
            statusBadge.HorizontalAlignment = HorizontalAlignment.Center;
            statusBadge.BorderThickness = new Thickness(1);

            StackPanel statusInner = new StackPanel();
            statusInner.Orientation = Orientation.Horizontal;
            statusInner.VerticalAlignment = VerticalAlignment.Center;
            statusInner.HorizontalAlignment = HorizontalAlignment.Center;

            Border statusDot = new Border();
            statusDot.Width = 7;
            statusDot.Height = 7;
            statusDot.CornerRadius = new CornerRadius(3.5);
            statusDot.Margin = new Thickness(0, 0, 5, 0);
            statusDot.VerticalAlignment = VerticalAlignment.Center;

            TextBlock statusTxt = new TextBlock();
            statusTxt.FontSize = 11;
            statusTxt.FontWeight = FontWeights.Bold;
            statusTxt.VerticalAlignment = VerticalAlignment.Center;

            statusInner.Children.Add(statusDot);
            statusInner.Children.Add(statusTxt);
            statusBadge.Child = statusInner;

            amb.StatusBadge = statusBadge;
            amb.StatusDot = statusDot;
            amb.StatusText = statusTxt;

            leftToolsColumn.Children.Add(statusBadge);

            Border userBadge = new Border();
            userBadge.Background = UiTheme.BrushFromHex("#0C4A6E");
            userBadge.BorderBrush = UiTheme.BrushFromHex(UiTheme.PrimaryBlueHex);
            userBadge.BorderThickness = new Thickness(1);
            userBadge.CornerRadius = new CornerRadius(5);
            userBadge.Padding = new Thickness(7, 2, 7, 2);
            userBadge.Margin = new Thickness(0, 4, 0, 0);
            userBadge.HorizontalAlignment = HorizontalAlignment.Center;
            userBadge.VerticalAlignment = VerticalAlignment.Center;
            TextBlock userTxt = new TextBlock();
            userTxt.Foreground = UiTheme.AccentLightBlue;
            userTxt.FontSize = 11;
            userTxt.FontWeight = FontWeights.Bold;
            userBadge.Child = userTxt;
            amb.UserTagBorder = userBadge;
            amb.UserTagText = userTxt;
            leftToolsColumn.Children.Add(userBadge);

            Grid.SetRow(leftToolsColumn, 0);
            Grid.SetColumn(leftToolsColumn, 0);
            cardGrid.Children.Add(leftToolsColumn);

            StackPanel infoColumn = new StackPanel();
            infoColumn.VerticalAlignment = VerticalAlignment.Center;
            infoColumn.Margin = new Thickness(0, 0, 12, 0);

            WrapPanel titleWrap = new WrapPanel();
            titleWrap.VerticalAlignment = VerticalAlignment.Center;

            TextBlock lblEnvName = new TextBlock();
            lblEnvName.Text = amb.NomeIni;
            lblEnvName.Foreground = UiTheme.BrushFromHex(UiTheme.PrimaryBlueHex);
            lblEnvName.FontSize = 15;
            lblEnvName.FontWeight = FontWeights.Bold;
            lblEnvName.Margin = new Thickness(0, 0, 8, 0);
            lblEnvName.VerticalAlignment = VerticalAlignment.Center;
            titleWrap.Children.Add(lblEnvName);

            infoColumn.Children.Add(titleWrap);

            TextBlock lblPath = new TextBlock();
            lblPath.Text = amb.AppDir;
            lblPath.Foreground = UiTheme.TextMuted;
            lblPath.FontSize = 11;
            lblPath.TextTrimming = TextTrimming.CharacterEllipsis;
            lblPath.Margin = new Thickness(0, 3, 0, 6);
            infoColumn.Children.Add(lblPath);

            WrapPanel metaWrap = new WrapPanel();
            metaWrap.Children.Add(CreateMetaPill("WebApp", !string.IsNullOrEmpty(amb.WebAppPort) ? amb.WebAppPort : "-"));
            metaWrap.Children.Add(CreateMetaPill("TCP", !string.IsNullOrEmpty(amb.TcpPort) ? amb.TcpPort : "-"));
            if (!string.IsNullOrEmpty(amb.RestPort))
            {
                metaWrap.Children.Add(CreateMetaPill("REST", amb.RestPort));
            }
            if (!string.IsNullOrEmpty(amb.WebServicesPort))
            {
                metaWrap.Children.Add(CreateMetaPill("WebServices", amb.WebServicesPort));
            }
            metaWrap.Children.Add(CreateMetaPill("SpecialKey", !string.IsNullOrEmpty(amb.SpecialKey) ? amb.SpecialKey : "-"));
            metaWrap.Children.Add(CreateMetaPill("DBAlias", !string.IsNullOrEmpty(amb.DBAlias) ? amb.DBAlias : "-"));
            metaWrap.Children.Add(CreateMetaPill("DBPort", !string.IsNullOrEmpty(amb.DBPort) ? amb.DBPort : "-"));
            infoColumn.Children.Add(metaWrap);

            Grid.SetRow(infoColumn, 0);
            Grid.SetColumn(infoColumn, 1);
            cardGrid.Children.Add(infoColumn);

            StackPanel rightActionsColumn = new StackPanel();
            rightActionsColumn.Orientation = Orientation.Horizontal;
            rightActionsColumn.VerticalAlignment = VerticalAlignment.Center;
            rightActionsColumn.HorizontalAlignment = HorizontalAlignment.Right;

            StackPanel accessButtonsStack = new StackPanel();
            accessButtonsStack.Orientation = Orientation.Vertical;
            accessButtonsStack.VerticalAlignment = VerticalAlignment.Center;
            accessButtonsStack.Margin = new Thickness(0, 0, 8, 0);

            Button btnWeb = UiTheme.CreateWebLinkButton("Protheus", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
            btnWeb.ToolTip = !string.IsNullOrEmpty(amb.WebAppPort)
                ? string.Format("Abrir http://localhost:{0}", amb.WebAppPort)
                : "Porta WEBAPP não configurada no appserver.ini";
            if (string.IsNullOrEmpty(amb.WebAppPort))
                btnWeb.Opacity = 0.50;
            btnWeb.Click += delegate { AbrirNavegador(amb); };
            amb.BtnWeb = btnWeb;
            accessButtonsStack.Children.Add(btnWeb);

            if (!string.IsNullOrEmpty(amb.RestPort))
            {
                Button btnRest = UiTheme.CreateWebLinkButton("REST", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
                btnRest.ToolTip = string.Format("Abrir http://localhost:{0}/rest", amb.RestPort);
                btnRest.Click += delegate { AbrirRestNavegador(amb); };
                amb.BtnRest = btnRest;
                accessButtonsStack.Children.Add(btnRest);
            }
            else
            {
                amb.BtnRest = null;
            }

            bool hasCustomWs = _customLinks.Any(l => string.Equals(l.Label, "WebServices", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(amb.WebServicesPort) && !hasCustomWs)
            {
                Button btnWs = UiTheme.CreateWebLinkButton("WebServices", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
                btnWs.ToolTip = string.Format("Abrir http://localhost:{0}/ws", amb.WebServicesPort);
                btnWs.Click += delegate { AbrirWebServicesNavegador(amb); };
                amb.BtnWebServices = btnWs;
                accessButtonsStack.Children.Add(btnWs);
            }
            else
            {
                amb.BtnWebServices = null;
            }

            amb.CustomButtons.Clear();
            foreach (CustomLinkButton linkDef in _customLinks)
            {
                CustomLinkButton capturedDef = linkDef;
                string resolvedUrl = ResolverFormulaLink(amb, capturedDef.Formula);
                if (string.IsNullOrEmpty(resolvedUrl))
                    continue;

                Button btnCustom = UiTheme.CreateWebLinkButton(capturedDef.Label, UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
                btnCustom.ToolTip = string.Format("Abrir {0}", resolvedUrl);
                btnCustom.Click += delegate { AbrirLinkCustomizado(amb, capturedDef); };
                amb.CustomButtons.Add(btnCustom);
                accessButtonsStack.Children.Add(btnCustom);
            }

            rightActionsColumn.Children.Add(accessButtonsStack);

            StackPanel powerButtonsStack = new StackPanel();
            powerButtonsStack.Orientation = Orientation.Vertical;
            powerButtonsStack.VerticalAlignment = VerticalAlignment.Center;

            Button btnStart = UiTheme.CreateActionButton(
                UiTheme.CreatePlayIcon(Brushes.White, 13),
                "#059669",
                "#10B981");
            btnStart.Width = 34;
            btnStart.Height = 26;
            btnStart.Padding = new Thickness(0);
            btnStart.Margin = new Thickness(0, 2, 0, 2);
            btnStart.ToolTip = "Iniciar";
            btnStart.Click += delegate { IniciarAmbienteAsync(amb, null); };
            amb.BtnStart = btnStart;

            Button btnRestart = UiTheme.CreateActionButton(
                UiTheme.CreateRestartIcon(Brushes.White, 13),
                "#D97706",
                "#F59E0B");
            btnRestart.Width = 34;
            btnRestart.Height = 26;
            btnRestart.Padding = new Thickness(0);
            btnRestart.Margin = new Thickness(0, 2, 0, 2);
            btnRestart.ToolTip = "Reiniciar";
            btnRestart.Click += delegate { ReiniciarAmbienteAsync(amb); };
            amb.BtnRestart = btnRestart;

            Button btnStop = UiTheme.CreateActionButton(
                UiTheme.CreateStopXIcon(Brushes.White, 13),
                "#DC2626",
                "#EF4444");
            btnStop.Width = 34;
            btnStop.Height = 26;
            btnStop.Padding = new Thickness(0);
            btnStop.Margin = new Thickness(0, 2, 0, 2);
            btnStop.ToolTip = "Parar";
            btnStop.Click += delegate { EncerrarAmbienteAsync(amb); };
            amb.BtnStop = btnStop;

            powerButtonsStack.Children.Add(btnStart);
            powerButtonsStack.Children.Add(btnRestart);
            powerButtonsStack.Children.Add(btnStop);
            rightActionsColumn.Children.Add(powerButtonsStack);

            Grid.SetRow(rightActionsColumn, 0);
            Grid.SetColumn(rightActionsColumn, 2);
            cardGrid.Children.Add(rightActionsColumn);

            string noteText;
            if (_cardNotes.TryGetValue(amb.AppDir, out noteText) && !string.IsNullOrWhiteSpace(noteText))
            {
                Border noteBanner = new Border();
                noteBanner.Background = UiTheme.NoteBannerBg;
                noteBanner.BorderBrush = UiTheme.BorderSubtle;
                noteBanner.BorderThickness = new Thickness(1);
                noteBanner.CornerRadius = new CornerRadius(7);
                noteBanner.Padding = new Thickness(10, 6, 10, 6);
                noteBanner.Margin = new Thickness(0, 10, 0, 0);
                noteBanner.Cursor = Cursors.Hand;
                noteBanner.ToolTip = "Clique para editar a anotação";
                noteBanner.MouseLeftButtonUp += delegate { EditarNotaCard(amb); };

                TextBlock noteLbl = new TextBlock();
                noteLbl.Text = "📝 " + noteText;
                noteLbl.Foreground = UiTheme.NoteTextFg;
                noteLbl.FontSize = 12;
                noteLbl.TextWrapping = TextWrapping.Wrap;
                noteBanner.Child = noteLbl;

                Grid.SetRow(noteBanner, 1);
                Grid.SetColumnSpan(noteBanner, 3);
                cardGrid.Children.Add(noteBanner);
            }

            card.Child = cardGrid;
            AtualizarVisualCard(amb);
            return card;
        }

        private void EditarNotaCard(AmbienteInfo amb)
        {
            string atual = "";
            _cardNotes.TryGetValue(amb.AppDir, out atual);

            Window dlg = new Window();
            dlg.Title = "Anotação — " + amb.NomeIni;
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
            lblTitle.Text = "Anotação para " + amb.NomeIni;
            lblTitle.Foreground = UiTheme.TextPrimary;
            lblTitle.FontSize = 14;
            lblTitle.FontWeight = FontWeights.Bold;
            lblTitle.VerticalAlignment = VerticalAlignment.Center;
            topRow.Children.Add(lblTitle);
            stack.Children.Add(topRow);

            TextBox txtNote = new TextBox();
            txtNote.Text = atual ?? "";
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
            txtNote.Margin = new Thickness(0, 0, 0, 14);
            stack.Children.Add(txtNote);

            StackPanel btnsRow = new StackPanel();
            btnsRow.Orientation = Orientation.Horizontal;
            btnsRow.HorizontalAlignment = HorizontalAlignment.Right;

            Button btnClear = UiTheme.CreateActionButton("Limpar", UiTheme.NeutralBtnHex, "#DC2626");
            btnClear.Margin = new Thickness(0, 0, 8, 0);
            btnClear.Click += delegate
            {
                _cardNotes.Remove(amb.AppDir);
                SalvarNotasAppData();
                dlg.Close();
                RenderizarCards();
            };

            Button btnSave = UiTheme.CreateActionButton("Salvar", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
            btnSave.Click += delegate
            {
                string val = (txtNote.Text ?? "").Trim();
                if (string.IsNullOrEmpty(val))
                    _cardNotes.Remove(amb.AppDir);
                else
                    _cardNotes[amb.AppDir] = val;

                SalvarNotasAppData();
                dlg.Close();
                RenderizarCards();
            };

            btnsRow.Children.Add(btnClear);
            btnsRow.Children.Add(btnSave);
            stack.Children.Add(btnsRow);

            outer.Child = stack;
            dlg.Content = outer;
            dlg.ShowDialog();
        }

        private void AtualizarVisualCard(AmbienteInfo amb)
        {
            if (amb.CardBorder == null) return;

            if (amb.ModeTagText != null)
            {
                string modoBase = amb.IsServiceRegistered
                    ? string.Format("Serviço ({0})", amb.ServiceName)
                    : "Console";

                if (amb.State == EnvRunState.Active && amb.Pid > 0)
                {
                    string execMode = amb.RunningAsService ? "Serviço" : "Console";
                    amb.ModeTagText.Text = string.Format("{0}  •  PID {1}", execMode, amb.Pid);
                }
                else
                {
                    amb.ModeTagText.Text = modoBase;
                }
            }

            if (amb.UserTagBorder != null && amb.UserTagText != null)
            {
                if (amb.State == EnvRunState.Active && (amb.Pid > 0 || !string.IsNullOrEmpty(amb.ProcessUser)))
                {
                    string userInfo = "";
                    if (_showProcessUser && !string.IsNullOrEmpty(amb.ProcessUser))
                        userInfo = "👤 " + amb.ProcessUser;
                    if (amb.Pid > 0)
                        userInfo = string.IsNullOrEmpty(userInfo) ? "PID " + amb.Pid : userInfo + "  •  PID " + amb.Pid;

                    amb.UserTagText.Text = userInfo;
                    amb.UserTagBorder.Visibility = string.IsNullOrEmpty(userInfo) ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    amb.UserTagBorder.Visibility = Visibility.Collapsed;
                }
            }

            if (amb.State == EnvRunState.Active)
            {
                amb.CardBorder.Background = amb.RunningAsService ? UiTheme.BgCardServiceActive : UiTheme.BgCardConsoleActive;
                amb.CardBorder.BorderBrush = amb.RunningAsService ? UiTheme.BrushFromHex(UiTheme.PrimaryBlueHex) : UiTheme.AccentGreen;
                amb.StatusBadge.Background = UiTheme.BrushFromHex("#064E3B");
                amb.StatusBadge.BorderBrush = UiTheme.AccentGreen;
                amb.StatusDot.Background = UiTheme.AccentGreen;
                amb.StatusText.Text = "RUNNING";
                amb.StatusText.Foreground = UiTheme.AccentGreen;

                amb.BtnStart.Visibility = Visibility.Visible;
                amb.BtnRestart.Visibility = Visibility.Visible;
                amb.BtnStop.Visibility = Visibility.Visible;
                amb.BtnStart.IsEnabled = false;
                amb.BtnStart.Opacity = 0.30;
                amb.BtnRestart.IsEnabled = true;
                amb.BtnRestart.Opacity = 1.0;
                amb.BtnStop.IsEnabled = true;
                amb.BtnStop.Opacity = 1.0;
            }
            else if (amb.State == EnvRunState.Initializing)
            {
                amb.CardBorder.Background = UiTheme.BgCard;
                amb.CardBorder.BorderBrush = UiTheme.AccentAmber;
                amb.StatusBadge.Background = UiTheme.BrushFromHex("#451A03");
                amb.StatusBadge.BorderBrush = UiTheme.AccentAmber;
                amb.StatusDot.Background = UiTheme.AccentAmber;
                amb.StatusText.Text = "STARTING";
                amb.StatusText.Foreground = UiTheme.AccentAmber;

                amb.BtnStart.Visibility = Visibility.Visible;
                amb.BtnRestart.Visibility = Visibility.Visible;
                amb.BtnStop.Visibility = Visibility.Visible;
                amb.BtnStart.IsEnabled = false;
                amb.BtnStart.Opacity = 0.30;
                amb.BtnRestart.IsEnabled = false;
                amb.BtnRestart.Opacity = 0.30;
                amb.BtnStop.IsEnabled = false;
                amb.BtnStop.Opacity = 0.30;
            }
            else if (amb.State == EnvRunState.Stopping)
            {
                amb.CardBorder.Background = UiTheme.BgCard;
                amb.CardBorder.BorderBrush = UiTheme.AccentAmber;
                amb.StatusBadge.Background = UiTheme.BrushFromHex("#451A03");
                amb.StatusBadge.BorderBrush = UiTheme.AccentAmber;
                amb.StatusDot.Background = UiTheme.AccentAmber;
                amb.StatusText.Text = "STOPPING";
                amb.StatusText.Foreground = UiTheme.AccentAmber;

                amb.BtnStart.Visibility = Visibility.Visible;
                amb.BtnRestart.Visibility = Visibility.Visible;
                amb.BtnStop.Visibility = Visibility.Visible;
                amb.BtnStart.IsEnabled = false;
                amb.BtnStart.Opacity = 0.30;
                amb.BtnRestart.IsEnabled = false;
                amb.BtnRestart.Opacity = 0.30;
                amb.BtnStop.IsEnabled = false;
                amb.BtnStop.Opacity = 0.30;
            }
            else
            {
                amb.CardBorder.Background = UiTheme.BgCard;
                amb.CardBorder.BorderBrush = UiTheme.BorderSubtle;
                amb.StatusBadge.Background = UiTheme.TagBg;
                amb.StatusBadge.BorderBrush = UiTheme.BorderSubtle;
                amb.StatusDot.Background = UiTheme.TextMuted;
                amb.StatusText.Text = "STOPPED";
                amb.StatusText.Foreground = UiTheme.TextSecondary;

                amb.BtnStart.Visibility = Visibility.Visible;
                amb.BtnRestart.Visibility = Visibility.Visible;
                amb.BtnStop.Visibility = Visibility.Visible;
                amb.BtnStart.IsEnabled = true;
                amb.BtnStart.Opacity = 1.0;
                amb.BtnRestart.IsEnabled = false;
                amb.BtnRestart.Opacity = 0.30;
                amb.BtnStop.IsEnabled = false;
                amb.BtnStop.Opacity = 0.30;
            }
        }

        private void AtualizarResumoEVisualCards()
        {
            int totalVisiveis = 0;
            int ativosCount = 0;

            foreach (AmbienteInfo amb in _ambientes)
            {
                AtualizarVisualCard(amb);
                if (!_hiddenDirs.Contains(amb.AppDir))
                {
                    totalVisiveis++;
                    if (amb.State == EnvRunState.Active)
                        ativosCount++;
                }
            }

            if (_statsLabel != null)
            {
                _statsLabel.Text = string.Format("{0} de {1} ativos ({2} ocultos)",
                    ativosCount,
                    totalVisiveis,
                    _hiddenDirs.Count);
            }
        }

        private string ResolverFormulaLink(AmbienteInfo amb, string formula)
        {
            if (amb == null || string.IsNullOrWhiteSpace(formula))
                return null;

            bool missingKey = false;
            string result = Regex.Replace(formula.Trim(), @"(\{([^}]+)\}|<([^>]+)>)", delegate(Match m)
            {
                string inner = m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Value;
                inner = (inner ?? "").Trim();
                if (string.IsNullOrEmpty(inner))
                {
                    missingKey = true;
                    return "";
                }

                string secName = null;
                string keyName = null;

                char[] seps = new char[] { ':', '>', '/', '.' };
                int sepIdx = inner.IndexOfAny(seps);
                if (sepIdx > 0)
                {
                    secName = inner.Substring(0, sepIdx).Trim().Trim('[', ']');
                    keyName = inner.Substring(sepIdx + 1).Trim();
                }
                else
                {
                    keyName = inner.Trim();
                }

                if (string.Equals(secName, "ambiente", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(secName, "<ambiente>", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(secName, "environment", StringComparison.OrdinalIgnoreCase))
                {
                    secName = amb.NomeIni;
                }

                string val = null;
                if (!string.IsNullOrEmpty(secName) && !string.IsNullOrEmpty(keyName))
                {
                    Dictionary<string, string> secDict;
                    if (amb.IniSectionsMap.TryGetValue(secName, out secDict))
                    {
                        secDict.TryGetValue(keyName, out val);
                    }
                    if (string.IsNullOrWhiteSpace(val) && string.Equals(keyName, "Port", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(secName, "HTTPREST", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(secName, "REST", StringComparison.OrdinalIgnoreCase))
                        {
                            val = amb.RestPort;
                        }
                        else if (string.Equals(secName, "WEBSERVICES", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(secName, "WS", StringComparison.OrdinalIgnoreCase))
                        {
                            val = amb.WebServicesPort;
                        }
                        else if (string.Equals(secName, "WEBAPP", StringComparison.OrdinalIgnoreCase))
                        {
                            val = amb.WebAppPort;
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(keyName))
                {
                    if (string.Equals(keyName, "WebAppPort", StringComparison.OrdinalIgnoreCase))
                        val = amb.WebAppPort;
                    else if (string.Equals(keyName, "RestPort", StringComparison.OrdinalIgnoreCase))
                        val = amb.RestPort;
                    else if (string.Equals(keyName, "WebServicesPort", StringComparison.OrdinalIgnoreCase))
                        val = amb.WebServicesPort;
                    else if (string.Equals(keyName, "TcpPort", StringComparison.OrdinalIgnoreCase))
                        val = amb.TcpPort;
                    else if (string.Equals(keyName, "SpecialKey", StringComparison.OrdinalIgnoreCase))
                        val = amb.SpecialKey;
                    else if (string.Equals(keyName, "DBAlias", StringComparison.OrdinalIgnoreCase))
                        val = amb.DBAlias;
                    else if (string.Equals(keyName, "DBPort", StringComparison.OrdinalIgnoreCase))
                        val = amb.DBPort;
                    else if (string.Equals(keyName, "ambiente", StringComparison.OrdinalIgnoreCase))
                        val = amb.NomeIni;
                    else
                        val = ObterValorChaveIni(amb, amb.NomeIni, keyName);
                }

                if (string.IsNullOrWhiteSpace(val))
                {
                    missingKey = true;
                    return "";
                }
                return val.Trim();
            });

            if (missingKey || string.IsNullOrWhiteSpace(result))
                return null;

            if (!result.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !result.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                result = "http://" + result;
            }
            return result;
        }

        private void AbrirUrlNoNavegador(string url, int delaySeconds)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            url = url.Trim();
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "http://" + url;
            }

            Uri uriResult;
            bool isSafe = Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (!isSafe) return;

            ThreadPool.QueueUserWorkItem(delegate
            {
                if (delaySeconds > 0)
                    Thread.Sleep(delaySeconds * 1000);
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = uriResult.AbsoluteUri;
                    psi.UseShellExecute = true;
                    Process.Start(psi);
                }
                catch { }
            });
        }

        private void AbrirNavegador(AmbienteInfo amb)
        {
            if (string.IsNullOrEmpty(amb.WebAppPort))
            {
                StyledDialog.Info(this, "Porta WebApp Ausente", "Este ambiente não possui porta configurada na seção [WEBAPP].", amb.IniPath);
                return;
            }

            string url = string.Format("http://localhost:{0}", amb.WebAppPort);
            if (amb.State != EnvRunState.Active)
            {
                int delay = _browserDelaySeconds;
                IniciarAmbienteAsync(amb, delegate { AbrirUrlNoNavegador(url, delay); });
            }
            else
            {
                AbrirUrlNoNavegador(url, 0);
            }
        }

        private void AbrirRestNavegador(AmbienteInfo amb)
        {
            if (string.IsNullOrEmpty(amb.RestPort))
            {
                StyledDialog.Info(this, "Porta REST Ausente", "Este ambiente não possui porta configurada na seção [HTTPREST].", amb.IniPath);
                return;
            }

            string url = string.Format("http://localhost:{0}/rest", amb.RestPort);
            if (amb.State != EnvRunState.Active)
            {
                int delay = _browserDelaySeconds;
                IniciarAmbienteAsync(amb, delegate { AbrirUrlNoNavegador(url, delay); });
            }
            else
            {
                AbrirUrlNoNavegador(url, 0);
            }
        }

        private void AbrirWebServicesNavegador(AmbienteInfo amb)
        {
            if (string.IsNullOrEmpty(amb.WebServicesPort))
            {
                StyledDialog.Info(this, "Porta WebServices Ausente", "Este ambiente não possui porta configurada para WebServices.", amb.IniPath);
                return;
            }

            string url = string.Format("http://localhost:{0}/ws", amb.WebServicesPort);
            if (amb.State != EnvRunState.Active)
            {
                int delay = _browserDelaySeconds;
                IniciarAmbienteAsync(amb, delegate { AbrirUrlNoNavegador(url, delay); });
            }
            else
            {
                AbrirUrlNoNavegador(url, 0);
            }
        }

        private void AbrirLinkCustomizado(AmbienteInfo amb, CustomLinkButton linkDef)
        {
            string url = ResolverFormulaLink(amb, linkDef.Formula);
            if (string.IsNullOrEmpty(url))
            {
                StyledDialog.Info(
                    this,
                    "Chave Não Encontrada",
                    string.Format("Não foi possível montar o link \"{0}\" para o ambiente {1}.", linkDef.Label, amb.NomeIni),
                    string.Format("Verifique se as seções/chaves da fórmula ({0}) existem no appserver.ini.", linkDef.Formula));
                return;
            }

            if (amb.State != EnvRunState.Active)
            {
                int delay = _browserDelaySeconds;
                IniciarAmbienteAsync(amb, delegate { AbrirUrlNoNavegador(url, delay); });
            }
            else
            {
                AbrirUrlNoNavegador(url, 0);
            }
        }

        private static void ExecutarComandoSilencioso(string fileName, string args, bool tentarElevadoSeFalhar)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = fileName;
                psi.Arguments = args;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                using (Process p = Process.Start(psi))
                {
                    if (p != null)
                        p.WaitForExit(3000);
                }
            }
            catch
            {
                if (tentarElevadoSeFalhar)
                {
                    try
                    {
                        ProcessStartInfo psiElevated = new ProcessStartInfo();
                        psiElevated.FileName = fileName;
                        psiElevated.Arguments = args;
                        psiElevated.Verb = "runas";
                        psiElevated.UseShellExecute = true;
                        psiElevated.WindowStyle = ProcessWindowStyle.Hidden;
                        using (Process p = Process.Start(psiElevated))
                        {
                            if (p != null)
                                p.WaitForExit(3000);
                        }
                    }
                    catch { }
                }
            }
        }

        private void IniciarPorLinhaDeComandoOuServico(AmbienteInfo amb, bool preferService = true)
        {
            bool startedService = false;
            if (preferService && amb.IsServiceRegistered && !string.IsNullOrEmpty(amb.ServiceName))
            {
                try
                {
                    using (ServiceController sc = new ServiceController(amb.ServiceName))
                    {
                        if (sc.Status == ServiceControllerStatus.Stopped || sc.Status == ServiceControllerStatus.Paused)
                        {
                            sc.Start();
                            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(8));
                            startedService = true;
                        }
                        else if (sc.Status == ServiceControllerStatus.Running)
                        {
                            startedService = true;
                        }
                    }
                }
                catch
                {
                    ExecutarComandoSilencioso("net.exe", string.Format("start \"{0}\"", amb.ServiceName), true);
                    Thread.Sleep(1000);
                    startedService = true;
                }
            }

            if (!startedService && File.Exists(amb.ExePath))
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = amb.ExePath;
                    psi.Arguments = "-console";
                    psi.WorkingDirectory = amb.AppDir;
                    psi.UseShellExecute = true;
                    psi.Verb = "runas";
                    Process.Start(psi);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    try
                    {
                        ProcessStartInfo fallback = new ProcessStartInfo();
                        fallback.FileName = amb.ExePath;
                        fallback.Arguments = "-console";
                        fallback.WorkingDirectory = amb.AppDir;
                        fallback.UseShellExecute = true;
                        Process.Start(fallback);
                    }
                    catch { }
                }
                catch { }
            }
        }

        private void PararPorPidOuServico(AmbienteInfo amb)
        {
            if (!string.IsNullOrEmpty(amb.ServiceName))
            {
                try
                {
                    using (ServiceController sc = new ServiceController(amb.ServiceName))
                    {
                        if (sc.Status != ServiceControllerStatus.Stopped)
                        {
                            if (sc.Status != ServiceControllerStatus.StopPending)
                            {
                                sc.Stop();
                            }
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(6));
                        }
                    }
                }
                catch
                {
                    ExecutarComandoSilencioso("net.exe", string.Format("stop \"{0}\" /y", amb.ServiceName), true);
                    ExecutarComandoSilencioso("sc.exe", string.Format("stop \"{0}\"", amb.ServiceName), true);
                }
            }

            if (amb.Pid > 0)
            {
                try
                {
                    using (Process p = Process.GetProcessById(amb.Pid))
                    {
                        if (!p.HasExited)
                        {
                            p.Kill();
                            p.WaitForExit(3000);
                        }
                    }
                }
                catch
                {
                    ExecutarComandoSilencioso("taskkill.exe", string.Format("/F /T /PID {0}", amb.Pid), true);
                }
            }

            if (!string.IsNullOrEmpty(amb.ExePath) && File.Exists(amb.ExePath))
            {
                string targetExeNorm = NormalizePath(amb.ExePath);
                try
                {
                    foreach (Process p in Process.GetProcesses())
                    {
                        try
                        {
                            if (p.ProcessName.IndexOf("appserver", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                string pPath = NativeWin32.GetProcessImagePathSafe(p.Id);
                                if (string.IsNullOrEmpty(pPath))
                                {
                                    try { pPath = p.MainModule.FileName; } catch { }
                                }
                                if (!string.IsNullOrEmpty(pPath) && string.Equals(NormalizePath(pPath), targetExeNorm, StringComparison.OrdinalIgnoreCase))
                                {
                                    try
                                    {
                                        if (!p.HasExited)
                                        {
                                            p.Kill();
                                            p.WaitForExit(2000);
                                        }
                                    }
                                    catch
                                    {
                                        ExecutarComandoSilencioso("taskkill.exe", string.Format("/F /T /PID {0}", p.Id), true);
                                    }
                                }
                            }
                        }
                        catch { }
                        finally
                        {
                            p.Dispose();
                        }
                    }
                }
                catch { }
            }

            int waited = 0;
            while (waited < 5000)
            {
                bool anyAlive = false;
                if (amb.Pid > 0)
                {
                    try
                    {
                        using (Process p = Process.GetProcessById(amb.Pid))
                        {
                            if (!p.HasExited) anyAlive = true;
                        }
                    }
                    catch { }
                }

                if (!anyAlive && !string.IsNullOrEmpty(amb.ServiceName))
                {
                    try
                    {
                        using (ServiceController sc = new ServiceController(amb.ServiceName))
                        {
                            if (sc.Status != ServiceControllerStatus.Stopped)
                                anyAlive = true;
                        }
                    }
                    catch { }
                }

                if (!anyAlive)
                    break;

                Thread.Sleep(250);
                waited += 250;
            }
        }

        private void IniciarAmbienteAsync(AmbienteInfo amb, Action onStartedCallback)
        {
            if (amb.State == EnvRunState.Initializing || amb.State == EnvRunState.Stopping)
                return;

            if (amb.State == EnvRunState.Active)
            {
                if (onStartedCallback != null)
                    onStartedCallback();
                return;
            }

            amb.State = EnvRunState.Initializing;
            AtualizarResumoEVisualCards();
            bool fetchUser = _showProcessUser;

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    IniciarPorLinhaDeComandoOuServico(amb, amb.IsServiceRegistered);
                    Thread.Sleep(1500);
                    SincronizarEstadoProcessosEServicos(_ambientes.ToList(), fetchUser);
                }
                finally
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        if (amb.State == EnvRunState.Initializing)
                            amb.State = EnvRunState.Stopped;
                        SincronizarEstadoProcessosEServicos(new List<AmbienteInfo> { amb }, fetchUser);
                        AtualizarResumoEVisualCards();
                        if (amb.State == EnvRunState.Active && onStartedCallback != null)
                            onStartedCallback();
                    }));
                }
            });
        }

        private void EncerrarAmbienteAsync(AmbienteInfo amb)
        {
            if (amb.State == EnvRunState.Stopped || amb.State == EnvRunState.Stopping)
                return;

            amb.State = EnvRunState.Stopping;
            AtualizarResumoEVisualCards();
            bool fetchUser = _showProcessUser;

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    PararPorPidOuServico(amb);
                    Thread.Sleep(800);
                }
                finally
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        amb.State = EnvRunState.Stopped;
                        amb.Pid = 0;
                        amb.ProcessUser = null;
                        SincronizarEstadoProcessosEServicos(new List<AmbienteInfo> { amb }, fetchUser);
                        AtualizarResumoEVisualCards();
                    }));
                }
            });
        }

        private void ReiniciarAmbienteAsync(AmbienteInfo amb)
        {
            if (amb.State == EnvRunState.Initializing || amb.State == EnvRunState.Stopping)
                return;

            amb.State = EnvRunState.Initializing;
            AtualizarResumoEVisualCards();
            bool fetchUser = _showProcessUser;
            bool wasService = amb.RunningAsService && !string.IsNullOrEmpty(amb.ServiceName);

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    PararPorPidOuServico(amb);
                    Thread.Sleep(1500);
                    IniciarPorLinhaDeComandoOuServico(amb, wasService);
                    Thread.Sleep(2000);
                    SincronizarEstadoProcessosEServicos(_ambientes.ToList(), fetchUser);
                }
                finally
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        if (amb.State == EnvRunState.Initializing)
                            amb.State = EnvRunState.Stopped;
                        SincronizarEstadoProcessosEServicos(new List<AmbienteInfo> { amb }, fetchUser);
                        AtualizarResumoEVisualCards();
                    }));
                }
            });
        }
    }
}
