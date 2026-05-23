using System.IO;

namespace GameTranslate.Services;

public static class CudaNativeLibraryResolver
{
    public static string GetCudaLlamaLibraryPath(string appBaseDirectory)
    {
        var path = Path.Combine(
            appBaseDirectory,
            "runtimes",
            "win-x64",
            "native",
            "cuda12",
            "llama.dll");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("找不到 CUDA12 llama.dll。请确认 LLamaSharp.Backend.Cuda12.Windows 已复制到输出目录。", path);
        }

        return path;
    }
}

