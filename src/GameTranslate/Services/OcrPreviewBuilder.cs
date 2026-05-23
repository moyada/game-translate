using System.Windows.Media;
using System.Windows.Media.Imaging;
using GameTranslate.Models;

namespace GameTranslate.Services;

public static class OcrPreviewBuilder
{
    public static BitmapSource CreatePreview(CapturedFrame frame)
    {
        var pixels = OcrImagePreprocessor.CreateHighContrastBgra(
            frame.BgraPixels,
            frame.Width,
            frame.Height,
            frame.Stride);
        var stride = OcrImagePreprocessor.GetOutputStride(frame.Width);
        var preview = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        preview.Freeze();
        return preview;
    }
}
