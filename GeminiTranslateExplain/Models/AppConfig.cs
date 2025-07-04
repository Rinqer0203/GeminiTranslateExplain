﻿using System.IO;
using System.Text.Json;


namespace GeminiTranslateExplain.Models
{
    public enum WindowType
    {
        SimpleResultWindow,
        MainWindow,
        Clipboard
    }

    public readonly record struct WindowSize(double Width, double Height);

    public class AppConfig
    {
        public static AppConfig Instance { get; } = LoadConfig();

        public const string ConfigFileName = "appconfig.json";

        // ここから先はJsonSerializerでシリアライズされるプロパティ
        public string GeminiApiKey { get; set; } = string.Empty;

        public string OpenAiApiKey { get; set; } = string.Empty;

        public WindowType SelectedResultWindowType { get; set; } = WindowType.SimpleResultWindow;

        public bool UseCustomInstruction { get; set; } = false;

        public AiModel[] AIModels { get; set; } = [
            new AiModel("gemini-2.0-flash-lite", AiType.gemini),
            new AiModel("gemini-2.0-flash", AiType.gemini),
            new AiModel("gemini-2.5-flash-preview-05-20", AiType.gemini),
            new AiModel("gpt-4.1-nano", AiType.openai),
            new AiModel("gpt-4o-mini", AiType.openai),
            new AiModel("gpt-4.1", AiType.openai),
        ];

        public AiModel SelectedAiModel { get; set; }

        public string SystemInstruction { get; set; } = "以下の英文を、読みやすく正確な日本語に翻訳してください。\r\n" +
            "あなたのすべての出力形式はプレーンテキスト（Markdownや記法のない普通の文章）とし、装飾やコード記法、" +
            "リンク形式などは一切使用しないでください。\r\n翻訳対象に単独で現れる固有名詞については、" +
            "それが何であるかの簡単な説明を追記してください。";

        public string CustomSystemInstruction { get; set; } = "以下の単語について説明してください\n";

        public WindowSize MainWindowSize { get; set; } = new WindowSize(-1, -1);

        public WindowSize SimpleResultWindowSize { get; set; } = new WindowSize(-1, -1);

        public bool StartupWithWindows { get; set; } = false;

        public bool MinimizeToTray { get; set; } = false;

        public bool DebugWindowPosition { get; set; } = false;

        public bool UseDummyApi { get; set; } = false;

        // ここまでJsonSerializerでシリアライズされるプロパティ

        private static AppConfig LoadConfig()
        {
            AppConfig? config = null;
            if (File.Exists(ConfigFileName))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFileName);
                    config = JsonSerializer.Deserialize<AppConfig>(json);
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading config file '{ConfigFileName}': {ex.Message}");
                }
                catch (Exception ex) // その他の予期せぬエラー
                {
                    System.Diagnostics.Debug.WriteLine($"An unexpected error occurred while loading config: {ex.Message}");
                }
            }
            var loadedConfig = config ?? new AppConfig();

            if (loadedConfig.AIModels.Length > 0)
                loadedConfig.SelectedAiModel = loadedConfig.AIModels[0];

            return loadedConfig;
        }

        public void SaveConfigJson()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFileName, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving config file '{ConfigFileName}': {ex.Message}");
            }
        }
    }
}
