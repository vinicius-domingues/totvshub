using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
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

            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.Padding = new Thickness(24);
            Grid.SetRow(scroll, 1);

            Border card = new Border();
            card.MaxWidth = 620;
            card.HorizontalAlignment = HorizontalAlignment.Center;
            card.VerticalAlignment = VerticalAlignment.Center;
            card.Background = UiTheme.BgCard;
            card.BorderBrush = UiTheme.BorderSubtle;
            card.BorderThickness = new Thickness(1);
            card.CornerRadius = new CornerRadius(16);
            card.Padding = new Thickness(36, 32, 36, 32);

            StackPanel stack = new StackPanel();
            stack.HorizontalAlignment = HorizontalAlignment.Stretch;

            Border logoBadge = new Border();
            logoBadge.Width = 64;
            logoBadge.Height = 64;
            logoBadge.CornerRadius = new CornerRadius(18);
            logoBadge.Background = UiTheme.BrushFromHex("#0F172A");
            logoBadge.BorderThickness = new Thickness(0);
            logoBadge.HorizontalAlignment = HorizontalAlignment.Center;
            logoBadge.Margin = new Thickness(0, 0, 0, 16);
            logoBadge.Child = TotvsLogo.CreateTotvsWhiteSvgLogo(_cachedSvgPath, 36);
            stack.Children.Add(logoBadge);

            TextBlock lblTitle = new TextBlock();
            lblTitle.Text = "Configuração Inicial";
            lblTitle.Foreground = UiTheme.TextPrimary;
            lblTitle.FontSize = 22;
            lblTitle.FontWeight = FontWeights.Bold;
            lblTitle.HorizontalAlignment = HorizontalAlignment.Center;
            lblTitle.TextAlignment = TextAlignment.Center;
            lblTitle.Margin = new Thickness(0, 0, 0, 6);
            stack.Children.Add(lblTitle);

            TextBlock lblSubtitle = new TextBlock();
            lblSubtitle.Text = "Informe os diretórios dos serviços para iniciar o monitoramento e gerenciamento integrado.";
            lblSubtitle.Foreground = UiTheme.TextSecondary;
            lblSubtitle.FontSize = 13;
            lblSubtitle.TextAlignment = TextAlignment.Center;
            lblSubtitle.TextWrapping = TextWrapping.Wrap;
            lblSubtitle.Margin = new Thickness(0, 0, 0, 24);
            stack.Children.Add(lblSubtitle);

            TextBox txtAppServer;
            TextBlock errAppServer;
            stack.Children.Add(CriarLinhaOnboardingInput(
                "PASTA RAIZ DOS AMBIENTES PROTHEUS (APPSERVER) *",
                "Ex: C:\\bases ou selecione a pasta com os ambientes...",
                Directory.Exists(@"C:\bases") ? @"C:\bases" : (Directory.Exists(_rootPath) ? _rootPath : @"C:\bases"),
                out txtAppServer,
                out errAppServer));

            TextBox txtDbAccess;
            TextBlock errDbAccess;
            string defaultDbCandidate = @"C:\totvs\dbaccess";
            if (!Directory.Exists(defaultDbCandidate) && Directory.Exists(@"C:\dbaccess"))
                defaultDbCandidate = @"C:\dbaccess";
            stack.Children.Add(CriarLinhaOnboardingInput(
                "PASTA DO DBACCESS (ONDE FICA DBACCESS.INI)",
                "Ex: C:\\totvs\\dbaccess (opcional, pode configurar depois)",
                Directory.Exists(defaultDbCandidate) ? defaultDbCandidate : "",
                out txtDbAccess,
                out errDbAccess));

            TextBox txtSmartView;
            TextBlock errSmartView;
            string defaultSvCandidate = @"C:\TOTVS\smart-view";
            stack.Children.Add(CriarLinhaOnboardingInput(
                "PASTA DO SMARTVIEW",
                "Ex: C:\\TOTVS\\smart-view (opcional, detectado pelas portas 7017/7019)",
                Directory.Exists(defaultSvCandidate) ? defaultSvCandidate : "",
                out txtSmartView,
                out errSmartView));

            TextBlock lblGeneralError = new TextBlock();
            lblGeneralError.Foreground = UiTheme.AccentRed;
            lblGeneralError.FontSize = 12;
            lblGeneralError.FontWeight = FontWeights.SemiBold;
            lblGeneralError.Margin = new Thickness(2, 6, 0, 0);
            lblGeneralError.TextWrapping = TextWrapping.Wrap;
            lblGeneralError.Visibility = Visibility.Collapsed;
            stack.Children.Add(lblGeneralError);

            Button btnContinue = UiTheme.CreateActionButton("Começar a Usar o TOTVSHub", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
            btnContinue.Height = 42;
            btnContinue.Padding = new Thickness(36, 8, 36, 8);
            btnContinue.FontSize = 14;
            btnContinue.FontWeight = FontWeights.Bold;
            btnContinue.Margin = new Thickness(0, 22, 0, 0);
            btnContinue.HorizontalAlignment = HorizontalAlignment.Center;
            btnContinue.Click += delegate
            {
                ConfirmarOnboardingTudo(txtAppServer.Text, txtDbAccess.Text, txtSmartView.Text, lblGeneralError);
            };
            stack.Children.Add(btnContinue);

            card.Child = stack;
            scroll.Content = card;
            rootGrid.Children.Add(scroll);
            _pageHost.Content = rootGrid;
        }

        private UIElement CriarLinhaOnboardingInput(
            string labelText,
            string placeholderText,
            string initialValue,
            out TextBox txtOut,
            out TextBlock errOut)
        {
            StackPanel group = new StackPanel();
            group.Margin = new Thickness(0, 0, 0, 16);

            TextBlock lbl = new TextBlock();
            lbl.Text = labelText;
            lbl.Foreground = UiTheme.TextMuted;
            lbl.FontSize = 11;
            lbl.FontWeight = FontWeights.Bold;
            lbl.Margin = new Thickness(2, 0, 0, 6);
            group.Children.Add(lbl);

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
            placeholder.Text = placeholderText;
            placeholder.Foreground = UiTheme.TextMuted;
            placeholder.FontSize = 12;
            placeholder.VerticalAlignment = VerticalAlignment.Center;
            placeholder.IsHitTestVisible = false;

            TextBox txt = new TextBox();
            txt.Text = initialValue ?? "";
            txt.Background = Brushes.Transparent;
            txt.Foreground = UiTheme.TextPrimary;
            txt.CaretBrush = UiTheme.TextPrimary;
            txt.BorderThickness = new Thickness(0);
            txt.FontSize = 13;
            txt.VerticalAlignment = VerticalAlignment.Center;

            placeholder.Visibility = string.IsNullOrEmpty(txt.Text) ? Visibility.Visible : Visibility.Collapsed;
            txt.TextChanged += delegate
            {
                placeholder.Visibility = string.IsNullOrEmpty(txt.Text) ? Visibility.Visible : Visibility.Collapsed;
            };

            boxInner.Children.Add(placeholder);
            boxInner.Children.Add(txt);
            boxBorder.Child = boxInner;
            Grid.SetColumn(boxBorder, 0);
            inputGrid.Children.Add(boxBorder);

            StackPanel browseContent = new StackPanel();
            browseContent.Orientation = Orientation.Horizontal;
            browseContent.VerticalAlignment = VerticalAlignment.Center;
            browseContent.Children.Add(UiTheme.CreateFolderIcon(Brushes.White, 14));
            TextBlock browseLbl = new TextBlock();
            browseLbl.Text = "Procurar...";
            browseLbl.Foreground = Brushes.White;
            browseLbl.FontSize = 12;
            browseLbl.FontWeight = FontWeights.SemiBold;
            browseLbl.Margin = new Thickness(6, 0, 0, 0);
            browseLbl.VerticalAlignment = VerticalAlignment.Center;
            browseContent.Children.Add(browseLbl);

            Button btnBrowse = UiTheme.CreateActionButton(browseContent, UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
            btnBrowse.Height = 36;
            btnBrowse.Padding = new Thickness(12, 0, 12, 0);
            btnBrowse.Click += delegate
            {
                SelecionarPastaOnboarding(labelText, txt);
            };
            Grid.SetColumn(btnBrowse, 1);
            inputGrid.Children.Add(btnBrowse);

            group.Children.Add(inputGrid);

            TextBlock err = new TextBlock();
            err.Foreground = UiTheme.AccentRed;
            err.FontSize = 11;
            err.Margin = new Thickness(2, 4, 0, 0);
            err.Visibility = Visibility.Collapsed;
            group.Children.Add(err);

            txtOut = txt;
            errOut = err;
            return group;
        }
    }
}
