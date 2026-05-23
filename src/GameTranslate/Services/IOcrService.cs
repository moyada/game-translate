using GameTranslate.Models;

namespace GameTranslate.Services;

public interface IOcrService
{
    Task<string> RecognizeTextAsync(CapturedFrame frame, CancellationToken cancellationToken = default);
}

