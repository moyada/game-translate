namespace GameTranslate.Services;

public readonly record struct PreviewContentPoint(double X, double Y);

public readonly record struct PreviewTransformState(double Zoom, double PanX, double PanY);

public static class PreviewSelectionTransform
{
    private const double ZoomStep = 1.15;

    public static PreviewContentPoint ToContentPoint(
        double viewportX,
        double viewportY,
        double zoom,
        double panX,
        double panY)
    {
        if (zoom <= 0)
        {
            return new PreviewContentPoint(viewportX, viewportY);
        }

        return new PreviewContentPoint(
            (viewportX - panX) / zoom,
            (viewportY - panY) / zoom);
    }

    public static PreviewTransformState ZoomAround(
        double anchorX,
        double anchorY,
        double currentZoom,
        double currentPanX,
        double currentPanY,
        int wheelDelta)
    {
        var factor = wheelDelta > 0 ? ZoomStep : 1 / ZoomStep;
        var nextZoom = Math.Clamp(
            currentZoom * factor,
            MainWindowSelectionMode.MinimumPreviewZoom,
            MainWindowSelectionMode.MaximumPreviewZoom);

        if (Math.Abs(nextZoom - MainWindowSelectionMode.MinimumPreviewZoom) < 0.001)
        {
            return new PreviewTransformState(MainWindowSelectionMode.MinimumPreviewZoom, 0, 0);
        }

        var anchorContentPoint = ToContentPoint(anchorX, anchorY, currentZoom, currentPanX, currentPanY);
        return new PreviewTransformState(
            nextZoom,
            anchorX - anchorContentPoint.X * nextZoom,
            anchorY - anchorContentPoint.Y * nextZoom);
    }
}
