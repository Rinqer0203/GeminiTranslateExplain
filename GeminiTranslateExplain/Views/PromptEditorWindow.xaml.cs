using GeminiTranslateExplain.ViewModels;
using System.Windows;

namespace GeminiTranslateExplain.Views
{
    /// <summary>
    /// PromptEditorWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class PromptEditorWindow : Window
    {
        public PromptEditorWindow()
        {
            InitializeComponent();
            this.Closed += (_, _) =>
            {
                if (this.DataContext is PromptEditorViewModel vm)
                {
                    vm.OnClosed();
                }
            };
        }
    }
}
