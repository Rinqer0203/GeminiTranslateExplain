using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WpfControl = System.Windows.Controls.Control;
using WpfApplication = System.Windows.Application;

namespace QuickExplain
{
    public static class FontZoomBehavior
    {
        public static readonly DependencyProperty IsZoomEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsZoomEnabled",
                typeof(bool),
                typeof(FontZoomBehavior),
                new PropertyMetadata(false, OnIsZoomEnabledChanged));

        public static bool GetIsZoomEnabled(DependencyObject obj) =>
            (bool)obj.GetValue(IsZoomEnabledProperty);

        public static void SetIsZoomEnabled(DependencyObject obj, bool value) =>
            obj.SetValue(IsZoomEnabledProperty, value);

        private static void OnIsZoomEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not WpfControl control)
                return;

            if ((bool)e.NewValue)
            {
                control.PreviewMouseWheel += OnPreviewMouseWheel;
            }
            else
            {
                control.PreviewMouseWheel -= OnPreviewMouseWheel;
            }
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not WpfControl control)
                return;

            // Ctrlキーが押されているときだけ動作
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                double currentSize = control.FontSize;
                double delta = e.Delta > 0 ? 1 : -1;
                double newSize = currentSize + delta;

                // 最小・最大サイズ制限（任意）
                newSize = Math.Max(8, Math.Min(40, newSize));

                control.FontSize = newSize;
                UpdateMarkdownFontResources(control, newSize);
                e.Handled = true;
            }
        }

        private static void UpdateMarkdownFontResources(DependencyObject source, double baseFontSize)
        {
            var resources = FindResourceDictionary(source, "MarkdownFontSize");
            if (resources == null)
                return;

            resources["MarkdownFontSize"] = baseFontSize;
            resources["MarkdownHeading1FontSize"] = baseFontSize * 1.55;
            resources["MarkdownHeading2FontSize"] = baseFontSize * 1.35;
            resources["MarkdownHeading3FontSize"] = baseFontSize * 1.20;
            resources["MarkdownHeading4FontSize"] = baseFontSize * 1.10;
            resources["MarkdownHeading5FontSize"] = baseFontSize;
            resources["MarkdownHeading6FontSize"] = baseFontSize * 0.95;
        }

        private static ResourceDictionary? FindResourceDictionary(DependencyObject source, string key)
        {
            var current = source;
            while (current != null)
            {
                if (current is FrameworkElement element && element.Resources.Contains(key))
                    return element.Resources;

                var logicalParent = LogicalTreeHelper.GetParent(current);
                if (logicalParent != null)
                {
                    current = logicalParent;
                    continue;
                }

                current = current is Visual or Visual3D
                    ? VisualTreeHelper.GetParent(current)
                    : null;
            }

            return WpfApplication.Current.Resources.Contains(key)
                ? WpfApplication.Current.Resources
                : null;
        }
    }
}
