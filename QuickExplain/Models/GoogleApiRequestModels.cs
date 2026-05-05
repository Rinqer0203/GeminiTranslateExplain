using System.Text.Json.Serialization;

namespace QuickExplain.Models
{
    /// <summary>
    /// Google API 向けのリクエスト構造モデル定義
    /// </summary>
    public static class GoogleApiRequestModels
    {
        public record InlineData(
            [property: JsonPropertyName("mime_type")] string MimeType,
            [property: JsonPropertyName("data")] string Data);

        public record Part(
            [property: JsonPropertyName("text")]
            [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            string? Text = null,
            [property: JsonPropertyName("inline_data")]
            [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            InlineData? InlineData = null);

        public record SystemInstruction(Part[] Parts);

        public record Content(string Role, Part[] Parts);

        public record Request(SystemInstruction System_instruction, Content[] Contents);

        public static Request CreateRequest(string instruction, ReadOnlySpan<(string role, string text)> messages)
        {
            var contents = new Content[messages.Length];
            for (int i = 0; i < messages.Length; i++)
            {
                var (role, text) = messages[i];
                contents[i] = new Content(ConvertRole(role), [new Part(Text: text)]);
            }

            return new Request(
                new SystemInstruction([new Part(Text: instruction)]),
                contents
            );
        }

        public static Request CreateImageRequest(string instruction, ReadOnlySpan<(string role, string text)> messages, string imageBase64, string mimeType)
        {
            var baseRequest = CreateRequest(instruction, messages);
            var contents = new Content[baseRequest.Contents.Length + 1];
            baseRequest.Contents.CopyTo(contents, 0);
            contents[^1] = new Content("user",
            [
                new Part(InlineData: new InlineData(mimeType, imageBase64))
            ]);

            return new Request(baseRequest.System_instruction, contents);
        }

        /// <summary>
        /// Google APIのロールに変換する
        /// </summary>
        private static string ConvertRole(string role)
        {
            if (role == "user")
                return "user";
            return "model";
        }
    }
}
