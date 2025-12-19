using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeminiTranslateExplain.Services
{
    public class TrayManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly Icon _defaultIcon;
        private readonly Icon _checkIcon;
        private readonly Icon _processingIcon;
        private readonly Icon _failedIcon;
        private CancellationTokenSource? _iconChangeTokenSource;
        private bool _isProcessing;

        public TrayManager(Action onShowWindow, Action onExit)
        {
            var iconStream = System.Windows.Application.GetResourceStream(new Uri("icon.ico", UriKind.Relative))?.Stream;
            if (iconStream == null)
            {
                throw new InvalidOperationException("アイコンファイルが見つかりません。");
            }

            using (var baseIcon = new Icon(iconStream))
            {
                _defaultIcon = (Icon)baseIcon.Clone();
            }

            _checkIcon = LoadIconFromResource("check.ico");
            _processingIcon = LoadIconFromResource("processing.ico");
            _failedIcon = LoadIconFromResource("failed.ico");

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
                _notifyIcon.Icon = _checkIcon;

                await Task.Delay(durationMs, token);
            }
            catch (TaskCanceledException) { }
            finally
            {
                _notifyIcon.Icon = _isProcessing ? _processingIcon : _defaultIcon;
            }
        }

        public async void ChangeFailedTemporaryIcon(int durationMs = 3000)
        {
            _iconChangeTokenSource?.Cancel();

            _iconChangeTokenSource = new CancellationTokenSource();
            var token = _iconChangeTokenSource.Token;

            try
            {
                _notifyIcon.Icon = _failedIcon;

                await Task.Delay(durationMs, token);
            }
            catch (TaskCanceledException) { }
            finally
            {
                _notifyIcon.Icon = _isProcessing ? _processingIcon : _defaultIcon;
            }
        }

        public void SetProcessingIcon(bool isProcessing)
        {
            _isProcessing = isProcessing;
            if (isProcessing)
            {
                _iconChangeTokenSource?.Cancel();
                _notifyIcon.Icon = _processingIcon;
                return;
            }

            _notifyIcon.Icon = _defaultIcon;
        }

        public void Dispose()
        {
            _notifyIcon.Dispose();
            _defaultIcon.Dispose();
            _checkIcon.Dispose();
            _processingIcon.Dispose();
            _failedIcon.Dispose();
        }

        private static Icon LoadIconFromResource(string fileName)
        {
            var resourceUri = new Uri($"pack://application:,,,/{fileName}", UriKind.Absolute);
            var streamResource = System.Windows.Application.GetResourceStream(resourceUri)?.Stream;
            if (streamResource == null)
            {
                throw new InvalidOperationException($"アイコンファイルが見つかりません: {fileName}");
            }

            using var baseIcon = new Icon(streamResource);
            return (Icon)baseIcon.Clone();
        }
    }
}
