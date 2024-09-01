using System.Diagnostics;
using NUnit.Framework;

namespace Tests.Benchmarking;

[Parallelizable(ParallelScope.All)]
public class NUnitTests
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