using GameTranslate.Models;

namespace GameTranslate.Services;

public interface IScreenCaptureService
{
    CapturedFrame Capture(CaptureRegion region);
}

