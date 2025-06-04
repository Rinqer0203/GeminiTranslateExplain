using GeminiTranslateExplain.Models;
using GeminiTranslateExplain.Services;
using System.Windows;
using System.Windows.Threading;
using Clipboard = System.Windows.Clipboard;

namespace GeminiTranslateExplain
{
    public partial class App : System.Windows.Application
    {
        private static Mutex? _mutex;

        private ClipboardMonitor? _clipboardMonitor;
        private TrayManager? _trayManager;

        private DateTime _lastClipboardUpdateTime = DateTime.MinValue;
        private string _lastClipboardText = string.Empty;
        private string _lastResultText = string.Empty;


        // 指定された秒数以内に同じテキストがクリップボードにコピーされた場合に翻訳を実行
        private const int TranslationTriggerIntervalSeconds = 1;


        protected override void OnStartup(StartupEventArgs e)
        {
            const string mutexName = "GeminiTranslateExplain_SingleInstance_Mutex";

            _mutex = new Mutex(true, mutexName, out bool createdNew);

            if (!createdNew)
            {
                // 既に起動しているので自分自身を終了
                System.Windows.MessageBox.Show("すでに起動しています。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            ExceptionHandlerManager.RegisterHandlers();
            base.OnStartup(e);

            var mainWindow = new MainWindow();
            if (mainWindow.DataContext is MainWindowViewModel mainWindowVM)
                WindowManager.Register(mainWindow, mainWindowVM);
            mainWindow.Closing += (s, args) =>
            {
                args.Cancel = true; // ウィンドウを閉じない
                mainWindow.Hide(); // ウィンドウを隠す
            };
            this.MainWindow = mainWindow;

            var simpleResultWindow = new SimpleResultWindow();
            if (simpleResultWindow.DataContext is SimpleResultWindowViewModel simpleResultVM)
                WindowManager.Register(simpleResultWindow, simpleResultVM);
            simpleResultWindow.Closing += (s, args) =>
            {
                args.Cancel = true; // ウィンドウを閉じない
                simpleResultWindow.Hide(); // ウィンドウを隠す
            };

            _clipboardMonitor = new ClipboardMonitor(MainWindow, OnClipboardUpdate);
            _trayManager = new TrayManager(() => ShowWindow(MainWindow), Shutdown);

            if (!AppConfig.Instance.MinimizeToTray)
                MainWindow.Show();

            // デバッグモードでウィンドウ位置を更新するタイマーを設定
            if (AppConfig.Instance.DebugWindowPositionMode)
            {
                var debugWindowTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(1500)
                };
                debugWindowTimer.Tick += (sender, args) =>
                {
                    Window? targetWindow = null;

                    if (AppConfig.Instance.SelectedResultWindowType == WindowType.MainWindow)
                        targetWindow = MainWindow;
                    else if (AppConfig.Instance.SelectedResultWindowType == WindowType.SimpleResultWindow)
                        targetWindow = WindowManager.GetView<SimpleResultWindow>();

                    if (targetWindow != null)
                    {
                        SetWindowPosition(targetWindow);
                        ShowWindow(targetWindow);
                    }
                };
                debugWindowTimer.Start();
            }
        }


        private void OnClipboardUpdate()
        {
            // このイベントはclipboard.settextのときに、テキストのリセット&セットで2回呼ばれる
            // _lastResultTextによる分岐がないと初期値のcurrentTextとリセット時のテキストが同じになって無限ループになる
            if (!Clipboard.ContainsText())
                return;

            string currentText = Clipboard.GetText();
            DateTime now = DateTime.Now;

            if (_lastResultText == currentText || string.IsNullOrWhiteSpace(currentText))
                return;

            // 指定秒数以内かつ同じテキストかどうか
            if ((now - _lastClipboardUpdateTime).TotalSeconds <= TranslationTriggerIntervalSeconds &&
                currentText == _lastClipboardText)
            {

                if (MainWindow?.DataContext is MainWindowViewModel mainwindowVM)
                {
                    mainwindowVM.SourceText = currentText;
                }

                if (AppConfig.Instance.SelectedResultWindowType == WindowType.MainWindow)
                {
                    ShowWindow(MainWindow);
                    SetWindowPosition(MainWindow);
                }
                else if (AppConfig.Instance.SelectedResultWindowType == WindowType.SimpleResultWindow)
                {
                    var window = WindowManager.GetView<SimpleResultWindow>();
                    ShowWindow(window);
                    SetWindowPosition(window);
                }

                var geminiApiManager = GeminiApiManager.Instance;
                geminiApiManager.ClearMessages();
                geminiApiManager.AddMessage("user", currentText);

                Task.Run(async () =>
                {
                    var result = await geminiApiManager.RequestTranslation();
                    if (AppConfig.Instance.SelectedResultWindowType == WindowType.Clipboard)
                    {
                        // クリップボードに翻訳結果をコピー
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            _lastResultText = result;
                            Clipboard.SetText(result);
                            _trayManager?.ChangeCheckTemporaryIcon(2000);
                        });
                    }
                });
            }

            _lastClipboardUpdateTime = now;
            _lastClipboardText = currentText;
        }

        private static void SetWindowPosition(Window? window)
        {
            WindowPositioner.SetWindowPosition(window);
        }

        private static void ShowWindow(Window? window)
        {
            if (window == null) return;

            window.Show();
            window.WindowState = WindowState.Normal;
            window.Activate();
            window.Topmost = true;
            window.Topmost = false;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _clipboardMonitor?.Dispose();
            _trayManager?.Dispose();
            AppConfig.Instance.SaveConfigJson();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}