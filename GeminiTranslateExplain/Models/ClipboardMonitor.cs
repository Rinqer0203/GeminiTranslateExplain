using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GeminiTranslateExplain
{
    public class ClipboardMonitor : IDisposable
    {
        private readonly HwndSource _hwndSource;
        private readonly Window _window;
        private readonly Action _onClipboardUpdate;
        private readonly IntPtr _hwnd;
        private bool _disposed = false;

        private const int WM_CLIPBOARDUPDATE = 0x031D;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        public ClipboardMonitor(Window window, Action onClipboardUpdate)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _onClipboardUpdate = onClipboardUpdate ?? throw new ArgumentNullException(nameof(onClipboardUpdate));

            var helper = new WindowInteropHelper(_window);
            _hwnd = helper.EnsureHandle();
            _hwndSource = HwndSource.FromHwnd(_hwnd) ?? throw new InvalidOperationException("HwndSource could not be created.");

            _hwndSource.AddHook(WndProc);

            AddClipboardFormatListener(_hwnd);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                _onClipboardUpdate?.Invoke();
                handled = true;
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            RemoveClipboardFormatListener(_hwnd);
            _hwndSource.RemoveHook(WndProc);
        }
    }
}