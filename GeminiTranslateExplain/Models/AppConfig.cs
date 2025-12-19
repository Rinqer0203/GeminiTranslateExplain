using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace GeminiTranslateExplain.Models
{
    public enum WindowType
    {
        SimpleResultWindow,
        MainWindow,
        Clipboard
    }

    public enum ThemeMode
    {
        System,
        Light,
        Dark
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
            new AiModel("gpt-4o-mini", AiType.openai),
            new AiModel("gpt-4.1-nano", AiType.openai),
            new AiModel("gpt-4.1-mini", AiType.openai),
            new AiModel("gpt-5-mini", AiType.openai),
            new AiModel("gpt-5-nano", AiType.openai),
            new AiModel("gpt-5.2", AiType.openai),
        ];

        public AiModel SelectedAiModel { get; set; }

        public string SystemInstruction { get; set; } = "入力として与えられる英語は、英文または単語・短い語句のいずれかです。\r\n" +
            "入力内容の種類を判断し、それに応じた日本語出力を行ってください。\r\n\r\n" +
            "・入力が英文または文脈を持つ文章の場合：\r\n" +
            "  日本語として自然で意味が正確に対応する文章に翻訳し、\r\n" +
            "  理解に必要な場合のみ、語や表現の補足説明を全角丸括弧で簡潔に付与してください。\r\n\r\n" +
            "・入力が単語、略語、または短い語句のみの場合：\r\n" +
            "  翻訳文は作らず、日本語での意味や用法を簡潔に説明してください。\r\n" +
            "  必要に応じて、どのような文脈で使われる語かも補足してください。\r\n\r\n" +
            "いずれの場合も、出力は常にプレーンテキストのみとし、\r\n" +
            "Markdown、装飾、箇条書き、コード記法、リンク形式などは一切使用しないでください。\r\n\r\n" +
            "説明や補足は、辞書の丸写しではなく、一般的な日本語話者が理解できる表現を用い、\r\n" +
            "不要に長くならないよう最小限に留めてください。";

        public string CustomSystemInstruction { get; set; } = "以下の単語について説明してください\n";

        public ObservableCollection<PromptProfile> PromptProfiles { get; set; } = new();

        public string SelectedPromptId { get; set; } = string.Empty;

        public ThemeMode ThemeMode { get; set; } = ThemeMode.System;

        public WindowSize MainWindowSize { get; set; } = new WindowSize(-1, -1);

        public WindowSize SimpleResultWindowSize { get; set; } = new WindowSize(-1, -1);

        public bool MinimizeToTray { get; set; } = false;

        public bool DebugWindowPosition { get; set; } = false;

        public bool UseDummyApi { get; set; } = false;

        public bool DebugClipboardAction { get; set; } = false;

        public HotKeyDefinition GlobalHotKey { get; set; } = HotKeyDefinition.Default;

        public HotKeyDefinition ScreenshotHotKey { get; set; } = HotKeyDefinition.ScreenshotDefault;

        public bool EnableDoubleCopyAction { get; set; } = true;

        public bool ScreenshotStealthMode { get; set; } = false;

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
            {
                var selected = loadedConfig.SelectedAiModel;
                if (!string.IsNullOrWhiteSpace(selected.Name))
                {
                    var found = false;
                    foreach (var model in loadedConfig.AIModels)
                    {
                        if (model.Name == selected.Name && model.Type == selected.Type)
                        {
                            loadedConfig.SelectedAiModel = model;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        loadedConfig.SelectedAiModel = loadedConfig.AIModels[0];
                }
                else
                {
                    loadedConfig.SelectedAiModel = loadedConfig.AIModels[0];
                }
            }

            InitializePromptProfiles(loadedConfig);

            if (loadedConfig.GlobalHotKey.Key == Key.None || loadedConfig.GlobalHotKey.Modifiers == ModifierKeys.None)
            {
                loadedConfig.GlobalHotKey = HotKeyDefinition.Default;
            }

            if (loadedConfig.ScreenshotHotKey.Key == Key.None || loadedConfig.ScreenshotHotKey.Modifiers == ModifierKeys.None)
            {
                loadedConfig.ScreenshotHotKey = HotKeyDefinition.ScreenshotDefault;
            }

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

        public event Action<HotKeyDefinition>? GlobalHotKeyChanged;
        public event Action<HotKeyDefinition>? ScreenshotHotKeyChanged;
        public event Action<ThemeMode>? ThemeModeChanged;

        public void UpdateGlobalHotKey(HotKeyDefinition hotKey)
        {
            if (GlobalHotKey.Equals(hotKey))
                return;

            GlobalHotKey = hotKey;
            GlobalHotKeyChanged?.Invoke(hotKey);
        }

        public void UpdateScreenshotHotKey(HotKeyDefinition hotKey)
        {
            if (ScreenshotHotKey.Equals(hotKey))
                return;

            ScreenshotHotKey = hotKey;
            ScreenshotHotKeyChanged?.Invoke(hotKey);
        }

        public void UpdateThemeMode(ThemeMode themeMode)
        {
            if (ThemeMode == themeMode)
                return;

            ThemeMode = themeMode;
            ThemeModeChanged?.Invoke(themeMode);
        }

        public PromptProfile GetSelectedPromptProfile()
        {
            if (PromptProfiles.Count == 0)
            {
                var fallback = new PromptProfile
                {
                    Name = "デフォルト",
                    Instruction = SystemInstruction
                };
                PromptProfiles.Add(fallback);
                SelectedPromptId = fallback.Id;
                return fallback;
            }

            var selected = PromptProfiles.FirstOrDefault(p => p.Id == SelectedPromptId);
            if (selected != null)
                return selected;

            SelectedPromptId = PromptProfiles[0].Id;
            return PromptProfiles[0];
        }

        private static void InitializePromptProfiles(AppConfig config)
        {
            if (config.PromptProfiles != null && config.PromptProfiles.Count > 0)
            {
                foreach (var profile in config.PromptProfiles)
                {
                    if (string.IsNullOrWhiteSpace(profile.Id))
                        profile.Id = Guid.NewGuid().ToString("N");
                    if (string.IsNullOrWhiteSpace(profile.Name))
                        profile.Name = "プロンプト";
                }
            }
            else
            {
                config.PromptProfiles = new ObservableCollection<PromptProfile>
                {
                    new PromptProfile
                    {
                        Name = "デフォルト",
                        Instruction = config.SystemInstruction
                    },
                    new PromptProfile
                    {
                        Name = "カスタム",
                        Instruction = config.CustomSystemInstruction
                    }
                };
            }

            if (string.IsNullOrWhiteSpace(config.SelectedPromptId))
            {
                if (config.UseCustomInstruction && config.PromptProfiles.Count > 1)
                {
                    config.SelectedPromptId = config.PromptProfiles[1].Id;
                }
                else
                {
                    config.SelectedPromptId = config.PromptProfiles[0].Id;
                }
            }
            else if (config.PromptProfiles.Any(p => p.Id == config.SelectedPromptId) == false)
            {
                config.SelectedPromptId = config.PromptProfiles[0].Id;
            }
        }
    }
}
