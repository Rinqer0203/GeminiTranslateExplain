using GeminiTranslateExplain.Models;

namespace GeminiTranslateExplain.Services.ApiClients
{
    public record Part(string Text);
    public record SystemInstruction(Part[] Parts);
    public record Content(string Role, Part[] Parts);
    public record GeminiRequest(SystemInstruction System_instruction, Content[] Contents);

    public interface IGeminiApiClient
    {
        Task StreamGenerateContentAsync(string apiKey, GeminiRequest body, AIModel model, Action<string> onGetContent);
    }
}
