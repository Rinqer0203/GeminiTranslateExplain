using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace GeminiTranslateExplain
{
    internal class GeminiApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        internal GeminiApiClient(HttpClient httpClient, string apiKey)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
        }

        internal async Task<string> GenerateContentAsync(string prompt, GeminiModel model)
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };
            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model.Name}:generateContent?key={_apiKey}";

            var response = await _httpClient.PostAsJsonAsync(apiUrl, requestBody);
            if (response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                var json = await JsonDocument.ParseAsync(stream);
                var replyText = json.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
                return replyText ?? "(空のレスポンス)";
            }
            return "(エラー: " + response.StatusCode + ")";
        }


        internal async Task StreamGenerateContentAsync(string prompt, GeminiModel model, IProgress<string> progress)
        {
            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model.Name}:streamGenerateContent?alt=sse&key={_apiKey}";

            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
            request.Content = JsonContent.Create(new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            });

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                progress.Report($"(エラー: {response.StatusCode})");
                return;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
                    continue;

                var jsonPart = line.Substring("data:".Length).Trim();
                if (jsonPart == "[DONE]") break;

                try
                {
                    using var doc = JsonDocument.Parse(jsonPart);
                    var content = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    if (!string.IsNullOrEmpty(content))
                    {
                        progress.Report(content);
                    }
                }
                catch (Exception ex)
                {
                    progress.Report($"(JSONパースエラー: {ex.Message})");
                }
            }
        }

    }
}
