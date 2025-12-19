using GeminiTranslateExplain.Models;

namespace GeminiTranslateExplain.Services.ApiClients
{
    public interface IOpenAiApiClient
    {
        Task StreamGenerateContentAsync(
            string apiKey,
            OpenAiApiRequestModels.Request request,
            Action<string> onGetContent,
            Action<string> onError);
    }
}
