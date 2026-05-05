using GeminiTranslateExplain.Models;
using GeminiTranslateExplain.Models.Extensions;
using GeminiTranslateExplain.Services.AiProviders;
using GeminiTranslateExplain.Services.ApiClients;
using GeminiTranslateExplain.ViewModels;
using System;
using System.Net.Http;
using System.Text;
using System.Windows.Threading;

namespace GeminiTranslateExplain.Services
{
    /// <summary>
    /// Gemini APIリクエストを管理して、<see cref="RegisterProgressReceiver"/>で登録されたReceiverに進捗を通知するクラス
    /// </summary>
    public class ApiRequestManager
    {
        public static ApiRequestManager Instance { get; } = new ApiRequestManager();

        private readonly AiProviderRegistry _providerRegistry;
        private readonly StringBuilder _sb = new();
        private readonly List<(string role, string text)> _messages = new(64);
        private readonly List<IProgressTextReceiver> _progressReceivers = new();
        public event Action? RequestStarted;
        public event Action<bool>? RequestCompleted;
        private bool _requestHadError;

        private ApiRequestManager()
        {
            var httpClient = new HttpClient();

            if (AppConfig.Instance.UseDummyApi)
            {
                _providerRegistry = new AiProviderRegistry(
                [
                    new GeminiProvider(new DummyGeminiApiClient()),
                    new OpenAiProvider(new DummyOpenAiApiClient()),
                    new DummyOllamaProvider()
                ]);
            }
            else
            {
                _providerRegistry = new AiProviderRegistry(
                [
                    new GeminiProvider(new GeminiApiClient(httpClient)),
                    new OpenAiProvider(new OpenAiApiClient(httpClient)),
                    new OllamaProvider(httpClient)
                ]);
            }

        }

        private bool _isRequesting = false;

        public void AddUserMessage(string text)
        {
            _messages.Add(("user", text));
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
            RequestStarted?.Invoke();

            var success = false;
            try
            {
                _sb.Clear();
                _requestHadError = false;
                var config = AppConfig.Instance;
                if (string.IsNullOrWhiteSpace(config.SelectedAiModel.Name))
                {
                    _requestHadError = true;
                    return "使用するAIモデルが設定されていません。モデル編集からモデルを追加してください。";
                }

                if (!_providerRegistry.TryGetProvider(config.SelectedAiModel.Type, out var provider))
                {
                    _requestHadError = true;
                    return "サポートされていないAIモデルです";
                }

                var request = new AiProviderRequest(
                    config.SelectedAiModel.Name,
                    GetSystemInstruction(),
                    _messages.ToArray());
                OnStatusAction("応答を待っています...");
                await provider.StreamGenerateContentAsync(request, OnGetContentAction, OnStatusAction, OnErrorAction);

                var result = _sb.ToString();

                // システムの返答のロールをmodelにしているが、
                // それぞれのCreateRequestで決められたAPIロールに変換されるので問題ない
                _messages.Add(("model", result));

                success = !_requestHadError;
                return result;
            }
            finally
            {
                _isRequesting = false;
                RequestCompleted?.Invoke(success);
            }
        }

        public async Task<string> RequestImageQuestion(byte[] imageBytes)
        {
            if (_isRequesting)
            {
                System.Media.SystemSounds.Beep.Play();
                return "リクエスト中です";
            }

            if (imageBytes == null || imageBytes.Length == 0)
                return "画像データが空です";

            _isRequesting = true;
            RequestStarted?.Invoke();

            var success = false;
            try
            {
                _sb.Clear();
                _requestHadError = false;

                var config = AppConfig.Instance;
                ImageLogService.SaveSentImage(imageBytes, config.SelectedAiModel.Type.ToString());
                if (string.IsNullOrWhiteSpace(config.SelectedAiModel.Name))
                {
                    _requestHadError = true;
                    return "使用するAIモデルが設定されていません。モデル編集からモデルを追加してください。";
                }

                if (!_providerRegistry.TryGetProvider(config.SelectedAiModel.Type, out var provider))
                {
                    _requestHadError = true;
                    return "サポートされていないAIモデルです";
                }

                var request = new AiProviderRequest(
                    config.SelectedAiModel.Name,
                    GetSystemInstruction(),
                    _messages.ToArray(),
                    imageBytes);
                OnStatusAction("応答を待っています...");
                await provider.StreamGenerateContentAsync(request, OnGetContentAction, OnStatusAction, OnErrorAction);

                var result = _sb.ToString();
                _messages.Add(("user", "[画像]"));
                _messages.Add(("model", result));

                success = !_requestHadError;
                return result;
            }
            finally
            {
                _isRequesting = false;
                RequestCompleted?.Invoke(success);
            }
        }

        private void OnGetContentAction(string text)
        {
            _sb.Append(text);
            var currentText = _sb.ToString();
            UpdateProgressReceivers(currentText);
        }

        private void OnStatusAction(string text)
        {
            var currentText = _sb.Length == 0
                ? text
                : $"{_sb}{Environment.NewLine}{text}";
            UpdateProgressReceivers(currentText);
        }

        private void OnErrorAction(string text)
        {
            _requestHadError = true;
            _sb.Append(text);
            var currentText = _sb.ToString();
            UpdateProgressReceivers(currentText);
        }

        private void UpdateProgressReceivers(string text)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                SetProgressReceiverText(text);
                return;
            }

            dispatcher.BeginInvoke(() => SetProgressReceiverText(text), DispatcherPriority.Background);
        }

        private void SetProgressReceiverText(string text)
        {
            foreach (var holder in _progressReceivers.ToArray())
            {
                holder.Text = text;
            }
        }

        private static string GetSystemInstruction()
        {
            return AppConfig.Instance.GetSelectedPromptProfile().Instruction;
        }
    }
}
