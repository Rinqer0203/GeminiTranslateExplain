using GeminiTranslateExplain.Models;
using GeminiTranslateExplain.Services.ApiClients;

namespace GeminiTranslateExplain.Services.AiProviders
{
    internal sealed class GeminiProvider : IAiProvider
    {
        private readonly IGeminiApiClient _client;

        public GeminiProvider(IGeminiApiClient client)
        {
            _client = client;
        }

        public AiType Type => AiType.gemini;

        public Task StreamGenerateContentAsync(
            AiProviderRequest request,
            Action<string> onGetContent,
            Action<string> onStatus,
            Action<string> onError)
        {
            var messages = request.Messages.ToArray();
            if (request.ImageBytes == null)
            {
                var body = GeminiApiRequestModels.CreateRequest(request.SystemInstruction, messages.AsSpan());
                return _client.StreamGenerateContentAsync(AppConfig.Instance.GeminiApiKey, body, request.ModelName, onGetContent, onError);
            }

            var imageBase64 = Convert.ToBase64String(request.ImageBytes);
            var imageBody = GeminiApiRequestModels.CreateImageRequest(
                request.SystemInstruction,
                messages.AsSpan(),
                imageBase64,
                request.ImageMimeType);

            return _client.StreamGenerateContentAsync(AppConfig.Instance.GeminiApiKey, imageBody, request.ModelName, onGetContent, onError);
        }
    }
}
