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
        builder.AppendLine("将以下游戏聊天文本翻译为中文。");
        builder.AppendLine();
        builder.AppendLine("# 任务目标");
        builder.AppendLine("只翻译游戏聊天中的英文消息内容，输出中文译文。");
        builder.AppendLine();
        builder.AppendLine("# 严格约束");
        builder.AppendLine("1. 只输出翻译后的结果，不要解释，不要添加注释，不要输出任何额外内容。");
        builder.AppendLine("2. 如果输入中存在用户名聊天前缀，必须保留用户名原文，严禁翻译、改写或删除。");
        builder.AppendLine("3. 用户名前缀格式为：");
        builder.AppendLine("   {user} : {message}");
        builder.AppendLine("   {user}: {message}");
        builder.AppendLine("   {user}：{message}");
        builder.AppendLine("4. 对于用户名聊天前缀：");
        builder.AppendLine("   - 只翻译分隔符后面的 {message}");
        builder.AppendLine("   - {user} 必须逐字符原样保留");
        builder.AppendLine("   - 分隔符 : 或 ： 必须保留");
        builder.AppendLine("   - 分隔符左右空格必须尽量保持原样");
        builder.AppendLine("5. 如果一行开头看起来像用户名，但没有明确的 : 或 ： 分隔符，不要把它当作用户名，不要擅自添加冒号。");
        if (glossaryTerms.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("# 游戏术语参考，English => Chinese");
            foreach (var term in glossaryTerms)
            {
                builder.AppendLine($"- {term.English} => {term.Chinese}");
            }
        }

        builder.AppendLine("<|im_end|>");
        builder.AppendLine("<|im_start|>user");
        builder.AppendLine("# 待翻译文本");
        builder.AppendLine(normalizedText);
        builder.AppendLine("<|im_end|>");
        builder.Append("<|im_start|>assistant");
        builder.AppendLine();
        return builder.ToString();
    }
}
