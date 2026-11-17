using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Win32;

using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Point = System.Windows.Point;

namespace TOTVSHub
{
    public enum EnvRunState
    {
        Stopped,
        Initializing,
        Active,
        Stopping
    }

    public class ConflictRule
    {
        public string SectionPattern { get; set; }
        public string KeyName { get; set; }

        public bool IsSelfSection
        {
            get { return string.IsNullOrWhiteSpace(KeyName); }
        }

        public ConflictRule(string sectionPattern, string keyName)
        {
            SectionPattern = (sectionPattern ?? "").Trim();
            KeyName = (keyName ?? "").Trim();
        }
    }

    public class CustomLinkButton
    {
        public string Label { get; set; }
        public string Formula { get; set; }
        public bool IsBuiltIn { get; set; }

        public CustomLinkButton(string label, string formula, bool isBuiltIn)
        {
            Label = (label ?? "").Trim();
            Formula = (formula ?? "").Trim();
            IsBuiltIn = isBuiltIn;
        }
    }

    public class AmbienteInfo
    {
        public string NomeIni { get; set; }
        public string PastaBase { get; set; }
        public string AppDir { get; set; }
        public string ExePath { get; set; }
        public string IniPath { get; set; }
        public string WebAppPort { get; set; }
        public string TcpPort { get; set; }
        public string RestPort { get; set; }
        public string SpecialKey { get; set; }
        public string DBAlias { get; set; }
        public string DBPort { get; set; }

        public Dictionary<string, Dictionary<string, string>> IniSectionsMap { get; set; }

        public bool IsServiceRegistered { get; set; }
        public string ServiceName { get; set; }
        public string ServiceDisplayName { get; set; }
        public bool RunningAsService { get; set; }

        public EnvRunState State { get; set; }
        public int Pid { get; set; }
        public string ProcessUser { get; set; }

        public Border CardBorder { get; set; }
        public Border StatusBadge { get; set; }
        public TextBlock StatusText { get; set; }
        public Border StatusDot { get; set; }
        public TextBlock ModeTagText { get; set; }
        public Border UserTagBorder { get; set; }
        public TextBlock UserTagText { get; set; }
        public Button BtnStart { get; set; }
        public Button BtnRestart { get; set; }
        public Button BtnStop { get; set; }
        public Button BtnWeb { get; set; }
        public Button BtnRest { get; set; }
        public List<Button> CustomButtons { get; set; }

        public AmbienteInfo()
        {
            State = EnvRunState.Stopped;
            IniSectionsMap = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            CustomButtons = new List<Button>();
        }
    }

    public class AlertaConflito
    {
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public List<AmbienteInfo> Envolvidos { get; set; }

        public AlertaConflito()
        {
            Envolvidos = new List<AmbienteInfo>();
        }
    }

    public class IniEntry
    {
        public bool IsPureComment { get; set; }
        public bool IsCommented { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        public Border RowContainer { get; set; }
        public TextBlock CommentPrefixLabel { get; set; }
        public TextBox KeyBox { get; set; }
        public TextBox ValueBox { get; set; }
        public Button BtnToggleComment { get; set; }
    }

    public class IniSection
    {
        public string Name { get; set; }
        public bool IsCommented { get; set; }
        public List<IniEntry> Entries { get; set; }

        public Border SectionCard { get; set; }
        public Border HeaderBar { get; set; }
        public TextBlock HeaderCommentPrefix { get; set; }
        public TextBox SectionNameBox { get; set; }
        public Border HiddenBadge { get; set; }
        public Button BtnToggleSection { get; set; }
        public StackPanel EntriesPanel { get; set; }

        public IniSection()
        {
            Entries = new List<IniEntry>();
        }
    }

    public static class NativeWin32
    {
        public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        public const uint TOKEN_QUERY = 0x0008;
        public const int WM_GETMINMAXINFO = 0x0024;
        public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        public static string GetProcessImagePathSafe(int pid)
        {
            IntPtr hProc = IntPtr.Zero;
            try
            {
                hProc = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
                if (hProc != IntPtr.Zero)
                {
                    StringBuilder sb = new StringBuilder(1024);
                    int size = sb.Capacity;
                    if (QueryFullProcessImageName(hProc, 0, sb, ref size))
                    {
                        return sb.ToString();
                    }
                }
            }
            catch { }
            finally
            {
                if (hProc != IntPtr.Zero)
                    CloseHandle(hProc);
            }
            return null;
        }

        public static string GetProcessOwnerUser(int pid)
        {
            if (pid <= 0) return null;

            // 1. Tentativa rapida nativa via Token do Processo
            IntPtr hProc = IntPtr.Zero;
            IntPtr hToken = IntPtr.Zero;
            try
            {
                hProc = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
                if (hProc != IntPtr.Zero && OpenProcessToken(hProc, TOKEN_QUERY, out hToken))
                {
                    using (WindowsIdentity wi = new WindowsIdentity(hToken))
                    {
                        string full = wi.Name;
                        if (!string.IsNullOrEmpty(full))
                        {
                            int slash = full.IndexOf('\\');
                            return slash >= 0 ? full.Substring(slash + 1) : full;
                        }
                    }
                }
            }
            catch { }
            finally
            {
                if (hToken != IntPtr.Zero) CloseHandle(hToken);
                if (hProc != IntPtr.Zero) CloseHandle(hProc);
            }

            // 2. Fallback via tasklist /v /fi "PID eq <pid>" /fo csv /nh
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "tasklist.exe";
                psi.Arguments = string.Format("/v /fi \"PID eq {0}\" /fo csv /nh", pid);
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                using (Process p = Process.Start(psi))
                {
                    if (p != null)
                    {
                        string output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(2000);
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            string[] cols = output.Trim().Split(new string[] { "\",\"" }, StringSplitOptions.None);
                            if (cols.Length >= 7)
                            {
                                string userCol = cols[6].Trim('"').Trim();
                                int slash = userCol.IndexOf('\\');
                                return slash >= 0 ? userCol.Substring(slash + 1) : userCol;
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        public static IntPtr MultiMonitorWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
                IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    MONITORINFO monitorInfo = new MONITORINFO();
                    monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                    if (GetMonitorInfo(monitor, ref monitorInfo))
                    {
                        RECT rcWork = monitorInfo.rcWork;
                        RECT rcMonitor = monitorInfo.rcMonitor;
                        mmi.ptMaxPosition.X = Math.Abs(rcWork.Left - rcMonitor.Left);
                        mmi.ptMaxPosition.Y = Math.Abs(rcWork.Top - rcMonitor.Top);
                        mmi.ptMaxSize.X = Math.Abs(rcWork.Right - rcWork.Left);
                        mmi.ptMaxSize.Y = Math.Abs(rcWork.Bottom - rcWork.Top);
                    }
                }
                Marshal.StructureToPtr(mmi, lParam, true);
                handled = true;
            }
            return IntPtr.Zero;
        }
    }

    public static class UiTheme
    {
        public const string AppVersion = "v2.4.0";
        public const string DefaultUpdateDriveUrl = "https://drive.google.com";
        public const string SourceTotvsWhiteSvgPath = @"C:\Users\vinicius.domingues\Downloads\totvs_white.svg";

        // SVG branco oficial TOTVS embarcado como fallback automatico caso o arquivo local nao exista
        public const string EmbeddedTotvsWhiteSvg =
            "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 180 42\">" +
            "<path fill=\"#FFFFFF\" d=\"M3 6h28l-2.2 6.2H19.6L14.2 36H7.6L13 12.2H0.8L3 6z\"/>" +
            "<path fill=\"#FFFFFF\" d=\"M48.5 5.5c9.8 0 16.5 6.3 16.5 15.2 0 9.4-7.2 15.8-17.2 15.8-9.8 0-16.5-6.3-16.5-15.2 0-9.4 7.2-15.8 17.2-15.8zm-0.4 6.2c-5.6 0-9.6 3.8-9.6 9.3 0 5.4 3.8 9.2 9.4 9.2 5.6 0 9.6-3.8 9.6-9.3 0-5.4-3.8-9.2-9.4-9.2z\"/>" +
            "<path fill=\"#FFFFFF\" d=\"M69 6h28l-2.2 6.2H85.6L80.2 36h-6.6L79 12.2H66.8L69 6z\"/>" +
            "<path fill=\"#FFFFFF\" d=\"M99.5 6h7.2l5.4 20.8L124.2 6h7.4l-16.8 30h-6.8L99.5 6z\"/>" +
            "<path fill=\"#FFFFFF\" d=\"M149.2 5.5c5.8 0 10.6 2.2 13.4 5.6l-4.6 4.7c-2.1-2.4-5.2-4-8.9-4-3.6 0-6.2 1.6-6.2 4.1 0 2.4 2.1 3.5 7.4 4.8 7.5 1.8 11.8 4.4 11.8 10.3 0 6.5-5.6 10.8-14.2 10.8-6.8 0-12.4-2.5-15.6-6.5l4.8-4.8c2.4 2.9 6.1 4.9 10.8 4.9 4.2 0 6.9-1.7 6.9-4.4 0-2.5-1.9-3.7-7.6-5.1-7.3-1.8-11.5-4.3-11.5-10.1 0-6.2 5.4-10.3 13.5-10.3z\"/>" +
            "</svg>";

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

        // Garante que o SVG branco esteja salvo em %APPDATA%\TOTVSHub\totvs_white.svg
        public static string EnsureTotvsWhiteSvgInAppData(string appDataDir)
        {
            string cachedSvgPath = System.IO.Path.Combine(appDataDir, "totvs_white.svg");
            try
            {
                if (File.Exists(SourceTotvsWhiteSvgPath))
                {
                    File.Copy(SourceTotvsWhiteSvgPath, cachedSvgPath, true);
                }
                else if (!File.Exists(cachedSvgPath))
                {
                    File.WriteAllText(cachedSvgPath, EmbeddedTotvsWhiteSvg, Encoding.UTF8);
                }
            }
            catch
            {
                if (!File.Exists(cachedSvgPath))
                {
                    try { File.WriteAllText(cachedSvgPath, EmbeddedTotvsWhiteSvg, Encoding.UTF8); } catch { }
                }
            }
            return cachedSvgPath;
        }

        // Renderiza o SVG branco carregado do AppData
        public static UIElement CreateTotvsWhiteSvgLogo(string cachedSvgPath, double targetHeight)
        {
            string svgContent = EmbeddedTotvsWhiteSvg;
            try
            {
                if (!string.IsNullOrEmpty(cachedSvgPath) && File.Exists(cachedSvgPath))
                {
                    string read = File.ReadAllText(cachedSvgPath, Encoding.UTF8);
                    if (!string.IsNullOrWhiteSpace(read))
                        svgContent = read;
                }
            }
            catch { }

            double vbX = 0, vbY = 0, vbW = 180, vbH = 42;
            Match vbMatch = Regex.Match(svgContent, "viewBox\\s*=\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
            if (vbMatch.Success)
            {
                string[] nums = vbMatch.Groups[1].Value.Trim().Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (nums.Length == 4)
                {
                    double.TryParse(nums[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out vbX);
                    double.TryParse(nums[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out vbY);
                    double.TryParse(nums[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out vbW);
                    double.TryParse(nums[3], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out vbH);
                }
            }

            if (vbW <= 0) vbW = 180;
            if (vbH <= 0) vbH = 42;

            Canvas canvas = new Canvas();
            canvas.Width = vbW;
            canvas.Height = vbH;

            Brush logoFill = IsDark ? Brushes.White : BrushFromHex("#0F172A");

            MatchCollection pathMatches = Regex.Matches(svgContent, "<path[^>]*\\bd\\s*=\\s*\"([^\"]+)\"[^>]*>", RegexOptions.IgnoreCase);
            foreach (Match m in pathMatches)
            {
                string d = m.Groups[1].Value.Trim();
                if (string.IsNullOrEmpty(d)) continue;
                try
                {
                    System.Windows.Shapes.Path p = new System.Windows.Shapes.Path();
                    p.Data = Geometry.Parse(d);
                    p.Fill = logoFill;
                    Canvas.SetLeft(p, -vbX);
                    Canvas.SetTop(p, -vbY);
                    canvas.Children.Add(p);
                }
                catch { }
            }

            Viewbox vb = new Viewbox();
            vb.Height = targetHeight;
            vb.Stretch = Stretch.Uniform;
            vb.Child = canvas;
            vb.VerticalAlignment = VerticalAlignment.Center;
            return vb;
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

            UIElement centerLogo = CreateTotvsWhiteSvgLogo(cachedSvgPath, 14);

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

        public static Bitmap CreateTotvsLogoBitmap(int size)
        {
            Bitmap bmp = new Bitmap(size, size);
            float scale = size / 32f;
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);
                g.ScaleTransform(scale, scale);

                using (GraphicsPath bgPath = new GraphicsPath())
                {
                    int r = 7;
                    bgPath.AddArc(1, 1, r * 2, r * 2, 180, 90);
                    bgPath.AddArc(31 - r * 2, 1, r * 2, r * 2, 270, 90);
                    bgPath.AddArc(31 - r * 2, 31 - r * 2, r * 2, r * 2, 0, 90);
                    bgPath.AddArc(1, 31 - r * 2, r * 2, r * 2, 90, 90);
                    bgPath.CloseFigure();

                    using (SolidBrush bgBrush = new SolidBrush(System.Drawing.Color.FromArgb(15, 23, 42)))
                    {
                        g.FillPath(bgBrush, bgPath);
                    }
                    using (System.Drawing.Pen borderPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(56, 189, 248), 1.3f))
                    {
                        g.DrawPath(borderPen, bgPath);
                    }
                }

                // T branco TOTVS
                PointF[] topPts = new PointF[]
                {
                    new PointF(5f, 7f),
                    new PointF(27f, 7f),
                    new PointF(25.2f, 12.2f),
                    new PointF(6.8f, 12.2f)
                };
                g.FillPolygon(System.Drawing.Brushes.White, topPts);

                PointF[] stemPts = new PointF[]
                {
                    new PointF(13.2f, 12.2f),
                    new PointF(18.8f, 12.2f),
                    new PointF(17.4f, 25.5f),
                    new PointF(11.8f, 25.5f)
                };
                g.FillPolygon(System.Drawing.Brushes.White, stemPts);
            }
            return bmp;
        }

        public static Icon CreateTotvsTrayIcon()
        {
            using (Bitmap bmp = CreateTotvsLogoBitmap(32))
            {
                IntPtr hIcon = bmp.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }

        public static ImageSource CreateTotvsWindowImageSource()
        {
            using (Bitmap bmp = CreateTotvsLogoBitmap(64))
            {
                IntPtr hIcon = bmp.GetHicon();
                try
                {
                    ImageSource src = Imaging.CreateBitmapSourceFromHIcon(
                        hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    src.Freeze();
                    return src;
                }
                finally
                {
                    NativeWin32.DestroyIcon(hIcon);
                }
            }
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

            ControlTemplate tpl = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "bd";
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
            borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

            FrameworkElementFactory contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
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

    // =========================================================================
    // POPUP ESTILIZADO COM TOGGLE SWITCH (SIM / NAO) E DOUBLE CHECK
    // =========================================================================
    public class StyledDialog : Window
    {
        public enum DialogChoice
        {
            Cancel,
            Yes,
            No
        }

        public DialogChoice ResultChoice { get; private set; }

        public StyledDialog(string title, string message, string subMessage, string togglePrompt, bool showToggle, bool initialToggleState, string confirmLabel, string confirmBgHex, string confirmHoverHex, bool showCancel)
        {
            ResultChoice = DialogChoice.Cancel;
            bool toggleState = initialToggleState;

            Width = 460;
            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Background = UiTheme.BgHeader;
            FontFamily = new FontFamily("Segoe UI");

            UiTheme.ApplyWindowChromeState(this, false);

            Border outer = new Border();
            outer.Background = UiTheme.BgHeader;
            outer.BorderBrush = UiTheme.BorderSubtle;
            outer.BorderThickness = new Thickness(1);
            outer.CornerRadius = new CornerRadius(12);
            outer.Padding = new Thickness(22, 18, 22, 20);
            outer.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ClickCount == 1)
                {
                    try { DragMove(); } catch { }
                }
            };

            StackPanel stack = new StackPanel();

            DockPanel topRow = new DockPanel();
            topRow.Margin = new Thickness(0, 0, 0, 12);

            if (showCancel)
            {
                Button btnClose = UiTheme.CreateWindowButton("✕", "#DC2626");
                btnClose.Width = 28;
                btnClose.Height = 26;
                DockPanel.SetDock(btnClose, Dock.Right);
                btnClose.Click += delegate
                {
                    ResultChoice = DialogChoice.Cancel;
                    Close();
                };
                topRow.Children.Add(btnClose);
            }

            StackPanel titleStack = new StackPanel();
            titleStack.Orientation = Orientation.Horizontal;

            Border iconBadge = new Border();
            iconBadge.Background = UiTheme.BrushFromHex(UiTheme.PrimaryBlueHex);
            iconBadge.CornerRadius = new CornerRadius(7);
            iconBadge.Padding = new Thickness(7, 3, 7, 3);
            iconBadge.Margin = new Thickness(0, 0, 10, 0);
            TextBlock iconTxt = new TextBlock();
            iconTxt.Text = "TOTVSHub";
            iconTxt.Foreground = Brushes.White;
            iconTxt.FontWeight = FontWeights.Bold;
            iconTxt.FontSize = 11;
            iconBadge.Child = iconTxt;

            TextBlock lblTitle = new TextBlock();
            lblTitle.Text = title;
            lblTitle.Foreground = UiTheme.TextPrimary;
            lblTitle.FontSize = 15;
            lblTitle.FontWeight = FontWeights.Bold;
            lblTitle.VerticalAlignment = VerticalAlignment.Center;

            titleStack.Children.Add(iconBadge);
            titleStack.Children.Add(lblTitle);
            topRow.Children.Add(titleStack);
            stack.Children.Add(topRow);

            TextBlock lblMsg = new TextBlock();
            lblMsg.Text = message;
            lblMsg.Foreground = UiTheme.TextPrimary;
            lblMsg.FontSize = 14;
            lblMsg.TextWrapping = TextWrapping.Wrap;
            lblMsg.Margin = new Thickness(0, 2, 0, 8);
            stack.Children.Add(lblMsg);

            if (!string.IsNullOrEmpty(subMessage))
            {
                TextBlock lblSub = new TextBlock();
                lblSub.Text = subMessage;
                lblSub.Foreground = UiTheme.TextSecondary;
                lblSub.FontSize = 12;
                lblSub.TextWrapping = TextWrapping.Wrap;
                lblSub.Margin = new Thickness(0, 0, 0, 14);
                stack.Children.Add(lblSub);
            }

            if (showToggle)
            {
                Border toggleCard = new Border();
                toggleCard.Background = UiTheme.BgInput;
                toggleCard.BorderBrush = UiTheme.BorderSubtle;
                toggleCard.BorderThickness = new Thickness(1);
                toggleCard.CornerRadius = new CornerRadius(8);
                toggleCard.Padding = new Thickness(12, 9, 12, 9);
                toggleCard.Margin = new Thickness(0, 0, 0, 16);

                DockPanel toggleDock = new DockPanel();
                UIElement toggleCtrl = UiTheme.CreateModernToggleSwitch(toggleState, "Sim", "Não", delegate(bool val)
                {
                    toggleState = val;
                });
                DockPanel.SetDock(toggleCtrl, Dock.Right);

                TextBlock toggleText = new TextBlock();
                toggleText.Text = togglePrompt;
                toggleText.Foreground = UiTheme.TextPrimary;
                toggleText.FontSize = 13;
                toggleText.FontWeight = FontWeights.SemiBold;
                toggleText.VerticalAlignment = VerticalAlignment.Center;

                toggleDock.Children.Add(toggleCtrl);
                toggleDock.Children.Add(toggleText);
                toggleCard.Child = toggleDock;
                stack.Children.Add(toggleCard);
            }

            Button btnConfirm = UiTheme.CreateActionButton(confirmLabel, confirmBgHex, confirmHoverHex);
            btnConfirm.Padding = new Thickness(18, 8, 18, 8);
            btnConfirm.HorizontalAlignment = HorizontalAlignment.Right;
            btnConfirm.Click += delegate
            {
                ResultChoice = toggleState ? DialogChoice.Yes : DialogChoice.No;
                Close();
            };
            stack.Children.Add(btnConfirm);

            outer.Child = stack;
            Content = outer;

            KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape)
                {
                    ResultChoice = DialogChoice.Cancel;
                    Close();
                }
                else if (e.Key == Key.Enter)
                {
                    ResultChoice = toggleState ? DialogChoice.Yes : DialogChoice.No;
                    Close();
                }
            };
        }

        public static DialogChoice AskWithToggle(Window owner, string title, string message, string subMessage, string togglePrompt, bool initialState, string confirmLabel, string confirmBgHex, string confirmHoverHex)
        {
            StyledDialog dlg = new StyledDialog(title, message, subMessage, togglePrompt, true, initialState, confirmLabel, confirmBgHex, confirmHoverHex, true);
            dlg.Owner = owner;
            dlg.ShowDialog();
            return dlg.ResultChoice;
        }

        public static void Info(Window owner, string title, string message, string subMessage)
        {
            StyledDialog dlg = new StyledDialog(title, message, subMessage, null, false, true, "OK", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex, false);
            dlg.Owner = owner;
            dlg.ShowDialog();
        }
    }

    // =========================================================================
    // JANELA DE ALERTAS (TITULO: "Alertas (N)")
    // =========================================================================
    public class AlertCenterWindow : Window
    {
        public Action<AmbienteInfo> OnOpenIniRequested { get; set; }

        public AlertCenterWindow(List<AlertaConflito> alertas)
        {
            Title = string.Format("Alertas ({0})", alertas.Count);
            Width = 680;
            Height = 520;
            MinWidth = 540;
            MinHeight = 380;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            Background = UiTheme.BgWindow;
            FontFamily = new FontFamily("Segoe UI");

            UiTheme.ApplyWindowChromeState(this, false);

            Border shell = new Border();
            shell.Background = UiTheme.BgWindow;
            shell.BorderBrush = UiTheme.BorderSubtle;
            shell.BorderThickness = new Thickness(1);
            shell.CornerRadius = new CornerRadius(12);

            Grid root = new Grid();
            RowDefinition r0 = new RowDefinition(); r0.Height = GridLength.Auto;
            RowDefinition r1 = new RowDefinition(); r1.Height = new GridLength(1, GridUnitType.Star);
            root.RowDefinitions.Add(r0);
            root.RowDefinitions.Add(r1);

            Border header = new Border();
            header.Background = UiTheme.BgHeader;
            header.BorderBrush = UiTheme.BorderSubtle;
            header.BorderThickness = new Thickness(0, 0, 0, 1);
            header.Padding = new Thickness(18, 12, 14, 12);
            header.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ClickCount == 1)
                {
                    try { DragMove(); } catch { }
                }
            };

            DockPanel dock = new DockPanel();
            Button btnClose = UiTheme.CreateWindowButton("✕", "#DC2626");
            DockPanel.SetDock(btnClose, Dock.Right);
            btnClose.Click += delegate { Close(); };
            dock.Children.Add(btnClose);

            StackPanel titleStack = new StackPanel();
            titleStack.Orientation = Orientation.Horizontal;
            titleStack.VerticalAlignment = VerticalAlignment.Center;

            titleStack.Children.Add(UiTheme.CreateThinBellIcon(UiTheme.TextPrimary));

            TextBlock lblTitle = new TextBlock();
            lblTitle.Text = string.Format("Alertas ({0})", alertas.Count);
            lblTitle.Foreground = UiTheme.TextPrimary;
            lblTitle.FontSize = 15;
            lblTitle.FontWeight = FontWeights.Bold;
            lblTitle.Margin = new Thickness(10, 0, 0, 0);
            titleStack.Children.Add(lblTitle);

            dock.Children.Add(titleStack);
            header.Child = dock;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.Padding = new Thickness(18, 14, 18, 18);

            StackPanel listPanel = new StackPanel();

            if (alertas.Count == 0)
            {
                Border okBox = new Border();
                okBox.Background = UiTheme.BgCard;
                okBox.BorderBrush = UiTheme.BorderSubtle;
                okBox.BorderThickness = new Thickness(1);
                okBox.CornerRadius = new CornerRadius(10);
                okBox.Padding = new Thickness(24);
                okBox.Margin = new Thickness(0, 20, 0, 0);

                StackPanel okStack = new StackPanel();
                okStack.HorizontalAlignment = HorizontalAlignment.Center;

                TextBlock okTitle = new TextBlock();
                okTitle.Text = "✓ Nenhum conflito detectado";
                okTitle.Foreground = UiTheme.TextPrimary;
                okTitle.FontSize = 14;
                okTitle.FontWeight = FontWeights.Bold;
                okTitle.HorizontalAlignment = HorizontalAlignment.Center;

                TextBlock okSub = new TextBlock();
                okSub.Text = "Nenhuma duplicidade encontrada para as regras configuradas.";
                okSub.Foreground = UiTheme.TextSecondary;
                okSub.FontSize = 12;
                okSub.Margin = new Thickness(0, 6, 0, 0);
                okSub.HorizontalAlignment = HorizontalAlignment.Center;

                okStack.Children.Add(okTitle);
                okStack.Children.Add(okSub);
                okBox.Child = okStack;
                listPanel.Children.Add(okBox);
            }
            else
            {
                foreach (AlertaConflito alerta in alertas)
                {
                    Border card = new Border();
                    card.Background = UiTheme.BgCard;
                    card.BorderBrush = UiTheme.BorderSubtle;
                    card.BorderThickness = new Thickness(1);
                    card.CornerRadius = new CornerRadius(10);
                    card.Padding = new Thickness(14, 12, 14, 12);
                    card.Margin = new Thickness(0, 0, 0, 10);

                    StackPanel cardStack = new StackPanel();

                    TextBlock tTitle = new TextBlock();
                    tTitle.Text = alerta.Titulo;
                    tTitle.Foreground = UiTheme.TextPrimary;
                    tTitle.FontSize = 13;
                    tTitle.FontWeight = FontWeights.Bold;
                    cardStack.Children.Add(tTitle);

                    TextBlock tDesc = new TextBlock();
                    tDesc.Text = alerta.Descricao;
                    tDesc.Foreground = UiTheme.TextSecondary;
                    tDesc.FontSize = 12;
                    tDesc.TextWrapping = TextWrapping.Wrap;
                    tDesc.Margin = new Thickness(0, 4, 0, 8);
                    cardStack.Children.Add(tDesc);

                    foreach (AmbienteInfo env in alerta.Envolvidos)
                    {
                        AmbienteInfo capturedEnv = env;
                        Border envRow = new Border();
                        envRow.Background = UiTheme.BgInput;
                        envRow.BorderBrush = UiTheme.BorderSubtle;
                        envRow.BorderThickness = new Thickness(1);
                        envRow.CornerRadius = new CornerRadius(6);
                        envRow.Padding = new Thickness(10, 6, 8, 6);
                        envRow.Margin = new Thickness(0, 0, 0, 4);

                        DockPanel envDock = new DockPanel();

                        Button btnFix = UiTheme.CreateActionButton("Configurar", UiTheme.NeutralBtnHex, UiTheme.NeutralBtnHoverHex);
                        btnFix.Padding = new Thickness(9, 4, 9, 4);
                        btnFix.FontSize = 11;
                        DockPanel.SetDock(btnFix, Dock.Right);
                        btnFix.Click += delegate
                        {
                            if (OnOpenIniRequested != null)
                            {
                                Close();
                                OnOpenIniRequested(capturedEnv);
                            }
                        };

                        StackPanel envInfo = new StackPanel();
                        envInfo.VerticalAlignment = VerticalAlignment.Center;

                        TextBlock eName = new TextBlock();
                        eName.Text = string.Format("{0}  ({1})", capturedEnv.NomeIni, capturedEnv.PastaBase);
                        eName.Foreground = UiTheme.TextPrimary;
                        eName.FontSize = 12;
                        eName.FontWeight = FontWeights.SemiBold;

                        TextBlock ePath = new TextBlock();
                        ePath.Text = capturedEnv.AppDir;
                        ePath.Foreground = UiTheme.TextMuted;
                        ePath.FontSize = 11;

                        envInfo.Children.Add(eName);
                        envInfo.Children.Add(ePath);

                        envDock.Children.Add(btnFix);
                        envDock.Children.Add(envInfo);
                        envRow.Child = envDock;
                        cardStack.Children.Add(envRow);
                    }

                    card.Child = cardStack;
                    listPanel.Children.Add(card);
                }
            }

            scroll.Content = listPanel;
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            shell.Child = root;
            Content = shell;
        }
    }

    // =========================================================================
    // JANELA PRINCIPAL - TOTVSHub
    // =========================================================================
    public class MainWindow : Window
    {
        private string _rootPath = @"C:\TOTVS\bases";
        private int _pollIntervalSeconds = 60;
        private int _scanBasesMinutes = 60;
        private int _browserDelaySeconds = 4;
        private bool _ignoreOldFolders = true;
        private bool _showProcessUser = true;
        private bool _showIniComments = false; // Padrao desmarcado
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
            _appDataDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TOTVSHub");

            if (!Directory.Exists(_appDataDir))
                Directory.CreateDirectory(_appDataDir);

            // Carrega/copia a logo totvs_white.svg para AppData automaticamente
            _cachedSvgPath = UiTheme.EnsureTotvsWhiteSvgInAppData(_appDataDir);

            _configPath = System.IO.Path.Combine(_appDataDir, "config.txt");
            _hiddenFilePath = System.IO.Path.Combine(_appDataDir, "ocultos.txt");
            _notesFilePath = System.IO.Path.Combine(_appDataDir, "notas.txt");
            _startupPrefPath = System.IO.Path.Combine(_appDataDir, "startup.txt");
            _themePrefPath = System.IO.Path.Combine(_appDataDir, "theme.txt");
            _timersPrefPath = System.IO.Path.Combine(_appDataDir, "timers.txt");
            _ignoreOldPrefPath = System.IO.Path.Combine(_appDataDir, "ignore_old.txt");
            _showUserPrefPath = System.IO.Path.Combine(_appDataDir, "show_pid_user.txt");
            _showCommentsPrefPath = System.IO.Path.Combine(_appDataDir, "show_ini_comments.txt");
            _rulesPrefPath = System.IO.Path.Combine(_appDataDir, "alert_rules.txt");
            _customLinksPrefPath = System.IO.Path.Combine(_appDataDir, "custom_links.txt");
            _driveUrlPrefPath = System.IO.Path.Combine(_appDataDir, "drive_url.txt");

            IniciarListenerSobrescrita();
            CarregarPreferenciasAppData();
            ConfigurarInicializacaoWindowsAutomatica();

            Title = "TOTVSHub";
            Icon = UiTheme.CreateTotvsWindowImageSource();
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
                if (_isInDashboardView)
                {
                    RenderizarCards();
                }
            };

            SizeChanged += delegate
            {
                if (_isInDashboardView && WindowState == WindowState.Maximized)
                {
                    AtualizarColunasGridMaximizado();
                }
            };

            InicializarBandejaWindows();
            BuildShellAndDashboard();
            CarregarAmbientesAsync(false);

            _monitorTimer = new DispatcherTimer();
            _monitorTimer.Interval = TimeSpan.FromSeconds(_pollIntervalSeconds);
            _monitorTimer.Tick += delegate { DispararAtualizacaoProcessosAsync(); };
            _monitorTimer.Start();

            _basesRefreshTimer = new DispatcherTimer();
            _basesRefreshTimer.Interval = TimeSpan.FromMinutes(_scanBasesMinutes);
            _basesRefreshTimer.Tick += delegate { CarregarAmbientesAsync(true); };
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

        // =====================================================================
        // BANDEJA DO WINDOWS (SYSTEM TRAY)
        // =====================================================================
        private class DarkTrayRenderer : System.Windows.Forms.ToolStripProfessionalRenderer
        {
            protected override void OnRenderMenuItemBackground(System.Windows.Forms.ToolStripItemRenderEventArgs e)
            {
                System.Drawing.Rectangle rc = new System.Drawing.Rectangle(System.Drawing.Point.Empty, e.Item.Size);
                System.Drawing.Color bg = e.Item.Selected && e.Item.Enabled
                    ? System.Drawing.Color.FromArgb(30, 41, 59)
                    : System.Drawing.Color.FromArgb(15, 23, 42);
                using (SolidBrush b = new SolidBrush(bg))
                {
                    e.Graphics.FillRectangle(b, rc);
                }
            }

            protected override void OnRenderToolStripBackground(System.Windows.Forms.ToolStripRenderEventArgs e)
            {
                using (SolidBrush b = new SolidBrush(System.Drawing.Color.FromArgb(15, 23, 42)))
                {
                    e.Graphics.FillRectangle(b, e.AffectedBounds);
                }
            }

            protected override void OnRenderToolStripBorder(System.Windows.Forms.ToolStripRenderEventArgs e)
            {
                using (System.Drawing.Pen p = new System.Drawing.Pen(System.Drawing.Color.FromArgb(51, 65, 85)))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
                }
            }
        }

        private void InicializarBandejaWindows()
        {
            _trayMenu = new System.Windows.Forms.ContextMenuStrip();
            _trayMenu.Renderer = new DarkTrayRenderer();
            _trayMenu.ForeColor = System.Drawing.Color.FromArgb(248, 250, 252);

            _trayIcon = new System.Windows.Forms.NotifyIcon();
            _trayIcon.Icon = UiTheme.CreateTotvsTrayIcon();
            _trayIcon.Text = "TOTVSHub";
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += delegate { RestaurarJanelaPrincipal(); };

            _trayIcon.MouseUp += delegate(object s, System.Windows.Forms.MouseEventArgs e)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    ReconstruirMenuBandeja();
                    if (_trayMenu.Handle != IntPtr.Zero)
                        NativeWin32.SetForegroundWindow(_trayMenu.Handle);
                    _trayMenu.Show(System.Windows.Forms.Cursor.Position);
                }
            };
        }

        private void ReconstruirMenuBandeja()
        {
            if (_trayMenu == null) return;
            _trayMenu.Items.Clear();

            var openItem = new System.Windows.Forms.ToolStripMenuItem("Abrir TOTVSHub");
            openItem.ForeColor = System.Drawing.Color.White;
            openItem.Click += delegate { RestaurarJanelaPrincipal(); };
            _trayMenu.Items.Add(openItem);

            _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var protheusMenu = new System.Windows.Forms.ToolStripMenuItem("Protheus");
            protheusMenu.ForeColor = System.Drawing.Color.White;
            var basesComPorta = _ambientes
                .Where(a => !_hiddenDirs.Contains(a.AppDir) && (!string.IsNullOrEmpty(a.WebAppPort) || !string.IsNullOrEmpty(a.RestPort)))
                .ToList();

            if (basesComPorta.Count == 0)
            {
                var vazio = new System.Windows.Forms.ToolStripMenuItem("Nenhuma base configurada");
                vazio.Enabled = false;
                protheusMenu.DropDownItems.Add(vazio);
            }
            else
            {
                foreach (AmbienteInfo amb in basesComPorta)
                {
                    AmbienteInfo captured = amb;
                    if (!string.IsNullOrEmpty(captured.WebAppPort))
                    {
                        var subWeb = new System.Windows.Forms.ToolStripMenuItem(string.Format("{0} — Protheus (:{1})", captured.NomeIni, captured.WebAppPort));
                        subWeb.ForeColor = System.Drawing.Color.White;
                        subWeb.Click += delegate { AbrirNavegador(captured); };
                        protheusMenu.DropDownItems.Add(subWeb);
                    }
                    if (!string.IsNullOrEmpty(captured.RestPort))
                    {
                        var subRest = new System.Windows.Forms.ToolStripMenuItem(string.Format("{0} — REST (:{1}/rest)", captured.NomeIni, captured.RestPort));
                        subRest.ForeColor = System.Drawing.Color.White;
                        subRest.Click += delegate { AbrirRestNavegador(captured); };
                        protheusMenu.DropDownItems.Add(subRest);
                    }
                }
            }
            _trayMenu.Items.Add(protheusMenu);

            var desativarMenu = new System.Windows.Forms.ToolStripMenuItem("Desativar");
            desativarMenu.ForeColor = System.Drawing.Color.White;
            var basesAtivas = _ambientes.Where(a => a.State == EnvRunState.Active).ToList();
            if (basesAtivas.Count == 0)
            {
                var nenhumaAtiva = new System.Windows.Forms.ToolStripMenuItem("Nenhuma base ativa no momento");
                nenhumaAtiva.Enabled = false;
                desativarMenu.DropDownItems.Add(nenhumaAtiva);
            }
            else
            {
                foreach (AmbienteInfo amb in basesAtivas)
                {
                    AmbienteInfo captured = amb;
                    string modo = captured.RunningAsService ? "Serviço" : "Console";
                    var subStop = new System.Windows.Forms.ToolStripMenuItem(string.Format("Parar {0} ({1})", captured.NomeIni, modo));
                    subStop.ForeColor = System.Drawing.Color.FromArgb(248, 113, 113);
                    subStop.Click += delegate { EncerrarAmbienteAsync(captured); };
                    desativarMenu.DropDownItems.Add(subStop);
                }
            }
            _trayMenu.Items.Add(desativarMenu);

            var checkUpdateItem = new System.Windows.Forms.ToolStripMenuItem("Verificar atualizações");
            checkUpdateItem.ForeColor = System.Drawing.Color.FromArgb(56, 189, 248);
            checkUpdateItem.Click += delegate { AbrirDriveAtualizacoes(); };
            _trayMenu.Items.Add(checkUpdateItem);

            _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var aboutItem = new System.Windows.Forms.ToolStripMenuItem("Sobre (" + UiTheme.AppVersion + ")");
            aboutItem.ForeColor = System.Drawing.Color.FromArgb(148, 163, 184);
            aboutItem.Click += delegate
            {
                RestaurarJanelaPrincipal();
                ShowSettingsView();
            };
            _trayMenu.Items.Add(aboutItem);

            var exitItem = new System.Windows.Forms.ToolStripMenuItem("Encerrar");
            exitItem.ForeColor = System.Drawing.Color.FromArgb(248, 113, 113);
            exitItem.Click += delegate
            {
                _isRealExit = true;
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                }
                Application.Current.Shutdown();
            };
            _trayMenu.Items.Add(exitItem);
        }

        private void AbrirDriveAtualizacoes()
        {
            try
            {
                string url = string.IsNullOrWhiteSpace(_updateDriveUrl) ? UiTheme.DefaultUpdateDriveUrl : _updateDriveUrl.Trim();
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = url;
                psi.UseShellExecute = true;
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                StyledDialog.Info(this, "Erro ao Abrir Drive", "Não foi possível abrir o link de atualizações.", ex.Message);
            }
        }

        private void RestaurarJanelaPrincipal()
        {
            Show();
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            Activate();
        }

        // =====================================================================
        // PREFERENCIAS E REGISTRO DO WINDOWS
        // =====================================================================
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

            if (File.Exists(_driveUrlPrefPath))
            {
                string urlVal = File.ReadAllText(_driveUrlPrefPath, Encoding.UTF8).Trim();
                if (!string.IsNullOrEmpty(urlVal))
                    _updateDriveUrl = urlVal;
            }

            if (File.Exists(_configPath))
            {
                string saved = File.ReadAllText(_configPath, Encoding.UTF8).Trim();
                if (!string.IsNullOrEmpty(saved) && Directory.Exists(saved))
                    _rootPath = saved;
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

        // =====================================================================
        // ESTRUTURA DA JANELA PRINCIPAL E ARRASTE FLUIDO ENTRE MONITORES
        // =====================================================================
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

            ShowDashboardView();
        }

        private Border BuildTopWindowBar(UIElement leftContent, UIElement rightExtraControls)
        {
            Border titleBar = new Border();
            titleBar.Background = UiTheme.BgHeader;
            titleBar.BorderBrush = UiTheme.BorderSubtle;
            titleBar.BorderThickness = new Thickness(0, 0, 0, 1);
            titleBar.Padding = new Thickness(20, 11, 14, 11);

            titleBar.PreviewMouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                // Ignora cliques em botoes do topo
                DependencyObject dep = e.OriginalSource as DependencyObject;
                while (dep != null && dep != titleBar)
                {
                    if (dep is Button || dep is TextBox)
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

            StackPanel rightControls = new StackPanel();
            rightControls.Orientation = Orientation.Horizontal;
            rightControls.VerticalAlignment = VerticalAlignment.Center;
            DockPanel.SetDock(rightControls, Dock.Right);

            if (rightExtraControls != null)
            {
                rightControls.Children.Add(rightExtraControls);
            }

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

            titleDock.Children.Add(leftContent);
            titleBar.Child = titleDock;
            return titleBar;
        }

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

            // Esquerda do TopBar: Logo branca TOTVS do AppData + TOTVSHub
            StackPanel titleLeft = new StackPanel();
            titleLeft.Orientation = Orientation.Horizontal;
            titleLeft.VerticalAlignment = VerticalAlignment.Center;

            titleLeft.Children.Add(UiTheme.CreateTotvsWhiteSvgLogo(_cachedSvgPath, 19));

            TextBlock appTitle = new TextBlock();
            appTitle.Text = "Hub";
            appTitle.Foreground = UiTheme.AccentLightBlue;
            appTitle.FontSize = 18;
            appTitle.FontWeight = FontWeights.Bold;
            appTitle.Margin = new Thickness(3, 0, 0, 0);
            appTitle.VerticalAlignment = VerticalAlignment.Center;
            titleLeft.Children.Add(appTitle);

            // Direita do TopBar: Sino + Sanduiche 3 listras
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

            Border topBar = BuildTopWindowBar(titleLeft, topIcons);
            Grid.SetRow(topBar, 0);
            rootGrid.Children.Add(topBar);

            // ================= BARRA DE FILTRO E ACOES =================
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

            // ================= TRILHO DE LOADING HORIZONTAL =================
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

            System.Windows.Media.LinearGradientBrush beamBrush = new System.Windows.Media.LinearGradientBrush();
            beamBrush.StartPoint = new Point(0, 0);
            beamBrush.EndPoint = new Point(1, 0);
            beamBrush.GradientStops.Add(new GradientStop(Colors.Transparent, 0.0));
            beamBrush.GradientStops.Add(new GradientStop((Color)System.Windows.Media.ColorConverter.ConvertFromString("#0284C7"), 0.5));
            beamBrush.GradientStops.Add(new GradientStop((Color)System.Windows.Media.ColorConverter.ConvertFromString("#38BDF8"), 0.85));
            beamBrush.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));
            _loadingBeam.Background = beamBrush;

            _loadingBeamTransform = new TranslateTransform(-240, 0);
            _loadingBeam.RenderTransform = _loadingBeamTransform;
            _loadingTrackGrid.Children.Add(_loadingBeam);

            Grid.SetRow(_loadingTrackGrid, 2);
            rootGrid.Children.Add(_loadingTrackGrid);

            // ================= CONTAINER DE AMBIENTES (LISTA EM NORMAL, CARDS GRID EM TELA CHEIA) =================
            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.Padding = new Thickness(20, 4, 20, 18);
            _cardsScrollViewer = scroll;

            if (WindowState == WindowState.Maximized)
            {
                UniformGrid ug = new UniformGrid();
                ug.Columns = ActualWidth >= 1750 ? 3 : 2;
                ug.VerticalAlignment = VerticalAlignment.Top;
                _cardsContainer = ug;
            }
            else
            {
                StackPanel sp = new StackPanel();
                _cardsContainer = sp;
            }

            scroll.Content = _cardsContainer;
            Grid.SetRow(scroll, 3);
            rootGrid.Children.Add(scroll);

            _pageHost.Content = rootGrid;
            RenderizarCards();
        }

        private void AtualizarColunasGridMaximizado()
        {
            UniformGrid ug = _cardsContainer as UniformGrid;
            if (ug != null)
            {
                int cols = ActualWidth >= 1750 ? 3 : 2;
                if (ug.Columns != cols)
                    ug.Columns = cols;
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
            btnBackArrow.Click += delegate { ShowDashboardView(); };

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

                _rootPath = @"C:\TOTVS\bases";
                _pollIntervalSeconds = 60;
                _scanBasesMinutes = 60;
                _browserDelaySeconds = 4;
                _ignoreOldFolders = true;
                _showProcessUser = true;
                _showIniComments = false;
                _updateDriveUrl = UiTheme.DefaultUpdateDriveUrl;
                _conflictRules = ObterRegrasPadrao();
                _customLinks.Clear();

                File.WriteAllText(_configPath, _rootPath, Encoding.UTF8);
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

                ShowSettingsView();
                CarregarAmbientesAsync(false);
            };

            Button btnCheckUpdates = UiTheme.CreateActionButton("Verificar atualizações", UiTheme.PrimaryBlueHex, UiTheme.PrimaryBlueHoverHex);
            btnCheckUpdates.ToolTip = "Abrir pasta de atualizações no Google Drive";
            btnCheckUpdates.Click += delegate { AbrirDriveAtualizacoes(); };

            aboutRightBtns.Children.Add(btnRestoreDefault);
            aboutRightBtns.Children.Add(btnCheckUpdates);
            aboutTopDock.Children.Add(aboutRightBtns);

            StackPanel aboutTop = new StackPanel();
            aboutTop.Orientation = Orientation.Horizontal;
            aboutTop.Children.Add(UiTheme.CreateTotvsWhiteSvgLogo(_cachedSvgPath, 22));

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
                ShowDashboardView();

                if (oldIgnoreChanged)
                {
                    CarregarAmbientesAsync(true);
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

        // =====================================================================
        // TELA INTEIRA: EDITOR DE APPSERVER.INI
        // =====================================================================
        private void ShowIniEditorView(AmbienteInfo amb)
        {
            _isInDashboardView = false;

            List<IniSection> sections = ParseIniFile(amb.IniPath);
            string iniFileName = System.IO.Path.GetFileName(amb.IniPath);

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

            StackPanel titleStack = new StackPanel();
            titleStack.VerticalAlignment = VerticalAlignment.Center;

            TextBlock lblIniTitle = new TextBlock();
            lblIniTitle.Text = iniFileName;
            lblIniTitle.Foreground = UiTheme.TextPrimary;
            lblIniTitle.FontSize = 16;
            lblIniTitle.FontWeight = FontWeights.Bold;

            TextBlock lblIniPath = new TextBlock();
            lblIniPath.Text = amb.IniPath;
            lblIniPath.Foreground = UiTheme.TextMuted;
            lblIniPath.FontSize = 11;

            titleStack.Children.Add(lblIniTitle);
            titleStack.Children.Add(lblIniPath);

            topLeft.Children.Add(btnBackArrow);
            topLeft.Children.Add(titleStack);

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

                    sectionsContainer.Children.Add(BuildIniSectionCard(amb, sections, sec, entriesToShow, renderIniSections));
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
                txtSecName.TextChanged += delegate { sec.Name = txtSecName.Text.Trim(); };
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
            DockPanel valDock = new DockPanel();
            valDock.Margin = new Thickness(0, 0, 8, 0);

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
            valBox.TextChanged += delegate { entry.Value = valBox.Text; };
            entry.ValueBox = valBox;

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

            keyBox.TextChanged += delegate
            {
                entry.Key = keyBox.Text.Trim();
                btnPickPath.Visibility = PathTagKeys.Contains(entry.Key ?? "") ? Visibility.Visible : Visibility.Collapsed;
            };

            valDock.Children.Add(btnPickPath);
            valDock.Children.Add(valBox);
            Grid.SetColumn(valDock, 3);
            rowGrid.Children.Add(valDock);

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

            rowBorder.Child = rowGrid;
            AtualizarVisualIniLinha(entry);
            return rowBorder;
        }

        private void AbrirSeletorCaminhoPreCarregado(AmbienteInfo amb, IniSection sec, string keyName, TextBox targetBox)
        {
            string currentVal = (targetBox.Text ?? "").Trim();
            bool isFileKey = string.Equals(keyName, "ConsoleFile", StringComparison.OrdinalIgnoreCase);

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
                        string withRoot = System.IO.Path.Combine(rootPathFromSec.TrimEnd('\\', '/'), candidate.TrimStart('\\', '/'));
                        if (Directory.Exists(withRoot) || File.Exists(withRoot))
                            candidate = withRoot;
                        else
                            candidate = System.IO.Path.Combine(amb.AppDir.TrimEnd('\\', '/'), candidate.TrimStart('\\', '/'));
                    }
                    else if (!System.IO.Path.IsPathRooted(candidate))
                    {
                        candidate = System.IO.Path.GetFullPath(System.IO.Path.Combine(amb.AppDir, candidate.TrimStart('\\', '/')));
                    }

                    if (File.Exists(candidate))
                        resolvedDir = System.IO.Path.GetDirectoryName(candidate);
                    else if (Directory.Exists(candidate))
                        resolvedDir = candidate;
                    else
                    {
                        string parent = System.IO.Path.GetDirectoryName(candidate);
                        if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                            resolvedDir = parent;
                        else if (Directory.Exists(rootPathFromSec))
                            resolvedDir = rootPathFromSec;
                    }
                }
            }
            catch { }

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
                    dlg.FileName = System.IO.Path.GetFileName(currentVal);
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
                    string chosenFolder = System.IO.Path.GetDirectoryName(dlg.FileName);
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

        // =====================================================================
        // DESCOBERTA ASSINCRONA DE AMBIENTES E DETECCAO DE PID/SERVICOS/USUARIO
        // =====================================================================
        private void SelecionarPastaBase()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Selecione qualquer arquivo dentro da pasta raiz (ex: C:\\TOTVS\\bases)";
            dlg.Filter = "Pastas|*.none";
            dlg.FileName = "Selecionar esta pasta";
            dlg.CheckFileExists = false;
            dlg.CheckPathExists = true;
            if (Directory.Exists(_rootPath))
                dlg.InitialDirectory = _rootPath;

            if (dlg.ShowDialog() == true)
            {
                string chosen = System.IO.Path.GetDirectoryName(dlg.FileName);
                if (!string.IsNullOrEmpty(chosen) && Directory.Exists(chosen))
                {
                    _rootPath = chosen;
                    if (_rootPathLabel != null)
                        _rootPathLabel.Text = "Diretório: " + _rootPath;
                    File.WriteAllText(_configPath, _rootPath, Encoding.UTF8);
                    CarregarAmbientesAsync(true);
                }
            }
        }

        private void CarregarAmbientesAsync(bool animateBeam)
        {
            if (_isLoadingBases) return;
            _isLoadingBases = true;

            if (animateBeam)
                DispararAnimacaoLoadingHorizontal();

            string currentRoot = _rootPath;
            bool skipOld = _ignoreOldFolders;
            bool fetchUser = _showProcessUser;

            ThreadPool.QueueUserWorkItem(delegate
            {
                List<AmbienteInfo> novosAmbientes = new List<AmbienteInfo>();
                try
                {
                    if (Directory.Exists(currentRoot))
                    {
                        List<string> exesEncontrados = new List<string>();
                        BuscarExecutaveis(currentRoot, 0, 4, skipOld, exesEncontrados);

                        foreach (string exePath in exesEncontrados)
                        {
                            string appDir = System.IO.Path.GetDirectoryName(exePath);
                            string iniPath = System.IO.Path.Combine(appDir, "appserver.ini");
                            if (!File.Exists(iniPath))
                                continue;

                            AmbienteInfo amb = new AmbienteInfo();
                            amb.ExePath = NormalizePath(exePath);
                            amb.AppDir = NormalizePath(appDir);
                            amb.IniPath = iniPath;
                            AtualizarDadosIniAmbiente(amb);
                            novosAmbientes.Add(amb);
                        }
                    }

                    novosAmbientes = novosAmbientes.OrderBy(a => a.NomeIni).ToList();
                    SincronizarEstadoProcessosEServicos(novosAmbientes, fetchUser);
                }
                catch { }

                Dispatcher.BeginInvoke(new Action(delegate
                {
                    try
                    {
                        _ambientes = novosAmbientes;
                        _isInitialScanCompleted = true;
                        _lastUpdatedTime = DateTime.Now;
                        if (_lastUpdateLabel != null)
                            _lastUpdateLabel.Text = string.Format("Atualizado em {0:dd/MM/yyyy HH:mm:ss}  •", _lastUpdatedTime);

                        RecalcularAlertas();
                        RenderizarCards();
                    }
                    finally
                    {
                        _isLoadingBases = false;
                    }
                }));
            });
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";
            try
            {
                return System.IO.Path.GetFullPath(path.Trim().Trim('"')).TrimEnd('\\');
            }
            catch
            {
                return path.Trim().Trim('"').TrimEnd('\\');
            }
        }

        private static string ExtractExeFromServicePathName(string pathName)
        {
            if (string.IsNullOrWhiteSpace(pathName)) return "";
            string p = pathName.Trim();
            if (p.StartsWith("\""))
            {
                int endQuote = p.IndexOf('"', 1);
                if (endQuote > 1)
                    return NormalizePath(p.Substring(1, endQuote - 1));
            }
            int exeIdx = p.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exeIdx > 0)
            {
                return NormalizePath(p.Substring(0, exeIdx + 4));
            }
            return NormalizePath(p);
        }

        private void BuscarExecutaveis(string dir, int nivel, int maxNivel, bool ignoreOld, List<string> resultados)
        {
            if (nivel > maxNivel) return;

            try
            {
                string folderName = System.IO.Path.GetFileName(dir.TrimEnd('\\'));
                if (ignoreOld && !string.IsNullOrEmpty(folderName) &&
                    folderName.EndsWith(".old", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                string candidato = System.IO.Path.Combine(dir, "appserver.exe");
                if (File.Exists(candidato))
                {
                    resultados.Add(candidato);
                    return;
                }

                string[] subDirs = Directory.GetDirectories(dir);
                foreach (string sub in subDirs)
                {
                    BuscarExecutaveis(sub, nivel + 1, maxNivel, ignoreOld, resultados);
                }
            }
            catch { }
        }

        private string ObterValorChaveIni(AmbienteInfo info, string preferredSection, string keyName)
        {
            Dictionary<string, string> secDict;
            string val;
            if (!string.IsNullOrEmpty(preferredSection) &&
                info.IniSectionsMap.TryGetValue(preferredSection, out secDict) &&
                secDict.TryGetValue(keyName, out val) &&
                !string.IsNullOrWhiteSpace(val))
            {
                return val;
            }

            foreach (KeyValuePair<string, Dictionary<string, string>> kv in info.IniSectionsMap)
            {
                if (kv.Value.TryGetValue(keyName, out val) && !string.IsNullOrWhiteSpace(val))
                    return val;
            }
            return null;
        }

        private void AtualizarDadosIniAmbiente(AmbienteInfo info)
        {
            DirectoryInfo di = new DirectoryInfo(info.AppDir);
            if (di.Parent != null && di.Parent.Parent != null)
                info.PastaBase = di.Parent.Parent.Name;
            else
                info.PastaBase = di.Name;

            info.WebAppPort = null;
            info.TcpPort = null;
            info.RestPort = null;
            info.SpecialKey = null;
            info.DBAlias = null;
            info.DBPort = null;
            info.IniSectionsMap.Clear();

            string primeiroBloco = null;
            string secaoAtual = "";

            string[] linhas = File.ReadAllLines(info.IniPath, Encoding.Default);
            foreach (string rawLine in linhas)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";"))
                    continue;

                if (line.StartsWith("[") && line.Contains("]"))
                {
                    int endIdx = line.IndexOf(']');
                    string secName = line.Substring(1, endIdx - 1).Trim();
                    secaoAtual = secName;

                    if (!string.IsNullOrEmpty(secName))
                    {
                        if (primeiroBloco == null)
                            primeiroBloco = secName;

                        if (!info.IniSectionsMap.ContainsKey(secName))
                        {
                            info.IniSectionsMap[secName] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        }
                    }
                }
                else
                {
                    int eqIdx = line.IndexOf('=');
                    if (eqIdx > 0 && !string.IsNullOrEmpty(secaoAtual))
                    {
                        string key = line.Substring(0, eqIdx).Trim();
                        string val = line.Substring(eqIdx + 1);
                        int commentIdx = val.IndexOf(';');
                        if (commentIdx >= 0)
                            val = val.Substring(0, commentIdx);
                        val = val.Trim();

                        Dictionary<string, string> secDict;
                        if (info.IniSectionsMap.TryGetValue(secaoAtual, out secDict))
                        {
                            secDict[key] = val;
                        }

                        if (string.Equals(key, "PORT", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(val))
                        {
                            if (string.Equals(secaoAtual, "WEBAPP", StringComparison.OrdinalIgnoreCase))
                                info.WebAppPort = val;
                            else if (string.Equals(secaoAtual, "TCP", StringComparison.OrdinalIgnoreCase))
                                info.TcpPort = val;
                            else if (string.Equals(secaoAtual, "HTTPREST", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(secaoAtual, "HTTP", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(secaoAtual, "REST", StringComparison.OrdinalIgnoreCase))
                            {
                                info.RestPort = val;
                            }
                        }
                    }
                }
            }

            info.NomeIni = !string.IsNullOrEmpty(primeiroBloco) ? primeiroBloco : info.PastaBase;
            info.SpecialKey = ObterValorChaveIni(info, info.NomeIni, "SpecialKey");
            info.DBAlias = ObterValorChaveIni(info, info.NomeIni, "DBAlias");
            info.DBPort = ObterValorChaveIni(info, "DBACCESS", "DBPort") ?? ObterValorChaveIni(info, "TOPCONNECT", "DBPort");
        }

        private class ServiceMatchInfo
        {
            public string ServiceName;
            public string DisplayName;
            public string State;
            public int ProcessId;
        }

        private void SincronizarEstadoProcessosEServicos(List<AmbienteInfo> lista, bool fetchUser)
        {
            Dictionary<string, ServiceMatchInfo> serviceMap = new Dictionary<string, ServiceMatchInfo>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> processMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (ManagementObjectSearcher svcSearcher = new ManagementObjectSearcher(
                    "SELECT Name, DisplayName, PathName, State, ProcessId FROM Win32_Service WHERE PathName LIKE '%appserver%'"))
                {
                    foreach (ManagementObject obj in svcSearcher.Get())
                    {
                        string rawPath = obj["PathName"] != null ? obj["PathName"].ToString() : "";
                        string exeNorm = ExtractExeFromServicePathName(rawPath);
                        if (!string.IsNullOrEmpty(exeNorm) && !serviceMap.ContainsKey(exeNorm))
                        {
                            ServiceMatchInfo sm = new ServiceMatchInfo();
                            sm.ServiceName = obj["Name"] != null ? obj["Name"].ToString() : "";
                            sm.DisplayName = obj["DisplayName"] != null ? obj["DisplayName"].ToString() : sm.ServiceName;
                            sm.State = obj["State"] != null ? obj["State"].ToString() : "";
                            sm.ProcessId = obj["ProcessId"] != null ? Convert.ToInt32(obj["ProcessId"]) : 0;
                            serviceMap[exeNorm] = sm;
                        }
                    }
                }
            }
            catch { }

            try
            {
                foreach (Process p in Process.GetProcesses())
                {
                    try
                    {
                        if (p.ProcessName.IndexOf("appserver", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            string fullPath = NativeWin32.GetProcessImagePathSafe(p.Id);
                            if (string.IsNullOrEmpty(fullPath))
                            {
                                try { fullPath = p.MainModule.FileName; } catch { }
                            }

                            if (!string.IsNullOrEmpty(fullPath))
                            {
                                string exeNorm = NormalizePath(fullPath);
                                if (!string.IsNullOrEmpty(exeNorm) && !processMap.ContainsKey(exeNorm))
                                    processMap[exeNorm] = p.Id;
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

            try
            {
                using (ManagementObjectSearcher procSearcher = new ManagementObjectSearcher(
                    "SELECT ProcessId, ExecutablePath FROM Win32_Process WHERE Name LIKE 'appserver%.exe'"))
                {
                    foreach (ManagementObject obj in procSearcher.Get())
                    {
                        object pathObj = obj["ExecutablePath"];
                        object pidObj = obj["ProcessId"];
                        if (pathObj != null && pidObj != null)
                        {
                            string exeNorm = NormalizePath(pathObj.ToString());
                            int pid = Convert.ToInt32(pidObj);
                            if (!string.IsNullOrEmpty(exeNorm) && !processMap.ContainsKey(exeNorm))
                                processMap[exeNorm] = pid;
                        }
                    }
                }
            }
            catch { }

            foreach (AmbienteInfo amb in lista)
            {
                string key = NormalizePath(amb.ExePath);

                ServiceMatchInfo svc;
                if (serviceMap.TryGetValue(key, out svc))
                {
                    amb.IsServiceRegistered = true;
                    amb.ServiceName = svc.ServiceName;
                    amb.ServiceDisplayName = svc.DisplayName;
                }
                else
                {
                    amb.IsServiceRegistered = false;
                    amb.ServiceName = null;
                    amb.ServiceDisplayName = null;
                }

                if (amb.State == EnvRunState.Initializing || amb.State == EnvRunState.Stopping)
                    continue;

                if (svc != null && string.Equals(svc.State, "Running", StringComparison.OrdinalIgnoreCase))
                {
                    amb.State = EnvRunState.Active;
                    amb.RunningAsService = true;
                    int pId;
                    amb.Pid = svc.ProcessId > 0 ? svc.ProcessId : (processMap.TryGetValue(key, out pId) ? pId : 0);
                    amb.ProcessUser = (fetchUser && amb.Pid > 0) ? NativeWin32.GetProcessOwnerUser(amb.Pid) : null;
                }
                else
                {
                    int pid;
                    if (processMap.TryGetValue(key, out pid))
                    {
                        amb.State = EnvRunState.Active;
                        amb.RunningAsService = (svc != null && svc.ProcessId == pid);
                        amb.Pid = pid;
                        amb.ProcessUser = (fetchUser && pid > 0) ? NativeWin32.GetProcessOwnerUser(pid) : null;
                    }
                    else
                    {
                        amb.State = EnvRunState.Stopped;
                        amb.RunningAsService = amb.IsServiceRegistered;
                        amb.Pid = 0;
                        amb.ProcessUser = null;
                    }
                }
            }
        }

        private void DispararAtualizacaoProcessosAsync()
        {
            if (_isPollingProcesses) return;
            _isPollingProcesses = true;

            List<AmbienteInfo> snapshot = _ambientes.ToList();
            bool fetchUser = _showProcessUser;

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    SincronizarEstadoProcessosEServicos(snapshot, fetchUser);
                }
                finally
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        try
                        {
                            AtualizarResumoEVisualCards();
                        }
                        finally
                        {
                            _isPollingProcesses = false;
                        }
                    }));
                }
            });
        }

        // =====================================================================
        // MOTOR DINAMICO DE ALERTAS (TITULO CURTO: "Duplicidade em <chave>")
        // =====================================================================
        private void RecalcularAlertas()
        {
            _alertasAtivos.Clear();

            List<AmbienteInfo> ativos = _ambientes
                .Where(a => !_hiddenDirs.Contains(a.AppDir))
                .ToList();

            foreach (ConflictRule rule in _conflictRules)
            {
                if (string.IsNullOrWhiteSpace(rule.SectionPattern))
                    continue;

                bool isEnvPlaceholder = string.Equals(rule.SectionPattern, "<ambiente>", StringComparison.OrdinalIgnoreCase);

                if (rule.IsSelfSection)
                {
                    if (isEnvPlaceholder)
                    {
                        foreach (var grp in ativos.Where(a => !string.IsNullOrEmpty(a.NomeIni))
                                                  .GroupBy(a => a.NomeIni, StringComparer.OrdinalIgnoreCase)
                                                  .Where(g => g.Count() > 1))
                        {
                            AlertaConflito al = new AlertaConflito();
                            al.Titulo = string.Format("Duplicidade em [{0}]", grp.Key);
                            al.Descricao = string.Format("{0} possuem a mesma primeira seção [{1}].",
                                UiTheme.Pluralize(grp.Count(), "base", "bases"), grp.Key);
                            al.Envolvidos = grp.ToList();
                            _alertasAtivos.Add(al);
                        }
                    }
                    else
                    {
                        List<AmbienteInfo> comSecao = ativos
                            .Where(a => a.IniSectionsMap.ContainsKey(rule.SectionPattern))
                            .ToList();
                        if (comSecao.Count > 1)
                        {
                            AlertaConflito al = new AlertaConflito();
                            al.Titulo = string.Format("Duplicidade em [{0}]", rule.SectionPattern);
                            al.Descricao = string.Format("{0} possuem a seção [{1}].",
                                UiTheme.Pluralize(comSecao.Count, "base", "bases"), rule.SectionPattern);
                            al.Envolvidos = comSecao;
                            _alertasAtivos.Add(al);
                        }
                    }
                }
                else
                {
                    Dictionary<string, List<AmbienteInfo>> valueGroups = new Dictionary<string, List<AmbienteInfo>>(StringComparer.OrdinalIgnoreCase);

                    foreach (AmbienteInfo amb in ativos)
                    {
                        string targetSection = isEnvPlaceholder ? amb.NomeIni : rule.SectionPattern;
                        if (string.IsNullOrEmpty(targetSection))
                            continue;

                        Dictionary<string, string> secDict;
                        if (amb.IniSectionsMap.TryGetValue(targetSection, out secDict))
                        {
                            string foundVal;
                            if (secDict.TryGetValue(rule.KeyName, out foundVal) && !string.IsNullOrWhiteSpace(foundVal))
                            {
                                if (!valueGroups.ContainsKey(foundVal))
                                    valueGroups[foundVal] = new List<AmbienteInfo>();
                                if (!valueGroups[foundVal].Contains(amb))
                                    valueGroups[foundVal].Add(amb);
                            }
                        }
                    }

                    foreach (KeyValuePair<string, List<AmbienteInfo>> kv in valueGroups)
                    {
                        if (kv.Value.Count > 1)
                        {
                            AlertaConflito al = new AlertaConflito();
                            al.Titulo = string.Format("Duplicidade em {0}", rule.KeyName);
                            al.Descricao = string.Format("{0} possuem o mesmo valor \"{1}\" na tag [{2}] > {3}.",
                                UiTheme.Pluralize(kv.Value.Count, "base", "bases"),
                                kv.Key,
                                rule.SectionPattern,
                                rule.KeyName);
                            al.Envolvidos = kv.Value;
                            _alertasAtivos.Add(al);
                        }
                    }
                }
            }

            if (_alertBadge != null && _alertCountText != null)
            {
                _alertCountText.Text = _alertasAtivos.Count.ToString();
                _alertBadge.Background = _alertasAtivos.Count > 0
                    ? UiTheme.AccentRed
                    : UiTheme.BrushFromHex("#475569");
            }
        }

        private void AbrirCentralAlertas()
        {
            AlertCenterWindow win = new AlertCenterWindow(_alertasAtivos);
            win.Owner = this;
            win.OnOpenIniRequested = delegate(AmbienteInfo amb)
            {
                ShowIniEditorView(amb);
            };
            win.ShowDialog();
        }

        // =====================================================================
        // RENDERIZACAO DOS CARDS (LISTA EM NORMAL, GRID DE CARDS EM TELA CHEIA)
        // =====================================================================
        private void AtualizarBotaoOcultos()
        {
            if (_btnToggleHiddenView == null) return;
            _btnToggleHiddenView.Content = _showingHidden ? "👁 Ativos" : "👁 Ocultos";
            _btnToggleHiddenView.Background = UiTheme.BrushFromHex(UiTheme.NeutralBtnHex);
        }

        private void RenderizarCards()
        {
            if (_cardsContainer == null) return;

            AtualizarBotaoOcultos();
            _cardsContainer.Children.Clear();

            bool isMaximized = (WindowState == WindowState.Maximized);
            if (isMaximized)
                AtualizarColunasGridMaximizado();

            // Animacao vetorial enquanto a primeira varredura nao conclui
            if (!_isInitialScanCompleted)
            {
                UniformGrid ug = _cardsContainer as UniformGrid;
                if (ug != null) ug.Columns = 1;
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
                if (isMaximized)
                {
                    cardUi.Margin = new Thickness(5, 0, 5, 10);
                }
                _cardsContainer.Children.Add(cardUi);
            }

            if (visiveis == 0)
            {
                UniformGrid ug = _cardsContainer as UniformGrid;
                if (ug != null) ug.Columns = 1;

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

            // ================= COLUNA 0 (ESQUERDA): LAPIS E OLHO CENTRALIZADOS VERTICALMENTE + STATUS ABAIXO =================
            StackPanel leftToolsColumn = new StackPanel();
            leftToolsColumn.Orientation = Orientation.Vertical;
            leftToolsColumn.VerticalAlignment = VerticalAlignment.Center;
            leftToolsColumn.HorizontalAlignment = HorizontalAlignment.Center;
            leftToolsColumn.Margin = new Thickness(0, 0