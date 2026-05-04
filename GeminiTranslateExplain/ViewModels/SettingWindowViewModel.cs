using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeminiTranslateExplain.Models;
using GeminiTranslateExplain.Services;
using IWshRuntimeLibrary;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace GeminiTranslateExplain
{
    public partial class SettingWindowViewModel : ObservableObject
    {
        public List<WindowType> WindowTypeItems { get; }
        public List<ThemeModeItem> ThemeModeItems { get; }
        [ObservableProperty]
        private string _geminiApiKey = AppConfig.Instance.GeminiApiKey;

        [ObservableProperty]
        private string _openAiApiKey = AppConfig.Instance.OpenAiApiKey;

        [ObservableProperty]
        private WindowType _selectedResultWindowType;

        [ObservableProperty]
        private ThemeMode _selectedThemeMode;

        [ObservableProperty]
        private bool _startupWithWindows = StartupShortcutExists();

        [ObservableProperty]
        private bool _minimizeToTray = AppConfig.Instance.MinimizeToTray;

        [ObservableProperty]
        private bool _enableDoubleCopyAction = AppConfig.Instance.EnableDoubleCopyAction;

        [ObservableProperty]
        private bool _checkUpdatesOnStartup = AppConfig.Instance.CheckUpdatesOnStartup;

        [ObservableProperty]
        private string _updateStatus = string.Empty;

        [ObservableProperty]
        private bool _isCheckingForUpdates;

        [ObservableProperty]
        private HotKeyDefinition _globalHotKey = AppConfig.Instance.GlobalHotKey;

        [ObservableProperty]
        private string _globalHotKeyDisplay = string.Empty;

        [ObservableProperty]
        private HotKeyDefinition _screenshotHotKey = AppConfig.Instance.ScreenshotHotKey;

        [ObservableProperty]
        private string _screenshotHotKeyDisplay = string.Empty;

        public SettingWindowViewModel()
        {
            WindowTypeItems = Enum.GetValues(typeof(WindowType))
                .Cast<WindowType>()
                .Where(type => type != WindowType.Clipboard)
                .ToList();
            ThemeModeItems =
            [
                new ThemeModeItem(ThemeMode.System, "システム"),
                new ThemeModeItem(ThemeMode.Light, "ライト"),
                new ThemeModeItem(ThemeMode.Dark, "ダーク")
            ];
            SelectedResultWindowType = AppConfig.Instance.SelectedResultWindowType;
            SelectedThemeMode = AppConfig.Instance.ThemeMode;
            GlobalHotKeyDisplay = FormatHotKey(GlobalHotKey);
            ScreenshotHotKeyDisplay = FormatHotKey(ScreenshotHotKey);
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

            var exists = StartupShortcutExists();
            if (_startupWithWindows != exists)
            {
                _startupWithWindows = exists;
                OnPropertyChanged(nameof(StartupWithWindows));
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

        partial void OnCheckUpdatesOnStartupChanged(bool value)
        {
            AppConfig.Instance.CheckUpdatesOnStartup = value;
        }

        partial void OnGlobalHotKeyChanged(HotKeyDefinition value)
        {
            AppConfig.Instance.UpdateGlobalHotKey(value);
            GlobalHotKeyDisplay = FormatHotKey(value);
        }

        partial void OnScreenshotHotKeyChanged(HotKeyDefinition value)
        {
            AppConfig.Instance.UpdateScreenshotHotKey(value);
            ScreenshotHotKeyDisplay = FormatHotKey(value);
        }

        public void SetGlobalHotKey(HotKeyDefinition hotKey)
        {
            GlobalHotKey = hotKey;
        }

        public void SetScreenshotHotKey(HotKeyDefinition hotKey)
        {
            ScreenshotHotKey = hotKey;
        }

        [RelayCommand]
        private async Task CheckForUpdatesAsync()
        {
            if (IsCheckingForUpdates)
                return;

            IsCheckingForUpdates = true;
            UpdateStatus = "更新を確認しています...";

            try
            {
                var result = await AppUpdateService.Instance.CheckForUpdatesAsync(
                    progress => SetUpdateStatus($"更新をダウンロードしています... {progress}%"));

                switch (result.Status)
                {
                    case UpdateCheckStatus.NotManagedByVelopack:
                        UpdateStatus = "Velopackパッケージとして起動されていないため更新できません。";
                        break;
                    case UpdateCheckStatus.UpToDate:
                        UpdateStatus = "最新版です。";
                        break;
                    case UpdateCheckStatus.UpdateReady:
                        UpdateStatus = "更新のダウンロードが完了しました。";
                        var versionText = string.IsNullOrWhiteSpace(result.Version) ? "新しいバージョン" : $"バージョン {result.Version}";
                        var response = System.Windows.MessageBox.Show(
                            $"{versionText} を適用するため、アプリを再起動しますか？",
                            "更新",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);
                        if (response == MessageBoxResult.Yes)
                        {
                            await AppUpdateService.Instance.ApplyPendingUpdateAndRestartAsync();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Log("Manual update check failed", ex);
                UpdateStatus = $"更新確認に失敗しました: {ex.Message}";
            }
            finally
            {
                IsCheckingForUpdates = false;
            }
        }

        private void SetUpdateStatus(string message)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                UpdateStatus = message;
                return;
            }

            dispatcher.Invoke(() => UpdateStatus = message);
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

        private static bool StartupShortcutExists()
        {
            string appName = "GeminiTranslateExplain";
            string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = Path.Combine(startupFolderPath, $"{appName}.lnk");
            return System.IO.File.Exists(shortcutPath);
        }

        public readonly record struct ThemeModeItem(ThemeMode Mode, string Label);
    }
}
