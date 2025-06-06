using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GeminiTranslateExplain.Services
{
    public static class WindowPositioner
    {
        private const double OFFSET_IN_DIU = 30.0;

        public static void SetWindowPosition(Window? window)
        {
            if (window == null) return;

            var helper = new WindowInteropHelper(window);
            if (helper.Handle == IntPtr.Zero)
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                return;
            }

            if (!GetCursorPos(out POINT cursorPoint))
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                return;
            }

            var currentScreen = Screen.FromPoint(new System.Drawing.Point(cursorPoint.X, cursorPoint.Y));
            var workArea = currentScreen.WorkingArea;

            var source = PresentationSource.FromVisual(window);
            if (source?.CompositionTarget == null)
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                return;
            }
            var transform = source.CompositionTarget.TransformToDevice;
            double scaleX = transform.M11;
            double scaleY = transform.M22;

            double offsetXPixels = OFFSET_IN_DIU * scaleX;
            double offsetYPixels = OFFSET_IN_DIU * scaleY;

            double windowWidthPixels = window.Width * scaleX;
            double windowHeightPixels = window.Height * scaleY;

            double x, y;

            x = cursorPoint.X + offsetXPixels;
            y = cursorPoint.Y + offsetYPixels;
            if (IsWithin(x, y, windowWidthPixels, windowHeightPixels, workArea))
            {
                SetWindowPositionInDips(window, x, y, scaleX, scaleY);
                return;
            }

            x = cursorPoint.X - windowWidthPixels - offsetXPixels;
            y = cursorPoint.Y + offsetYPixels;
            if (IsWithin(x, y, windowWidthPixels, windowHeightPixels, workArea))
            {
                SetWindowPositionInDips(window, x, y, scaleX, scaleY);
                return;
            }

            x = cursorPoint.X + offsetXPixels;
            y = cursorPoint.Y - windowHeightPixels - offsetYPixels;
            if (IsWithin(x, y, windowWidthPixels, windowHeightPixels, workArea))
            {
                SetWindowPositionInDips(window, x, y, scaleX, scaleY);
                return;
            }

            x = cursorPoint.X - windowWidthPixels - offsetXPixels;
            y = cursorPoint.Y - windowHeightPixels - offsetYPixels;
            if (IsWithin(x, y, windowWidthPixels, windowHeightPixels, workArea))
            {
                SetWindowPositionInDips(window, x, y, scaleX, scaleY);
                return;
            }

            x = cursorPoint.X + offsetXPixels;
            y = cursorPoint.Y + offsetYPixels;
            SetWindowPositionInDips(window, x, y, scaleX, scaleY);
        }

        private static bool IsWithin(double x, double y, double width, double height, System.Drawing.Rectangle workArea)
        {
            return x >= workArea.Left &&
                   y >= workArea.Top &&
                   (x + width) <= workArea.Right &&
                   (y + height) <= workArea.Bottom;
        }

        private static void SetWindowPositionInDips(Window window, double pixelX, double pixelY, double scaleX, double scaleY)
        {
            window.Left = pixelX / scaleX;
            window.Top = pixelY / scaleY;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);
    }
}