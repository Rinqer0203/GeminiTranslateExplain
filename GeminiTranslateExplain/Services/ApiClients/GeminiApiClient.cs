using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace GeminiTranslateExplain.Services.ApiClients
{
    internal class GeminiApiClient : IGeminiApiClient
    {
        private readonly HttpClient _httpClient = new();

        internal GeminiApiClient()
        {
            _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
        }

        public static GeminiRequest CreateRequestBody(string instruction, ReadOnlySpan<(string role, string text)> messages)
        {
            var contents = new Content[messages.Length];
            for (int i = 0; i < messages.Length; i++)
            {
                var (role, text) = messages[i];
                contents[i] = new Content(role, [new Part(text)]);
            }

            return new GeminiRequest(
                new SystemInstruction([new Part(instruction)]),
                contents
            );
        }

        /// <summary>
        /// ストリーミング形式でテキスト生成リクエストを送信します  
        /// </summary>
        /// <param name="apiKey">Gemini API の認証キー</param>
        /// <param name="geminiRequest">送信するリクエストのコンテンツ</param>
        /// <param name="modelName">使用する Gemini モデル</param>
        /// <param name="onGetContent">生成されたテキストコンテンツが届いた際に呼び出されるコールバック</param>
        /// <returns>非同期タスク</returns>
        async Task IGeminiApiClient.StreamGenerateContentAsync(string apiKey, GeminiRequest geminiRequest, string modelName, Action<string> onGetContent)
        {
            var path = $"models/{modelName}:streamGenerateContent?alt=sse&key={apiKey}";

            using var request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
            request.Content = JsonContent.Create(geminiRequest);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                await SseStreamProcessor.HandleErrorAsync(response, onGetContent);
                return;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            await SseStreamProcessor.ProcessStreamAsync(stream, ExtractContentFromJson, onGetContent);
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
