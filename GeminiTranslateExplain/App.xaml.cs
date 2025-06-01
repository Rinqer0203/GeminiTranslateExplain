using GeminiTranslateExplain.Models;
using System.Windows;
using Clipboard = System.Windows.Clipboard;

namespace GeminiTranslateExplain
{
    public partial class App : System.Windows.Application
    {
        private static Mutex? _mutex;

        private ClipboardMonitor? _clipboardMonitor;
        private TrayManager? _trayManager;
        private SimpleResultWindow? _simpleResultWindow;

        private DateTime _lastClipboardUpdateTime = DateTime.MinValue;
        private string _lastClipboardText = string.Empty;
        private string _lastResultText = string.Empty;

        // 指定された秒数以内に同じテキストがクリップボードにコピーされた場合に翻訳を実行
        private const int TranslationTriggerIntervalSeconds = 1;

        protected override void OnStartup(StartupEventArgs e)
        {
            const string mutexName = "GeminiTranslateExplain_SingleInstance_Mutex";

            bool createdNew;
            _mutex = new Mutex(true, mutexName, out createdNew);

            if (!createdNew)
            {
                // 既に起動しているので自分自身を終了
                System.Windows.MessageBox.Show("すでに起動しています。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }
            base.OnStartup(e);

            MainWindow = new MainWindow();
            MainWindow.Closing += (s, args) =>
            {
                args.Cancel = true; // ウィンドウを閉じない
                MainWindow.Hide(); // ウィンドウを隠す
            };

            _simpleResultWindow = new();
            _simpleResultWindow.Closing += (s, args) =>
            {
                args.Cancel = true; // ウィンドウを閉じない
                _simpleResultWindow.Hide(); // ウィンドウを隠す
            };

            _clipboardMonitor = new ClipboardMonitor(MainWindow, OnClipboardUpdate);
            _trayManager = new TrayManager(() => ShowWindow(MainWindow), Shutdown);

            if (!AppConfig.Instance.MinimizeToTray)
                MainWindow.Show();
        }


        private void OnClipboardUpdate()
        {
            // このイベントはclipboard.settextのときに、テキストのリセット&セットで2回呼ばれる
            // _lastResultTextによる分岐がないと初期値のcurrentTextとリセット時のテキストが同じになって無限ループになる
            if (!Clipboard.ContainsText())
                return;

            string currentText = Clipboard.GetText();
            DateTime now = DateTime.Now;

            if (_lastResultText == currentText)
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
                    SetWindowPosition(MainWindow);
                    ShowWindow(MainWindow);
                }
                else if (AppConfig.Instance.SelectedResultWindowType == WindowType.SimpleResultWindow)
                {
                    SetWindowPosition(_simpleResultWindow);
                    ShowWindow(_simpleResultWindow);
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
            if (window == null) return;

            var cursorPosition = Cursor.Position;

            double windowWidth = window.Width > 0 ? window.Width : 400;
            double windowHeight = window.Height > 0 ? window.Height : 300;

            var screen = Screen.FromPoint(cursorPosition);
            var workingArea = screen.WorkingArea;

            // デフォルトのオフセット
            int offsetX = 20;
            int offsetY = 20;

            // 方向を決める（右下がデフォルトだが、画面の端に近い場合は反転）
            bool moveLeft = (cursorPosition.X + offsetX + windowWidth > workingArea.Right);
            bool moveUp = (cursorPosition.Y + offsetY + windowHeight > workingArea.Bottom);

            double targetLeft = cursorPosition.X + (moveLeft ? -offsetX - windowWidth : offsetX);
            double targetTop = cursorPosition.Y + (moveUp ? -offsetY - windowHeight : offsetY);

            // ウィンドウが画面外に出ないよう最終チェック（念のため）
            if (targetLeft < workingArea.Left)
                targetLeft = workingArea.Left;
            else if (targetLeft + windowWidth > workingArea.Right)
                targetLeft = workingArea.Right - windowWidth;

            if (targetTop < workingArea.Top)
                targetTop = workingArea.Top;
            else if (targetTop + windowHeight > workingArea.Bottom)
                targetTop = workingArea.Bottom - windowHeight;

            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = targetLeft;
            window.Top = targetTop;
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