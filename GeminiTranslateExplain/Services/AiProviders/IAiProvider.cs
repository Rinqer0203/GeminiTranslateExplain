using GeminiTranslateExplain.Models;

namespace GeminiTranslateExplain.Services.AiProviders
{
    internal interface IAiProvider
    {
        AiType Type { get; }

        Task StreamGenerateContentAsync(
            AiProviderRequest request,
            Action<string> onGetContent,
            Action<string> onStatus,
            Action<string> onError);
    }
}
