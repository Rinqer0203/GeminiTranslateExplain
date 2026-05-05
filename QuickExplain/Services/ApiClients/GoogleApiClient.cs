using QuickExplain.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickExplain.Services.ApiClients
{
    internal class GoogleApiClient : IGoogleApiClient
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/";

        internal GoogleApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// ストリーミング形式でテキスト生成リクエストを送信します  
        /// </summary>
        /// <param name="apiKey">Google API の認証キー</param>
        /// <param name="request">送信するリクエストのコンテンツ</param>
        /// <param name="modelName">使用する Google モデル</param>
        /// <param name="onGetContent">生成されたテキストコンテンツが届いた際に呼び出されるコールバック</param>
        /// <param name="onError">エラーが発生した際に呼び出されるコールバック</param>
        /// <returns>非同期タスク</returns>
        async Task IGoogleApiClient.StreamGenerateContentAsync(
            string apiKey,
            GoogleApiRequestModels.Request request,
            string modelName,
            Action<string> onGetContent,
            Action<string> onError)
        {
            var path = $"{BaseUrl}models/{modelName}:streamGenerateContent?alt=sse&key={apiKey}";
            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, path);
            requestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
            requestMessage.Content = new StringContent(JsonSerializer.Serialize(request, jsonOptions), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                await SseStreamProcessor.HandleErrorAsync(response, onError);
                return;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            await Task.Run(() => SseStreamProcessor.ProcessStreamAsync(stream, ExtractContentFromJson, onGetContent, onError));
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
                throw new InvalidOperationException($"JSONパースエラー: {ex.Message}", ex);
            }
        }
    }
}
