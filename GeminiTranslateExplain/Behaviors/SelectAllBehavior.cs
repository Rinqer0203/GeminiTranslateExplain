using System.Windows;
using System.Windows.Input;
using TextBox = System.Windows.Controls.TextBox;

namespace GeminiTranslateExplain
{
    public static class SelectAllBehavior
    {
        /// <summary>
        /// IsSelectAllOnFocusEnabled 添付プロパティ
        /// </summary>
        public static readonly DependencyProperty IsSelectAllOnFocusEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsSelectAllOnFocusEnabled",
                typeof(bool),
                typeof(SelectAllBehavior),
                new PropertyMetadata(false, OnIsSelectAllOnFocusEnabledChanged));

        public static bool GetIsSelectAllOnFocusEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsSelectAllOnFocusEnabledProperty);
        }

        public static void SetIsSelectAllOnFocusEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsSelectAllOnFocusEnabledProperty, value);
        }

        private static void OnIsSelectAllOnFocusEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBox || e.NewValue is not bool isEnabled)
                return;

            // イベントを一旦解除してから再登録
            textBox.GotFocus -= OnTextBoxGotFocus;
            textBox.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;

            if (isEnabled)
            {
                textBox.GotFocus += OnTextBoxGotFocus;
                textBox.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            }
        }

        private static void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && GetIsSelectAllOnFocusEnabled(textBox))
            {
                textBox.SelectAll();
            }
        }

        private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox && !textBox.IsKeyboardFocusWithin)
            {
                textBox.Focus();
                e.Handled = true; // 後続のクリック処理をキャンセル（選択状態を防ぐ）
            }
        }
    }
}
