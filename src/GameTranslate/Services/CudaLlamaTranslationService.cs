using System.IO;
using System.Text;
using LLama;
using LLama.Common;
using LLama.Native;

namespace GameTranslate.Services;

public sealed class CudaLlamaTranslationService : ITranslationService, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private LLamaWeights? _weights;
    private ModelParams? _modelParams;
    private TranslationOptions? _options;

    public bool IsLoaded => _weights is not null && _modelParams is not null && _options is not null;

    public async Task LoadAsync(TranslationOptions options, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10))
        {
            throw new PlatformNotSupportedException("第一版只支持 Windows 10 / Windows 11 x64。");
        }

        if (string.IsNullOrWhiteSpace(options.ModelPath))
        {
            throw new InvalidOperationException("请先设置 GGUF 模型路径。");
        }

        if (!File.Exists(options.ModelPath))
        {
            throw new FileNotFoundException("找不到模型文件。请下载 unsloth/Qwen3-1.7B-GGUF 的 UD-Q4_K_XL GGUF 文件，并放到 Models 目录。", options.ModelPath);
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            DisposeModel();

            var cudaLlamaPath = CudaNativeLibraryResolver.GetCudaLlamaLibraryPath(AppContext.BaseDirectory);
            NativeLibraryConfig.All
                .WithLogs(true)
                .WithLibrary(cudaLlamaPath, string.Empty);

            _modelParams = new ModelParams(options.ModelPath)
            {
                ContextSize = options.ContextSize,
                GpuLayerCount = options.GpuLayerCount,
                BatchSize = options.BatchSize
            };
            _options = options;
            _weights = await Task.Run(() => LLamaWeights.LoadFromFile(_modelParams), cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string> TranslateToChineseAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        if (!IsLoaded)
        {
            throw new InvalidOperationException("CUDA 模型尚未加载。");
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var prompt = TranslationPromptBuilder.BuildEnglishToChinesePrompt(text);
            var output = new StringBuilder();

            using var context = _weights!.CreateContext(_modelParams!);
            var executor = new InteractiveExecutor(context);
            var inferenceParams = new InferenceParams
            {
                MaxTokens = _options!.MaxTokens,
                AntiPrompts = new List<string>
                {
                    "<|im_end|>",
                    "<|endoftext|>",
                    "\nText:"
                }
            };

            await foreach (var token in executor.InferAsync(prompt, inferenceParams).WithCancellation(cancellationToken))
            {
                output.Append(token);
            }

            return CleanOutput(output.ToString());
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string CleanOutput(string value)
    {
        return value
            .Replace("<|im_end|>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("<|endoftext|>", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private void DisposeModel()
    {
        _weights?.Dispose();
        _weights = null;
        _modelParams = null;
        _options = null;
    }

    public void Dispose()
    {
        DisposeModel();
        _gate.Dispose();
    }
}
