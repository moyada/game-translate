using System.Management;

namespace GameTranslate.Services;

public static class WindowsGpuDetector
{
    public static bool HasNvidiaGpu()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
            using var results = searcher.Get();
            foreach (var item in results)
            {
                var name = item["Name"]?.ToString();
                if (!string.IsNullOrWhiteSpace(name)
                    && name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch (ManagementException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        return false;
    }
}
