using System.Windows;
using Clipboard = System.Windows.Clipboard;

namespace GeminiTranslateExplain
{
    public partial class App : System.Windows.Application
    {
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
                args.Cancel = true;
                _simpleResultWindow.Hide();
            };

            _clipboardMonitor = new ClipboardMonitor(MainWindow, OnClipboardUpdate);
            _trayManager = new TrayManager(() => TryShowWindow(MainWindow), Shutdown);
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
                    TryShowWindow(MainWindow);
                }
                else if (AppConfig.Instance.SelectedResultWindowType == WindowType.SimpleResultWindow)
                {
                    SetWindowPosition(_simpleResultWindow);
                    TryShowWindow(_simpleResultWindow);
                }

                var geminiApiManagerInstance = GeminiApiManager.Instance;
                geminiApiManagerInstance.ClearMessages();
                geminiApiManagerInstance.AddMessage("user", currentText);

                var progress = new Progress<string>(text =>
                {
                    if (MainWindow?.DataContext is MainWindowViewModel mainwindowVM)
                    {
                        mainwindowVM.TranslatedText = text;
                    }
                    if (_simpleResultWindow?.DataContext is SimpleResultWindowViewModel simpleResultWindowVM)
                    {
                        simpleResultWindowVM.TranslatedText = text;
                    }
                });

                Task.Run(async () =>
                {
                    var result = await geminiApiManagerInstance.RequestTranslation(progress);
                    _trayManager?.ChangeCheckTemporaryIcon(2000);
                    if (AppConfig.Instance.SelectedResultWindowType == WindowType.Clipboard)
                    {
                        // クリップボードに翻訳結果をコピー
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            _lastResultText = result;
                            Clipboard.SetText(result);
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

        private static void TryShowWindow(Window? window)
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
            base.OnExit(e);
        }
    }
}