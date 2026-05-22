using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GameTranslate.Models;
using Drawing = System.Drawing;
using DrawingImaging = System.Drawing.Imaging;

namespace GameTranslate.Services;

public sealed class ScreenCaptureService : IScreenCaptureService
{
    public CapturedFrame Capture(CaptureRegion region)
    {
        if (region.IsEmpty)
        {
            throw new InvalidOperationException("请先选择截图区域。");
        }

        var x = (int)Math.Round(region.X);
        var y = (int)Math.Round(region.Y);
        var width = Math.Max(1, (int)Math.Round(region.Width));
        var height = Math.Max(1, (int)Math.Round(region.Height));

        using var bitmap = new Drawing.Bitmap(width, height, DrawingImaging.PixelFormat.Format32bppArgb);
        using (var graphics = Drawing.Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(x, y, 0, 0, new Drawing.Size(width, height), Drawing.CopyPixelOperation.SourceCopy);
        }

        var rectangle = new Drawing.Rectangle(0, 0, width, height);
        var data = bitmap.LockBits(rectangle, DrawingImaging.ImageLockMode.ReadOnly, DrawingImaging.PixelFormat.Format32bppArgb);
        try
        {
            var stride = Math.Abs(data.Stride);
            var pixels = new byte[stride * height];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            var preview = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
            preview.Freeze();
            return new CapturedFrame(width, height, stride, pixels, preview);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}
