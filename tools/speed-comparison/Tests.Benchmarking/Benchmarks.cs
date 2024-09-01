using BenchmarkDotNet.Attributes;
using TUnit.Engine.Services;
using Xunit.Runners;

namespace Tests.Benchmarking;

[MarkdownExporterAttribute.GitHub]
public class Benchmarks
{
    private static readonly ManualResetEvent Finished = new ManualResetEvent(false);

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
        var runner = AssemblyRunner.WithoutAppDomain(typeof(xUnitTests).Assembly.Location);
        
        runner.OnExecutionComplete += _ => Finished.Set();
        
        runner.Start(new AssemblyRunnerStartOptions());

        Finished.WaitOne();
    }
}