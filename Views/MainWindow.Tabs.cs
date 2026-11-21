using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TOTVSHub
{
    public partial class MainWindow : Window
    {
        private HubActiveTab _currentTab = HubActiveTab.Ambientes;

        public HubActiveTab CurrentTab
        {
            get { return _currentTab; }
            set { _currentTab = value; }
        }

        public void SwitchToTab(HubActiveTab tab)
        {
            _currentTab = tab;
            switch (tab)
            {
                case HubActiveTab.HomeOverview:
                    ShowHomeOverviewView();
                    break;
                case HubActiveTab.Ambientes:
                    ShowDashboardView();
                    break;
                case HubActiveTab.DbAccess:
                    ShowDbAccessView();
                    break;
                case HubActiveTab.SmartView:
                    ShowSmartView();
                    break;
            }
        }

        public StackPanel CreateBrandHeaderWithTabs(HubActiveTab activeTab)
        {
            StackPanel headerStack = new StackPanel();
            headerStack.Tag = "BrandWithTabs";
            headerStack.Orientation = Orientation.Horizontal;
            headerStack.VerticalAlignment = VerticalAlignment.Center;

            StackPanel brand = CreateBrandTitleHeader();
            headerStack.Children.Add(brand);

            Border separator = new Border();
            separator.Width = 1;
            separator.Height = 22;
            separator.Background = UiTheme.BorderSubtle;
            separator.Margin = new Thickness(18, 0, 16, 0);
            separator.VerticalAlignment = VerticalAlignment.Center;
            headerStack.Children.Add(separator);

            UIElement tabs = BuildBrowserTabBar(activeTab);
            headerStack.Children.Add(tabs);

            return headerStack;
        }

        public UIElement BuildBrowserTabBar()
        {
            return BuildBrowserTabBar(_currentTab);
        }

        public UIElement BuildBrowserTabBar(HubActiveTab activeTab)
        {
            _currentTab = activeTab;

            StackPanel tabContainer = new StackPanel();
            tabContainer.Tag = "BrowserTabs";
            tabContainer.Orientation = Orientation.Horizontal;
            tabContainer.VerticalAlignment = VerticalAlignment.Bottom;
            tabContainer.Margin = new Thickness(0, 0, 0, 0);

            // Tab 1: Ambientes
            string ambCountStr = (_ambientes != null && _ambientes.Count > 0) ? _ambientes.Count.ToString() : null;
            UIElement tabAmbientes = CreateBrowserTabItem(
                "Ambientes",
                CreateTabGridIcon(activeTab == HubActiveTab.Ambientes ? UiTheme.AccentLightBlue : UiTheme.TextSecondary, 14),
                activeTab == HubActiveTab.Ambientes,
                ambCountStr,
                null,
                delegate { SwitchToTab(HubActiveTab.Ambientes); });
            tabContainer.Children.Add(tabAmbientes);

            // Tab 2: DBAccess (sem sinal de ligado na aba)
            UIElement tabDbAccess = CreateBrowserTabItem(
                "DBAccess",
                CreateTabDatabaseIcon(activeTab == HubActiveTab.DbAccess ? UiTheme.AccentLightBlue : UiTheme.TextSecondary, 14),
                activeTab == HubActiveTab.DbAccess,
                null,
                null,
                delegate { SwitchToTab(HubActiveTab.DbAccess); });
            tabContainer.Children.Add(tabDbAccess);

            // Tab 3: SmartView (sem sinal de ligado na aba)
            UIElement tabSmartView = CreateBrowserTabItem(
                "SmartView",
                CreateTabChartIcon(activeTab == HubActiveTab.SmartView ? UiTheme.AccentLightBlue : UiTheme.TextSecondary, 14),
                activeTab == HubActiveTab.SmartView,
                null,
                null,
                delegate { SwitchToTab(HubActiveTab.SmartView); });
            tabContainer.Children.Add(tabSmartView);

            return tabContainer;
        }

        private UIElement CreateBrowserTabItem(
            string label,
            UIElement icon,
            bool isActive,
            string badgeText,
            Brush statusDotBrush,
            Action onClick)
        {
            Border tabBorder = new Border();
            tabBorder.Height = 36;
            tabBorder.Padding = new Thickness(14, 0, 14, 0);
            tabBorder.CornerRadius = new CornerRadius(8, 8, 0, 0);
            tabBorder.VerticalAlignment = VerticalAlignment.Bottom;

            Brush normalBg = isActive ? UiTheme.BgCard : Brushes.Transparent;
            Brush normalBorder = isActive ? UiTheme.BorderSubtle : Brushes.Transparent;
            tabBorder.Background = normalBg;
            tabBorder.BorderBrush = normalBorder;
            tabBorder.BorderThickness = new Thickness(1, 1, 1, 0);

            Grid innerGrid = new Grid();
            innerGrid.VerticalAlignment = VerticalAlignment.Center;

            StackPanel contentStack = new StackPanel();
            contentStack.Orientation = Orientation.Horizontal;
            contentStack.VerticalAlignment = VerticalAlignment.Center;

            if (icon != null)
            {
                FrameworkElement feIcon = icon as FrameworkElement;
                if (feIcon != null)
                {
                    feIcon.Margin = new Thickness(0, 0, 8, 0);
                    feIcon.VerticalAlignment = VerticalAlignment.Center;
                }
                contentStack.Children.Add(icon);
            }

            TextBlock textBlock = new TextBlock();
            textBlock.Text = label;
            textBlock.FontSize = 13;
            textBlock.FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal;
            textBlock.Foreground = isActive ? UiTheme.TextPrimary : UiTheme.TextSecondary;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            contentStack.Children.Add(textBlock);

            if (statusDotBrush != null)
            {
                Border dot = new Border();
                dot.Width = 7;
                dot.Height = 7;
                dot.CornerRadius = new CornerRadius(3.5);
                dot.Background = statusDotBrush;
                dot.Margin = new Thickness(8, 0, 0, 0);
                dot.VerticalAlignment = VerticalAlignment.Center;
                contentStack.Children.Add(dot);
            }
            else if (!string.IsNullOrEmpty(badgeText))
            {
                Border badge = new Border();
                badge.Background = isActive ? UiTheme.TagBg : UiTheme.BrushFromHex("#1E293B");
                badge.CornerRadius = new CornerRadius(8);
                badge.Padding = new Thickness(6, 1, 6, 1);
                badge.Margin = new Thickness(7, 0, 0, 0);
                badge.VerticalAlignment = VerticalAlignment.Center;

                TextBlock badgeTxt = new TextBlock();
                badgeTxt.Text = badgeText;
                badgeTxt.FontSize = 10;
                badgeTxt.FontWeight = FontWeights.Bold;
                badgeTxt.Foreground = isActive ? UiTheme.AccentLightBlue : UiTheme.TextMuted;
                badge.Child = badgeTxt;
                contentStack.Children.Add(badge);
            }

            innerGrid.Children.Add(contentStack);

            tabBorder.Child = innerGrid;

            // Hover effects for non-active tabs
            if (!isActive)
            {
                tabBorder.MouseEnter += delegate
                {
                    tabBorder.Background = UiTheme.BrushFromHex(UiTheme.IsDark ? "#1E293B" : "#E2E8F0");
                    textBlock.Foreground = UiTheme.TextPrimary;
                };
                tabBorder.MouseLeave += delegate
                {
                    tabBorder.Background = Brushes.Transparent;
                    textBlock.Foreground = UiTheme.TextSecondary;
                };
            }

            // Utiliza Button real com template transparente para evitar que o arrasto do TopBar intercepte o clique
            Button tabButton = new Button();
            tabButton.Tag = "TabItem";
            tabButton.Background = Brushes.Transparent;
            tabButton.BorderThickness = new Thickness(0);
            tabButton.Padding = new Thickness(0);
            tabButton.Margin = new Thickness(0, 0, 4, 0);
            tabButton.Cursor = Cursors.Hand;
            tabButton.VerticalAlignment = VerticalAlignment.Bottom;
            tabButton.Height = 36;

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory fef = new FrameworkElementFactory(typeof(ContentPresenter));
            template.VisualTree = fef;
            tabButton.Template = template;
            tabButton.Content = tabBorder;

            tabButton.Click += delegate
            {
                if (onClick != null)
                    onClick();
            };

            return tabButton;
        }

        private static UIElement CreateTabGridIcon(Brush strokeOrFill, double size = 14)
        {
            Canvas canvas = new Canvas();
            canvas.Width = size;
            canvas.Height = size;

            Path path = new Path();
            path.Data = Geometry.Parse("M 2,2 L 6.5,2 L 6.5,6.5 L 2,6.5 Z M 8.5,2 L 13,2 L 13,6.5 L 8.5,6.5 Z M 2,8.5 L 6.5,8.5 L 6.5,13 L 2,13 Z M 8.5,8.5 L 13,8.5 L 13,13 L 8.5,13 Z");
            path.Fill = strokeOrFill;
            canvas.Children.Add(path);
            return canvas;
        }

        private static UIElement CreateTabDatabaseIcon(Brush strokeOrFill, double size = 14)
        {
            Canvas canvas = new Canvas();
            canvas.Width = size;
            canvas.Height = size;

            Path path = new Path();
            path.Data = Geometry.Parse("M 2.5,4 C 2.5,2.5 12.5,2.5 12.5,4 C 12.5,5.5 2.5,5.5 2.5,4 Z M 2.5,4 L 2.5,7.5 C 2.5,9 12.5,9 12.5,7.5 L 12.5,4 M 2.5,7.5 L 2.5,11 C 2.5,12.5 12.5,12.5 12.5,11 L 12.5,7.5");
            path.Stroke = strokeOrFill;
            path.StrokeThickness = 1.4;
            path.StrokeLineJoin = PenLineJoin.Round;
            path.StrokeStartLineCap = PenLineCap.Round;
            path.StrokeEndLineCap = PenLineCap.Round;
            canvas.Children.Add(path);
            return canvas;
        }

        private static UIElement CreateTabChartIcon(Brush strokeOrFill, double size = 14)
        {
            Canvas canvas = new Canvas();
            canvas.Width = size;
            canvas.Height = size;

            Path path = new Path();
            path.Data = Geometry.Parse("M 2,13 L 13,13 M 4,11 L 4,8.5 M 7.5,11 L 7.5,5 M 11,11 L 11,2.5");
            path.Stroke = strokeOrFill;
            path.StrokeThickness = 1.6;
            path.StrokeStartLineCap = PenLineCap.Round;
            path.StrokeEndLineCap = PenLineCap.Round;
            canvas.Children.Add(path);
            return canvas;
        }
    }
}
