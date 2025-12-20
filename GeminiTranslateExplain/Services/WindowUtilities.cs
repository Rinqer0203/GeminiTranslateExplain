using GeminiTranslateExplain.Models;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GeminiTranslateExplain.Services
{
    internal class WindowUtilities
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AttachThreadInput(int idAttach, int idAttachTo, bool fAttach);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        /// <summary>
        /// Windowフォームアクティブ化処理
        /// </summary>
        /// <param name="handle">フォームハンドル</param>
        /// <returns>true : 成功 / false : 失敗</returns>
        internal static bool ForceActive(Window window)
        {
            var handle = new WindowInteropHelper(window).Handle;
            const uint SPI_GETFOREGROUNDLOCKTIMEOUT = 0x2000;
            const uint SPI_SETFOREGROUNDLOCKTIMEOUT = 0x2001;
            const int SPIF_SENDCHANGE = 0x2;

            IntPtr dummy = IntPtr.Zero;
            IntPtr timeout = IntPtr.Zero;

            bool isSuccess = false;

            int processId;
            // フォアグラウンドウィンドウを作成したスレッドのIDを取得
            int foregroundID = GetWindowThreadProcessId(GetForegroundWindow(), out processId);
            // 目的のウィンドウを作成したスレッドのIDを取得
            int targetID = GetWindowThreadProcessId(handle, out processId);

            // スレッドのインプット状態を結び付ける
            AttachThreadInput(targetID, foregroundID, true);

            // 現在の設定を timeout に保存
            SystemParametersInfo(SPI_GETFOREGROUNDLOCKTIMEOUT, 0, timeout, 0);
            // ウィンドウの切り替え時間を 0ms にする
            SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, dummy, SPIF_SENDCHANGE);

            // ウィンドウをフォアグラウンドに持ってくる
            isSuccess = SetForegroundWindow(handle);

            // 設定を元に戻す
            SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, timeout, SPIF_SENDCHANGE);

            // スレッドのインプット状態を切り離す
            AttachThreadInput(targetID, foregroundID, false);

            return isSuccess;
        }

        internal static void ApplyTitleBarTheme(Window window)
        {
            if (window == null || window.WindowStyle == WindowStyle.None)
                return;

            if (window.IsLoaded)
            {
                ApplyTitleBarThemeCore(window);
                return;
            }

            window.SourceInitialized += (_, _) => ApplyTitleBarThemeCore(window);
        }

        internal static void ApplyTitleBarThemeToAllWindows()
        {
            if (System.Windows.Application.Current == null)
                return;

            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                ApplyTitleBarTheme(window);
            }
        }

        private static void ApplyTitleBarThemeCore(Window window)
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
                return;

            var isDark = ResolveIsDarkTheme();
            var captionColor = isDark ? ToColorRef(0x2B, 0x2B, 0x2B) : ToColorRef(0xF3, 0xF3, 0xF3);
            var textColor = isDark ? ToColorRef(0xFF, 0xFF, 0xFF) : ToColorRef(0x00, 0x00, 0x00);

            const int DWMWA_CAPTION_COLOR = 35;
            const int DWMWA_TEXT_COLOR = 36;

            DwmSetWindowAttribute(handle, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));
            DwmSetWindowAttribute(handle, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
        }

        private static bool ResolveIsDarkTheme()
        {
            return AppConfig.Instance.ThemeMode switch
            {
                ThemeMode.Light => false,
                ThemeMode.Dark => true,
                _ => IsSystemDarkTheme()
            };
        }

        private static bool IsSystemDarkTheme()
        {
            const string key = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using var registryKey = Registry.CurrentUser.OpenSubKey(key);
            if (registryKey?.GetValue("AppsUseLightTheme") is int value)
            {
                return value == 0;
            }
            return false;
        }

        private static int ToColorRef(byte r, byte g, byte b)
        {
            return r | (g << 8) | (b << 16);
        }
    }
}
