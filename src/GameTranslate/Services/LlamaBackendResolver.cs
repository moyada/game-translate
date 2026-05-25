using System.IO;

namespace GameTranslate.Services;

public enum LlamaBackendKind
{
    Cuda12,
    Vulkan
}

public sealed record LlamaBackendSelection(
    LlamaBackendKind Kind,
    string LibraryPath,
    string DisplayName);

public static class LlamaBackendResolver
{
    public static LlamaBackendSelection Resolve(string appBaseDirectory, bool hasNvidiaGpu)
    {
        var cudaPath = GetBackendLibraryPath(appBaseDirectory, "cuda12");
        var vulkanPath = GetBackendLibraryPath(appBaseDirectory, "vulkan");

        if (hasNvidiaGpu && File.Exists(cudaPath))
        {
            WindowsNativeLibrarySearchPath.AddNativeDllDirectories(appBaseDirectory, "cuda12");
            return new LlamaBackendSelection(LlamaBackendKind.Cuda12, cudaPath, "CUDA12");
        }

        if (File.Exists(vulkanPath))
        {
            WindowsNativeLibrarySearchPath.AddNativeDllDirectories(appBaseDirectory, "vulkan");
            return new LlamaBackendSelection(LlamaBackendKind.Vulkan, vulkanPath, "Vulkan");
        }

        if (hasNvidiaGpu)
        {
            throw new FileNotFoundException("找不到 CUDA12 或 Vulkan llama.dll。请确认 LLamaSharp CUDA12/Vulkan backend 已复制到输出目录。", cudaPath);
        }

        throw new FileNotFoundException("未检测到 NVIDIA 显卡，且找不到 Vulkan llama.dll。请确认 LLamaSharp.Backend.Vulkan 已复制到输出目录。", vulkanPath);
    }

    private static string GetBackendLibraryPath(string appBaseDirectory, string backendDirectory)
    {
        return Path.Combine(
            appBaseDirectory,
            "runtimes",
            "win-x64",
            "native",
            backendDirectory,
            "llama.dll");
    }
}
