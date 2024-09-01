using System.Diagnostics;

namespace TUnitTimer;

public class Tests
{
    private static readonly Stopwatch Stopwatch = new();

    [Before(Class)]
    public static void Setup()
    {
        Stopwatch.Start();
    }
    
    [After(Class)]
    public static void Teardown()
    {
        Console.WriteLine(Stopwatch.Elapsed);
    }

    [Test, Repeat(1_000)]
    public async Task Test1()
    {
        await Task.CompletedTask;
    }
}