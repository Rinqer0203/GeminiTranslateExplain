﻿using GeminiTranslateExplain.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GeminiTranslateExplain.Services.ApiClients
{
    internal class OpenAiApiClient : IOpenAiApiClient
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.openai.com/v1/";

        internal OpenAiApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task StreamGenerateContentAsync(string apiKey, OpenAiApiRequestModels.Request request, Action<string> onGetContent)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}chat/completions");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            requestMessage.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                await SseStreamProcessor.HandleErrorAsync(response, onGetContent);
                return;
            }

            using var stream = await response.Content.ReadAsStreamAsync();

            await Task.Run(() => SseStreamProcessor.ProcessStreamAsync(stream, ExtractContentFromJson, onGetContent));
        }

        private static string? ExtractContentFromJson(string jsonPart)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonPart);

                var delta = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("delta");

                if (delta.TryGetProperty("content", out var contentProperty))
                    return contentProperty.GetString();

                return null;
            }
            catch (Exception ex)
            {
                return $"(パースエラー: {ex.Message})";
            }
        }
    }
}
