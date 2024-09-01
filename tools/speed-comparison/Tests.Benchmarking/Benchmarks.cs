using BenchmarkDotNet.Attributes;
using TUnit.Engine.Services;
using Xunit.Runners;

public class Benchmarks
{
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
        using var runner = AssemblyRunner.WithoutAppDomain(typeof(xUnitTimer.Tests).Assembly.FullName);
        
        using var finished = new ManualResetEvent(false);

        runner.OnExecutionComplete += _ =>  finished.Set();
        
        runner.Start(new AssemblyRunnerStartOptions());

        finished.WaitOne();
    }
    
    [Benchmark]
    public void MSTest()
    {
        Microsoft.VisualStudio.TestPlatform.TestHost.Program.Main([]);
    }
}