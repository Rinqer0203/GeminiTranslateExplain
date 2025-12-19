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

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (IsModifierKey(key))
            {
                e.Handled = true;
                return;
            }

            var modifiers = Keyboard.Modifiers;
            if (modifiers == ModifierKeys.None)
            {
                System.Windows.MessageBox.Show("修飾キー（Ctrl/Alt/Shift/Win）を含めてください。", "ショートカット設定", MessageBoxButton.OK, MessageBoxImage.Information);
                e.Handled = true;
                return;
            }

            vm.SetGlobalHotKey(new HotKeyDefinition(modifiers, key));
            e.Handled = true;
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
