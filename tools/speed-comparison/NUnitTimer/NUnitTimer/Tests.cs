using System.Diagnostics;

namespace NUnitTimer;

[Parallelizable(ParallelScope.All)]
public class Tests
{
    private readonly Stopwatch _stopwatch = new();

    [OneTimeSetUp]
    public void Setup()
    {
        _stopwatch.Start();
    }
    
    [OneTimeTearDown]
    public void Teardown()
    {
        Console.WriteLine(_stopwatch.Elapsed);
    }

    [Test, Repeat(1_000)]
    public async Task Test1()
    {
        await Task.Delay(50);
    }
}