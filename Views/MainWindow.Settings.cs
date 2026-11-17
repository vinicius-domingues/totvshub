using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        // =====================================================================
        // TELA INTEIRA: CONFIGURACOES (COM TODOS OS TOGGLES, LINKS E DOUBLE CHECK)
        // =====================================================================
        private void ShowSettingsView()
        {
            _isInDashboardView = false;

            bool tempDark = UiTheme.IsDark;
            bool tempAutoStart = IsStartupEnabled();
            bool tempIgnoreOld = _ignoreOldFolders;
            bool tempShowUser = _showProcessUser;
            bool tempShowComments = _showIniComments;

            List<ConflictRule> tempRules = _conflictRules
                .Select(r => new ConflictRule(r.SectionPattern, r.KeyName))
                .ToList();

            List<CustomLinkButton> tempLinks = _customLinks
                .Select(l => new CustomLinkButton(l.Label, l.Formula, false))
                .ToList();

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
                "Voltar");
            btnBackArrow.Margin = new Thickness(0, 0, 12, 0);
            btnBackArrow.Click += delegate
            {
                if (_needsOnboarding)
                    ShowOnboardingView();
                else
                    ShowDashboardView();
            };

            TextBlock titleLbl = new TextBlock();
            titleLbl.Text = "Configurações";
            titleLbl.Foreground = UiTheme.TextPrimary;
            titleLbl.FontSize = 16;
            titleLbl.FontWeight = FontWeights.Bold;
            titleLbl.VerticalAlignment = VerticalAlignment.Center;

            topLeft.Children.Add(btnBackArrow);
            topLeft.Children.Add(titleLbl);

            Border topBar = BuildTopWindowBar(topLeft, null);
            Grid.SetRow(topBar, 0);
            root.Children.Add(topBar);

            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.Padding = new Thickness(28, 18, 28, 18);

            StackPanel contentStack = new StackPanel();
            contentStack.MaxWidth = 860;

            // --- Secao 1: Aparencia e Sistema (Todos em Toggle) ---
            contentStack.Children.Add(CreateSettingsSectionHeader("Aparência e Comportamento"));

            Border cardGeneral = CreateSettingsCard();
            StackPanel genStack = new StackPanel();

            // Toggle: Tema da Interface (Escuro / Claro)
            DockPanel themeRow = new DockPanel();
            themeRow.Margin = new Thickness(0, 0, 0, 14);
            UIElement toggleTheme = UiTheme.CreateModernToggleSwitch(tempDark, "Escuro", "Claro", delegate(bool val)
            {
                tempDark = val;
            });
            DockPanel.SetDock(toggleTheme, Dock.Right);

            StackPanel themeLabels = new StackPanel();
            themeLabels.Children.Add(CreateSettingsTitle("Tema da Interface"));
            themeLabels.Children.Add(CreateSettingsDesc("Alterne entre o modo escuro e o modo claro"));
            themeRow.Children.Add(toggleTheme);
            themeRow.Children.Add(themeLabels);
            genStack.Children.Add(themeRow);

            // Toggle: Iniciar com o Windows (Recomendado)
            DockPanel startRow = new DockPanel();
            startRow.Margin = new Thickness(0, 0, 0, 14);
            UIElement toggleAutoStart = UiTheme.CreateModernToggleSwitch(tempAutoStart, "Sim", "Não", delegate(bool val)
            {
                tempAutoStart = val;
            });
            DockPanel.SetDock(toggleAutoStart, Dock.Right);

            StackPanel startLabels = new StackPanel();
            startLabels.Children.Add(CreateSettingsTitle("Iniciar com o Windows (Recomendado)"));
            startLabels.Children.Add(CreateSettingsDesc("Inicia o TOTVSHub automaticamente ao ligar o computador"));
            startRow.Children.Add(toggleAutoStart);
            startRow.Children.Add(startLabels);
            genStack.Children.Add(startRow);

            // Toggle: Ignorar pastas .old
            DockPanel ignoreOldRow = new DockPanel();
            ignoreOldRow.Margin = new Thickness(0, 0, 0, 14);
            UIElement toggleIgnoreOld = UiTheme.CreateModernToggleSwitch(tempIgnoreOld, "Sim", "Não", delegate(bool val)
            {
                tempIgnoreOld = val;
            });
            DockPanel.SetDock(toggleIgnoreOld, Dock.Right);

            StackPanel ignoreOldLabels = new StackPanel();
            ignoreOldLabels.Children.Add(CreateSettingsTitle("Ignorar pastas .old"));
            ignoreOldLabels.Children.Add(CreateSettingsDesc("Não carrega ambientes de diretórios terminados em .old (ex: appserver.old)"));
            ignoreOldRow.Children.Add(toggleIgnoreOld);
            ignoreOldRow.Children.Add(ignoreOldLabels);
            genStack.Children.Add(ignoreOldRow);

            // Toggle: Mostrar usuario que abriu o PID
            DockPanel showUserRow = new DockPanel();
            showUserRow.Margin = new Thickness(0, 0, 0, 14);
            UIElement toggleShowUser = UiTheme.CreateModernToggleSwitch(tempShowUser, "Sim", "Não", delegate(bool val)
            {
                tempShowUser = val;
            });
            DockPanel.SetDock(toggleShowUser, Dock.Right);

            StackPanel showUserLabels = new StackPanel();
            showUserLabels.Children.Add(CreateSettingsTitle("Mostrar usuário do processo (PID)"));
            showUserLabels.Children.Add(CreateSettingsDesc("Exibe nos cards o usuário do Windows que está executando o AppServer"));
            showUserRow.Children.Add(toggleShowUser);
            showUserRow.Children.Add(showUserLabels);
            genStack.Children.Add(showUserRow);

            // Toggle: Ver comentarios no .ini (padrao desmarcado)
            DockPanel showCommentsRow = new DockPanel();
            UIElement toggleShowComments = UiTheme.CreateModernToggleSwitch(tempShowComments, "Sim", "Não", delegate(bool val)
            {
                tempShowComments = val;
            });
            DockPanel.SetDock(toggleShowComments, Dock.Right);

            StackPanel showCommentsLabels = new StackPanel();
            showCommentsLabels.Children.Add(CreateSettingsTitle("Ver comentários"));
            showCommentsLabels.Children.Add(CreateSettingsDesc("Exibe linhas de comentários (;) dentro do editor do arquivo .ini"));
            showCommentsRow.Children.Add(toggleShowComments);
            showCommentsRow.Children.Add(showCommentsLabels);
            genStack.Children.Add(showCommentsRow);

            cardGeneral.Child = genStack;
            contentStack.Children.Add(cardGeneral);

            // --- Secao 2: Tempos de Monitoramento e Abertura ---
            contentStack.Children.Add(CreateSettingsSectionHeader("Tempos de Monitoramento e Abertura"));

            Border cardTimers = CreateSettingsCard();
            StackPanel timersStack = new StackPanel();

            TextBox txtPollSec = CreateNumericSettingBox(_pollIntervalSeconds.ToString());
            timersStack.Children.Add(CreateTimerRow(
                "Reconhecimento de processos/serviços ligados (segundos)",
                "Intervalo para checar se os AppServers estão ativos ou parados (padrão: 60s = 1 minuto)",
                txtPollSec,
                true));

            TextBox txtScanMin = CreateNumericSettingBox(_scanBasesMinutes.ToString());
            timersStack.Children.Add(CreateTimerRow(
                "Busca automática por novas bases (minutos)",
                "Intervalo para escanear o diretório raiz em busca de novos ambientes (padrão: 60 min = 1 hora)",
                txtScanMin,
                true));

            TextBox txtBrowserSec = CreateNumericSettingBox(_browserDelaySeconds.ToString());
            timersStack.Children.Add(CreateTimerRow(
                "Atraso para abrir aba no navegador (segundos)",
                "Tempo de espera após iniciar o AppServer antes de abrir o navegador (padrão: 4s)",
                txtBrowserSec,
                false));

            cardTimers.Child = timersStack;
            contentStack.Children.Add(cardTimers);

            // --- Secao 3: Botoes de Acesso Rapido (Formulas de Link ao lado de Protheus e REST) ---
            contentStack.Children.Add(CreateSettingsSectionHeader("Botões de Acesso Rápido (Links por Fórmula)"));

            Border cardLinks = CreateSettingsCard();
            StackPanel linksCardStack = new StackPanel();

            TextBlock linksHint = CreateSettingsDesc("Crie botões personalizados que aparecerão junto a Protheus e REST nos cards. Use <SECAO:CHAVE> ou {SECAO:CHAVE} para monitorar qualquer porta ou chave do .ini (ex: localhost:<HTTPREST:PORT>/portal).");
            linksHint.Margin = new Thickness(0, 0, 0, 12);
            linksCardStack.Children.Add(linksHint);

            // Exemplo fixo nao-editavel da formula do Protheus para referencia visual
            Border builtInRow = new Border();
            builtInRow.Background = UiTheme.BgInput;
            builtInRow.BorderBrush = UiTheme.BorderSubtle;
            builtInRow.BorderThickness = new Thickness(1);
            builtInRow.CornerRadius = new CornerRadius(7);
            builtInRow.Padding = new Thickness(12, 8, 12, 8);
            builtInRow.Margin = new Thickness(0, 0, 0, 8);

            DockPanel builtInDock = new DockPanel();
            Border lockTag = new Border();
            lockTag.Background = UiTheme.TagBg;
            lockTag.CornerRadius = new CornerRadius(5);
            lockTag.Padding = new Thickness(8, 3, 8, 3);
            DockPanel.SetDock(lockTag, Dock.Right);
            TextBlock lockTxt = new TextBlock();
            lockTxt.Text = "Fórmula Padrão (Somente leitura)";
            lockTxt.Foreground = UiTheme.TextMuted;
            lockTxt.FontSize = 10;
            lockTxt.FontWeight = FontWeights.Bold;
            lockTag.Child = lockTxt;

            StackPanel builtInFormulaStack = new StackPanel();
            builtInFormulaStack.Orientation = Orientation.Horizontal;
            builtInFormulaStack.VerticalAlignment = VerticalAlignment.Center;

            TextBlock bName = new TextBlock();
            bName.Text = "Protheus  →  ";
            bName.Foreground = UiTheme.TextPrimary;
            bName.FontWeight = FontWeights.Bold;
            bName.FontSize = 12;
            bName.VerticalAlignment = VerticalAlignment.Center;
            builtInFormulaStack.Children.Add(bName);
            builtInFormulaStack.Children.Add(RenderFormulaVisualPills("http://localhost:<WEBAPP:PORT>"));

            builtInDock.Children.Add(lockTag);
            builtInDock.Children.Add(builtInFormulaStack);
            builtInRow.Child = builtInDock;
            linksCardStack.Children.Add(builtInRow);

            // Lista editavel dos botoes criados pelo usuario
            StackPanel userLinksPanel = new StackPanel();
            linksCardStack.Children.Add(userLinksPanel);

            Action renderUserLinks = null;
            renderUserLinks = delegate
            {
                userLinksPanel.Children.Clear();
                foreach (CustomLinkButton link in tempLinks)
                {
                    CustomLinkButton capturedLink = link;
                    Border lRow = new Border();
                    lRow.Background = UiTheme.BgInput;
                    lRow.BorderBrush = UiTheme.BorderSubtle;
                    lRow.BorderThickness = new Thickness(1);
                    lRow.CornerRadius = new CornerRadius(7);
                    lRow.Padding = new Thickness(12, 7, 8, 7);
                    lRow.Margin = new Thickness(0, 0, 0, 6);

                    StackPanel lRowStack = new StackPanel();
                    DockPanel lDock = new DockPanel();

                    StackPanel lBtns = new StackPanel();
                    lBtns.Orientation = Orientation.Horizontal;
                    DockPanel.SetDock(lBtns, Dock.Right);

                    Button btnDelLink = UiTheme.CreateActionButton("✕", UiTheme.NeutralBtnHex, "#DC2626");
                    btnDelLink.Padding = new Thickness(8, 3, 8, 3);
                    btnDelLink.FontSize = 11;
                    btnDelLink.Click += delegate
                    {
                        tempLinks.Remove(capturedLink);
                        renderUserLinks();
                    };
                    lBtns.Children.Add(btnDelLink);

                    Grid editFieldsGrid = new Grid();
                    ColumnDefinition lc1 = new ColumnDefinition(); lc1.Width = new GridLength(140);
                    ColumnDefinition lc2 = new ColumnDefinition(); lc2.Width = new GridLength(1, GridUnitType.Star);
                    editFieldsGrid.ColumnDefinitions.Add(lc1);
                    editFieldsGrid.ColumnDefinitions.Add(lc2);

                    ContentControl formulaPreviewHost = new ContentControl();
                    formulaPreviewHost.Margin = new Thickness(2, 6, 0, 0);
                    formulaPreviewHost.Content = RenderFormulaVisualPills(capturedLink.Formula);

                    TextBox editLabelBox = CreateTextSettingBox("Nome do botão");
                    editLabelBox.Text = capturedLink.Label;
                    editLabelBox.Height = 28;
                    editLabelBox.Margin = new Thickness(0, 0, 8, 0);
                    editLabelBox.TextChanged += delegate { capturedLink.Label = editLabelBox.Text.Trim(); };
                    Grid.SetColumn(editLabelBox, 0);

                    TextBox editFormulaBox = CreateTextSettingBox("Fórmula (ex: localhost:<HTTPREST:PORT>/swagger)");
                    editFormulaBox.Text = capturedLink.Formula;
                    editFormulaBox.Height = 28;
                    editFormulaBox.Margin = new Thickness(0, 0, 8, 0);
                    editFormulaBox.TextChanged += delegate
                    {
                        capturedLink.Formula = editFormulaBox.Text.Trim();
                        formulaPreviewHost.Content = RenderFormulaVisualPills(capturedLink.Formula);
                    };
                    Grid.SetColumn(editFormulaBox, 1);

                    editFieldsGrid.Children.Add(editLabelBox);
                    editFieldsGrid.Children.Add(editFormulaBox);

                    lDock.Children.Add(lBtns);
                    lDock.Children.Add(editFieldsGrid);
                    lRowStack.Children.Add(lDock);
                    lRowStack.Children.Add(formulaPreviewHost);
                    lRow.Child = lRowStack;
                    userLinksPanel.Children.Add(lRow);
                }
            };

            renderUserLinks();

            // Formulario para adicionar novo botao personalizado
            Grid addLinkGrid = new Grid();
            addLinkGrid.Margin = new Thickness(0, 8, 0, 0);
            ColumnDefinition alc1 = new ColumnDefinition(); alc1.Width = new GridLength(160);
            ColumnDefinition alc2 = new ColumnDefinition(); alc2.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition alc3 = new ColumnDefinition(); alc3.Width = GridLength.Auto;
            addLinkGrid.ColumnDefinitions.Add(alc1);
            addLinkGrid.ColumnDefinitions.Add(alc2);
            addLinkGrid.ColumnDefinitions.Add(alc3);

            TextBox txtNewBtnLabel = CreateTextSettingBox("Nome (ex: Portal)");
            txtNewBtnLabel.Margin = new Thickness(0, 0, 8, 0);
            Grid.SetColumn(txtNewBtnLabel, 0);
            addLinkGrid.Children.Add(txtNewBtnLabel);

            TextBox txtNewBtnFormula = CreateTextSettingBox("Fórmula (ex: localhost:<HTTPREST:PORT>/portal)");
            txtNewBtnFormula.Margin = new Thickness(0, 0, 8, 0);
            Grid.SetColumn(txtNewBtnFormula, 1);
            addLinkGrid.Children.Add(txtNewBtnFormula);

            Button btnAddLink = UiTheme.CreateActionButton("+ Adicionar", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
            btnAddLink.Height = 32;
            btnAddLink.Click += delegate
            {
                string lbl = txtNewBtnLabel.Text.Trim();
                string form = txtNewBtnFormula.Text.Trim();
                if (string.IsNullOrEmpty(lbl) || string.IsNullOrEmpty(form))
                    return;

                tempLinks.Add(new CustomLinkButton(lbl, form, false));
                renderUserLinks();
                txtNewBtnLabel.Text = "";
                txtNewBtnFormula.Text = "";
            };
            Grid.SetColumn(btnAddLink, 2);
            addLinkGrid.Children.Add(btnAddLink);

            linksCardStack.Children.Add(addLinkGrid);
            cardLinks.Child = linksCardStack;
            contentStack.Children.Add(cardLinks);

            // --- Secao 4: Regras de Verificacao de Duplicidade (Alertas) ---
            contentStack.Children.Add(CreateSettingsSectionHeader("Regras de Verificação de Duplicidade (Alertas)"));

            Border cardRules = CreateSettingsCard();
            StackPanel rulesCardStack = new StackPanel();

            TextBlock rulesHint = CreateSettingsDesc("Use <ambiente> para representar a primeira seção [nome] de cada base. Deixe a tag vazia para verificar duplicidade da própria seção.");
            rulesHint.Margin = new Thickness(0, 0, 0, 12);
            rulesCardStack.Children.Add(rulesHint);

            StackPanel rulesListPanel = new StackPanel();
            rulesCardStack.Children.Add(rulesListPanel);

            Action renderRulesList = null;
            renderRulesList = delegate
            {
                rulesListPanel.Children.Clear();
                foreach (ConflictRule rule in tempRules)
                {
                    ConflictRule capturedRule = rule;
                    Border rRow = new Border();
                    rRow.Background = UiTheme.BgInput;
                    rRow.BorderBrush = UiTheme.BorderSubtle;
                    rRow.BorderThickness = new Thickness(1);
                    rRow.CornerRadius = new CornerRadius(7);
                    rRow.Padding = new Thickness(12, 7, 8, 7);
                    rRow.Margin = new Thickness(0, 0, 0, 6);

                    DockPanel rDock = new DockPanel();

                    Button btnRemRule = UiTheme.CreateActionButton("✕", UiTheme.NeutralBtnHex, "#DC2626");
                    btnRemRule.Padding = new Thickness(8, 3, 8, 3);
                    btnRemRule.FontSize = 11;
                    DockPanel.SetDock(btnRemRule, Dock.Right);
                    btnRemRule.Click += delegate
                    {
                        tempRules.Remove(capturedRule);
                        renderRulesList();
                    };

                    StackPanel ruleTextStack = new StackPanel();
                    ruleTextStack.Orientation = Orientation.Horizontal;
                    ruleTextStack.VerticalAlignment = VerticalAlignment.Center;

                    TextBlock secLbl = new TextBlock();
                    secLbl.Text = string.Format("[{0}]", capturedRule.SectionPattern);
                    secLbl.Foreground = UiTheme.AccentLightBlue;
                    secLbl.FontWeight = FontWeights.Bold;
                    secLbl.FontSize = 13;
                    ruleTextStack.Children.Add(secLbl);

                    if (capturedRule.IsSelfSection)
                    {
                        TextBlock selfLbl = new TextBlock();
                        selfLbl.Text = "  (si mesmo)";
                        selfLbl.Foreground = UiTheme.TextSecondary;
                        selfLbl.FontStyle = FontStyles.Italic;
                        selfLbl.FontSize = 12;
                        selfLbl.VerticalAlignment = VerticalAlignment.Center;
                        ruleTextStack.Children.Add(selfLbl);
                    }
                    else
                    {
                        TextBlock arrowLbl = new TextBlock();
                        arrowLbl.Text = "  >  ";
                        arrowLbl.Foreground = UiTheme.TextMuted;
                        arrowLbl.FontWeight = FontWeights.Bold;
                        arrowLbl.FontSize = 13;
                        ruleTextStack.Children.Add(arrowLbl);

                        TextBlock keyLbl = new TextBlock();
                        keyLbl.Text = capturedRule.KeyName;
                        keyLbl.Foreground = UiTheme.TextPrimary;
                        keyLbl.FontWeight = FontWeights.SemiBold;
                        keyLbl.FontSize = 13;
                        ruleTextStack.Children.Add(keyLbl);
                    }

                    rDock.Children.Add(btnRemRule);
                    rDock.Children.Add(ruleTextStack);
                    rRow.Child = rDock;
                    rulesListPanel.Children.Add(rRow);
                }
            };

            renderRulesList();

            Grid addRuleGrid = new Grid();
            addRuleGrid.Margin = new Thickness(0, 8, 0, 0);
            ColumnDefinition rc1 = new ColumnDefinition(); rc1.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition rcArrow = new ColumnDefinition(); rcArrow.Width = GridLength.Auto;
            ColumnDefinition rc2 = new ColumnDefinition(); rc2.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition rcBtn = new ColumnDefinition(); rcBtn.Width = GridLength.Auto;
            addRuleGrid.ColumnDefinitions.Add(rc1);
            addRuleGrid.ColumnDefinitions.Add(rcArrow);
            addRuleGrid.ColumnDefinitions.Add(rc2);
            addRuleGrid.ColumnDefinitions.Add(rcBtn);

            TextBox txtNewSec = CreateTextSettingBox("Seção (ex: HTTPREST ou <ambiente>)");
            Grid.SetColumn(txtNewSec, 0);
            addRuleGrid.Children.Add(txtNewSec);

            TextBlock sepArrow = new TextBlock();
            sepArrow.Text = "  >  ";
            sepArrow.Foreground = UiTheme.TextMuted;
            sepArrow.FontWeight = FontWeights.Bold;
            sepArrow.FontSize = 14;
            sepArrow.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(sepArrow, 1);
            addRuleGrid.Children.Add(sepArrow);

            TextBox txtNewKey = CreateTextSettingBox("Chave (ex: Port, ou vazio = si mesmo)");
            txtNewKey.Margin = new Thickness(0, 0, 8, 0);
            Grid.SetColumn(txtNewKey, 2);
            addRuleGrid.Children.Add(txtNewKey);

            Button btnAddRule = UiTheme.CreateActionButton("+ Adicionar", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
            btnAddRule.Height = 32;
            btnAddRule.Click += delegate
            {
                string sec = txtNewSec.Text.Trim().Trim('[', ']');
                string key = txtNewKey.Text.Trim();
                if (string.IsNullOrEmpty(sec))
                    return;

                bool exists = tempRules.Any(r =>
                    string.Equals(r.SectionPattern, sec, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(r.KeyName ?? "", key, StringComparison.OrdinalIgnoreCase));

                if (!exists)
                {
                    tempRules.Add(new ConflictRule(sec, key));
                    renderRulesList();
                }
                txtNewSec.Text = "";
                txtNewKey.Text = "";
            };
            Grid.SetColumn(btnAddRule, 3);
            addRuleGrid.Children.Add(btnAddRule);

            rulesCardStack.Children.Add(addRuleGrid);
            cardRules.Child = rulesCardStack;
            contentStack.Children.Add(cardRules);

            // --- Secao 5: Sobre (Restaurar Padrao ao lado esquerdo de Verificar Atualizacoes) ---
            contentStack.Children.Add(CreateSettingsSectionHeader("Sobre"));

            Border cardAbout = CreateSettingsCard();
            StackPanel aboutStack = new StackPanel();

            DockPanel aboutTopDock = new DockPanel();
            aboutTopDock.Margin = new Thickness(0, 0, 0, 10);

            StackPanel aboutRightBtns = new StackPanel();
            aboutRightBtns.Orientation = Orientation.Horizontal;
            aboutRightBtns.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(aboutRightBtns, Dock.Right);

            // Restaurar padrao a ESQUERDA de Verificar atualizacoes, com Double Check Popup
            Button btnRestoreDefault = UiTheme.CreateActionButton("Restaurar padrão", "#DC2626", "#EF4444");
            btnRestoreDefault.Margin = new Thickness(0, 0, 8, 0);
            btnRestoreDefault.ToolTip = "Desoculta tudo e volta todas as configurações para o padrão";
            btnRestoreDefault.Click += delegate
            {
                StyledDialog.DialogChoice confirm = StyledDialog.AskWithToggle(
                    this,
                    "Restaurar Padrão",
                    "É isso mesmo que você deseja fazer?",
                    "Esta ação irá desocultar todas as bases e redefinir tema, tempos, regras de alerta, links personalizados e preferências para o padrão original.",
                    "Sim, tenho certeza",
                    false,
                    "Confirmar",
                    "#DC2626",
                    "#EF4444");

                if (confirm != StyledDialog.DialogChoice.Yes)
                    return;

                _hiddenDirs.Clear();
                SalvarOcultosAppData();

                _rootPath = @"C:\bases";
                _pollIntervalSeconds = 60;
                _scanBasesMinutes = 60;
                _browserDelaySeconds = 4;
                _ignoreOldFolders = true;
                _showProcessUser = true;
                _showIniComments = false;
                _updateDriveUrl = UiTheme.DefaultUpdateDriveUrl;
                _conflictRules = ObterRegrasPadrao();
                _customLinks.Clear();

                try { if (File.Exists(_configPath)) File.Delete(_configPath); } catch { }
                File.WriteAllText(_timersPrefPath, "60;60;4", Encoding.UTF8);
                File.WriteAllText(_startupPrefPath, "1", Encoding.UTF8);
                AplicarRegistroWindowsStartup(true);
                File.WriteAllText(_themePrefPath, "dark", Encoding.UTF8);
                File.WriteAllText(_ignoreOldPrefPath, "1", Encoding.UTF8);
                File.WriteAllText(_showUserPrefPath, "1", Encoding.UTF8);
                File.WriteAllText(_showCommentsPrefPath, "0", Encoding.UTF8);
                File.WriteAllText(_driveUrlPrefPath, _updateDriveUrl, Encoding.UTF8);
                SalvarRegrasAppData();
                SalvarCustomLinksAppData();

                UiTheme.ApplyTheme(true);
                Background = UiTheme.BgWindow;

                _monitorTimer.Interval = TimeSpan.FromSeconds(_pollIntervalSeconds);
                _basesRefreshTimer.Interval = TimeSpan.FromMinutes(_scanBasesMinutes);

                if (Directory.Exists(@"C:\bases"))
                {
                    _needsOnboarding = false;
                    ShowSettingsView();
                    CarregarAmbientesAsync(false);
                }
                else
                {
                    _needsOnboarding = true;
                    _isInitialScanCompleted = false;
                    ShowOnboardingView();
                }
            };

            Button btnCheckUpdates = UiTheme.CreateActionButton("Verificar atualizações", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
            btnCheckUpdates.ToolTip = "Abrir pasta de atualizações no Google Drive";
            btnCheckUpdates.Click += delegate { AbrirDriveAtualizacoes(); };

            aboutRightBtns.Children.Add(btnRestoreDefault);
            aboutRightBtns.Children.Add(btnCheckUpdates);
            aboutTopDock.Children.Add(aboutRightBtns);

            StackPanel aboutTop = new StackPanel();
            aboutTop.Orientation = Orientation.Horizontal;
            aboutTop.Children.Add(TotvsLogo.CreateTotvsWhiteSvgLogo(_cachedSvgPath, 30));

            StackPanel aboutTitles = new StackPanel();
            aboutTitles.Margin = new Thickness(12, 0, 10, 0);
            aboutTitles.VerticalAlignment = VerticalAlignment.Center;

            TextBlock aboutName = new TextBlock();
            aboutName.Text = "TOTVSHub  •  " + UiTheme.AppVersion;
            aboutName.Foreground = UiTheme.TextPrimary;
            aboutName.FontSize = 14;
            aboutName.FontWeight = FontWeights.Bold;

            TextBlock aboutContact = new TextBlock();
            aboutContact.Text = "Dúvidas, contate vinicius.domingues@totvs.com.br";
            aboutContact.Foreground = UiTheme.AccentLightBlue;
            aboutContact.FontSize = 12;
            aboutContact.Margin = new Thickness(0, 2, 0, 0);

            aboutTitles.Children.Add(aboutName);
            aboutTitles.Children.Add(aboutContact);
            aboutTop.Children.Add(aboutTitles);
            aboutTopDock.Children.Add(aboutTop);
            aboutStack.Children.Add(aboutTopDock);

            TextBlock aboutDesc = new TextBlock();
            aboutDesc.Text = "O TOTVSHub é uma central unificada projetada para simplificar o gerenciamento diário de ambientes TOTVS AppServer (Console e Serviços do Windows). Permite monitorar estados em tempo real, detectar duplicidades de portas e chaves no appserver.ini, organizar anotações por base e editar configurações de forma visual e segura.";
            aboutDesc.Foreground = UiTheme.TextSecondary;
            aboutDesc.FontSize = 12;
            aboutDesc.TextWrapping = TextWrapping.Wrap;
            aboutDesc.LineHeight = 18;
            aboutStack.Children.Add(aboutDesc);

            cardAbout.Child = aboutStack;
            contentStack.Children.Add(cardAbout);

            scroll.Content = contentStack;
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            // Barra Inferior apenas com Salvar
            Border bottomBar = new Border();
            bottomBar.Background = UiTheme.BgHeader;
            bottomBar.BorderBrush = UiTheme.BorderSubtle;
            bottomBar.BorderThickness = new Thickness(0, 1, 0, 0);
            bottomBar.Padding = new Thickness(24, 12, 24, 12);

            Button btnSaveSettings = UiTheme.CreateActionButton("Salvar", "#059669", "#10B981");
            btnSaveSettings.Padding = new Thickness(22, 8, 22, 8);
            btnSaveSettings.HorizontalAlignment = HorizontalAlignment.Right;
            btnSaveSettings.Click += delegate
            {
                int newPoll, newScan, newDelay;
                if (!int.TryParse(txtPollSec.Text.Trim(), out newPoll) || newPoll < 5) newPoll = 60;
                if (!int.TryParse(txtScanMin.Text.Trim(), out newScan) || newScan < 1) newScan = 60;
                if (!int.TryParse(txtBrowserSec.Text.Trim(), out newDelay) || newDelay < 0) newDelay = 4;

                bool oldIgnoreChanged = (_ignoreOldFolders != tempIgnoreOld);

                _pollIntervalSeconds = newPoll;
                _scanBasesMinutes = newScan;
                _browserDelaySeconds = newDelay;
                _ignoreOldFolders = tempIgnoreOld;
                _showProcessUser = tempShowUser;
                _showIniComments = tempShowComments;
                _conflictRules = tempRules;
                _customLinks = tempLinks;

                File.WriteAllText(_timersPrefPath, string.Format("{0};{1};{2}", _pollIntervalSeconds, _scanBasesMinutes, _browserDelaySeconds), Encoding.UTF8);
                File.WriteAllText(_startupPrefPath, tempAutoStart ? "1" : "0", Encoding.UTF8);
                AplicarRegistroWindowsStartup(tempAutoStart);

                File.WriteAllText(_themePrefPath, tempDark ? "dark" : "light", Encoding.UTF8);
                File.WriteAllText(_ignoreOldPrefPath, _ignoreOldFolders ? "1" : "0", Encoding.UTF8);
                File.WriteAllText(_showUserPrefPath, _showProcessUser ? "1" : "0", Encoding.UTF8);
                File.WriteAllText(_showCommentsPrefPath, _showIniComments ? "1" : "0", Encoding.UTF8);
                SalvarRegrasAppData();
                SalvarCustomLinksAppData();

                UiTheme.ApplyTheme(tempDark);
                Background = UiTheme.BgWindow;

                _monitorTimer.Interval = TimeSpan.FromSeconds(_pollIntervalSeconds);
                _basesRefreshTimer.Interval = TimeSpan.FromMinutes(_scanBasesMinutes);

                RecalcularAlertas();
                if (_needsOnboarding)
                {
                    ShowOnboardingView();
                }
                else
                {
                    ShowDashboardView();
                    if (oldIgnoreChanged)
                    {
                        CarregarAmbientesAsync(true);
                    }
                }
            };

            bottomBar.Child = btnSaveSettings;
            Grid.SetRow(bottomBar, 2);
            root.Children.Add(bottomBar);

            _pageHost.Content = root;
        }

        // Renderiza visualmente a formula diferenciando string literal e variavel <SECAO:CHAVE> ou {SECAO:CHAVE}
        private UIElement RenderFormulaVisualPills(string formula)
        {
            WrapPanel sp = new WrapPanel();
            sp.VerticalAlignment = VerticalAlignment.Center;

            string safeFormula = formula ?? "";
            MatchCollection matches = Regex.Matches(safeFormula, @"(\{[^}]+\}|<[^>]+>)");
            int lastIdx = 0;
            foreach (Match m in matches)
            {
                if (m.Index > lastIdx)
                {
                    string literal = safeFormula.Substring(lastIdx, m.Index - lastIdx);
                    Border litPill = new Border();
                    litPill.Background = UiTheme.TagBg;
                    litPill.CornerRadius = new CornerRadius(4);
                    litPill.Padding = new Thickness(5, 1, 5, 1);
                    litPill.Margin = new Thickness(1, 0, 1, 0);

                    TextBlock litTb = new TextBlock();
                    litTb.Text = literal;
                    litTb.Foreground = UiTheme.TextSecondary;
                    litTb.FontSize = 11;
                    litTb.VerticalAlignment = VerticalAlignment.Center;
                    litPill.Child = litTb;
                    sp.Children.Add(litPill);
                }

                Border varPill = new Border();
                varPill.Background = UiTheme.BrushFromHex("#0C4A6E");
                varPill.BorderBrush = UiTheme.BrushFromHex(UiTheme.PrimaryBlueHex);
                varPill.BorderThickness = new Thickness(1);
                varPill.CornerRadius = new CornerRadius(4);
                varPill.Padding = new Thickness(6, 1, 6, 1);
                varPill.Margin = new Thickness(2, 0, 2, 0);

                TextBlock varTb = new TextBlock();
                varTb.Text = m.Value;
                varTb.Foreground = UiTheme.AccentLightBlue;
                varTb.FontSize = 11;
                varTb.FontWeight = FontWeights.Bold;
                varPill.Child = varTb;
                sp.Children.Add(varPill);

                lastIdx = m.Index + m.Length;
            }

            if (lastIdx < safeFormula.Length)
            {
                string tail = safeFormula.Substring(lastIdx);
                Border tailPill = new Border();
                tailPill.Background = UiTheme.TagBg;
                tailPill.CornerRadius = new CornerRadius(4);
                tailPill.Padding = new Thickness(5, 1, 5, 1);
                tailPill.Margin = new Thickness(1, 0, 1, 0);

                TextBlock tailTb = new TextBlock();
                tailTb.Text = tail;
                tailTb.Foreground = UiTheme.TextSecondary;
                tailTb.FontSize = 11;
                tailTb.VerticalAlignment = VerticalAlignment.Center;
                tailPill.Child = tailTb;
                sp.Children.Add(tailPill);
            }

            return sp;
        }

        private TextBox CreateTextSettingBox(string placeholder)
        {
            TextBox tb = new TextBox();
            tb.Height = 32;
            tb.Foreground = UiTheme.TextPrimary;
            tb.Background = UiTheme.BgInput;
            tb.CaretBrush = UiTheme.TextPrimary;
            tb.BorderBrush = UiTheme.BorderSubtle;
            tb.BorderThickness = new Thickness(1);
            tb.Padding = new Thickness(8, 0, 8, 0);
            tb.FontSize = 12;
            tb.VerticalContentAlignment = VerticalAlignment.Center;
            tb.ToolTip = placeholder;
            return tb;
        }

        private TextBlock CreateSettingsSectionHeader(string text)
        {
            TextBlock tb = new TextBlock();
            tb.Text = text.ToUpperInvariant();
            tb.Foreground = UiTheme.TextMuted;
            tb.FontSize = 11;
            tb.FontWeight = FontWeights.Bold;
            tb.Margin = new Thickness(2, 6, 0, 8);
            return tb;
        }

        private Border CreateSettingsCard()
        {
            Border b = new Border();
            b.Background = UiTheme.BgCard;
            b.BorderBrush = UiTheme.BorderSubtle;
            b.BorderThickness = new Thickness(1);
            b.CornerRadius = new CornerRadius(10);
            b.Padding = new Thickness(18, 15, 18, 15);
            b.Margin = new Thickness(0, 0, 0, 16);
            return b;
        }

        private TextBlock CreateSettingsTitle(string text)
        {
            TextBlock tb = new TextBlock();
            tb.Text = text;
            tb.Foreground = UiTheme.TextPrimary;
            tb.FontSize = 13;
            tb.FontWeight = FontWeights.SemiBold;
            return tb;
        }

        private TextBlock CreateSettingsDesc(string text)
        {
            TextBlock tb = new TextBlock();
            tb.Text = text;
            tb.Foreground = UiTheme.TextSecondary;
            tb.FontSize = 11;
            tb.Margin = new Thickness(0, 2, 16, 0);
            tb.TextWrapping = TextWrapping.Wrap;
            return tb;
        }

        private TextBox CreateNumericSettingBox(string val)
        {
            TextBox tb = new TextBox();
            tb.Text = val;
            tb.Width = 76;
            tb.Height = 32;
            tb.Foreground = UiTheme.TextPrimary;
            tb.Background = UiTheme.BgInput;
            tb.CaretBrush = UiTheme.TextPrimary;
            tb.BorderBrush = UiTheme.BorderSubtle;
            tb.BorderThickness = new Thickness(1);
            tb.FontSize = 13;
            tb.FontWeight = FontWeights.Bold;
            tb.TextAlignment = TextAlignment.Center;
            tb.VerticalContentAlignment = VerticalAlignment.Center;
            return tb;
        }

        private DockPanel CreateTimerRow(string title, string desc, TextBox inputBox, bool bottomMargin)
        {
            DockPanel dp = new DockPanel();
            if (bottomMargin) dp.Margin = new Thickness(0, 0, 0, 14);
            DockPanel.SetDock(inputBox, Dock.Right);

            StackPanel labels = new StackPanel();
            labels.VerticalAlignment = VerticalAlignment.Center;
            labels.Children.Add(CreateSettingsTitle(title));
            labels.Children.Add(CreateSettingsDesc(desc));

            dp.Children.Add(inputBox);
            dp.Children.Add(labels);
            return dp;
        }
    }
}
