using System.Text.RegularExpressions;

namespace GameTranslate.Services;

public static partial class TranslationOutputCleaner
{
    public static string Clean(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cleaned = ThinkBlockRegex().Replace(value, string.Empty);
        return cleaned
            .Replace("<think>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("</think>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("<|im_end|>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("<|endoftext|>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    [GeneratedRegex("<think\\b[^>]*>.*?</think>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ThinkBlockRegex();
}
