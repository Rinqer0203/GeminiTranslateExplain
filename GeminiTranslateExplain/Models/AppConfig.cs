using System;
using System.IO;
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

        public AiModel[] AIModels { get; set; } = [
            new AiModel("gemini-2.0-flash-lite", AiType.gemini),
            new AiModel("gemini-2.0-flash", AiType.gemini),
            new AiModel("gpt-4o-mini", AiType.openai),
            new AiModel("gpt-4.1-nano", AiType.openai),
            new AiModel("gpt-4.1-mini", AiType.openai),
            new AiModel("gpt-4.1", AiType.openai),
            new AiModel("gpt-5-mini", AiType.openai),
            new AiModel("gpt-5-nano", AiType.openai),
            new AiModel("gpt-5.2", AiType.openai),
        ];

        public AiModel SelectedAiModel { get; set; }

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

    }
}
