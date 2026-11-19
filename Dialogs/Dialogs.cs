using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TOTVSHub
{
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

#if CORP
            Title = title ?? "TOTVSHub Corp";
#else
            Title = title ?? "TOTVSHub";
#endif
            Icon = TotvsLogo.CreateTotvsWindowImageSource(null);
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
            iconBadge.Padding = new Thickness(7, 4, 8, 4);
            iconBadge.Margin = new Thickness(0, 0, 10, 0);

            StackPanel badgeStack = new StackPanel();
            badgeStack.Orientation = Orientation.Horizontal;
            badgeStack.VerticalAlignment = VerticalAlignment.Center;
            UIElement miniLogo = TotvsLogo.CreateTotvsWhiteSvgLogo(null, 13);
            badgeStack.Children.Add(miniLogo);

            TextBlock iconTxt = new TextBlock();
#if CORP
            iconTxt.Text = "TOTVSHub Corp";
#else
            iconTxt.Text = "TOTVSHub";
#endif
            iconTxt.Foreground = Brushes.White;
            iconTxt.FontWeight = FontWeights.Bold;
            iconTxt.FontSize = 11;
            iconTxt.Margin = new Thickness(5, 0, 0, 0);
            iconTxt.VerticalAlignment = VerticalAlignment.Center;
            badgeStack.Children.Add(iconTxt);
            iconBadge.Child = badgeStack;

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

                toggleCard.Cursor = Cursors.Hand;
                toggleCard.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                };
                toggleCard.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
                {
                    e.Handled = true;
                    FrameworkElement fe = toggleCtrl as FrameworkElement;
                    Action doToggle = fe != null ? fe.Tag as Action : null;
                    if (doToggle != null) doToggle();
                };

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
            return;
        }
    }
}
