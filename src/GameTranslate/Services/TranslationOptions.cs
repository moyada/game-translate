namespace GameTranslate.Services;

public sealed record TranslationOptions
{
    public required string ModelPath { get; init; }

    public uint ContextSize { get; init; } = 2048;

    public int GpuLayerCount { get; init; } = 999;

    public uint BatchSize { get; init; } = 256;

    public int MaxTokens { get; init; } = 512;

    public static TranslationOptions CreateDefault(string modelPath)
    {
        return new TranslationOptions
        {
            ModelPath = modelPath
        };
    }
}

