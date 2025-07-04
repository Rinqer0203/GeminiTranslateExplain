﻿using System.Runtime.InteropServices;
using System.Windows;

namespace GeminiTranslateExplain.Services
{
    internal class WindowUtilities
    {
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
            var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
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
    }
}