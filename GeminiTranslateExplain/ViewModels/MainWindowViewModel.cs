using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeminiTranslateExplain.Models;
using GeminiTranslateExplain.Services;
using GeminiTranslateExplain.ViewModels;
using System.Collections.ObjectModel;

namespace GeminiTranslateExplain
{
    internal partial class MainWindowViewModel : ObservableObject, IProgressTextReceiver
    {
        public AiModel[] AiModels { get; } = AppConfig.Instance.AIModels;
        public ObservableCollection<PromptProfile> PromptProfiles { get; } = AppConfig.Instance.PromptProfiles;

        string IProgressTextReceiver.Text
        {
            set => TranslatedText = value;
        }

        [ObservableProperty]
        private AiModel _selectedAiModel = AppConfig.Instance.SelectedAiModel;

        [ObservableProperty]
        private PromptProfile? _selectedPromptProfile = AppConfig.Instance.GetSelectedPromptProfile();

        [ObservableProperty]
        private string _sourceText = string.Empty;

        [ObservableProperty]
        private string _translatedText = string.Empty;

        [ObservableProperty]
        private string _questionText = string.Empty;

        public MainWindowViewModel()
        {
            ApiRequestManager.Instance.RegisterProgressReceiver(this);
        }

        [RelayCommand]
        private async Task TranslateText()
        {
            if (string.IsNullOrWhiteSpace(SourceText))
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            var instance = ApiRequestManager.Instance;
            instance.ClearMessages();
            instance.AddUserMessage(SourceText);
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

            var instance = ApiRequestManager.Instance;
            instance.AddUserMessage(QuestionText);
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

        [RelayCommand]
        private static void OpenPromptEditorWindow()
        {
            var promptEditorWindow = new Views.PromptEditorWindow();
            promptEditorWindow.Owner = System.Windows.Application.Current.MainWindow;
            promptEditorWindow.ShowDialog();
        }

        partial void OnSelectedAiModelChanged(AiModel value)
        {
            AppConfig.Instance.SelectedAiModel = value;
        }

        partial void OnSelectedPromptProfileChanged(PromptProfile? value)
        {
            if (value == null)
                return;

            AppConfig.Instance.SelectedPromptId = value.Id;
        }
    }
}
