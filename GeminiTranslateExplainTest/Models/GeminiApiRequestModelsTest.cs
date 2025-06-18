using GeminiTranslateExplain.Models;

namespace GeminiTranslateExplain.Tests.Models
{
    public class GeminiApiRequestModelsTests
    {
        [Fact]
        public void CreateRequest_ReturnsExpectedRequest()
        {
            // Arrange
            string instruction = "Translate the text";
            var messages = new (string role, string text)[]
            {
                ("user", "こんにちは"),
                ("model", "Hello")
            };

            // Act
            var request = GeminiApiRequestModels.CreateRequest(instruction, messages);

            // Assert
            Assert.Equal("Translate the text", request.System_instruction.Parts[0].Text);
            Assert.Equal(2, request.Contents.Length);
            Assert.Equal("user", request.Contents[0].Role);
            Assert.Equal("こんにちは", request.Contents[0].Parts[0].Text);
            Assert.Equal("model", request.Contents[1].Role); // <- ConvertRole により変換された結果
            Assert.Equal("Hello", request.Contents[1].Parts[0].Text);
        }
    }
}
