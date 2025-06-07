using CommunityToolkit.Mvvm.ComponentModel;
using GeminiTranslateExplain.Models;
using IWshRuntimeLibrary;
using System.Diagnostics;
using System.IO;

namespace GeminiTranslateExplain
{
    public partial class SettingWindowViewModel : ObservableObject
    {
        public List<WindowType> WindowTypeItems { get; }

        [ObservableProperty]
        private string _geminiApiKey = AppConfig.Instance.GeminiApiKey;

        [ObservableProperty]
        private string _openAiApiKey = AppConfig.Instance.OpenAiApiKey;

        [ObservableProperty]
        private WindowType _selectedResultWindowType;

        [ObservableProperty]
        private string _systemInstruction = AppConfig.Instance.SystemInstruction;

        [ObservableProperty]
        private string _customSystemInstruction = AppConfig.Instance.CustomSystemInstruction;

        [ObservableProperty]
        private bool _startupWithWindows = AppConfig.Instance.StartupWithWindows;

        [ObservableProperty]
        private bool _minimizeToTray = AppConfig.Instance.MinimizeToTray;

        public SettingWindowViewModel()
        {
            WindowTypeItems = Enum.GetValues(typeof(WindowType)).Cast<WindowType>().ToList();
            SelectedResultWindowType = AppConfig.Instance.SelectedResultWindowType;
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

        partial void OnSystemInstructionChanged(string value)
        {
            AppConfig.Instance.SystemInstruction = value;
        }

        partial void OnCustomSystemInstructionChanged(string value)
        {
            AppConfig.Instance.CustomSystemInstruction = value;
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
    }
}
