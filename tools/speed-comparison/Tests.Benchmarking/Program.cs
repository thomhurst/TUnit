using BenchmarkDotNet.Running;
using Tests.Benchmarking;

BenchmarkRunner.Run<Benchmarks>();

var output = new DirectoryInfo(Environment.CurrentDirectory)
    .GetFiles("*.md", SearchOption.AllDirectories)
    .First();
    
Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", await File.ReadAllTextAsync(output.FullName));