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
            // これを発生させないい実装を考える
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

                //var mainWindowVM = MainWindow?.DataContext as MainWindowViewModel;
                //var simpleResultWindowVM = _simpleResultWindow.DataContext as SimpleResultWindowViewModel;

                //mainWindowVM.SourceText = currentText;

                if (AppConfig.Instance.SelectedResultWindowType == WindowType.MainWindow)
                    TryShowWindow(MainWindow);
                else if (AppConfig.Instance.SelectedResultWindowType == WindowType.SimpleResultWindow)
                    TryShowWindow(_simpleResultWindow);

                var instance = GeminiApiManager.Instance;
                instance.ClearMessages();
                instance.AddMessage("user", currentText);
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
                    var result = await instance.RequestTranslation(progress);
                    if (AppConfig.Instance.SelectedResultWindowType == WindowType.Clipboard)
                    {
                        // クリップボードに翻訳結果をコピー
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            _lastResultText = result;
                            Clipboard.SetText(result);
                            //System.Windows.MessageBox.Show(result);
                        });
                    }
                });
            }

            _lastClipboardUpdateTime = now;
            _lastClipboardText = currentText;
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
