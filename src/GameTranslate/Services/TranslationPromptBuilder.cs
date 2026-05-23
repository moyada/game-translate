using System.Text;

namespace GameTranslate.Services;

public static class TranslationPromptBuilder
{
    public static string BuildEnglishToChinesePrompt(string sourceText)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return string.Empty;
        }

        var normalizedText = TranslationSourceNormalizer.ExtractTranslatableText(sourceText);
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return string.Empty;
        }

        var glossaryTerms = TranslationGlossary.LoadDefault().FindRelevantTerms(normalizedText, maxTerms: 24);
        var builder = new StringBuilder();
        builder.AppendLine("<|im_start|>system");
        builder.AppendLine("/no_think");
        builder.AppendLine("You are a MapleStory game chat translation engine for 冒险岛.");
        builder.AppendLine("Translate English game chat into concise, natural Simplified Chinese.");
        builder.AppendLine("Only output the Chinese translation.");
        builder.AppendLine("Do not explain. Do not add notes. Do not repeat the English source.");
        builder.AppendLine("If OCR text contains a player name before a colon, semicolon, single separator space, or @mention prefix, ignore the name and translate only the message.");
        builder.AppendLine("If the input has multiple lines, output the same number of translated lines in the same order.");
        builder.AppendLine("Preserve numbers, channel names, short commands, and player names when they appear inside the message body.");
        builder.AppendLine("Use official or common MapleStory Chinese terms when a glossary term applies.");
        if (glossaryTerms.Count > 0)
        {
            builder.AppendLine("Relevant glossary, English => Chinese:");
            foreach (var term in glossaryTerms)
            {
                builder.AppendLine($"- {term.English} => {term.Chinese}");
            }
        }

        builder.AppendLine("<|im_end|>");
        builder.AppendLine("<|im_start|>user");
        builder.AppendLine(normalizedText);
        builder.AppendLine("<|im_end|>");
        builder.Append("<|im_start|>assistant");
        builder.AppendLine();
        return builder.ToString();
    }
}
