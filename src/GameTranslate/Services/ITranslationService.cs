namespace GameTranslate.Services;

public interface ITranslationService
{
    bool IsLoaded { get; }

    Task LoadAsync(TranslationOptions options, CancellationToken cancellationToken = default);

    Task<string> TranslateToChineseAsync(string text, CancellationToken cancellationToken = default);
}

