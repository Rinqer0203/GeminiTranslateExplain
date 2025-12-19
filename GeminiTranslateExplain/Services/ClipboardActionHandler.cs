using GeminiTranslateExplain.Models;
using System.Runtime.InteropServices;
using System.Windows;
using Clipboard = System.Windows.Clipboard;

namespace GeminiTranslateExplain.Services
{
    public class ClipboardActionHandler : IDisposable
    {
        private readonly record struct ClipboardSnapshot(System.Windows.IDataObject? DataObject, string? Text, uint Sequence);

        private static readonly object ClipboardLock = new();

        private readonly ClipboardMonitor _clipboardMonitor;
        private readonly Action<string> _clipboardAction;

        private DateTime _lastUpdateTime = DateTime.MinValue;
        private string _lastText = string.Empty;
        private string _lastSetText = string.Empty;

        /// <summary>
        /// クリップボード更新のアクションを実行する間隔 (ミリ秒)
        /// </summary>
        private const int IntervalMaxMs = 500;
        private const int IntervalMinMs = 100;
        private const int RetryCount = 5;
        private const int RetryDelayMs = 10;

        [DllImport("user32.dll")]
        private static extern uint GetClipboardSequenceNumber();

        public ClipboardActionHandler(Window mainWindow, Action<string> clipBoardAction)
        {
            _clipboardAction = clipBoardAction;
            _clipboardMonitor = new ClipboardMonitor(mainWindow, OnClipboardUpdate);
        }

        public void SafeSetClipboardText(string text)
        {
            ExecuteWithRetry(() => Clipboard.SetText(text), "クリップボードへのテキスト設定に失敗しました。");
            _lastSetText = text;
            /// クリップボードの更新時間を記録
            /// クリップボードを更新すると、数msでonClipboardUpdateが呼ばれるため、
            /// _lastUpdateTimeを更新して更新間隔フィルターで弾く
            _lastUpdateTime = DateTime.Now;
        }

        public bool TryGetClipboardText(out string text)
        {
            text = string.Empty;
            try
            {
                if (SafeClipboardContainsText() == false)
                    return false;

                text = SafeGetClipboardText();
                return string.IsNullOrWhiteSpace(text) == false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public async Task<string?> TryGetSelectedTextAsync(int delayMs = 20, int retryCount = 20)
        {
            var snapshot = CaptureClipboardSnapshot();
            var beforeText = snapshot.Text ?? string.Empty;
            var beforeSequence = snapshot.Sequence;
            _lastUpdateTime = DateTime.Now;
            _lastText = string.Empty;

            string? foundText = null;
            try
            {
                KeyboardUtilities.SendCopyShortcut();
                KeyboardUtilities.TrySendCopyMessageToFocusedControl();
                KeyboardUtilities.SendCopyMessageToForeground();
                for (int i = 0; i < retryCount; i++)
                {
                    await Task.Delay(delayMs);
                    if (GetClipboardSequenceNumber() == beforeSequence)
                    {
                        continue;
                    }

                    if (TryGetClipboardText(out var text) == false)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    if (text != beforeText)
                    {
                        foundText = text;
                        break;
                    }
                }
            }
            catch
            {
                foundText = null;
            }

            await RestoreClipboardAsync(snapshot);
            return foundText;
        }

        private static bool SafeClipboardContainsText()
        {
            return ExecuteWithRetry(() => Clipboard.ContainsText(), "クリップボードのテキスト確認に失敗しました。");
        }

        private static string SafeGetClipboardText()
        {
            return ExecuteWithRetry(() => Clipboard.GetText(), "クリップボードからのテキスト取得に失敗しました。");
        }

        private ClipboardSnapshot CaptureClipboardSnapshot()
        {
            var dataObject = SafeCloneClipboardDataObject();
            var text = TryGetClipboardText(out var existingText) ? existingText : string.Empty;
            var sequence = GetClipboardSequenceNumber();
            return new ClipboardSnapshot(dataObject, text, sequence);
        }

        private static System.Windows.IDataObject? SafeCloneClipboardDataObject()
        {
            try
            {
                var source = ExecuteWithRetry(() => Clipboard.GetDataObject(), "クリップボードからのデータ取得に失敗しました。");
                if (source == null)
                    return null;

                var clone = new System.Windows.DataObject();
                foreach (var format in source.GetFormats())
                {
                    try
                    {
                        var data = source.GetData(format, true);
                        if (data != null)
                            clone.SetData(format, data);
                    }
                    catch
                    {
                        // 一部フォーマットは取得に失敗するため無視
                    }
                }

                return clone;
            }
            catch
            {
                return null;
            }
        }

        private async Task RestoreClipboardAsync(ClipboardSnapshot snapshot)
        {
            if (snapshot.DataObject == null && string.IsNullOrWhiteSpace(snapshot.Text))
                return;

            try
            {
                await Task.Delay(40);
                _lastUpdateTime = DateTime.Now;
                if (snapshot.DataObject != null)
                {
                    ExecuteWithRetry(() => Clipboard.SetDataObject(snapshot.DataObject, true), "クリップボードの復元に失敗しました。");
                }
                else if (!string.IsNullOrWhiteSpace(snapshot.Text))
                {
                    ExecuteWithRetry(() => Clipboard.SetText(snapshot.Text), "クリップボードの復元に失敗しました。");
                }

                if (snapshot.Text != null)
                    _lastSetText = snapshot.Text;
            }
            catch
            {
                // 復元失敗は無視
            }
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
            // 前回の更新からの時間が最小間隔未満、またはクリップボードにテキストがない場合は何もしない
            if (intervalMs < IntervalMinMs)
            {
                return;
            }
            if (TryGetClipboardText(out var clipboardText) == false)
                return;

            // クリップボードの内容が空、または前にクリップボードに設定したテキストと同じ場合は何もしない
            if (string.IsNullOrWhiteSpace(clipboardText) || clipboardText == _lastSetText)
                return;

            // 前回の更新からの時間が指定した間隔以内で、かつクリップボードの内容が前回と同じ場合はアクションを実行
            if (intervalMs <= IntervalMaxMs && clipboardText == _lastText)
            {
                if (AppConfig.Instance.DebugClipboardAction)
                    clipboardText += $" (Debug: interval {intervalMs}ms)";
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
