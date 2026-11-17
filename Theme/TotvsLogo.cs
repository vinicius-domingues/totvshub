using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace TOTVSHub
{
    public static class TotvsLogo
    {
        public const string DownloadsTotvsWhiteSvgPath = @"C:\Users\vinicius.domingues\Downloads\totvs_white.svg";

        // Conteudo exato do arquivo oficial totvs_white.svg embarcado no codigo
        public const string EmbeddedTotvsWhiteSvg =
@"<?xml version=""1.0"" standalone=""no""?>
<!DOCTYPE svg PUBLIC ""-//W3C//DTD SVG 20010904//EN""
 ""http://www.w3.org/TR/2001/REC-SVG-20010904/DTD/svg10.dtd"">
<svg version=""1.0"" xmlns=""http://www.w3.org/2000/svg""
 width=""900.000000pt"" height=""900.000000pt"" viewBox=""0 0 900.000000 900.000000""
 preserveAspectRatio=""xMidYMid meet"">

<g transform=""translate(0.000000,900.000000) scale(0.100000,-0.100000)""
fill=""#ffffff"" stroke=""none"">
<path d=""M4375 7365 c-77 -12 -116 -22 -270 -66 -74 -21 -326 -104 -360 -119
 -11 -4 -47 -18 -80 -30 -61 -23 -95 -37 -162 -66 -21 -9 -51 -22 -68 -29 -363
 -153 -896 -471 -1078 -644 -95 -90 -133 -164 -175 -341 -41 -171 -53 -239 -82
 -480 -13 -111 -14 -2075 0 -2168 5 -37 14 -110 21 -162 6 -52 17 -124 25 -160
 8 -36 19 -90 25 -120 36 -175 80 -273 157 -355 98 -105 386 -300 687 -467 97
 -54 396 -199 470 -228 11 -4 36 -15 57 -24 20 -9 39 -16 42 -16 3 0 25 -8 49
 -19 23 -10 58 -24 77 -31 19 -7 44 -16 55 -21 27 -11 272 -91 360 -117 352
 -103 472 -94 973 74 459 153 858 347 1254 610 302 201 392 294 436 459 58 214
 91 414 112 670 13 171 13 1816 0 1978 -18 210 -44 395 -70 497 -5 19 -17 67
 -26 105 -50 213 -109 290 -349 457 -132 90 -378 248 -389 248 -3 0 -29 14 -58
 32 -52 31 -403 208 -412 208 -3 0 -30 11 -59 25 -187 85 -586 223 -802 276
 -70 17 -209 39 -245 37 -19 0 -71 -6 -115 -13z m340 -1644 c388 -105 718 -231
 1055 -400 186 -94 229 -118 389 -216 310 -189 469 -323 523 -442 28 -60 31
 -78 31 -158 0 -102 -22 -166 -89 -256 -44 -60 -275 -236 -419 -319 -16 -10
 -79 -48 -140 -84 -113 -69 -487 -258 -585 -296 -30 -12 -71 -28 -90 -37 -88
 -38 -209 -83 -341 -126 -134 -44 -224 -71 -399 -117 -25 -7 -94 -12 -155 -12
 -111 0 -133 4 -340 64 -38 11 -79 22 -90 25 -11 2 -29 8 -40 12 -11 5 -45 16
 -75 26 -353 114 -753 298 -1080 495 -231 139 -410 269 -483 350 -145 161 -147
 378 -5 544 119 139 618 452 998 624 36 16 87 39 115 52 82 37 325 130 410 157
 44 14 89 29 100 33 71 26 305 92 360 101 87 13 263 3 350 -20z""/>
</g>
</svg>";

        // Garante que o SVG oficial totvs_white.svg esteja salvo em %APPDATA%\TOTVSHub\totvs_white.svg
        public static string EnsureTotvsWhiteSvgInAppData(string appDataDir)
        {
            string cachedSvgPath = Path.Combine(appDataDir, "totvs_white.svg");
            try
            {
                string localSvg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "totvs_white.svg");
                if (File.Exists(DownloadsTotvsWhiteSvgPath))
                {
                    string dlContent = File.ReadAllText(DownloadsTotvsWhiteSvgPath, Encoding.UTF8);
                    if (dlContent.Contains("M4375 7365"))
                    {
                        File.WriteAllText(cachedSvgPath, dlContent, Encoding.UTF8);
                        return cachedSvgPath;
                    }
                }

                if (File.Exists(localSvg))
                {
                    string localContent = File.ReadAllText(localSvg, Encoding.UTF8);
                    if (localContent.Contains("M4375 7365"))
                    {
                        File.WriteAllText(cachedSvgPath, localContent, Encoding.UTF8);
                        return cachedSvgPath;
                    }
                }

                bool needsWrite = !File.Exists(cachedSvgPath);
                if (!needsWrite)
                {
                    string existing = File.ReadAllText(cachedSvgPath, Encoding.UTF8);
                    if (!existing.Contains("M4375 7365"))
                        needsWrite = true;
                }

                if (needsWrite)
                {
                    File.WriteAllText(cachedSvgPath, EmbeddedTotvsWhiteSvg, Encoding.UTF8);
                }
            }
            catch
            {
                try { File.WriteAllText(cachedSvgPath, EmbeddedTotvsWhiteSvg, Encoding.UTF8); } catch { }
            }
            return cachedSvgPath;
        }

        // Extrai e constroi a geometria fiel do totvs_white.svg (aplicando a inversao de eixo Y do Potrace)
        public static Geometry BuildTotvsLogoGeometry(string cachedSvgPath)
        {
            string svgContent = EmbeddedTotvsWhiteSvg;
            try
            {
                if (!string.IsNullOrEmpty(cachedSvgPath) && File.Exists(cachedSvgPath))
                {
                    string read = File.ReadAllText(cachedSvgPath, Encoding.UTF8);
                    if (!string.IsNullOrWhiteSpace(read) && read.Contains("<path"))
                        svgContent = read;
                }
            }
            catch { }

            GeometryGroup group = new GeometryGroup();
            group.FillRule = FillRule.EvenOdd;

            MatchCollection pathMatches = Regex.Matches(svgContent, "<path[^>]*\\bd\\s*=\\s*\"([^\"]+)\"[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (Match m in pathMatches)
            {
                string d = Regex.Replace(m.Groups[1].Value, "\\s+", " ").Trim();
                if (string.IsNullOrEmpty(d)) continue;
                try
                {
                    Geometry parsed = Geometry.Parse(d);
                    group.Children.Add(parsed);
                }
                catch { }
            }

            if (Regex.IsMatch(svgContent, "scale\\s*\\(\\s*[0-9.]+\\s*,\\s*-[0-9.]+\\s*\\)", RegexOptions.IgnoreCase))
            {
                group.Transform = new ScaleTransform(1.0, -1.0);
            }

            return group;
        }

        // Renderiza o simbolo vetorial branco do totvs_white.svg para o topo e telas do app
        public static UIElement CreateTotvsWhiteSvgLogo(string cachedSvgPath, double targetSize)
        {
            System.Windows.Shapes.Path logoPath = new System.Windows.Shapes.Path();
            logoPath.Data = BuildTotvsLogoGeometry(cachedSvgPath);
            logoPath.Fill = Brushes.White;
            logoPath.Stretch = Stretch.Uniform;
            logoPath.VerticalAlignment = VerticalAlignment.Center;
            logoPath.HorizontalAlignment = HorizontalAlignment.Center;

            if (UiTheme.IsDark)
            {
                logoPath.Width = targetSize;
                logoPath.Height = targetSize;
                return logoPath;
            }
            else
            {
                // No tema claro, envolve o logo branco (#FFFFFF) em um badge escuro elegante para manter o logo 100% branco e visivel
                Border darkBadge = new Border();
                darkBadge.Width = targetSize + 8;
                darkBadge.Height = targetSize + 8;
                darkBadge.CornerRadius = new CornerRadius((targetSize + 8) * 0.24);
                darkBadge.Background = UiTheme.BrushFromHex("#0F172A");
                darkBadge.VerticalAlignment = VerticalAlignment.Center;
                logoPath.Width = targetSize - 2;
                logoPath.Height = targetSize - 2;
                darkBadge.Child = logoPath;
                return darkBadge;
            }
        }

        // Renderiza o icone oficial em BitmapSource WPF (para Window.Icon, .ico e Bandeja do Windows)
        public static BitmapSource RenderTotvsIconBitmapSource(string cachedSvgPath, int size)
        {
            Grid root = new Grid();
            root.Width = size;
            root.Height = size;
            root.Background = Brushes.Transparent;

            System.Windows.Shapes.Path logoPath = new System.Windows.Shapes.Path();
            logoPath.Data = BuildTotvsLogoGeometry(cachedSvgPath);
            logoPath.Fill = Brushes.White;
            logoPath.Stretch = Stretch.Uniform;
            logoPath.Width = size * 0.90;
            logoPath.Height = size * 0.90;
            logoPath.HorizontalAlignment = HorizontalAlignment.Center;
            logoPath.VerticalAlignment = VerticalAlignment.Center;

            root.Children.Add(logoPath);

            root.Measure(new System.Windows.Size(size, size));
            root.Arrange(new Rect(0, 0, size, size));
            root.UpdateLayout();

            RenderTargetBitmap rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(root);
            rtb.Freeze();
            return rtb;
        }

        public static Bitmap CreateTotvsLogoBitmap(string cachedSvgPath, int size)
        {
            BitmapSource rtb = RenderTotvsIconBitmapSource(cachedSvgPath, size);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                ms.Position = 0;
                using (Bitmap temp = new Bitmap(ms))
                {
                    return new Bitmap(temp);
                }
            }
        }

        public static Icon CreateTotvsTrayIcon(string cachedSvgPath)
        {
            using (Bitmap bmp = CreateTotvsLogoBitmap(cachedSvgPath, 32))
            {
                IntPtr hIcon = bmp.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }

        public static ImageSource CreateTotvsWindowImageSource(string cachedSvgPath)
        {
            return RenderTotvsIconBitmapSource(cachedSvgPath, 64);
        }

        // Gera um arquivo .ico multi-resolucao (16, 32, 48, 256 px) fiel ao totvs_white.svg
        public static void WriteMultiSizeIcoFile(string cachedSvgPath, string icoOutputPath)
        {
            int[] sizes = new int[] { 16, 32, 48, 256 };
            byte[][] pngPayloads = new byte[sizes.Length][];

            for (int i = 0; i < sizes.Length; i++)
            {
                BitmapSource rtb = RenderTotvsIconBitmapSource(cachedSvgPath, sizes[i]);
                PngBitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtb));
                using (MemoryStream ms = new MemoryStream())
                {
                    enc.Save(ms);
                    pngPayloads[i] = ms.ToArray();
                }
            }

            using (FileStream fs = new FileStream(icoOutputPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                // ICONDIR header
                bw.Write((ushort)0); // Reserved
                bw.Write((ushort)1); // Type: 1 = ICO
                bw.Write((ushort)sizes.Length); // Count

                int offset = 6 + (16 * sizes.Length);
                for (int i = 0; i < sizes.Length; i++)
                {
                    int s = sizes[i];
                    bw.Write((byte)(s >= 256 ? 0 : s)); // Width
                    bw.Write((byte)(s >= 256 ? 0 : s)); // Height
                    bw.Write((byte)0);                  // ColorCount
                    bw.Write((byte)0);                  // Reserved
                    bw.Write((ushort)1);                // Planes
                    bw.Write((ushort)32);               // BitCount
                    bw.Write((uint)pngPayloads[i].Length); // BytesInRes
                    bw.Write((uint)offset);             // ImageOffset
                    offset += pngPayloads[i].Length;
                }

                for (int i = 0; i < sizes.Length; i++)
                {
                    bw.Write(pngPayloads[i]);
                }
            }
        }
    }
}
