using GeminiTranslateExplain.Models;
using GeminiTranslateExplain.Models.Extensions;
using GeminiTranslateExplain.Services.ApiClients;
using GeminiTranslateExplain.ViewModels;
using System.Text;

namespace GeminiTranslateExplain.Services
{
    /// <summary>
    /// Gemini APIリクエストを管理して、登録された<see cref="_progressReceivers"/>に進捗を通知するクラス
    /// </summary>
    public class ApiManager
    {
        public static ApiManager Instance { get; } = new ApiManager();

        private readonly IGeminiApiClient _client;
        private readonly StringBuilder _sb = new();
        private readonly List<(string role, string text)> _messages = new(64);
        private readonly List<IProgressTextReceiver> _progressReceivers = new();

        private ApiManager()
        {
            if (AppConfig.Instance.UseDummyApi)
            {
                _client = new DummyApiClient();
            }
            else
            {
                _client = new GeminiApiClient();
            }
        }

        private bool _isRequesting = false;

        public void AddMessage(string role, string text)
        {
            _messages.Add((role, text));
        }

        public void RegisterProgressReceiver(IProgressTextReceiver receiver)
        {
            if (!_progressReceivers.Contains(receiver))
            {
                _progressReceivers.Add(receiver);
            }
        }

        public void UnregisterProgressReceiver(IProgressTextReceiver receiver)
        {
            _progressReceivers.Remove(receiver);
        }

        public void ClearMessages()
        {
            _messages.Clear();
        }

        public async Task<string> RequestTranslation()
        {
            if (_isRequesting)
            {
                System.Media.SystemSounds.Beep.Play();
                return "リクエスト中です";
            }

            _isRequesting = true;

            _sb.Clear();
            var request = GeminiApiClient.CreateRequestBody(GetSystemInstruction(), _messages.AsSpan());
            var config = AppConfig.Instance;
            await _client.StreamGenerateContentAsync(config.GeminiApiKey, request, config.SelectedGeminiModel.Name, OnGetContentAction);
            var result = _sb.ToString();
            _messages.Add(("model", result));

            _isRequesting = false;
            return result;
        }

        private void OnGetContentAction(string text)
        {
            _sb.Append(text);
            var currentText = _sb.ToString();
            foreach (var holder in _progressReceivers)
            {
                holder.Text = currentText;
            }
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
