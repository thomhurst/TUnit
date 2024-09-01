using NUnit.Framework;

namespace Tests.Benchmarking;

[Parallelizable(ParallelScope.All)]
public class NUnitTests
{
    [Test]
    public async Task Test1()
    {
        await Task.CompletedTask;
    }
}