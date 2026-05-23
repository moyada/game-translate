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
    public const string PreviewLabel = "PaddleOCR 输入预览";

    public const double InputScaleFactor = 3.0;

    public const bool RecreatesEngineAfterFailure = true;

    public const bool RecreatesEngineAfterRecognition = true;

    public const string FailureMessage = "PaddleOCR 识别失败，已重置 OCR 引擎。请再次点击翻译，或重新开始监控。";

    private readonly SemaphoreSlim _recognizeLock = new(1, 1);
    private readonly object _ocrSync = new();
    private PaddleOcrAll? _ocr;
    private bool _disposed;

    public async Task<string> RecognizeTextAsync(CapturedFrame frame, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PaddleSharpOcrService));
        }

        if (frame.Width <= 0 || frame.Height <= 0 || frame.BgraPixels.Length == 0)
        {
            return string.Empty;
        }

        await _recognizeLock.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() => RecognizeText(frame, cancellationToken), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ResetOcrEngine();
            throw new InvalidOperationException(FailureMessage, ex);
        }
        finally
        {
            _recognizeLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ResetOcrEngine();
        _recognizeLock.Dispose();
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
        Cv2.Resize(bgr, enlarged, new CvSize(), InputScaleFactor, InputScaleFactor, InterpolationFlags.Cubic);

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var result = GetOrCreateOcr().Run(enlarged);
            cancellationToken.ThrowIfCancellationRequested();
            return OcrTextNormalizer.NormalizeLines(SplitLines(result.Text));
        }
        finally
        {
            ResetOcrEngine();
        }
    }

    private PaddleOcrAll GetOrCreateOcr()
    {
        lock (_ocrSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _ocr ??= CreateOcr();
            return _ocr;
        }
    }

    private void ResetOcrEngine()
    {
        lock (_ocrSync)
        {
            _ocr?.Dispose();
            _ocr = null;
        }
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
