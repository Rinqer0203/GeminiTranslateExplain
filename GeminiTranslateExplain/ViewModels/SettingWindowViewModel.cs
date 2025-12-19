using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeminiTranslateExplain.Models;
using IWshRuntimeLibrary;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace GeminiTranslateExplain
{
    public partial class SettingWindowViewModel : ObservableObject
    {
        public List<WindowType> WindowTypeItems { get; }
        public ObservableCollection<PromptProfile> PromptProfiles { get; }

        [ObservableProperty]
        private string _geminiApiKey = AppConfig.Instance.GeminiApiKey;

        [ObservableProperty]
        private string _openAiApiKey = AppConfig.Instance.OpenAiApiKey;

        [ObservableProperty]
        private WindowType _selectedResultWindowType;

        [ObservableProperty]
        private bool _startupWithWindows = AppConfig.Instance.StartupWithWindows;

        [ObservableProperty]
        private bool _minimizeToTray = AppConfig.Instance.MinimizeToTray;

        [ObservableProperty]
        private HotKeyDefinition _globalHotKey = AppConfig.Instance.GlobalHotKey;

        [ObservableProperty]
        private string _globalHotKeyDisplay = string.Empty;

        [ObservableProperty]
        private PromptProfile? _selectedPromptProfile;

        [ObservableProperty]
        private string _promptName = string.Empty;

        [ObservableProperty]
        private string _promptInstruction = string.Empty;

        public SettingWindowViewModel()
        {
            WindowTypeItems = Enum.GetValues(typeof(WindowType)).Cast<WindowType>().ToList();
            SelectedResultWindowType = AppConfig.Instance.SelectedResultWindowType;
            GlobalHotKeyDisplay = FormatHotKey(GlobalHotKey);

            PromptProfiles = AppConfig.Instance.PromptProfiles;
            SelectedPromptProfile = AppConfig.Instance.GetSelectedPromptProfile();
            if (SelectedPromptProfile != null)
            {
                PromptName = SelectedPromptProfile.Name;
                PromptInstruction = SelectedPromptProfile.Instruction;
            }
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

        partial void OnGlobalHotKeyChanged(HotKeyDefinition value)
        {
            AppConfig.Instance.UpdateGlobalHotKey(value);
            GlobalHotKeyDisplay = FormatHotKey(value);
        }

        partial void OnSelectedPromptProfileChanged(PromptProfile? value)
        {
            if (value == null)
                return;

            AppConfig.Instance.SelectedPromptId = value.Id;
            PromptName = value.Name;
            PromptInstruction = value.Instruction;
        }

        partial void OnPromptNameChanged(string value)
        {
            if (SelectedPromptProfile == null)
                return;

            SelectedPromptProfile.Name = value;
        }

        partial void OnPromptInstructionChanged(string value)
        {
            if (SelectedPromptProfile == null)
                return;

            SelectedPromptProfile.Instruction = value;
        }

        public void SetGlobalHotKey(HotKeyDefinition hotKey)
        {
            GlobalHotKey = hotKey;
        }

        [RelayCommand]
        private void AddPromptProfile()
        {
            var profile = new PromptProfile
            {
                Name = "新しいプロンプト",
                Instruction = string.Empty
            };
            PromptProfiles.Add(profile);
            SelectedPromptProfile = profile;
        }

        [RelayCommand]
        private void RemovePromptProfile()
        {
            if (SelectedPromptProfile == null || PromptProfiles.Count <= 1)
                return;

            var index = PromptProfiles.IndexOf(SelectedPromptProfile);
            PromptProfiles.Remove(SelectedPromptProfile);

            if (PromptProfiles.Count == 0)
                return;

            if (index >= PromptProfiles.Count)
                index = PromptProfiles.Count - 1;

            SelectedPromptProfile = PromptProfiles[index];
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
