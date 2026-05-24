using System.Runtime.InteropServices;

namespace GameTranslate.Services;

public sealed class SafeHotKeyMonitor : IDisposable
{
    private const int VK_MENU = 0x12;
    private const int VK_OEM_3 = 0xC0;

    private readonly Action _trigger;
    private readonly CancellationTokenSource _cancellation = new();
    private Task? _monitorTask;

    public SafeHotKeyMonitor(Action trigger)
    {
        _trigger = trigger;
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    public void StartMonitoring()
    {
        _monitorTask ??= Task.Run(MonitorAsync);
    }

    public void Dispose()
    {
        _cancellation.Cancel();
    }

    private async Task MonitorAsync()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            if (IsKeyDown(VK_MENU) && IsKeyDown(VK_OEM_3))
            {
                _trigger();
                await DelayAsync(500);
            }

            await DelayAsync(120);
        }
    }

    private async Task DelayAsync(int milliseconds)
    {
        try
        {
            await Task.Delay(milliseconds, _cancellation.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static bool IsKeyDown(int vKey)
    {
        return (GetAsyncKeyState(vKey) & 0x8000) != 0;
    }
}
