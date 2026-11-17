using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;

using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

namespace TOTVSHub
{
    public static class UiTheme
    {
        public const string AppVersion = "v2.4.0";
        public const string DefaultUpdateDriveUrl = "https://drive.google.com";

        // Tom azul unico padronizado para todos os botoes azuis e toggles
        public const string PrimaryBlueHex = "#0284C7";
        public const string PrimaryBlueHoverHex = "#0EA5E9";
        public static readonly Brush PrimaryBlueBrush = BrushFromHex(PrimaryBlueHex);

        public static bool IsDark = true;

        public static Brush BgWindow { get; private set; }
        public static Brush BgHeader { get; private set; }
        public static Brush BgCard { get; private set; }
        public static Brush BgCardDim { get; private set; }
        public static Brush BgCardConsoleActive { get; private set; }
        public static Brush BgCardServiceActive { get; private set; }
        public static Brush BgInput { get; private set; }
        public static Brush BgSectionHeader { get; private set; }
        public static Brush BorderSubtle { get; private set; }
        public static Brush TextPrimary { get; private set; }
        public static Brush TextSecondary { get; private set; }
        public static Brush TextMuted { get; private set; }
        public static Brush TagBg { get; private set; }
        public static Brush NoteBannerBg { get; private set; }
        public static Brush NoteTextFg { get; private set; }
        public static string NeutralBtnHex { get; private set; }
        public static string NeutralBtnHoverHex { get; private set; }

        public static readonly Brush AccentGreen = BrushFromHex("#10B981");
        public static readonly Brush AccentAmber = BrushFromHex("#F59E0B");
        public static readonly Brush AccentLightBlue = BrushFromHex("#38BDF8");
        public static readonly Brush AccentRed = BrushFromHex("#EF4444");

        static UiTheme()
        {
            ApplyTheme(true);
        }

        public static void ApplyTheme(bool dark)
        {
            IsDark = dark;
            if (dark)
            {
                BgWindow = BrushFromHex("#0B0F19");
                BgHeader = BrushFromHex("#111827");
                BgCard = BrushFromHex("#151C2C");
                BgCardDim = BrushFromHex("#101522");
                BgCardConsoleActive = BrushFromHex("#112625");
                BgCardServiceActive = BrushFromHex("#13203B");
                BgInput = BrushFromHex("#0D1320");
                BgSectionHeader = BrushFromHex("#1A2336");
                BorderSubtle = BrushFromHex("#243044");
                TextPrimary = BrushFromHex("#F8FAFC");
                TextSecondary = BrushFromHex("#CBD5E1");
                TextMuted = BrushFromHex("#64748B");
                TagBg = BrushFromHex("#1E293B");
                NoteBannerBg = BrushFromHex("#132038");
                NoteTextFg = BrushFromHex("#F8FAFC");
                NeutralBtnHex = "#1E293B";
                NeutralBtnHoverHex = "#334155";
            }
            else
            {
                BgWindow = BrushFromHex("#F1F5F9");
                BgHeader = BrushFromHex("#FFFFFF");
                BgCard = BrushFromHex("#FFFFFF");
                BgCardDim = BrushFromHex("#E2E8F0");
                BgCardConsoleActive = BrushFromHex("#ECFDF5");
                BgCardServiceActive = BrushFromHex("#EFF6FF");
                BgInput = BrushFromHex("#F8FAFC");
                BgSectionHeader = BrushFromHex("#E2E8F0");
                BorderSubtle = BrushFromHex("#CBD5E1");
                TextPrimary = BrushFromHex("#0F172A");
                TextSecondary = BrushFromHex("#334155");
                TextMuted = BrushFromHex("#64748B");
                TagBg = BrushFromHex("#E2E8F0");
                NoteBannerBg = BrushFromHex("#E0F2FE");
                NoteTextFg = BrushFromHex("#0F172A");
                NeutralBtnHex = "#334155";
                NeutralBtnHoverHex = "#475569";
            }
        }

        public static Brush BrushFromHex(string hex)
        {
            return (SolidColorBrush)(new BrushConverter().ConvertFrom(hex));
        }

        public static string Pluralize(int count, string singular, string plural)
        {
            return string.Format("{0} {1}", count, count == 1 ? singular : plural);
        }

        public static void ApplyWindowChromeState(Window win, bool isMaximized)
        {
            WindowChrome chrome = WindowChrome.GetWindowChrome(win);
            if (chrome == null)
            {
                chrome = new WindowChrome();
                chrome.CaptionHeight = 0;
                chrome.GlassFrameThickness = new Thickness(0);
                WindowChrome.SetWindowChrome(win, chrome);
            }
            // Quando maximizado, ResizeBorderThickness = 0 permite arrastar pela borda superior sem interceptar como resize
            chrome.ResizeBorderThickness = isMaximized ? new Thickness(0) : new Thickness(6);
            chrome.CornerRadius = isMaximized ? new CornerRadius(0) : new CornerRadius(12);
        }

        public static string EnsureTotvsWhiteSvgInAppData(string appDataDir)
        {
            return TotvsLogo.EnsureTotvsWhiteSvgInAppData(appDataDir);
        }

        public static UIElement CreateTotvsWhiteSvgLogo(string cachedSvgPath, double targetHeight)
        {
            return TotvsLogo.CreateTotvsWhiteSvgLogo(cachedSvgPath, targetHeight);
        }

        public static Icon CreateTotvsTrayIcon(string cachedSvgPath)
        {
            return TotvsLogo.CreateTotvsTrayIcon(cachedSvgPath);
        }

        public static ImageSource CreateTotvsWindowImageSource(string cachedSvgPath)
        {
            return TotvsLogo.CreateTotvsWindowImageSource(cachedSvgPath);
        }

        // Toggle Switch moderno padronizado com o mesmo tom azul (#0284C7)
        public static UIElement CreateModernToggleSwitch(bool initialState, string onLabel, string offLabel, Action<bool> onChanged)
        {
            bool state = initialState;

            StackPanel container = new StackPanel();
            container.Orientation = Orientation.Horizontal;
            container.VerticalAlignment = VerticalAlignment.Center;
            container.Cursor = Cursors.Hand;

            TextBlock stateLabel = new TextBlock();
            stateLabel.Text = state ? onLabel : offLabel;
            stateLabel.Foreground = state ? AccentLightBlue : TextSecondary;
            stateLabel.FontSize = 12;
            stateLabel.FontWeight = FontWeights.Bold;
            stateLabel.MinWidth = 42;
            stateLabel.TextAlignment = TextAlignment.Right;
            stateLabel.Margin = new Thickness(0, 0, 8, 0);
            stateLabel.VerticalAlignment = VerticalAlignment.Center;

            Border track = new Border();
            track.Width = 48;
            track.Height = 24;
            track.CornerRadius = new CornerRadius(12);
            track.Background = state ? BrushFromHex(PrimaryBlueHex) : BrushFromHex("#334155");
            track.BorderBrush = state ? BrushFromHex(PrimaryBlueHoverHex) : BrushFromHex("#475569");
            track.BorderThickness = new Thickness(1);

            Border thumb = new Border();
            thumb.Width = 18;
            thumb.Height = 18;
            thumb.CornerRadius = new CornerRadius(9);
            thumb.Background = Brushes.White;
            thumb.HorizontalAlignment = HorizontalAlignment.Left;
            thumb.VerticalAlignment = VerticalAlignment.Center;

            TranslateTransform thumbTrans = new TranslateTransform(state ? 26 : 2, 0);
            thumb.RenderTransform = thumbTrans;
            track.Child = thumb;

            container.Children.Add(stateLabel);
            container.Children.Add(track);

            container.MouseLeftButtonUp += delegate
            {
                state = !state;
                stateLabel.Text = state ? onLabel : offLabel;
                stateLabel.Foreground = state ? AccentLightBlue : TextSecondary;
                track.Background = state ? BrushFromHex(PrimaryBlueHex) : BrushFromHex("#334155");
                track.BorderBrush = state ? BrushFromHex(PrimaryBlueHoverHex) : BrushFromHex("#475569");

                DoubleAnimation anim = new DoubleAnimation();
                anim.To = state ? 26 : 2;
                anim.Duration = TimeSpan.FromMilliseconds(160);
                anim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
                thumbTrans.BeginAnimation(TranslateTransform.XProperty, anim);

                if (onChanged != null)
                    onChanged(state);
            };

            return container;
        }

        // Animacao Vetorial de Loading para primeira varredura
        public static UIElement CreateVectorLoadingScreen(string cachedSvgPath)
        {
            Border wrapper = new Border();
            wrapper.Background = BgCard;
            wrapper.BorderBrush = BorderSubtle;
            wrapper.BorderThickness = new Thickness(1);
            wrapper.CornerRadius = new CornerRadius(14);
            wrapper.Padding = new Thickness(40, 52, 40, 52);
            wrapper.Margin = new Thickness(0, 14, 0, 0);

            StackPanel stack = new StackPanel();
            stack.HorizontalAlignment = HorizontalAlignment.Center;
            stack.VerticalAlignment = VerticalAlignment.Center;

            Grid spinnerHost = new Grid();
            spinnerHost.Width = 96;
            spinnerHost.Height = 96;
            spinnerHost.HorizontalAlignment = HorizontalAlignment.Center;

            System.Windows.Shapes.Path outerArc = new System.Windows.Shapes.Path();
            outerArc.Data = Geometry.Parse("M 48,4 A 44,44 0 1 1 10,68");
            outerArc.Stroke = BrushFromHex(PrimaryBlueHoverHex);
            outerArc.StrokeThickness = 2.6;
            outerArc.StrokeStartLineCap = PenLineCap.Round;
            outerArc.StrokeEndLineCap = PenLineCap.Round;
            outerArc.RenderTransformOrigin = new Point(0.5, 0.5);
            RotateTransform rtOuter = new RotateTransform(0);
            outerArc.RenderTransform = rtOuter;

            DoubleAnimation animOuter = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(1250));
            animOuter.RepeatBehavior = RepeatBehavior.Forever;
            rtOuter.BeginAnimation(RotateTransform.AngleProperty, animOuter);

            System.Windows.Shapes.Path innerArc = new System.Windows.Shapes.Path();
            innerArc.Data = Geometry.Parse("M 48,14 A 34,34 0 1 0 74,64");
            innerArc.Stroke = BrushFromHex(PrimaryBlueHex);
            innerArc.StrokeThickness = 2.4;
            innerArc.StrokeStartLineCap = PenLineCap.Round;
            innerArc.StrokeEndLineCap = PenLineCap.Round;
            innerArc.RenderTransformOrigin = new Point(0.5, 0.5);
            RotateTransform rtInner = new RotateTransform(0);
            innerArc.RenderTransform = rtInner;

            DoubleAnimation animInner = new DoubleAnimation(360, 0, TimeSpan.FromMilliseconds(1750));
            animInner.RepeatBehavior = RepeatBehavior.Forever;
            rtInner.BeginAnimation(RotateTransform.AngleProperty, animInner);

            UIElement centerLogo = TotvsLogo.CreateTotvsWhiteSvgLogo(cachedSvgPath, 26);

            spinnerHost.Children.Add(outerArc);
            spinnerHost.Children.Add(innerArc);
            spinnerHost.Children.Add(centerLogo);
            stack.Children.Add(spinnerHost);

            TextBlock title = new TextBlock();
            title.Text = "Buscando ambientes TOTVS...";
            title.Foreground = TextPrimary;
            title.FontSize = 15;
            title.FontWeight = FontWeights.Bold;
            title.HorizontalAlignment = HorizontalAlignment.Center;
            title.Margin = new Thickness(0, 18, 0, 4);
            stack.Children.Add(title);

            TextBlock sub = new TextBlock();
            sub.Text = "Escaneando diretórios, serviços do Windows e processos ativos";
            sub.Foreground = TextSecondary;
            sub.FontSize = 12;
            sub.HorizontalAlignment = HorizontalAlignment.Center;
            stack.Children.Add(sub);

            wrapper.Child = stack;
            return wrapper;
        }

        public static void InstallCustomBlueScrollBar(Application app)
        {
            string xaml =
                "<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
                "       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
                "       TargetType='{x:Type ScrollBar}'>" +
                "  <Setter Property='Stylus.IsFlicksEnabled' Value='false'/>" +
                "  <Setter Property='Background' Value='Transparent'/>" +
                "  <Setter Property='Width' Value='14'/>" +
                "  <Setter Property='Template'>" +
                "    <Setter.Value>" +
                "      <ControlTemplate TargetType='{x:Type ScrollBar}'>" +
                "        <Grid x:Name='GridRoot' Width='14' Background='Transparent' Margin='2,2,0,2'>" +
                "          <Border Background='#1E293B' Opacity='0.40' CornerRadius='6' Width='10' HorizontalAlignment='Center'/>" +
                "          <Track x:Name='PART_Track' IsDirectionReversed='true' Focusable='false'>" +
                "            <Track.Thumb>" +
                "              <Thumb x:Name='Thumb'>" +
                "                <Thumb.Template>" +
                "                  <ControlTemplate TargetType='{x:Type Thumb}'>" +
                "                    <Border x:Name='ThumbBorder' Background='#0284C7' CornerRadius='5.5' Width='10' HorizontalAlignment='Center'/>" +
                "                    <ControlTemplate.Triggers>" +
                "                      <Trigger Property='IsMouseOver' Value='true'>" +
                "                        <Setter TargetName='ThumbBorder' Property='Background' Value='#0EA5E9'/>" +
                "                      </Trigger>" +
                "                      <Trigger Property='IsDragging' Value='true'>" +
                "                        <Setter TargetName='ThumbBorder' Property='Background' Value='#38BDF8'/>" +
                "                      </Trigger>" +
                "                    </ControlTemplate.Triggers>" +
                "                  </ControlTemplate>" +
                "                </Thumb.Template>" +
                "              </Thumb>" +
                "            </Track.Thumb>" +
                "            <Track.IncreaseRepeatButton>" +
                "              <RepeatButton Command='ScrollBar.PageDownCommand' Opacity='0' Focusable='false'/>" +
                "            </Track.IncreaseRepeatButton>" +
                "            <Track.DecreaseRepeatButton>" +
                "              <RepeatButton Command='ScrollBar.PageUpCommand' Opacity='0' Focusable='false'/>" +
                "            </Track.DecreaseRepeatButton>" +
                "          </Track>" +
                "        </Grid>" +
                "        <ControlTemplate.Triggers>" +
                "          <Trigger Property='Orientation' Value='Horizontal'>" +
                "            <Setter TargetName='GridRoot' Property='Height' Value='14'/>" +
                "            <Setter TargetName='GridRoot' Property='Width' Value='Auto'/>" +
                "            <Setter TargetName='PART_Track' Property='IsDirectionReversed' Value='false'/>" +
                "          </Trigger>" +
                "        </ControlTemplate.Triggers>" +
                "      </ControlTemplate>" +
                "    </Setter.Value>" +
                "  </Setter>" +
                "</Style>";

            Style sbStyle = (Style)XamlReader.Parse(xaml);
            app.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] = sbStyle;
        }

        public static System.Windows.Shapes.Path CreateThinRefreshIcon(Brush strokeBrush)
        {
            System.Windows.Shapes.Path icon = new System.Windows.Shapes.Path();
            icon.Data = Geometry.Parse("M 20,11 A 8,8 0 1 0 17.6,17.6 M 20,4 L 20,11 L 13,11");
            icon.Stroke = strokeBrush;
            icon.StrokeThickness = 1.75;
            icon.StrokeStartLineCap = PenLineCap.Round;
            icon.StrokeEndLineCap = PenLineCap.Round;
            icon.StrokeLineJoin = PenLineJoin.Round;
            icon.Width = 18;
            icon.Height = 18;
            icon.Stretch = Stretch.Uniform;
            icon.VerticalAlignment = VerticalAlignment.Center;
            icon.RenderTransformOrigin = new Point(0.5, 0.5);
            icon.RenderTransform = new RotateTransform(0);
            return icon;
        }

        public static UIElement CreateHamburgerIcon(Brush strokeBrush)
        {
            System.Windows.Shapes.Path icon = new System.Windows.Shapes.Path();
            icon.Data = Geometry.Parse("M 3,6 L 21,6 M 3,12 L 21,12 M 3,18 L 21,18");
            icon.Stroke = strokeBrush;
            icon.StrokeThickness = 1.9;
            icon.StrokeStartLineCap = PenLineCap.Round;
            icon.StrokeEndLineCap = PenLineCap.Round;
            icon.Width = 18;
            icon.Height = 18;
            icon.Stretch = Stretch.Uniform;
            icon.VerticalAlignment = VerticalAlignment.Center;
            return icon;
        }

        public static UIElement CreateLargeThinLeftArrowIcon(Brush strokeBrush)
        {
            System.Windows.Shapes.Path arrow = new System.Windows.Shapes.Path();
            arrow.Data = Geometry.Parse("M 20,12 L 4,12 M 11,5 L 4,12 L 11,19");
            arrow.Stroke = strokeBrush;
            arrow.StrokeThickness = 1.85;
            arrow.StrokeStartLineCap = PenLineCap.Round;
            arrow.StrokeEndLineCap = PenLineCap.Round;
            arrow.StrokeLineJoin = PenLineJoin.Round;
            arrow.Width = 22;
            arrow.Height = 22;
            arrow.Stretch = Stretch.Uniform;
            arrow.VerticalAlignment = VerticalAlignment.Center;
            return arrow;
        }

        public static UIElement CreateThinBellIcon(Brush strokeBrush)
        {
            System.Windows.Shapes.Path bell = new System.Windows.Shapes.Path();
            bell.Data = Geometry.Parse("M 18,8 A 6,6 0 0 0 6,8 C 6,15 3,17 3,17 L 21,17 C 21,17 18,15 18,8 Z M 13.73,21 A 2,2 0 0 1 10.27,21");
            bell.Stroke = strokeBrush;
            bell.StrokeThickness = 1.7;
            bell.StrokeStartLineCap = PenLineCap.Round;
            bell.StrokeEndLineCap = PenLineCap.Round;
            bell.StrokeLineJoin = PenLineJoin.Round;
            bell.Width = 18;
            bell.Height = 18;
            bell.Stretch = Stretch.Uniform;
            bell.VerticalAlignment = VerticalAlignment.Center;
            return bell;
        }

        // Icone de Lapis (Configurar card - sem fundo)
        public static UIElement CreatePencilIcon(Brush strokeBrush)
        {
            System.Windows.Shapes.Path pencil = new System.Windows.Shapes.Path();
            pencil.Data = Geometry.Parse("M 17,3 A 2.8,2.8 0 1 1 21,7 L 7.5,20.5 L 2,22 L 3.5,16.5 Z");
            pencil.Stroke = strokeBrush;
            pencil.StrokeThickness = 1.8;
            pencil.StrokeStartLineCap = PenLineCap.Round;
            pencil.StrokeEndLineCap = PenLineCap.Round;
            pencil.StrokeLineJoin = PenLineJoin.Round;
            pencil.Width = 16;
            pencil.Height = 16;
            pencil.Stretch = Stretch.Uniform;
            pencil.VerticalAlignment = VerticalAlignment.Center;
            return pencil;
        }

        // Icone de Olho (Ocultar card - sem fundo)
        public static UIElement CreateEyeIcon(Brush strokeBrush)
        {
            System.Windows.Shapes.Path eye = new System.Windows.Shapes.Path();
            eye.Data = Geometry.Parse("M 1,12 C 1,12 5,4 12,4 C 19,4 23,12 23,12 C 23,12 19,20 12,20 C 5,20 1,12 1,12 Z M 12,15 A 3,3 0 1 0 12,9 A 3,3 0 1 0 12,15 Z");
            eye.Stroke = strokeBrush;
            eye.StrokeThickness = 1.8;
            eye.StrokeLineJoin = PenLineJoin.Round;
            eye.Width = 17;
            eye.Height = 17;
            eye.Stretch = Stretch.Uniform;
            eye.VerticalAlignment = VerticalAlignment.Center;
            return eye;
        }

        // Icone de Olho Cortado (Desocultar card - sem fundo)
        public static UIElement CreateEyeOffIcon(Brush strokeBrush)
        {
            System.Windows.Shapes.Path eyeOff = new System.Windows.Shapes.Path();
            eyeOff.Data = Geometry.Parse("M 17.94,17.94 A 10.07,10.07 0 0 1 12,20 C 5,20 1,12 1,12 A 18.45,18.45 0 0 1 5.06,5.06 M 9.9,4.24 A 9.12,9.12 0 0 1 12,4 C 19,4 23,12 23,12 A 18.5,18.5 0 0 1 20.84,15.18 M 1,1 L 23,23");
            eyeOff.Stroke = strokeBrush;
            eyeOff.StrokeThickness = 1.8;
            eyeOff.StrokeStartLineCap = PenLineCap.Round;
            eyeOff.StrokeEndLineCap = PenLineCap.Round;
            eyeOff.StrokeLineJoin = PenLineJoin.Round;
            eyeOff.Width = 17;
            eyeOff.Height = 17;
            eyeOff.Stretch = Stretch.Uniform;
            eyeOff.VerticalAlignment = VerticalAlignment.Center;
            return eyeOff;
        }

        // Icone vetorial de Pasta
        public static UIElement CreateFolderIcon(Brush fillBrush, double size)
        {
            System.Windows.Shapes.Path icon = new System.Windows.Shapes.Path();
            icon.Data = Geometry.Parse("M 3,5 C 2.4,5 2,5.4 2,6 L 2,18 C 2,19.1 2.9,20 4,20 L 20,20 C 21.1,20 22,19.1 22,18 L 22,8 C 22,6.9 21.1,6 20,6 L 12,6 L 10,4 L 4,4 C 3.4,4 3,4.4 3,5 Z");
            icon.Fill = fillBrush;
            icon.Width = size;
            icon.Height = size;
            icon.Stretch = Stretch.Uniform;
            icon.VerticalAlignment = VerticalAlignment.Center;
            return icon;
        }

        // Icone vetorial de Lupa
        public static UIElement CreateSearchIcon()
        {
            System.Windows.Shapes.Path icon = new System.Windows.Shapes.Path();
            icon.Data = Geometry.Parse("M 11,2 A 9,9 0 1 0 16.6,18.0 L 21.3,22.7 A 1.2,1.2 0 0 0 23.0,21.0 L 18.3,16.3 A 9,9 0 0 0 11,2 Z M 11,4.4 A 6.6,6.6 0 1 1 4.4,11 A 6.6,6.6 0 0 1 11,4.4 Z");
            icon.Fill = TextMuted;
            icon.Width = 14;
            icon.Height = 14;
            icon.Stretch = Stretch.Uniform;
            icon.VerticalAlignment = VerticalAlignment.Center;
            icon.Margin = new Thickness(0, 0, 8, 0);
            icon.IsHitTestVisible = false;
            return icon;
        }

        // Icone de Play (Comecar video / Iniciar)
        public static UIElement CreatePlayIcon(Brush fillOrStrokeBrush, double size = 14)
        {
            System.Windows.Shapes.Path icon = new System.Windows.Shapes.Path();
            icon.Data = Geometry.Parse("M 6,4 L 19,12 L 6,20 Z");
            icon.Fill = fillOrStrokeBrush;
            icon.Stroke = fillOrStrokeBrush;
            icon.StrokeThickness = 1.4;
            icon.StrokeLineJoin = PenLineJoin.Round;
            icon.Width = size;
            icon.Height = size;
            icon.Stretch = Stretch.Uniform;
            icon.VerticalAlignment = VerticalAlignment.Center;
            icon.HorizontalAlignment = HorizontalAlignment.Center;
            return icon;
        }

        // Icone de X (Parar)
        public static UIElement CreateStopXIcon(Brush strokeBrush, double size = 14)
        {
            System.Windows.Shapes.Path icon = new System.Windows.Shapes.Path();
            icon.Data = Geometry.Parse("M 6,6 L 18,18 M 18,6 L 6,18");
            icon.Stroke = strokeBrush;
            icon.StrokeThickness = 2.2;
            icon.StrokeStartLineCap = PenLineCap.Round;
            icon.StrokeEndLineCap = PenLineCap.Round;
            icon.StrokeLineJoin = PenLineJoin.Round;
            icon.Width = size;
            icon.Height = size;
            icon.Stretch = Stretch.Uniform;
            icon.VerticalAlignment = VerticalAlignment.Center;
            icon.HorizontalAlignment = HorizontalAlignment.Center;
            return icon;
        }

        // Icone de Reiniciar padrao (Seta circular)
        public static UIElement CreateRestartIcon(Brush strokeBrush, double size = 14)
        {
            System.Windows.Shapes.Path icon = new System.Windows.Shapes.Path();
            icon.Data = Geometry.Parse("M 20,11 A 8,8 0 1 0 17.6,17.6 M 20,4 L 20,11 L 13,11");
            icon.Stroke = strokeBrush;
            icon.StrokeThickness = 2.0;
            icon.StrokeStartLineCap = PenLineCap.Round;
            icon.StrokeEndLineCap = PenLineCap.Round;
            icon.StrokeLineJoin = PenLineJoin.Round;
            icon.Width = size;
            icon.Height = size;
            icon.Stretch = Stretch.Uniform;
            icon.VerticalAlignment = VerticalAlignment.Center;
            icon.HorizontalAlignment = HorizontalAlignment.Center;
            return icon;
        }

        // Icone de Balao de Comentario (Anotacao do card)
        public static UIElement CreateCommentBubbleIcon(Brush strokeBrush, double size = 16)
        {
            System.Windows.Shapes.Path icon = new System.Windows.Shapes.Path();
            icon.Data = Geometry.Parse("M 21,15 A 2,2 0 0 1 19,17 L 7,17 L 3,21 L 3,5 A 2,2 0 0 1 5,3 L 19,3 A 2,2 0 0 1 21,5 Z");
            icon.Stroke = strokeBrush;
            icon.StrokeThickness = 1.8;
            icon.StrokeStartLineCap = PenLineCap.Round;
            icon.StrokeEndLineCap = PenLineCap.Round;
            icon.StrokeLineJoin = PenLineJoin.Round;
            icon.Width = size;
            icon.Height = size;
            icon.Stretch = Stretch.Uniform;
            icon.VerticalAlignment = VerticalAlignment.Center;
            icon.HorizontalAlignment = HorizontalAlignment.Center;
            return icon;
        }

        // Icone Worldwide / Globo (Links web)
        public static UIElement CreateGlobeIcon(Brush strokeBrush, double size = 12)
        {
            System.Windows.Shapes.Path globe = new System.Windows.Shapes.Path();
            globe.Data = Geometry.Parse("M 12,2 A 10,10 0 1 0 12,22 A 10,10 0 1 0 12,2 Z M 2,12 L 22,12 M 12,2 A 15,10 0 0 1 12,22 A 15,10 0 0 1 12,2 Z");
            globe.Stroke = strokeBrush;
            globe.StrokeThickness = 1.5;
            globe.Width = size;
            globe.Height = size;
            globe.Stretch = Stretch.Uniform;
            globe.VerticalAlignment = VerticalAlignment.Center;
            return globe;
        }

        // Icone de Seta Diagonal Superior Direita (Links externos / web)
        public static UIElement CreateDiagonalArrowIcon(Brush strokeBrush, double size = 10)
        {
            System.Windows.Shapes.Path arrow = new System.Windows.Shapes.Path();
            arrow.Data = Geometry.Parse("M 7,17 L 17,7 M 8,7 L 17,7 L 17,16");
            arrow.Stroke = strokeBrush;
            arrow.StrokeThickness = 1.8;
            arrow.StrokeStartLineCap = PenLineCap.Round;
            arrow.StrokeEndLineCap = PenLineCap.Round;
            arrow.StrokeLineJoin = PenLineJoin.Round;
            arrow.Width = size;
            arrow.Height = size;
            arrow.Stretch = Stretch.Uniform;
            arrow.VerticalAlignment = VerticalAlignment.Center;
            return arrow;
        }

        // Icone de 4 blocos (Modo Cards / Grade)
        public static UIElement CreateGridCardIcon(Brush strokeBrush, double size = 14)
        {
            System.Windows.Shapes.Path p = new System.Windows.Shapes.Path();
            p.Data = Geometry.Parse("M 2,2 L 8,2 L 8,8 L 2,8 Z M 11,2 L 17,2 L 17,8 L 11,8 Z M 2,11 L 8,11 L 8,17 L 2,17 Z M 11,11 L 17,11 L 17,17 L 11,17 Z");
            p.Stroke = strokeBrush;
            p.StrokeThickness = 1.6;
            p.Width = size;
            p.Height = size;
            p.Stretch = Stretch.Uniform;
            p.VerticalAlignment = VerticalAlignment.Center;
            return p;
        }

        // Icone de Linhas / Lista
        public static UIElement CreateListIcon(Brush strokeBrush, double size = 14)
        {
            System.Windows.Shapes.Path p = new System.Windows.Shapes.Path();
            p.Data = Geometry.Parse("M 2,4 L 17,4 M 2,9.5 L 17,9.5 M 2,15 L 17,15");
            p.Stroke = strokeBrush;
            p.StrokeThickness = 1.8;
            p.StrokeStartLineCap = PenLineCap.Round;
            p.StrokeEndLineCap = PenLineCap.Round;
            p.Width = size;
            p.Height = size;
            p.Stretch = Stretch.Uniform;
            p.VerticalAlignment = VerticalAlignment.Center;
            return p;
        }

        public const double WebButtonWidth = 128;

        // Conteudo visual para botoes de links web: [Globo] [Label] ........ [↗]
        // Seta diagonal sempre alinhada a direita do botao; Globo e Label alinhados a esquerda
        public static UIElement CreateWebLinkContent(string label)
        {
            DockPanel dock = new DockPanel();
            dock.LastChildFill = true;
            dock.VerticalAlignment = VerticalAlignment.Center;
            dock.HorizontalAlignment = HorizontalAlignment.Stretch;

            FrameworkElement arrow = (FrameworkElement)CreateDiagonalArrowIcon(Brushes.White, 10);
            arrow.Margin = new Thickness(6, 0, 0, 0);
            arrow.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(arrow, Dock.Right);
            dock.Children.Add(arrow);

            StackPanel leftGroup = new StackPanel();
            leftGroup.Orientation = Orientation.Horizontal;
            leftGroup.VerticalAlignment = VerticalAlignment.Center;
            leftGroup.HorizontalAlignment = HorizontalAlignment.Left;

            FrameworkElement globe = (FrameworkElement)CreateGlobeIcon(Brushes.White, 12);
            globe.Margin = new Thickness(0, 0, 6, 0);
            globe.VerticalAlignment = VerticalAlignment.Center;
            leftGroup.Children.Add(globe);

            TextBlock txt = new TextBlock();
            txt.Text = label;
            txt.Foreground = Brushes.White;
            txt.FontSize = 11;
            txt.FontWeight = FontWeights.SemiBold;
            txt.VerticalAlignment = VerticalAlignment.Center;
            txt.TextTrimming = TextTrimming.CharacterEllipsis;
            leftGroup.Children.Add(txt);

            dock.Children.Add(leftGroup);

            return dock;
        }

        // Cria botao de link web padronizado na largura do maior item (WebServices = 128px)
        public static Button CreateWebLinkButton(string label, string normalHex, string hoverHex)
        {
            Button btn = CreateActionButton(CreateWebLinkContent(label), normalHex, hoverHex);
            btn.Width = WebButtonWidth;
            btn.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            btn.Padding = new Thickness(9, 4, 9, 4);
            btn.Margin = new Thickness(0, 2, 0, 2);
            return btn;
        }

        // Icone de maximizar (1 janela) e restaurar (aba dupla sobreposta)
        public static UIElement CreateWindowStateGlyph(bool isMaximized, Brush strokeBrush)
        {
            System.Windows.Shapes.Path p = new System.Windows.Shapes.Path();
            if (isMaximized)
            {
                // Aba dupla (duas janelas sobrepostas para restaurar da tela cheia)
                p.Data = Geometry.Parse("M 6,8 L 16,8 L 16,18 L 6,18 Z M 9,8 L 9,5 L 19,5 L 19,15 L 16,15");
            }
            else
            {
                // Janela unica para maximizar
                p.Data = Geometry.Parse("M 5,5 L 19,5 L 19,19 L 5,19 Z");
            }
            p.Stroke = strokeBrush;
            p.StrokeThickness = 1.5;
            p.StrokeLineJoin = PenLineJoin.Round;
            p.Width = 13;
            p.Height = 13;
            p.Stretch = Stretch.Uniform;
            p.VerticalAlignment = VerticalAlignment.Center;
            p.HorizontalAlignment = HorizontalAlignment.Center;
            return p;
        }

        public static Button CreateFramelessIconButton(UIElement iconElement, string tooltip)
        {
            Button btn = new Button();
            btn.Content = iconElement;
            btn.Background = Brushes.Transparent;
            btn.BorderThickness = new Thickness(0);
            btn.Padding = new Thickness(6);
            btn.Cursor = Cursors.Hand;
            btn.ToolTip = tooltip;

            ControlTemplate tpl = new ControlTemplate(typeof(Button));
            FrameworkElementFactory bd = new FrameworkElementFactory(typeof(Border));
            bd.Name = "bd";
            bd.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            bd.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(cp);
            tpl.VisualTree = bd;

            Trigger hover = new Trigger();
            hover.Property = UIElement.IsMouseOverProperty;
            hover.Value = true;
            hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.72));
            tpl.Triggers.Add(hover);

            btn.Template = tpl;
            return btn;
        }

        public static Button CreateActionButton(object content, string normalHex, string hoverHex)
        {
            Button btn = new Button();
            btn.Content = content;
            btn.Foreground = Brushes.White;
            btn.Background = BrushFromHex(normalHex);
            btn.FontSize = 12;
            btn.FontWeight = FontWeights.SemiBold;
            btn.Padding = new Thickness(11, 6, 11, 6);
            btn.Cursor = Cursors.Hand;
            btn.BorderThickness = new Thickness(0);

            btn.HorizontalContentAlignment = HorizontalAlignment.Center;
            btn.VerticalContentAlignment = VerticalAlignment.Center;

            ControlTemplate tpl = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "bd";
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
            borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

            FrameworkElementFactory contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, new TemplateBindingExtension(Control.HorizontalContentAlignmentProperty));
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, new TemplateBindingExtension(Control.VerticalContentAlignmentProperty));
            borderFactory.AppendChild(contentFactory);

            tpl.VisualTree = borderFactory;

            Trigger hoverTrigger = new Trigger();
            hoverTrigger.Property = UIElement.IsMouseOverProperty;
            hoverTrigger.Value = true;
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, BrushFromHex(hoverHex), "bd"));
            tpl.Triggers.Add(hoverTrigger);

            btn.Template = tpl;
            return btn;
        }

        public static Button CreateWindowButton(object content, string hoverHex)
        {
            Button btn = new Button();
            btn.Content = content;
            btn.Foreground = TextSecondary;
            btn.Background = Brushes.Transparent;
            btn.Width = 34;
            btn.Height = 30;
            btn.FontSize = 12;
            btn.Cursor = Cursors.Hand;
            btn.Margin = new Thickness(4, 0, 0, 0);

            ControlTemplate tpl = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "bd";
            borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

            FrameworkElementFactory contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentFactory);

            tpl.VisualTree = borderFactory;

            Trigger hoverTrigger = new Trigger();
            hoverTrigger.Property = UIElement.IsMouseOverProperty;
            hoverTrigger.Value = true;
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, BrushFromHex(hoverHex), "bd"));
            hoverTrigger.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
            tpl.Triggers.Add(hoverTrigger);

            btn.Template = tpl;
            return btn;
        }
    }
}
