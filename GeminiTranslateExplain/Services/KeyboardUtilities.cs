using System.Runtime.InteropServices;

namespace GeminiTranslateExplain.Services
{
    public static class KeyboardUtilities
    {
        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const ushort VK_CONTROL = 0x11;
        private const ushort VK_C = 0x43;
        private const int WM_COPY = 0x0301;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern nint GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [DllImport("user32.dll")]
        private static extern nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public INPUTUNION u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public nint dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GUITHREADINFO
        {
            public uint cbSize;
            public uint flags;
            public nint hwndActive;
            public nint hwndFocus;
            public nint hwndCapture;
            public nint hwndMenuOwner;
            public nint hwndMoveSize;
            public nint hwndCaret;
            public RECT rcCaret;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        public static void SendCopyShortcut()
        {
            try
            {
                System.Windows.Forms.SendKeys.SendWait("^c");
                System.Windows.Forms.SendKeys.Flush();
                return;
            }
            catch
            {
                // SendKeysが失敗する環境向けにSendInputへフォールバック
            }

            var inputs = new[]
            {
                CreateKeyInput(VK_CONTROL, 0),
                CreateKeyInput(VK_C, 0),
                CreateKeyInput(VK_C, KEYEVENTF_KEYUP),
                CreateKeyInput(VK_CONTROL, KEYEVENTF_KEYUP),
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }

        public static void SendCopyMessageToForeground()
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == nint.Zero)
                return;

            SendMessage(hwnd, WM_COPY, nint.Zero, nint.Zero);
        }

        public static bool TrySendCopyMessageToFocusedControl()
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == nint.Zero)
                return false;

            var threadId = GetWindowThreadProcessId(hwnd, out _);
            var info = new GUITHREADINFO { cbSize = (uint)Marshal.SizeOf<GUITHREADINFO>() };
            if (GetGUIThreadInfo(threadId, ref info) == false || info.hwndFocus == nint.Zero)
                return false;

            SendMessage(info.hwndFocus, WM_COPY, nint.Zero, nint.Zero);
            return true;
        }

        private static INPUT CreateKeyInput(ushort key, uint flags)
        {
            return new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new INPUTUNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = key,
                        wScan = 0,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = 0
                    }
                }
            };
        }
    }
}
