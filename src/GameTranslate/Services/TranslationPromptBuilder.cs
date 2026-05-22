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

        var builder = new StringBuilder();
        builder.AppendLine("/no_think");
        builder.AppendLine("Translate the following English game chat text into natural Simplified Chinese.");
        builder.AppendLine("Only output the Chinese translation.");
        builder.AppendLine("Do not explain.");
        builder.AppendLine("Keep player names, numbers, commands, item names, skill names, and game terms unchanged when appropriate.");
        builder.AppendLine();
        builder.AppendLine("Text:");
        builder.AppendLine(sourceText.Trim());
        return builder.ToString();
    }
}

