using System.Threading.Tasks;

namespace UnifiedTests;

[TestClass]
public class AsyncTests
{
    [Test]
    public async Task SimpleAsyncTest()
    {
        var result = await ComputeAsync(10);
        var text = await ProcessTextAsync("hello");
        var combined = result + text.Length;
    }

    [Test]
    public async Task ParallelAsyncOperationsTest()
    {
        var tasks = Enumerable.Range(0, 10)
            .Select(i => ComputeAsync(i))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var sum = results.Sum();
        var average = sum / results.Length;
    }

    private async Task<int> ComputeAsync(int value)
    {
        await Task.Delay(50);
        return value * value;
    }

    private async Task<string> ProcessTextAsync(string text)
    {
        await Task.Delay(50);
        return text.ToUpper();
    }
}
