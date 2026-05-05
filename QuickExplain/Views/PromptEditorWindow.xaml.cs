using QuickExplain.ViewModels;
using QuickExplain.Services;
using System.Windows;

namespace QuickExplain.Views
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

