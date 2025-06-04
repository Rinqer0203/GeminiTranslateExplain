namespace GeminiTranslateExplain.Services
{
    public class TrayManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly Icon _defaultIcon;
        private CancellationTokenSource? _iconChangeTokenSource;

        public TrayManager(Action onShowWindow, Action onExit)
        {
            var iconStream = System.Windows.Application.GetResourceStream(new Uri("icon.ico", UriKind.Relative))?.Stream;
            if (iconStream == null)
            {
                throw new InvalidOperationException("アイコンファイルが見つかりません。");
            }

            _defaultIcon = new Icon(iconStream);

            _notifyIcon = new NotifyIcon
            {
                Icon = _defaultIcon,
                Visible = true,
                Text = "GeminiTranslateExplain"
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("表示", null, (_, _) => onShowWindow());
            contextMenu.Items.Add("終了", null, (_, _) => onExit());

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (_, _) => onShowWindow();

            var iconUri = new Uri("pack://application:,,,/icon.ico", UriKind.Absolute);
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (System.Windows.Application.Current.MainWindow != null)
                {
                    System.Windows.Application.Current.MainWindow.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(iconUri);
                }
            });
        }

        public async void ChangeCheckTemporaryIcon(int durationMs = 3000)
        {
            // キャンセルがあれば前回の一時変更を終了
            _iconChangeTokenSource?.Cancel();

            _iconChangeTokenSource = new CancellationTokenSource();
            var token = _iconChangeTokenSource.Token;

            try
            {
                var streamResource = System.Windows.Application.GetResourceStream(new Uri("check.ico", UriKind.Relative))?.Stream;
                if (streamResource == null) return;

                using var tempIcon = new Icon(streamResource);
                _notifyIcon.Icon = tempIcon;

                await Task.Delay(durationMs, token);
            }
            catch (TaskCanceledException) { }
            finally
            {
                _notifyIcon.Icon = _defaultIcon;
            }
        }

        public void Dispose()
        {
            _notifyIcon.Dispose();
        }
    }
}