using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeminiTranslateExplain.Models;

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
            GeminiApiManager.Instance.RegisterProgressReceiver(this);
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
            await instance.RequestTranslation();
        }
    }
}
