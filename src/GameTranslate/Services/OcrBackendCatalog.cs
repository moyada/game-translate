namespace GameTranslate.Services;

public static class OcrBackendCatalog
{
    public static OcrBackend DefaultBackend => OcrBackend.Windows;

    public static IReadOnlyList<OcrBackendChoice> Choices { get; } =
    [
        new(
            OcrBackend.Windows,
            "Windows OCR",
            "系统自带，轻量；适合先验证截图和预处理。",
            true,
            "OCR 预处理预览"),
        new(
            OcrBackend.PaddleSharp,
            "PaddleOCR",
            "本地 PaddleSharp 后端，使用原始彩色截图识别游戏小字。",
            false,
            "PaddleOCR 输入预览")
    ];

    public static OcrBackendChoice DefaultChoice => Choices.First(choice => choice.Backend == DefaultBackend);
}
