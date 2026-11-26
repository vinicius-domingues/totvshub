using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        private string _rootPath = @"C:\bases";
        private bool _needsOnboarding = false;
        private int _pollIntervalSeconds = 60;
        private int _scanBasesMinutes = 60;
        private int _browserDelaySeconds = 4;
        private bool _ignoreOldFolders = true;
        private bool _showProcessUser = true;
        private bool _showIniComments = false; // Padrao desmarcado
        private bool _ignoreSameSubfolderConflicts = true; // Desconsiderar duplicidade entre ambientes da mesma subpasta (padrao ativado)
        private string _updateDriveUrl = UiTheme.DefaultUpdateDriveUrl;

        private readonly string _appDataDir;
        private readonly string _cachedSvgPath;
        private readonly string _configPath;
        private readonly string _hiddenFilePath;
        private readonly string _notesFilePath;
        private readonly string _startupPrefPath;
        private readonly string _themePrefPath;
        private readonly string _timersPrefPath;
        private readonly string _ignoreOldPrefPath;
        private readonly string _showUserPrefPath;
        private readonly string _showCommentsPrefPath;
        private readonly string _ignoreSameSubfolderPrefPath;
        private readonly string _rulesPrefPath;
        private readonly string _customLinksPrefPath;
        private readonly string _driveUrlPrefPath;

        private HashSet<string> _hiddenDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _cardNotes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private List<ConflictRule> _conflictRules = new List<ConflictRule>();
        private List<CustomLinkButton> _customLinks = new List<CustomLinkButton>();

        // Secoes protegidas que nao podem ser excluidas nem ter o nome editado
        private static readonly HashSet<string> ProtectedIniSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "environment", "topconnect", "drivers", "tcp", "general", "webapp"
        };

        // Tags que recebem icone de pasta para abrir o seletor pre-posicionado
        private static readonly HashSet<string> PathTagKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ConsoleFile", "StartPath", "RootPath", "RPOCustom", "SourcePath"
        };

        private bool _showingHidden = false;
        private bool _isPollingProcesses = false;
        private bool _isLoadingBases = false;
        private bool _isInitialScanCompleted = false;
        private bool _isRealExit = false;
        private bool _isInDashboardView = true;
        private DateTime _lastUpdatedTime = DateTime.Now;

        // Estado para arraste fluido em tela cheia entre monitores
        private bool _isDraggingFromMaximized = false;
        private Point _maxDragStartPoint;
        private double _maxDragRatioX = 0.5;

        private List<AmbienteInfo> _ambientes = new List<AmbienteInfo>();
        private List<AlertaConflito> _alertasAtivos = new List<AlertaConflito>();

        private Border _mainShellBorder;
        private ContentControl _pageHost;
        private ScrollViewer _cardsScrollViewer;
        private Panel _cardsContainer;
        private TextBox _searchBox;
        private TextBlock _placeholderText;
        private TextBlock _rootPathLabel;
        private TextBlock _lastUpdateLabel;
        private TextBlock _statsLabel;
        private Button _btnToggleHiddenView;
        private bool _isCardsViewMode = false;
        private Border _alertBadge;
        private TextBlock _alertCountText;
        private System.Windows.Shapes.Path _refreshIconPath;

        private Grid _loadingTrackGrid;
        private Border _loadingBeam;
        private TranslateTransform _loadingBeamTransform;

        private DispatcherTimer _monitorTimer;
        private DispatcherTimer _basesRefreshTimer;

        private System.Windows.Forms.NotifyIcon _trayIcon;
        private System.Windows.Forms.ContextMenuStrip _trayMenu;
        private EventWaitHandle _overwriteSignal;

        public MainWindow()
        {
            _appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TOTVSHub");

            if (!Directory.Exists(_appDataDir))
                Directory.CreateDirectory(_appDataDir);

            // Carrega/copia a logo oficial totvs_white.svg para AppData automaticamente
            _cachedSvgPath = TotvsLogo.EnsureTotvsWhiteSvgInAppData(_appDataDir);

            _configPath = Path.Combine(_appDataDir, "config.txt");
            _hiddenFilePath = Path.Combine(_appDataDir, "ocultos.txt");
            _notesFilePath = Path.Combine(_appDataDir, "notas.txt");
            _startupPrefPath = Path.Combine(_appDataDir, "startup.txt");
            _themePrefPath = Path.Combine(_appDataDir, "theme.txt");
            _timersPrefPath = Path.Combine(_appDataDir, "timers.txt");
            _ignoreOldPrefPath = Path.Combine(_appDataDir, "ignore_old.txt");
            _showUserPrefPath = Path.Combine(_appDataDir, "show_pid_user.txt");
            _showCommentsPrefPath = Path.Combine(_appDataDir, "show_ini_comments.txt");
            _ignoreSameSubfolderPrefPath = Path.Combine(_appDataDir, "ignore_same_subfolder.txt");
            _rulesPrefPath = Path.Combine(_appDataDir, "alert_rules.txt");
            _customLinksPrefPath = Path.Combine(_appDataDir, "custom_links.txt");
            _driveUrlPrefPath = Path.Combine(_appDataDir, "drive_url.txt");

            IniciarListenerSobrescrita();
            CarregarPreferenciasAppData();
            ConfigurarInicializacaoWindowsAutomatica();

            Title = "TOTVSHub";
            Icon = TotvsLogo.CreateTotvsWindowImageSource(_cachedSvgPath);
            Width = 1120;
            Height = 700;
            MinWidth = 920;
            MinHeight = 540;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            Background = UiTheme.BgWindow;
            FontFamily = new FontFamily("Segoe UI");

            UiTheme.ApplyWindowChromeState(this, false);

            StateChanged += delegate
            {
                bool isMax = (WindowState == WindowState.Maximized);
                UiTheme.ApplyWindowChromeState(this, isMax);
                if (_mainShellBorder != null)
                {
                    _mainShellBorder.CornerRadius = isMax ? new CornerRadius(0) : new CornerRadius(12);
                    _mainShellBorder.BorderThickness = isMax ? new Thickness(0) : new Thickness(1);
                }
                if (_isInDashboardView && !_needsOnboarding)
                {
                    RenderizarCards();
                }
            };

            SizeChanged += delegate
            {
                if (_isInDashboardView && !_needsOnboarding && WindowState == WindowState.Maximized)
                {
                    AtualizarColunasGridMaximizado();
                }
            };

            InicializarBandejaWindows();
            BuildShellAndDashboard();

            _monitorTimer = new DispatcherTimer();
            _monitorTimer.Interval = TimeSpan.FromSeconds(_pollIntervalSeconds);
            _monitorTimer.Tick += delegate
            {
                if (!_needsOnboarding)
                    DispararAtualizacaoProcessosAsync();
            };
            _monitorTimer.Start();

            _basesRefreshTimer = new DispatcherTimer();
            _basesRefreshTimer.Interval = TimeSpan.FromMinutes(_scanBasesMinutes);
            _basesRefreshTimer.Tick += delegate
            {
                if (!_needsOnboarding)
                    CarregarAmbientesAsync(true);
            };
            _basesRefreshTimer.Start();

            Closing += delegate(object s, System.ComponentModel.CancelEventArgs e)
            {
                if (!_isRealExit)
                {
                    e.Cancel = true;
                    Hide();
                }
            };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            HwndSource source = HwndSource.FromHwnd(hwnd);
            if (source != null)
            {
                source.AddHook(NativeWin32.MultiMonitorWndProc);
            }
        }

        public string ObterCaminhoSubpastaBase(AmbienteInfo amb)
        {
            if (amb == null || string.IsNullOrWhiteSpace(amb.AppDir)) return "";
            try
            {
                string normAppDir = NormalizePath(amb.AppDir);
                string normRoot = NormalizePath(_rootPath);

                if (!string.IsNullOrEmpty(normRoot) && normAppDir.StartsWith(normRoot, StringComparison.OrdinalIgnoreCase))
                {
                    string rel = normAppDir.Substring(normRoot.Length).TrimStart('\\', '/');
                    int firstSep = rel.IndexOfAny(new char[] { '\\', '/' });
                    if (firstSep > 0)
                    {
                        string subName = rel.Substring(0, firstSep);
                        return Path.Combine(normRoot, subName);
                    }
                    return Path.Combine(normRoot, rel);
                }

                // Se normAppDir nao comeca com _rootPath, sobe ate a pasta do ambiente (1 nivel abaixo da pasta raiz pai)
                DirectoryInfo di = new DirectoryInfo(amb.AppDir);
                while (di != null && di.Parent != null && di.Parent.Parent != null && di.Parent.Parent.Parent != null)
                {
                    di = di.Parent;
                }
                if (di != null)
                    return di.FullName;

                return amb.PastaBase ?? amb.AppDir;
            }
            catch
            {
                return amb.PastaBase ?? amb.AppDir;
            }
        }

        private List<ConflictRule> ObterRegrasPadrao()
        {
            return new List<ConflictRule>
            {
                new ConflictRule("HTTPREST", "Port"),
                new ConflictRule("WEBAPP", "Port"),
                new ConflictRule("<ambiente>", "SpecialKey"),
                new ConflictRule("<ambiente>", "DBAlias"),
                new ConflictRule("<ambiente>", "")
            };
        }

        private void SalvarRegrasAppData()
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (ConflictRule r in _conflictRules)
                {
                    if (!string.IsNullOrWhiteSpace(r.SectionPattern))
                        lines.Add(string.Format("{0}|{1}", r.SectionPattern.Trim(), (r.KeyName ?? "").Trim()));
                }
                File.WriteAllLines(_rulesPrefPath, lines.ToArray(), Encoding.UTF8);
            }
            catch { }
        }

        private void SalvarCustomLinksAppData()
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (CustomLinkButton btn in _customLinks)
                {
                    if (!btn.IsBuiltIn && !string.IsNullOrWhiteSpace(btn.Label) && !string.IsNullOrWhiteSpace(btn.Formula))
                        lines.Add(string.Format("{0}|{1}", btn.Label.Trim(), btn.Formula.Trim()));
                }
                File.WriteAllLines(_customLinksPrefPath, lines.ToArray(), Encoding.UTF8);
            }
            catch { }
        }

        private void IniciarListenerSobrescrita()
        {
            try
            {
                _overwriteSignal = new EventWaitHandle(false, EventResetMode.ManualReset, "Global\\TOTVSHub_Overwrite_Signal_2026");
                ThreadPool.QueueUserWorkItem(delegate
                {
                    _overwriteSignal.WaitOne();
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        _isRealExit = true;
                        if (_trayIcon != null)
                        {
                            _trayIcon.Visible = false;
                            _trayIcon.Dispose();
                        }
                        Environment.Exit(0);
                    }));
                });
            }
            catch { }
        }

        private void ConfigurarInicializacaoWindowsAutomatica()
        {
            bool enabled = true;
            if (File.Exists(_startupPrefPath))
            {
                string val = File.ReadAllText(_startupPrefPath, Encoding.UTF8).Trim();
                enabled = !string.Equals(val, "0", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                File.WriteAllText(_startupPrefPath, "1", Encoding.UTF8);
            }

            AplicarRegistroWindowsStartup(enabled);
        }

        private void AplicarRegistroWindowsStartup(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key == null) return;
                    if (enable)
                    {
                        string exePath = Process.GetCurrentProcess().MainModule.FileName;
                        key.SetValue("TOTVSHub", "\"" + exePath + "\"");
                    }
                    else
                    {
                        if (key.GetValue("TOTVSHub") != null)
                            key.DeleteValue("TOTVSHub", false);
                    }
                }
            }
            catch { }
        }

        private bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key != null && key.GetValue("TOTVSHub") != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private void CarregarPreferenciasAppData()
        {
            if (File.Exists(_themePrefPath))
            {
                string themeVal = File.ReadAllText(_themePrefPath, Encoding.UTF8).Trim();
                bool isDark = !string.Equals(themeVal, "light", StringComparison.OrdinalIgnoreCase);
                UiTheme.ApplyTheme(isDark);
            }

            if (File.Exists(_timersPrefPath))
            {
                string[] parts = File.ReadAllText(_timersPrefPath, Encoding.UTF8).Trim().Split(';');
                int v1, v2, v3;
                if (parts.Length >= 1 && int.TryParse(parts[0], out v1) && v1 >= 5) _pollIntervalSeconds = v1;
                if (parts.Length >= 2 && int.TryParse(parts[1], out v2) && v2 >= 1) _scanBasesMinutes = v2;
                if (parts.Length >= 3 && int.TryParse(parts[2], out v3) && v3 >= 0) _browserDelaySeconds = v3;
            }

            if (File.Exists(_ignoreOldPrefPath))
            {
                string oldVal = File.ReadAllText(_ignoreOldPrefPath, Encoding.UTF8).Trim();
                _ignoreOldFolders = !string.Equals(oldVal, "0", StringComparison.OrdinalIgnoreCase);
            }

            if (File.Exists(_showUserPrefPath))
            {
                string uVal = File.ReadAllText(_showUserPrefPath, Encoding.UTF8).Trim();
                _showProcessUser = !string.Equals(uVal, "0", StringComparison.OrdinalIgnoreCase);
            }

            if (File.Exists(_showCommentsPrefPath))
            {
                string cVal = File.ReadAllText(_showCommentsPrefPath, Encoding.UTF8).Trim();
                _showIniComments = string.Equals(cVal, "1", StringComparison.OrdinalIgnoreCase);
            }

            if (File.Exists(_ignoreSameSubfolderPrefPath))
            {
                string sVal = File.ReadAllText(_ignoreSameSubfolderPrefPath, Encoding.UTF8).Trim();
                _ignoreSameSubfolderConflicts = !string.Equals(sVal, "0", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                _ignoreSameSubfolderConflicts = true;
                try { File.WriteAllText(_ignoreSameSubfolderPrefPath, "1", Encoding.UTF8); } catch { }
            }

            if (File.Exists(_driveUrlPrefPath))
            {
                string urlVal = File.ReadAllText(_driveUrlPrefPath, Encoding.UTF8).Trim();
                if (!string.IsNullOrEmpty(urlVal))
                    _updateDriveUrl = urlVal;
            }

            bool hasValidSavedConfig = false;
            if (File.Exists(_configPath))
            {
                string saved = File.ReadAllText(_configPath, Encoding.UTF8).Trim();
                if (!string.IsNullOrEmpty(saved) && Directory.Exists(saved))
                {
                    _rootPath = saved;
                    hasValidSavedConfig = true;
                }
            }

            if (hasValidSavedConfig)
            {
                _needsOnboarding = false;
            }
            else if (Directory.Exists(@"C:\bases"))
            {
                _rootPath = @"C:\bases";
                _needsOnboarding = false;
            }
            else
            {
                _rootPath = @"C:\bases";
                _needsOnboarding = true;
            }

            _hiddenDirs.Clear();
            if (File.Exists(_hiddenFilePath))
            {
                foreach (string line in File.ReadAllLines(_hiddenFilePath, Encoding.UTF8))
                {
                    string clean = line.Trim();
                    if (!string.IsNullOrEmpty(clean))
                        _hiddenDirs.Add(clean);
                }
            }

            _cardNotes.Clear();
            if (File.Exists(_notesFilePath))
            {
                foreach (string line in File.ReadAllLines(_notesFilePath, Encoding.UTF8))
                {
                    int sep = line.IndexOf('|');
                    if (sep > 0)
                    {
                        string dir = line.Substring(0, sep).Trim();
                        string note = line.Substring(sep + 1).Replace("\\n", "\n").Trim();
                        if (!string.IsNullOrEmpty(dir) && !string.IsNullOrEmpty(note))
                            _cardNotes[dir] = note;
                    }
                }
            }

            _conflictRules.Clear();
            if (File.Exists(_rulesPrefPath))
            {
                foreach (string line in File.ReadAllLines(_rulesPrefPath, Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    int sep = line.IndexOf('|');
                    if (sep >= 0)
                    {
                        string sec = line.Substring(0, sep).Trim();
                        string key = line.Substring(sep + 1).Trim();
                        if (!string.IsNullOrEmpty(sec))
                            _conflictRules.Add(new ConflictRule(sec, key));
                    }
                    else
                    {
                        _conflictRules.Add(new ConflictRule(line.Trim(), ""));
                    }
                }
            }
            else
            {
                _conflictRules = ObterRegrasPadrao();
                SalvarRegrasAppData();
            }

            _customLinks.Clear();
            if (File.Exists(_customLinksPrefPath))
            {
                foreach (string line in File.ReadAllLines(_customLinksPrefPath, Encoding.UTF8))
                {
                    int sep = line.IndexOf('|');
                    if (sep > 0)
                    {
                        string lbl = line.Substring(0, sep).Trim();
                        string form = line.Substring(sep + 1).Trim();
                        if (!string.IsNullOrEmpty(lbl) && !string.IsNullOrEmpty(form))
                            _customLinks.Add(new CustomLinkButton(lbl, form, false));
                    }
                }
            }
        }

        private void SalvarOcultosAppData()
        {
            try { File.WriteAllLines(_hiddenFilePath, _hiddenDirs.ToArray(), Encoding.UTF8); }
            catch { }
        }

        private void SalvarNotasAppData()
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (KeyValuePair<string, string> kv in _cardNotes)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Value))
                    {
                        string safeNote = kv.Value.Replace("\r", "").Replace("\n", "\\n").Trim();
                        lines.Add(kv.Key + "|" + safeNote);
                    }
                }
                File.WriteAllLines(_notesFilePath, lines.ToArray(), Encoding.UTF8);
            }
            catch { }
        }

        private StackPanel CreateBrandTitleHeader()
        {
            StackPanel titleLeft = new StackPanel();
            titleLeft.Orientation = Orientation.Horizontal;
            titleLeft.VerticalAlignment = VerticalAlignment.Center;
            titleLeft.Cursor = Cursors.Hand;
            titleLeft.ToolTip = "Monitoramento (Home)";
            titleLeft.MouseLeftButtonUp += delegate { SwitchToTab(HubActiveTab.HomeOverview); };

            titleLeft.Children.Add(TotvsLogo.CreateTotvsWhiteSvgLogo(_cachedSvgPath, 24));

            TextBlock totvsTxt = new TextBlock();
            totvsTxt.Text = "TOTVS";
            totvsTxt.Foreground = UiTheme.TextPrimary;
            totvsTxt.FontSize = 17;
            totvsTxt.FontWeight = FontWeights.Bold;
            totvsTxt.Margin = new Thickness(9, 0, 0, 0);
            totvsTxt.VerticalAlignment = VerticalAlignment.Center;
            titleLeft.Children.Add(totvsTxt);

            TextBlock appTitle = new TextBlock();
            appTitle.Text = "Hub";
            appTitle.Foreground = UiTheme.AccentLightBlue;
            appTitle.FontSize = 17;
            appTitle.FontWeight = FontWeights.Bold;
            appTitle.Margin = new Thickness(1, 0, 0, 0);
            appTitle.VerticalAlignment = VerticalAlignment.Center;
            titleLeft.Children.Add(appTitle);

            return titleLeft;
        }

        private void BuildShellAndDashboard()
        {
            Background = UiTheme.BgWindow;

            bool isMax = (WindowState == WindowState.Maximized);
            Border mainShell = new Border();
            mainShell.Background = UiTheme.BgWindow;
            mainShell.BorderBrush = UiTheme.BorderSubtle;
            mainShell.BorderThickness = isMax ? new Thickness(0) : new Thickness(1);
            mainShell.CornerRadius = isMax ? new CornerRadius(0) : new CornerRadius(12);
            _mainShellBorder = mainShell;

            _pageHost = new ContentControl();
            mainShell.Child = _pageHost;
            Content = mainShell;

            if (_needsOnboarding)
            {
                ShowOnboardingView();
            }
            else
            {
                ShowDashboardView();
                CarregarAmbientesAsync(false);
            }
        }

        private StackPanel CreateStandardTopRightIcons()
        {
            StackPanel topIcons = new StackPanel();
            topIcons.Orientation = Orientation.Horizontal;
            topIcons.VerticalAlignment = VerticalAlignment.Center;
            topIcons.Margin = new Thickness(0, 0, 8, 0);

            Grid bellGrid = new Grid();
            bellGrid.Children.Add(UiTheme.CreateThinBellIcon(UiTheme.TextPrimary));

            _alertBadge = new Border();
            _alertBadge.Background = _alertasAtivos.Count > 0 ? UiTheme.AccentRed : UiTheme.BrushFromHex("#475569");
            _alertBadge.CornerRadius = new CornerRadius(7);
            _alertBadge.Padding = new Thickness(4, 0, 4, 0);
            _alertBadge.HorizontalAlignment = HorizontalAlignment.Right;
            _alertBadge.VerticalAlignment = VerticalAlignment.Top;
            _alertBadge.Margin = new Thickness(10, -4, -5, 0);

            _alertCountText = new TextBlock();
            _alertCountText.Text = _alertasAtivos.Count.ToString();
            _alertCountText.Foreground = Brushes.White;
            _alertCountText.FontSize = 9;
            _alertCountText.FontWeight = FontWeights.Bold;
            _alertBadge.Child = _alertCountText;
            bellGrid.Children.Add(_alertBadge);

            Button btnAlerts = UiTheme.CreateFramelessIconButton(bellGrid, "Alertas");
            btnAlerts.Margin = new Thickness(0, 0, 10, 0);
            btnAlerts.Click += delegate { AbrirCentralAlertas(); };
            topIcons.Children.Add(btnAlerts);

            Button btnSettings = UiTheme.CreateFramelessIconButton(
                UiTheme.CreateHamburgerIcon(UiTheme.TextPrimary),
                "Configurações");
            btnSettings.Click += delegate { ShowSettingsView(); };
            topIcons.Children.Add(btnSettings);

            return topIcons;
        }

        private Border BuildTopWindowBar(UIElement leftContent, UIElement rightExtraControls = null)
        {
            Border titleBar = new Border();
            titleBar.Background = UiTheme.BgHeader;
            titleBar.BorderBrush = UiTheme.BorderSubtle;
            titleBar.BorderThickness = new Thickness(0, 0, 0, 1);
            titleBar.Height = 52;
            titleBar.Padding = new Thickness(20, 0, 14, 0);

            titleBar.PreviewMouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                // Ignora cliques em botoes, caixas de texto e abas do topo
                DependencyObject dep = e.OriginalSource as DependencyObject;
                while (dep != null && dep != titleBar)
                {
                    if (dep is Button || dep is TextBox)
                        return;
                    FrameworkElement fe = dep as FrameworkElement;
                    if (fe != null && fe.Tag != null && fe.Tag.ToString().IndexOf("Tab", StringComparison.OrdinalIgnoreCase) >= 0)
                        return;
                    dep = VisualTreeHelper.GetParent(dep);
                }

                if (e.ClickCount == 2)
                {
                    WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
                    e.Handled = true;
                    return;
                }

                if (e.ClickCount != 1) return;

                if (WindowState == WindowState.Maximized)
                {
                    _isDraggingFromMaximized = true;
                    _maxDragStartPoint = e.GetPosition(this);
                    _maxDragRatioX = _maxDragStartPoint.X / Math.Max(1.0, ActualWidth);
                    titleBar.CaptureMouse();
                }
                else
                {
                    _isDraggingFromMaximized = false;
                    try { DragMove(); } catch { }
                }
            };

            titleBar.PreviewMouseMove += delegate(object s, MouseEventArgs e)
            {
                if (!_isDraggingFromMaximized || e.LeftButton != MouseButtonState.Pressed)
                    return;

                Point currentPt = e.GetPosition(this);
                if (Math.Abs(currentPt.X - _maxDragStartPoint.X) > 4 ||
                    Math.Abs(currentPt.Y - _maxDragStartPoint.Y) > 4)
                {
                    _isDraggingFromMaximized = false;
                    if (titleBar.IsMouseCaptured)
                        titleBar.ReleaseMouseCapture();

                    System.Drawing.Point cursor = System.Windows.Forms.Cursor.Position;
                    double dipX = cursor.X;
                    double dipY = cursor.Y;

                    PresentationSource source = PresentationSource.FromVisual(this);
                    if (source != null && source.CompositionTarget != null)
                    {
                        Matrix m = source.CompositionTarget.TransformFromDevice;
                        Point dipPt = m.Transform(new Point(cursor.X, cursor.Y));
                        dipX = dipPt.X;
                        dipY = dipPt.Y;
                    }

                    double targetW = RestoreBounds.Width > 100 ? RestoreBounds.Width : 1120;
                    WindowState = WindowState.Normal;
                    Left = dipX - (targetW * _maxDragRatioX);
                    Top = dipY - Math.Min(24.0, _maxDragStartPoint.Y);

                    try { DragMove(); } catch { }
                }
            };

            titleBar.PreviewMouseLeftButtonUp += delegate
            {
                _isDraggingFromMaximized = false;
                if (titleBar.IsMouseCaptured)
                    titleBar.ReleaseMouseCapture();
            };

            DockPanel titleDock = new DockPanel();
            titleDock.VerticalAlignment = VerticalAlignment.Center;

            StackPanel rightControls = new StackPanel();
            rightControls.Orientation = Orientation.Horizontal;
            rightControls.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(rightControls, Dock.Right);

            // Sempre garante o Sino + Sanduiche padronizados na mesma posicao em todas as telas
            StackPanel standardNavIcons = CreateStandardTopRightIcons();
            if (rightExtraControls != null)
            {
                Panel extraPanel = rightExtraControls as Panel;
                bool alreadyHasBellAndHamburger = (extraPanel != null && extraPanel.Children.Count == 2 &&
                    extraPanel.Children[0] is Button && extraPanel.Children[1] is Button);
                if (!alreadyHasBellAndHamburger)
                {
                    rightControls.Children.Add(rightExtraControls);
                }
            }
            rightControls.Children.Add(standardNavIcons);

            Button btnMin = UiTheme.CreateWindowButton("—", UiTheme.NeutralBtnHoverHex);
            btnMin.ToolTip = "Minimizar";
            btnMin.Click += delegate { WindowState = WindowState.Minimized; };

            // Botao Maximizar / Tirar da Tela Cheia (icone de aba dupla quando maximizado)
            Button btnMax = UiTheme.CreateWindowButton(
                UiTheme.CreateWindowStateGlyph(WindowState == WindowState.Maximized, UiTheme.TextSecondary),
                UiTheme.NeutralBtnHoverHex);
            btnMax.ToolTip = WindowState == WindowState.Maximized ? "Restaurar tamanho" : "Maximizar";
            btnMax.Click += delegate
            {
                WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
                btnMax.Content = UiTheme.CreateWindowStateGlyph(WindowState == WindowState.Maximized, UiTheme.TextSecondary);
                btnMax.ToolTip = WindowState == WindowState.Maximized ? "Restaurar tamanho" : "Maximizar";
            };

            StateChanged += delegate
            {
                btnMax.Content = UiTheme.CreateWindowStateGlyph(WindowState == WindowState.Maximized, UiTheme.TextSecondary);
                btnMax.ToolTip = WindowState == WindowState.Maximized ? "Restaurar tamanho" : "Maximizar";
            };

            Button btnClose = UiTheme.CreateWindowButton("✕", "#DC2626");
            btnClose.ToolTip = "Minimizar para a bandeja";
            btnClose.Click += delegate { Close(); };

            rightControls.Children.Add(btnMin);
            rightControls.Children.Add(btnMax);
            rightControls.Children.Add(btnClose);
            titleDock.Children.Add(rightControls);

            if (leftContent is FrameworkElement)
            {
                ((FrameworkElement)leftContent).VerticalAlignment = VerticalAlignment.Center;
            }

            if (_isInDashboardView)
            {
                _currentTab = HubActiveTab.Ambientes;
            }

            StackPanel sp = leftContent as StackPanel;
            if (sp != null && !HasBrowserTabs(sp))
            {
                // Limita largura de pilulas de caminho (ex: no IniEditor) para acomodar as abas
                foreach (UIElement child in sp.Children)
                {
                    Border b = child as Border;
                    if (b != null)
                    {
                        TextBlock tb = b.Child as TextBlock;
                        if (tb != null && tb.Text != null && (tb.Text.Contains("\\") || tb.Text.Contains("/")))
                        {
                            b.MaxWidth = 260;
                            tb.TextTrimming = TextTrimming.CharacterEllipsis;
                            tb.ToolTip = tb.Text;
                        }
                    }
                }

                Border separator = new Border();
                separator.Width = 1;
                separator.Height = 22;
                separator.Background = UiTheme.BorderSubtle;
                separator.Margin = new Thickness(18, 0, 16, 0);
                separator.VerticalAlignment = VerticalAlignment.Center;
                sp.Children.Add(separator);

                UIElement tabs = BuildBrowserTabBar(_currentTab);
                sp.Children.Add(tabs);
            }

            titleDock.Children.Add(leftContent);
            titleBar.Child = titleDock;
            return titleBar;
        }

        private bool HasBrowserTabs(UIElement element)
        {
            if (element == null) return false;
            FrameworkElement fe = element as FrameworkElement;
            if (fe != null && fe.Tag != null)
            {
                string tag = fe.Tag.ToString();
                if (string.Equals(tag, "NoTabs", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (tag.IndexOf("Tab", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            Panel panel = element as Panel;
            if (panel != null)
            {
                foreach (UIElement child in panel.Children)
                {
                    if (HasBrowserTabs(child)) return true;
                }
            }
            return false;
        }
    }
}
