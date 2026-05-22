namespace GameTranslate.Models;

public readonly record struct CaptureRegion(double X, double Y, double Width, double Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public string ToDisplayText()
    {
        if (IsEmpty)
        {
            return "未选择区域";
        }

        return $"X={X:0}, Y={Y:0}, W={Width:0}, H={Height:0}";
    }

    public static CaptureRegion Normalize(double x, double y, double width, double height)
    {
        if (width < 0)
        {
            x += width;
            width = Math.Abs(width);
        }

        if (height < 0)
        {
            y += height;
            height = Math.Abs(height);
        }

        return new CaptureRegion(x, y, width, height);
    }

    public CaptureRegion Clamp(double minX, double minY, double maxWidth, double maxHeight)
    {
        var x = Math.Clamp(X, minX, minX + maxWidth);
        var y = Math.Clamp(Y, minY, minY + maxHeight);
        var width = Math.Clamp(Width, 0, minX + maxWidth - x);
        var height = Math.Clamp(Height, 0, minY + maxHeight - y);
        return new CaptureRegion(x, y, width, height);
    }
}

