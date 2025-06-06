using GeminiTranslateExplain.Models;
using System.Windows;
using System.Windows.Threading;

namespace GeminiTranslateExplain.Services
{
    public static class DebugManager
    {
        private static Window? _lastTargetWindow = null;

        private const int UpdateSpanMs = 10;

        public static void Initialize()
        {
            if (AppConfig.Instance.DebugWindowPosition)
            {
                var debugWindowTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(UpdateSpanMs)
                };
                debugWindowTimer.Tick += (sender, args) =>
                {
                    Window? targetWindow = null;
                    if (AppConfig.Instance.SelectedResultWindowType == WindowType.MainWindow)
                        targetWindow = WindowManager.GetView<MainWindow>();
                    else if (AppConfig.Instance.SelectedResultWindowType == WindowType.SimpleResultWindow)
                        targetWindow = WindowManager.GetView<SimpleResultWindow>();

                    if (targetWindow == null)
                        return;

                    if (_lastTargetWindow != targetWindow)
                    {
                        targetWindow.Show();
                        targetWindow.Activate();
                        _lastTargetWindow = targetWindow;
                    }

                    WindowPositioner.SetWindowPosition(targetWindow);
                };
                debugWindowTimer.Start();
            }
        }
    }
}
