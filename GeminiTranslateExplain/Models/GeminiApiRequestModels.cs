namespace GeminiTranslateExplain.Models
{
    /// <summary>
    /// Gemini API 向けのリクエスト構造モデル定義
    /// </summary>
    public static class GeminiApiRequestModels
    {
        public record Part(string Text);

        public record SystemInstruction(Part[] Parts);

        public record Content(string Role, Part[] Parts);

        public record Request(SystemInstruction System_instruction, Content[] Contents);

        public static Request CreateRequest(string instruction, ReadOnlySpan<(string role, string text)> messages)
        {
            var contents = new Content[messages.Length];
            for (int i = 0; i < messages.Length; i++)
            {
                var (role, text) = messages[i];
                contents[i] = new Content(role, [new Part(text)]);
            }

            return new Request(
                new SystemInstruction([new Part(instruction)]),
                contents
            );
        }
    }
}
