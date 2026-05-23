using System.IO;
using System.Runtime.InteropServices;

namespace GameTranslate.Services;

public static class WindowsNativeLibrarySearchPath
{
    private const uint LoadLibrarySearchDefaultDirs = 0x00001000;

    public static void AddNativeDllDirectories(string appBaseDirectory)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        _ = SetDefaultDllDirectories(LoadLibrarySearchDefaultDirs);

        var directories = Directory
            .EnumerateFiles(appBaseDirectory, "*.dll", SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(path => path!.Contains("cuda12", StringComparison.OrdinalIgnoreCase))
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase);

        foreach (var directory in directories)
        {
            _ = AddDllDirectory(directory!);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetDefaultDllDirectories(uint directoryFlags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint AddDllDirectory(string newDirectory);
}

