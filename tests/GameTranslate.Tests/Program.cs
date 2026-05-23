using GameTranslate.Services;
using GameTranslate.Models;

var tests = new List<(string Name, Action Test)>
{
    ("default model fallback path", DefaultModelFallbackPath),
    ("default model chooses first gguf", DefaultModelChoosesFirstGguf),
    ("capture region normalize", CaptureRegionNormalize),
    ("capture region display", CaptureRegionDisplay),
    ("image change detector unchanged", ImageChangeDetectorUnchanged),
    ("image change detector changed", ImageChangeDetectorChanged),
    ("ocr text normalizer", OcrTextNormalizerTrimsAndDropsBlankLines),
    ("exception formatter includes inner exception", ExceptionFormatterIncludesInnerException),
    ("cuda native library resolver missing file", CudaNativeLibraryResolverMissingFile),
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

static void CaptureRegionNormalize()
{
    var region = CaptureRegion.Normalize(100, 90, -30, -20);
    Assert(region.X == 70, "X mismatch");
    Assert(region.Y == 70, "Y mismatch");
    Assert(region.Width == 30, "Width mismatch");
    Assert(region.Height == 20, "Height mismatch");
}

static void CaptureRegionDisplay()
{
    var region = new CaptureRegion(12.4, 20.5, 300.2, 80.8);
    Assert(region.ToDisplayText() == "X=12, Y=21, W=300, H=81", region.ToDisplayText());
    Assert(default(CaptureRegion).ToDisplayText() == "未选择区域", "empty region text mismatch");
}

static void ImageChangeDetectorUnchanged()
{
    var pixels = CreateBgraPixels(32, 32, 40, 50, 60);
    var first = ImageChangeDetector.CreateFingerprint(pixels, 32, 32, 32 * 4);
    var second = ImageChangeDetector.CreateFingerprint(pixels, 32, 32, 32 * 4);

    Assert(ImageChangeDetector.HasMeaningfulChange(null, first), "first frame should be treated as changed");
    Assert(!ImageChangeDetector.HasMeaningfulChange(first, second), "same frame should not be treated as changed");
}

static void ImageChangeDetectorChanged()
{
    var dark = CreateBgraPixels(32, 32, 20, 20, 20);
    var bright = CreateBgraPixels(32, 32, 220, 220, 220);
    var first = ImageChangeDetector.CreateFingerprint(dark, 32, 32, 32 * 4);
    var second = ImageChangeDetector.CreateFingerprint(bright, 32, 32, 32 * 4);

    Assert(ImageChangeDetector.HasMeaningfulChange(first, second), "different frame should be treated as changed");
}

static byte[] CreateBgraPixels(int width, int height, byte blue, byte green, byte red)
{
    var stride = width * 4;
    var pixels = new byte[stride * height];
    for (var offset = 0; offset < pixels.Length; offset += 4)
    {
        pixels[offset] = blue;
        pixels[offset + 1] = green;
        pixels[offset + 2] = red;
        pixels[offset + 3] = 255;
    }

    return pixels;
}

static void OcrTextNormalizerTrimsAndDropsBlankLines()
{
    var text = OcrTextNormalizer.NormalizeLines(new[]
    {
        "  hello team  ",
        "",
        "   ",
        " push mid "
    });

    Assert(text == $"hello team{Environment.NewLine}push mid", text);
}

static void ExceptionFormatterIncludesInnerException()
{
    var exception = new InvalidOperationException(
        "outer",
        new FileNotFoundException("missing native dll", "llama.dll"));

    var text = ExceptionFormatter.Format(exception);
    Assert(text.Contains("System.InvalidOperationException: outer", StringComparison.Ordinal), text);
    Assert(text.Contains("System.IO.FileNotFoundException: missing native dll", StringComparison.Ordinal), text);
    Assert(text.Contains("File: llama.dll", StringComparison.Ordinal), text);
}

static void CudaNativeLibraryResolverMissingFile()
{
    var baseDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    try
    {
        try
        {
            _ = CudaNativeLibraryResolver.GetCudaLlamaLibraryPath(baseDirectory);
            throw new InvalidOperationException("resolver should throw");
        }
        catch (FileNotFoundException ex)
        {
            Assert(ex.FileName?.EndsWith(Path.Combine("runtimes", "win-x64", "native", "cuda12", "llama.dll"), StringComparison.Ordinal) == true, ex.FileName ?? "missing filename");
        }
    }
    finally
    {
        if (Directory.Exists(baseDirectory))
        {
            Directory.Delete(baseDirectory, recursive: true);
        }
    }
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
