namespace GeminiTranslateExplain.Models
{
    public readonly record struct AIModel(string Name, int Rpm, int Tpm, int Rpd)
    {
        public override string ToString() => Name;
    };

    public static class UsableAIModels
    {
        internal static readonly AIModel[] Models =
        [
            new AIModel("gemini-2.0-flash-lite", 30, 1000000, 1500),
            new AIModel("gemini-2.0-flash", 15, 1000000, 1500),
            new AIModel("gemini-1.5-flash", 15, 250000, 500),
            new AIModel("gemini-1.5-flash-8b", 15, 250000, 500),
            new AIModel("gemini-2.5-flash-preview-05-20", 10, 250000, 500),
        ];
    }
}
