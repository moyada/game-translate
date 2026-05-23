namespace GameTranslate.Services;

public sealed record OcrBackendChoice(
    OcrBackend Backend,
    string DisplayName,
    string Description,
    bool UsesColorPreprocessing,
    string PreviewLabel);
