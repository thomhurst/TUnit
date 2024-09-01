using System.Diagnostics;

namespace Tests.Benchmarking;

public class Timer : IDisposable
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public void Dispose()
    {
        Console.WriteLine(_stopwatch.Elapsed);
    }
}