using System.IO;
using System.Text.Json;


namespace GeminiTranslateExplain
{
    public class AppConfig
    {
        public static AppConfig Instance { get; } = LoadConfig();

        public const string ConfigFileName = "appconfig.json";
        public string ApiKey { get; set; } = string.Empty;

        public string SystemInstruction { get; set; } =
            "以下の英文を、読みやすく正確な日本語に翻訳してください。" +
            "\n出力形式はプレーンテキスト（Markdownや記法のない普通の文章）とし、装飾やコード記法、リンク形式などは一切使用しないでください。" +
            "\n翻訳対象に単独で現れる固有名詞については、それが何であるかの簡単な説明を追記してください。";

        public string CustomSystemInstruction { get; set; } = "以下の単語について説明してください\n";

        private static AppConfig LoadConfig()
        {
            AppConfig? config = null;
            if (File.Exists(ConfigFileName))
            {
                //todo: 例外対応
                string json = File.ReadAllText(ConfigFileName);
                config = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json);
            }
            return config ?? new AppConfig();
        }

        public void SaveConfigJson()
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFileName, json);
        }
    }
}
