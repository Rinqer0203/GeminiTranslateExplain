using GeminiTranslateExplain.Models;
using System.Text;

namespace GeminiTranslateExplain.Services
{
    internal class DummyApiClient : IGeminiApiClient
    {
        Task IGeminiApiClient.StreamGenerateContentAsync(string apiKey, GeminiApiClient.RequestBody body, GeminiModel model, IProgress<string> progress)
        {
            return Task.Run(() =>
            {
                System.Threading.Thread.Sleep(300);
                // bodyの内容を返す

                var sb = new StringBuilder();
                sb.AppendLine("Dummy API Response:");
                sb.AppendLine($"System Instruction: {string.Join(" ", body.System_instruction.Parts.Select(p => p.Text))}");
                foreach (var content in body.Contents)
                {
                    sb.AppendLine($"{content.Role}: {string.Join(" ", content.Parts.Select(p => p.Text))}");
                }
                progress.Report(sb.ToString());
            });
        }
    }
}
