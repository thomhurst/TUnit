using BenchmarkDotNet.Running;
using Tests.Benchmark;

BenchmarkRunner.Run<Benchmarks>();

var output = new DirectoryInfo(Environment.CurrentDirectory)
    .GetFiles("*.md", SearchOption.AllDirectories)
    .OrderBy(x => x.Name)
    .First();

var file = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");

if (!string.IsNullOrEmpty(file))
{
    await File.WriteAllTextAsync(file, await File.ReadAllTextAsync(output.FullName));
}