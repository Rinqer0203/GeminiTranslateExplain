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

        private readonly GeminiApiClient _client;
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly List<(string role, string text)> _messages = new(64);

        internal MainWindowViewModel()
        {
            AppConfig config = AppConfig.LoadConfigJson();
            _client = new GeminiApiClient(config.ApiKey);
        }

        [RelayCommand]
        private async Task TestAsync()
        {
            TranslatedText = "処理開始";

            // 3秒待つだけのテスト処理（UIはフリーズしない）
            for (int i = 0; i < 100; i++)
            {
                await Task.Delay(50);
                TranslatedText = $"処理中...{i + 1}回目";
            }

            TranslatedText = "処理完了";
        }

        [RelayCommand]
        private void TranslateText()
        {
            Task.Run(async () =>
            {

                string instruction = $"以下の英文を、読みやすく正確な日本語に翻訳してください。" +
                $"\n出力形式はプレーンテキスト（Markdownや記法のない普通の文章）とし、装飾やコード記法、リンク形式などは一切使用しないでください。" +
                $"\n翻訳対象に単独で現れる固有名詞については、それが何であるかの簡単な説明を追記してください。";


                var progress = new Progress<string>(text =>
                {
                    _sb.Append(text);
                    TranslatedText = _sb.ToString();
                    System.Diagnostics.Debug.WriteLine(TranslatedText);
                    //System.Diagnostics.Debug.WriteLine("更新");
                });

                _sb.Clear();
                _messages.Clear();
                _messages.Add(("user", SourceText));
                var body = GeminiApiClient.CreateRequestBody(instruction, _messages.AsSpan());

                await _client.StreamGenerateContentAsync(instruction, body, SelectedGeminiModelName, progress);
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
