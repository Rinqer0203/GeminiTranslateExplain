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
            var hwnd = helper.EnsureHandle();
            _hwndSource = HwndSource.FromHwnd(hwnd)!;
            _hwndSource.AddHook(WndProc);

            AddClipboardFormatListener(hwnd);
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
            var hwnd = new WindowInteropHelper(_window).Handle;
            RemoveClipboardFormatListener(hwnd);
            _hwndSource.RemoveHook(WndProc);
        }
    }
}