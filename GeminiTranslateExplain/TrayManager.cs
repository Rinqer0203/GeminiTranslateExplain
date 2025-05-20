namespace GeminiTranslateExplain
{
    public class TrayManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;

        public TrayManager(Action onShowWindow, Action onExit)
        {
            var iconStream = System.Windows.Application.GetResourceStream(new Uri("icon.ico", UriKind.Relative))?.Stream;
            if (iconStream == null)
                throw new InvalidOperationException("アイコンファイルが見つかりません。");

            _notifyIcon = new NotifyIcon
            {
                Icon = new Icon(iconStream),
                Visible = true,
                Text = "GeminiTranslateExplain"
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("表示", null, (_, _) => onShowWindow());
            contextMenu.Items.Add("終了", null, (_, _) => onExit());

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (_, _) => onShowWindow();
        }

        public void Dispose()
        {
            _notifyIcon.Dispose();
        }
    }
}