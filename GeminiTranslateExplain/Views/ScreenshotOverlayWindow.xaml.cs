using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GeminiTranslateExplain.Services;
using System.Windows.Shapes;

namespace GeminiTranslateExplain.Views
{
    /// <summary>
    /// ScreenshotOverlayWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ScreenshotOverlayWindow : Window
    {
        private readonly bool _stealthMode;
        private System.Windows.Point _startPoint;
        private bool _dragging;
        private TaskCompletionSource<Rect?>? _tcs;

        public ScreenshotOverlayWindow()
        {
            InitializeComponent();
            _stealthMode = Models.AppConfig.Instance.ScreenshotStealthMode;

            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
            if (_stealthMode)
            {
                Background = System.Windows.Media.Brushes.Black;
                Opacity = 0.01;
                InstructionText.Visibility = Visibility.Collapsed;
            }
            else
            {
                Cursor = System.Windows.Input.Cursors.Cross;
            }

            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            KeyDown += OnKeyDown;
        }

        public Task<Rect?> CaptureAsync()
        {
            _tcs = new TaskCompletionSource<Rect?>();
            if (!_stealthMode)
            {
                InstructionText.Visibility = Visibility.Visible;
                PositionInstructionTextOnActiveScreen();
            }
            Show();
            ShowActivated = true;
            Activate();
            WindowUtilities.ForceActive(this);
            Focus();
            return _tcs.Task;
        }

        private void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(this);
            _dragging = true;
            if (!_stealthMode)
            {
                SelectionRect.Visibility = Visibility.Visible;
            }
            CaptureMouse();
        }

        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_dragging)
                return;

            var current = e.GetPosition(this);
            if (!_stealthMode)
            {
                var rect = CalculateRect(current);
                Canvas.SetLeft(SelectionRect, rect.X);
                Canvas.SetTop(SelectionRect, rect.Y);
                SelectionRect.Width = rect.Width;
                SelectionRect.Height = rect.Height;
            }
        }

        private void OnMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_dragging)
                return;

            _dragging = false;
            ReleaseMouseCapture();

            var rect = CalculateRect(e.GetPosition(this));

            CloseWithResult(rect.Width < 2 || rect.Height < 2 ? null : rect);
        }

        private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                CloseWithResult(null);
            }
        }

        private void CloseWithResult(Rect? rect)
        {
            SelectionRect.Visibility = Visibility.Collapsed;
            Hide();
            _tcs?.TrySetResult(rect);
        }

        private Rect CalculateRect(System.Windows.Point currentPoint)
        {
            var x = Math.Min(_startPoint.X, currentPoint.X);
            var y = Math.Min(_startPoint.Y, currentPoint.Y);
            var width = Math.Abs(_startPoint.X - currentPoint.X);
            var height = Math.Abs(_startPoint.Y - currentPoint.Y);
            return new Rect(x, y, width, height);
        }

        public void CancelCapture()
        {
            CloseWithResult(null);
        }

        private void PositionInstructionTextOnActiveScreen()
        {
            try
            {
                var cursor = System.Windows.Forms.Cursor.Position;
                var screen = System.Windows.Forms.Screen.FromPoint(cursor);
                var offsetX = screen.Bounds.Left - (int)SystemParameters.VirtualScreenLeft;
                var offsetY = screen.Bounds.Top - (int)SystemParameters.VirtualScreenTop;
                Canvas.SetLeft(InstructionText, offsetX + 16);
                Canvas.SetTop(InstructionText, offsetY + 16);
            }
            catch
            {
                Canvas.SetLeft(InstructionText, 16);
                Canvas.SetTop(InstructionText, 16);
            }
        }
    }
}
