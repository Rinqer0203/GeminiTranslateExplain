using GeminiTranslateExplain.Models;

namespace GeminiTranslateExplain.Services.ApiClients
{
    public interface IGeminiApiClient
    {
        Task StreamGenerateContentAsync(
            string apiKey, GeminiRequestModels.Request request, string modelName, Action<string> onGetContent);
    }
}
