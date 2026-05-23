using GameTranslate.Models;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Security.Cryptography;

namespace GameTranslate.Services;

public sealed class WindowsOcrService : IOcrService
{
    private readonly OcrEngine _engine;

    public WindowsOcrService()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10))
        {
            throw new PlatformNotSupportedException("OCR 只支持 Windows 10 / Windows 11。");
        }

        _engine = OcrEngine.TryCreateFromLanguage(new Language("en-US"))
            ?? OcrEngine.TryCreateFromUserProfileLanguages()
            ?? throw new InvalidOperationException("无法创建 Windows OCR 引擎，请确认系统 OCR 语言组件可用。");
    }

    public async Task<string> RecognizeTextAsync(CapturedFrame frame, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (frame.Width <= 0 || frame.Height <= 0 || frame.BgraPixels.Length == 0)
        {
            return string.Empty;
        }

        if (frame.Width > OcrEngine.MaxImageDimension || frame.Height > OcrEngine.MaxImageDimension)
        {
            throw new InvalidOperationException($"截图区域过大，Windows OCR 单边最大支持 {OcrEngine.MaxImageDimension} 像素。");
        }

        var preprocessedPixels = OcrImagePreprocessor.CreateHighContrastBgra(
            frame.BgraPixels,
            frame.Width,
            frame.Height,
            frame.Stride);
        var buffer = CryptographicBuffer.CreateFromByteArray(preprocessedPixels);
        using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(
            buffer,
            BitmapPixelFormat.Bgra8,
            frame.Width,
            frame.Height,
            BitmapAlphaMode.Ignore);

        var result = await _engine.RecognizeAsync(bitmap).AsTask(cancellationToken);
        return OcrTextNormalizer.NormalizeLines(result.Lines.Select(line => line.Text));
    }
}
