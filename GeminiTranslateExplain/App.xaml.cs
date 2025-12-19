using GeminiTranslateExplain.Models;
using GeminiTranslateExplain.Services;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GeminiTranslateExplain
{
    public partial class App : System.Windows.Application
    {
        private static Mutex? _mutex;

        private ClipboardActionHandler? _clipboardActionHandler;
        private TrayManager? _trayManager;
        private ForegroundWatcher? _foregroundWatcher;
        private GlobalHotKeyManager? _globalHotKeyManager;
        private BundledTheme? _bundledTheme;
        private int? _screenshotHotKeyId;
        private bool _isScreenshotCapturing;
        private Views.ScreenshotOverlayWindow? _activeScreenshotOverlay;
        private DateTime _ignoreSimpleResultWindowHideUntil = DateTime.MinValue;


        protected override void OnStartup(StartupEventArgs e)
        {
            const string mutexName = "GeminiTranslateExplain_SingleInstance_Mutex";
            _mutex = new Mutex(true, mutexName, out bool createdNew);

            if (!createdNew)
            {
                // 既に起動しているので終了
                System.Windows.MessageBox.Show("すでに起動しています。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            ExceptionHandlerManager.RegisterHandlers();
            base.OnStartup(e);

            // wpfのテーマを設定
            _bundledTheme = Resources.MergedDictionaries.OfType<BundledTheme>().FirstOrDefault();
            ApplyTheme();

            //　MainWindow初期化
            var mainWindow = new MainWindow();
            if (mainWindow.DataContext is MainWindowViewModel mainWindowVM)
                WindowManager.Register(mainWindow, mainWindowVM);
            mainWindow.Closing += (s, args) =>
            {
                args.Cancel = true; // ウィンドウを閉じない
                mainWindow.Hide(); // ウィンドウを隠す
            };
            this.MainWindow = mainWindow;

            // Foreground change: hide SimpleResultWindow when focus moves to another window/app
            _foregroundWatcher = new ForegroundWatcher();
            _foregroundWatcher.ForegroundChanged += (hwnd) =>
            {
                if (DateTime.UtcNow < _ignoreSimpleResultWindowHideUntil)
                    return;

                var srw = Services.WindowManager.GetView<SimpleResultWindow>();
                if (srw == null || !srw.IsVisible) return;
                var srwHandle = new System.Windows.Interop.WindowInteropHelper(srw).Handle;
                if (hwnd != srwHandle)
                {
                    srw.Dispatcher.Invoke(() =>
                    {
                        if (srw.IsVisible)
                            srw.Hide();
                    });
                }
            };

            // SimpleResultWindow初期化
            var simpleResultWindow = new SimpleResultWindow();
            if (simpleResultWindow.DataContext is SimpleResultWindowViewModel simpleResultVM)
                WindowManager.Register(simpleResultWindow, simpleResultVM);
            simpleResultWindow.Closing += (s, args) =>
            {
                args.Cancel = true; // ウィンドウを閉じない
                simpleResultWindow.Hide(); // ウィンドウを隠す
            };

            _trayManager = new TrayManager(() => ShowWindow(mainWindow), Shutdown);
            DebugManager.Initialize();

            if (!AppConfig.Instance.MinimizeToTray)
                MainWindow.Show();

            AppConfig.Instance.ThemeModeChanged += _ => ApplyTheme();


            // ショートカット (ctrl + c + c) のアクションを設定
            _clipboardActionHandler = new ClipboardActionHandler(MainWindow, text => _ = ExecuteTranslationAsync(text));

            _globalHotKeyManager = new GlobalHotKeyManager(MainWindow);
            _globalHotKeyManager.HotKeyPressed += () => _ = OnGlobalHotKeyPressedAsync();
            UpdateGlobalHotKey(AppConfig.Instance.GlobalHotKey);
            AppConfig.Instance.GlobalHotKeyChanged += UpdateGlobalHotKey;
            UpdateScreenshotHotKey(AppConfig.Instance.ScreenshotHotKey);
            AppConfig.Instance.ScreenshotHotKeyChanged += UpdateScreenshotHotKey;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _clipboardActionHandler?.Dispose();
            _trayManager?.Dispose();
            _foregroundWatcher?.Dispose();
            _globalHotKeyManager?.Dispose();
            AppConfig.Instance.SaveConfigJson();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }

        /// <summary>
        /// Windowsのテーマがダークモードかどうかを判定
        /// </summary>
        private static bool IsDarkTheme()
        {
            const string key = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using var registryKey = Registry.CurrentUser.OpenSubKey(key);
            if (registryKey?.GetValue("AppsUseLightTheme") is int value)
            {
                return value == 0; // 0 = dark, 1 = light
            }
            return false;
        }

        private static BaseTheme ResolveBaseTheme()
        {
            return AppConfig.Instance.ThemeMode switch
            {
                ThemeMode.Light => BaseTheme.Light,
                ThemeMode.Dark => BaseTheme.Dark,
                _ => IsDarkTheme() ? BaseTheme.Dark : BaseTheme.Light
            };
        }

        private void ApplyTheme()
        {
            if (_bundledTheme == null)
                return;

            _bundledTheme.BaseTheme = ResolveBaseTheme();

            var isDark = _bundledTheme.BaseTheme == BaseTheme.Dark;
            if (isDark)
            {
                SetBrushResource("ChatUserBubbleForeground", System.Windows.Media.Color.FromRgb(0xEA, 0xF2, 0xF8));
                SetBrushResource("ChatAiBubbleBackground", System.Windows.Media.Color.FromRgb(0x2B, 0x2B, 0x2B));
                SetBrushResource("ChatAiBubbleBorder", System.Windows.Media.Color.FromRgb(0x4A, 0x4A, 0x4A));
                SetBrushResource("ChatAiBubbleForeground", System.Windows.Media.Color.FromRgb(0xE8, 0xE8, 0xE8));
                SetBrushResource("ChatLabelForeground", System.Windows.Media.Color.FromRgb(0xBD, 0xBD, 0xBD));
            }
            else
            {
                SetBrushResource("ChatUserBubbleForeground", System.Windows.Media.Color.FromRgb(0x1B, 0x2B, 0x3A));
                SetBrushResource("ChatAiBubbleBackground", System.Windows.Media.Color.FromRgb(0xE3, 0xE8, 0xEE));
                SetBrushResource("ChatAiBubbleBorder", System.Windows.Media.Color.FromRgb(0xB3, 0xBF, 0xCC));
                SetBrushResource("ChatAiBubbleForeground", System.Windows.Media.Color.FromRgb(0x22, 0x22, 0x22));
                SetBrushResource("ChatLabelForeground", System.Windows.Media.Color.FromRgb(0x6B, 0x6B, 0x6B));
            }
        }

        private void SetBrushResource(string key, System.Windows.Media.Color color)
        {
            Resources[key] = new SolidColorBrush(color);
        }

        private static void ShowWindow(Window? window)
        {
            if (window == null) return;

            window.Topmost = true;
            window.Show();
            WindowUtilities.ForceActive(window);
            window.Topmost = false;
        }

        private async Task ExecuteTranslationAsync(string text)
        {
            MainWindowViewModel? mainwindowVM = null;
            if (MainWindow?.DataContext is MainWindowViewModel vm)
            {
                mainwindowVM = vm;
            }

            // 設定されたウィンドウタイプのウィンドウを表示して位置を設定
            if (AppConfig.Instance.SelectedResultWindowType == WindowType.MainWindow)
            {
                ShowWindow(MainWindow);
                WindowPositioner.SetWindowPosition(MainWindow);
            }
            else if (AppConfig.Instance.SelectedResultWindowType == WindowType.SimpleResultWindow)
            {
                var window = WindowManager.GetView<SimpleResultWindow>();
                if (window != null)
                {
                    window.Owner = this.MainWindow;
                    window.ShowActivated = true;
                    ShowWindow(window);
                    WindowPositioner.SetWindowPosition(window);
                }
            }

            var result = mainwindowVM == null
                ? string.Empty
                : await mainwindowVM.SubmitMessageAsync(text, true);
            if (AppConfig.Instance.SelectedResultWindowType == WindowType.Clipboard)
            {
                _clipboardActionHandler?.SafeSetClipboardText(result);
                _trayManager?.ChangeCheckTemporaryIcon(1000);
            }
        }

        private async Task OnGlobalHotKeyPressedAsync()
        {
            if (_clipboardActionHandler == null)
                return;

            var text = await _clipboardActionHandler.TryGetSelectedTextAsync();
            if (string.IsNullOrWhiteSpace(text) == false)
            {
                await ExecuteTranslationAsync(text);
                return;
            }

            SystemSounds.Beep.Play();
        }

        private async Task OnScreenshotHotKeyPressedAsync()
        {
            if (_isScreenshotCapturing)
            {
                _activeScreenshotOverlay?.CancelCapture();
                return;
            }

            if (MainWindow?.DataContext is not MainWindowViewModel mainwindowVM)
                return;

            if (AppConfig.Instance.SelectedAiModel.Type != AiType.openai)
            {
                System.Windows.MessageBox.Show("画像入力はOpenAIモデルのみ対応しています。", "スクリーンショット", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _isScreenshotCapturing = true;
            Views.ScreenshotOverlayWindow? overlay = null;
            try
            {
                overlay = new Views.ScreenshotOverlayWindow();
                _activeScreenshotOverlay = overlay;
                var rect = await overlay.CaptureAsync();

                if (rect == null)
                    return;

                byte[] imageBytes;
                try
                {
                    imageBytes = ScreenCaptureService.CapturePngBytes(rect.Value);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"スクリーンショットの取得に失敗しました: {ex.Message}", "スクリーンショット", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await ExecuteImageQuestionAsync(mainwindowVM, imageBytes);
            }
            finally
            {
                overlay?.Close();
                _activeScreenshotOverlay = null;
                _isScreenshotCapturing = false;
            }
        }

        private async Task ExecuteImageQuestionAsync(MainWindowViewModel mainwindowVM, byte[] imageBytes)
        {
            if (AppConfig.Instance.SelectedResultWindowType == WindowType.MainWindow)
            {
                ShowWindow(MainWindow);
                WindowPositioner.SetWindowPosition(MainWindow);
            }
            else if (AppConfig.Instance.SelectedResultWindowType == WindowType.SimpleResultWindow)
            {
                var window = WindowManager.GetView<SimpleResultWindow>();
                if (window != null)
                {
                    _ignoreSimpleResultWindowHideUntil = DateTime.UtcNow.AddMilliseconds(750);
                    window.Owner = this.MainWindow;
                    window.ShowActivated = true;
                    ShowWindow(window);
                    WindowPositioner.SetWindowPosition(window);
                }
            }

            var result = await mainwindowVM.SubmitImageMessageAsync(imageBytes, true);
            if (AppConfig.Instance.SelectedResultWindowType == WindowType.Clipboard)
            {
                _clipboardActionHandler?.SafeSetClipboardText(result);
                _trayManager?.ChangeCheckTemporaryIcon(1000);
            }
        }

        private void UpdateGlobalHotKey(HotKeyDefinition hotKey)
        {
            if (_globalHotKeyManager == null)
                return;

            var registered = _globalHotKeyManager.Register(hotKey);
            if (!registered)
            {
                System.Windows.MessageBox.Show("グローバルショートカットの登録に失敗しました。他のアプリと競合している可能性があります。", "ショートカット設定", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateScreenshotHotKey(HotKeyDefinition hotKey)
        {
            if (_globalHotKeyManager == null)
                return;

            if (_screenshotHotKeyId.HasValue)
            {
                _globalHotKeyManager.UnregisterAdditional(_screenshotHotKeyId.Value);
                _screenshotHotKeyId = null;
            }

            if (_globalHotKeyManager.RegisterAdditional(hotKey, () => _ = OnScreenshotHotKeyPressedAsync(), out var newId))
            {
                _screenshotHotKeyId = newId;
                return;
            }

            System.Windows.MessageBox.Show("スクリーンショット用ショートカットの登録に失敗しました。他のアプリと競合している可能性があります。", "ショートカット設定", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
