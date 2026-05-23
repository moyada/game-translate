using GameTranslate.Models;

namespace GameTranslate.Services;

public static class CoordinateScaler
{
    public static CaptureRegion Scale(CaptureRegion region, double scaleX, double scaleY)
    {
        if (region.IsEmpty)
        {
            return region;
        }

        return new CaptureRegion(
            region.X * scaleX,
            region.Y * scaleY,
            region.Width * scaleX,
            region.Height * scaleY);
    }
}

