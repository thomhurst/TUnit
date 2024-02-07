using System.Runtime.CompilerServices;
using Hardware.Info;

namespace TUnit.TestAdapter;

internal class SystemResourceMonitor : IDisposable
{
    private static readonly HardwareInfo HardwareInfo = new();
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(5));
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    [ModuleInitializer]
    public static void Initialize()
    {
        Task.Run(Refresh);
    }

    public SystemResourceMonitor()
    {
        Task.Factory.StartNew(async () =>
        {
            while (await _periodicTimer.WaitForNextTickAsync())
            {
                Refresh();
            }
        }, TaskCreationOptions.LongRunning);
    }

    public bool IsSystemStrained()
    {
        return IsCpuStrained() || IsMemoryStrained();
    }

    private bool IsCpuStrained()
    {
        if (!HardwareInfo.CpuList.Any())
        {
            return false;
        }

        return HardwareInfo.CpuList.All(x => x.PercentProcessorTime > 90);
    }
    
    private bool IsMemoryStrained()
    {
        if (HardwareInfo.MemoryStatus.AvailablePhysical is 0
            || HardwareInfo.MemoryStatus.TotalPhysical is 0)
        {
            return false;
        }
        
        return GetPercentage(HardwareInfo.MemoryStatus.AvailablePhysical, HardwareInfo.MemoryStatus.TotalPhysical) > 80;
    }

    private static ulong GetPercentage(ulong available, ulong total)
    {
        return (available / total) * 100;
    }

    private static void Refresh()
    {
        HardwareInfo.RefreshCPUList();
        HardwareInfo.RefreshMemoryStatus();
    }

    public void Dispose()
    {
        _periodicTimer.Dispose();
        _cancellationTokenSource.Dispose();
    }
}