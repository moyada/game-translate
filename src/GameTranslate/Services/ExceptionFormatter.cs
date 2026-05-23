using System.IO;
using System.Text;

namespace GameTranslate.Services;

public static class ExceptionFormatter
{
    public static string Format(Exception exception)
    {
        var builder = new StringBuilder();
        var current = exception;
        var level = 0;

        while (current is not null)
        {
            if (level > 0)
            {
                builder.AppendLine();
                builder.AppendLine($"Inner exception {level}:");
            }

            builder.AppendLine($"{current.GetType().FullName}: {current.Message}");

            if (current is FileNotFoundException fileNotFoundException && !string.IsNullOrWhiteSpace(fileNotFoundException.FileName))
            {
                builder.AppendLine($"File: {fileNotFoundException.FileName}");
            }

            current = current.InnerException;
            level++;
        }

        return builder.ToString().Trim();
    }
}
