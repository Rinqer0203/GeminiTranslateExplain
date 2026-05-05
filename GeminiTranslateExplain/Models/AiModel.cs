namespace GeminiTranslateExplain.Models
{
    public enum AiType
    {
        gemini,
        openai,
        ollama
    }
    public readonly record struct AiModel(string Name, AiType Type)
    {
        public override string ToString() => Name;
    };
}
