using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeminiTranslateExplain.Services;
using GeminiTranslateExplain.ViewModels;

namespace GeminiTranslateExplain
{
    public partial class SimpleResultWindowViewModel : ObservableObject, IProgressTextReceiver
    {
        string IProgressTextReceiver.Text
        {
            set => TranslatedText = value;
        }

        [ObservableProperty]
        private string _translatedText = string.Empty;

        [ObservableProperty]
        private string _questionText = string.Empty;

        public SimpleResultWindowViewModel()
        {
            ApiRequestManager.Instance.RegisterProgressReceiver(this);
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
    }
}
