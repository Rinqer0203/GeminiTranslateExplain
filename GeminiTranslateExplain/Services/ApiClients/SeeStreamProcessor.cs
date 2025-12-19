using System.IO;
using System.Net.Http;
using System.Text;

namespace GeminiTranslateExplain.Services.ApiClients
{
    internal static class SseStreamProcessor
    {
        public static async Task ProcessStreamAsync(
            Stream stream,
            Func<string, string?> extractContentFromJson,
            Action<string> onGetContent,
            Action<string> onError)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
                    continue;

                var jsonPart = line["data:".Length..].Trim();
                if (jsonPart == "[DONE]") break;

                try
                {
                    var content = extractContentFromJson(jsonPart);
                    if (!string.IsNullOrEmpty(content))
                        onGetContent(content);
                }
                catch (Exception ex)
                {
                    onError($"({ex.Message})");
                }
            }
        }

        public static async Task HandleErrorAsync(HttpResponseMessage response, Action<string> onError)
        {
            var error = await response.Content.ReadAsStringAsync();
            onError.Invoke($"(エラー: {response.StatusCode})\n{error}");
        }
    }
}
