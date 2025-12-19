using GeminiTranslateExplain.Models;
using System.Windows;
using System.Windows.Input;

namespace GeminiTranslateExplain
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window
    {
        public SettingWindow()
        {
            InitializeComponent();
            this.Closed += (_, _) =>
            {
                if (this.DataContext is SettingWindowViewModel vm)
                {
                    vm.OnClosed();
                }
            };
        }

        private void GlobalHotKeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is not SettingWindowViewModel vm)
                return;

            if (!TryBuildHotKey(e, out var hotKey))
            {
                e.Handled = true;
                return;
            }

            vm.SetGlobalHotKey(hotKey.Value);
            e.Handled = true;
        }

        private void ScreenshotHotKeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is not SettingWindowViewModel vm)
                return;

            if (!TryBuildHotKey(e, out var hotKey))
            {
                e.Handled = true;
                return;
            }

            vm.SetScreenshotHotKey(hotKey.Value);
            e.Handled = true;
        }

        private static bool TryBuildHotKey(System.Windows.Input.KeyEventArgs e, out HotKeyDefinition? hotKey)
        {
            hotKey = null;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (IsModifierKey(key))
            {
                return false;
            }

            var modifiers = Keyboard.Modifiers;
            if (modifiers == ModifierKeys.None)
            {
                System.Windows.MessageBox.Show("修飾キー（Ctrl/Alt/Shift/Win）を含めてください。", "ショートカット設定", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            hotKey = new HotKeyDefinition(modifiers, key);
            return true;
        }

        private static bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl
                || key == Key.LeftShift || key == Key.RightShift
                || key == Key.LeftAlt || key == Key.RightAlt
                || key == Key.LWin || key == Key.RWin;
        }
    }
}
