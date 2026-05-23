namespace GameTranslate.Services;

public static class OcrBackendCatalog
{
    public static OcrBackend DefaultBackend => OcrBackend.Windows;

    public static IReadOnlyList<OcrBackendChoice> Choices { get; } =
    [
        new(OcrBackend.Windows, "Windows OCR", "系统自带，轻量；适合先验证截图和预处理。"),
        new(OcrBackend.PaddleSharp, "PaddleOCR", "本地 PaddleSharp 后端，适合游戏彩色小字对照测试。")
    ];

    public static OcrBackendChoice DefaultChoice => Choices.First(choice => choice.Backend == DefaultBackend);
}
