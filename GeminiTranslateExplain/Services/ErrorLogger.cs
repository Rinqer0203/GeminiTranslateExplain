using System.IO;
using System.Text;

namespace GeminiTranslateExplain.Services
{
    public static class ErrorLogger
    {
        private static readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");

        public static void Log(string type, Exception? ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {type}");
            sb.AppendLine(ex?.ToString() ?? "例外情報が null です。");
            sb.AppendLine(new string('-', 80));

            try
            {
                File.AppendAllText(logFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }
    }
}
