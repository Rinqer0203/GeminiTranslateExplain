using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace GeminiTranslateExplain.Services
{
    public static class ScreenCaptureService
    {
        public static byte[] CapturePngBytes(Rect rect)
        {
            // rectはスクリーン座標（物理解像度px）を想定
            var left = (int)Math.Round(rect.X);
            var top = (int)Math.Round(rect.Y);
            var width = Math.Max(1, (int)Math.Round(rect.Width));
            var height = Math.Max(1, (int)Math.Round(rect.Height));

            using var bitmap = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(width, height));
            }

            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
    }
}
