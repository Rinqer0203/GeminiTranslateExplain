using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace GeminiTranslateExplain
{
    public partial class SimpleResultWindow : Window
    {
        public SimpleResultWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                // ウィンドウのサイズを設定
                var config = AppConfig.Instance.SimpleResultWindowSize;
                if (!double.IsNaN(config.Width)) this.Width = config.Width;
                if (!double.IsNaN(config.Height)) this.Height = config.Height;
            };

            this.SizeChanged += (s, e) =>
            {
                // ウィンドウのサイズを保存
                if (this.WindowState == WindowState.Normal)
                {
                    var config = AppConfig.Instance.SimpleResultWindowSize;
                    config.Width = e.NewSize.Width;
                    config.Height = e.NewSize.Height;
                }
            };
        }

        // ウィンドウ移動
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        // 「×」ボタンで非表示
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        // Win32 API による右下リサイズ
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTBOTTOMRIGHT = 17;

        private void ResizeGrip_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ReleaseCapture();
                SendMessage(new System.Windows.Interop.WindowInteropHelper(this).Handle, WM_NCLBUTTONDOWN, (IntPtr)HTBOTTOMRIGHT, IntPtr.Zero);
            }
        }
    }
}
