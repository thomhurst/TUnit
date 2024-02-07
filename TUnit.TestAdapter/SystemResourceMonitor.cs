using System.Diagnostics;
using System.Runtime.CompilerServices;
using Hardware.Info;

namespace TUnit.TestAdapter;

internal class SystemResourceMonitor : IDisposable
{
    private static readonly HardwareInfo HardwareInfo = new();
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromSeconds(5));
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private static double _fallbackCpuPercentage;

    [ModuleInitializer]
    public static void Initialize()
    {
        Task.Run(Refresh);
        
        // According to Hardware.Info, there might be a 21 second delay on getting CPU stats
        // So let's just run this 50 times (as it contains a 500ms delay in it) which means it'll stop
        // after around 25 seconds
        Task.Run(async () =>
        {
            for (var i = 0; i < 50; i++)
            {
                _fallbackCpuPercentage = await GetCpuUsagePercentageForProcess();
            }
        });
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
            return _fallbackCpuPercentage > 90;
        }

        return HardwareInfo.CpuList.All(x => x.PercentProcessorTime > 90);
    }
    
    private static async Task<double> GetCpuUsagePercentageForProcess()
    {
        var startTime = DateTime.UtcNow;
        
        var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
        
        await Task.Delay(500);
    
        var endTime = DateTime.UtcNow;
        
        var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
        
        var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
        
        var totalMsPassed = (endTime - startTime).TotalMilliseconds;
        
        var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
        
        return cpuUsageTotal * 100;
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