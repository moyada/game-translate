using GameTranslate.Services;
using GameTranslate.Models;
using GameTranslate.ViewModels;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

var tests = new List<(string Name, Action Test)>
{
    ("default model fallback path", DefaultModelFallbackPath),
    ("default model chooses first gguf", DefaultModelChoosesFirstGguf),
    ("capture region normalize", CaptureRegionNormalize),
    ("capture region display", CaptureRegionDisplay),
    ("coordinate scaler scales capture region", CoordinateScalerScalesCaptureRegion),
    ("capture selection display includes scale", CaptureSelectionDisplayIncludesScale),
    ("monitoring start requires selection", MonitoringStartRequiresSelection),
    ("translate requires selection", TranslateRequiresSelection),
    ("translate command refreshes on selection", TranslateCommandRefreshesOnSelection),
    ("clear selection disables controls", ClearSelectionDisablesControls),
    ("monitoring button text", MonitoringButtonText),
    ("llm lazy load policy", LlmLazyLoadPolicy),
    ("single shot translation policy", SingleShotTranslationPolicy),
    ("manual translation captures ocr and translates once", ManualTranslationCapturesOcrAndTranslatesOnce),
    ("image change detector unchanged", ImageChangeDetectorUnchanged),
    ("image change detector changed", ImageChangeDetectorChanged),
    ("paddle ocr preview label", PaddleOcrPreviewLabel),
    ("paddle ocr upscale factor", PaddleOcrUpscaleFactor),
    ("paddle ocr light contrast enhancement", PaddleOcrLightContrastEnhancement),
    ("paddle ocr recreates engine after failure", PaddleOcrRecreatesEngineAfterFailure),
    ("paddle ocr recreates engine after recognition", PaddleOcrRecreatesEngineAfterRecognition),
    ("paddle ocr failure message supports manual retry", PaddleOcrFailureMessageSupportsManualRetry),
    ("selection overlay behavior", SelectionOverlayBehaviorFlags),
    ("ocr text normalizer", OcrTextNormalizerTrimsAndDropsBlankLines),
    ("exception formatter includes inner exception", ExceptionFormatterIncludesInnerException),
    ("cuda native library resolver missing file", CudaNativeLibraryResolverMissingFile),
    ("translation options", TranslationOptionsDefaults),
    ("qwen chat prompt content", QwenChatPromptContent),
    ("simple translation prompt", SimpleTranslationPrompt),
    ("chat speaker prefix stripped", ChatSpeakerPrefixStripped),
    ("chat speaker prefix restored", ChatSpeakerPrefixRestored),
    ("chat speaker prefix only colon", ChatSpeakerPrefixOnlyColon),
    ("chat at mention prefix preserved", ChatAtMentionPrefixPreserved),
    ("maplestory colon speaker prefixes", MapleStoryColonSpeakerPrefixes),
    ("maplestory glossary prompt content", MapleStoryGlossaryPromptContent),
    ("maplestory glossary file persists entries", MapleStoryGlossaryFilePersistsEntries),
    ("translation output removes think block", TranslationOutputRemovesThinkBlock),
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

static void MonitoringStartRequiresSelection()
{
    using var viewModel = new MainViewModel(
        new FakeTranslationService(),
        new FakeScreenCaptureService(),
        new FakeOcrService());

    Assert(!viewModel.CanStartMonitoring, "monitoring should not start without selection");
    Assert(!viewModel.ToggleMonitoringCommand.CanExecute(null), "monitoring command should be disabled without selection");
    viewModel.SetCaptureSelection(new CaptureSelection(
        new CaptureRegion(10, 20, 100, 50),
        new CaptureRegion(10, 20, 100, 50),
        1,
        1));
    Assert(viewModel.CanStartMonitoring, "monitoring should start after selection when idle");
    Assert(viewModel.ToggleMonitoringCommand.CanExecute(null), "monitoring command should be enabled after selection");
}

static void TranslateRequiresSelection()
{
    using var viewModel = new MainViewModel(
        new FakeTranslationService(),
        new FakeScreenCaptureService(),
        new FakeOcrService());

    Assert(!viewModel.CanTranslate, "translation should not be enabled without selection");
    Assert(!viewModel.TranslateCommand.CanExecute(null), "translation command should be disabled without selection");
    viewModel.SetCaptureSelection(new CaptureSelection(
        new CaptureRegion(10, 20, 100, 50),
        new CaptureRegion(10, 20, 100, 50),
        1,
        1));
    Assert(viewModel.CanTranslate, "translation should be enabled after selection");
    Assert(viewModel.TranslateCommand.CanExecute(null), "translation command should be enabled after selection");
}

static void TranslateCommandRefreshesOnSelection()
{
    using var viewModel = new MainViewModel(
        new FakeTranslationService(),
        new FakeScreenCaptureService(),
        new FakeOcrService())
    {
        SourceText = string.Empty
    };

    var refreshCount = 0;
    viewModel.TranslateCommand.CanExecuteChanged += (_, _) => refreshCount++;

    viewModel.SetCaptureSelection(new CaptureSelection(
        new CaptureRegion(10, 20, 100, 50),
        new CaptureRegion(10, 20, 100, 50),
        1,
        1));

    Assert(refreshCount > 0, "selection should refresh translation command state");
    Assert(viewModel.CanTranslate, "translation should only require a selected region");
    Assert(viewModel.TranslateCommand.CanExecute(null), "translation command should be enabled after selection even before OCR text arrives");
}

static void ClearSelectionDisablesControls()
{
    using var viewModel = new MainViewModel(
        new FakeTranslationService(),
        new FakeScreenCaptureService(),
        new FakeOcrService());

    viewModel.SetCaptureSelection(new CaptureSelection(
        new CaptureRegion(10, 20, 100, 50),
        new CaptureRegion(10, 20, 100, 50),
        1,
        1));
    viewModel.ClearCaptureSelectionAsync().GetAwaiter().GetResult();

    Assert(!viewModel.CanToggleMonitoring, "monitoring should be disabled after clearing selection");
    Assert(!viewModel.CanTranslate, "translation should be disabled after clearing selection");
}

static void MonitoringButtonText()
{
    using var viewModel = new MainViewModel(
        new FakeTranslationService(),
        new FakeScreenCaptureService(),
        new FakeOcrService());

    Assert(viewModel.MonitoringButtonText == "开始监控", viewModel.MonitoringButtonText);
}

static void LlmLazyLoadPolicy()
{
    Assert(MainViewModel.UsesLazyModelLoading, "model should lazy-load on first translation");
}

static void SingleShotTranslationPolicy()
{
    Assert(MainViewModel.UsesSingleShotCaptureTranslation, "manual translation should capture, OCR, and translate once");
}

static void ManualTranslationCapturesOcrAndTranslatesOnce()
{
    var capture = new FakeScreenCaptureService();
    var ocr = new FakeOcrService("hello, team");
    var translation = new FakeTranslationService("大家好");
    using var viewModel = new MainViewModel(translation, capture, ocr);

    viewModel.SetCaptureSelection(new CaptureSelection(
        new CaptureRegion(10, 20, 100, 50),
        new CaptureRegion(10, 20, 100, 50),
        1,
        1));

    viewModel.TranslateCommand.Execute(null);
    SpinWait.SpinUntil(() => translation.TranslateCount == 1 || viewModel.StatusText == "翻译失败", TimeSpan.FromSeconds(2));

    Assert(capture.CaptureCount == 1, $"capture count {capture.CaptureCount}");
    Assert(ocr.CallCount == 1, $"ocr count {ocr.CallCount}");
    Assert(translation.LoadCount == 1, $"load count {translation.LoadCount}");
    Assert(translation.TranslateCount == 1, $"translate count {translation.TranslateCount}");
    Assert(translation.LastText == "hello, team", translation.LastText ?? "missing translation text");
    Assert(viewModel.SourceText == "hello, team", viewModel.SourceText);
    Assert(viewModel.TranslatedText == "大家好", viewModel.TranslatedText);
    Assert(viewModel.StatusText == "翻译完成", viewModel.StatusText);
    Assert(!viewModel.IsCaptureBusy, "manual translation should release capture busy state");
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

static void PaddleOcrPreviewLabel()
{
    Assert(PaddleSharpOcrService.PreviewLabel == "PaddleOCR 输入预览", PaddleSharpOcrService.PreviewLabel);
}

static void PaddleOcrUpscaleFactor()
{
    Assert(PaddleSharpOcrService.InputScaleFactor == 3.0, "PaddleOCR should upscale small game text");
}

static void PaddleOcrLightContrastEnhancement()
{
    Assert(PaddleSharpOcrService.UsesLightContrastEnhancement, "PaddleOCR should enhance low-contrast game chat text");
}

static void PaddleOcrRecreatesEngineAfterFailure()
{
    Assert(PaddleSharpOcrService.RecreatesEngineAfterFailure, "PaddleOCR engine should reset after native predictor failures");
}

static void PaddleOcrRecreatesEngineAfterRecognition()
{
    Assert(PaddleSharpOcrService.RecreatesEngineAfterRecognition, "PaddleOCR engine should reset after each native predictor run");
}

static void PaddleOcrFailureMessageSupportsManualRetry()
{
    Assert(PaddleSharpOcrService.FailureMessage.Contains("再次点击翻译", StringComparison.Ordinal), PaddleSharpOcrService.FailureMessage);
    Assert(PaddleSharpOcrService.FailureMessage.Contains("重新开始监控", StringComparison.Ordinal), PaddleSharpOcrService.FailureMessage);
}

static void SelectionOverlayBehaviorFlags()
{
    Assert(!SelectionOverlayBehavior.ShowsConfirmButtons, "selection overlay should not show confirm/cancel buttons");
    Assert(!SelectionOverlayBehavior.ShowsInstructionText, "selection overlay should not show instruction text");
    Assert(SelectionOverlayBehavior.AllowsClickThroughOutsideSelection, "selection overlay should pass clicks outside the selection through");
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
    Assert(prompt.Contains("same number of translated lines", StringComparison.Ordinal), "missing multi-line output rule");
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

static void ChatSpeakerPrefixStripped()
{
    var text = TranslationSourceNormalizer.ExtractTranslatableText("Steam : ew you have cooties get away from me");
    Assert(text == "ew you have cooties get away from me", text);

    var prompt = TranslationPromptBuilder.BuildEnglishToChinesePrompt("Steam : ew you have cooties get away from me");
    Assert(prompt.Contains("ew you have cooties get away from me", StringComparison.Ordinal), "missing chat content");
    Assert(!prompt.Contains("Steam :", StringComparison.Ordinal), "speaker name should not be sent as source text");
}

static void ChatSpeakerPrefixRestored()
{
    var text = TranslationSourceNormalizer.ApplySpeakerPrefix("Steam : ew you have cooties get away from me", "恶心，离我远点");
    Assert(text == "Steam：恶心，离我远点", text);
}

static void ChatSpeakerPrefixOnlyColon()
{
    var semicolon = TranslationSourceNormalizer.Parse("Steam ; ew you have cooties");
    Assert(semicolon.SpeakerPrefix is null, semicolon.SpeakerPrefix ?? "unexpected speaker");
    Assert(semicolon.TranslatableText == "Steam ; ew you have cooties", semicolon.TranslatableText);

    var space = TranslationSourceNormalizer.Parse("Steam ew you have cooties");
    Assert(space.SpeakerPrefix is null, space.SpeakerPrefix ?? "unexpected speaker");
    Assert(space.TranslatableText == "Steam ew you have cooties", space.TranslatableText);

    var normalSentence = TranslationSourceNormalizer.Parse("go kill Slime");
    Assert(normalSentence.SpeakerPrefix is null, normalSentence.SpeakerPrefix ?? "unexpected speaker");
    Assert(normalSentence.TranslatableText == "go kill Slime", normalSentence.TranslatableText);

    var greeting = TranslationSourceNormalizer.Parse("Hello team");
    Assert(greeting.SpeakerPrefix is null, greeting.SpeakerPrefix ?? "unexpected speaker");
    Assert(greeting.TranslatableText == "Hello team", greeting.TranslatableText);
}

static void ChatAtMentionPrefixPreserved()
{
    var parsed = TranslationSourceNormalizer.Parse("@Steam : ew you have cooties");
    Assert(parsed.SpeakerPrefix == "@Steam", parsed.SpeakerPrefix ?? "missing speaker");
    Assert(parsed.TranslatableText == "ew you have cooties", parsed.TranslatableText);

    var text = TranslationSourceNormalizer.ApplySpeakerPrefix("@Steam : ew you have cooties", "恶心，离我远点");
    Assert(text == "@Steam：恶心，离我远点", text);

    var missingColon = TranslationSourceNormalizer.Parse("@Steam ew you have cooties");
    Assert(missingColon.SpeakerPrefix is null, missingColon.SpeakerPrefix ?? "unexpected speaker");
    Assert(missingColon.TranslatableText == "@Steam ew you have cooties", missingColon.TranslatableText);
}

static void MapleStoryColonSpeakerPrefixes()
{
    var source = string.Join(Environment.NewLine, new[]
    {
        "LordLemon : pls nerf mage",
        "Tako : lol",
        "Willow : TRUE",
        "Tako : ILL MAKE IT MY GOAL",
        "LordLemon : TY",
        "Tako : maybe",
        "Tako : B)",
        "Ziren : THERE better not be another ***kng stage"
    });

    var parsed = TranslationSourceNormalizer.Parse(source);
    Assert(parsed.TranslatableText.Contains("pls nerf mage", StringComparison.Ordinal), parsed.TranslatableText);
    Assert(!parsed.TranslatableText.Contains("LordLemon", StringComparison.Ordinal), parsed.TranslatableText);
    Assert(!parsed.TranslatableText.Contains("Tako", StringComparison.Ordinal), parsed.TranslatableText);

    var restored = TranslationSourceNormalizer.ApplySpeakerPrefix(source, string.Join(Environment.NewLine, new[]
    {
        "请削弱法师",
        "哈哈",
        "真的",
        "我会把它当成目标",
        "谢谢",
        "也许",
        "B)",
        "最好不要再来一个该死的关卡"
    }));

    Assert(restored.Contains("LordLemon：请削弱法师", StringComparison.Ordinal), restored);
    Assert(restored.Contains("Tako：哈哈", StringComparison.Ordinal), restored);
    Assert(restored.Contains("Willow：真的", StringComparison.Ordinal), restored);
    Assert(restored.Contains("Ziren：最好不要再来一个该死的关卡", StringComparison.Ordinal), restored);
}

static void MapleStoryGlossaryPromptContent()
{
    var prompt = TranslationPromptBuilder.BuildEnglishToChinesePrompt("go kill Slime near Henesys");
    Assert(prompt.Contains("MapleStory", StringComparison.Ordinal), "missing MapleStory context");
    Assert(prompt.Contains("冒险岛", StringComparison.Ordinal), "missing Chinese game context");
    Assert(prompt.Contains("Slime => 绿水灵", StringComparison.Ordinal), "missing monster glossary");
    Assert(prompt.Contains("Henesys => 射手村", StringComparison.Ordinal), "missing map glossary");
}

static void MapleStoryGlossaryFilePersistsEntries()
{
    var glossary = TranslationGlossary.LoadDefault();
    Assert(glossary.Entries.Count >= 60, $"glossary entry count {glossary.Entries.Count}");
    Assert(glossary.FindRelevantTerms("Slime and Henesys", 10).Any(entry => entry.English == "Slime" && entry.Chinese == "绿水灵"), "missing Slime entry");
    Assert(glossary.FindRelevantTerms("Slime and Henesys", 10).Any(entry => entry.English == "Henesys" && entry.Chinese == "射手村"), "missing Henesys entry");
}

static void TranslationOutputRemovesThinkBlock()
{
    var text = TranslationOutputCleaner.Clean("<think>\nI should translate this.\n</think>\n你好。<|im_end|>");
    Assert(text == "你好。", text);
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

internal sealed class FakeTranslationService : ITranslationService
{
    private readonly string? _result;

    public FakeTranslationService(string? result = null)
    {
        _result = result;
    }

    public bool IsLoaded => true;

    public int LoadCount { get; private set; }

    public int TranslateCount { get; private set; }

    public string? LastText { get; private set; }

    public Task LoadAsync(TranslationOptions options, CancellationToken cancellationToken = default)
    {
        LoadCount++;
        return Task.CompletedTask;
    }

    public Task<string> TranslateToChineseAsync(string text, CancellationToken cancellationToken = default)
    {
        TranslateCount++;
        LastText = text;
        return Task.FromResult(_result ?? text);
    }
}

internal sealed class FakeScreenCaptureService : IScreenCaptureService
{
    public int CaptureCount { get; private set; }

    public CapturedFrame Capture(CaptureRegion region)
    {
        CaptureCount++;
        return TestFrameFactory.Create();
    }
}

internal sealed class FakeOcrService : IOcrService
{
    private readonly string _text;

    public FakeOcrService(string text = "")
    {
        _text = text;
    }

    public int CallCount { get; private set; }

    public Task<string> RecognizeTextAsync(CapturedFrame frame, CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(_text);
    }
}

internal static class TestFrameFactory
{
    public static CapturedFrame Create()
    {
        const int width = 4;
        const int height = 4;
        const int stride = width * 4;
        var pixels = new byte[stride * height];
        for (var offset = 0; offset < pixels.Length; offset += 4)
        {
            pixels[offset] = 20;
            pixels[offset + 1] = 30;
            pixels[offset + 2] = 40;
            pixels[offset + 3] = 255;
        }

        var preview = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        preview.Freeze();
        return new CapturedFrame(width, height, stride, pixels, preview);
    }
}
