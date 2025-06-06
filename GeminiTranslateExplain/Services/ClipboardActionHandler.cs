using System.Windows;
using Clipboard = System.Windows.Clipboard; // エイリアスで明確化

namespace GeminiTranslateExplain.Services
{
    public class ClipboardActionHandler : IDisposable
    {
        private static readonly object ClipboardLock = new();

        private readonly ClipboardMonitor _clipboardMonitor;
        private readonly Action<string> _clipboardAction;

        private DateTime _lastUpdateTime = DateTime.MinValue;
        private string _lastText = string.Empty;
        private string _lastSetText = string.Empty;

        /// <summary>
        /// クリップボード更新のアクションを実行する間隔 (ミリ秒)
        /// </summary>
        private const int IntervalMs = 1000;
        private const int RetryCount = 5;
        private const int RetryDelayMs = 10;

        public ClipboardActionHandler(Window mainWindow, Action<string> ClipBoardAction)
        {
            _clipboardAction = ClipBoardAction;
            _clipboardMonitor = new ClipboardMonitor(mainWindow, OnClipboardUpdate);
        }

        public void SafeSetClipboardText(string text)
        {
            ExecuteWithRetry(() => Clipboard.SetText(text), "クリップボードへのテキスト設定に失敗しました。");
            _lastSetText = text;
        }

        private static bool SafeClipboardContainsText()
        {
            return ExecuteWithRetry(() => Clipboard.ContainsText(), "クリップボードのテキスト確認に失敗しました。");
        }

        private static string SafeGetClipboardText()
        {
            return ExecuteWithRetry(() => Clipboard.GetText(), "クリップボードからのテキスト取得に失敗しました。");
        }

        /// <summary>
        /// クリップボードの操作は競合するとCOMExceptionが発生する可能性があるため、リトライ処理を行う
        /// </summary>
        private static T ExecuteWithRetry<T>(Func<T> action, string errorMessage)
        {
            lock (ClipboardLock)
            {
                for (int i = 0; i < RetryCount; i++)
                {
                    try
                    {
                        return action();
                    }
                    catch (System.Runtime.InteropServices.COMException ex)
                    {
                        ErrorLogger.Log($"{errorMessage} 再試行します {i + 1}回目", ex);
                        Thread.Sleep(RetryDelayMs);
                    }
                }
                throw new InvalidOperationException($"{errorMessage} 最大リトライ回数を超えました。");
            }
        }

        private static void ExecuteWithRetry(Action action, string errorMessage)
        {
            ExecuteWithRetry(() =>
            {
                action();
                return true;
            }, errorMessage);
        }

        /// <summary>
        /// クリップボードの更新イベント
        /// 仕様で一度のテキスト選択に対して何度コピーしても、クリップボードの更新は2回までらしい
        /// </summary>
        private void OnClipboardUpdate()
        {
            var now = DateTime.Now;
            var intervalMs = (now - _lastUpdateTime).TotalMilliseconds;

            // 特定のテキストボックスで、コピー時に2回クリップボードが更新されることがあるため、
            // 前回の更新からの時間が1ミリ秒未満、またはクリップボードにテキストがない場合は何もしない
            if (intervalMs < 1 || SafeClipboardContainsText() == false)
                return;

            var clipboardText = SafeGetClipboardText();

            // クリップボードの内容が空、または前にクリップボードに設定したテキストと同じ場合は何もしない
            if (string.IsNullOrWhiteSpace(clipboardText) || clipboardText == _lastSetText)
                return;

            // 前回の更新からの時間が指定した間隔以内で、かつクリップボードの内容が前回と同じ場合はアクションを実行
            if (intervalMs <= IntervalMs && clipboardText == _lastText)
            {
                _clipboardAction.Invoke(clipboardText);
            }

            _lastUpdateTime = now;
            _lastText = clipboardText;
        }

        public void Dispose()
        {
            _clipboardMonitor?.Dispose();
        }
    }
}