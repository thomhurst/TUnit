using BenchmarkDotNet.Attributes;
using TUnit.Engine.Services;
using Xunit.Runners;

namespace Tests.Benchmarking;

[MarkdownExporterAttribute.GitHub]
public class Benchmarks
{
    private static readonly string XUnitAssemblyFileName = typeof(xUnitTests).Assembly.Location;

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
        using var finished = new ManualResetEvent(false);
        
        var runner = AssemblyRunner.WithoutAppDomain(XUnitAssemblyFileName);
        
        runner.OnExecutionComplete += _ => finished.Set();
        
        runner.Start();

        finished.WaitOne();
    }
}