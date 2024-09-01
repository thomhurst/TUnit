using BenchmarkDotNet.Attributes;
using TUnit.Engine.Services;
using Xunit.Runners;

namespace Tests.Benchmarking;

[MarkdownExporterAttribute.GitHub]
public class Benchmarks
{
    private static readonly ManualResetEvent Finished = new ManualResetEvent(false);
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
        var runner = AssemblyRunner.WithoutAppDomain(XUnitAssemblyFileName);
        
        runner.OnExecutionComplete += _ => Finished.Set();
        
        runner.Start();

        Finished.WaitOne();
    }
}