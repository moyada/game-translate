using GameTranslate.Services;
using GameTranslate.Models;

var tests = new List<(string Name, Action Test)>
{
    ("default model fallback path", DefaultModelFallbackPath),
    ("default model chooses first gguf", DefaultModelChoosesFirstGguf),
    ("capture region normalize", CaptureRegionNormalize),
    ("capture region display", CaptureRegionDisplay),
    ("coordinate scaler scales capture region", CoordinateScalerScalesCaptureRegion),
    ("capture selection display includes scale", CaptureSelectionDisplayIncludesScale),
    ("image change detector unchanged", ImageChangeDetectorUnchanged),
    ("image change detector changed", ImageChangeDetectorChanged),
    ("ocr preprocessor detects colored text", OcrPreprocessorDetectsColoredText),
    ("ocr preprocessor converts colored text to black", OcrPreprocessorConvertsColoredTextToBlack),
    ("ocr preprocessor output stride", OcrPreprocessorOutputStride),
    ("ocr text normalizer", OcrTextNormalizerTrimsAndDropsBlankLines),
    ("exception formatter includes inner exception", ExceptionFormatterIncludesInnerException),
    ("cuda native library resolver missing file", CudaNativeLibraryResolverMissingFile),
    ("translation options", TranslationOptionsDefaults),
    ("qwen chat prompt content", QwenChatPromptContent),
    ("simple translation prompt", SimpleTranslationPrompt),
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

static void CoordinateScalerScalesCaptureRegion()
{
    var region = new CaptureRegion(100, 200, 300, 80);
    var scaled = CoordinateScaler.Scale(region, 1.25, 1.5);

    Assert(scaled.X == 125, "X mismatch");
    Assert(scaled.Y == 300, "Y mismatch");
    Assert(scaled.Width == 375, "Width mismatch");
    Assert(scaled.Height == 120, "Height mismatch");
}

static void CaptureSelectionDisplayIncludesScale()
{
    var selection = new CaptureSelection(
        new CaptureRegion(10, 20, 100, 50),
        new CaptureRegion(15, 30, 150, 75),
        1.5,
        1.5);

    var text = selection.ToDisplayText();
    Assert(text.Contains("X=15, Y=30, W=150, H=75", StringComparison.Ordinal), text);
    Assert(text.Contains("缩放 150% x 150%", StringComparison.Ordinal), text);
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

static void OcrPreprocessorDetectsColoredText()
{
    Assert(OcrImagePreprocessor.IsLikelyTextPixel(210, 210, 20), "yellow notice text should be treated as text");
    Assert(OcrImagePreprocessor.IsLikelyTextPixel(165, 35, 130), "magenta chat text should be treated as text");
    Assert(!OcrImagePreprocessor.IsLikelyTextPixel(130, 130, 130), "gray chat background should not be treated as text");
}

static void OcrPreprocessorConvertsColoredTextToBlack()
{
    const int width = 5;
    const int height = 5;
    var pixels = CreateBgraPixels(width, height, 130, 130, 130);
    var centerOffset = (2 * width + 2) * 4;
    pixels[centerOffset] = 20;
    pixels[centerOffset + 1] = 210;
    pixels[centerOffset + 2] = 210;
    pixels[centerOffset + 3] = 255;

    var processed = OcrImagePreprocessor.CreateHighContrastBgra(pixels, width, height, width * 4);
    var farBackgroundOffset = 0;

    Assert(processed[centerOffset] == 0, "text blue channel should be black");
    Assert(processed[centerOffset + 1] == 0, "text green channel should be black");
    Assert(processed[centerOffset + 2] == 0, "text red channel should be black");
    Assert(processed[farBackgroundOffset] == 255, "background should be white");
}

static void OcrPreprocessorOutputStride()
{
    Assert(OcrImagePreprocessor.GetOutputStride(17) == 68, "stride should be width * 4");
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

static void QwenChatPromptContent()
{
    var prompt = TranslationPromptBuilder.BuildEnglishToChinesePrompt("push mid now");
    Assert(prompt.Contains("<|im_start|>system", StringComparison.Ordinal), "missing system header");
    Assert(prompt.Contains("<|im_start|>user", StringComparison.Ordinal), "missing user header");
    Assert(prompt.Contains("<|im_start|>assistant", StringComparison.Ordinal), "missing assistant header");
    Assert(prompt.Contains("/no_think", StringComparison.Ordinal), "missing /no_think");
    Assert(prompt.Contains("Only output the Chinese translation.", StringComparison.Ordinal), "missing output rule");
    Assert(prompt.Contains("push mid now", StringComparison.Ordinal), "missing source text");
    Assert(prompt.TrimEnd().EndsWith("<|im_start|>assistant", StringComparison.Ordinal), "prompt should end at assistant turn");
}

static void SimpleTranslationPrompt()
{
    var prompt = TranslationPromptBuilder.BuildEnglishToChinesePrompt("Hello.");
    Assert(prompt.Contains("Hello.", StringComparison.Ordinal), "missing simple sentence");
    Assert(prompt.Contains("Do not repeat the English source.", StringComparison.Ordinal), "missing source repeat guard");
    Assert(!prompt.Contains("Text:", StringComparison.Ordinal), "legacy prompt marker should not be used");
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
