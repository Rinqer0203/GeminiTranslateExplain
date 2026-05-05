using QuickExplain.Models;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace QuickExplain.Services
{
    internal static class ModelCatalogService
    {
        private static readonly HttpClient HttpClient = new();

        public static async Task<IReadOnlyList<AiModel>> GetModelsAsync(AiType provider, CancellationToken cancellationToken = default)
        {
            return provider switch
            {
                AiType.Google => await GetGoogleModelsAsync(cancellationToken),
                AiType.OpenAi => await GetOpenAiModelsAsync(cancellationToken),
                AiType.Ollama => await GetOllamaModelsAsync(cancellationToken),
                _ => Array.Empty<AiModel>()
            };
        }

        public static async Task<(IReadOnlyList<AiModel> Models, IReadOnlyList<string> Errors, IReadOnlyList<string> Statuses)> GetAllModelsAsync(CancellationToken cancellationToken = default)
        {
            var models = new List<AiModel>();
            var errors = new List<string>();
            var statuses = new List<string>();

            foreach (var provider in new[] { AiType.Google, AiType.OpenAi, AiType.Ollama })
            {
                try
                {
                    var providerModels = await GetModelsAsync(provider, cancellationToken);
                    models.AddRange(providerModels);
                    if (provider == AiType.Ollama)
                        statuses.Add($"Ollama: 接続済み / {providerModels.Count} 件");
                }
                catch (Exception ex)
                {
                    if (provider == AiType.Ollama)
                    {
                        statuses.Add("Ollama: 未接続または取得失敗");
                        continue;
                    }

                    errors.Add(ex.Message);
                }
            }

            return (models
                .Distinct()
                .OrderBy(model => model.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(model => model.Type)
                .ToArray(), errors, statuses);
        }

        private static async Task<IReadOnlyList<AiModel>> GetGoogleModelsAsync(CancellationToken cancellationToken)
        {
            var apiKey = AppConfig.Instance.GoogleApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Google API Key が設定されていません。");

            using var response = await HttpClient.GetAsync($"https://generativelanguage.googleapis.com/v1beta/models?key={Uri.EscapeDataString(apiKey)}&pageSize=1000", cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Google のモデル取得に失敗しました。{CreateErrorDetail(body)}");

            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("models", out var models))
                return Array.Empty<AiModel>();

            return models.EnumerateArray()
                .Where(IsGoogleTextGenerationModel)
                .Select(model => model.GetProperty("name").GetString() ?? string.Empty)
                .Select(name => name.StartsWith("models/", StringComparison.OrdinalIgnoreCase) ? name["models/".Length..] : name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(name => new AiModel(name, AiType.Google))
                .ToArray();
        }

        private static async Task<IReadOnlyList<AiModel>> GetOpenAiModelsAsync(CancellationToken cancellationToken)
        {
            var apiKey = AppConfig.Instance.OpenAiApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("OpenAI API Key が設定されていません。");

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"OpenAI のモデル取得に失敗しました。{CreateErrorDetail(body)}");

            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("data", out var data))
                return Array.Empty<AiModel>();

            return data.EnumerateArray()
                .Select(model => model.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty)
                .Where(IsOpenAiTextModel)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(name => new AiModel(name, AiType.OpenAi))
                .ToArray();
        }

        private static async Task<IReadOnlyList<AiModel>> GetOllamaModelsAsync(CancellationToken cancellationToken)
        {
            var baseUrl = AppConfig.Instance.OllamaBaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Ollama Base URL が設定されていません。");

            var endpoint = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), "api/tags");
            using var response = await HttpClient.GetAsync(endpoint, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Ollama のモデル取得に失敗しました。{CreateErrorDetail(body)}");

            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("models", out var models))
                return Array.Empty<AiModel>();

            return models.EnumerateArray()
                .Select(model => model.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(name => new AiModel(name, AiType.Ollama))
                .ToArray();
        }

        private static bool IsGoogleTextGenerationModel(JsonElement model)
        {
            if (!model.TryGetProperty("name", out var nameElement))
                return false;

            var name = nameElement.GetString() ?? string.Empty;
            if (IsExcludedGoogleModel(name))
                return false;

            if (!model.TryGetProperty("supportedGenerationMethods", out var methods))
                return false;

            return methods.EnumerateArray()
                .Any(method => string.Equals(method.GetString(), "generateContent", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsExcludedGoogleModel(string name)
        {
            return Regex.IsMatch(name, "(embedding|imagen|veo|aqa|tts|image|lyria|robotics|computer-use|deep-research|native-audio|live)", RegexOptions.IgnoreCase);
        }

        private static bool IsOpenAiTextModel(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (!name.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase))
                return false;

            return !Regex.IsMatch(name, "(audio|realtime|image|dall-e|sora|tts|whisper|transcribe|embedding|moderation|search|codex|computer-use|deep-research|instruct)", RegexOptions.IgnoreCase);
        }

        private static string CreateErrorDetail(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
                return string.Empty;

            try
            {
                using var document = JsonDocument.Parse(responseBody);
                if (document.RootElement.TryGetProperty("error", out var error))
                {
                    if (error.TryGetProperty("message", out var message))
                        return $" {message.GetString()}";
                }
            }
            catch (JsonException)
            {
            }

            return string.Empty;
        }
    }
}
