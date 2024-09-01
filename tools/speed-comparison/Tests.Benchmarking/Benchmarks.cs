using BenchmarkDotNet.Attributes;
using TUnit.Engine.Services;
using Xunit.Runners;

namespace Tests.Benchmarking;

[MarkdownExporterAttribute.GitHub]
public class Benchmarks
{
    private TextWriter _defaultOut;

    [GlobalSetup]
    public void DisableConsole()
    {
        _defaultOut = Console.Out;
        Console.SetOut(TextWriter.Null);
    }

    [GlobalCleanup]
    public void EnableConsole()
    {
        Console.SetOut(_defaultOut);
    }
    
    [Benchmark]
    public async Task TUnit()
    {
        await TUnitRunner.RunTests();
    }
    
    [Benchmark]
    public void NUnit()
    {
        new NUnitLite.AutoRun().Execute([]);
    }
    
    [Benchmark]
    public void xUnit()
    {
        using var runner = AssemblyRunner.WithoutAppDomain(typeof(xUnitTests).Assembly.GetName().Name);
        
        using var finished = new ManualResetEvent(false);

        runner.OnExecutionComplete += _ => finished.Set();
        
        runner.Start(new AssemblyRunnerStartOptions());

        finished.WaitOne();
    }
}