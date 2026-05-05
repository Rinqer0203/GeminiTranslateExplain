using GeminiTranslateExplain.Services;
using GeminiTranslateExplain.ViewModels;
using System.Windows;

namespace GeminiTranslateExplain.Views
{
    /// <summary>
    /// ModelAddWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ModelAddWindow : Window
    {
        public ModelAddWindow(ModelAddWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            WindowUtilities.ApplyTitleBarTheme(this);

            Loaded += (_, _) =>
            {
                if (viewModel.RefreshModelsCommand.CanExecute(null))
                    viewModel.RefreshModelsCommand.Execute(null);
            };
        }
    }
}
