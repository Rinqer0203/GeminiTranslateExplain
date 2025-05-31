using System.Windows;

namespace GeminiTranslateExplain
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                // ウィンドウのサイズを設定
                var size = AppConfig.Instance.MainWindowSize;
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
                        AppConfig.Instance.MainWindowSize = new WindowSize(this.Width, this.Height);
                    }
                };
            };
        }
    }
}