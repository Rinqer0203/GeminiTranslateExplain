using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GeminiTranslateExplain
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        public GeminiModel[] GeminiModelNames { get; } = UsableGeminiModels.Models;

        [ObservableProperty]
        private GeminiModel _selectedGeminiModel = AppConfig.Instance.SelectedGeminiModel;

        [ObservableProperty]
        private string _sourceText = string.Empty;

        [ObservableProperty]
        private string _translatedText = string.Empty;

        [ObservableProperty]
        private string _questionText = string.Empty;

        [ObservableProperty]
        private bool _useCustomInstruction = AppConfig.Instance.UseCustomInstruction;


        [RelayCommand]
        private async Task TranslateText()
        {
            if (string.IsNullOrWhiteSpace(SourceText))
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            var instance = GeminiApiManager.Instance;
            instance.ClearMessages();
            instance.AddMessage("user", SourceText);

            var progress = new Progress<string>(text => TranslatedText = text);
            await instance.RequestTranslation(progress);
        }

        [RelayCommand]
        private async Task SendQuestion()
        {
            if (string.IsNullOrWhiteSpace(QuestionText))
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            var instance = GeminiApiManager.Instance;
            instance.AddMessage("user", QuestionText);
            QuestionText = string.Empty;

            var progress = new Progress<string>(text => TranslatedText = text);
            await instance.RequestTranslation(progress);
        }


        [RelayCommand]
        private void OpenSettingWindow()
        {
            var settingWindow = new SettingWindow();
            settingWindow.Owner = System.Windows.Application.Current.MainWindow;  // 所有者を明示
            settingWindow.ShowDialog();  // モーダル表示
        }

        partial void OnSelectedGeminiModelChanged(GeminiModel value)
        {
            AppConfig.Instance.SelectedGeminiModel = value;
        }

        partial void OnUseCustomInstructionChanged(bool value)
        {
            AppConfig.Instance.UseCustomInstruction = value;
        }
    }
}
