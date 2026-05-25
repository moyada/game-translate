using System.Runtime.InteropServices;
using GameTranslate.Models;
using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR.Models.Local;

namespace GameTranslate.Services;

public sealed class PaddleSharpOcrService : IOcrService, IDisposable
{
    public const string PreviewLabel = "PaddleOCR 输入预览";

    public const string RecognitionModelName = "en_PP-OCRv5_mobile_rec";

    public const bool UsesImagePreprocessing = false;

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
        return new PaddleOcrAll(CreateEnglishV5Model(), PaddleDevice.Mkldnn())
        {
            AllowRotateDetection = false,
            Enable180Classification = false
        };
    }

    private string RecognizeText(CapturedFrame frame, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var bgr = CreateBgrMat(frame);

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var result = GetOrCreateOcr().Run(bgr);
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

    private static FullOcrModel CreateEnglishV5Model()
    {
        return new FullOcrModel(
            LocalDetectionModel.ChineseV5,
            new LocalRecognizationModel(RecognitionModelName, string.Empty, ModelVersion.V5));
    }

    private static Mat CreateBgrMat(CapturedFrame frame)
    {
        var mat = new Mat(frame.Height, frame.Width, MatType.CV_8UC3);
        var rowBytes = frame.Width * 3;
        var pixels = new byte[frame.Height * rowBytes];

        for (var y = 0; y < frame.Height; y++)
        {
            var sourceOffset = y * frame.Stride;
            var targetOffset = y * rowBytes;
            for (var x = 0; x < frame.Width; x++)
            {
                var sourcePixel = sourceOffset + x * 4;
                var targetPixel = targetOffset + x * 3;
                pixels[targetPixel] = frame.BgraPixels[sourcePixel];
                pixels[targetPixel + 1] = frame.BgraPixels[sourcePixel + 1];
                pixels[targetPixel + 2] = frame.BgraPixels[sourcePixel + 2];
            }
        }

        Marshal.Copy(pixels, 0, mat.Data, pixels.Length);
        return mat;
    }

    private static IEnumerable<string> SplitLines(string text)
    {
        return text.Split(["\r\n", "\n"], StringSplitOptions.None);
    }
}
