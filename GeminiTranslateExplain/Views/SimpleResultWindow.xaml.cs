using GeminiTranslateExplain.Models;
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
                var size = AppConfig.Instance.SimpleResultWindowSize;
                if (size.Width > 0 && size.Height > 0)
                {
                    this.Width = size.Width;
                    this.Height = size.Height;
                }

                this.SizeChanged += (s, e) =>
                {
                    // ウィンドウのサイズを保存
                    if (this.WindowState == WindowState.Normal)
                    {
                        AppConfig.Instance.SimpleResultWindowSize = new WindowSize(this.Width, this.Height);
                    }
                };
            };

            // 非アクティブ時にウィンドウを閉じる
            this.Deactivated += (s, e) =>
            {
                this.Hide();
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
