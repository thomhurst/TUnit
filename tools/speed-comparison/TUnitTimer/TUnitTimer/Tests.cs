using System.Diagnostics;

namespace TUnitTimer;

public class Tests
{
    private static Stopwatch _stopwatch;

    [Before(Class)]
    public static void Setup()
    {
        _stopwatch = Stopwatch.StartNew();
    }
    
    [After(Class)]
    public static void Teardown()
    {
        Console.WriteLine(_stopwatch.Elapsed);
    }

    [Test, Repeat(1_000)]
    public async Task Test1()
    {
        await Task.Delay(50);
    }
}