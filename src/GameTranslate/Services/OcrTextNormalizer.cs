namespace GameTranslate.Services;

public static class OcrTextNormalizer
{
    public static string NormalizeLines(IEnumerable<string> lines)
    {
        return string.Join(
            Environment.NewLine,
            lines
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line)));
    }
}

