using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        private DbAccessConfig _dbAccessConfig = new DbAccessConfig();
        private string _dbAccessPathPrefPath;
        private string _dbAccessHiddenPrefPath;
        private ScrollViewer _dbAccessScrollViewer;
        private Panel _dbAccessCardsContainer;
        private DateTime _dbAccessLastUpdatedTime = DateTime.Now;
#pragma warning disable 0649
        private Border _dbAccessServerStatusBadge;
        private TextBlock _dbAccessServerStatusText;
#pragma warning restore 0649
        private HashSet<string> _hiddenDbAccessEnvs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _dbAccessNote = "";
        private string _dbAccessNotePrefPath;
        private string _dbAccessFilterText = "";
        private bool _dbAccessShowingHidden = false;
        private TextBlock _dbAccessStatusCountLabel;
        private TextBlock _dbAccessLastUpdateLabel;

        private void CarregarNotaDbAccessAppData()
        {
            if (string.IsNullOrEmpty(_dbAccessNotePrefPath))
                _dbAccessNotePrefPath = System.IO.Path.Combine(_appDataDir, "dbaccess_nota.txt");

            try
            {
                if (File.Exists(_dbAccessNotePrefPath))
                    _dbAccessNote = File.ReadAllText(_dbAccessNotePrefPath, Encoding.UTF8).Trim();
                else
                    _dbAccessNote = "";
            }
            catch { _dbAccessNote = ""; }
        }

        private void SalvarNotaDbAccessAppData()
        {
            if (string.IsNullOrEmpty(_dbAccessNotePrefPath))
                _dbAccessNotePrefPath = System.IO.Path.Combine(_appDataDir, "dbaccess_nota.txt");

            try
            {
                File.WriteAllText(_dbAccessNotePrefPath, _dbAccessNote ?? "", Encoding.UTF8);
            }
            catch { }
        }

        private void CarregarOcultosDbAccessAppData()
        {
            if (string.IsNullOrEmpty(_dbAccessHiddenPrefPath))
                _dbAccessHiddenPrefPath = System.IO.Path.Combine(_appDataDir, "dbaccess_ocultos.txt");

            _hiddenDbAccessEnvs.Clear();
            if (File.Exists(_dbAccessHiddenPrefPath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(_dbAccessHiddenPrefPath, Encoding.UTF8);
                    foreach (string l in lines)
                    {
                        string trimmed = l.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                            _hiddenDbAccessEnvs.Add(trimmed);
                    }
                }
                catch { }
            }
        }

        private void SalvarOcultosDbAccessAppData()
        {
            if (string.IsNullOrEmpty(_dbAccessHiddenPrefPath))
                _dbAccessHiddenPrefPath = System.IO.Path.Combine(_appDataDir, "dbaccess_ocultos.txt");

            try
            {
                List<string> list = new List<string>(_hiddenDbAccessEnvs);
                File.WriteAllLines(_dbAccessHiddenPrefPath, list.ToArray(), Encoding.UTF8);
            }
            catch { }
        }

        private void InicializarDbAccessConfig()
        {
            CarregarOcultosDbAccessAppData();

            if (string.IsNullOrEmpty(_dbAccessPathPrefPath))
            {
                _dbAccessPathPrefPath = System.IO.Path.Combine(_appDataDir, "dbaccess_path.txt");
            }

            if (File.Exists(_dbAccessPathPrefPath))
            {
                string savedPath = File.ReadAllText(_dbAccessPathPrefPath, Encoding.UTF8).Trim();
                if (!string.IsNullOrEmpty(savedPath))
                {
                    if (File.Exists(savedPath))
                    {
                        _dbAccessConfig.IniPath = savedPath;
                        _dbAccessConfig.FolderPath = System.IO.Path.GetDirectoryName(savedPath);
                    }
                    else if (Directory.Exists(savedPath))
                    {
                        _dbAccessConfig.FolderPath = savedPath;
                        string iniCandidate = System.IO.Path.Combine(savedPath, "dbaccess.ini");
                        if (File.Exists(iniCandidate))
                        {
                            _dbAccessConfig.IniPath = iniCandidate;
                        }
                    }
                }
            }

            // Auto-detectar caso comum se nada estiver salvo
            if (string.IsNullOrEmpty(_dbAccessConfig.FolderPath) || !Directory.Exists(_dbAccessConfig.FolderPath))
            {
                string[] commonPaths = new string[]
                {
                    @"C:\totvs\dbaccess",
                    @"C:\dbaccess",
                    @"C:\totvs\dbaccess64",
                    @"D:\totvs\dbaccess",
                    @"D:\dbaccess"
                };

                foreach (string path in commonPaths)
                {
                    if (Directory.Exists(path) && File.Exists(System.IO.Path.Combine(path, "dbaccess.ini")))
                    {
                        _dbAccessConfig.FolderPath = path;
                        _dbAccessConfig.IniPath = System.IO.Path.Combine(path, "dbaccess.ini");
                        SalvarDbAccessPathAppData(path);
                        break;
                    }
                }
            }
        }

        private void SalvarDbAccessPathAppData(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(_dbAccessPathPrefPath))
                {
                    _dbAccessPathPrefPath = System.IO.Path.Combine(_appDataDir, "dbaccess_path.txt");
                }
                File.WriteAllText(_dbAccessPathPrefPath, path ?? "", Encoding.UTF8);
            }
            catch { }
        }

        public void ShowDbAccessView()
        {
            _isInDashboardView = false;
            _currentTab = HubActiveTab.DbAccess;

            InicializarDbAccessConfig();

            Grid rootGrid = new Grid();
            RowDefinition rowTop = new RowDefinition(); rowTop.Height = GridLength.Auto;
            RowDefinition rowBody = new RowDefinition(); rowBody.Height = new GridLength(1, GridUnitType.Star);
            rootGrid.RowDefinitions.Add(rowTop);
            rootGrid.RowDefinitions.Add(rowBody);

            StackPanel topLeft = CreateBrandHeaderWithTabs(HubActiveTab.DbAccess);
            Border topBar = BuildTopWindowBar(topLeft, null);
            Grid.SetRow(topBar, 0);
            rootGrid.Children.Add(topBar);

            bool hasValidConfig = !string.IsNullOrEmpty(_dbAccessConfig.FolderPath) &&
                                 Directory.Exists(_dbAccessConfig.FolderPath) &&
                                 !string.IsNullOrEmpty(_dbAccessConfig.IniPath) &&
                                 File.Exists(_dbAccessConfig.IniPath);

            UIElement bodyElement;
            if (!hasValidConfig)
            {
                bodyElement = BuildDbAccessSetupView();
            }
            else
            {
                bodyElement = BuildDbAccessMainView();
                CarregarDadosDbAccessAsync();
            }

            Grid.SetRow(bodyElement, 1);
            rootGrid.Children.Add(bodyElement);

            _pageHost.Content = rootGrid;
        }

        // =====================================================================
        // TELA DE ONBOARDING / SELEÇÃO DE PASTA DO DBACCESS
        // =====================================================================
        private UIElement BuildDbAccessSetupView()
        {
            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.Padding = new Thickness(24, 28, 24, 28);

            StackPanel panel = new StackPanel();
            panel.MaxWidth = 680;
            panel.HorizontalAlignment = HorizontalAlignment.Center;
            panel.VerticalAlignment = VerticalAlignment.Center;

            Border card = new Border();
            card.Background = UiTheme.BgCard;
            card.BorderBrush = UiTheme.BorderSubtle;
            card.BorderThickness = new Thickness(1);
            card.CornerRadius = new CornerRadius(12);
            card.Padding = new Thickness(32, 28, 32, 28);

            StackPanel cardStack = new StackPanel();

            // Ícone grande
            Border iconCircle = new Border();
            iconCircle.Width = 64;
            iconCircle.Height = 64;
            iconCircle.CornerRadius = new CornerRadius(32);
            iconCircle.Background = UiTheme.TagBg;
            iconCircle.HorizontalAlignment = HorizontalAlignment.Center;
            iconCircle.Margin = new Thickness(0, 0, 0, 18);

            UIElement dbIcon = CreateTabDatabaseIcon(UiTheme.AccentLightBlue, 32);
            iconCircle.Child = dbIcon;
            cardStack.Children.Add(iconCircle);

            TextBlock title = new TextBlock();
            title.Text = "Configurar DBAccess";
            title.FontSize = 20;
            title.FontWeight = FontWeights.Bold;
            title.Foreground = UiTheme.TextPrimary;
            title.HorizontalAlignment = HorizontalAlignment.Center;
            title.Margin = new Thickness(0, 0, 0, 8);
            cardStack.Children.Add(title);

            TextBlock subtitle = new TextBlock();
            subtitle.Text = "Informe a pasta de instalação do DBAccess (onde se encontra o arquivo dbaccess.ini) para monitorar conexões, portas e gerenciar ambientes.";
            subtitle.FontSize = 13;
            subtitle.Foreground = UiTheme.TextSecondary;
            subtitle.TextWrapping = TextWrapping.Wrap;
            subtitle.TextAlignment = TextAlignment.Center;
            subtitle.Margin = new Thickness(0, 0, 0, 24);
            cardStack.Children.Add(subtitle);

            TextBlock lblPath = new TextBlock();
            lblPath.Text = "Caminho da Pasta do DBAccess:";
            lblPath.FontSize = 12;
            lblPath.FontWeight = FontWeights.SemiBold;
            lblPath.Foreground = UiTheme.TextPrimary;
            lblPath.Margin = new Thickness(0, 0, 0, 6);
            cardStack.Children.Add(lblPath);

            Grid inputGrid = new Grid();
            ColumnDefinition c1 = new ColumnDefinition(); c1.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition c2 = new ColumnDefinition(); c2.Width = GridLength.Auto;
            inputGrid.ColumnDefinitions.Add(c1);
            inputGrid.ColumnDefinitions.Add(c2);

            TextBox txtFolder = new TextBox();
            txtFolder.Text = _dbAccessConfig.FolderPath ?? @"C:\totvs\dbaccess";
            txtFolder.Background = UiTheme.BgInput;
            txtFolder.Foreground = UiTheme.TextPrimary;
            txtFolder.CaretBrush = UiTheme.TextPrimary;
            txtFolder.BorderBrush = UiTheme.BorderSubtle;
            txtFolder.BorderThickness = new Thickness(1);
            txtFolder.Padding = new Thickness(10, 8, 10, 8);
            txtFolder.FontSize = 13;
            Grid.SetColumn(txtFolder, 0);
            inputGrid.Children.Add(txtFolder);

            Button btnBrowse = UiTheme.CreateActionButton("Procurar...", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
            btnBrowse.Height = 36;
            btnBrowse.Padding = new Thickness(14, 0, 14, 0);
            btnBrowse.Margin = new Thickness(10, 0, 0, 0);
            btnBrowse.Click += delegate
            {
                using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog())
                {
                    fbd.Description = "Selecione a pasta do DBAccess contendo dbaccess.ini";
                    if (Directory.Exists(txtFolder.Text))
                        fbd.SelectedPath = txtFolder.Text;

                    if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        txtFolder.Text = fbd.SelectedPath;
                    }
                }
            };
            Grid.SetColumn(btnBrowse, 1);
            inputGrid.Children.Add(btnBrowse);

            cardStack.Children.Add(inputGrid);

            TextBlock errorMsg = new TextBlock();
            errorMsg.Text = "";
            errorMsg.Foreground = UiTheme.AccentRed;
            errorMsg.FontSize = 12;
            errorMsg.Margin = new Thickness(0, 8, 0, 0);
            errorMsg.Visibility = Visibility.Collapsed;
            cardStack.Children.Add(errorMsg);

            Button btnConfirm = UiTheme.CreateActionButton("Confirmar e Carregar Ambientes", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
            btnConfirm.Height = 40;
            btnConfirm.Margin = new Thickness(0, 20, 0, 0);
            btnConfirm.FontWeight = FontWeights.SemiBold;
            btnConfirm.Click += delegate
            {
                string sel = (txtFolder.Text ?? "").Trim();
                if (!Directory.Exists(sel))
                {
                    errorMsg.Text = "O diretório informado não foi encontrado.";
                    errorMsg.Visibility = Visibility.Visible;
                    return;
                }

                string candidateIni = System.IO.Path.Combine(sel, "dbaccess.ini");
                if (!File.Exists(candidateIni))
                {
                    errorMsg.Text = "Arquivo 'dbaccess.ini' não foi localizado nesta pasta.";
                    errorMsg.Visibility = Visibility.Visible;
                    return;
                }

                _dbAccessConfig.FolderPath = sel;
                _dbAccessConfig.IniPath = candidateIni;
                SalvarDbAccessPathAppData(sel);
                ShowDbAccessView();
            };
            cardStack.Children.Add(btnConfirm);

            card.Child = cardStack;
            panel.Children.Add(card);
            scroll.Content = panel;
            return scroll;
        }

        // =====================================================================
        // TELA PRINCIPAL DO DBACCESS (Item 5: Card Único como appserver.ini)
        // =====================================================================
        private UIElement BuildDbAccessMainView()
        {
            CarregarNotaDbAccessAppData();

            Grid mainGrid = new Grid();
            RowDefinition rToolbar = new RowDefinition(); rToolbar.Height = GridLength.Auto;
            RowDefinition rInfoBar = new RowDefinition(); rInfoBar.Height = GridLength.Auto;
            RowDefinition rContent = new RowDefinition(); rContent.Height = new GridLength(1, GridUnitType.Star);
            mainGrid.RowDefinitions.Add(rToolbar);
            mainGrid.RowDefinitions.Add(rInfoBar);
            mainGrid.RowDefinitions.Add(rContent);

            // 1. Toolbar superior (Item 6: icone pasta + busca + card ou não + ocultos + atualizar)
            Border toolbar = BuildDbAccessToolbar();
            Grid.SetRow(toolbar, 0);
            mainGrid.Children.Add(toolbar);

            // 2. Info bar com ativos/inativos e horário de atualização (Item 14)
            Border infoBar = BuildDbAccessInfoBar();
            Grid.SetRow(infoBar, 1);
            mainGrid.Children.Add(infoBar);

            // 3. ScrollViewer com Card Único do DBAccess
            _dbAccessScrollViewer = new ScrollViewer();
            _dbAccessScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _dbAccessScrollViewer.Padding = new Thickness(20, 14, 20, 20);

            StackPanel sp = new StackPanel();
            _dbAccessCardsContainer = sp;
            _dbAccessScrollViewer.Content = _dbAccessCardsContainer;

            Grid.SetRow(_dbAccessScrollViewer, 2);
            mainGrid.Children.Add(_dbAccessScrollViewer);

            return mainGrid;
        }

        private Border BuildDbAccessInfoBar()
        {
            Border infoBar = new Border();
            infoBar.Padding = new Thickness(24, 8, 24, 0);

            DockPanel dp = new DockPanel();

            _dbAccessStatusCountLabel = new TextBlock();
            _dbAccessStatusCountLabel.Text = _dbAccessConfig.IsRunning ? "1 ativo  •  0 inativos" : "0 ativos  •  1 inativo";
            _dbAccessStatusCountLabel.Foreground = UiTheme.TextMuted;
            _dbAccessStatusCountLabel.FontSize = 12;
            _dbAccessStatusCountLabel.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(_dbAccessStatusCountLabel, Dock.Left);
            dp.Children.Add(_dbAccessStatusCountLabel);

            _dbAccessLastUpdateLabel = new TextBlock();
            _dbAccessLastUpdateLabel.Text = string.Format("Atualizado às {0:HH:mm:ss}", DateTime.Now);
            _dbAccessLastUpdateLabel.Foreground = UiTheme.TextMuted;
            _dbAccessLastUpdateLabel.FontSize = 12;
            _dbAccessLastUpdateLabel.VerticalAlignment = VerticalAlignment.Center;
            _dbAccessLastUpdateLabel.HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(_dbAccessLastUpdateLabel, Dock.Right);
            dp.Children.Add(_dbAccessLastUpdateLabel);

            infoBar.Child = dp;
            return infoBar;
        }

        private Border BuildDbAccessToolbar()
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

            // 1. Ícone Pasta (à esquerda)
            Button btnFolder = UiTheme.CreateActionButton(
                UiTheme.CreateFolderIcon(Brushes.White, 16),
                UiTheme.NeutralBtnHex,
                UiTheme.NeutralBtnHoverHex);
            btnFolder.Width = 38;
            btnFolder.Height = 34;
            btnFolder.Padding = new Thickness(0);
            btnFolder.Margin = new Thickness(0, 0, 10, 0);
            btnFolder.ToolTip = "Configurar pasta de instalação do DBAccess";
            btnFolder.Click += delegate
            {
                using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog())
                {
                    fbd.Description = "Selecione a pasta do DBAccess contendo dbaccess.ini";
                    if (Directory.Exists(_dbAccessConfig.FolderPath))
                        fbd.SelectedPath = _dbAccessConfig.FolderPath;

                    if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string candidateIni = System.IO.Path.Combine(fbd.SelectedPath, "dbaccess.ini");
                        if (File.Exists(candidateIni))
                        {
                            _dbAccessConfig.FolderPath = fbd.SelectedPath;
                            _dbAccessConfig.IniPath = candidateIni;
                            SalvarDbAccessPathAppData(fbd.SelectedPath);
                            ShowDbAccessView();
                        }
                        else
                        {
                            MessageBox.Show(this, "Arquivo 'dbaccess.ini' não foi localizado nesta pasta.", "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            };
            Grid.SetColumn(btnFolder, 0);
            toolGrid.Children.Add(btnFolder);

            // 2. Barra de Busca
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
            searchPlaceholder.Text = "Filtrar parâmetros e seções do dbaccess.ini...";
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
                _dbAccessFilterText = txtSearch.Text;
                RenderizarCardsDbAccess();
            };

            searchInner.Children.Add(searchPlaceholder);
            searchInner.Children.Add(txtSearch);
            searchDock.Children.Add(searchInner);
            searchContainer.Child = searchDock;
            Grid.SetColumn(searchContainer, 1);
            toolGrid.Children.Add(searchContainer);

            // 3. Ferramentas à Direita: Card/Lista + Ocultos + Atualizar (idêntico ao AppServer)
            StackPanel rightTools = new StackPanel();
            rightTools.Orientation = Orientation.Horizontal;
            rightTools.VerticalAlignment = VerticalAlignment.Center;

            Border toggleSegmented = UiTheme.CreateViewModeSegmentedToggle(_isCardsViewMode, delegate(bool isCards)
            {
                _isCardsViewMode = isCards;
                RenderizarCardsDbAccess();
            });
            toggleSegmented.Margin = new Thickness(0, 0, 10, 0);
            rightTools.Children.Add(toggleSegmented);

            Button btnToggleHiddenView = UiTheme.CreateActionButton("👁 Ocultos", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
            btnToggleHiddenView.Width = 104;
            btnToggleHiddenView.Height = 34;
            btnToggleHiddenView.Margin = new Thickness(0, 0, 10, 0);
            btnToggleHiddenView.ToolTip = "Alternar entre seções ativas e ocultas";
            btnToggleHiddenView.Click += delegate
            {
                _dbAccessShowingHidden = !_dbAccessShowingHidden;
                RenderizarCardsDbAccess();
            };
            rightTools.Children.Add(btnToggleHiddenView);

            System.Windows.Shapes.Path spinIcon = (System.Windows.Shapes.Path)UiTheme.CreateThinRefreshIcon(UiTheme.AccentLightBlue);
            Button btnReload = UiTheme.CreateFramelessIconButton(spinIcon, "Atualizar dados do dbaccess.ini e testar conectividade");
            btnReload.Click += delegate
            {
                RotateTransform rt = spinIcon.RenderTransform as RotateTransform;
                if (rt == null) { rt = new RotateTransform(0); spinIcon.RenderTransform = rt; }
                DoubleAnimation spin = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(650));
                spin.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut };
                rt.BeginAnimation(RotateTransform.AngleProperty, spin);
                CarregarDadosDbAccessAsync();
            };
            rightTools.Children.Add(btnReload);

            Grid.SetColumn(rightTools, 2);
            toolGrid.Children.Add(rightTools);

            toolbar.Child = toolGrid;
            return toolbar;
        }

        // =====================================================================
        // CARREGAMENTO E PARSING DO DBACCESS.INI E DBMONITOR.INI
        // =====================================================================
        private void CarregarDadosDbAccessAsync()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                // 1. Parse dbaccess.ini
                ParseDbAccessIni();

                // 2. Parse dbmonitor.ini
                ParseDbMonitorIni();

                // 3. Teste de processo e porta TCP
                TestarStatusDbAccess();

                Dispatcher.BeginInvoke(new Action(delegate
                {
                    AtualizarBannerDbAccess();
                    RenderizarCardsDbAccess();
                }));
            });
        }

        private void ParseDbAccessIni()
        {
            if (string.IsNullOrEmpty(_dbAccessConfig.IniPath) || !File.Exists(_dbAccessConfig.IniPath))
                return;

            try
            {
                List<IniSection> sections = ParseIniFile(_dbAccessConfig.IniPath);
                _dbAccessConfig.RawSections = sections;
                _dbAccessConfig.Environments.Clear();

                // 1. General
                IniSection generalSec = sections.FirstOrDefault(s => string.Equals(s.Name, "General", StringComparison.OrdinalIgnoreCase));
                if (generalSec != null)
                {
                    IniEntry licServerEntry = generalSec.Entries.FirstOrDefault(e => string.Equals(e.Key, "LicenseServer", StringComparison.OrdinalIgnoreCase));
                    if (licServerEntry != null && !string.IsNullOrEmpty(licServerEntry.Value))
                        _dbAccessConfig.LicenseServer = licServerEntry.Value;

                    IniEntry licPortEntry = generalSec.Entries.FirstOrDefault(e => string.Equals(e.Key, "LicensePort", StringComparison.OrdinalIgnoreCase));
                    if (licPortEntry != null && !string.IsNullOrEmpty(licPortEntry.Value))
                        _dbAccessConfig.LicensePort = licPortEntry.Value;
                }

                // 2. DBMS Sections e environments=
                string[] knownDbms = new string[] { "MSSQL", "ORACLE", "POSTGRES", "POSTGRESQL", "MYSQL", "INFORMIX", "DB2", "PROGRESS" };
                HashSet<string> processedSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (IniSection sec in sections)
                {
                    if (sec.IsCommented) continue;

                    string secName = sec.Name.Trim();
                    bool isKnownDbms = knownDbms.Any(k => string.Equals(k, secName, StringComparison.OrdinalIgnoreCase));

                    IniEntry envsEntry = sec.Entries.FirstOrDefault(e => string.Equals(e.Key, "environments", StringComparison.OrdinalIgnoreCase));
                    if (envsEntry != null && !string.IsNullOrEmpty(envsEntry.Value))
                    {
                        string dbmsName = secName.ToUpperInvariant();
                        string[] envNames = envsEntry.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string rawEnv in envNames)
                        {
                            string envName = rawEnv.Trim();
                            if (string.IsNullOrEmpty(envName)) continue;

                            string targetSecName = dbmsName + "/" + envName;
                            IniSection envSec = sections.FirstOrDefault(s => string.Equals(s.Name, targetSecName, StringComparison.OrdinalIgnoreCase));
                            if (envSec == null)
                            {
                                envSec = sections.FirstOrDefault(s => string.Equals(s.Name, envName, StringComparison.OrdinalIgnoreCase));
                            }

                            DbAccessEnvironment env = CriarAmbienteAPartirDaSecao(dbmsName, envName, targetSecName, envSec);
                            _dbAccessConfig.Environments.Add(env);
                            if (envSec != null)
                                processedSections.Add(envSec.Name);
                        }
                    }
                }

                // 3. Procura seções adicionais no formato [DBMS/Ambiente] que não estavam em environments=
                foreach (IniSection sec in sections)
                {
                    if (sec.IsCommented || processedSections.Contains(sec.Name))
                        continue;

                    int slashIdx = sec.Name.IndexOf('/');
                    if (slashIdx > 0)
                    {
                        string dbms = sec.Name.Substring(0, slashIdx).Trim().ToUpperInvariant();
                        string envName = sec.Name.Substring(slashIdx + 1).Trim();

                        if (!string.IsNullOrEmpty(envName))
                        {
                            DbAccessEnvironment env = CriarAmbienteAPartirDaSecao(dbms, envName, sec.Name, sec);
                            _dbAccessConfig.Environments.Add(env);
                            processedSections.Add(sec.Name);
                        }
                    }
                }
            }
            catch { }
        }

        private DbAccessEnvironment CriarAmbienteAPartirDaSecao(string dbms, string envName, string sectionName, IniSection sec)
        {
            DbAccessEnvironment env = new DbAccessEnvironment();
            env.Dbms = dbms;
            env.Name = envName;
            env.SectionName = sectionName;

            if (sec != null)
            {
                foreach (IniEntry e in sec.Entries)
                {
                    if (e.IsPureComment || string.IsNullOrEmpty(e.Key)) continue;

                    string key = e.Key.Trim();
                    string val = e.Value != null ? e.Value.Trim() : "";

                    if (string.Equals(key, "user", StringComparison.OrdinalIgnoreCase))
                        env.User = val;
                    else if (string.Equals(key, "password", StringComparison.OrdinalIgnoreCase))
                        env.Password = val;
                    else if (string.Equals(key, "TableSpace", StringComparison.OrdinalIgnoreCase))
                        env.TableSpace = val;
                    else if (string.Equals(key, "IndexSpace", StringComparison.OrdinalIgnoreCase))
                        env.IndexSpace = val;
                    else if (string.Equals(key, "Disable", StringComparison.OrdinalIgnoreCase))
                    {
                        env.IsDisabled = string.Equals(val, "1", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(val, "true", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(val, "yes", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (string.Equals(key, "Server", StringComparison.OrdinalIgnoreCase))
                        env.Server = val;
                    else if (string.Equals(key, "Port", StringComparison.OrdinalIgnoreCase))
                        env.Port = val;
                    else if (string.Equals(key, "Database", StringComparison.OrdinalIgnoreCase))
                        env.Database = val;
                    else if (string.Equals(key, "Alias", StringComparison.OrdinalIgnoreCase))
                        env.Alias = val;
                    else
                        env.AdditionalKeys[key] = val;
                }
            }

            return env;
        }

        private void ParseDbMonitorIni()
        {
            if (string.IsNullOrEmpty(_dbAccessConfig.FolderPath)) return;

            string monIni = System.IO.Path.Combine(_dbAccessConfig.FolderPath, "dbmonitor.ini");
            _dbAccessConfig.MonitorIniPath = monIni;

            if (File.Exists(monIni))
            {
                try
                {
                    List<IniSection> sections = ParseIniFile(monIni);
                    IniSection monSec = sections.FirstOrDefault(s => string.Equals(s.Name, "Monitor", StringComparison.OrdinalIgnoreCase));
                    if (monSec != null)
                    {
                        IniEntry srv = monSec.Entries.FirstOrDefault(e => string.Equals(e.Key, "Server", StringComparison.OrdinalIgnoreCase));
                        if (srv != null && !string.IsNullOrEmpty(srv.Value))
                            _dbAccessConfig.MonitorServer = srv.Value.Trim();

                        IniEntry prt = monSec.Entries.FirstOrDefault(e => string.Equals(e.Key, "Port", StringComparison.OrdinalIgnoreCase));
                        int portVal;
                        if (prt != null && int.TryParse(prt.Value.Trim(), out portVal) && portVal > 0)
                            _dbAccessConfig.MonitorPort = portVal;
                    }
                }
                catch { }
            }
        }

        private void TestarStatusDbAccess()
        {
            // 1. Processo do DBAccess
            try
            {
                Process[] procs = Process.GetProcessesByName("dbaccess");
                if (procs.Length == 0)
                    procs = Process.GetProcessesByName("dbaccess64");

                int dbPid = 0;
                if (procs.Length > 0)
                {
                    dbPid = procs[0].Id;
                    _dbAccessConfig.ProcessName = procs[0].ProcessName;
                }
                else
                {
                    int mPort = _dbAccessConfig.MonitorPort > 0 ? _dbAccessConfig.MonitorPort : 7890;
                    dbPid = NativeWin32.GetProcessIdByTcpPort(mPort);
                    if (dbPid > 0) _dbAccessConfig.ProcessName = "dbaccess";
                }

                if (dbPid > 0)
                {
                    _dbAccessConfig.IsRunning = true;
                    _dbAccessConfig.ProcessId = dbPid;

                    TimeSpan lastCpu = _dbAccessConfig.LastCpuTime;
                    DateTime lastSample = _dbAccessConfig.LastCpuSampleTime;
                    double cpu;
                    long ram;
                    NativeWin32.GetProcessCpuAndRam(_dbAccessConfig.ProcessId, ref lastCpu, ref lastSample, out cpu, out ram);
                    _dbAccessConfig.CpuPercent = cpu;
                    _dbAccessConfig.RamMb = ram;
                    _dbAccessConfig.LastCpuTime = lastCpu;
                    _dbAccessConfig.LastCpuSampleTime = lastSample;
                }
                else
                {
                    _dbAccessConfig.IsRunning = false;
                    _dbAccessConfig.ProcessId = 0;
                    _dbAccessConfig.ProcessName = "";
                    _dbAccessConfig.CpuPercent = 0;
                    _dbAccessConfig.RamMb = 0;
                }
            }
            catch
            {
                _dbAccessConfig.IsRunning = false;
                _dbAccessConfig.CpuPercent = 0;
                _dbAccessConfig.RamMb = 0;
            }

            // 2. Teste de Socket TCP na porta do DBAccess (7890 padrão)
            int targetPort = _dbAccessConfig.MonitorPort > 0 ? _dbAccessConfig.MonitorPort : 7890;
            string targetHost = !string.IsNullOrEmpty(_dbAccessConfig.MonitorServer) ? _dbAccessConfig.MonitorServer : "127.0.0.1";
            if (string.Equals(targetHost, "localhost", StringComparison.OrdinalIgnoreCase))
                targetHost = "127.0.0.1";

            Stopwatch sw = Stopwatch.StartNew();
            bool portOpen = false;
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    IAsyncResult ar = client.BeginConnect(targetHost, targetPort, null, null);
                    bool connected = ar.AsyncWaitHandle.WaitOne(750, true);
                    if (connected && client.Connected)
                    {
                        client.EndConnect(ar);
                        portOpen = true;
                    }
                }
            }
            catch
            {
                portOpen = false;
            }
            sw.Stop();

            _dbAccessConfig.IsPortOpen = portOpen;
            _dbAccessConfig.LatencyMs = portOpen ? sw.ElapsedMilliseconds : -1;

            if (_dbAccessConfig.IsPortOpen && _dbAccessConfig.IsRunning)
            {
                _dbAccessConfig.StatusSummary = string.Format("Ativo (PID {0}, Porta {1} respondendo em {2}ms)",
                    _dbAccessConfig.ProcessId, targetPort, _dbAccessConfig.LatencyMs);
            }
            else if (_dbAccessConfig.IsPortOpen)
            {
                _dbAccessConfig.StatusSummary = string.Format("Ativo (Porta {0} respondendo em {1}ms)",
                    targetPort, _dbAccessConfig.LatencyMs);
            }
            else if (_dbAccessConfig.IsRunning)
            {
                _dbAccessConfig.StatusSummary = string.Format("Processo em execução (PID {0}), porta {1} fechada",
                    _dbAccessConfig.ProcessId, targetPort);
            }
            else
            {
                _dbAccessConfig.StatusSummary = string.Format("Inativo (Porta {0} inacessível)", targetPort);
            }
        }

        private void AtualizarBannerDbAccess()
        {
            _dbAccessLastUpdatedTime = DateTime.Now;

            if (_dbAccessServerStatusBadge != null && _dbAccessServerStatusText != null)
            {
                bool isOnline = _dbAccessConfig.IsPortOpen || _dbAccessConfig.IsRunning;
                _dbAccessServerStatusBadge.Background = isOnline ? UiTheme.AccentGreen : UiTheme.BrushFromHex("#475569");
                _dbAccessServerStatusText.Text = isOnline
                    ? string.Format("● DBAccess Ativo ({0}ms)", _dbAccessConfig.LatencyMs >= 0 ? _dbAccessConfig.LatencyMs.ToString() : "0")
                    : "○ DBAccess Inativo";
                _dbAccessServerStatusText.Foreground = Brushes.White;
            }
        }

        // =====================================================================
        // RENDERIZAÇÃO DOS CARDS DE AMBIENTES DO DBACCESS
        // =====================================================================
        private void RenderizarCardsDbAccess()
        {
            if (_dbAccessCardsContainer == null) return;
            _dbAccessCardsContainer.Children.Clear();

            // Item 14: Atualiza InfoBar
            if (_dbAccessStatusCountLabel != null)
            {
                _dbAccessStatusCountLabel.Text = _dbAccessConfig.IsRunning ? "1 ativo  •  0 inativos" : "0 ativos  •  1 inativo";
            }
            if (_dbAccessLastUpdateLabel != null)
            {
                _dbAccessLastUpdateLabel.Text = string.Format("Atualizado às {0:HH:mm:ss}", DateTime.Now);
            }

            // Item 5: Card Único do DBAccess (estilo appserver.ini)
            Border cardUnico = CriarCardDbAccessUnico();
            if (cardUnico != null)
            {
                _dbAccessCardsContainer.Children.Add(cardUnico);
            }

            AtualizarBannerDbAccess();
        }

        private Border CriarCardDbAccessUnico()
        {
            bool isHidden = _hiddenDbAccessEnvs.Contains("DBAccess");
            if (isHidden && !_dbAccessShowingHidden)
                return null;

            Border card = new Border();
            card.Background = _dbAccessConfig.IsRunning ? UiTheme.BgCardConsoleActive : UiTheme.BgCard;
            card.BorderBrush = _dbAccessConfig.IsRunning ? UiTheme.AccentGreen : UiTheme.BorderSubtle;
            card.BorderThickness = new Thickness(1);
            card.CornerRadius = new CornerRadius(12);
            card.Padding = new Thickness(16, 14, 16, 14);
            card.Margin = new Thickness(0, 0, 0, 10);
            card.HorizontalAlignment = HorizontalAlignment.Stretch;

            Grid cardGrid = new Grid();
            RowDefinition rMain = new RowDefinition(); rMain.Height = GridLength.Auto;
            RowDefinition rNote = new RowDefinition(); rNote.Height = GridLength.Auto;
            cardGrid.RowDefinitions.Add(rMain);
            cardGrid.RowDefinitions.Add(rNote);

            ColumnDefinition colLeft = new ColumnDefinition(); colLeft.Width = GridLength.Auto;
            ColumnDefinition colInfo = new ColumnDefinition(); colInfo.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition colPower = new ColumnDefinition(); colPower.Width = GridLength.Auto;
            ColumnDefinition colDots = new ColumnDefinition(); colDots.Width = GridLength.Auto;

            cardGrid.ColumnDefinitions.Add(colLeft);
            cardGrid.ColumnDefinitions.Add(colInfo);
            cardGrid.ColumnDefinitions.Add(colPower);
            cardGrid.ColumnDefinitions.Add(colDots);

            // ================= COLUNA 0: STATUS BADGE + PID + EDITAR INI =================
            StackPanel leftStack = new StackPanel();
            leftStack.Orientation = Orientation.Vertical;
            leftStack.VerticalAlignment = VerticalAlignment.Center;
            leftStack.HorizontalAlignment = HorizontalAlignment.Center;
            leftStack.Width = 96;
            leftStack.Margin = new Thickness(0, 0, 16, 0);

            Border statusBadge = new Border();
            statusBadge.Width = 96;
            statusBadge.Height = 26;
            statusBadge.CornerRadius = new CornerRadius(10);
            statusBadge.Padding = new Thickness(6, 3, 6, 3);
            statusBadge.HorizontalAlignment = HorizontalAlignment.Center;
            statusBadge.BorderThickness = new Thickness(1);
            statusBadge.Background = _dbAccessConfig.IsRunning ? UiTheme.BrushFromHex("#064E3B") : UiTheme.TagBg;
            statusBadge.BorderBrush = _dbAccessConfig.IsRunning ? UiTheme.AccentGreen : UiTheme.BorderSubtle;

            StackPanel statusInner = new StackPanel();
            statusInner.Orientation = Orientation.Horizontal;
            statusInner.HorizontalAlignment = HorizontalAlignment.Center;
            statusInner.VerticalAlignment = VerticalAlignment.Center;

            Border statusDot = new Border();
            statusDot.Width = 7;
            statusDot.Height = 7;
            statusDot.CornerRadius = new CornerRadius(3.5);
            statusDot.Margin = new Thickness(0, 0, 5, 0);
            statusDot.Background = _dbAccessConfig.IsRunning ? UiTheme.AccentGreen : UiTheme.TextMuted;
            statusInner.Children.Add(statusDot);

            TextBlock statusTxt = new TextBlock();
            statusTxt.Text = _dbAccessConfig.IsRunning ? "RUNNING" : "STOPPED";
            statusTxt.FontSize = 11;
            statusTxt.FontWeight = FontWeights.Bold;
            statusTxt.Foreground = _dbAccessConfig.IsRunning ? UiTheme.AccentGreen : UiTheme.TextSecondary;
            statusInner.Children.Add(statusTxt);
            statusBadge.Child = statusInner;
            leftStack.Children.Add(statusBadge);

            // Tag de PID
            if (_dbAccessConfig.IsRunning && _dbAccessConfig.ProcessId > 0)
            {
                Border pidBadge = new Border();
                pidBadge.Width = 96;
                pidBadge.Height = 24;
                pidBadge.Background = UiTheme.BrushFromHex("#0C4A6E");
                pidBadge.BorderBrush = UiTheme.BrushFromHex(UiTheme.PrimaryBlueHex);
                pidBadge.BorderThickness = new Thickness(1);
                pidBadge.CornerRadius = new CornerRadius(5);
                pidBadge.Padding = new Thickness(4, 2, 4, 2);
                pidBadge.Margin = new Thickness(0, 4, 0, 0);

                TextBlock pidTxt = new TextBlock();
                pidTxt.Text = "PID " + _dbAccessConfig.ProcessId;
                pidTxt.Foreground = UiTheme.AccentLightBlue;
                pidTxt.FontSize = 10;
                pidTxt.FontWeight = FontWeights.Bold;
                pidTxt.HorizontalAlignment = HorizontalAlignment.Center;
                pidTxt.VerticalAlignment = VerticalAlignment.Center;
                pidBadge.Child = pidTxt;
                leftStack.Children.Add(pidBadge);
            }

            // Botão Editar dbaccess.ini (Lápis / Editar INI - Item 2)
            Button btnEditIni = UiTheme.CreateActionButton(
                UiTheme.CreatePencilIcon(UiTheme.TextSecondary, 13),
                UiTheme.NeutralBtnHex,
                UiTheme.NeutralBtnHoverHex);
            btnEditIni.Width = 34;
            btnEditIni.Height = 26;
            btnEditIni.Margin = new Thickness(0, 6, 0, 0);
            btnEditIni.ToolTip = "Editar dbaccess.ini com o Editor Visual";
            btnEditIni.Click += delegate { ShowDbAccessIniEditorView(); };
            leftStack.Children.Add(btnEditIni);

            Grid.SetRow(leftStack, 0);
            Grid.SetColumn(leftStack, 0);
            cardGrid.Children.Add(leftStack);

            // ================= COLUNA 1: INFO (NOME, CAMINHO, TAGS, CPU/RAM) =================
            StackPanel infoStack = new StackPanel();
            infoStack.VerticalAlignment = VerticalAlignment.Center;
            infoStack.Margin = new Thickness(0, 0, 12, 0);

            // Nome DBAccess com botão copiar ao lado (Item 12)
            WrapPanel titleWrap = new WrapPanel();
            titleWrap.VerticalAlignment = VerticalAlignment.Center;

            TextBlock title = new TextBlock();
            title.Text = "DBAccess";
            title.FontSize = 15;
            title.FontWeight = FontWeights.Bold;
            title.Foreground = UiTheme.BrushFromHex(UiTheme.PrimaryBlueHex);
            title.Margin = new Thickness(0, 0, 6, 0);
            title.VerticalAlignment = VerticalAlignment.Center;
            titleWrap.Children.Add(title);

            Button btnCopyName = UiTheme.CreateFramelessIconButton(
                UiTheme.CreateCopyIcon(UiTheme.TextMuted, 13),
                "Copiar nome DBAccess");
            btnCopyName.Padding = new Thickness(2);
            btnCopyName.Margin = new Thickness(0, 0, 8, 0);
            btnCopyName.VerticalAlignment = VerticalAlignment.Center;
            btnCopyName.Click += delegate { try { Clipboard.SetText("DBAccess"); } catch { } };
            titleWrap.Children.Add(btnCopyName);
            infoStack.Children.Add(titleWrap);

            // Caminho clicável com botão copiar ao lado (Item 12)
            StackPanel pathStack = new StackPanel();
            pathStack.Orientation = Orientation.Horizontal;
            pathStack.Margin = new Thickness(0, 3, 0, 6);

            TextBlock lblFolder = new TextBlock();
            lblFolder.Text = _dbAccessConfig.FolderPath ?? "";
            lblFolder.FontSize = 11;
            lblFolder.Foreground = UiTheme.TextMuted;
            lblFolder.Cursor = Cursors.Hand;
            lblFolder.ToolTip = "Clique para abrir no Windows Explorer: " + (_dbAccessConfig.FolderPath ?? "");
            lblFolder.VerticalAlignment = VerticalAlignment.Center;
            lblFolder.MouseEnter += delegate { lblFolder.Foreground = UiTheme.AccentLightBlue; lblFolder.TextDecorations = TextDecorations.Underline; };
            lblFolder.MouseLeave += delegate { lblFolder.Foreground = UiTheme.TextMuted; lblFolder.TextDecorations = null; };
            lblFolder.MouseLeftButtonUp += delegate { AbrirNoExplorer(_dbAccessConfig.FolderPath); };
            pathStack.Children.Add(lblFolder);

            Button btnCopyPath = UiTheme.CreateFramelessIconButton(
                UiTheme.CreateCopyIcon(UiTheme.TextMuted, 11),
                "Copiar caminho do DBAccess");
            btnCopyPath.Padding = new Thickness(2);
            btnCopyPath.Margin = new Thickness(6, 0, 0, 0);
            btnCopyPath.VerticalAlignment = VerticalAlignment.Center;
            btnCopyPath.Click += delegate { try { Clipboard.SetText(_dbAccessConfig.FolderPath ?? ""); } catch { } };
            pathStack.Children.Add(btnCopyPath);
            infoStack.Children.Add(pathStack);

            Grid metaContainer = new Grid();
            metaContainer.Margin = new Thickness(0, 0, 0, 4);
            metaContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            metaContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            UniformGrid metaGrid = new UniformGrid();
            metaGrid.Columns = 2;

            int monPort = _dbAccessConfig.MonitorPort > 0 ? _dbAccessConfig.MonitorPort : 7890;
            metaGrid.Children.Add(CreateMetaPill("Porta", monPort.ToString()));
            metaGrid.Children.Add(CreateMetaPill("License", _dbAccessConfig.LicenseServer + ":" + _dbAccessConfig.LicensePort));
            metaGrid.Children.Add(CreateMetaPill("Aliases", _dbAccessConfig.Environments.Count + " configurados"));

            HashSet<string> dbmsSet = new HashSet<string>(_dbAccessConfig.Environments.Select(e => e.Dbms).Where(d => !string.IsNullOrEmpty(d)), StringComparer.OrdinalIgnoreCase);
            if (dbmsSet.Count > 0)
                metaGrid.Children.Add(CreateMetaPill("DBMS", string.Join(", ", dbmsSet.ToArray())));

            Grid.SetColumn(metaGrid, 0);
            metaContainer.Children.Add(metaGrid);
            infoStack.Children.Add(metaContainer);

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
            cpuTxt.Text = _dbAccessConfig.IsRunning ? string.Format("CPU: {0:0.0}%", _dbAccessConfig.CpuPercent) : "CPU: -";
            cpuTxt.FontSize = 10;
            cpuTxt.FontWeight = FontWeights.SemiBold;
            cpuTxt.Foreground = _dbAccessConfig.IsRunning ? UiTheme.AccentLightBlue : UiTheme.TextMuted;
            cpuBadge.Child = cpuTxt;
            metricsStack.Children.Add(cpuBadge);
            _dbAccessConfig.CpuText = cpuTxt;

            Border ramBadge = new Border();
            ramBadge.Background = UiTheme.BrushFromHex("#0F172A");
            ramBadge.BorderBrush = UiTheme.BorderSubtle;
            ramBadge.BorderThickness = new Thickness(1);
            ramBadge.CornerRadius = new CornerRadius(5);
            ramBadge.Padding = new Thickness(6, 2, 6, 2);

            TextBlock ramTxt = new TextBlock();
            ramTxt.Text = _dbAccessConfig.IsRunning ? string.Format("RAM: {0} MB", _dbAccessConfig.RamMb) : "RAM: -";
            ramTxt.FontSize = 10;
            ramTxt.FontWeight = FontWeights.SemiBold;
            ramTxt.Foreground = _dbAccessConfig.IsRunning ? UiTheme.AccentGreen : UiTheme.TextMuted;
            ramBadge.Child = ramTxt;
            metricsStack.Children.Add(ramBadge);
            _dbAccessConfig.RamText = ramTxt;
            _dbAccessConfig.CardBorder = card;

            infoStack.Children.Add(metricsStack);

            Grid.SetRow(infoStack, 0);
            Grid.SetColumn(infoStack, 1);
            cardGrid.Children.Add(infoStack);

            StackPanel powerStack = new StackPanel();
            powerStack.Orientation = Orientation.Vertical;
            powerStack.VerticalAlignment = VerticalAlignment.Center;
            powerStack.HorizontalAlignment = HorizontalAlignment.Center;
            powerStack.Margin = new Thickness(12, 0, 12, 0);

            Button btnStart = UiTheme.CreateActionButton(UiTheme.CreatePlayIcon(Brushes.White, 13), "#059669", "#10B981");
            btnStart.Width = 34; btnStart.Height = 26; btnStart.Padding = new Thickness(0); btnStart.Margin = new Thickness(0, 2, 0, 2);
            btnStart.ToolTip = "Iniciar DBAccess";
            btnStart.IsEnabled = !_dbAccessConfig.IsRunning;
            btnStart.Opacity = _dbAccessConfig.IsRunning ? 0.3 : 1.0;
            btnStart.Click += delegate { IniciarDbAccessAsync(); };

            Button btnRestart = UiTheme.CreateActionButton(UiTheme.CreateRestartIcon(Brushes.White, 13), "#D97706", "#F59E0B");
            btnRestart.Width = 34; btnRestart.Height = 26; btnRestart.Padding = new Thickness(0); btnRestart.Margin = new Thickness(0, 2, 0, 2);
            btnRestart.ToolTip = "Reiniciar DBAccess";
            btnRestart.IsEnabled = _dbAccessConfig.IsRunning;
            btnRestart.Opacity = _dbAccessConfig.IsRunning ? 1.0 : 0.3;
            btnRestart.Click += delegate { ReiniciarDbAccessAsync(); };

            Button btnStop = UiTheme.CreateActionButton(UiTheme.CreateStopXIcon(Brushes.White, 13), "#DC2626", "#EF4444");
            btnStop.Width = 34; btnStop.Height = 26; btnStop.Padding = new Thickness(0); btnStop.Margin = new Thickness(0, 2, 0, 2);
            btnStop.ToolTip = "Parar DBAccess";
            btnStop.IsEnabled = _dbAccessConfig.IsRunning;
            btnStop.Opacity = _dbAccessConfig.IsRunning ? 1.0 : 0.3;
            btnStop.Click += delegate { PararDbAccessAsync(); };

            powerStack.Children.Add(btnStart);
            powerStack.Children.Add(btnRestart);
            powerStack.Children.Add(btnStop);

            Grid.SetRow(powerStack, 0);
            Grid.SetColumn(powerStack, 2);
            cardGrid.Children.Add(powerStack);

            Button btnDots = UiTheme.CreateFramelessIconButton(UiTheme.CreateThreeDotsIcon(UiTheme.TextSecondary, 16), "Mais opções do DBAccess");
            btnDots.VerticalAlignment = VerticalAlignment.Top;
            btnDots.HorizontalAlignment = HorizontalAlignment.Right;
            btnDots.Margin = new Thickness(4, -4, 0, 0);

            ContextMenu menuDots = UiTheme.CreateDarkContextMenu();

            menuDots.Items.Add(UiTheme.CreateDarkMenuItem("Logs", UiTheme.CreateCopyIcon(UiTheme.AccentLightBlue, 14), delegate
            {
                AbrirTelaCompletaLogs("DBAccess", _dbAccessConfig.FolderPath, _dbAccessConfig.ProcessId, "dbaccess");
            }));

            menuDots.Items.Add(UiTheme.CreateDarkMenuItem("Editar INI", UiTheme.CreatePencilIcon(UiTheme.TextSecondary, 14), delegate
            {
                ShowDbAccessIniEditorView();
            }));

            menuDots.Items.Add(UiTheme.CreateDarkMenuItem("Atualizar Binário", UiTheme.CreateThinRefreshIcon(UiTheme.TextSecondary), delegate
            {
                MessageBox.Show(this, "Funcionalidade em desenvolvimento:\nTroca e atualização automática de binários em breve!", "TOTVSHub", MessageBoxButton.OK, MessageBoxImage.Information);
            }));

            menuDots.Items.Add(UiTheme.CreateDarkMenuItem("Comentário", UiTheme.CreateCommentBubbleIcon(UiTheme.TextSecondary, 14), delegate
            {
                EditarNotaDbAccess();
            }));

            menuDots.Items.Add(new Separator { Background = UiTheme.BorderSubtle, Margin = new Thickness(4, 2, 4, 2) });

            menuDots.Items.Add(UiTheme.CreateDarkMenuItem(isHidden ? "Desocultar DBAccess" : "Ocultar DBAccess", isHidden ? UiTheme.CreateEyeOffIcon(UiTheme.TextSecondary, 14) : UiTheme.CreateEyeIcon(UiTheme.TextSecondary, 14), delegate
            {
                if (_hiddenDbAccessEnvs.Contains("DBAccess")) _hiddenDbAccessEnvs.Remove("DBAccess");
                else _hiddenDbAccessEnvs.Add("DBAccess");
                SalvarOcultosDbAccessAppData();
                RenderizarCardsDbAccess();
            }));

            menuDots.Items.Add(UiTheme.CreateDarkMenuItem("Abrir pasta no Explorer", UiTheme.CreateFolderIcon(UiTheme.TextSecondary, 14), delegate
            {
                AbrirNoExplorer(_dbAccessConfig.FolderPath);
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

            // Banner de anotação/comentário se houver (Item 1 & 11)
            if (!string.IsNullOrWhiteSpace(_dbAccessNote))
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
                noteBanner.MouseLeftButtonUp += delegate { EditarNotaDbAccess(); };

                TextBlock noteLbl = new TextBlock();
                noteLbl.Text = "📝 " + _dbAccessNote;
                noteLbl.Foreground = UiTheme.NoteTextFg;
                noteLbl.FontSize = 12;
                noteLbl.TextWrapping = TextWrapping.Wrap;
                noteBanner.Child = noteLbl;

                Grid.SetRow(noteBanner, 1);
                Grid.SetColumnSpan(noteBanner, 4);
                cardGrid.Children.Add(noteBanner);
            }

            card.Child = cardGrid;
            return card;
        }

        private void EditarNotaDbAccess()
        {
            CarregarNotaDbAccessAppData();

            Window dlg = new Window();
            dlg.Title = "Anotação — DBAccess";
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
                if (e.ClickCount == 1) { try { dlg.DragMove(); } catch { } }
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
            lblTitle.Text = "Anotação para DBAccess";
            lblTitle.Foreground = UiTheme.TextPrimary;
            lblTitle.FontSize = 14;
            lblTitle.FontWeight = FontWeights.Bold;
            lblTitle.VerticalAlignment = VerticalAlignment.Center;
            topRow.Children.Add(lblTitle);
            stack.Children.Add(topRow);

            TextBox txtNote = new TextBox();
            txtNote.Text = _dbAccessNote ?? "";
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
                _dbAccessNote = "";
                SalvarNotaDbAccessAppData();
                dlg.Close();
                RenderizarCardsDbAccess();
            };

            Button btnSave = UiTheme.CreateActionButton("Salvar", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
            btnSave.Click += delegate
            {
                _dbAccessNote = (txtNote.Text ?? "").Trim();
                SalvarNotaDbAccessAppData();
                dlg.Close();
                RenderizarCardsDbAccess();
            };

            btnsRow.Children.Add(btnClear);
            btnsRow.Children.Add(btnSave);
            footerRow.Children.Add(btnsRow);
            stack.Children.Add(footerRow);

            outer.Child = stack;
            dlg.Content = outer;
            dlg.ShowDialog();
        }

        // =====================================================================
        // GERENCIADOR DE ALIASES E USUÁRIOS DO DBACCESS (Item 5)
        // =====================================================================
        private void AbrirGerenciadorUsuariosDbAccess()
        {
            Window dlg = new Window();
            dlg.Title = "Configuração do dbaccess.ini - Gerenciar Aliases e Usuários";
            dlg.Width = 680;
            dlg.Height = 520;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.Owner = this;
            dlg.Background = UiTheme.BgWindow;
            dlg.WindowStyle = WindowStyle.ToolWindow;

            Border shell = new Border();
            shell.Padding = new Thickness(20);
            shell.Background = UiTheme.BgWindow;

            Grid g = new Grid();
            RowDefinition rTitle = new RowDefinition(); rTitle.Height = GridLength.Auto;
            RowDefinition rList = new RowDefinition(); rList.Height = new GridLength(1, GridUnitType.Star);
            RowDefinition rBtns = new RowDefinition(); rBtns.Height = GridLength.Auto;
            g.RowDefinitions.Add(rTitle);
            g.RowDefinitions.Add(rList);
            g.RowDefinitions.Add(rBtns);

            TextBlock title = new TextBlock();
            title.Text = "Aliases e Usuários Configurados no dbaccess.ini";
            title.FontSize = 16;
            title.FontWeight = FontWeights.Bold;
            title.Foreground = UiTheme.TextPrimary;
            title.Margin = new Thickness(0, 0, 0, 14);
            Grid.SetRow(title, 0);
            g.Children.Add(title);

            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            StackPanel listStack = new StackPanel();

            if (_dbAccessConfig.Environments.Count == 0)
            {
                TextBlock empty = new TextBlock();
                empty.Text = "Nenhuma seção de banco de dados encontrada no dbaccess.ini.";
                empty.Foreground = UiTheme.TextMuted;
                empty.FontSize = 13;
                empty.Margin = new Thickness(0, 14, 0, 0);
                listStack.Children.Add(empty);
            }
            else
            {
                foreach (DbAccessEnvironment env in _dbAccessConfig.Environments)
                {
                    DbAccessEnvironment capturedEnv = env;
                    Border itemBox = new Border();
                    itemBox.Background = UiTheme.BgCard;
                    itemBox.BorderBrush = UiTheme.BorderSubtle;
                    itemBox.BorderThickness = new Thickness(1);
                    itemBox.CornerRadius = new CornerRadius(8);
                    itemBox.Padding = new Thickness(12, 10, 12, 10);
                    itemBox.Margin = new Thickness(0, 0, 0, 8);

                    DockPanel dp = new DockPanel();

                    Button btnEdit = UiTheme.CreateActionButton("Editar", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
                    btnEdit.Height = 28;
                    btnEdit.Padding = new Thickness(12, 0, 12, 0);
                    btnEdit.FontSize = 11;
                    DockPanel.SetDock(btnEdit, Dock.Right);
                    btnEdit.Click += delegate
                    {
                        AbrirEditorSecaoAmbienteDbAccess(capturedEnv);
                    };
                    dp.Children.Add(btnEdit);

                    StackPanel textStack = new StackPanel();
                    textStack.VerticalAlignment = VerticalAlignment.Center;

                    StackPanel nameRow = new StackPanel();
                    nameRow.Orientation = Orientation.Horizontal;

                    Border dbmsBadge = new Border();
                    dbmsBadge.Background = UiTheme.BrushFromHex("#0F172A");
                    dbmsBadge.BorderBrush = UiTheme.BorderSubtle;
                    dbmsBadge.BorderThickness = new Thickness(1);
                    dbmsBadge.CornerRadius = new CornerRadius(4);
                    dbmsBadge.Padding = new Thickness(6, 1, 6, 1);
                    dbmsBadge.Margin = new Thickness(0, 0, 8, 0);

                    TextBlock dbmsTxt = new TextBlock();
                    dbmsTxt.Text = capturedEnv.Dbms ?? "DBMS";
                    dbmsTxt.FontSize = 10;
                    dbmsTxt.FontWeight = FontWeights.Bold;
                    dbmsTxt.Foreground = UiTheme.AccentLightBlue;
                    dbmsBadge.Child = dbmsTxt;
                    nameRow.Children.Add(dbmsBadge);

                    TextBlock nameTxt = new TextBlock();
                    nameTxt.Text = capturedEnv.Name;
                    nameTxt.FontSize = 13;
                    nameTxt.FontWeight = FontWeights.SemiBold;
                    nameTxt.Foreground = UiTheme.TextPrimary;
                    nameRow.Children.Add(nameTxt);

                    textStack.Children.Add(nameRow);

                    TextBlock detailsTxt = new TextBlock();
                    detailsTxt.Text = string.Format("Usuário: {0} • TableSpace: {1} • Status: {2}",
                        !string.IsNullOrEmpty(capturedEnv.User) ? capturedEnv.User : "(vazio)",
                        !string.IsNullOrEmpty(capturedEnv.TableSpace) ? capturedEnv.TableSpace : "(padrão)",
                        capturedEnv.IsDisabled ? "Desabilitado" : "Ativo");
                    detailsTxt.FontSize = 11;
                    detailsTxt.Foreground = UiTheme.TextMuted;
                    detailsTxt.Margin = new Thickness(0, 4, 0, 0);
                    textStack.Children.Add(detailsTxt);

                    dp.Children.Add(textStack);
                    itemBox.Child = dp;
                    listStack.Children.Add(itemBox);
                }
            }

            scroll.Content = listStack;
            Grid.SetRow(scroll, 1);
            g.Children.Add(scroll);

            DockPanel bottomDock = new DockPanel();
            bottomDock.Margin = new Thickness(0, 16, 0, 0);

            Button btnOpenIni = UiTheme.CreateActionButton("📝 Abrir dbaccess.ini Completo", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
            btnOpenIni.Height = 32;
            btnOpenIni.Padding = new Thickness(12, 0, 12, 0);
            btnOpenIni.FontSize = 11;
            btnOpenIni.Click += delegate
            {
                if (File.Exists(_dbAccessConfig.IniPath))
                {
                    try { Process.Start(_dbAccessConfig.IniPath); } catch { }
                }
            };
            DockPanel.SetDock(btnOpenIni, Dock.Left);
            bottomDock.Children.Add(btnOpenIni);

            Button btnClose = UiTheme.CreateActionButton("Fechar", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
            btnClose.Height = 32;
            btnClose.Padding = new Thickness(18, 0, 18, 0);
            btnClose.HorizontalAlignment = HorizontalAlignment.Right;
            DockPanel.SetDock(btnClose, Dock.Right);
            btnClose.Click += delegate
            {
                dlg.Close();
                CarregarDadosDbAccessAsync();
            };
            bottomDock.Children.Add(btnClose);

            Grid.SetRow(bottomDock, 2);
            g.Children.Add(bottomDock);

            shell.Child = g;
            dlg.Content = shell;
            dlg.ShowDialog();
        }

        private void IniciarDbAccessAsync()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    if (_dbAccessConfig.IsService && !string.IsNullOrEmpty(_dbAccessConfig.ServiceName))
                    {
                        using (ServiceController sc = new ServiceController(_dbAccessConfig.ServiceName))
                        {
                            if (sc.Status != ServiceControllerStatus.Running && sc.Status != ServiceControllerStatus.StartPending)
                            {
                                sc.Start();
                                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(8));
                            }
                        }
                    }
                    else
                    {
                        string exe = Path.Combine(_dbAccessConfig.FolderPath, "dbaccess64.exe");
                        if (!File.Exists(exe)) exe = Path.Combine(_dbAccessConfig.FolderPath, "dbaccess.exe");
                        if (File.Exists(exe))
                        {
                            ProcessStartInfo psi = new ProcessStartInfo(exe);
                            psi.WorkingDirectory = _dbAccessConfig.FolderPath;
                            psi.UseShellExecute = true;
                            Process.Start(psi);
                        }
                    }
                    Thread.Sleep(1200);
                    CarregarDadosDbAccessAsync();
                }
                catch { }
            });
        }

        private void PararDbAccessAsync()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    if (_dbAccessConfig.IsService && !string.IsNullOrEmpty(_dbAccessConfig.ServiceName))
                    {
                        using (ServiceController sc = new ServiceController(_dbAccessConfig.ServiceName))
                        {
                            if (sc.Status != ServiceControllerStatus.Stopped && sc.Status != ServiceControllerStatus.StopPending)
                            {
                                sc.Stop();
                                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(8));
                            }
                        }
                    }

                    if (_dbAccessConfig.ProcessId > 0)
                    {
                        using (Process p = Process.GetProcessById(_dbAccessConfig.ProcessId))
                        {
                            if (!p.HasExited)
                            {
                                p.Kill();
                                p.WaitForExit(3000);
                            }
                        }
                    }
                    Thread.Sleep(800);
                    CarregarDadosDbAccessAsync();
                }
                catch { }
            });
        }

        private void ReiniciarDbAccessAsync()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    PararDbAccessAsync();
                    Thread.Sleep(1500);
                    IniciarDbAccessAsync();
                }
                catch { }
            });
        }

        private Border BuildDbAccessEnvironmentCard(DbAccessEnvironment env, bool isHidden)
        {
            Border card = new Border();
            card.Background = UiTheme.BgCard;
            card.BorderBrush = UiTheme.BorderSubtle;
            card.BorderThickness = new Thickness(1);
            card.CornerRadius = new CornerRadius(12);
            card.Padding = new Thickness(16, 14, 16, 14);

            card.HorizontalAlignment = HorizontalAlignment.Stretch;
            card.Margin = new Thickness(0, 0, 0, 10);

            env.CardBorder = card;

            Grid cardGrid = new Grid();
            RowDefinition rMain = new RowDefinition(); rMain.Height = GridLength.Auto;
            cardGrid.RowDefinitions.Add(rMain);

            ColumnDefinition colLeft = new ColumnDefinition(); colLeft.Width = GridLength.Auto;
            ColumnDefinition colInfo = new ColumnDefinition(); colInfo.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition colRight = new ColumnDefinition(); colRight.Width = GridLength.Auto;
            ColumnDefinition colDots = new ColumnDefinition(); colDots.Width = GridLength.Auto;
            cardGrid.ColumnDefinitions.Add(colLeft);
            cardGrid.ColumnDefinitions.Add(colInfo);
            cardGrid.ColumnDefinitions.Add(colRight);
            cardGrid.ColumnDefinitions.Add(colDots);

            // ================= COLUNA 0 (ESQUERDA, 96px): STATUS BADGE, DBMS BADGE, EDITAR/OCULTAR =================
            StackPanel leftToolsColumn = new StackPanel();
            leftToolsColumn.Orientation = Orientation.Vertical;
            leftToolsColumn.VerticalAlignment = VerticalAlignment.Center;
            leftToolsColumn.HorizontalAlignment = HorizontalAlignment.Center;
            leftToolsColumn.Width = 96;
            leftToolsColumn.Margin = new Thickness(0, 0, 16, 0);

            // 1. Status Badge (Habilitado / Desabilitado) 96x26
            Border statusBadge = new Border();
            statusBadge.Width = 96;
            statusBadge.Height = 26;
            statusBadge.CornerRadius = new CornerRadius(10);
            statusBadge.Padding = new Thickness(6, 3, 6, 3);
            statusBadge.HorizontalAlignment = HorizontalAlignment.Center;
            statusBadge.BorderThickness = new Thickness(1);
            statusBadge.Background = env.IsDisabled ? UiTheme.BrushFromHex("#334155") : UiTheme.AccentGreen;
            statusBadge.BorderBrush = env.IsDisabled ? UiTheme.BorderSubtle : UiTheme.AccentGreen;
            env.StatusBadge = statusBadge;

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
            statusDot.Background = env.IsDisabled ? UiTheme.BrushFromHex("#94A3B8") : Brushes.White;
            statusInner.Children.Add(statusDot);

            TextBlock statusTxt = new TextBlock();
            statusTxt.Text = env.IsDisabled ? "Desabilitado" : "Habilitado";
            statusTxt.FontSize = 10;
            statusTxt.FontWeight = FontWeights.Bold;
            statusTxt.Foreground = Brushes.White;
            statusTxt.VerticalAlignment = VerticalAlignment.Center;
            env.StatusText = statusTxt;
            statusInner.Children.Add(statusTxt);

            statusBadge.Child = statusInner;
            leftToolsColumn.Children.Add(statusBadge);

            // 2. DBMS Badge (MSSQL / Oracle / Postgres) 96x24
            Border dbmsBadge = new Border();
            dbmsBadge.Width = 96;
            dbmsBadge.Height = 24;
            dbmsBadge.CornerRadius = new CornerRadius(5);
            dbmsBadge.Margin = new Thickness(0, 4, 0, 0);
            dbmsBadge.HorizontalAlignment = HorizontalAlignment.Center;
            dbmsBadge.VerticalAlignment = VerticalAlignment.Center;

            if (string.Equals(env.Dbms, "MSSQL", StringComparison.OrdinalIgnoreCase))
                dbmsBadge.Background = UiTheme.BrushFromHex("#0369A1");
            else if (string.Equals(env.Dbms, "ORACLE", StringComparison.OrdinalIgnoreCase))
                dbmsBadge.Background = UiTheme.BrushFromHex("#C2410C");
            else if (string.Equals(env.Dbms, "POSTGRES", StringComparison.OrdinalIgnoreCase) || string.Equals(env.Dbms, "POSTGRESQL", StringComparison.OrdinalIgnoreCase))
                dbmsBadge.Background = UiTheme.BrushFromHex("#0F766E");
            else
                dbmsBadge.Background = UiTheme.BrushFromHex("#475569");

            TextBlock dbmsText = new TextBlock();
            dbmsText.Text = env.Dbms;
            dbmsText.FontSize = 11;
            dbmsText.FontWeight = FontWeights.Bold;
            dbmsText.Foreground = Brushes.White;
            dbmsText.TextAlignment = TextAlignment.Center;
            dbmsText.VerticalAlignment = VerticalAlignment.Center;
            dbmsBadge.Child = dbmsText;
            leftToolsColumn.Children.Add(dbmsBadge);

            // 3. Linha com Editar (Pencil) e Ocultar (Eye)
            StackPanel editHideRow = new StackPanel();
            editHideRow.Orientation = Orientation.Horizontal;
            editHideRow.HorizontalAlignment = HorizontalAlignment.Center;
            editHideRow.VerticalAlignment = VerticalAlignment.Center;
            editHideRow.Margin = new Thickness(0, 6, 0, 0);

            Button btnConfig = UiTheme.CreateActionButton(
                UiTheme.CreatePencilIcon(UiTheme.TextSecondary, 13),
                UiTheme.NeutralBtnHex,
                UiTheme.NeutralBtnHoverHex);
            btnConfig.Width = 34;
            btnConfig.Height = 26;
            btnConfig.Padding = new Thickness(0);
            btnConfig.Margin = new Thickness(0, 0, 4, 0);
            btnConfig.ToolTip = "Editar parâmetros desta seção no dbaccess.ini";
            btnConfig.Click += delegate { AbrirEditorSecaoAmbienteDbAccess(env); };

            Button btnHide = UiTheme.CreateActionButton(
                isHidden ? UiTheme.CreateEyeOffIcon(UiTheme.AccentAmber, 13) : UiTheme.CreateEyeIcon(UiTheme.TextSecondary, 13),
                UiTheme.NeutralBtnHex,
                UiTheme.NeutralBtnHoverHex);
            btnHide.Width = 34;
            btnHide.Height = 26;
            btnHide.Padding = new Thickness(0);
            btnHide.ToolTip = isHidden ? "Desocultar ambiente" : "Ocultar ambiente";
            btnHide.Click += delegate
            {
                string key = env.SectionName ?? env.Name;
                if (_hiddenDbAccessEnvs.Contains(key))
                    _hiddenDbAccessEnvs.Remove(key);
                else
                    _hiddenDbAccessEnvs.Add(key);

                SalvarOcultosDbAccessAppData();
                RenderizarCardsDbAccess();
            };

            editHideRow.Children.Add(btnConfig);
            editHideRow.Children.Add(btnHide);
            leftToolsColumn.Children.Add(editHideRow);

            Grid.SetRow(leftToolsColumn, 0);
            Grid.SetColumn(leftToolsColumn, 0);
            cardGrid.Children.Add(leftToolsColumn);

            // ================= COLUNA 1 (CENTRO): NOME, SEÇÃO E METADADOS =================
            StackPanel infoColumn = new StackPanel();
            infoColumn.VerticalAlignment = VerticalAlignment.Center;
            infoColumn.Margin = new Thickness(0, 0, 12, 0);

            // 1. Título do ambiente + ícone de cópia sem fundo
            WrapPanel titleWrap = new WrapPanel();
            titleWrap.VerticalAlignment = VerticalAlignment.Center;

            TextBlock lblEnvName = new TextBlock();
            lblEnvName.Text = env.Name;
            lblEnvName.Foreground = UiTheme.BrushFromHex(UiTheme.PrimaryBlueHex);
            lblEnvName.FontSize = 15;
            lblEnvName.FontWeight = FontWeights.Bold;
            lblEnvName.Margin = new Thickness(0, 0, 6, 0);
            lblEnvName.VerticalAlignment = VerticalAlignment.Center;
            titleWrap.Children.Add(lblEnvName);

            Button btnCopyName = UiTheme.CreateFramelessIconButton(
                UiTheme.CreateCopyIcon(UiTheme.TextMuted, 13),
                "Copiar configuração deste ambiente");
            btnCopyName.Padding = new Thickness(2);
            btnCopyName.Margin = new Thickness(0, 0, 8, 0);
            btnCopyName.VerticalAlignment = VerticalAlignment.Center;
            btnCopyName.Click += delegate { CopiarDadosAmbienteDbAccessParaClipboard(env); };
            titleWrap.Children.Add(btnCopyName);

            infoColumn.Children.Add(titleWrap);

            // 2. Linha com Nome da Seção no dbaccess.ini clicável + cópia sem fundo
            StackPanel sectionRow = new StackPanel();
            sectionRow.Orientation = Orientation.Horizontal;
            sectionRow.Margin = new Thickness(0, 3, 0, 6);

            string secLabel = "[" + (env.SectionName ?? (env.Dbms + "/" + env.Name)) + "]";
            TextBlock lblSection = new TextBlock();
            lblSection.Text = secLabel;
            lblSection.Foreground = UiTheme.TextMuted;
            lblSection.FontSize = 11;
            lblSection.Cursor = Cursors.Hand;
            lblSection.ToolTip = "Clique para editar os parâmetros desta seção no dbaccess.ini";
            lblSection.VerticalAlignment = VerticalAlignment.Center;
            lblSection.MouseEnter += delegate
            {
                lblSection.Foreground = UiTheme.AccentLightBlue;
                lblSection.TextDecorations = TextDecorations.Underline;
            };
            lblSection.MouseLeave += delegate
            {
                lblSection.Foreground = UiTheme.TextMuted;
                lblSection.TextDecorations = null;
            };
            lblSection.MouseLeftButtonUp += delegate
            {
                AbrirEditorSecaoAmbienteDbAccess(env);
            };
            sectionRow.Children.Add(lblSection);

            Button btnCopySec = UiTheme.CreateFramelessIconButton(
                UiTheme.CreateCopyIcon(UiTheme.TextMuted, 11),
                "Copiar nome da seção");
            btnCopySec.Padding = new Thickness(2);
            btnCopySec.Margin = new Thickness(6, 0, 0, 0);
            btnCopySec.VerticalAlignment = VerticalAlignment.Center;
            btnCopySec.Click += delegate
            {
                try { Clipboard.SetText(secLabel); } catch { }
            };
            sectionRow.Children.Add(btnCopySec);

            infoColumn.Children.Add(sectionRow);

            // 3. Metadados do ambiente
            WrapPanel metaWrap = new WrapPanel();
            metaWrap.Orientation = Orientation.Horizontal;

            metaWrap.Children.Add(CreateMetaPill("User", !string.IsNullOrEmpty(env.User) ? env.User : "—"));
            if (!string.IsNullOrEmpty(env.TableSpace))
                metaWrap.Children.Add(CreateMetaPill("TableSpace", env.TableSpace));
            if (!string.IsNullOrEmpty(env.IndexSpace))
                metaWrap.Children.Add(CreateMetaPill("IndexSpace", env.IndexSpace));
            if (!string.IsNullOrEmpty(env.Server))
                metaWrap.Children.Add(CreateMetaPill("Server", env.Server));
            if (!string.IsNullOrEmpty(env.Database))
                metaWrap.Children.Add(CreateMetaPill("DB", env.Database));
            if (!string.IsNullOrEmpty(env.Port))
                metaWrap.Children.Add(CreateMetaPill("Port", env.Port));

            infoColumn.Children.Add(metaWrap);

            Grid.SetRow(infoColumn, 0);
            Grid.SetColumn(infoColumn, 1);
            cardGrid.Children.Add(infoColumn);

            // ================= COLUNA 2 (DIREITA): BOTAO ATIVAR (VERDE) / DESATIVAR (VERMELHO COM X) =================
            StackPanel rightActionsColumn = new StackPanel();
            rightActionsColumn.Orientation = Orientation.Horizontal;
            rightActionsColumn.VerticalAlignment = VerticalAlignment.Center;
            rightActionsColumn.Margin = new Thickness(6, 0, 6, 0);

            if (!env.IsDisabled)
            {
                // ATIVO: Botão Desativar na cor vermelha do X (#DC2626)
                StackPanel btnContent = new StackPanel();
                btnContent.Orientation = Orientation.Horizontal;
                btnContent.VerticalAlignment = VerticalAlignment.Center;

                UIElement stopIcon = UiTheme.CreateStopXIcon(Brushes.White, 12);
                btnContent.Children.Add(stopIcon);

                TextBlock btnTxt = new TextBlock();
                btnTxt.Text = "Desativar";
                btnTxt.FontWeight = FontWeights.SemiBold;
                btnTxt.FontSize = 12;
                btnTxt.Margin = new Thickness(6, 0, 0, 0);
                btnContent.Children.Add(btnTxt);

                Button btnToggle = UiTheme.CreateActionButton(btnContent, "#DC2626", "#EF4444");
                btnToggle.Height = 32;
                btnToggle.Padding = new Thickness(14, 0, 14, 0);
                btnToggle.ToolTip = "Desabilitar ambiente no dbaccess.ini";
                btnToggle.Click += delegate { AlternarEstadoAmbienteDbAccess(env); };
                rightActionsColumn.Children.Add(btnToggle);
            }
            else
            {
                // INATIVO / DESABILITADO: Botão Ativar em verde
                StackPanel btnContent = new StackPanel();
                btnContent.Orientation = Orientation.Horizontal;
                btnContent.VerticalAlignment = VerticalAlignment.Center;

                UIElement playIcon = UiTheme.CreatePlayIcon(Brushes.White, 12);
                btnContent.Children.Add(playIcon);

                TextBlock btnTxt = new TextBlock();
                btnTxt.Text = "Ativar";
                btnTxt.FontWeight = FontWeights.SemiBold;
                btnTxt.FontSize = 12;
                btnTxt.Margin = new Thickness(6, 0, 0, 0);
                btnContent.Children.Add(btnTxt);

                Button btnToggle = UiTheme.CreateActionButton(btnContent, "#059669", "#10B981");
                btnToggle.Height = 32;
                btnToggle.Padding = new Thickness(14, 0, 14, 0);
                btnToggle.ToolTip = "Habilitar ambiente no dbaccess.ini";
                btnToggle.Click += delegate { AlternarEstadoAmbienteDbAccess(env); };
                rightActionsColumn.Children.Add(btnToggle);
            }

            Grid.SetRow(rightActionsColumn, 0);
            Grid.SetColumn(rightActionsColumn, 2);
            cardGrid.Children.Add(rightActionsColumn);

            // ================= COLUNA 3 (SUPERIOR DIREITO): BOTAO TRES PONTOS ('•••') COM POPUP =================
            Button btnDots = UiTheme.CreateFramelessIconButton(
                UiTheme.CreateThreeDotsIcon(UiTheme.TextSecondary, 16),
                "Mais opções");
            btnDots.VerticalAlignment = VerticalAlignment.Top;
            btnDots.HorizontalAlignment = HorizontalAlignment.Right;
            btnDots.Margin = new Thickness(4, -4, 0, 0);

            ContextMenu menuDots = new ContextMenu();
            menuDots.Background = UiTheme.BgHeader;
            menuDots.BorderBrush = UiTheme.BorderSubtle;
            menuDots.BorderThickness = new Thickness(1);
            menuDots.Foreground = UiTheme.TextPrimary;

            MenuItem miCopiar = new MenuItem();
            miCopiar.Header = "Copiar dados do ambiente";
            miCopiar.Icon = UiTheme.CreateCopyIcon(UiTheme.TextSecondary, 14);
            miCopiar.Foreground = UiTheme.TextPrimary;
            miCopiar.Click += delegate { CopiarDadosAmbienteDbAccessParaClipboard(env); };

            MenuItem miEditar = new MenuItem();
            miEditar.Header = "Editar no dbaccess.ini";
            miEditar.Icon = UiTheme.CreatePencilIcon(UiTheme.TextSecondary, 14);
            miEditar.Foreground = UiTheme.TextPrimary;
            miEditar.Click += delegate { AbrirEditorSecaoAmbienteDbAccess(env); };

            MenuItem miOcultar = new MenuItem();
            miOcultar.Header = isHidden ? "Desocultar ambiente" : "Ocultar ambiente";
            miOcultar.Icon = isHidden ? UiTheme.CreateEyeOffIcon(UiTheme.TextSecondary, 14) : UiTheme.CreateEyeIcon(UiTheme.TextSecondary, 14);
            miOcultar.Foreground = UiTheme.TextPrimary;
            miOcultar.Click += delegate
            {
                string key = env.SectionName ?? env.Name;
                if (_hiddenDbAccessEnvs.Contains(key))
                    _hiddenDbAccessEnvs.Remove(key);
                else
                    _hiddenDbAccessEnvs.Add(key);

                SalvarOcultosDbAccessAppData();
                RenderizarCardsDbAccess();
            };

            menuDots.Items.Add(miCopiar);
            menuDots.Items.Add(miEditar);
            menuDots.Items.Add(new Separator { Background = UiTheme.BorderSubtle, Margin = new Thickness(4, 2, 4, 2) });
            menuDots.Items.Add(miOcultar);

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

            card.Child = cardGrid;
            return card;
        }

        private void CopiarDadosAmbienteDbAccessParaClipboard(DbAccessEnvironment env)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[" + (env.SectionName ?? (env.Dbms + "/" + env.Name)) + "]");
                if (!string.IsNullOrEmpty(env.User)) sb.AppendLine("user=" + env.User);
                if (!string.IsNullOrEmpty(env.TableSpace)) sb.AppendLine("TableSpace=" + env.TableSpace);
                if (!string.IsNullOrEmpty(env.IndexSpace)) sb.AppendLine("IndexSpace=" + env.IndexSpace);
                sb.AppendLine("Disable=" + (env.IsDisabled ? "1" : "0"));
                if (!string.IsNullOrEmpty(env.Server)) sb.AppendLine("Server=" + env.Server);
                if (!string.IsNullOrEmpty(env.Port)) sb.AppendLine("Port=" + env.Port);
                if (!string.IsNullOrEmpty(env.Database)) sb.AppendLine("Database=" + env.Database);
                if (!string.IsNullOrEmpty(env.Alias)) sb.AppendLine("Alias=" + env.Alias);
                foreach (KeyValuePair<string, string> kv in env.AdditionalKeys)
                    sb.AppendLine(kv.Key + "=" + kv.Value);

                Clipboard.SetText(sb.ToString());
            }
            catch { }
        }

        private void AlternarEstadoAmbienteDbAccess(DbAccessEnvironment env)
        {
            if (string.IsNullOrEmpty(_dbAccessConfig.IniPath) || !File.Exists(_dbAccessConfig.IniPath))
                return;

            try
            {
                bool novoEstadoDisabled = !env.IsDisabled;
                string secTarget = env.SectionName ?? (env.Dbms + "/" + env.Name);

                List<IniSection> sections = ParseIniFile(_dbAccessConfig.IniPath);
                IniSection sec = sections.FirstOrDefault(s => string.Equals(s.Name, secTarget, StringComparison.OrdinalIgnoreCase));
                if (sec == null)
                    sec = sections.FirstOrDefault(s => string.Equals(s.Name, env.Name, StringComparison.OrdinalIgnoreCase));

                if (sec != null)
                {
                    IniEntry disEntry = sec.Entries.FirstOrDefault(e => string.Equals(e.Key, "Disable", StringComparison.OrdinalIgnoreCase));
                    if (disEntry != null)
                    {
                        disEntry.Value = novoEstadoDisabled ? "1" : "0";
                        disEntry.IsCommented = false;
                    }
                    else
                    {
                        IniEntry newEntry = new IniEntry();
                        newEntry.Key = "Disable";
                        newEntry.Value = novoEstadoDisabled ? "1" : "0";
                        sec.Entries.Add(newEntry);
                    }

                    SalvarArquivoIni(_dbAccessConfig.IniPath, sections);
                    env.IsDisabled = novoEstadoDisabled;

                    // Atualiza UI
                    if (env.StatusBadge != null && env.StatusText != null)
                    {
                        env.StatusBadge.Background = env.IsDisabled ? UiTheme.BrushFromHex("#475569") : UiTheme.AccentGreen;
                        env.StatusText.Text = env.IsDisabled ? "Desabilitado" : "Habilitado";
                    }

                    AtualizarBannerDbAccess();
                    RenderizarCardsDbAccess();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Falha ao atualizar dbaccess.ini: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =====================================================================
        // DIÁLOGO / EDITOR DE SEÇÃO DO AMBIENTE
        // =====================================================================
        private void AbrirEditorSecaoAmbienteDbAccess(DbAccessEnvironment env)
        {
            Window dlg = new Window();
            dlg.Title = "Editar Configuração - " + env.Name;
            dlg.Width = 520;
            dlg.Height = 480;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.Owner = this;
            dlg.Background = UiTheme.BgWindow;
            dlg.WindowStyle = WindowStyle.ToolWindow;

            Border shell = new Border();
            shell.Padding = new Thickness(20);
            shell.Background = UiTheme.BgWindow;

            Grid g = new Grid();
            RowDefinition rTitle = new RowDefinition(); rTitle.Height = GridLength.Auto;
            RowDefinition rFields = new RowDefinition(); rFields.Height = new GridLength(1, GridUnitType.Star);
            RowDefinition rBtns = new RowDefinition(); rBtns.Height = GridLength.Auto;
            g.RowDefinitions.Add(rTitle);
            g.RowDefinitions.Add(rFields);
            g.RowDefinitions.Add(rBtns);

            TextBlock title = new TextBlock();
            title.Text = "Configuração: [" + (env.SectionName ?? (env.Dbms + "/" + env.Name)) + "]";
            title.FontSize = 16;
            title.FontWeight = FontWeights.Bold;
            title.Foreground = UiTheme.TextPrimary;
            title.Margin = new Thickness(0, 0, 0, 16);
            Grid.SetRow(title, 0);
            g.Children.Add(title);

            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            StackPanel fieldsStack = new StackPanel();

            TextBox txtUser = CriarCampoEditor(fieldsStack, "Usuário (user):", env.User);
            TextBox txtPass = CriarCampoEditor(fieldsStack, "Senha (password):", env.Password);
            TextBox txtTableSpace = CriarCampoEditor(fieldsStack, "TableSpace:", env.TableSpace);
            TextBox txtIndexSpace = CriarCampoEditor(fieldsStack, "IndexSpace:", env.IndexSpace);

            CheckBox chkDisable = new CheckBox();
            chkDisable.Content = "Desabilitar ambiente (Disable=1)";
            chkDisable.IsChecked = env.IsDisabled;
            chkDisable.Foreground = UiTheme.TextPrimary;
            chkDisable.Margin = new Thickness(0, 10, 0, 14);
            fieldsStack.Children.Add(chkDisable);

            scroll.Content = fieldsStack;
            Grid.SetRow(scroll, 1);
            g.Children.Add(scroll);

            StackPanel btnStack = new StackPanel();
            btnStack.Orientation = Orientation.Horizontal;
            btnStack.HorizontalAlignment = HorizontalAlignment.Right;
            btnStack.Margin = new Thickness(0, 16, 0, 0);

            Button btnCancel = UiTheme.CreateActionButton("Cancelar", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
            btnCancel.Width = 90;
            btnCancel.Height = 34;
            btnCancel.Margin = new Thickness(0, 0, 10, 0);
            btnCancel.Click += delegate { dlg.Close(); };
            btnStack.Children.Add(btnCancel);

            Button btnSave = UiTheme.CreateActionButton("Salvar", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
            btnSave.Width = 90;
            btnSave.Height = 34;
            btnSave.FontWeight = FontWeights.SemiBold;
            btnSave.Click += delegate
            {
                try
                {
                    string secTarget = env.SectionName ?? (env.Dbms + "/" + env.Name);
                    List<IniSection> sections = ParseIniFile(_dbAccessConfig.IniPath);
                    IniSection sec = sections.FirstOrDefault(s => string.Equals(s.Name, secTarget, StringComparison.OrdinalIgnoreCase));
                    if (sec == null)
                        sec = sections.FirstOrDefault(s => string.Equals(s.Name, env.Name, StringComparison.OrdinalIgnoreCase));

                    if (sec != null)
                    {
                        AtualizarOuAdicionarChave(sec, "user", txtUser.Text);
                        AtualizarOuAdicionarChave(sec, "password", txtPass.Text);
                        AtualizarOuAdicionarChave(sec, "TableSpace", txtTableSpace.Text);
                        AtualizarOuAdicionarChave(sec, "IndexSpace", txtIndexSpace.Text);
                        AtualizarOuAdicionarChave(sec, "Disable", (chkDisable.IsChecked == true) ? "1" : "0");

                        SalvarArquivoIni(_dbAccessConfig.IniPath, sections);

                        env.User = txtUser.Text;
                        env.Password = txtPass.Text;
                        env.TableSpace = txtTableSpace.Text;
                        env.IndexSpace = txtIndexSpace.Text;
                        env.IsDisabled = (chkDisable.IsChecked == true);

                        dlg.Close();
                        RenderizarCardsDbAccess();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao salvar: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            btnStack.Children.Add(btnSave);

            Grid.SetRow(btnStack, 2);
            g.Children.Add(btnStack);

            shell.Child = g;
            dlg.Content = shell;
            dlg.ShowDialog();
        }

        private static TextBox CriarCampoEditor(StackPanel parent, string label, string value)
        {
            TextBlock lbl = new TextBlock();
            lbl.Text = label;
            lbl.FontSize = 12;
            lbl.FontWeight = FontWeights.SemiBold;
            lbl.Foreground = UiTheme.TextSecondary;
            lbl.Margin = new Thickness(0, 0, 0, 4);
            parent.Children.Add(lbl);

            TextBox txt = new TextBox();
            txt.Text = value ?? "";
            txt.Background = UiTheme.BgInput;
            txt.Foreground = UiTheme.TextPrimary;
            txt.CaretBrush = UiTheme.TextPrimary;
            txt.BorderBrush = UiTheme.BorderSubtle;
            txt.BorderThickness = new Thickness(1);
            txt.Padding = new Thickness(8, 6, 8, 6);
            txt.FontSize = 13;
            txt.Margin = new Thickness(0, 0, 0, 10);
            parent.Children.Add(txt);

            return txt;
        }

        private static void AtualizarOuAdicionarChave(IniSection sec, string key, string value)
        {
            IniEntry e = sec.Entries.FirstOrDefault(entry => string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase));
            if (e != null)
            {
                e.Value = value ?? "";
                e.IsCommented = false;
            }
            else
            {
                IniEntry newEntry = new IniEntry();
                newEntry.Key = key;
                newEntry.Value = value ?? "";
                sec.Entries.Add(newEntry);
            }
        }

        private void SalvarArquivoIni(string path, List<IniSection> sections)
        {
            StringBuilder sb = new StringBuilder();
            foreach (IniSection sec in sections)
            {
                if (string.IsNullOrEmpty(sec.Name)) continue;

                if (sec.IsCommented)
                    sb.AppendLine(";" + "[" + sec.Name + "]");
                else
                    sb.AppendLine("[" + sec.Name + "]");

                foreach (IniEntry e in sec.Entries)
                {
                    if (e.IsPureComment)
                    {
                        sb.AppendLine(";" + (e.Value ?? ""));
                    }
                    else
                    {
                        string line = string.Format("{0}={1}", e.Key, e.Value ?? "");
                        if (e.IsCommented)
                            sb.AppendLine(";" + line);
                        else
                            sb.AppendLine(line);
                    }
                }
                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString(), Encoding.Default);
        }

        // =====================================================================
        // TELA INTEIRA: EDITOR VISUAL COMPLETO DO DBACCESS.INI
        // =====================================================================
        private void ShowDbAccessIniEditorView()
        {
            if (string.IsNullOrEmpty(_dbAccessConfig.IniPath) || !File.Exists(_dbAccessConfig.IniPath))
            {
                MessageBox.Show("Arquivo dbaccess.ini não localizado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _isInDashboardView = false;
            List<IniSection> sections = ParseIniFile(_dbAccessConfig.IniPath);

            Grid root = new Grid();
            RowDefinition rTop = new RowDefinition(); rTop.Height = GridLength.Auto;
            RowDefinition rBody = new RowDefinition(); rBody.Height = new GridLength(1, GridUnitType.Star);
            RowDefinition rBottom = new RowDefinition(); rBottom.Height = GridLength.Auto;
            root.RowDefinitions.Add(rTop);
            root.RowDefinitions.Add(rBody);
            root.RowDefinitions.Add(rBottom);

            StackPanel topLeft = new StackPanel();
            topLeft.Orientation = Orientation.Horizontal;
            topLeft.VerticalAlignment = VerticalAlignment.Center;

            Button btnBackArrow = UiTheme.CreateFramelessIconButton(
                UiTheme.CreateLargeThinLeftArrowIcon(UiTheme.TextPrimary),
                "Voltar para o DBAccess");
            btnBackArrow.Padding = new Thickness(4, 2, 4, 2);
            btnBackArrow.Margin = new Thickness(0, 0, 10, 0);
            btnBackArrow.Click += delegate { ShowDbAccessView(); };
            topLeft.Children.Add(btnBackArrow);

            TextBlock title = new TextBlock();
            title.Text = "Editor do dbaccess.ini";
            title.FontSize = 16;
            title.FontWeight = FontWeights.Bold;
            title.Foreground = UiTheme.TextPrimary;
            title.VerticalAlignment = VerticalAlignment.Center;
            topLeft.Children.Add(title);

            Border topBar = BuildTopWindowBar(topLeft, null);
            Grid.SetRow(topBar, 0);
            root.Children.Add(topBar);

            // Container de seções
            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.Padding = new Thickness(20);

            StackPanel sectionsPanel = new StackPanel();

            foreach (IniSection sec in sections)
            {
                Border secCard = new Border();
                secCard.Background = UiTheme.BgCard;
                secCard.BorderBrush = UiTheme.BorderSubtle;
                secCard.BorderThickness = new Thickness(1);
                secCard.CornerRadius = new CornerRadius(8);
                secCard.Margin = new Thickness(0, 0, 0, 12);
                secCard.Padding = new Thickness(16);

                StackPanel secStack = new StackPanel();

                // Cabeçalho da seção
                DockPanel secHeader = new DockPanel();
                secHeader.Margin = new Thickness(0, 0, 0, 10);

                TextBlock secTitle = new TextBlock();
                secTitle.Text = "[" + sec.Name + "]";
                secTitle.FontSize = 14;
                secTitle.FontWeight = FontWeights.Bold;
                secTitle.Foreground = UiTheme.AccentLightBlue;
                secHeader.Children.Add(secTitle);
                secStack.Children.Add(secHeader);

                // Entradas da seção
                foreach (IniEntry entry in sec.Entries)
                {
                    if (entry.IsPureComment) continue;

                    Grid rowGrid = new Grid();
                    rowGrid.Margin = new Thickness(0, 0, 0, 6);
                    ColumnDefinition cK = new ColumnDefinition(); cK.Width = new GridLength(200);
                    ColumnDefinition cV = new ColumnDefinition(); cV.Width = new GridLength(1, GridUnitType.Star);
                    rowGrid.ColumnDefinitions.Add(cK);
                    rowGrid.ColumnDefinitions.Add(cV);

                    TextBlock kTxt = new TextBlock();
                    kTxt.Text = entry.Key;
                    kTxt.FontSize = 12;
                    kTxt.FontWeight = FontWeights.SemiBold;
                    kTxt.Foreground = UiTheme.TextSecondary;
                    kTxt.VerticalAlignment = VerticalAlignment.Center;
                    Grid.SetColumn(kTxt, 0);
                    rowGrid.Children.Add(kTxt);

                    TextBox vBox = new TextBox();
                    vBox.Text = entry.Value ?? "";
                    vBox.Background = UiTheme.BgInput;
                    vBox.Foreground = UiTheme.TextPrimary;
                    vBox.CaretBrush = UiTheme.TextPrimary;
                    vBox.BorderBrush = UiTheme.BorderSubtle;
                    vBox.BorderThickness = new Thickness(1);
                    vBox.Padding = new Thickness(6, 4, 6, 4);
                    vBox.FontSize = 12;
                    vBox.TextChanged += delegate { entry.Value = vBox.Text; };
                    Grid.SetColumn(vBox, 1);
                    rowGrid.Children.Add(vBox);

                    secStack.Children.Add(rowGrid);
                }

                secCard.Child = secStack;
                sectionsPanel.Children.Add(secCard);
            }

            scroll.Content = sectionsPanel;
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            // Barra inferior com Salvar e Cancelar
            Border bottomBar = new Border();
            bottomBar.Background = UiTheme.BgHeader;
            bottomBar.BorderBrush = UiTheme.BorderSubtle;
            bottomBar.BorderThickness = new Thickness(0, 1, 0, 0);
            bottomBar.Padding = new Thickness(20, 10, 20, 10);

            StackPanel bStack = new StackPanel();
            bStack.Orientation = Orientation.Horizontal;
            bStack.HorizontalAlignment = HorizontalAlignment.Right;

            Button btnCancelBottom = UiTheme.CreateActionButton("Cancelar", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
            btnCancelBottom.Height = 36;
            btnCancelBottom.Width = 100;
            btnCancelBottom.Margin = new Thickness(0, 0, 10, 0);
            btnCancelBottom.Click += delegate { ShowDbAccessView(); };
            bStack.Children.Add(btnCancelBottom);

            Button btnSaveBottom = UiTheme.CreateActionButton("💾 Salvar Alterações", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
            btnSaveBottom.Height = 36;
            btnSaveBottom.Padding = new Thickness(16, 0, 16, 0);
            btnSaveBottom.FontWeight = FontWeights.SemiBold;
            btnSaveBottom.Click += delegate
            {
                try
                {
                    SalvarArquivoIni(_dbAccessConfig.IniPath, sections);
                    MessageBox.Show("dbaccess.ini salvo com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    ShowDbAccessView();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao salvar dbaccess.ini: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            bStack.Children.Add(btnSaveBottom);

            bottomBar.Child = bStack;
            Grid.SetRow(bottomBar, 2);
            root.Children.Add(bottomBar);

            _pageHost.Content = root;
        }
    }
}
