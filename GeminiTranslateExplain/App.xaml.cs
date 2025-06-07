using GeminiTranslateExplain.Models;
using GeminiTranslateExplain.Services;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System.Windows;

namespace GeminiTranslateExplain
{
    public partial class App : System.Windows.Application
    {
        private static Mutex? _mutex;
        private static readonly object ClipboardLock = new object(); // クリップボード操作の同期用

        private ClipboardActionHandler? _clipboardActionHandler;
        private TrayManager? _trayManager;


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
            var bundledTheme = new BundledTheme
            {
                BaseTheme = IsDarkTheme() ? BaseTheme.Dark : BaseTheme.Light,
                PrimaryColor = PrimaryColor.Indigo,
                SecondaryColor = SecondaryColor.DeepPurple
            };
            Resources.MergedDictionaries.Add(bundledTheme);
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml")
            });

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


            // ショートカット (ctrl + c + c) のアクションを設定
            _clipboardActionHandler = new ClipboardActionHandler(MainWindow, async (text) =>
            {
                // SourceTextを更新
                if (MainWindow?.DataContext is MainWindowViewModel mainwindowVM)
                {
                    mainwindowVM.SourceText = text;
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
                    ShowWindow(window);
                    WindowPositioner.SetWindowPosition(window);
                }

                var geminiApiManager = ApiRequestManager.Instance;
                geminiApiManager.ClearMessages();
                geminiApiManager.AddMessage("user", text);

                var result = await geminiApiManager.RequestTranslation();
                if (AppConfig.Instance.SelectedResultWindowType == WindowType.Clipboard)
                {
                    _clipboardActionHandler?.SafeSetClipboardText(result);
                    _trayManager?.ChangeCheckTemporaryIcon(1000);
                }
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _clipboardActionHandler?.Dispose();
            _trayManager?.Dispose();
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

        private static void ShowWindow(Window? window)
        {
            if (window == null) return;

            window.Topmost = true;
            window.Show();
            WindowUtilities.ForceActive(window);
            window.Topmost = false;
        }
    }
}