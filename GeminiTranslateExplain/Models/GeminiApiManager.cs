using System.Text;

namespace GeminiTranslateExplain
{
    public class GeminiApiManager
    {
        public static GeminiApiManager Instance { get; } = new GeminiApiManager();

        private readonly GeminiApiClient _client = new();
        private readonly StringBuilder _sb = new();
        private readonly List<(string role, string text)> _messages = new(64);

        private GeminiApiManager() { }

        private bool _isRequesting = false;

        public void AddMessage(string role, string text)
        {
            _messages.Add((role, text));
        }

        public void ClearMessages()
        {
            _messages.Clear();
        }

        public async Task<string> RequestTranslation(IProgress<string> progress)
        {
            if (_isRequesting)
            {
                System.Media.SystemSounds.Beep.Play();
                return string.Empty;
            }

            _isRequesting = true;

            var sourceProgress = new Progress<string>(text =>
            {
                _sb.Append(text);
                progress.Report(_sb.ToString());
            });

            _sb.Clear();
            var body = GeminiApiClient.CreateRequestBody(GetSystemInstruction(), _messages.AsSpan());
            var config = AppConfig.Instance;
            await _client.StreamGenerateContentAsync(config.ApiKey, body, config.SelectedGeminiModel, sourceProgress);
            var result = _sb.ToString();
            _messages.Add(("model", result));

            _isRequesting = false;
            return result;
        }

        private static string GetSystemInstruction()
        {
            if (AppConfig.Instance.UseCustomInstruction)
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
