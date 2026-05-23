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

    public const bool UsesLightContrastEnhancement = true;

    public const double ContrastClipLimit = 3.0;

    public const double SharpenAmount = 0.55;

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
        using var enhanced = new Mat();
        Cv2.CvtColor(bgra, bgr, ColorConversionCodes.BGRA2BGR);
        Cv2.Resize(bgr, enlarged, new CvSize(), InputScaleFactor, InputScaleFactor, InterpolationFlags.Cubic);
        EnhanceGameChatText(enlarged, enhanced);

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var result = GetOrCreateOcr().Run(enhanced);
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

    private static void EnhanceGameChatText(Mat input, Mat output)
    {
        using var lab = new Mat();
        using var enhancedLab = new Mat();
        using var sharpened = new Mat();
        using var blurred = new Mat();
        Cv2.CvtColor(input, lab, ColorConversionCodes.BGR2Lab);

        var channels = Cv2.Split(lab);
        try
        {
            using var clahe = Cv2.CreateCLAHE(clipLimit: ContrastClipLimit, tileGridSize: new CvSize(8, 8));
            clahe.Apply(channels[0], channels[0]);
            Cv2.Merge(channels, enhancedLab);
            Cv2.CvtColor(enhancedLab, sharpened, ColorConversionCodes.Lab2BGR);
        }
        finally
        {
            foreach (var channel in channels)
            {
                channel.Dispose();
            }
        }

        Cv2.GaussianBlur(sharpened, blurred, new CvSize(0, 0), 1.0);
        Cv2.AddWeighted(sharpened, 1.0 + SharpenAmount, blurred, -SharpenAmount, 0, output);
    }

    private static IEnumerable<string> SplitLines(string text)
    {
        return text.Split(["\r\n", "\n"], StringSplitOptions.None);
    }
}
