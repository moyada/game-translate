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

        var normalizedText = sourceText.Trim();
        var builder = new StringBuilder();
        builder.AppendLine("<|im_start|>system");
        builder.AppendLine("/no_think");
        builder.AppendLine("You are a game chat translation engine.");
        builder.AppendLine("Translate English into natural Simplified Chinese.");
        builder.AppendLine("Only output the Chinese translation.");
        builder.AppendLine("Do not explain. Do not add notes. Do not repeat the English source.");
        builder.AppendLine("Keep player names, numbers, commands, item names, skill names, and game terms unchanged when appropriate.");
        builder.AppendLine("<|im_end|>");
        builder.AppendLine("<|im_start|>user");
        builder.AppendLine(normalizedText);
        builder.AppendLine("<|im_end|>");
        builder.Append("<|im_start|>assistant");
        builder.AppendLine();
        return builder.ToString();
    }
}
