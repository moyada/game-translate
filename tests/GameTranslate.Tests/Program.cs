using GameTranslate.Services;

var tests = new List<(string Name, Action Test)>
{
    ("default model fallback path", DefaultModelFallbackPath),
    ("default model chooses first gguf", DefaultModelChoosesFirstGguf),
    ("translation options", TranslationOptionsDefaults),
    ("prompt content", PromptContent),
    ("blank prompt", BlankPrompt)
};

var failures = 0;
foreach (var (name, test) in tests)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine($"FAIL {name}: {ex.Message}");
    }
}

return failures == 0 ? 0 : 1;

static void DefaultModelFallbackPath()
{
    var baseDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = ModelPathResolver.GetDefaultModelPath(baseDirectory);
    Assert(path.EndsWith(Path.Combine("Models", "Qwen3-1.7B-UD-Q4_K_XL.gguf"), StringComparison.Ordinal), path);
}

static void DefaultModelChoosesFirstGguf()
{
    var baseDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var modelDirectory = ModelPathResolver.GetDefaultModelDirectory(baseDirectory);
    Directory.CreateDirectory(modelDirectory);

    try
    {
        var secondModel = Path.Combine(modelDirectory, "z-model.gguf");
        var firstModel = Path.Combine(modelDirectory, "a-model.gguf");
        File.WriteAllText(secondModel, string.Empty);
        File.WriteAllText(firstModel, string.Empty);

        var path = ModelPathResolver.GetDefaultModelPath(baseDirectory);
        Assert(path == firstModel, path);
    }
    finally
    {
        Directory.Delete(baseDirectory, recursive: true);
    }
}

static void TranslationOptionsDefaults()
{
    var options = TranslationOptions.CreateDefault(@"C:\model.gguf");
    Assert(options.ModelPath == @"C:\model.gguf", "ModelPath mismatch");
    Assert(options.ContextSize == 2048, "ContextSize mismatch");
    Assert(options.GpuLayerCount == 999, "GpuLayerCount mismatch");
    Assert(options.BatchSize == 256, "BatchSize mismatch");
    Assert(options.MaxTokens == 512, "MaxTokens mismatch");
}

static void PromptContent()
{
    var prompt = TranslationPromptBuilder.BuildEnglishToChinesePrompt("push mid now");
    Assert(prompt.Contains("/no_think", StringComparison.Ordinal), "missing /no_think");
    Assert(prompt.Contains("Only output the Chinese translation.", StringComparison.Ordinal), "missing output rule");
    Assert(prompt.Contains("push mid now", StringComparison.Ordinal), "missing source text");
}

static void BlankPrompt()
{
    Assert(TranslationPromptBuilder.BuildEnglishToChinesePrompt("   ") == string.Empty, "blank prompt should stay empty");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
