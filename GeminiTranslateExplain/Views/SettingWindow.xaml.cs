using System.Windows;

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
    }
}
