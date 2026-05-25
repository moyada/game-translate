using GameTranslate.Models;

namespace GameTranslate.Services;

public static class PreviewSelectionMapper
{
    public static CaptureSelection CreateSelection(
        CaptureRegion screenRegion,
        int imagePixelWidth,
        int imagePixelHeight,
        double viewportWidth,
        double viewportHeight,
        CaptureRegion dragRegion)
    {
        if (screenRegion.IsEmpty
            || imagePixelWidth <= 0
            || imagePixelHeight <= 0
            || viewportWidth <= 0
            || viewportHeight <= 0)
        {
            return new CaptureSelection(default, default, 1, 1);
        }

        var imageScale = Math.Min(viewportWidth / imagePixelWidth, viewportHeight / imagePixelHeight);
        var displayedWidth = imagePixelWidth * imageScale;
        var displayedHeight = imagePixelHeight * imageScale;
        var offsetX = (viewportWidth - displayedWidth) / 2;
        var offsetY = (viewportHeight - displayedHeight) / 2;
        var displayedRegion = new CaptureRegion(offsetX, offsetY, displayedWidth, displayedHeight);
        var normalizedDrag = CaptureRegion.Normalize(dragRegion.X, dragRegion.Y, dragRegion.Width, dragRegion.Height);
        var clippedDrag = Intersect(normalizedDrag, displayedRegion);

        if (clippedDrag.IsEmpty)
        {
            return new CaptureSelection(default, default, 1, 1);
        }

        var imageX = (clippedDrag.X - offsetX) / imageScale;
        var imageY = (clippedDrag.Y - offsetY) / imageScale;
        var imageWidth = clippedDrag.Width / imageScale;
        var imageHeight = clippedDrag.Height / imageScale;
        var screenScaleX = screenRegion.Width / imagePixelWidth;
        var screenScaleY = screenRegion.Height / imagePixelHeight;
        var pixelRegion = new CaptureRegion(
            screenRegion.X + imageX * screenScaleX,
            screenRegion.Y + imageY * screenScaleY,
            imageWidth * screenScaleX,
            imageHeight * screenScaleY);

        return new CaptureSelection(clippedDrag, pixelRegion, screenScaleX / imageScale, screenScaleY / imageScale);
    }

    private static CaptureRegion Intersect(CaptureRegion first, CaptureRegion second)
    {
        var x = Math.Max(first.X, second.X);
        var y = Math.Max(first.Y, second.Y);
        var right = Math.Min(first.X + first.Width, second.X + second.Width);
        var bottom = Math.Min(first.Y + first.Height, second.Y + second.Height);
        return new CaptureRegion(x, y, Math.Max(0, right - x), Math.Max(0, bottom - y));
    }
}
