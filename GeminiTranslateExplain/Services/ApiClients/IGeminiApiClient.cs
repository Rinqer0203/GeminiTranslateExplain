using GeminiTranslateExplain.Models;

namespace GeminiTranslateExplain.Services.ApiClients
{
    public record Part(string Text);
    public record SystemInstruction(Part[] Parts);
    public record Content(string Role, Part[] Parts);
    public record RequestBody(SystemInstruction System_instruction, Content[] Contents);

    public interface IGeminiApiClient
    {
        Task StreamGenerateContentAsync(string apiKey, RequestBody body, GeminiModel model, Action<string> onGetContent);
    }
}
