using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        // =====================================================================
        // CADASTRO DE DIRETORIO INICIAL (ONBOARDING QUANDO C:\bases NAO EXISTE)
        // =====================================================================
        private void ShowOnboardingView()
        {
            _isInDashboardView = false;

            Grid rootGrid = new Grid();
            RowDefinition rowTop = new RowDefinition(); rowTop.Height = GridLength.Auto;
            RowDefinition rowBody = new RowDefinition(); rowBody.Height = new GridLength(1, GridUnitType.Star);
            rootGrid.RowDefinitions.Add(rowTop);
            rootGrid.RowDefinitions.Add(rowBody);

            StackPanel topLeft = CreateBrandTitleHeader();
            Border topBar = BuildTopWindowBar(topLeft);
            Grid.SetRow(topBar, 0);
            rootGrid.Children.Add(topBar);

            Grid centerHost = new Grid();
            centerHost.Margin = new Thickness(24);
            Grid.SetRow(centerHost, 1);

            Border card = new Border();
            card.MaxWidth = 560;
            card.HorizontalAlignment = HorizontalAlignment.Center;
            card.VerticalAlignment = VerticalAlignment.Center;
            card.Background = UiTheme.BgCard;
            card.BorderBrush = UiTheme.BorderSubtle;
            card.BorderThickness = new Thickness(1);
            card.CornerRadius = new CornerRadius(16);
            card.Padding = new Thickness(38, 36, 38, 34);

            StackPanel stack = new StackPanel();
            stack.HorizontalAlignment = HorizontalAlignment.Stretch;

            // Badge superior com a logo branca TOTVS
            Border logoBadge = new Border();
            logoBadge.Width = 68;
            logoBadge.Height = 68;
            logoBadge.CornerRadius = new CornerRadius(18);
            logoBadge.Background = UiTheme.BrushFromHex("#0F172A");
            logoBadge.BorderThickness = new Thickness(0);
            logoBadge.HorizontalAlignment = HorizontalAlignment.Center;
            logoBadge.Margin = new Thickness(0, 0, 0, 20);
            logoBadge.Child = TotvsLogo.CreateTotvsWhiteSvgLogo(_cachedSvgPath, 38);
            stack.Children.Add(logoBadge);

            TextBlock lblTitle = new TextBlock();
            lblTitle.Text = "Qual pasta raíz guarda seus ambientes?";
            lblTitle.Foreground = UiTheme.TextPrimary;
            lblTitle.FontSize = 22;
            lblTitle.FontWeight = FontWeights.Bold;
            lblTitle.HorizontalAlignment = HorizontalAlignment.Center;
            lblTitle.TextAlignment = TextAlignment.Center;
            lblTitle.Margin = new Thickness(0, 0, 0, 24);
            stack.Children.Add(lblTitle);

            TextBlock lblField = new TextBlock();
            lblField.Text = "DIRETÓRIO DOS AMBIENTES";
            lblField.Foreground = UiTheme.TextMuted;
            lblField.FontSize = 11;
            lblField.FontWeight = FontWeights.Bold;
            lblField.Margin = new Thickness(2, 0, 0, 6);
            stack.Children.Add(lblField);

            Grid inputGrid = new Grid();
            ColumnDefinition colTxt = new ColumnDefinition(); colTxt.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition colBtn = new ColumnDefinition(); colBtn.Width = GridLength.Auto;
            inputGrid.ColumnDefinitions.Add(colTxt);
            inputGrid.ColumnDefinitions.Add(colBtn);

            Border boxBorder = new Border();
            boxBorder.Background = UiTheme.BgInput;
            boxBorder.BorderBrush = UiTheme.BorderSubtle;
            boxBorder.BorderThickness = new Thickness(1);
            boxBorder.CornerRadius = new CornerRadius(8);
            boxBorder.Padding = new Thickness(12, 8, 12, 8);
            boxBorder.Margin = new Thickness(0, 0, 10, 0);

            Grid boxInner = new Grid();
            TextBlock placeholder = new TextBlock();
            placeholder.Text = "Ex: C:\\TOTVS\\bases ou selecione ao lado...";
            placeholder.Foreground = UiTheme.TextMuted;
            placeholder.FontSize = 13;
            placeholder.VerticalAlignment = VerticalAlignment.Center;
            placeholder.IsHitTestVisible = false;

            TextBox txtPath = new TextBox();
            txtPath.Background = Brushes.Transparent;
            txtPath.Foreground = UiTheme.TextPrimary;
            txtPath.CaretBrush = UiTheme.TextPrimary;
            txtPath.BorderThickness = new Thickness(0);
            txtPath.FontSize = 13;
            txtPath.VerticalAlignment = VerticalAlignment.Center;

            boxInner.Children.Add(placeholder);
            boxInner.Children.Add(txtPath);
            boxBorder.Child = boxInner;
            Grid.SetColumn(boxBorder, 0);
            inputGrid.Children.Add(boxBorder);

            StackPanel browseContent = new StackPanel();
            browseContent.Orientation = Orientation.Horizontal;
            browseContent.VerticalAlignment = VerticalAlignment.Center;
            browseContent.Children.Add(UiTheme.CreateFolderIcon(Brushes.White, 15));
            TextBlock browseLbl = new TextBlock();
            browseLbl.Text = "Selecionar pasta";
            browseLbl.Foreground = Brushes.White;
            browseLbl.FontSize = 12;
            browseLbl.FontWeight = FontWeights.SemiBold;
            browseLbl.Margin = new Thickness(7, 0, 0, 0);
            browseLbl.VerticalAlignment = VerticalAlignment.Center;
            browseContent.Children.Add(browseLbl);

            Button btnBrowse = UiTheme.CreateActionButton(browseContent, UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
            btnBrowse.Height = 38;
            btnBrowse.Padding = new Thickness(14, 0, 14, 0);
            Grid.SetColumn(btnBrowse, 1);
            inputGrid.Children.Add(btnBrowse);

            stack.Children.Add(inputGrid);

            TextBlock lblError = new TextBlock();
            lblError.Foreground = UiTheme.AccentRed;
            lblError.FontSize = 12;
            lblError.FontWeight = FontWeights.SemiBold;
            lblError.Margin = new Thickness(2, 8, 0, 0);
            lblError.TextWrapping = TextWrapping.Wrap;
            lblError.Visibility = Visibility.Collapsed;
            stack.Children.Add(lblError);

            txtPath.TextChanged += delegate
            {
                placeholder.Visibility = string.IsNullOrEmpty(txtPath.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                lblError.Visibility = Visibility.Collapsed;
            };

            btnBrowse.Click += delegate
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "Selecione a pasta que guarda seus ambientes TOTVS";
                dlg.Filter = "Pastas|*.none";
                dlg.FileName = "Selecionar esta pasta";
                dlg.CheckFileExists = false;
                dlg.CheckPathExists = true;

                string currentTyped = (txtPath.Text ?? "").Trim().Trim('"');
                if (!string.IsNullOrEmpty(currentTyped) && Directory.Exists(currentTyped))
                    dlg.InitialDirectory = currentTyped;

                if (dlg.ShowDialog() == true)
                {
                    string chosen = Path.GetDirectoryName(dlg.FileName);
                    if (!string.IsNullOrEmpty(chosen) && Directory.Exists(chosen))
                    {
                        txtPath.Text = chosen;
                        lblError.Visibility = Visibility.Collapsed;
                    }
                }
            };

            Action confirmarDiretorio = delegate
            {
                string candidate = (txtPath.Text ?? "").Trim().Trim('"');
                if (string.IsNullOrEmpty(candidate))
                {
                    lblError.Text = "Selecione ou informe a pasta onde seus ambientes ficam armazenados.";
                    lblError.Visibility = Visibility.Visible;
                    return;
                }

                if (!Directory.Exists(candidate))
                {
                    lblError.Text = "A pasta informada não existe neste computador. Selecione uma pasta válida.";
                    lblError.Visibility = Visibility.Visible;
                    return;
                }

                ConcluirOnboardingDiretorio(candidate);
            };

            txtPath.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter)
                {
                    confirmarDiretorio();
                }
            };

            Button btnContinue = UiTheme.CreateActionButton("Começar", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
            btnContinue.Height = 40;
            btnContinue.Padding = new Thickness(40, 8, 40, 8);
            btnContinue.FontSize = 14;
            btnContinue.Margin = new Thickness(0, 22, 0, 0);
            btnContinue.HorizontalAlignment = HorizontalAlignment.Center;
            btnContinue.Click += delegate { confirmarDiretorio(); };
            stack.Children.Add(btnContinue);

            card.Child = stack;
            centerHost.Children.Add(card);
            rootGrid.Children.Add(centerHost);
            _pageHost.Content = rootGrid;
        }

        private void ConcluirOnboardingDiretorio(string chosenDir)
        {
            _rootPath = chosenDir;
            _needsOnboarding = false;
            _isInitialScanCompleted = false;

            try
            {
                File.WriteAllText(_configPath, _rootPath, Encoding.UTF8);
            }
            catch { }

            // Exibe imediatamente a tela de dashboard com a animacao vetorial (CreateVectorLoadingScreen)
            // e dispara a primeira varredura antes de exibir os cards
            ShowDashboardView();
            CarregarAmbientesAsync(false);
        }
    }
}
