using System.Runtime.InteropServices;
using GameTranslate.Models;
using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Local;
using CvSize = OpenCvSharp.Size;

namespace GameTranslate.Services;

public sealed class PaddleSharpOcrService : IOcrService, IDisposable
{
    public const double InputScaleFactor = 3.0;

    private readonly Lazy<PaddleOcrAll> _ocr = new(CreateOcr);
    private bool _disposed;

    public Task<string> RecognizeTextAsync(CapturedFrame frame, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PaddleSharpOcrService));
        }

        if (frame.Width <= 0 || frame.Height <= 0 || frame.BgraPixels.Length == 0)
        {
            return Task.FromResult(string.Empty);
        }

        return Task.Run(() => RecognizeText(frame, cancellationToken), cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_ocr.IsValueCreated)
        {
            _ocr.Value.Dispose();
        }

        _disposed = true;
    }

    private static PaddleOcrAll CreateOcr()
    {
        return new PaddleOcrAll(LocalFullModels.EnglishV3, PaddleDevice.Mkldnn())
        {
            AllowRotateDetection = false,
            Enable180Classification = false
        };
    }

    private string RecognizeText(CapturedFrame frame, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var bgra = CreateBgraMat(frame);
        using var bgr = new Mat();
        using var enlarged = new Mat();
        Cv2.CvtColor(bgra, bgr, ColorConversionCodes.BGRA2BGR);
        Cv2.Resize(bgr, enlarged, CvSize.Zero, InputScaleFactor, InputScaleFactor, InterpolationFlags.Cubic);

        cancellationToken.ThrowIfCancellationRequested();
        var result = _ocr.Value.Run(enlarged);
        return OcrTextNormalizer.NormalizeLines(SplitLines(result.Text));
    }

    private static Mat CreateBgraMat(CapturedFrame frame)
    {
        var mat = new Mat(frame.Height, frame.Width, MatType.CV_8UC4);
        var rowBytes = frame.Width * 4;

        for (var y = 0; y < frame.Height; y++)
        {
            var sourceOffset = y * frame.Stride;
            var targetOffset = y * rowBytes;
            Marshal.Copy(frame.BgraPixels, sourceOffset, IntPtr.Add(mat.Data, targetOffset), rowBytes);
        }

        return mat;
    }

    private static IEnumerable<string> SplitLines(string text)
    {
        return text.Split(["\r\n", "\n"], StringSplitOptions.None);
    }
}
