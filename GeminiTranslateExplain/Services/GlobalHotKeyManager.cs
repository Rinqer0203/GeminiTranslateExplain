using GeminiTranslateExplain.Models;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace GeminiTranslateExplain.Services
{
    public class GlobalHotKeyManager : IDisposable
    {
        private readonly Window _window;
        private readonly nint _hwnd;
        private readonly HwndSource _hwndSource;
        private bool _disposed;
        private bool _registered;
        private int _hotKeyId;

        private const int WM_HOTKEY = 0x0312;
        private const int ModAlt = 0x0001;
        private const int ModControl = 0x0002;
        private const int ModShift = 0x0004;
        private const int ModWin = 0x0008;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(nint hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(nint hWnd, int id);

        public event Action? HotKeyPressed;

        public GlobalHotKeyManager(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));

            var helper = new WindowInteropHelper(_window);
            _hwnd = helper.EnsureHandle();
            _hwndSource = HwndSource.FromHwnd(_hwnd) ?? throw new InvalidOperationException("HwndSource could not be created.");
            _hwndSource.AddHook(WndProc);
        }

        public bool Register(HotKeyDefinition hotKey)
        {
            Unregister();

            _hotKeyId = 0x1000;
            int modifiers = ConvertModifiers(hotKey.Modifiers);
            int vk = KeyInterop.VirtualKeyFromKey(hotKey.Key);

            _registered = RegisterHotKey(_hwnd, _hotKeyId, modifiers, vk);
            return _registered;
        }

        public void Unregister()
        {
            if (_registered)
            {
                UnregisterHotKey(_hwnd, _hotKeyId);
                _registered = false;
            }
        }

        private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == _hotKeyId)
            {
                HotKeyPressed?.Invoke();
                handled = true;
            }

            return nint.Zero;
        }

        private static int ConvertModifiers(ModifierKeys modifiers)
        {
            int result = 0;
            if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) result |= ModAlt;
            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control) result |= ModControl;
            if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) result |= ModShift;
            if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows) result |= ModWin;
            return result;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Unregister();
            _hwndSource.RemoveHook(WndProc);
        }
    }
}
