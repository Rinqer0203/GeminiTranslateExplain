using GeminiTranslateExplain.Models;
using GeminiTranslateExplain.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            WindowUtilities.ApplyTitleBarTheme(this);

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

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void QuestionTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateQuestionTextBoxHeight(sender as System.Windows.Controls.TextBox);
        }

        private void QuestionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateQuestionTextBoxHeight(sender as System.Windows.Controls.TextBox);
        }

        private static void UpdateQuestionTextBoxHeight(System.Windows.Controls.TextBox? textBox)
        {
            if (textBox == null)
                return;

            // 行数に応じて高さを調整して、表示エリアを最大限確保する
            int lineCount = Math.Max(1, textBox.LineCount);
            double lineHeight = textBox.FontSize * 1.4;
            double verticalPadding = textBox.Padding.Top + textBox.Padding.Bottom;
            double desiredHeight = lineHeight * lineCount + verticalPadding + 6;

            if (textBox.MinHeight > 0)
                desiredHeight = Math.Max(desiredHeight, textBox.MinHeight);
            if (textBox.MaxHeight > 0)
                desiredHeight = Math.Min(desiredHeight, textBox.MaxHeight);

            textBox.Height = desiredHeight;
        }
    }
}

