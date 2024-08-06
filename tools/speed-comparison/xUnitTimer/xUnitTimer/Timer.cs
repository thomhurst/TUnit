using System.Diagnostics;

namespace xUnitTimer;

public class Timer : IDisposable
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public void Dispose()
    {
        Console.WriteLine(_stopwatch.Elapsed);
    }
}