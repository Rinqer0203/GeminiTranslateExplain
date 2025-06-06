using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeminiTranslateExplain.Models;
using GeminiTranslateExplain.Services;
using GeminiTranslateExplain.ViewModels;

namespace GeminiTranslateExplain
{
    internal partial class MainWindowViewModel : ObservableObject, IProgressTextReceiver
    {
        public AIModel[] GeminiModelNames { get; } = UsableAIModels.Models;

        string IProgressTextReceiver.Text
        {
            set => TranslatedText = value;
        }


        [ObservableProperty]
        private AIModel _selectedGeminiModel = AppConfig.Instance.SelectedGeminiModel;

        [ObservableProperty]
        private string _sourceText = string.Empty;

        [ObservableProperty]
        private string _translatedText = string.Empty;

        [ObservableProperty]
        private string _questionText = string.Empty;

        [ObservableProperty]
        private bool _useCustomInstruction = AppConfig.Instance.UseCustomInstruction;

        public MainWindowViewModel()
        {
            ApiManager.Instance.RegisterProgressReceiver(this);
        }

        [RelayCommand]
        private async Task TranslateText()
        {
            if (string.IsNullOrWhiteSpace(SourceText))
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            var instance = ApiManager.Instance;
            instance.ClearMessages();
            instance.AddMessage("user", SourceText);
            await instance.RequestTranslation();
        }

        [RelayCommand]
        private async Task SendQuestion()
        {
            if (string.IsNullOrWhiteSpace(QuestionText))
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            var instance = ApiManager.Instance;
            instance.AddMessage("user", QuestionText);
            QuestionText = string.Empty;
            await instance.RequestTranslation();
        }


        [RelayCommand]
        private static void OpenSettingWindow()
        {
            var settingWindow = new SettingWindow();
            settingWindow.Owner = System.Windows.Application.Current.MainWindow;  // 所有者を明示
            settingWindow.ShowDialog();  // モーダル表示
        }

        partial void OnSelectedGeminiModelChanged(AIModel value)
        {
            AppConfig.Instance.SelectedGeminiModel = value;
        }

        partial void OnUseCustomInstructionChanged(bool value)
        {
            AppConfig.Instance.UseCustomInstruction = value;
        }
    }
}
