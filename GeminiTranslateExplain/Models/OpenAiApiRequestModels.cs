﻿namespace GeminiTranslateExplain.Models
{
    public class OpenAiApiRequestModels
    {
        public record Message(string role, string content);
        public record Request(string model, Message[] messages, bool stream = true);

        public static Request CreateRequest(string modelName, string instruction, ReadOnlySpan<(string role, string text)> messages)
        {
            int messageCount = messages.Length + 1;
            Message[] messageArray = new Message[messageCount];

            // システムメッセージを設定
            messageArray[0] = new Message("system", instruction);

            for (int i = 0; i < messages.Length; i++)
            {
                var (role, text) = messages[i];
                messageArray[i + 1] = new Message(ConvertRole(role), text);
            }

            return new Request(modelName, messageArray);
        }

        /// <summary>
        /// OpenAI APIのロールに変換する
        /// </summary>
        private static string ConvertRole(string role)
        {
            if (role == "user")
                return "user";
            return "system";
        }
    }
}
