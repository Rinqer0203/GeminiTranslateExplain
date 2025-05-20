using System.Windows;
using Clipboard = System.Windows.Clipboard;

namespace GeminiTranslateExplain
{
    public partial class App : System.Windows.Application
    {
        private ClipboardMonitor? _clipboardMonitor;
        private TrayManager? _trayManager;

        private DateTime _lastClipboardUpdateTime = DateTime.MinValue;
        private string _lastClipboardText = string.Empty;

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

            _clipboardMonitor = new ClipboardMonitor(MainWindow, OnClipboardUpdate);
            _trayManager = new TrayManager(ShowWindow, Shutdown);
            MainWindow.Show();
        }


        private void OnClipboardUpdate()
        {
            if (!Clipboard.ContainsText())
                return;

            string currentText = Clipboard.GetText();
            DateTime now = DateTime.Now;

            // 指定秒数以内かつ同じテキストかどうか
            if ((now - _lastClipboardUpdateTime).TotalSeconds <= TranslationTriggerIntervalSeconds &&
                currentText == _lastClipboardText)
            {
                if (MainWindow?.DataContext is MainWindowViewModel viewModel)
                {
                    // クリップボードの内容を翻訳する
                    viewModel.SourceText = currentText;
                    viewModel.TranslateTextCommand.Execute(null);
                    ShowWindow();
                }
            }

            _lastClipboardUpdateTime = now;
            _lastClipboardText = currentText;
        }

        private void ShowWindow()
        {
            if (MainWindow != null)
            {
                MainWindow.Show();
                MainWindow.WindowState = WindowState.Normal;
                MainWindow.Activate();
                MainWindow.Topmost = true;
                MainWindow.Topmost = false;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _clipboardMonitor?.Dispose();
            _trayManager?.Dispose();
            base.OnExit(e);
        }
    }

}
