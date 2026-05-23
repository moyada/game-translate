using System.Text.RegularExpressions;

namespace GameTranslate.Services;

public static partial class TranslationSourceNormalizer
{
    public static TranslationSourceContext Parse(string sourceText)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return new TranslationSourceContext(string.Empty, null);
        }

        var lines = sourceText
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(ParseLine)
            .Where(context => !string.IsNullOrWhiteSpace(context.TranslatableText))
            .ToArray();

        if (lines.Length == 0)
        {
            return new TranslationSourceContext(string.Empty, null);
        }

        var text = string.Join(Environment.NewLine, lines.Select(line => line.TranslatableText));
        var prefix = lines.Length == 1 ? lines[0].SpeakerPrefix : null;
        return new TranslationSourceContext(text, prefix);
    }

    public static string ExtractTranslatableText(string sourceText)
    {
        return Parse(sourceText).TranslatableText;
    }

    public static string ApplySpeakerPrefix(string sourceText, string translatedText)
    {
        if (string.IsNullOrWhiteSpace(translatedText))
        {
            return string.Empty;
        }

        var context = Parse(sourceText);
        if (string.IsNullOrWhiteSpace(context.SpeakerPrefix))
        {
            return translatedText.Trim();
        }

        return $"{context.SpeakerPrefix}：{translatedText.Trim()}";
    }

    private static TranslationSourceContext ParseLine(string line)
    {
        var trimmed = line.Trim();
        var explicitMatch = ExplicitSpeakerPrefixRegex().Match(trimmed);
        if (explicitMatch.Success)
        {
            return new TranslationSourceContext(
                explicitMatch.Groups["message"].Value.Trim(),
                explicitMatch.Groups["speaker"].Value.Trim());
        }

        var spaceMatch = SpaceSpeakerPrefixRegex().Match(trimmed);
        if (spaceMatch.Success && IsLikelySpeakerPrefix(spaceMatch.Groups["speaker"].Value))
        {
            return new TranslationSourceContext(
                spaceMatch.Groups["message"].Value.Trim(),
                spaceMatch.Groups["speaker"].Value.Trim());
        }

        return new TranslationSourceContext(trimmed, null);
    }

    private static bool IsLikelySpeakerPrefix(string speaker)
    {
        if (speaker.StartsWith('@'))
        {
            return true;
        }

        return !CommonSentenceStarts.Contains(speaker);
    }

    private static readonly HashSet<string> CommonSentenceStarts = new(StringComparer.OrdinalIgnoreCase)
    {
        "Hello",
        "Hi",
        "Hey",
        "Go",
        "Kill",
        "Come",
        "Need",
        "Can",
        "Where",
        "When",
        "Why",
        "What",
        "Trade",
        "Buy",
        "Sell",
        "Party",
        "Boss",
        "Help",
        "Anyone",
        "Looking"
    };

    [GeneratedRegex(@"^\s*(?<speaker>@?[A-Za-z0-9][A-Za-z0-9_\-.]{1,31})\s*[:：;；]\s*(?<message>.+)$")]
    private static partial Regex ExplicitSpeakerPrefixRegex();

    [GeneratedRegex(@"^\s*(?<speaker>@[A-Za-z0-9][A-Za-z0-9_\-.]{1,31}|[A-Z][A-Za-z0-9_\-.]{2,31})\s(?<message>.+)$")]
    private static partial Regex SpaceSpeakerPrefixRegex();
}

public sealed record TranslationSourceContext(string TranslatableText, string? SpeakerPrefix);
