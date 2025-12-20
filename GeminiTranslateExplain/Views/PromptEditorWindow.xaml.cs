using GeminiTranslateExplain.ViewModels;
using GeminiTranslateExplain.Services;
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
            WindowUtilities.ApplyTitleBarTheme(this);
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

