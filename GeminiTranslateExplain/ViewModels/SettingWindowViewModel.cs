using CommunityToolkit.Mvvm.ComponentModel;
using GeminiTranslateExplain.Models;
using IWshRuntimeLibrary;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace GeminiTranslateExplain
{
    public partial class SettingWindowViewModel : ObservableObject
    {
        public List<WindowType> WindowTypeItems { get; }
        public List<ThemeMode> ThemeModeItems { get; }
        [ObservableProperty]
        private string _geminiApiKey = AppConfig.Instance.GeminiApiKey;

        [ObservableProperty]
        private string _openAiApiKey = AppConfig.Instance.OpenAiApiKey;

        [ObservableProperty]
        private WindowType _selectedResultWindowType;

        [ObservableProperty]
        private ThemeMode _selectedThemeMode;

        [ObservableProperty]
        private bool _startupWithWindows = AppConfig.Instance.StartupWithWindows;

        [ObservableProperty]
        private bool _minimizeToTray = AppConfig.Instance.MinimizeToTray;

        [ObservableProperty]
        private bool _enableDoubleCopyAction = AppConfig.Instance.EnableDoubleCopyAction;

        [ObservableProperty]
        private HotKeyDefinition _globalHotKey = AppConfig.Instance.GlobalHotKey;

        [ObservableProperty]
        private string _globalHotKeyDisplay = string.Empty;

        public SettingWindowViewModel()
        {
            WindowTypeItems = Enum.GetValues(typeof(WindowType)).Cast<WindowType>().ToList();
            ThemeModeItems = Enum.GetValues(typeof(ThemeMode)).Cast<ThemeMode>().ToList();
            SelectedResultWindowType = AppConfig.Instance.SelectedResultWindowType;
            SelectedThemeMode = AppConfig.Instance.ThemeMode;
            GlobalHotKeyDisplay = FormatHotKey(GlobalHotKey);
        }

        public void OnClosed()
        {
            AppConfig.Instance.SaveConfigJson();
        }

        partial void OnGeminiApiKeyChanged(string value)
        {
            AppConfig.Instance.GeminiApiKey = value;
        }

        partial void OnOpenAiApiKeyChanged(string value)
        {
            AppConfig.Instance.OpenAiApiKey = value;
        }

        partial void OnSelectedResultWindowTypeChanged(WindowType value)
        {
            AppConfig.Instance.SelectedResultWindowType = value;
        }

        partial void OnSelectedThemeModeChanged(ThemeMode value)
        {
            AppConfig.Instance.UpdateThemeMode(value);
        }

        partial void OnStartupWithWindowsChanged(bool value)
        {
            AppConfig.Instance.StartupWithWindows = value;

            string appName = "GeminiTranslateExplain";
            string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = Path.Combine(startupFolderPath, $"{appName}.lnk");

            // 実行ファイル(.exe)のフルパスを取得
            var mainModule = Process.GetCurrentProcess().MainModule
                 ?? throw new InvalidOperationException("現在のプロセスのメインモジュールが取得できませんでした。");
            string exePath = mainModule.FileName;

            if (value)
            {
                // スタートアップにショートカットを作成する
                var shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                shortcut.Description = "GeminiTranslateExplain 自動起動";
                shortcut.Save();
            }
            else
            {
                // スタートアップからショートカットを削除する
                if (System.IO.File.Exists(shortcutPath))
                {
                    System.IO.File.Delete(shortcutPath);
                }
            }
        }

        partial void OnMinimizeToTrayChanged(bool value)
        {
            AppConfig.Instance.MinimizeToTray = value;
        }

        partial void OnEnableDoubleCopyActionChanged(bool value)
        {
            AppConfig.Instance.EnableDoubleCopyAction = value;
        }

        partial void OnGlobalHotKeyChanged(HotKeyDefinition value)
        {
            AppConfig.Instance.UpdateGlobalHotKey(value);
            GlobalHotKeyDisplay = FormatHotKey(value);
        }

        public void SetGlobalHotKey(HotKeyDefinition hotKey)
        {
            GlobalHotKey = hotKey;
        }

        private static string FormatHotKey(HotKeyDefinition hotKey)
        {
            var parts = new List<string>();
            if ((hotKey.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) parts.Add("Ctrl");
            if ((hotKey.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) parts.Add("Shift");
            if ((hotKey.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) parts.Add("Alt");
            if ((hotKey.Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows) parts.Add("Win");
            parts.Add(hotKey.Key.ToString());
            return string.Join(" + ", parts);
        }
    }
}
