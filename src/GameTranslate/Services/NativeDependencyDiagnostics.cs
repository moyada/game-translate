using System.IO;

namespace GameTranslate.Services;

public static class NativeDependencyDiagnostics
{
    private static readonly string[] KnownNativeFileNames =
    {
        "llama.dll",
        "ggml.dll",
        "ggml-base.dll",
        "ggml-cpu.dll",
        "ggml-cuda.dll",
        "ggml-vulkan.dll",
        "ggml-rpc.dll",
        "cudart64_12.dll",
        "cublas64_12.dll",
        "cublasLt64_12.dll",
        "nvrtc64_120_0.dll"
    };

    public static string CreateReport(string appBaseDirectory)
    {
        var existingFiles = Directory
            .EnumerateFiles(appBaseDirectory, "*.dll", SearchOption.AllDirectories)
            .Where(path => KnownNativeFileNames.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => Path.GetRelativePath(appBaseDirectory, path))
            .ToArray();

        if (existingFiles.Length == 0)
        {
            return $"未在输出目录找到 LLamaSharp native DLL。输出目录: {appBaseDirectory}";
        }

        return "已找到 LLamaSharp native DLL:" + Environment.NewLine + string.Join(Environment.NewLine, existingFiles);
    }
}
