namespace GameTranslate.Models;

public sealed record CaptureSelection(
    CaptureRegion DisplayRegion,
    CaptureRegion PixelRegion,
    double ScaleX,
    double ScaleY)
{
    public bool IsEmpty => DisplayRegion.IsEmpty || PixelRegion.IsEmpty;

    public string ToDisplayText()
    {
        if (IsEmpty)
        {
            return "未选择区域";
        }

        return $"{PixelRegion.ToDisplayText()} · 缩放 {ScaleX * 100:0}% x {ScaleY * 100:0}%";
    }
}

