using GeminiTranslateExplain.Models;
using System.Text;

namespace GeminiTranslateExplain.Services.ApiClients
{
    internal class DummyGeminiApiClient : IGeminiApiClient
    {
        Task IGeminiApiClient.StreamGenerateContentAsync(
            string apiKey,
            GeminiApiRequestModels.Request body,
            string modelName,
            Action<string> onGetContent,
            Action<string> onError)
        {
            return Task.Run(async () =>
            {
                await Task.Delay(500);  // 初期ディレイ

                var sb = new StringBuilder();
                sb.AppendLine("Dummy Gemini API Response:");
                sb.AppendLine($"System Instruction: {string.Join(" ", body.System_instruction.Parts.Select(p => p.Text))}");
                sb.AppendLine("--------------------------");

                foreach (var content in body.Contents)
                {
                    sb.AppendLine($"{content.Role} : {string.Join(" ", content.Parts.Select(p => p.Text))}");
                }

                string fullText = sb.ToString();
                int chunkSize = 10;

                for (int i = 0; i < fullText.Length; i += chunkSize)
                {
                    string chunk = fullText.Substring(i, Math.Min(chunkSize, fullText.Length - i));
                    onGetContent.Invoke(chunk);
                    await Task.Delay(50);  // 擬似ストリーム間隔
                }
            });
        }

    }
}
