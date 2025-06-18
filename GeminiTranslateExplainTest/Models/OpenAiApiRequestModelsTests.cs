using GeminiTranslateExplain.Models;

namespace GeminiTranslateExplain.Tests.Models
{
    public class OpenAiApiRequestModelsTests
    {
        [Fact]
        public void CreateRequest_ReturnsExpectedRequest()
        {
            // Arrange
            string modelName = "gpt-4";
            string instruction = "Translate the text";
            var inputMessages = new (string role, string text)[]
            {
                ("user", "こんにちは"),
                ("model", "Hello")
            };

            // Act
            var request = OpenAiApiRequestModels.CreateRequest(modelName, instruction, inputMessages);

            // Assert
            Assert.Equal("gpt-4", request.model);
            Assert.True(request.stream);
            Assert.Equal(3, request.messages.Length); // 1 system + 2 messages

            // システムメッセージの確認
            Assert.Equal("system", request.messages[0].role);
            Assert.Equal("Translate the text", request.messages[0].content);

            // ユーザーメッセージ
            Assert.Equal("user", request.messages[1].role); // ConvertRole("user") → "user"
            Assert.Equal("こんにちは", request.messages[1].content);

            // モデルメッセージ
            Assert.Equal("system", request.messages[2].role); // ConvertRole("model") → "system"
            Assert.Equal("Hello", request.messages[2].content);
        }

        [Fact]
        public void CreateRequest_WithEmptyMessages_ReturnsOnlySystemMessage()
        {
            // Arrange
            string modelName = "gpt-3.5";
            string instruction = "Only system instruction";
            var emptyMessages = Array.Empty<(string role, string text)>();

            // Act
            var request = OpenAiApiRequestModels.CreateRequest(modelName, instruction, emptyMessages);

            // Assert
            Assert.Single(request.messages);
            Assert.Equal("system", request.messages[0].role);
            Assert.Equal("Only system instruction", request.messages[0].content);
        }
    }
}
