using System.Diagnostics;
using TUnit.Core;

namespace Tests.Benchmarking;

public class TUnitTests
{
    private static readonly Stopwatch Stopwatch = new();

    [Before(HookType.Class)]
    public static void Setup()
    {
        Stopwatch.Start();
    }
    
    [After(HookType.Class)]
    public static void Teardown()
    {
        Console.WriteLine(Stopwatch.Elapsed);
    }

    [Test, Repeat(1_000)]
    public async Task Test1()
    {
        await Task.Delay(50);
    }
}