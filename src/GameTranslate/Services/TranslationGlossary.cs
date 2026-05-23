using System.IO;
using System.Text.Json;

namespace GameTranslate.Services;

public sealed class TranslationGlossary
{
    private const string RelativeGlossaryPath = "Resources/Glossary/maplestory-glossary.json";
    private static readonly Lazy<TranslationGlossary> DefaultGlossary = new(LoadDefaultCore);

    public List<TranslationGlossaryEntry> Entries { get; init; } = [];

    public static TranslationGlossary LoadDefault()
    {
        return DefaultGlossary.Value;
    }

    public IReadOnlyList<TranslationGlossaryEntry> FindRelevantTerms(string sourceText, int maxTerms)
    {
        if (string.IsNullOrWhiteSpace(sourceText) || maxTerms <= 0)
        {
            return [];
        }

        return Entries
            .Where(entry => entry.Matches(sourceText))
            .OrderByDescending(entry => entry.English.Length)
            .ThenBy(entry => entry.English, StringComparer.OrdinalIgnoreCase)
            .Take(maxTerms)
            .ToArray();
    }

    private static TranslationGlossary LoadDefaultCore()
    {
        var path = FindDefaultGlossaryPath();
        if (path is null)
        {
            return new TranslationGlossary();
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var data = File.ReadAllText(path);
        return JsonSerializer.Deserialize<TranslationGlossary>(data, options) ?? new TranslationGlossary();
    }

    private static string? FindDefaultGlossaryPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var outputPath = Path.Combine(baseDirectory, RelativeGlossaryPath);
        if (File.Exists(outputPath))
        {
            return outputPath;
        }

        var directory = new DirectoryInfo(baseDirectory);
        while (directory is not null)
        {
            var sourcePath = Path.Combine(directory.FullName, "src", "GameTranslate", RelativeGlossaryPath);
            if (File.Exists(sourcePath))
            {
                return sourcePath;
            }

            directory = directory.Parent;
        }

        return null;
    }
}

public sealed class TranslationGlossaryEntry
{
    public string Category { get; init; } = string.Empty;

    public string English { get; init; } = string.Empty;

    public string Chinese { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string[] Aliases { get; init; } = [];

    public bool Matches(string sourceText)
    {
        return ContainsTerm(sourceText, English) || Aliases.Any(alias => ContainsTerm(sourceText, alias));
    }

    private static bool ContainsTerm(string sourceText, string term)
    {
        return !string.IsNullOrWhiteSpace(term)
            && sourceText.Contains(term, StringComparison.OrdinalIgnoreCase);
    }
}
