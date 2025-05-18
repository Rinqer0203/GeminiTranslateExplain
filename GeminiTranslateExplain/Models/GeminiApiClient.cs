using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace GeminiTranslateExplain
{
    internal class GeminiApiClient
    {
        public record Part(string Text);
        public record SystemInstruction(Part[] Parts);
        public record Content(string Role, Part[] Parts);
        public record RequestBody(SystemInstruction System_instruction, Content[] Contents);

        private readonly HttpClient _httpClient = new();
        private readonly string _apiKey;

        internal GeminiApiClient(string apiKey)
        {
            _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
            _apiKey = apiKey;
        }

        private static RequestBody CreateRequestBody(string instruction, string prompt)
        {
            return new RequestBody(
                new SystemInstruction([new Part(instruction)]),
                [new Content("user", [new Part(prompt)])]
                );
        }

        public static RequestBody CreateRequestBody(string instruction, ReadOnlySpan<(string role, string text)> messages)
        {
            var contents = new Content[messages.Length];
            for (int i = 0; i < messages.Length; i++)
            {
                var (role, text) = messages[i];
                contents[i] = new Content(role, [new Part(text)]);
            }

            return new RequestBody(
                new SystemInstruction([new Part(instruction)]),
                contents
            );
        }

        private static string? ExtractContentFromJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
            }
            catch (Exception ex)
            {
                return $"(JSONパースエラー: {ex.Message})";
            }
        }

        internal async Task StreamGenerateContentAsync(string instruction, RequestBody body,
            GeminiModel model, IProgress<string> progress)
        {
            var path = $"models/{model.Name}:streamGenerateContent?alt=sse&key={_apiKey}";

            using var request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

            request.Content = JsonContent.Create(body);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                var errorDetails = await response.Content.ReadAsStringAsync();
                progress.Report($"(エラー: {response.StatusCode})\n{errorDetails}");
                return;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line) || !line.AsSpan().TrimStart().StartsWith("data:".AsSpan()))
                    continue;

                var jsonPart = line["data:".Length..].Trim();
                if (jsonPart == "[DONE]") break;

                var content = ExtractContentFromJson(jsonPart);
                if (content != null)
                {
                    progress.Report(content);
                }
            }
        }
    }
}
