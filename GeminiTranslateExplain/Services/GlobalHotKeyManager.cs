using GeminiTranslateExplain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace GeminiTranslateExplain.Services
{
    public class GlobalHotKeyManager : IDisposable
    {
        private const int PrimaryHotKeyId = 0x1000;

        private readonly Window _window;
        private readonly nint _hwnd;
        private readonly HwndSource _hwndSource;
        private readonly Dictionary<int, Action> _callbacks = new();
        private bool _disposed;
        private int _nextId = PrimaryHotKeyId + 1;

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
            return RegisterInternal(PrimaryHotKeyId, hotKey, () => HotKeyPressed?.Invoke());
        }

        public bool RegisterAdditional(HotKeyDefinition hotKey, Action callback, out int id)
        {
            id = _nextId++;
            return RegisterInternal(id, hotKey, callback);
        }

        public void Unregister()
        {
            if (_callbacks.ContainsKey(PrimaryHotKeyId))
            {
                UnregisterHotKey(_hwnd, PrimaryHotKeyId);
                _callbacks.Remove(PrimaryHotKeyId);
            }
        }

        public void UnregisterAdditional(int id)
        {
            if (id == PrimaryHotKeyId)
                return;

            if (_callbacks.ContainsKey(id))
            {
                UnregisterHotKey(_hwnd, id);
                _callbacks.Remove(id);
            }
        }

        private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                var id = wParam.ToInt32();
                if (_callbacks.TryGetValue(id, out var callback))
                {
                    callback.Invoke();
                    handled = true;
                }
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

            foreach (var id in _callbacks.Keys.ToArray())
            {
                UnregisterHotKey(_hwnd, id);
            }
            _callbacks.Clear();
            _hwndSource.RemoveHook(WndProc);
        }

        private bool RegisterInternal(int id, HotKeyDefinition hotKey, Action callback)
        {
            int modifiers = ConvertModifiers(hotKey.Modifiers);
            int vk = KeyInterop.VirtualKeyFromKey(hotKey.Key);

            var registered = RegisterHotKey(_hwnd, id, modifiers, vk);
            if (registered)
            {
                _callbacks[id] = callback;
            }
            return registered;
        }
    }
}
