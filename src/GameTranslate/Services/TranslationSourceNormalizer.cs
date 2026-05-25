using System.Text.RegularExpressions;

namespace GameTranslate.Services;

public static partial class TranslationSourceNormalizer
{
    public static TranslationSourceContext Parse(string sourceText)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return new TranslationSourceContext(string.Empty, []);
        }

        var lines = sourceText
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(ParseLine)
            .Where(context => !string.IsNullOrWhiteSpace(context.TranslatableText))
            .ToArray();

        if (lines.Length == 0)
        {
            return new TranslationSourceContext(string.Empty, []);
        }

        var text = string.Join(Environment.NewLine, lines.Select(line => line.TranslatableText));
        var prefixes = lines.Select(line => line.SpeakerPrefix).ToArray();
        return new TranslationSourceContext(text, prefixes);
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
        var translatedLines = translatedText
            .Trim()
            .Split(["\r\n", "\n"], StringSplitOptions.None);

        if (context.SpeakerPrefixes.Length == 0
            || context.SpeakerPrefixes.All(string.IsNullOrWhiteSpace)
            || context.SpeakerPrefixes.Length != translatedLines.Length)
        {
            return translatedText.Trim();
        }

        var restoredLines = translatedLines
            .Select((line, index) => string.IsNullOrWhiteSpace(context.SpeakerPrefixes[index])
                ? line.Trim()
                : $"{context.SpeakerPrefixes[index]}：{line.Trim()}");
        return string.Join(Environment.NewLine, restoredLines);
    }

    private static TranslationSourceLineContext ParseLine(string line)
    {
        var trimmed = line.Trim();
        var explicitMatch = ExplicitSpeakerPrefixRegex().Match(trimmed);
        if (explicitMatch.Success)
        {
            return TranslationSourceLineContext.WithSpeaker(
                explicitMatch.Groups["message"].Value.Trim(),
                explicitMatch.Groups["speaker"].Value.Trim());
        }

        var misreadColonMatch = MisreadColonSpeakerPrefixRegex().Match(trimmed);
        if (misreadColonMatch.Success)
        {
            return TranslationSourceLineContext.WithSpeaker(
                misreadColonMatch.Groups["message"].Value.Trim(),
                misreadColonMatch.Groups["speaker"].Value.Trim());
        }

        return new TranslationSourceLineContext(trimmed, null);
    }

    [GeneratedRegex(@"^\s*(?<speaker>@?[A-Za-z0-9][A-Za-z0-9_\-.]{1,31})\s*[:：]\s*(?<message>.+)$")]
    private static partial Regex ExplicitSpeakerPrefixRegex();

    [GeneratedRegex(@"^\s*(?<speaker>@?[A-Za-z0-9][A-Za-z0-9_\-.]{1,31})\s*[·;]\s*(?<message>.+)$")]
    private static partial Regex MisreadColonSpeakerPrefixRegex();
}

public sealed record TranslationSourceContext(string TranslatableText, string?[] SpeakerPrefixes)
{
    public string? SpeakerPrefix => SpeakerPrefixes.Length == 1 ? SpeakerPrefixes[0] : null;
}

internal sealed record TranslationSourceLineContext(string TranslatableText, string? SpeakerPrefix)
{
    public static TranslationSourceLineContext WithSpeaker(string translatableText, string speakerPrefix)
    {
        return new TranslationSourceLineContext(translatableText, speakerPrefix);
    }
}
