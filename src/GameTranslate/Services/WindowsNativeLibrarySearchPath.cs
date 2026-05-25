using System.IO;
using System.Runtime.InteropServices;

namespace GameTranslate.Services;

public static class WindowsNativeLibrarySearchPath
{
    private const uint LoadLibrarySearchDefaultDirs = 0x00001000;

    public static void AddNativeDllDirectories(string appBaseDirectory, string? preferredBackendDirectory = null)
    {
        if (!OperatingSystem.IsWindows() || !Directory.Exists(appBaseDirectory))
        {
            return;
        }

        _ = SetDefaultDllDirectories(LoadLibrarySearchDefaultDirs);

        var directories = Directory
            .EnumerateFiles(appBaseDirectory, "*.dll", SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(path => IsPreferredBackendDirectory(path!, preferredBackendDirectory))
            .ThenByDescending(path => path!.Contains("cuda12", StringComparison.OrdinalIgnoreCase))
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

    private static bool IsPreferredBackendDirectory(string path, string? preferredBackendDirectory)
    {
        return !string.IsNullOrWhiteSpace(preferredBackendDirectory)
            && path.Contains(preferredBackendDirectory, StringComparison.OrdinalIgnoreCase);
    }
}
