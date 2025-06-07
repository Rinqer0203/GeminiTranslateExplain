namespace GeminiTranslateExplain.Models
{
    public enum AiType
    {
        gemini,
        openai
    }
    public readonly record struct AiModel(string Name, AiType Type)
    {
        public override string ToString() => Name;
    };
}
