namespace GeminiTranslateExplain
{
    internal readonly record struct GeminiModel(string Name, int Rpm, int Tpm, int Rpd)
    {
        public override string ToString() => Name;
    };

    internal static class UsableGeminiModels
    {
        internal static readonly GeminiModel[] Models =
        [
            new GeminiModel("gemini-2.0-flash-lite", 30, 1000000, 1500),
            new GeminiModel("gemini-2.0-flash", 15, 1000000, 1500),
            new GeminiModel("gemini-1.5-flash", 15, 250000, 500),
            new GeminiModel("gemini-1.5-flash-8b", 15, 250000, 500),
        ];
    }
}
