using GeminiTranslateExplain.Models;
using GeminiTranslateExplain.Services.ApiClients;

namespace GeminiTranslateExplain.Services.AiProviders
{
    internal sealed class OpenAiProvider : IAiProvider
    {
        private readonly IOpenAiApiClient _client;

        public OpenAiProvider(IOpenAiApiClient client)
        {
            _client = client;
        }

        public AiType Type => AiType.openai;

        public Task StreamGenerateContentAsync(
            AiProviderRequest request,
            Action<string> onGetContent,
            Action<string> onStatus,
            Action<string> onError)
        {
            var messages = request.Messages.ToArray();
            var baseRequest = OpenAiApiRequestModels.CreateRequest(request.ModelName, request.SystemInstruction, messages.AsSpan());
            if (request.ImageBytes == null)
                return _client.StreamGenerateContentAsync(AppConfig.Instance.OpenAiApiKey, baseRequest, onGetContent, onError);

            var imageBase64 = Convert.ToBase64String(request.ImageBytes);
            var messageList = new List<OpenAiApiRequestModels.Message>(baseRequest.messages.Length + 1);
            messageList.AddRange(baseRequest.messages);

            var parts = new[]
            {
                new OpenAiApiRequestModels.ContentPart(
                    "image_url",
                    image_url: new OpenAiApiRequestModels.ImageUrl($"data:{request.ImageMimeType};base64,{imageBase64}"))
            };
            messageList.Add(new OpenAiApiRequestModels.Message("user", parts));

            var imageRequest = new OpenAiApiRequestModels.Request(request.ModelName, messageList.ToArray());
            return _client.StreamGenerateContentAsync(AppConfig.Instance.OpenAiApiKey, imageRequest, onGetContent, onError);
        }
    }
}
