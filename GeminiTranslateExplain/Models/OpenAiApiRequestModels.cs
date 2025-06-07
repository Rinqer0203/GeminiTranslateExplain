namespace GeminiTranslateExplain.Models
{
    public class OpenAiApiRequestModels
    {
        public record Message(string role, string content);
        public record Request(string model, Message[] messages, bool stream = true);

        public static Request CreateRequest(string modelName, string instruction, ReadOnlySpan<(string role, string text)> messages)
        {
            int messageCount = messages.Length + 1;
            Message[] messageArray = new Message[messageCount];

            // 最初のメッセージ（システムメッセージ）を設定
            messageArray[0] = new Message("system", instruction);

            // `messages` をループして Message[] 配列に設定
            for (int i = 0; i < messages.Length; i++)
            {
                var (role, text) = messages[i];
                messageArray[i + 1] = new Message(role, text); // 1から開始
            }

            // Requestオブジェクトを作成して返す
            return new Request(modelName, messageArray);
        }
    }
}
