using System.Windows;
using System.Windows.Input;
using TextBox = System.Windows.Controls.TextBox;
{

}

namespace GeminiTranslateExplain
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
            if (d is not TextBox textBox)
                return;

            if ((bool)e.NewValue)
            {
                textBox.PreviewMouseWheel += OnPreviewMouseWheel;
            }
            else
            {
                textBox.PreviewMouseWheel -= OnPreviewMouseWheel;
            }
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            // Ctrlキーが押されているときだけ動作
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                double currentSize = textBox.FontSize;
                double delta = e.Delta > 0 ? 1 : -1;
                double newSize = currentSize + delta;

                // 最小・最大サイズ制限（任意）
                newSize = Math.Max(8, Math.Min(40, newSize));

                textBox.FontSize = newSize;
                e.Handled = true;
            }
        }
    }
}
