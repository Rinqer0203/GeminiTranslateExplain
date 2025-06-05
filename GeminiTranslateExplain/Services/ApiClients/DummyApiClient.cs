using GeminiTranslateExplain.Models;
using System.Text;

namespace GeminiTranslateExplain.Services.ApiClients
{
    internal class DummyApiClient : IGeminiApiClient
    {
        Task IGeminiApiClient.StreamGenerateContentAsync(string apiKey, RequestBody body, GeminiModel model, Action<string> onGetContent)
        {
            return Task.Run(() =>
            {
                Thread.Sleep(300);

                var sb = new StringBuilder();
                sb.AppendLine("Dummy API Response:");
                sb.AppendLine($"System Instruction: {string.Join(" ", body.System_instruction.Parts.Select(p => p.Text))}");
                foreach (var content in body.Contents)
                {
                    sb.AppendLine($"{content.Role}: {string.Join(" ", content.Parts.Select(p => p.Text))}");
                }
                onGetContent.Invoke(sb.ToString());
            });
        }
    }
}
