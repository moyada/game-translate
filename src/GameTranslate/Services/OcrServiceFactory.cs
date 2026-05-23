namespace GameTranslate.Services;

public static class OcrServiceFactory
{
    public static IOcrService Create(OcrBackend backend)
    {
        return backend switch
        {
            OcrBackend.Windows => new WindowsOcrService(),
            OcrBackend.PaddleSharp => new PaddleSharpOcrService(),
            _ => throw new ArgumentOutOfRangeException(nameof(backend), backend, "不支持的 OCR 后端。")
        };
    }
}
