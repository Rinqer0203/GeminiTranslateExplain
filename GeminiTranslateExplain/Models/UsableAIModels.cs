namespace GeminiTranslateExplain.Models
{
    public enum AiType
    {
        gemini,
        openai
    }
    public readonly record struct AIModel(string Name, AiType Type)
    {
        public override string ToString() => Name;
    };

    public static class UsableAiModels
    {
        internal static readonly AIModel[] Models =
        [
            new AIModel("gemini-2.0-flash-lite", AiType.gemini),
            new AIModel("gemini-2.0-flash", AiType.gemini),
            new AIModel("gemini-2.5-flash-preview-05-20", AiType.gemini),
            new AIModel("gpt-4.1-nano", AiType.openai),
            new AIModel("gpt-4o-mini", AiType.openai),
            new AIModel("gpt-4.1", AiType.openai),
        ];
    }
}
