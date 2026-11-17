using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        // =====================================================================
        // TELA INTEIRA: EDITOR DE APPSERVER.INI
        // =====================================================================
        private void ShowIniEditorView(AmbienteInfo amb)
        {
            _isInDashboardView = false;

            List<IniSection> sections = ParseIniFile(amb.IniPath);
            string iniFileName = Path.GetFileName(amb.IniPath);

            Grid root = new Grid();
            RowDefinition rTop = new RowDefinition(); rTop.Height = GridLength.Auto;
            RowDefinition rSearch = new RowDefinition(); rSearch.Height = GridLength.Auto;
            RowDefinition rBody = new RowDefinition(); rBody.Height = new GridLength(1, GridUnitType.Star);
            RowDefinition rBottom = new RowDefinition(); rBottom.Height = GridLength.Auto;
            root.RowDefinitions.Add(rTop);
            root.RowDefinitions.Add(rSearch);
            root.RowDefinitions.Add(rBody);
            root.RowDefinitions.Add(rBottom);

            StackPanel topLeft = new StackPanel();
            topLeft.Orientation = Orientation.Horizontal;
            topLeft.VerticalAlignment = VerticalAlignment.Center;

            Button btnBackArrow = UiTheme.CreateFramelessIconButton(
                UiTheme.CreateLargeThinLeftArrowIcon(UiTheme.TextPrimary),
                "Voltar");
            btnBackArrow.Margin = new Thickness(0, 0, 12, 0);
            btnBackArrow.Click += delegate { ShowDashboardView(); };

            TextBlock lblIniTitle = new TextBlock();
            lblIniTitle.Text = iniFileName;
            lblIniTitle.Foreground = UiTheme.TextPrimary;
            lblIniTitle.FontSize = 16;
            lblIniTitle.FontWeight = FontWeights.Bold;
            lblIniTitle.VerticalAlignment = VerticalAlignment.Center;

            Border pathPill = new Border();
            pathPill.Background = UiTheme.TagBg;
            pathPill.BorderBrush = UiTheme.BorderSubtle;
            pathPill.BorderThickness = new Thickness(1);
            pathPill.CornerRadius = new CornerRadius(5);
            pathPill.Padding = new Thickness(8, 2, 8, 2);
            pathPill.Margin = new Thickness(12, 0, 0, 0);
            pathPill.VerticalAlignment = VerticalAlignment.Center;

            TextBlock lblIniPath = new TextBlock();
            lblIniPath.Text = amb.IniPath;
            lblIniPath.Foreground = UiTheme.TextSecondary;
            lblIniPath.FontSize = 11;
            lblIniPath.VerticalAlignment = VerticalAlignment.Center;
            pathPill.Child = lblIniPath;

            topLeft.Children.Add(btnBackArrow);
            topLeft.Children.Add(lblIniTitle);
            topLeft.Children.Add(pathPill);

            Border topBar = BuildTopWindowBar(topLeft, null);
            Grid.SetRow(topBar, 0);
            root.Children.Add(topBar);

            Border filterBar = new Border();
            filterBar.Padding = new Thickness(20, 12, 20, 10);
            filterBar.Background = UiTheme.BgWindow;

            Border searchBoxBorder = new Border();
            searchBoxBorder.Background = UiTheme.BgHeader;
            searchBoxBorder.BorderBrush = UiTheme.BorderSubtle;
            searchBoxBorder.BorderThickness = new Thickness(1);
            searchBoxBorder.CornerRadius = new CornerRadius(8);
            searchBoxBorder.Padding = new Thickness(12, 7, 12, 7);

            DockPanel searchDock = new DockPanel();
            UIElement searchIcon = UiTheme.CreateSearchIcon();
            DockPanel.SetDock(searchIcon, Dock.Left);
            searchDock.Children.Add(searchIcon);

            Grid searchInner = new Grid();
            TextBlock filterPlaceholder = new TextBlock();
            filterPlaceholder.Text = "Filtrar por [seção], chave, valor ou comentário...";
            filterPlaceholder.Foreground = UiTheme.TextMuted;
            filterPlaceholder.FontSize = 13;
            filterPlaceholder.VerticalAlignment = VerticalAlignment.Center;
            filterPlaceholder.IsHitTestVisible = false;

            TextBox filterBox = new TextBox();
            filterBox.Background = Brushes.Transparent;
            filterBox.Foreground = UiTheme.TextPrimary;
            filterBox.CaretBrush = UiTheme.TextPrimary;
            filterBox.BorderThickness = new Thickness(0);
            filterBox.FontSize = 13;
            filterBox.VerticalAlignment = VerticalAlignment.Center;

            searchInner.Children.Add(filterPlaceholder);
            searchInner.Children.Add(filterBox);
            searchDock.Children.Add(searchInner);
            searchBoxBorder.Child = searchDock;
            filterBar.Child = searchBoxBorder;

            Grid.SetRow(filterBar, 1);
            root.Children.Add(filterBar);

            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.Padding = new Thickness(20, 4, 20, 16);

            StackPanel sectionsContainer = new StackPanel();
            scroll.Content = sectionsContainer;
            Grid.SetRow(scroll, 2);
            root.Children.Add(scroll);

            Action renderIniSections = null;
            renderIniSections = delegate
            {
                sectionsContainer.Children.Clear();
                string filter = (filterBox.Text ?? "").Trim().ToLowerInvariant();

                HashSet<string> restSectionNames = IdentifyRestSectionNames(sections);
                List<KeyValuePair<IniSection, List<IniEntry>>> restMatched = new List<KeyValuePair<IniSection, List<IniEntry>>>();
                List<KeyValuePair<IniSection, List<IniEntry>>> allMatched = new List<KeyValuePair<IniSection, List<IniEntry>>>();

                foreach (IniSection sec in sections)
                {
                    bool secMatches = string.IsNullOrEmpty(filter)
                        || (sec.Name != null && sec.Name.ToLowerInvariant().Contains(filter));

                    List<IniEntry> entriesToShow = new List<IniEntry>();
                    foreach (IniEntry entry in sec.Entries)
                    {
                        // Respeita a configuracao "Ver comentarios" (padrao desmarcado)
                        if (entry.IsPureComment && !_showIniComments)
                            continue;

                        if (secMatches)
                        {
                            entriesToShow.Add(entry);
                        }
                        else
                        {
                            bool entryMatch = (entry.Key != null && entry.Key.ToLowerInvariant().Contains(filter))
                                           || (entry.Value != null && entry.Value.ToLowerInvariant().Contains(filter));
                            if (entryMatch)
                                entriesToShow.Add(entry);
                        }
                    }

                    if (!secMatches && entriesToShow.Count == 0)
                        continue;

                    KeyValuePair<IniSection, List<IniEntry>> item = new KeyValuePair<IniSection, List<IniEntry>>(sec, entriesToShow);
                    allMatched.Add(item);
                    if (!string.IsNullOrEmpty(sec.Name) && restSectionNames.Contains(sec.Name.Trim()))
                    {
                        restMatched.Add(item);
                    }
                }

                bool restGroupRendered = false;
                foreach (KeyValuePair<IniSection, List<IniEntry>> item in allMatched)
                {
                    bool isRestSec = !string.IsNullOrEmpty(item.Key.Name) && restSectionNames.Contains(item.Key.Name.Trim());
                    if (isRestSec)
                    {
                        if (!restGroupRendered)
                        {
                            restGroupRendered = true;
                            sectionsContainer.Children.Add(BuildRestGroupContainer(amb, sections, restMatched, renderIniSections));
                        }
                    }
                    else
                    {
                        sectionsContainer.Children.Add(BuildIniSectionCard(amb, sections, item.Key, item.Value, renderIniSections));
                    }
                }
            };

            filterBox.TextChanged += delegate
            {
                filterPlaceholder.Visibility = string.IsNullOrEmpty(filterBox.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                renderIniSections();
            };

            Border bottomBar = new Border();
            bottomBar.Background = UiTheme.BgHeader;
            bottomBar.BorderBrush = UiTheme.BorderSubtle;
            bottomBar.BorderThickness = new Thickness(0, 1, 0, 0);
            bottomBar.Padding = new Thickness(20, 12, 20, 12);

            DockPanel bottomDock = new DockPanel();

            Button btnAddSection = UiTheme.CreateActionButton("+ Nova Seção []", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
            btnAddSection.Padding = new Thickness(14, 8, 14, 8);
            DockPanel.SetDock(btnAddSection, Dock.Left);
            btnAddSection.Click += delegate
            {
                IniSection novaSec = new IniSection();
                novaSec.Name = "NOVA_SECAO";
                novaSec.IsCommented = false;

                IniEntry primeiraLinha = new IniEntry();
                primeiraLinha.IsPureComment = false;
                primeiraLinha.IsCommented = false;
                primeiraLinha.Key = "CHAVE";
                primeiraLinha.Value = "VALOR";
                novaSec.Entries.Add(primeiraLinha);

                sections.Insert(0, novaSec);
                filterBox.Text = "";
                renderIniSections();

                if (novaSec.SectionNameBox != null)
                {
                    novaSec.SectionNameBox.Focus();
                    novaSec.SectionNameBox.SelectAll();
                }
            };

            Button btnSaveIni = UiTheme.CreateActionButton("Salvar", "#059669", "#10B981");
            btnSaveIni.Padding = new Thickness(22, 8, 22, 8);
            DockPanel.SetDock(btnSaveIni, Dock.Right);
            btnSaveIni.Click += delegate
            {
                StyledDialog.DialogChoice choice = StyledDialog.AskWithToggle(
                    this,
                    "Salvar Configuração",
                    "Deseja salvar com cópia?",
                    "Quando ativado, gera um arquivo de backup em appserver.ini.bak antes de gravar.",
                    "Salvar com cópia (.bak)",
                    true,
                    "Confirmar",
                    "#059669",
                    "#10B981");

                if (choice == StyledDialog.DialogChoice.Cancel)
                    return;

                bool criarCopia = (choice == StyledDialog.DialogChoice.Yes);
                if (GravarArquivoIni(amb.IniPath, sections, criarCopia))
                {
                    AtualizarDadosIniAmbiente(amb);
                    RecalcularAlertas();
                    ShowDashboardView();
                }
            };

            bottomDock.Children.Add(btnAddSection);
            bottomDock.Children.Add(btnSaveIni);
            bottomDock.Children.Add(new Grid());

            bottomBar.Child = bottomDock;
            Grid.SetRow(bottomBar, 3);
            root.Children.Add(bottomBar);

            renderIniSections();
            _pageHost.Content = root;
        }

        private List<IniSection> ParseIniFile(string iniPath)
        {
            List<IniSection> sections = new List<IniSection>();
            if (!File.Exists(iniPath)) return sections;

            IniSection currentSection = null;
            string[] lines = File.ReadAllLines(iniPath, Encoding.Default);

            foreach (string raw in lines)
            {
                string trimmed = raw.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                bool isCommented = false;
                string content = trimmed;

                if (content.StartsWith(";"))
                {
                    isCommented = true;
                    content = content.TrimStart(';').Trim();
                }

                if (content.StartsWith("[") && content.Contains("]"))
                {
                    int closeIdx = content.IndexOf(']');
                    string secName = content.Substring(1, closeIdx - 1).Trim();
                    if (!string.IsNullOrEmpty(secName))
                    {
                        currentSection = new IniSection();
                        currentSection.Name = secName;
                        currentSection.IsCommented = isCommented;
                        sections.Add(currentSection);
                        continue;
                    }
                }

                if (currentSection == null)
                {
                    currentSection = new IniSection();
                    currentSection.Name = "GERAL";
                    currentSection.IsCommented = false;
                    sections.Add(currentSection);
                }

                int eqIdx = content.IndexOf('=');
                if (eqIdx > 0)
                {
                    string possibleKey = content.Substring(0, eqIdx).Trim();
                    if (!possibleKey.Contains(" ") && !possibleKey.Contains(":"))
                    {
                        string val = content.Substring(eqIdx + 1).Trim();
                        IniEntry entry = new IniEntry();
                        entry.IsPureComment = false;
                        entry.IsCommented = isCommented;
                        entry.Key = possibleKey;
                        entry.Value = val;
                        currentSection.Entries.Add(entry);
                        continue;
                    }
                }

                IniEntry commentEntry = new IniEntry();
                commentEntry.IsPureComment = true;
                commentEntry.IsCommented = true;
                commentEntry.Key = "";
                commentEntry.Value = trimmed.TrimStart(';').Trim();
                currentSection.Entries.Add(commentEntry);
            }

            return sections;
        }

        private Border BuildIniSectionCard(AmbienteInfo amb, List<IniSection> allSections, IniSection sec, List<IniEntry> visibleEntries, Action onRefreshList)
        {
            bool isProtectedSection = ProtectedIniSections.Contains(sec.Name ?? "");

            Border card = new Border();
            card.Background = UiTheme.BgCard;
            card.BorderBrush = UiTheme.BorderSubtle;
            card.BorderThickness = new Thickness(1);
            card.CornerRadius = new CornerRadius(10);
            card.Margin = new Thickness(0, 0, 0, 14);
            sec.SectionCard = card;

            StackPanel cardStack = new StackPanel();

            Border headerBar = new Border();
            headerBar.Background = UiTheme.BgSectionHeader;
            headerBar.CornerRadius = new CornerRadius(10, 10, 0, 0);
            headerBar.BorderBrush = UiTheme.BorderSubtle;
            headerBar.BorderThickness = new Thickness(0, 0, 0, 1);
            headerBar.Padding = new Thickness(14, 10, 14, 10);
            sec.HeaderBar = headerBar;

            DockPanel headerDock = new DockPanel();

            StackPanel secBtns = new StackPanel();
            secBtns.Orientation = Orientation.Horizontal;
            DockPanel.SetDock(secBtns, Dock.Right);

            Button btnAddLine = UiTheme.CreateActionButton("+ Linha", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
            btnAddLine.Padding = new Thickness(9, 5, 9, 5);
            btnAddLine.FontSize = 11;
            btnAddLine.Margin = new Thickness(0, 0, 6, 0);
            btnAddLine.Click += delegate
            {
                IniEntry nova = new IniEntry();
                nova.IsPureComment = false;
                nova.IsCommented = sec.IsCommented;
                nova.Key = "NOVA_CHAVE";
                nova.Value = "";
                sec.Entries.Add(nova);

                sec.EntriesPanel.Children.Add(BuildIniEntryRow(amb, sec, nova));
                AtualizarVisualIniSecao(sec, isProtectedSection);
                if (nova.KeyBox != null)
                {
                    nova.KeyBox.Focus();
                    nova.KeyBox.SelectAll();
                }
            };
            secBtns.Children.Add(btnAddLine);

            if (_showIniComments)
            {
                Button btnAddComment = UiTheme.CreateActionButton("+ Comentário", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
                btnAddComment.Padding = new Thickness(9, 5, 9, 5);
                btnAddComment.FontSize = 11;
                btnAddComment.Margin = new Thickness(0, 0, 6, 0);
                btnAddComment.Click += delegate
                {
                    IniEntry nota = new IniEntry();
                    nota.IsPureComment = true;
                    nota.IsCommented = true;
                    nota.Key = "";
                    nota.Value = "Novo comentário";
                    sec.Entries.Add(nota);

                    sec.EntriesPanel.Children.Add(BuildIniEntryRow(amb, sec, nota));
                };
                secBtns.Children.Add(btnAddComment);
            }

            Button btnToggleSec = UiTheme.CreateActionButton("; Comentar Seção", "#334155", "#475569");
            btnToggleSec.Padding = new Thickness(9, 5, 9, 5);
            btnToggleSec.FontSize = 11;
            btnToggleSec.Margin = new Thickness(0, 0, isProtectedSection ? 0 : 6, 0);
            btnToggleSec.Click += delegate
            {
                sec.IsCommented = !sec.IsCommented;
                foreach (IniEntry item in sec.Entries)
                {
                    if (!item.IsPureComment)
                        item.IsCommented = sec.IsCommented;
                }
                AtualizarVisualIniSecao(sec, isProtectedSection);
            };
            sec.BtnToggleSection = btnToggleSec;
            secBtns.Children.Add(btnToggleSec);

            // Secoes protegidas ([environment], [topconnect], [drivers], [tcp], [general], [webapp]) NAO possuem botao Excluir
            if (!isProtectedSection)
            {
                Button btnDelSec = UiTheme.CreateActionButton("🗑 Excluir", "#7F1D1D", "#DC2626");
                btnDelSec.Padding = new Thickness(9, 5, 9, 5);
                btnDelSec.FontSize = 11;
                btnDelSec.Click += delegate
                {
                    StyledDialog.DialogChoice res = StyledDialog.AskWithToggle(
                        this,
                        "Excluir Seção",
                        string.Format("Deseja realmente excluir a seção [{0}]?", sec.Name),
                        string.Format("Esta ação removerá {0} pertencente a ela.", UiTheme.Pluralize(sec.Entries.Count, "1 linha", sec.Entries.Count + " linhas").Replace("1 1 ", "1 ")),
                        "Confirmar exclusão da seção",
                        true,
                        "Excluir",
                        "#DC2626",
                        "#EF4444");

                    if (res == StyledDialog.DialogChoice.Yes)
                    {
                        allSections.Remove(sec);
                        onRefreshList();
                    }
                };
                secBtns.Children.Add(btnDelSec);
            }

            headerDock.Children.Add(secBtns);

            StackPanel nameStack = new StackPanel();
            nameStack.Orientation = Orientation.Horizontal;
            nameStack.VerticalAlignment = VerticalAlignment.Center;

            TextBlock commentPrefix = new TextBlock();
            commentPrefix.Text = ";";
            commentPrefix.Foreground = UiTheme.AccentAmber;
            commentPrefix.FontWeight = FontWeights.Bold;
            commentPrefix.FontSize = 15;
            commentPrefix.VerticalAlignment = VerticalAlignment.Center;
            commentPrefix.Margin = new Thickness(0, 0, 2, 0);
            sec.HeaderCommentPrefix = commentPrefix;

            TextBlock openBracket = new TextBlock();
            openBracket.Text = "[";
            openBracket.Foreground = UiTheme.AccentLightBlue;
            openBracket.FontWeight = FontWeights.Bold;
            openBracket.FontSize = 15;
            openBracket.VerticalAlignment = VerticalAlignment.Center;

            nameStack.Children.Add(commentPrefix);
            nameStack.Children.Add(openBracket);

            if (isProtectedSection)
            {
                // Secao protegida: nome somente leitura (nao editavel)
                TextBlock lblSecName = new TextBlock();
                lblSecName.Text = sec.Name;
                lblSecName.Foreground = UiTheme.AccentLightBlue;
                lblSecName.FontWeight = FontWeights.Bold;
                lblSecName.FontSize = 14;
                lblSecName.VerticalAlignment = VerticalAlignment.Center;
                lblSecName.Margin = new Thickness(2, 0, 2, 0);
                nameStack.Children.Add(lblSecName);
            }
            else
            {
                TextBox txtSecName = new TextBox();
                txtSecName.Text = sec.Name;
                txtSecName.Foreground = UiTheme.AccentLightBlue;
                txtSecName.Background = UiTheme.BgInput;
                txtSecName.CaretBrush = UiTheme.TextPrimary;
                txtSecName.BorderBrush = UiTheme.BorderSubtle;
                txtSecName.BorderThickness = new Thickness(1);
                txtSecName.FontWeight = FontWeights.Bold;
                txtSecName.FontSize = 14;
                txtSecName.VerticalAlignment = VerticalAlignment.Center;
                txtSecName.Padding = new Thickness(6, 2, 6, 2);
                txtSecName.Margin = new Thickness(3, 0, 3, 0);
                txtSecName.TextChanged += delegate
                {
                    sec.Name = txtSecName.Text.Trim();
                    AtualizarVisualIniSecao(sec, isProtectedSection);
                };
                sec.SectionNameBox = txtSecName;
                nameStack.Children.Add(txtSecName);
            }

            TextBlock closeBracket = new TextBlock();
            closeBracket.Text = "]";
            closeBracket.Foreground = UiTheme.AccentLightBlue;
            closeBracket.FontWeight = FontWeights.Bold;
            closeBracket.FontSize = 15;
            closeBracket.VerticalAlignment = VerticalAlignment.Center;

            Border hiddenBadge = new Border();
            hiddenBadge.Background = UiTheme.BrushFromHex("#451A03");
            hiddenBadge.BorderBrush = UiTheme.BrushFromHex("#92400E");
            hiddenBadge.BorderThickness = new Thickness(1);
            hiddenBadge.CornerRadius = new CornerRadius(5);
            hiddenBadge.Padding = new Thickness(7, 2, 7, 2);
            hiddenBadge.Margin = new Thickness(10, 0, 0, 0);
            hiddenBadge.VerticalAlignment = VerticalAlignment.Center;
            TextBlock hbTxt = new TextBlock();
            hbTxt.Text = "COMENTADA (;)";
            hbTxt.Foreground = UiTheme.AccentAmber;
            hbTxt.FontSize = 10;
            hbTxt.FontWeight = FontWeights.Bold;
            hiddenBadge.Child = hbTxt;
            sec.HiddenBadge = hiddenBadge;

            nameStack.Children.Add(closeBracket);
            nameStack.Children.Add(hiddenBadge);
            headerDock.Children.Add(nameStack);

            headerBar.Child = headerDock;
            cardStack.Children.Add(headerBar);

            StackPanel entriesPanel = new StackPanel();
            entriesPanel.Margin = new Thickness(14, 10, 14, 12);
            sec.EntriesPanel = entriesPanel;

            foreach (IniEntry entry in visibleEntries)
            {
                entriesPanel.Children.Add(BuildIniEntryRow(amb, sec, entry));
            }

            cardStack.Children.Add(entriesPanel);
            card.Child = cardStack;

            AtualizarVisualIniSecao(sec, isProtectedSection);
            return card;
        }

        // =====================================================================
        // ITEM 8: AGRUPAMENTO VISUAL DAS SECOES REST E ATIVAR/INATIVAR VIA [ONSTART]>jobs
        // =====================================================================
        private HashSet<string> IdentifyRestSectionNames(List<IniSection> allSections)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "HTTPV11", "HTTPREST", "HTTPURI", "HTTPJOB"
            };

            foreach (IniSection s in allSections)
            {
                string sName = (s.Name ?? "").Trim();
                if (string.IsNullOrEmpty(sName)) continue;

                if (string.Equals(sName, "HTTPV11", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (IniEntry e in s.Entries)
                    {
                        if (!e.IsPureComment && string.Equals(e.Key, "Sockets", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(e.Value))
                        {
                            foreach (string part in e.Value.Split(','))
                            {
                                string clean = part.Trim();
                                if (!string.IsNullOrEmpty(clean)) names.Add(clean);
                            }
                        }
                    }
                }

                foreach (IniEntry e in s.Entries)
                {
                    if (!e.IsPureComment && string.Equals(e.Key, "MAIN", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals((e.Value ?? "").Trim(), "HTTP_START", StringComparison.OrdinalIgnoreCase))
                    {
                        names.Add(sName);
                    }
                }
            }

            List<string> snapshot = names.ToList();
            foreach (IniSection s in allSections)
            {
                string sName = (s.Name ?? "").Trim();
                if (string.IsNullOrEmpty(sName) || !snapshot.Contains(sName, StringComparer.OrdinalIgnoreCase)) continue;

                foreach (IniEntry e in s.Entries)
                {
                    if (!e.IsPureComment && string.Equals(e.Key, "URIs", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(e.Value))
                    {
                        foreach (string part in e.Value.Split(','))
                        {
                            string clean = part.Trim();
                            if (!string.IsNullOrEmpty(clean)) names.Add(clean);
                        }
                    }
                }
            }

            return names;
        }

        private string ObterNomeJobRest(List<IniSection> allSections)
        {
            foreach (IniSection s in allSections)
            {
                string sName = (s.Name ?? "").Trim();
                if (string.IsNullOrEmpty(sName)) continue;

                foreach (IniEntry e in s.Entries)
                {
                    if (!e.IsPureComment && string.Equals(e.Key, "MAIN", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals((e.Value ?? "").Trim(), "HTTP_START", StringComparison.OrdinalIgnoreCase))
                    {
                        return sName;
                    }
                }
            }
            return "HTTPJOB";
        }

        private bool VerificarRestAtivoEmOnStart(List<IniSection> allSections, out string restJobName)
        {
            restJobName = ObterNomeJobRest(allSections);
            foreach (IniSection s in allSections)
            {
                if (s.IsCommented) continue;
                if (!string.Equals((s.Name ?? "").Trim(), "ONSTART", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (IniEntry e in s.Entries)
                {
                    if (e.IsPureComment || e.IsCommented) continue;
                    if (string.Equals((e.Key ?? "").Trim(), "jobs", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] items = (e.Value ?? "").Split(',');
                        foreach (string item in items)
                        {
                            string clean = item.Trim();
                            if (string.Equals(clean, restJobName, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(clean, "HTTPJOB", StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private void AlternarEstadoRestEmOnStart(List<IniSection> allSections, bool ativar)
        {
            string jobName = ObterNomeJobRest(allSections);
            IniSection onStartSec = allSections.FirstOrDefault(s =>
                string.Equals((s.Name ?? "").Trim(), "ONSTART", StringComparison.OrdinalIgnoreCase));

            if (!ativar)
            {
                // Inativar REST: NAO comenta [HTTPV11]/[HTTPREST]/[HTTPURI]/[HTTPJOB], vai em [ONSTART]>jobs
                if (onStartSec == null) return;

                List<IniEntry> activeJobsEntries = onStartSec.Entries.Where(e =>
                    !e.IsPureComment && !e.IsCommented &&
                    string.Equals((e.Key ?? "").Trim(), "jobs", StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (IniEntry entry in activeJobsEntries)
                {
                    List<string> jobsList = (entry.Value ?? "")
                        .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(j => j.Trim())
                        .Where(j => !string.IsNullOrEmpty(j))
                        .ToList();

                    bool hasRestJob = jobsList.Any(j =>
                        string.Equals(j, jobName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(j, "HTTPJOB", StringComparison.OrdinalIgnoreCase));

                    if (!hasRestJob) continue;

                    List<string> remaining = jobsList.Where(j =>
                        !string.Equals(j, jobName, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(j, "HTTPJOB", StringComparison.OrdinalIgnoreCase)).ToList();

                    if (remaining.Count == 0)
                    {
                        // Apenas HTTPJOB na linha: comenta a linha (;jobs=HTTPJOB)
                        entry.IsCommented = true;
                    }
                    else
                    {
                        // Tinha mais jobs separados por virgula: mantem a linha ativa apenas com os outros jobs
                        entry.Value = string.Join(",", remaining);
                    }
                }
            }
            else
            {
                // Ativar REST: garante [ONSTART] ativo e jobName na tag jobs
                if (onStartSec == null)
                {
                    onStartSec = new IniSection();
                    onStartSec.Name = "ONSTART";
                    onStartSec.IsCommented = false;

                    IniEntry newJobEntry = new IniEntry();
                    newJobEntry.IsPureComment = false;
                    newJobEntry.IsCommented = false;
                    newJobEntry.Key = "jobs";
                    newJobEntry.Value = jobName;
                    onStartSec.Entries.Add(newJobEntry);

                    IniEntry refreshEntry = new IniEntry();
                    refreshEntry.IsPureComment = false;
                    refreshEntry.IsCommented = false;
                    refreshEntry.Key = "RefreshRate";
                    refreshEntry.Value = "120";
                    onStartSec.Entries.Add(refreshEntry);

                    allSections.Add(onStartSec);
                    return;
                }

                if (onStartSec.IsCommented)
                    onStartSec.IsCommented = false;

                IniEntry activeJobs = onStartSec.Entries.FirstOrDefault(e =>
                    !e.IsPureComment && !e.IsCommented &&
                    string.Equals((e.Key ?? "").Trim(), "jobs", StringComparison.OrdinalIgnoreCase));

                List<IniEntry> commentedJobs = onStartSec.Entries.Where(e =>
                    !e.IsPureComment && e.IsCommented &&
                    string.Equals((e.Key ?? "").Trim(), "jobs", StringComparison.OrdinalIgnoreCase)).ToList();

                if (activeJobs != null)
                {
                    List<string> currentJobs = (activeJobs.Value ?? "")
                        .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(j => j.Trim())
                        .Where(j => !string.IsNullOrEmpty(j))
                        .ToList();

                    if (!currentJobs.Any(j => string.Equals(j, jobName, StringComparison.OrdinalIgnoreCase)))
                        currentJobs.Add(jobName);

                    activeJobs.Value = string.Join(",", currentJobs);

                    // Se havia uma linha separada ;jobs=HTTPJOB comentada, remove para nao duplicar
                    foreach (IniEntry cEntry in commentedJobs)
                    {
                        List<string> cList = (cEntry.Value ?? "")
                            .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(j => j.Trim())
                            .Where(j => !string.IsNullOrEmpty(j))
                            .ToList();
                        if (cList.Count == 1 && (string.Equals(cList[0], jobName, StringComparison.OrdinalIgnoreCase) || string.Equals(cList[0], "HTTPJOB", StringComparison.OrdinalIgnoreCase)))
                        {
                            onStartSec.Entries.Remove(cEntry);
                        }
                    }
                }
                else if (commentedJobs.Count > 0)
                {
                    IniEntry target = commentedJobs[0];
                    target.IsCommented = false;
                    List<string> currentJobs = (target.Value ?? "")
                        .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(j => j.Trim())
                        .Where(j => !string.IsNullOrEmpty(j))
                        .ToList();

                    if (!currentJobs.Any(j => string.Equals(j, jobName, StringComparison.OrdinalIgnoreCase)))
                        currentJobs.Add(jobName);

                    target.Value = string.Join(",", currentJobs);
                }
                else
                {
                    IniEntry newJobEntry = new IniEntry();
                    newJobEntry.IsPureComment = false;
                    newJobEntry.IsCommented = false;
                    newJobEntry.Key = "jobs";
                    newJobEntry.Value = jobName;
                    onStartSec.Entries.Insert(0, newJobEntry);
                }
            }
        }

        private Border BuildRestGroupContainer(
            AmbienteInfo amb,
            List<IniSection> allSections,
            List<KeyValuePair<IniSection, List<IniEntry>>> restItems,
            Action onRefreshList)
        {
            string restJobName;
            bool isRestActive = VerificarRestAtivoEmOnStart(allSections, out restJobName);

            Border groupBorder = new Border();
            groupBorder.Background = UiTheme.BgSectionHeader;
            groupBorder.BorderBrush = isRestActive
                ? UiTheme.BrushFromHex(UiTheme.PrimaryBlueHex)
                : UiTheme.BorderSubtle;
            groupBorder.BorderThickness = new Thickness(1.5);
            groupBorder.CornerRadius = new CornerRadius(12);
            groupBorder.Margin = new Thickness(0, 0, 0, 16);
            groupBorder.Padding = new Thickness(14, 12, 14, 4);

            StackPanel groupStack = new StackPanel();

            DockPanel groupHeader = new DockPanel();
            groupHeader.Margin = new Thickness(2, 0, 2, 12);

            Button btnToggleRest = UiTheme.CreateActionButton(
                isRestActive ? "Inativar REST" : "Ativar REST",
                isRestActive ? "#B45309" : "#059669",
                isRestActive ? "#D97706" : "#10B981");
            btnToggleRest.Padding = new Thickness(12, 5, 12, 5);
            btnToggleRest.FontSize = 11;
            btnToggleRest.ToolTip = isRestActive
                ? string.Format("Remove {0} da tag [ONSTART] > jobs sem comentar as seções REST", restJobName)
                : string.Format("Adiciona/ativa {0} na tag [ONSTART] > jobs", restJobName);
            DockPanel.SetDock(btnToggleRest, Dock.Right);
            btnToggleRest.Click += delegate
            {
                AlternarEstadoRestEmOnStart(allSections, !isRestActive);
                if (onRefreshList != null)
                    onRefreshList();
            };
            groupHeader.Children.Add(btnToggleRest);

            StackPanel titleStack = new StackPanel();
            titleStack.Orientation = Orientation.Horizontal;
            titleStack.VerticalAlignment = VerticalAlignment.Center;

            TextBlock groupTitle = new TextBlock();
            groupTitle.Text = "🌐 Configuração REST";
            groupTitle.Foreground = UiTheme.TextPrimary;
            groupTitle.FontSize = 14;
            groupTitle.FontWeight = FontWeights.Bold;
            groupTitle.VerticalAlignment = VerticalAlignment.Center;
            titleStack.Children.Add(groupTitle);

            Border statusBadge = new Border();
            statusBadge.Background = isRestActive
                ? UiTheme.BrushFromHex("#064E3B")
                : UiTheme.BrushFromHex("#451A03");
            statusBadge.BorderBrush = isRestActive
                ? UiTheme.AccentGreen
                : UiTheme.BrushFromHex("#92400E");
            statusBadge.BorderThickness = new Thickness(1);
            statusBadge.CornerRadius = new CornerRadius(5);
            statusBadge.Padding = new Thickness(8, 2, 8, 2);
            statusBadge.Margin = new Thickness(10, 0, 0, 0);
            statusBadge.VerticalAlignment = VerticalAlignment.Center;

            TextBlock statusTxt = new TextBlock();
            statusTxt.Text = isRestActive
                ? string.Format("REST ATIVO ([ONSTART] > jobs inclui {0})", restJobName)
                : string.Format("REST INATIVO ({0} fora de [ONSTART] > jobs)", restJobName);
            statusTxt.Foreground = isRestActive ? UiTheme.AccentGreen : UiTheme.AccentAmber;
            statusTxt.FontSize = 10;
            statusTxt.FontWeight = FontWeights.Bold;
            statusBadge.Child = statusTxt;
            titleStack.Children.Add(statusBadge);

            groupHeader.Children.Add(titleStack);
            groupStack.Children.Add(groupHeader);

            foreach (KeyValuePair<IniSection, List<IniEntry>> item in restItems)
            {
                groupStack.Children.Add(BuildIniSectionCard(amb, allSections, item.Key, item.Value, onRefreshList));
            }

            groupBorder.Child = groupStack;
            return groupBorder;
        }

        // =====================================================================
        // ITEM 12: AVISO EM TEMPO REAL DE PORTA/CHAVE MONITORADA DUPLICADA (SEM IMPEDIR)
        // =====================================================================
        private string VerificarConflitoValorIni(AmbienteInfo currentAmb, string secName, string keyName, string candidateValue)
        {
            string val = (candidateValue ?? "").Trim();
            string sName = (secName ?? "").Trim();
            string kName = (keyName ?? "").Trim();
            if (string.IsNullOrEmpty(val) || string.IsNullOrEmpty(sName) || string.IsNullOrEmpty(kName))
                return null;

            bool isWebAppPort = string.Equals(sName, "WEBAPP", StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(kName, "Port", StringComparison.OrdinalIgnoreCase);
            bool isTcpPort = string.Equals(sName, "TCP", StringComparison.OrdinalIgnoreCase) &&
                             string.Equals(kName, "Port", StringComparison.OrdinalIgnoreCase);
            bool isRestPort = (string.Equals(sName, "HTTPREST", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(sName, "HTTP", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(sName, "REST", StringComparison.OrdinalIgnoreCase)) &&
                              string.Equals(kName, "Port", StringComparison.OrdinalIgnoreCase);
            bool isDbPort = (string.Equals(sName, "DBACCESS", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(sName, "TOPCONNECT", StringComparison.OrdinalIgnoreCase)) &&
                            (string.Equals(kName, "Port", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(kName, "DBPort", StringComparison.OrdinalIgnoreCase));

            List<ConflictRule> matchingRules = new List<ConflictRule>();
            foreach (ConflictRule r in _conflictRules)
            {
                if (r.IsSelfSection) continue;
                if (!string.Equals(r.KeyName, kName, StringComparison.OrdinalIgnoreCase)) continue;

                bool isEnvPlaceholder = string.Equals(r.SectionPattern, "<ambiente>", StringComparison.OrdinalIgnoreCase);
                if (isEnvPlaceholder && string.Equals(sName, currentAmb.NomeIni ?? "", StringComparison.OrdinalIgnoreCase))
                {
                    matchingRules.Add(r);
                }
                else if (!isEnvPlaceholder && string.Equals(r.SectionPattern, sName, StringComparison.OrdinalIgnoreCase))
                {
                    matchingRules.Add(r);
                }
            }

            if (!isWebAppPort && !isTcpPort && !isRestPort && !isDbPort && matchingRules.Count == 0)
                return null;

            List<string> conflitos = new List<string>();
            foreach (AmbienteInfo other in _ambientes)
            {
                if (other == null || other == currentAmb) continue;
                if (string.Equals(other.AppDir, currentAmb.AppDir, StringComparison.OrdinalIgnoreCase)) continue;
                if (_hiddenDirs.Contains(other.AppDir)) continue;
                if (_ignoreSameSubfolderConflicts && string.Equals(ObterCaminhoSubpastaBase(other), ObterCaminhoSubpastaBase(currentAmb), StringComparison.OrdinalIgnoreCase)) continue;

                bool matchOther = false;

                if (isWebAppPort && string.Equals(other.WebAppPort, val, StringComparison.OrdinalIgnoreCase))
                    matchOther = true;
                else if (isTcpPort && string.Equals(other.TcpPort, val, StringComparison.OrdinalIgnoreCase))
                    matchOther = true;
                else if (isRestPort && string.Equals(other.RestPort, val, StringComparison.OrdinalIgnoreCase))
                    matchOther = true;
                else if (isDbPort && string.Equals(other.DBPort, val, StringComparison.OrdinalIgnoreCase))
                    matchOther = true;

                if (!matchOther && other.IniSectionsMap != null)
                {
                    Dictionary<string, string> otherSec;
                    string otherVal;
                    if (other.IniSectionsMap.TryGetValue(sName, out otherSec) &&
                        otherSec.TryGetValue(kName, out otherVal) &&
                        string.Equals((otherVal ?? "").Trim(), val, StringComparison.OrdinalIgnoreCase))
                    {
                        matchOther = true;
                    }

                    if (!matchOther)
                    {
                        foreach (ConflictRule r in matchingRules)
                        {
                            bool isEnvPlaceholder = string.Equals(r.SectionPattern, "<ambiente>", StringComparison.OrdinalIgnoreCase);
                            string targetSec = isEnvPlaceholder ? other.NomeIni : r.SectionPattern;
                            if (!string.IsNullOrEmpty(targetSec) &&
                                other.IniSectionsMap.TryGetValue(targetSec, out otherSec) &&
                                otherSec.TryGetValue(r.KeyName, out otherVal) &&
                                string.Equals((otherVal ?? "").Trim(), val, StringComparison.OrdinalIgnoreCase))
                            {
                                matchOther = true;
                                break;
                            }
                        }
                    }
                }

                if (matchOther)
                {
                    string label = !string.IsNullOrEmpty(other.PastaBase) && !string.Equals(other.NomeIni, other.PastaBase, StringComparison.OrdinalIgnoreCase)
                        ? string.Format("{0} ({1})", other.NomeIni, other.PastaBase)
                        : (other.NomeIni ?? other.PastaBase);
                    if (!conflitos.Contains(label, StringComparer.OrdinalIgnoreCase))
                        conflitos.Add(label);
                }
            }

            if (conflitos.Count > 0)
            {
                return "⚠ Porta/valor já utilizado em: " + string.Join(", ", conflitos);
            }
            return null;
        }

        private UIElement BuildIniEntryRow(AmbienteInfo amb, IniSection sec, IniEntry entry)
        {
            Border rowBorder = new Border();
            rowBorder.Background = UiTheme.BgInput;
            rowBorder.BorderBrush = UiTheme.BorderSubtle;
            rowBorder.BorderThickness = new Thickness(1);
            rowBorder.CornerRadius = new CornerRadius(6);
            rowBorder.Padding = new Thickness(10, 6, 8, 6);
            rowBorder.Margin = new Thickness(0, 0, 0, 6);
            entry.RowContainer = rowBorder;

            if (entry.IsPureComment)
            {
                DockPanel commentDock = new DockPanel();

                Button btnDelNote = UiTheme.CreateActionButton("✕", UiTheme.NeutralBtnHex, "#DC2626");
                btnDelNote.Padding = new Thickness(7, 3, 7, 3);
                btnDelNote.FontSize = 11;
                btnDelNote.Margin = new Thickness(8, 0, 0, 0);
                DockPanel.SetDock(btnDelNote, Dock.Right);
                btnDelNote.Click += delegate
                {
                    sec.Entries.Remove(entry);
                    sec.EntriesPanel.Children.Remove(rowBorder);
                };

                TextBlock prefixSemi = new TextBlock();
                prefixSemi.Text = "; ";
                prefixSemi.Foreground = UiTheme.AccentAmber;
                prefixSemi.FontWeight = FontWeights.Bold;
                prefixSemi.FontSize = 13;
                prefixSemi.VerticalAlignment = VerticalAlignment.Center;
                DockPanel.SetDock(prefixSemi, Dock.Left);

                TextBox noteBox = new TextBox();
                noteBox.Text = entry.Value;
                noteBox.Foreground = UiTheme.TextSecondary;
                noteBox.Background = UiTheme.BgCard;
                noteBox.CaretBrush = UiTheme.TextPrimary;
                noteBox.BorderBrush = UiTheme.BorderSubtle;
                noteBox.BorderThickness = new Thickness(1);
                noteBox.Padding = new Thickness(7, 4, 7, 4);
                noteBox.FontStyle = FontStyles.Italic;
                noteBox.FontSize = 12;
                noteBox.VerticalAlignment = VerticalAlignment.Center;
                noteBox.TextChanged += delegate
                {
                    entry.Value = noteBox.Text.TrimStart(';').Trim();
                };

                commentDock.Children.Add(btnDelNote);
                commentDock.Children.Add(prefixSemi);
                commentDock.Children.Add(noteBox);
                rowBorder.Child = commentDock;
                return rowBorder;
            }

            Grid rowGrid = new Grid();
            ColumnDefinition cPrefix = new ColumnDefinition(); cPrefix.Width = GridLength.Auto;
            ColumnDefinition cKey = new ColumnDefinition(); cKey.Width = new GridLength(220);
            ColumnDefinition cEq = new ColumnDefinition(); cEq.Width = GridLength.Auto;
            ColumnDefinition cVal = new ColumnDefinition(); cVal.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition cBtns = new ColumnDefinition(); cBtns.Width = GridLength.Auto;

            rowGrid.ColumnDefinitions.Add(cPrefix);
            rowGrid.ColumnDefinitions.Add(cKey);
            rowGrid.ColumnDefinitions.Add(cEq);
            rowGrid.ColumnDefinitions.Add(cVal);
            rowGrid.ColumnDefinitions.Add(cBtns);

            TextBlock prefix = new TextBlock();
            prefix.Text = "; ";
            prefix.Foreground = UiTheme.AccentAmber;
            prefix.FontWeight = FontWeights.Bold;
            prefix.FontSize = 13;
            prefix.VerticalAlignment = VerticalAlignment.Center;
            entry.CommentPrefixLabel = prefix;
            Grid.SetColumn(prefix, 0);
            rowGrid.Children.Add(prefix);

            TextBox keyBox = new TextBox();
            keyBox.Text = entry.Key;
            keyBox.Foreground = UiTheme.TextPrimary;
            keyBox.Background = UiTheme.BgCard;
            keyBox.CaretBrush = UiTheme.TextPrimary;
            keyBox.BorderBrush = UiTheme.BorderSubtle;
            keyBox.BorderThickness = new Thickness(1);
            keyBox.Padding = new Thickness(7, 4, 7, 4);
            keyBox.FontSize = 12;
            keyBox.FontWeight = FontWeights.SemiBold;
            keyBox.VerticalAlignment = VerticalAlignment.Center;
            keyBox.TextChanged += delegate { entry.Key = keyBox.Text.Trim(); };
            entry.KeyBox = keyBox;
            Grid.SetColumn(keyBox, 1);
            rowGrid.Children.Add(keyBox);

            TextBlock eqLbl = new TextBlock();
            eqLbl.Text = " = ";
            eqLbl.Foreground = UiTheme.TextMuted;
            eqLbl.FontWeight = FontWeights.Bold;
            eqLbl.FontSize = 13;
            eqLbl.VerticalAlignment = VerticalAlignment.Center;
            eqLbl.Margin = new Thickness(4, 0, 4, 0);
            Grid.SetColumn(eqLbl, 2);
            rowGrid.Children.Add(eqLbl);

            // Valor com icone de pasta para ConsoleFile, StartPath, RootPath, RPOCustom, SourcePath
            StackPanel valContainer = new StackPanel();
            valContainer.Margin = new Thickness(0, 0, 8, 0);
            valContainer.VerticalAlignment = VerticalAlignment.Center;

            DockPanel valDock = new DockPanel();

            TextBox valBox = new TextBox();
            valBox.Text = entry.Value;
            valBox.Foreground = UiTheme.TextPrimary;
            valBox.Background = UiTheme.BgCard;
            valBox.CaretBrush = UiTheme.TextPrimary;
            valBox.BorderBrush = UiTheme.BorderSubtle;
            valBox.BorderThickness = new Thickness(1);
            valBox.Padding = new Thickness(7, 4, 7, 4);
            valBox.FontSize = 12;
            valBox.VerticalAlignment = VerticalAlignment.Center;
            entry.ValueBox = valBox;

            TextBlock lblConflictWarn = new TextBlock();
            lblConflictWarn.Foreground = UiTheme.AccentRed;
            lblConflictWarn.FontSize = 11;
            lblConflictWarn.FontWeight = FontWeights.SemiBold;
            lblConflictWarn.Margin = new Thickness(2, 3, 0, 0);
            lblConflictWarn.TextWrapping = TextWrapping.Wrap;
            lblConflictWarn.Visibility = Visibility.Collapsed;

            Action updateConflictState = delegate
            {
                string warnMsg = null;
                if (!entry.IsCommented && !sec.IsCommented)
                {
                    warnMsg = VerificarConflitoValorIni(amb, sec.Name, entry.Key, valBox.Text);
                }

                if (!string.IsNullOrEmpty(warnMsg))
                {
                    valBox.BorderBrush = UiTheme.AccentRed;
                    valBox.Background = UiTheme.BrushFromHex(UiTheme.IsDark ? "#2A1215" : "#FEF2F2");
                    lblConflictWarn.Text = warnMsg;
                    lblConflictWarn.Visibility = Visibility.Visible;
                }
                else
                {
                    valBox.BorderBrush = UiTheme.BorderSubtle;
                    valBox.Background = UiTheme.BgCard;
                    lblConflictWarn.Visibility = Visibility.Collapsed;
                }
            };

            valBox.TextChanged += delegate
            {
                entry.Value = valBox.Text;
                updateConflictState();
            };

            Button btnPickPath = UiTheme.CreateActionButton(
                UiTheme.CreateFolderIcon(UiTheme.AccentLightBlue, 14),
                UiTheme.NeutralBtnHex,
                UiTheme.NeutralBtnHoverHex);
            btnPickPath.Width = 30;
            btnPickPath.Padding = new Thickness(0, 4, 0, 4);
            btnPickPath.Margin = new Thickness(5, 0, 0, 0);
            btnPickPath.ToolTip = "Abrir pasta no Explorer / Selecionar caminho atual";
            btnPickPath.Visibility = PathTagKeys.Contains(entry.Key ?? "") ? Visibility.Visible : Visibility.Collapsed;
            DockPanel.SetDock(btnPickPath, Dock.Right);

            btnPickPath.Click += delegate
            {
                AbrirSeletorCaminhoPreCarregado(amb, sec, entry.Key, valBox);
            };
            btnPickPath.MouseRightButtonUp += delegate
            {
                AbrirPastaNoExplorerDireto(amb, sec, valBox.Text);
            };

            keyBox.TextChanged += delegate
            {
                entry.Key = keyBox.Text.Trim();
                btnPickPath.Visibility = PathTagKeys.Contains(entry.Key ?? "") ? Visibility.Visible : Visibility.Collapsed;
                updateConflictState();
            };

            valDock.Children.Add(btnPickPath);
            valDock.Children.Add(valBox);
            valContainer.Children.Add(valDock);
            valContainer.Children.Add(lblConflictWarn);
            Grid.SetColumn(valContainer, 3);
            rowGrid.Children.Add(valContainer);

            StackPanel rowBtns = new StackPanel();
            rowBtns.Orientation = Orientation.Horizontal;
            rowBtns.VerticalAlignment = VerticalAlignment.Center;

            Button btnComment = UiTheme.CreateActionButton(";", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
            btnComment.Padding = new Thickness(9, 4, 9, 4);
            btnComment.FontSize = 12;
            btnComment.FontWeight = FontWeights.Bold;
            btnComment.Margin = new Thickness(0, 0, 5, 0);
            btnComment.Click += delegate
            {
                entry.IsCommented = !entry.IsCommented;
                if (!entry.IsCommented && sec.IsCommented)
                {
                    sec.IsCommented = false;
                }
                AtualizarVisualIniSecao(sec, ProtectedIniSections.Contains(sec.Name ?? ""));
                updateConflictState();
            };
            entry.BtnToggleComment = btnComment;

            Button btnDel = UiTheme.CreateActionButton("✕", UiTheme.NeutralBtnHex, "#DC2626");
            btnDel.Padding = new Thickness(8, 4, 8, 4);
            btnDel.FontSize = 11;
            btnDel.Click += delegate
            {
                sec.Entries.Remove(entry);
                sec.EntriesPanel.Children.Remove(rowBorder);
            };

            rowBtns.Children.Add(btnComment);
            rowBtns.Children.Add(btnDel);
            Grid.SetColumn(rowBtns, 4);
            rowGrid.Children.Add(rowBtns);

            valBox.Tag = updateConflictState;
            rowBorder.Child = rowGrid;
            AtualizarVisualIniLinha(entry);
            updateConflictState();
            return rowBorder;
        }

        private string ResolverDiretorioTagIni(AmbienteInfo amb, IniSection sec, string currentVal)
        {
            string resolvedDir = amb.AppDir;
            try
            {
                string rootPathFromSec = null;
                if (sec != null)
                {
                    foreach (IniEntry e in sec.Entries)
                    {
                        if (!e.IsPureComment && !e.IsCommented && string.Equals(e.Key, "RootPath", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(e.Value))
                        {
                            rootPathFromSec = e.Value.Trim().Trim('"');
                            break;
                        }
                    }
                }
                if (string.IsNullOrEmpty(rootPathFromSec))
                {
                    rootPathFromSec = ObterValorChaveIni(amb, amb.NomeIni, "RootPath") ?? amb.AppDir;
                }

                if (!string.IsNullOrEmpty(currentVal))
                {
                    string candidate = currentVal.Trim().Trim('"');
                    if (candidate.StartsWith("\\") && !candidate.StartsWith("\\\\"))
                    {
                        string withRoot = Path.Combine(rootPathFromSec.TrimEnd('\\', '/'), candidate.TrimStart('\\', '/'));
                        if (Directory.Exists(withRoot) || File.Exists(withRoot))
                            candidate = withRoot;
                        else
                            candidate = Path.Combine(amb.AppDir.TrimEnd('\\', '/'), candidate.TrimStart('\\', '/'));
                    }
                    else if (!Path.IsPathRooted(candidate))
                    {
                        candidate = Path.GetFullPath(Path.Combine(amb.AppDir, candidate.TrimStart('\\', '/')));
                    }

                    if (File.Exists(candidate))
                        resolvedDir = Path.GetDirectoryName(candidate);
                    else if (Directory.Exists(candidate))
                        resolvedDir = candidate;
                    else
                    {
                        string parent = Path.GetDirectoryName(candidate);
                        if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                            resolvedDir = parent;
                        else if (Directory.Exists(rootPathFromSec))
                            resolvedDir = rootPathFromSec;
                    }
                }
            }
            catch { }
            return resolvedDir;
        }

        private void AbrirPastaNoExplorerDireto(AmbienteInfo amb, IniSection sec, string rawVal)
        {
            try
            {
                string dir = ResolverDiretorioTagIni(amb, sec, rawVal);
                if (Directory.Exists(dir))
                    Process.Start("explorer.exe", dir);
            }
            catch { }
        }

        private void AbrirSeletorCaminhoPreCarregado(AmbienteInfo amb, IniSection sec, string keyName, TextBox targetBox)
        {
            string currentVal = (targetBox.Text ?? "").Trim();
            bool isFileKey = string.Equals(keyName, "ConsoleFile", StringComparison.OrdinalIgnoreCase);
            string resolvedDir = ResolverDiretorioTagIni(amb, sec, currentVal);

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = string.Format("Selecionar {0}", keyName);
            dlg.CheckPathExists = true;
            if (Directory.Exists(resolvedDir))
                dlg.InitialDirectory = resolvedDir;

            if (isFileKey)
            {
                dlg.Filter = "Arquivos de Log (*.log)|*.log|Todos os arquivos (*.*)|*.*";
                dlg.CheckFileExists = false;
                if (!string.IsNullOrEmpty(currentVal))
                    dlg.FileName = Path.GetFileName(currentVal);
            }
            else
            {
                dlg.Filter = "Pastas|*.none";
                dlg.FileName = "Selecionar esta pasta";
                dlg.CheckFileExists = false;
            }

            if (dlg.ShowDialog() == true)
            {
                if (isFileKey)
                {
                    targetBox.Text = dlg.FileName;
                }
                else
                {
                    string chosenFolder = Path.GetDirectoryName(dlg.FileName);
                    if (!string.IsNullOrEmpty(chosenFolder))
                        targetBox.Text = chosenFolder;
                }
            }
        }

        private void AtualizarVisualIniSecao(IniSection sec, bool isProtectedSection)
        {
            if (sec.SectionCard == null) return;

            if (sec.IsCommented)
            {
                sec.SectionCard.Background = UiTheme.BgCardDim;
                sec.SectionCard.BorderBrush = UiTheme.BrushFromHex("#92400E");
                sec.HeaderBar.Background = UiTheme.BgCardDim;
                sec.HeaderCommentPrefix.Visibility = Visibility.Visible;
                sec.HiddenBadge.Visibility = Visibility.Visible;
                if (sec.SectionNameBox != null)
                    sec.SectionNameBox.Foreground = UiTheme.TextMuted;
                sec.BtnToggleSection.Content = "✓ Descomentar Seção";
                sec.BtnToggleSection.Background = UiTheme.BrushFromHex("#92400E");
            }
            else
            {
                sec.SectionCard.Background = UiTheme.BgCard;
                sec.SectionCard.BorderBrush = UiTheme.BorderSubtle;
                sec.HeaderBar.Background = UiTheme.BgSectionHeader;
                sec.HeaderCommentPrefix.Visibility = Visibility.Collapsed;
                sec.HiddenBadge.Visibility = Visibility.Collapsed;
                if (sec.SectionNameBox != null)
                    sec.SectionNameBox.Foreground = UiTheme.AccentLightBlue;
                sec.BtnToggleSection.Content = "; Comentar Seção";
                sec.BtnToggleSection.Background = UiTheme.BrushFromHex(UiTheme.NeutralBtnHex);
            }

            foreach (IniEntry entry in sec.Entries)
            {
                AtualizarVisualIniLinha(entry);
            }
        }

        private void AtualizarVisualIniLinha(IniEntry entry)
        {
            if (entry.IsPureComment || entry.RowContainer == null) return;

            if (entry.IsCommented)
            {
                entry.CommentPrefixLabel.Visibility = Visibility.Visible;
                entry.RowContainer.Opacity = 0.65;
                entry.KeyBox.Foreground = UiTheme.TextMuted;
                entry.ValueBox.Foreground = UiTheme.TextMuted;
                entry.BtnToggleComment.Background = UiTheme.BrushFromHex("#92400E");
            }
            else
            {
                entry.CommentPrefixLabel.Visibility = Visibility.Collapsed;
                entry.RowContainer.Opacity = 1.0;
                entry.KeyBox.Foreground = UiTheme.TextPrimary;
                entry.ValueBox.Foreground = UiTheme.TextPrimary;
                entry.BtnToggleComment.Background = UiTheme.BrushFromHex(UiTheme.NeutralBtnHex);
            }

            if (entry.ValueBox != null)
            {
                Action updateConflict = entry.ValueBox.Tag as Action;
                if (updateConflict != null)
                    updateConflict();
            }
        }

        private bool GravarArquivoIni(string iniPath, List<IniSection> sections, bool criarCopia)
        {
            try
            {
                if (criarCopia && File.Exists(iniPath))
                {
                    File.Copy(iniPath, iniPath + ".bak", true);
                }

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < sections.Count; i++)
                {
                    IniSection sec = sections[i];
                    if (string.IsNullOrWhiteSpace(sec.Name))
                        continue;

                    if (i > 0)
                        sb.AppendLine();

                    string secPrefix = sec.IsCommented ? ";" : "";
                    sb.AppendLine(string.Format("{0}[{1}]", secPrefix, sec.Name.Trim()));

                    foreach (IniEntry entry in sec.Entries)
                    {
                        if (entry.IsPureComment)
                        {
                            if (!string.IsNullOrWhiteSpace(entry.Value))
                                sb.AppendLine("; " + entry.Value.Trim());
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(entry.Key))
                            continue;

                        bool commentLine = sec.IsCommented || entry.IsCommented;
                        string linePrefix = commentLine ? ";" : "";
                        sb.AppendLine(string.Format("{0}{1}={2}", linePrefix, entry.Key.Trim(), entry.Value ?? ""));
                    }
                }

                File.WriteAllText(iniPath, sb.ToString(), Encoding.Default);
                return true;
            }
            catch (Exception ex)
            {
                StyledDialog.Info(this, "Erro ao Salvar", "Não foi possível gravar o arquivo .ini.", ex.Message);
                return false;
            }
        }
    }
}
