using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TOTVSHub
{
    public class AlertCenterWindow : Window
    {
        public Action<AmbienteInfo> OnOpenIniRequested { get; set; }

        public AlertCenterWindow(List<AlertaConflito> alertas)
        {
            Title = string.Format("Alertas ({0})", alertas.Count);
            Icon = TotvsLogo.CreateTotvsWindowImageSource(null);
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
}
