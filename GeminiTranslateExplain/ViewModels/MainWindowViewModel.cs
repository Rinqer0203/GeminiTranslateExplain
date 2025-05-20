using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        [ObservableProperty]
        private string _questionText = string.Empty;

        [ObservableProperty]
        private bool _useCustomInstruction = false;

        private readonly GeminiApiClient _client;
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly List<(string role, string text)> _messages = new(64);

        public MainWindowViewModel()
        {
            _client = new GeminiApiClient();
        }

        [RelayCommand]
        private async Task TranslateText()
        {
            var progress = new Progress<string>(text =>
            {
                _sb.Append(text);
                TranslatedText = _sb.ToString();
                System.Diagnostics.Debug.WriteLine(TranslatedText);
            });

            _sb.Clear();
            _messages.Clear();
            _messages.Add(("user", SourceText));
            var body = GeminiApiClient.CreateRequestBody(GetSystemInstruction(), _messages.AsSpan());

            await _client.StreamGenerateContentAsync(AppConfig.Instance.ApiKey, body, SelectedGeminiModelName, progress);
            _messages.Add(("model", TranslatedText));
        }

        [RelayCommand]
        private async Task SendQuestion()
        {
            if (string.IsNullOrWhiteSpace(QuestionText))
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            _messages.Add(("user", QuestionText));
            var body = GeminiApiClient.CreateRequestBody(GetSystemInstruction(), _messages.AsSpan());
            QuestionText = string.Empty;

            var progress = new Progress<string>(text =>
            {
                _sb.Append(text);
                TranslatedText = _sb.ToString();
                System.Diagnostics.Debug.WriteLine(TranslatedText);
            });

            _sb.Clear();
            await _client.StreamGenerateContentAsync(AppConfig.Instance.ApiKey, body, SelectedGeminiModelName, progress);
            _messages.Add(("model", TranslatedText));
        }


        [RelayCommand]
        private void OpenSettingWindow()
        {
            var settingWindow = new SettingWindow();
            settingWindow.Owner = System.Windows.Application.Current.MainWindow;  // 所有者を明示
            settingWindow.ShowDialog();  // モーダル表示
        }

        private string GetSystemInstruction()
        {
            if (UseCustomInstruction)
            {
                return AppConfig.Instance.CustomSystemInstruction;
            }
            else
            {
                return AppConfig.Instance.SystemInstruction;
            }
        }
    }
}
