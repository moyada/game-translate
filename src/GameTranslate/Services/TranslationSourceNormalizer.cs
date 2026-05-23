using System.Text.RegularExpressions;

namespace GameTranslate.Services;

public static partial class TranslationSourceNormalizer
{
    public static string ExtractTranslatableText(string sourceText)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return string.Empty;
        }

        var lines = sourceText
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(ExtractLine)
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join(Environment.NewLine, lines);
    }

    private static string ExtractLine(string line)
    {
        var trimmed = line.Trim();
        var match = SpeakerPrefixRegex().Match(trimmed);
        if (!match.Success)
        {
            return trimmed;
        }

        return match.Groups["message"].Value.Trim();
    }

    [GeneratedRegex(@"^\s*(?<speaker>[A-Za-z0-9][A-Za-z0-9_\- .\[\]]{0,31})\s*[:：]\s*(?<message>.+)$")]
    private static partial Regex SpeakerPrefixRegex();
}
