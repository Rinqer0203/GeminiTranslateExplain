namespace QuickExplain.Models
{
    public enum AiType
    {
        Google,
        OpenAi,
        Ollama
    }
    public readonly record struct AiModel(string Name, AiType Type)
    {
        public string ProviderName => Type switch
        {
            AiType.Google => "Google",
            AiType.OpenAi => "OpenAI",
            AiType.Ollama => "Ollama",
            _ => Type.ToString()
        };

        public override string ToString() => Name;
    };
}
