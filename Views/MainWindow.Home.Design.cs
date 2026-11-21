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
        
                    
                    StackPanel titleLeft = CreateBrandHeaderWithTabs(HubActiveTab.Ambientes);
        
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
        
                    Border toggleSegmented = UiTheme.CreateViewModeSegmentedToggle(_isCardsViewMode, delegate(bool isCards)
                    {
                        _isCardsViewMode = isCards;
                        RenderizarCards();
                    });
                    toggleSegmented.Margin = new Thickness(0, 0, 10, 0);
                    rightTools.Children.Add(toggleSegmented);
        
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
        
                    StackPanel sp = new StackPanel();
                    _cardsContainer = sp;
        
                    scroll.Content = _cardsContainer;
                    Grid.SetRow(scroll, 3);
                    rootGrid.Children.Add(scroll);
        
                    _pageHost.Content = rootGrid;
                    RenderizarCards();
                }

        private void AtualizarColunasGridMaximizado()
                {
                    
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
                    if (_cardsContainer == null) return;
        
                    if (_isCardsViewMode)
                    {
                        if (!(_cardsContainer is System.Windows.Controls.Primitives.UniformGrid))
                        {
                            System.Windows.Controls.Primitives.UniformGrid ug = new System.Windows.Controls.Primitives.UniformGrid();
                            ug.Columns = 2;
                            _cardsContainer = ug;
                            if (_cardsScrollViewer != null) _cardsScrollViewer.Content = _cardsContainer;
                        }
                    }
                    else
                    {
                        if (!(_cardsContainer is StackPanel))
                        {
                            StackPanel spNew = new StackPanel();
                            _cardsContainer = spNew;
                            if (_cardsScrollViewer != null) _cardsScrollViewer.Content = _cardsContainer;
                        }
                    }
        
                    AtualizarBotaoOcultos();
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
        
                    int visiveis = 0;
        
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
        
                        visiveis++;
                        Border cardUi = CriarCardAmbiente(amb, isHidden);
                        _cardsContainer.Children.Add(cardUi);
                    }
        
                    if (visiveis == 0)
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
                    pill.Padding = new Thickness(6, 2, 6, 2);
                    pill.Margin = new Thickness(0, 0, 5, 4);
                    pill.Cursor = Cursors.Hand;
                    pill.HorizontalAlignment = HorizontalAlignment.Left;
                    pill.ToolTip = string.Format("Clique para copiar {0}: {1}", label, value);

                    DockPanel dp = new DockPanel();
                    dp.LastChildFill = true;

                    FrameworkElement copyIcon = UiTheme.CreateCopyIcon(UiTheme.TextMuted, 9) as FrameworkElement;
                    if (copyIcon != null)
                    {
                        copyIcon.VerticalAlignment = VerticalAlignment.Center;
                        copyIcon.Margin = new Thickness(5, 0, 0, 0);
                        DockPanel.SetDock(copyIcon, Dock.Right);
                        dp.Children.Add(copyIcon);
                    }

                    StackPanel textSp = new StackPanel();
                    textSp.Orientation = Orientation.Horizontal;
                    textSp.VerticalAlignment = VerticalAlignment.Center;

                    TextBlock lbl = new TextBlock();
                    lbl.Text = label + ": ";
                    lbl.Foreground = UiTheme.TextMuted;
                    lbl.FontSize = 9.5;
                    lbl.FontWeight = FontWeights.SemiBold;

                    TextBlock val = new TextBlock();
                    val.Text = string.IsNullOrWhiteSpace(value) ? "-" : value;
                    val.Foreground = UiTheme.TextPrimary;
                    val.FontSize = 9.5;
                    val.FontWeight = FontWeights.Bold;

                    textSp.Children.Add(lbl);
                    textSp.Children.Add(val);
                    dp.Children.Add(textSp);

                    pill.Child = dp;

                    pill.MouseEnter += delegate
                    {
                        pill.BorderBrush = UiTheme.AccentLightBlue;
                        pill.Background = UiTheme.BgInput;
                    };
                    pill.MouseLeave += delegate
                    {
                        pill.BorderBrush = UiTheme.BorderSubtle;
                        pill.Background = UiTheme.TagBg;
                    };

                    pill.MouseLeftButtonUp += delegate
                    {
                        try
                        {
                            Clipboard.SetText(!string.IsNullOrWhiteSpace(value) ? value : "");
                        }
                        catch { }
                    };

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
        
                    card.HorizontalAlignment = HorizontalAlignment.Stretch;
                    card.Margin = _isCardsViewMode ? new Thickness(0, 0, 10, 10) : new Thickness(0, 0, 0, 10);
        
                    amb.CardBorder = card;
        
                    Grid cardGrid = new Grid();
                    RowDefinition rMain = new RowDefinition(); rMain.Height = GridLength.Auto;
                    RowDefinition rNote = new RowDefinition(); rNote.Height = GridLength.Auto;
                    cardGrid.RowDefinitions.Add(rMain);
                    cardGrid.RowDefinitions.Add(rNote);
        
                    ColumnDefinition colLeftTools = new ColumnDefinition(); colLeftTools.Width = GridLength.Auto;
                    ColumnDefinition colInfo = new ColumnDefinition(); colInfo.Width = new GridLength(1, GridUnitType.Star);
                    ColumnDefinition colRightActions = new ColumnDefinition(); colRightActions.Width = GridLength.Auto;
                    ColumnDefinition colPowerDots = new ColumnDefinition(); colPowerDots.Width = GridLength.Auto;
                    cardGrid.ColumnDefinitions.Add(colLeftTools);
                    cardGrid.ColumnDefinitions.Add(colInfo);
                    cardGrid.ColumnDefinitions.Add(colRightActions);
                    cardGrid.ColumnDefinitions.Add(colPowerDots);
        
                    
                    StackPanel leftToolsColumn = new StackPanel();
                    leftToolsColumn.Orientation = Orientation.Vertical;
                    leftToolsColumn.VerticalAlignment = VerticalAlignment.Center;
                    leftToolsColumn.HorizontalAlignment = HorizontalAlignment.Center;
                    leftToolsColumn.Width = 104;
                    leftToolsColumn.Margin = new Thickness(0, 0, 16, 0);
        
                    Border statusBadge = new Border();
                    statusBadge.Width = 104;
                    statusBadge.Height = 26;
                    statusBadge.CornerRadius = new CornerRadius(10);
                    statusBadge.Padding = new Thickness(6, 3, 6, 3);
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
                    userBadge.Width = 104;
                    userBadge.Height = 24;
                    userBadge.Background = UiTheme.BrushFromHex("#0C4A6E");
                    userBadge.BorderBrush = UiTheme.BrushFromHex(UiTheme.PrimaryBlueHex);
                    userBadge.BorderThickness = new Thickness(1);
                    userBadge.CornerRadius = new CornerRadius(5);
                    userBadge.Padding = new Thickness(4, 2, 4, 2);
                    userBadge.Margin = new Thickness(0, 4, 0, 0);
                    userBadge.HorizontalAlignment = HorizontalAlignment.Center;
                    userBadge.VerticalAlignment = VerticalAlignment.Center;
        
                    TextBlock userTxt = new TextBlock();
                    userTxt.Foreground = UiTheme.AccentLightBlue;
                    userTxt.FontSize = 10;
                    userTxt.FontWeight = FontWeights.Bold;
                    userTxt.MaxWidth = 96;
                    userTxt.TextTrimming = TextTrimming.CharacterEllipsis;
                    userTxt.TextAlignment = TextAlignment.Center;
                    userTxt.VerticalAlignment = VerticalAlignment.Center;
                    userBadge.Child = userTxt;
                    amb.UserTagBorder = userBadge;
                    amb.UserTagText = userTxt;
                    leftToolsColumn.Children.Add(userBadge);
        
                    Grid mainButtonsGrid = new Grid();
                    mainButtonsGrid.Width = 104;
                    mainButtonsGrid.Margin = new Thickness(0, 8, 0, 0);
                    mainButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    mainButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    mainButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    Button btnConfig = UiTheme.CreateActionButton(
                        UiTheme.CreatePencilIcon(UiTheme.TextSecondary, 13),
                        UiTheme.NeutralBtnHex,
                        UiTheme.NeutralBtnHoverHex);
                    btnConfig.Width = double.NaN;
                    btnConfig.Height = 26;
                    btnConfig.HorizontalAlignment = HorizontalAlignment.Stretch;
                    btnConfig.Padding = new Thickness(0);
                    btnConfig.Margin = new Thickness(0, 0, 2, 0);
                    btnConfig.ToolTip = "Configurar appserver.ini";
                    btnConfig.Click += delegate { ShowIniEditorView(amb); };
                    Grid.SetColumn(btnConfig, 0);
                    mainButtonsGrid.Children.Add(btnConfig);

                    bool isD1Active = ObterD1Ativos().Contains(amb.AppDir);
                    Button btnD1 = UiTheme.CreateRpoPureIconButton(
                        UiTheme.CreateHistoryIcon(isD1Active ? Brushes.White : UiTheme.BrushFromHex("#94A3B8"), 13),
                        isD1Active);
                    btnD1.Width = double.NaN;
                    btnD1.Height = 26;
                    btnD1.HorizontalAlignment = HorizontalAlignment.Stretch;
                    btnD1.Padding = new Thickness(0);
                    btnD1.Margin = new Thickness(2, 0, 2, 0);
                    btnD1.ToolTip = isD1Active ? "D-1 Ativo (Clique para desativar e restaurar RPO padrão)" : "D-1 Inativo (Clique para ativar D-1)";
                    btnD1.Click += delegate
                    {
                        AlternarEstadoD1(amb, btnD1);
                        bool nowActive = ObterD1Ativos().Contains(amb.AppDir);
                        btnD1.ToolTip = nowActive ? "D-1 Ativo (Clique para desativar e restaurar RPO padrão)" : "D-1 Inativo (Clique para ativar D-1)";
                    };
                    Grid.SetColumn(btnD1, 1);
                    mainButtonsGrid.Children.Add(btnD1);

                    bool isCustomActive = VerificarRpoCustomAtivo(amb);
                    Button btnRpoCustom = UiTheme.CreateRpoPureIconButton(
                        UiTheme.CreateCubeIcon(isCustomActive ? Brushes.White : UiTheme.BrushFromHex("#94A3B8"), 13),
                        isCustomActive);
                    btnRpoCustom.Width = double.NaN;
                    btnRpoCustom.Height = 26;
                    btnRpoCustom.HorizontalAlignment = HorizontalAlignment.Stretch;
                    btnRpoCustom.Padding = new Thickness(0);
                    btnRpoCustom.Margin = new Thickness(2, 0, 0, 0);
                    btnRpoCustom.ToolTip = string.Format("RPOCustom ({0}) - {1}", ObterTamanhoCustomRpoFormatado(amb), isCustomActive ? "Ativo (Clique para desativar)" : "Inativo (Clique para reativar)");
                    btnRpoCustom.Click += delegate
                    {
                        AlternarEstadoRpoCustom(amb, btnRpoCustom, null);
                        bool nowCustom = VerificarRpoCustomAtivo(amb);
                        btnRpoCustom.ToolTip = string.Format("RPOCustom ({0}) - {1}", ObterTamanhoCustomRpoFormatado(amb), nowCustom ? "Ativo (Clique para desativar)" : "Inativo (Clique para reativar)");
                    };
                    Grid.SetColumn(btnRpoCustom, 2);
                    mainButtonsGrid.Children.Add(btnRpoCustom);
        
                    leftToolsColumn.Children.Add(mainButtonsGrid);
        
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
                    lblEnvName.Margin = new Thickness(0, 0, 6, 0);
                    lblEnvName.VerticalAlignment = VerticalAlignment.Center;
                    lblEnvName.Cursor = Cursors.Hand;
                    lblEnvName.ToolTip = "Clique para abrir Monitoramento, Consumo e Logs";
                    lblEnvName.MouseLeftButtonDown += delegate { AbrirTelaCompletaLogs(amb.NomeIni, amb.AppDir, amb.Pid, "appserver"); };
                    titleWrap.Children.Add(lblEnvName);
        
                    Button btnCopyName = UiTheme.CreateFramelessIconButton(
                        UiTheme.CreateCopyIcon(UiTheme.TextMuted, 13),
                        "Copiar todos os dados deste ambiente");
                    btnCopyName.Padding = new Thickness(2);
                    btnCopyName.Margin = new Thickness(0, 0, 8, 0);
                    btnCopyName.VerticalAlignment = VerticalAlignment.Center;
                    btnCopyName.Click += delegate { CopiarDadosCardParaClipboard(amb); };
                    titleWrap.Children.Add(btnCopyName);
        
                    infoColumn.Children.Add(titleWrap);
        
                    
                    StackPanel pathStack = new StackPanel();
                    pathStack.Orientation = Orientation.Horizontal;
                    pathStack.Margin = new Thickness(0, 3, 0, 6);
        
                    TextBlock lblPath = new TextBlock();
                    lblPath.Text = amb.AppDir;
                    lblPath.Foreground = UiTheme.TextMuted;
                    lblPath.FontSize = 11;
                    lblPath.TextTrimming = TextTrimming.CharacterEllipsis;
                    lblPath.Cursor = Cursors.Hand;
                    lblPath.ToolTip = "Clique para abrir no Windows Explorer: " + amb.AppDir;
                    lblPath.VerticalAlignment = VerticalAlignment.Center;
                    lblPath.MouseEnter += delegate
                    {
                        lblPath.Foreground = UiTheme.AccentLightBlue;
                        lblPath.TextDecorations = TextDecorations.Underline;
                    };
                    lblPath.MouseLeave += delegate
                    {
                        lblPath.Foreground = UiTheme.TextMuted;
                        lblPath.TextDecorations = null;
                    };
                    lblPath.MouseLeftButtonUp += delegate
                    {
                        AbrirNoExplorer(amb.AppDir);
                    };
                    pathStack.Children.Add(lblPath);
        
                    Button btnCopyPath = UiTheme.CreateFramelessIconButton(
                        UiTheme.CreateCopyIcon(UiTheme.TextMuted, 11),
                        "Copiar caminho da pasta");
                    btnCopyPath.Padding = new Thickness(2);
                    btnCopyPath.Margin = new Thickness(6, 0, 0, 0);
                    btnCopyPath.VerticalAlignment = VerticalAlignment.Center;
                    btnCopyPath.Click += delegate
                    {
                        try
                        {
                            Clipboard.SetText(amb.AppDir);
                        }
                        catch { }
                    };
                    pathStack.Children.Add(btnCopyPath);
        
                    infoColumn.Children.Add(pathStack);
        
                    Grid metaContainer = new Grid();
                    metaContainer.Margin = new Thickness(0, 2, 0, 4);
                    metaContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    metaContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    WrapPanel metaWrap = new WrapPanel();
                    metaWrap.Orientation = Orientation.Horizontal;
                    metaWrap.HorizontalAlignment = HorizontalAlignment.Left;
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

                    Grid.SetColumn(metaWrap, 0);
                    metaContainer.Children.Add(metaWrap);
                    infoColumn.Children.Add(metaContainer);
        
                    StackPanel metricsStack = new StackPanel();
                    metricsStack.Orientation = Orientation.Horizontal;
                    metricsStack.Margin = new Thickness(0, 6, 0, 0);
        
                    Border cpuBadge = new Border();
                    cpuBadge.Background = UiTheme.BrushFromHex("#0F172A");
                    cpuBadge.BorderBrush = UiTheme.BorderSubtle;
                    cpuBadge.BorderThickness = new Thickness(1);
                    cpuBadge.CornerRadius = new CornerRadius(5);
                    cpuBadge.Padding = new Thickness(6, 2, 6, 2);
                    cpuBadge.Margin = new Thickness(0, 0, 6, 0);
        
                    TextBlock cpuTxt = new TextBlock();
                    cpuTxt.Text = amb.State == EnvRunState.Active ? string.Format("CPU: {0:0.0}%", amb.CpuPercent) : "CPU: -";
                    cpuTxt.FontSize = 10;
                    cpuTxt.FontWeight = FontWeights.SemiBold;
                    cpuTxt.Foreground = amb.State == EnvRunState.Active ? UiTheme.AccentLightBlue : UiTheme.TextMuted;
                    cpuBadge.Child = cpuTxt;
                    amb.CpuUsageText = cpuTxt;
                    metricsStack.Children.Add(cpuBadge);
        
                    Border ramBadge = new Border();
                    ramBadge.Background = UiTheme.BrushFromHex("#0F172A");
                    ramBadge.BorderBrush = UiTheme.BorderSubtle;
                    ramBadge.BorderThickness = new Thickness(1);
                    ramBadge.CornerRadius = new CornerRadius(5);
                    ramBadge.Padding = new Thickness(6, 2, 6, 2);
        
                    TextBlock ramTxt = new TextBlock();
                    ramTxt.Text = amb.State == EnvRunState.Active ? string.Format("RAM: {0} MB", amb.RamMb) : "RAM: -";
                    ramTxt.FontSize = 10;
                    ramTxt.FontWeight = FontWeights.SemiBold;
                    ramTxt.Foreground = amb.State == EnvRunState.Active ? UiTheme.AccentGreen : UiTheme.TextMuted;
                    ramBadge.Child = ramTxt;
                    amb.RamUsageText = ramTxt;
                    metricsStack.Children.Add(ramBadge);
        
                    amb.MetricsPanel = metricsStack;
                    infoColumn.Children.Add(metricsStack);
        
                    Grid.SetRow(infoColumn, 0);
                    Grid.SetColumn(infoColumn, 1);
                    cardGrid.Children.Add(infoColumn);
        
                    
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
        
                    
                    StackPanel accessButtonsStack = new StackPanel();
                    accessButtonsStack.Orientation = Orientation.Vertical;
                    accessButtonsStack.VerticalAlignment = VerticalAlignment.Center;
                    accessButtonsStack.Margin = new Thickness(0, 0, 10, 0);
        
                    
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
        
                    Grid.SetRow(accessButtonsStack, 0);
                    Grid.SetColumn(accessButtonsStack, 2);
                    cardGrid.Children.Add(accessButtonsStack);
        
                    
                    StackPanel powerDotsStack = new StackPanel();
                    powerDotsStack.Orientation = Orientation.Vertical;
                    powerDotsStack.VerticalAlignment = VerticalAlignment.Center;
                    powerDotsStack.HorizontalAlignment = HorizontalAlignment.Right;
        
                    Button btnDots = UiTheme.CreateFramelessIconButton(
                        UiTheme.CreateThreeDotsIcon(UiTheme.TextSecondary, 16),
                        "Mais opções");
                    btnDots.HorizontalAlignment = HorizontalAlignment.Right;
                    btnDots.Margin = new Thickness(0, 0, 0, 6);
        
                    ContextMenu menuDots = UiTheme.CreateDarkContextMenu();
        
                    menuDots.Items.Add(UiTheme.CreateDarkMenuItem("Comentário", UiTheme.CreateCommentBubbleIcon(UiTheme.TextSecondary, 14), delegate
                    {
                        EditarNotaCard(amb);
                    }));
        
                    menuDots.Items.Add(UiTheme.CreateDarkMenuItem("Baixar D-1 Oficial", UiTheme.CreateDownloadIcon(UiTheme.TextSecondary, 14), delegate
                    {
                        AbrirModalDownloadD1(amb);
                    }));
        
                    menuDots.Items.Add(UiTheme.CreateDarkMenuItem("Trocar D-1 com outra base", UiTheme.CreateExchangeIcon(UiTheme.TextSecondary, 14), delegate
                    {
                        AbrirModalTrocaD1(amb);
                    }));
        
                    menuDots.Items.Add(UiTheme.CreateDarkMenuItem("Ver Fontes do D-1 (CSV)", UiTheme.CreateSourcesListIcon(UiTheme.TextSecondary, 14), delegate
                    {
                        AbrirTelaFontesD1(amb);
                    }));
        
                    menuDots.Items.Add(UiTheme.CreateDarkMenuItem("Trocar / Atualizar Binário", UiTheme.CreateThinRefreshIcon(UiTheme.TextSecondary), delegate
                    {
                        MessageBox.Show(this, "Funcionalidade em desenvolvimento:\nTroca e atualização automática de binários em breve!", "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Information);
                    }));
        
                    menuDots.Items.Add(UiTheme.CreateDarkMenuItem(isHidden ? "Desocultar ambiente" : "Ocultar ambiente", isHidden ? UiTheme.CreateEyeIcon(UiTheme.AccentAmber, 14) : UiTheme.CreateEyeOffIcon(UiTheme.TextSecondary, 14), delegate
                    {
                        if (_hiddenDirs.Contains(amb.AppDir))
                            _hiddenDirs.Remove(amb.AppDir);
                        else
                            _hiddenDirs.Add(amb.AppDir);
        
                        SalvarOcultosAppData();
                        RecalcularAlertas();
                        RenderizarCards();
                    }));
        
                    menuDots.Items.Add(new Separator { Background = UiTheme.BorderSubtle, Margin = new Thickness(4, 2, 4, 2) });
        
                    menuDots.Items.Add(UiTheme.CreateDarkMenuItem("Logs e Monitoramento", UiTheme.CreateHistoryIcon(UiTheme.AccentLightBlue, 14), delegate
                    {
                        AbrirTelaCompletaLogs(amb.NomeIni, amb.AppDir, amb.Pid, "appserver");
                    }));
        
                    menuDots.Items.Add(UiTheme.CreateDarkMenuItem("Abrir no Explorer", UiTheme.CreateFolderIcon(UiTheme.TextSecondary, 14), delegate
                    {
                        AbrirNoExplorer(amb.AppDir);
                    }));
        
                    btnDots.ContextMenu = menuDots;
                    btnDots.Click += delegate
                    {
                        menuDots.PlacementTarget = btnDots;
                        menuDots.Placement = PlacementMode.Bottom;
                        menuDots.IsOpen = true;
                    };
        
                    powerDotsStack.Children.Add(btnDots);
                    powerDotsStack.Children.Add(btnStart);
                    powerDotsStack.Children.Add(btnRestart);
                    powerDotsStack.Children.Add(btnStop);
        
                    Grid.SetRow(powerDotsStack, 0);
                    Grid.SetColumn(powerDotsStack, 3);
                    cardGrid.Children.Add(powerDotsStack);
        
                    
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
                        Grid.SetColumnSpan(noteBanner, 5);
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
                    footerRow.Children.Add(btnsRow);
                    stack.Children.Add(footerRow);
        
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
                            else if (amb.Pid > 0)
                                userInfo = "PID " + amb.Pid;
        
                            amb.UserTagText.Text = userInfo;
        
                            string fullInfo = "";
                            if (!string.IsNullOrEmpty(amb.ProcessUser))
                                fullInfo += "Usuário: " + amb.ProcessUser + "\n";
                            if (amb.Pid > 0)
                                fullInfo += "PID: " + amb.Pid + "\n";
                            fullInfo += "Modo: " + (amb.RunningAsService ? "Serviço Windows" : "Processo Console");
                            amb.UserTagBorder.ToolTip = fullInfo.Trim();
        
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
        
                    if (amb.CpuUsageText != null)
                    {
                        amb.CpuUsageText.Text = amb.State == EnvRunState.Active ? string.Format("CPU: {0:0.0}%", amb.CpuPercent) : "CPU: -";
                        amb.CpuUsageText.Foreground = amb.State == EnvRunState.Active ? UiTheme.AccentLightBlue : UiTheme.TextMuted;
                    }
                    if (amb.RamUsageText != null)
                    {
                        amb.RamUsageText.Text = amb.State == EnvRunState.Active ? string.Format("RAM: {0} MB", amb.RamMb) : "RAM: -";
                        amb.RamUsageText.Foreground = amb.State == EnvRunState.Active ? UiTheme.AccentGreen : UiTheme.TextMuted;
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
                        string ativoTxt = (ativosCount == 1) ? "ativo" : "ativos";
                        string ocultoTxt = (_hiddenDirs.Count == 1) ? "oculto" : "ocultos";
                        _statsLabel.Text = _hiddenDirs.Count > 0
                            ? string.Format("{0} de {1} {2} ({3} {4})", ativosCount, totalVisiveis, ativoTxt, _hiddenDirs.Count, ocultoTxt)
                            : string.Format("{0} de {1} {2}", ativosCount, totalVisiveis, ativoTxt);
                    }
                }

        private void AbrirModalDownloadD1(AmbienteInfo amb)
                {
                    Window dlg = new Window();
                    dlg.Title = "Baixar Repositório D-1 - " + amb.NomeIni;
                    dlg.Width = 540;
                    dlg.Height = 480;
                    dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    dlg.Owner = this;
                    dlg.Background = UiTheme.BgWindow;
                    dlg.WindowStyle = WindowStyle.ToolWindow;
        
                    Border shell = new Border();
                    shell.Padding = new Thickness(20);
                    shell.Background = UiTheme.BgWindow;
        
                    Grid g = new Grid();
                    g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
                    
                    StackPanel tStack = new StackPanel();
                    tStack.Margin = new Thickness(0, 0, 0, 16);
        
                    TextBlock title = new TextBlock();
                    title.Text = "Baixar D-1 Oficial (tttm120.rpo + CSV)";
                    title.FontSize = 16;
                    title.FontWeight = FontWeights.Bold;
                    title.Foreground = UiTheme.TextPrimary;
                    tStack.Children.Add(title);
        
                    string detectedVersion = DetectarVersaoDiagnosticsJson(amb);
        
                    TextBlock sub = new TextBlock();
                    if (!string.IsNullOrEmpty(detectedVersion))
                    {
                        sub.Text = string.Format("✓ Versão {0} detectada automaticamente via diagnostics.json", detectedVersion);
                        sub.Foreground = UiTheme.AccentGreen;
                    }
                    else
                    {
                        sub.Text = "Selecione a versão Protheus para preencher no armazenamento D-1:";
                        sub.Foreground = UiTheme.TextSecondary;
                    }
                    sub.FontSize = 12;
                    sub.Margin = new Thickness(0, 4, 0, 0);
                    tStack.Children.Add(sub);
        
                    Grid.SetRow(tStack, 0);
                    g.Children.Add(tStack);
        
                    
                    Grid gridVersions = new Grid();
                    gridVersions.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    gridVersions.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    gridVersions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    gridVersions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        
                    string[,] versoes = new string[,] {
                        { "12.1.2310", "https://arte.engpro.totvs.com.br/protheus/padrao/builds/12.1.2310/latest/repositorio/panthera_onca/" },
                        { "12.1.2410", "https://arte.engpro.totvs.com.br/protheus/padrao/builds/12.1.2410/latest/repositorio/panthera_onca/" },
                        { "12.1.2510", "https://arte.engpro.totvs.com.br/protheus/padrao/builds/12.1.2510/latest/repositorio/panthera_onca/" },
                        { "12.1.2610", "https://arte.engpro.totvs.com.br/protheus/padrao/builds/12.1.2610/latest/repositorio/panthera_onca/" }
                    };
        
                    ProgressBar pb = new ProgressBar();
                    pb.Height = 8;
                    pb.Margin = new Thickness(0, 12, 0, 6);
                    pb.Visibility = Visibility.Collapsed;
                    pb.Foreground = UiTheme.AccentLightBlue;
        
                    TextBlock txtStatus = new TextBlock();
                    txtStatus.Text = !string.IsNullOrEmpty(detectedVersion) ? "Iniciando download automático para a versão detectada..." : "Selecione uma versão acima para iniciar o download.";
                    txtStatus.FontSize = 11;
                    txtStatus.Foreground = UiTheme.TextSecondary;
                    txtStatus.HorizontalAlignment = HorizontalAlignment.Center;
                    txtStatus.Margin = new Thickness(0, 0, 0, 10);
        
                    List<Border> versionCards = new List<Border>();
        
                    Action<string, string> iniciarDownload = delegate(string versaoNome, string urlBase)
                    {
                        foreach (Border b in versionCards) b.IsEnabled = false;
                        pb.Visibility = Visibility.Visible;
                        pb.IsIndeterminate = true;
                        txtStatus.Text = string.Format("Conectando à TOTVS para versão {0}...", versaoNome);
                        txtStatus.Foreground = UiTheme.AccentLightBlue;
        
                        ThreadPool.QueueUserWorkItem(delegate
                        {
                            try
                            {
                                string targetDir = ObterPastaD1AppData(amb);
                                if (!Directory.Exists(targetDir))
                                    Directory.CreateDirectory(targetDir);
        
                                
                                string csvFileName = null;
                                using (System.Net.WebClient wcHtml = new System.Net.WebClient())
                                {
                                    wcHtml.Headers.Add("User-Agent", "Mozilla/5.0");
                                    string html = wcHtml.DownloadString(urlBase);
                                    var matches = System.Text.RegularExpressions.Regex.Matches(html, @"href=""([^""]+)""");
                                    foreach (System.Text.RegularExpressions.Match m in matches)
                                    {
                                        string href = m.Groups[1].Value;
                                        if (href.IndexOf("all", StringComparison.OrdinalIgnoreCase) >= 0 && href.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                                        {
                                            csvFileName = href.TrimStart('.', '/');
                                            break;
                                        }
                                    }
                                }
        
                                
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    txtStatus.Text = "Baixando tttm120.rpo...";
                                }));
        
                                string rpoUrl = urlBase.TrimEnd('/') + "/tttm120.rpo";
                                string rpoDest = Path.Combine(targetDir, "tttm120.rpo");
                                using (System.Net.WebClient wcRpo = new System.Net.WebClient())
                                {
                                    wcRpo.Headers.Add("User-Agent", "Mozilla/5.0");
                                    wcRpo.DownloadFile(rpoUrl, rpoDest);
                                }
        
                                
                                if (!string.IsNullOrEmpty(csvFileName))
                                {
                                    Dispatcher.BeginInvoke(new Action(delegate
                                    {
                                        txtStatus.Text = "Baixando CSV (" + csvFileName + ")...";
                                    }));
                                    string csvUrl = urlBase.TrimEnd('/') + "/" + csvFileName;
                                    string csvDest = Path.Combine(targetDir, csvFileName);
                                    using (System.Net.WebClient wcCsv = new System.Net.WebClient())
                                    {
                                        wcCsv.Headers.Add("User-Agent", "Mozilla/5.0");
                                        wcCsv.DownloadFile(csvUrl, csvDest);
                                    }
                                }
        
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    pb.IsIndeterminate = false;
                                    pb.Value = 100;
                                    txtStatus.Text = string.Format("✓ Sucesso! D-1 {0} salvo no armazenamento TOTVSHub.", versaoNome);
                                    txtStatus.Foreground = UiTheme.AccentGreen;
                                    foreach (Border b in versionCards) b.IsEnabled = true;
                                }));
                            }
                            catch (Exception ex)
                            {
                                Dispatcher.BeginInvoke(new Action(delegate
                                {
                                    pb.Visibility = Visibility.Collapsed;
                                    txtStatus.Text = "Falha no download: " + ex.Message;
                                    txtStatus.Foreground = UiTheme.AccentRed;
                                    foreach (Border b in versionCards) b.IsEnabled = true;
                                }));
                            }
                        });
                    };
        
                    string autoDownloadUrl = null;
                    string autoDownloadVersion = null;
        
                    for (int i = 0; i < 4; i++)
                    {
                        int row = i / 2;
                        int col = i % 2;
                        string vNome = versoes[i, 0];
                        string vUrl = versoes[i, 1];
        
                        bool isDetected = !string.IsNullOrEmpty(detectedVersion) && (vNome.Equals(detectedVersion, StringComparison.OrdinalIgnoreCase) || detectedVersion.Contains(vNome.Replace("12.1.", "")));
                        if (isDetected && autoDownloadUrl == null)
                        {
                            autoDownloadUrl = vUrl;
                            autoDownloadVersion = vNome;
                        }
        
                        Border c = new Border();
                        c.Background = isDetected ? UiTheme.BrushFromHex("#0C4A6E") : UiTheme.BgCard;
                        c.BorderBrush = isDetected ? UiTheme.AccentLightBlue : UiTheme.BorderSubtle;
                        c.BorderThickness = new Thickness(isDetected ? 2 : 1);
                        c.CornerRadius = new CornerRadius(8);
                        c.Padding = new Thickness(14);
                        c.Margin = new Thickness(col == 0 ? 0 : 6, row == 0 ? 0 : 6, col == 0 ? 6 : 0, row == 0 ? 6 : 0);
                        c.Cursor = Cursors.Hand;
        
                        StackPanel inner = new StackPanel();
                        inner.VerticalAlignment = VerticalAlignment.Center;
                        inner.HorizontalAlignment = HorizontalAlignment.Center;
        
                        TextBlock icon = new TextBlock();
                        icon.Text = isDetected ? "⭐" : "📦";
                        icon.FontSize = 20;
                        icon.HorizontalAlignment = HorizontalAlignment.Center;
                        icon.Margin = new Thickness(0, 0, 0, 6);
                        inner.Children.Add(icon);
        
                        TextBlock lblV = new TextBlock();
                        lblV.Text = vNome;
                        lblV.FontSize = 14;
                        lblV.FontWeight = FontWeights.Bold;
                        lblV.Foreground = UiTheme.AccentLightBlue;
                        lblV.HorizontalAlignment = HorizontalAlignment.Center;
                        inner.Children.Add(lblV);
        
                        TextBlock lblSubV = new TextBlock();
                        lblSubV.Text = isDetected ? "Detectada (Recomendada)" : "Panthera Onca (Latest)";
                        lblSubV.FontSize = 10;
                        lblSubV.Foreground = isDetected ? UiTheme.AccentGreen : UiTheme.TextMuted;
                        lblSubV.HorizontalAlignment = HorizontalAlignment.Center;
                        lblSubV.Margin = new Thickness(0, 2, 0, 0);
                        inner.Children.Add(lblSubV);
        
                        c.Child = inner;
        
                        c.MouseEnter += delegate { if (!isDetected) { c.Background = UiTheme.BrushFromHex("#1E293B"); c.BorderBrush = UiTheme.AccentLightBlue; } };
                        c.MouseLeave += delegate { if (!isDetected) { c.Background = UiTheme.BgCard; c.BorderBrush = UiTheme.BorderSubtle; } };
                        c.MouseLeftButtonUp += delegate { iniciarDownload(vNome, vUrl); };
        
                        Grid.SetRow(c, row);
                        Grid.SetColumn(c, col);
                        gridVersions.Children.Add(c);
                        versionCards.Add(c);
                    }
        
                    Grid.SetRow(gridVersions, 1);
                    g.Children.Add(gridVersions);
        
                    StackPanel progStack = new StackPanel();
                    progStack.Children.Add(pb);
                    progStack.Children.Add(txtStatus);
                    Grid.SetRow(progStack, 2);
                    g.Children.Add(progStack);
        
                    DockPanel bRow = new DockPanel();
                    bRow.Margin = new Thickness(0, 8, 0, 0);
        
                    Button btnOpenFolder = UiTheme.CreateActionButton("📂 Abrir Pasta D-1", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
                    btnOpenFolder.Height = 32;
                    btnOpenFolder.Padding = new Thickness(12, 0, 12, 0);
                    btnOpenFolder.Click += delegate
                    {
                        string targetDir = ObterPastaD1AppData(amb);
                        AbrirNoExplorer(targetDir);
                    };
                    DockPanel.SetDock(btnOpenFolder, Dock.Left);
                    bRow.Children.Add(btnOpenFolder);
        
                    Button btnClose = UiTheme.CreateActionButton("Fechar", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
                    btnClose.Height = 32;
                    btnClose.Padding = new Thickness(18, 0, 18, 0);
                    btnClose.HorizontalAlignment = HorizontalAlignment.Right;
                    DockPanel.SetDock(btnClose, Dock.Right);
                    btnClose.Click += delegate { dlg.Close(); };
                    bRow.Children.Add(btnClose);
        
                    Grid.SetRow(bRow, 3);
                    g.Children.Add(bRow);
        
                    shell.Child = g;
                    dlg.Content = shell;
        
                    
                    if (!string.IsNullOrEmpty(autoDownloadUrl) && !string.IsNullOrEmpty(autoDownloadVersion))
                    {
                        dlg.Loaded += delegate
                        {
                            iniciarDownload(autoDownloadVersion, autoDownloadUrl);
                        };
                    }
        
                    dlg.ShowDialog();
                }

        private void AbrirModalTrocaD1(AmbienteInfo amb)
                {
                    Window dlg = new Window();
                    dlg.Title = "Troca de D-1 - " + amb.NomeIni;
                    dlg.Width = 560;
                    dlg.Height = 440;
                    dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    dlg.Owner = this;
                    dlg.Background = UiTheme.BgWindow;
                    dlg.WindowStyle = WindowStyle.ToolWindow;
        
                    Border shell = new Border();
                    shell.Padding = new Thickness(20);
                    shell.Background = UiTheme.BgWindow;
        
                    Grid g = new Grid();
                    g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
                    
                    StackPanel tStack = new StackPanel();
                    tStack.Margin = new Thickness(0, 0, 0, 14);
        
                    TextBlock title = new TextBlock();
                    title.Text = "Substituir D-1 a partir de outra base";
                    title.FontSize = 16;
                    title.FontWeight = FontWeights.Bold;
                    title.Foreground = UiTheme.TextPrimary;
                    tStack.Children.Add(title);
        
                    TextBlock sub = new TextBlock();
                    sub.Text = string.Format("Selecione qual base mapeada fornecerá o repositório D-1 para {0}:", amb.NomeIni);
                    sub.FontSize = 12;
                    sub.Foreground = UiTheme.TextSecondary;
                    sub.Margin = new Thickness(0, 4, 0, 0);
                    tStack.Children.Add(sub);
        
                    Grid.SetRow(tStack, 0);
                    g.Children.Add(tStack);
        
                    
                    ListBox lbBases = new ListBox();
                    lbBases.Background = UiTheme.BgCard;
                    lbBases.BorderBrush = UiTheme.BorderSubtle;
                    lbBases.BorderThickness = new Thickness(1);
                    lbBases.Foreground = UiTheme.TextPrimary;
                    lbBases.Margin = new Thickness(0, 0, 0, 8);
        
                    List<AmbienteInfo> outrasBases = (_ambientes ?? new List<AmbienteInfo>())
                        .Where(a => a != null && !string.Equals(a.AppDir, amb.AppDir, StringComparison.OrdinalIgnoreCase))
                        .ToList();
        
                    Border warningBox = new Border();
                    warningBox.Background = UiTheme.BrushFromHex("#451A03");
                    warningBox.BorderBrush = UiTheme.BrushFromHex("#F59E0B");
                    warningBox.BorderThickness = new Thickness(1);
                    warningBox.CornerRadius = new CornerRadius(6);
                    warningBox.Padding = new Thickness(12, 8, 12, 8);
                    warningBox.Margin = new Thickness(0, 6, 0, 6);
                    warningBox.Visibility = Visibility.Collapsed;
        
                    TextBlock txtWarning = new TextBlock();
                    txtWarning.Foreground = UiTheme.BrushFromHex("#FDE68A");
                    txtWarning.FontSize = 12;
                    txtWarning.TextWrapping = TextWrapping.Wrap;
                    txtWarning.Text = "⚠️ A base selecionada não possui arquivos D-1 disponíveis em seu repositório. Não é possível realizar a troca.";
                    warningBox.Child = txtWarning;
        
                    Grid.SetRow(warningBox, 2);
                    g.Children.Add(warningBox);
        
                    DockPanel bRow = new DockPanel();
                    bRow.Margin = new Thickness(0, 8, 0, 0);
        
                    Button btnCancel = UiTheme.CreateActionButton("Cancelar", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
                    btnCancel.Height = 32;
                    btnCancel.Padding = new Thickness(16, 0, 16, 0);
                    DockPanel.SetDock(btnCancel, Dock.Left);
                    btnCancel.Click += delegate { dlg.Close(); };
                    bRow.Children.Add(btnCancel);
        
                    Button btnConfirm = UiTheme.CreateActionButton("Confirmar Substituição", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
                    btnConfirm.Height = 32;
                    btnConfirm.Padding = new Thickness(18, 0, 18, 0);
                    btnConfirm.HorizontalAlignment = HorizontalAlignment.Right;
                    btnConfirm.IsEnabled = false;
                    DockPanel.SetDock(btnConfirm, Dock.Right);
                    bRow.Children.Add(btnConfirm);
        
                    Grid.SetRow(bRow, 3);
                    g.Children.Add(bRow);
        
                    if (outrasBases.Count == 0)
                    {
                        TextBlock emptyMsg = new TextBlock();
                        emptyMsg.Text = "Nenhuma outra base cadastrada no TOTVSHub.";
                        emptyMsg.Foreground = UiTheme.TextMuted;
                        emptyMsg.Margin = new Thickness(12);
                        lbBases.Items.Add(emptyMsg);
                    }
                    else
                    {
                        foreach (AmbienteInfo outro in outrasBases)
                        {
                            string outroD1Dir = ObterPastaD1AppData(outro);
                            string[] rpos = Directory.Exists(outroD1Dir) ? Directory.GetFiles(outroD1Dir, "*.rpo") : new string[0];
                            bool temD1 = rpos.Length > 0;
        
                            Border itemCard = new Border();
                            itemCard.Tag = outro;
                            itemCard.Padding = new Thickness(10, 8, 10, 8);
                            itemCard.Margin = new Thickness(0, 2, 0, 2);
        
                            Grid itemGrid = new Grid();
                            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
                            StackPanel itemInfo = new StackPanel();
                            TextBlock itemNome = new TextBlock();
                            itemNome.Text = outro.NomeIni;
                            itemNome.FontWeight = FontWeights.SemiBold;
                            itemNome.Foreground = UiTheme.TextPrimary;
                            itemInfo.Children.Add(itemNome);
        
                            TextBlock itemDir = new TextBlock();
                            itemDir.Text = outro.AppDir;
                            itemDir.FontSize = 10;
                            itemDir.Foreground = UiTheme.TextMuted;
                            itemInfo.Children.Add(itemDir);
                            Grid.SetColumn(itemInfo, 0);
                            itemGrid.Children.Add(itemInfo);
        
                            Border badge = new Border();
                            badge.CornerRadius = new CornerRadius(4);
                            badge.Padding = new Thickness(6, 2, 6, 2);
                            badge.VerticalAlignment = VerticalAlignment.Center;
                            badge.Background = temD1 ? UiTheme.BrushFromHex("#064E3B") : UiTheme.BrushFromHex("#450A0A");
        
                            TextBlock txtBadge = new TextBlock();
                            txtBadge.Text = temD1 ? string.Format("✓ D-1 Disponível ({0})", rpos.Length) : "✗ Sem D-1";
                            txtBadge.FontSize = 11;
                            txtBadge.Foreground = temD1 ? UiTheme.AccentGreen : UiTheme.AccentRed;
                            badge.Child = txtBadge;
                            Grid.SetColumn(badge, 1);
                            itemGrid.Children.Add(badge);
        
                            itemCard.Child = itemGrid;
        
                            ListBoxItem lbi = new ListBoxItem();
                            lbi.Content = itemCard;
                            lbi.Tag = temD1;
                            lbBases.Items.Add(lbi);
                        }
                    }
        
                    lbBases.SelectionChanged += delegate
                    {
                        ListBoxItem selected = lbBases.SelectedItem as ListBoxItem;
                        if (selected != null)
                        {
                            bool temD1 = selected.Tag is bool && (bool)selected.Tag;
                            btnConfirm.IsEnabled = temD1;
                            warningBox.Visibility = temD1 ? Visibility.Collapsed : Visibility.Visible;
                        }
                        else
                        {
                            btnConfirm.IsEnabled = false;
                            warningBox.Visibility = Visibility.Collapsed;
                        }
                    };
        
                    btnConfirm.Click += delegate
                    {
                        ListBoxItem selected = lbBases.SelectedItem as ListBoxItem;
                        if (selected == null) return;
        
                        Border card = selected.Content as Border;
                        AmbienteInfo fonteAmb = card != null ? card.Tag as AmbienteInfo : null;
                        if (fonteAmb == null) return;
        
                        string srcD1Dir = ObterPastaD1AppData(fonteAmb);
                        string dstD1Dir = ObterPastaD1AppData(amb);
        
                        if (!Directory.Exists(srcD1Dir) || Directory.GetFiles(srcD1Dir, "*.rpo").Length == 0)
                        {
                            MessageBox.Show(dlg, "A base de origem não possui arquivos D-1 para copiar.", "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
        
                        try
                        {
                            if (!Directory.Exists(dstD1Dir)) Directory.CreateDirectory(dstD1Dir);
        
                            string[] files = Directory.GetFiles(srcD1Dir);
                            foreach (string f in files)
                            {
                                string destFile = Path.Combine(dstD1Dir, Path.GetFileName(f));
                                File.Copy(f, destFile, true);
                            }
        
                            
                            if (ObterD1Ativos().Contains(amb.AppDir))
                            {
                                string apoFolder = LocalizarPastaApo(amb);
                                if (!string.IsNullOrEmpty(apoFolder) && Directory.Exists(apoFolder))
                                {
                                    string[] dstRpos = Directory.GetFiles(dstD1Dir, "*.rpo");
                                    if (dstRpos.Length > 0)
                                    {
                                        string targetRpo = Path.Combine(apoFolder, Path.GetFileName(dstRpos[0]));
                                        File.Copy(dstRpos[0], targetRpo, true);
                                    }
                                }
                            }
        
                            MessageBox.Show(dlg, string.Format("Repositório D-1 copiado com sucesso de '{0}' para '{1}'!", fonteAmb.NomeIni, amb.NomeIni), "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Information);
                            dlg.Close();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(dlg, "Erro ao copiar D-1: " + ex.Message, "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };
        
                    Grid.SetRow(lbBases, 1);
                    g.Children.Add(lbBases);
        
                    shell.Child = g;
                    dlg.Content = shell;
                    dlg.ShowDialog();
                }

        private void AbrirTelaFontesD1(AmbienteInfo amb)
                {
                    string d1Dir = ObterPastaD1AppData(amb);
                    string[] csvFiles = Directory.Exists(d1Dir) ? Directory.GetFiles(d1Dir, "*.csv") : new string[0];
        
                    
                    if (csvFiles.Length == 0)
                    {
                        string legacyDir = Path.Combine(amb.AppDir ?? "", "totvshub_d1");
                        if (Directory.Exists(legacyDir))
                            csvFiles = Directory.GetFiles(legacyDir, "*.csv");
                    }
        
                    if (csvFiles.Length == 0)
                    {
                        var res = MessageBox.Show(this, string.Format("Nenhum arquivo CSV de fontes foi encontrado no repositório D-1 de '{0}'.\n\nDeseja realizar o download do D-1 agora?", amb.NomeIni), "TOTVSHub - Fontes D-1", MessageBoxButton.YesNo, MessageBoxImage.Information);
                        if (res == MessageBoxResult.Yes)
                        {
                            AbrirModalDownloadD1(amb);
                        }
                        return;
                    }
        
                    string csvPath = csvFiles[0];
                    string csvName = Path.GetFileName(csvPath);
                    DateTime csvDate = File.GetLastWriteTime(csvPath);
        
                    
                    List<string[]> rows = new List<string[]>();
                    try
                    {
                        string[] lines = File.ReadAllLines(csvPath);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            string line = lines[i].Trim();
                            if (string.IsNullOrEmpty(line)) continue;
        
                            string[] parts;
                            if (line.Contains(";")) parts = line.Split(';');
                            else if (line.Contains(",")) parts = line.Split(',');
                            else if (line.Contains("\t")) parts = line.Split('\t');
                            else parts = new string[] { line, "", "" };
        
                            string col0 = parts.Length > 0 ? parts[0].Trim() : "";
                            string col1 = parts.Length > 1 ? parts[1].Trim() : "";
                            string col2 = parts.Length > 2 ? parts[2].Trim() : "";
        
                            rows.Add(new string[] { col0, col1, col2 });
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "Erro ao ler arquivo CSV: " + ex.Message, "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
        
                    Window dlg = new Window();
                    dlg.Title = string.Format("{0} - D-1 ({1:dd/MM/yyyy})", amb.NomeIni, csvDate);
                    dlg.Width = 760;
                    dlg.Height = 560;
                    dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    dlg.Owner = this;
                    dlg.Background = UiTheme.BgWindow;
        
                    Border shell = new Border();
                    shell.Padding = new Thickness(20);
                    shell.Background = UiTheme.BgWindow;
        
                    Grid g = new Grid();
                    g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); 
                    g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); 
                    g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); 
                    g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); 
        
                    
                    DockPanel headDock = new DockPanel();
                    headDock.Margin = new Thickness(0, 0, 0, 14);
        
                    StackPanel titleGroup = new StackPanel();
                    TextBlock title = new TextBlock();
                    title.Text = string.Format("Fontes do Repositório D-1 - {0}", amb.NomeIni);
                    title.FontSize = 16;
                    title.FontWeight = FontWeights.Bold;
                    title.Foreground = UiTheme.TextPrimary;
                    titleGroup.Children.Add(title);
        
                    TextBlock sub = new TextBlock();
                    sub.Text = string.Format("Arquivo: {0} • Modificado em: {1:dd/MM/yyyy HH:mm}", csvName, csvDate);
                    sub.FontSize = 11;
                    sub.Foreground = UiTheme.TextSecondary;
                    sub.Margin = new Thickness(0, 2, 0, 0);
                    titleGroup.Children.Add(sub);
        
                    headDock.Children.Add(titleGroup);
                    Grid.SetRow(headDock, 0);
                    g.Children.Add(headDock);
        
                    
                    Grid searchGrid = new Grid();
                    searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    searchGrid.Margin = new Thickness(0, 0, 0, 10);
        
                    Border searchBorder = new Border();
                    searchBorder.Background = UiTheme.BgCard;
                    searchBorder.BorderBrush = UiTheme.BorderSubtle;
                    searchBorder.BorderThickness = new Thickness(1);
                    searchBorder.CornerRadius = new CornerRadius(6);
                    searchBorder.Padding = new Thickness(8, 4, 8, 4);
        
                    Grid sInner = new Grid();
                    sInner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    sInner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        
                    UIElement sIcon = UiTheme.CreateSearchIcon();
                    Grid.SetColumn(sIcon, 0);
                    sInner.Children.Add(sIcon);
        
                    TextBox txtSearch = new TextBox();
                    txtSearch.Background = Brushes.Transparent;
                    txtSearch.BorderThickness = new Thickness(0);
                    txtSearch.Foreground = UiTheme.TextPrimary;
                    txtSearch.FontSize = 12;
                    txtSearch.Margin = new Thickness(8, 0, 0, 0);
                    txtSearch.VerticalAlignment = VerticalAlignment.Center;
                    Grid.SetColumn(txtSearch, 1);
                    sInner.Children.Add(txtSearch);
        
                    searchBorder.Child = sInner;
                    Grid.SetColumn(searchBorder, 0);
                    searchGrid.Children.Add(searchBorder);
        
                    TextBlock txtTotal = new TextBlock();
                    txtTotal.Text = string.Format("Total: {0:N0} fontes", rows.Count);
                    txtTotal.Foreground = UiTheme.AccentLightBlue;
                    txtTotal.FontSize = 12;
                    txtTotal.FontWeight = FontWeights.SemiBold;
                    txtTotal.VerticalAlignment = VerticalAlignment.Center;
                    txtTotal.Margin = new Thickness(12, 0, 0, 0);
                    Grid.SetColumn(txtTotal, 1);
                    searchGrid.Children.Add(txtTotal);
        
                    Grid.SetRow(searchGrid, 1);
                    g.Children.Add(searchGrid);
        
                    
                    ListView lv = new ListView();
                    lv.Background = UiTheme.BgCard;
                    lv.BorderBrush = UiTheme.BorderSubtle;
                    lv.BorderThickness = new Thickness(1);
                    lv.Foreground = UiTheme.TextPrimary;
        
                    GridView gv = new GridView();
                    gv.Columns.Add(new GridViewColumn { Header = "Nome do Fonte / Programa", Width = 280, DisplayMemberBinding = new System.Windows.Data.Binding("[0]") });
                    gv.Columns.Add(new GridViewColumn { Header = "Data / Modificação", Width = 180, DisplayMemberBinding = new System.Windows.Data.Binding("[1]") });
                    gv.Columns.Add(new GridViewColumn { Header = "Informações Adicionais", Width = 220, DisplayMemberBinding = new System.Windows.Data.Binding("[2]") });
                    lv.View = gv;
        
                    Action atualizarLista = delegate
                    {
                        string query = (txtSearch.Text ?? "").Trim().ToLowerInvariant();
                        lv.Items.Clear();
        
                        int count = 0;
                        for (int i = 0; i < rows.Count; i++)
                        {
                            string[] r = rows[i];
                            if (string.IsNullOrEmpty(query) ||
                                r[0].ToLowerInvariant().Contains(query) ||
                                r[1].ToLowerInvariant().Contains(query) ||
                                r[2].ToLowerInvariant().Contains(query))
                            {
                                lv.Items.Add(r);
                                count++;
                            }
                        }
                        txtTotal.Text = string.Format("Exibindo {0:N0} de {1:N0} fontes", count, rows.Count);
                    };
        
                    txtSearch.TextChanged += delegate { atualizarLista(); };
                    atualizarLista();
        
                    Grid.SetRow(lv, 2);
                    g.Children.Add(lv);
        
                    
                    DockPanel bRow = new DockPanel();
                    bRow.Margin = new Thickness(0, 10, 0, 0);
        
                    Button btnCopyList = UiTheme.CreateActionButton("Copiar Lista Filtrada", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
                    btnCopyList.Height = 32;
                    btnCopyList.Padding = new Thickness(14, 0, 14, 0);
                    btnCopyList.Click += delegate
                    {
                        try
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (string[] r in lv.Items)
                            {
                                sb.AppendLine(string.Join("\t", r));
                            }
                            Clipboard.SetDataObject(sb.ToString().TrimEnd(), true);
                            MessageBox.Show(dlg, "Lista de fontes copiada para a área de transferência!", "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch { }
                    };
                    DockPanel.SetDock(btnCopyList, Dock.Left);
                    bRow.Children.Add(btnCopyList);
        
                    Button btnOpenFolder = UiTheme.CreateActionButton("📂 Abrir Pasta D-1", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
                    btnOpenFolder.Height = 32;
                    btnOpenFolder.Padding = new Thickness(14, 0, 14, 0);
                    btnOpenFolder.Margin = new Thickness(8, 0, 0, 0);
                    btnOpenFolder.Click += delegate
                    {
                        AbrirNoExplorer(Path.GetDirectoryName(csvPath));
                    };
                    DockPanel.SetDock(btnOpenFolder, Dock.Left);
                    bRow.Children.Add(btnOpenFolder);
        
                    Button btnClose = UiTheme.CreateActionButton("Fechar", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
                    btnClose.Height = 32;
                    btnClose.Padding = new Thickness(18, 0, 18, 0);
                    btnClose.HorizontalAlignment = HorizontalAlignment.Right;
                    DockPanel.SetDock(btnClose, Dock.Right);
                    btnClose.Click += delegate { dlg.Close(); };
                    bRow.Children.Add(btnClose);
        
                    Grid.SetRow(bRow, 3);
                    g.Children.Add(bRow);
        
                    shell.Child = g;
                    dlg.Content = shell;
                    dlg.ShowDialog();
                }
    }
}
