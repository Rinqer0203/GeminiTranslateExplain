using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Net.Http;
using System.Text;

namespace GeminiTranslateExplain
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        public GeminiModel[] GeminiModelNames { get; } = UsableGeminiModels.Models;

        [ObservableProperty]
        private GeminiModel _selectedGeminiModelName = UsableGeminiModels.Models[0];

        [ObservableProperty]
        private string _sourceText = string.Empty;

        [ObservableProperty]
        private string _translatedText = string.Empty;

        private string ApiKey = string.Empty;
        private readonly GeminiApiClient _client;

        internal MainWindowViewModel()
        {
            AppConfig config = AppConfig.LoadConfigJson();
            _client = new GeminiApiClient(new HttpClient(), config.ApiKey);
        }

        [RelayCommand]
        private void TranslateText()
        {
            Task.Run(async () =>
            {
                var prompt = $"次の文章を日本語に翻訳してください:\n{SourceText}";

                StringBuilder sb = new StringBuilder();
                var progress = new Progress<string>(text =>
                {
                    sb.Append(text);
                    TranslatedText = sb.ToString();
                    System.Diagnostics.Debug.WriteLine(TranslatedText);
                });

                await _client.StreamGenerateContentAsync(prompt, SelectedGeminiModelName, progress);
            });
        }

        [RelayCommand]
        private void SendQuestion()
        {
            //とりあえず音鳴らす
            System.Media.SystemSounds.Beep.Play();
        }
    }
}
