using GeminiTranslateExplain.Models;
using GeminiTranslateExplain.Models.Extensions;
using System.Text;

namespace GeminiTranslateExplain.Services
{
    public interface IProgressTextReceiver
    {
        string Text { set; }
    }

    public class GeminiApiManager
    {
        public static GeminiApiManager Instance { get; } = new GeminiApiManager();

        private readonly GeminiApiClient _client = new();
        private readonly StringBuilder _sb = new();
        private readonly List<(string role, string text)> _messages = new(64);
        private readonly List<IProgressTextReceiver> _progressReceivers = new();

        private GeminiApiManager() { }

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
                return string.Empty;
            }

            _isRequesting = true;

            var sourceProgress = new Progress<string>(text =>
            {
                _sb.Append(text);
                var currentText = _sb.ToString();
                foreach (var holder in _progressReceivers)
                {
                    holder.Text = currentText;
                }
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
