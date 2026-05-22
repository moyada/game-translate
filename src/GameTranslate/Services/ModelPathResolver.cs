namespace GameTranslate.Services;

public static class ModelPathResolver
{
    public const string ModelDirectoryName = "Models";
    public const string ModelFileName = "Qwen3-1.7B-UD-Q4_K_XL.gguf";

    public static string GetDefaultModelPath(string appBaseDirectory)
    {
        var modelDirectory = GetDefaultModelDirectory(appBaseDirectory);
        if (Directory.Exists(modelDirectory))
        {
            var firstModel = Directory
                .EnumerateFiles(modelDirectory, "*.gguf", SearchOption.TopDirectoryOnly)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstModel))
            {
                return firstModel;
            }
        }

        return Path.Combine(modelDirectory, ModelFileName);
    }

    public static string GetDefaultModelDirectory(string appBaseDirectory)
    {
        return Path.Combine(appBaseDirectory, ModelDirectoryName);
    }
}
