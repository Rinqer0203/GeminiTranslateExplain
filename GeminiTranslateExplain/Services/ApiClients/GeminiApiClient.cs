using GeminiTranslateExplain.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace GeminiTranslateExplain.Services.ApiClients
{
    internal class GeminiApiClient : IGeminiApiClient
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/";

        internal GeminiApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// ストリーミング形式でテキスト生成リクエストを送信します  
        /// </summary>
        /// <param name="apiKey">Gemini API の認証キー</param>
        /// <param name="request">送信するリクエストのコンテンツ</param>
        /// <param name="modelName">使用する Gemini モデル</param>
        /// <param name="onGetContent">生成されたテキストコンテンツが届いた際に呼び出されるコールバック</param>
        /// <returns>非同期タスク</returns>
        async Task IGeminiApiClient.StreamGenerateContentAsync(
            string apiKey, GeminiApiRequestModels.Request request, string modelName, Action<string> onGetContent)
        {
            var path = $"{BaseUrl}models/{modelName}:streamGenerateContent?alt=sse&key={apiKey}";

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, path);
            requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
            requestMessage.Content = JsonContent.Create(request);

            using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                await SseStreamProcessor.HandleErrorAsync(response, onGetContent);
                return;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            await Task.Run(() => SseStreamProcessor.ProcessStreamAsync(stream, ExtractContentFromJson, onGetContent));
        }

        /// <summary>
        /// jsonからテキストコンテンツを抽出する
        /// </summary>
        private static string? ExtractContentFromJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var parts = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")
                    .EnumerateArray()
                    .Select(p => p.GetProperty("text").GetString());

                return string.Join("", parts);
            }
            catch (Exception ex)
            {
                return $"(JSONパースエラー: {ex.Message})";
            }
        }
    }
}
