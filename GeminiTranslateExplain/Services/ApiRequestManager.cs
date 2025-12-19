using GeminiTranslateExplain.Models;
using GeminiTranslateExplain.Models.Extensions;
using GeminiTranslateExplain.Services.ApiClients;
using GeminiTranslateExplain.ViewModels;
using System;
using System.Net.Http;
using System.Text;

namespace GeminiTranslateExplain.Services
{
    /// <summary>
    /// Gemini APIリクエストを管理して、<see cref="RegisterProgressReceiver"/>で登録されたReceiverに進捗を通知するクラス
    /// </summary>
    public class ApiRequestManager
    {
        public static ApiRequestManager Instance { get; } = new ApiRequestManager();

        private readonly IGeminiApiClient _geminiApiClient;
        private readonly IOpenAiApiClient _openAiApiClient;
        private readonly StringBuilder _sb = new();
        private readonly List<(string role, string text)> _messages = new(64);
        private readonly List<IProgressTextReceiver> _progressReceivers = new();

        private ApiRequestManager()
        {
            var httpClient = new HttpClient();

            if (AppConfig.Instance.UseDummyApi)
            {
                _geminiApiClient = new DummyGeminiApiClient();
                _openAiApiClient = new DummyOpenAiApiClient();
            }
            else
            {
                _geminiApiClient = new GeminiApiClient(httpClient);
                _openAiApiClient = new OpenAiApiClient(httpClient);
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

            _sb.Clear();
            var config = AppConfig.Instance;
            if (config.SelectedAiModel.Type == AiType.openai)
            {
                var request = OpenAiApiRequestModels.CreateRequest(config.SelectedAiModel.Name, GetSystemInstruction(), _messages.AsSpan());
                await _openAiApiClient.StreamGenerateContentAsync(config.OpenAiApiKey, request, OnGetContentAction);
            }
            else if (config.SelectedAiModel.Type == AiType.gemini)
            {
                var request = GeminiApiRequestModels.CreateRequest(GetSystemInstruction(), _messages.AsSpan());
                await _geminiApiClient.StreamGenerateContentAsync(config.GeminiApiKey, request, config.SelectedAiModel.Name, OnGetContentAction);
            }
            else
            {
                _isRequesting = false;
                return "サポートされていないAIモデルです";
            }
            var result = _sb.ToString();

            // システムの返答のロールをmodelにしているが、
            // それぞれのCreateRequestで決められたAPIロールに変換されるので問題ない
            _messages.Add(("model", result));

            _isRequesting = false;
            return result;
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
            _sb.Clear();

            var config = AppConfig.Instance;
            if (config.SelectedAiModel.Type != AiType.openai)
            {
                _isRequesting = false;
                return "画像入力はOpenAIモデルのみ対応しています";
            }

            var baseRequest = OpenAiApiRequestModels.CreateRequest(config.SelectedAiModel.Name, GetSystemInstruction(), _messages.AsSpan());
            var messageList = new List<OpenAiApiRequestModels.Message>(baseRequest.messages.Length + 1);
            messageList.AddRange(baseRequest.messages);

            var imageBase64 = Convert.ToBase64String(imageBytes);
            var parts = new[]
            {
                new OpenAiApiRequestModels.ContentPart("image_url", image_url: new OpenAiApiRequestModels.ImageUrl($"data:image/png;base64,{imageBase64}"))
            };
            messageList.Add(new OpenAiApiRequestModels.Message("user", parts));

            var request = new OpenAiApiRequestModels.Request(config.SelectedAiModel.Name, messageList.ToArray());
            await _openAiApiClient.StreamGenerateContentAsync(config.OpenAiApiKey, request, OnGetContentAction);

            var result = _sb.ToString();
            _messages.Add(("user", "[画像]"));
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
            return AppConfig.Instance.GetSelectedPromptProfile().Instruction;
        }
    }
}
