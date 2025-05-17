using System.IO;
using System.Text.Json;


namespace GeminiTranslateExplain
{
    public class AppConfig
    {
        public const string ConfigFileName = "appconfig.json";
        public string ApiKey { get; set; } = string.Empty;

        public static AppConfig LoadConfigJson()
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
