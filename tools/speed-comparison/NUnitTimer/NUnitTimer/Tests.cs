using System.Diagnostics;

namespace NUnitTimer;

[Parallelizable(ParallelScope.All)]
public class Tests
{
    private Stopwatch _stopwatch;

    [OneTimeSetUp]
    public void Setup()
    {
        _stopwatch = Stopwatch.StartNew();
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