using System.Threading.Tasks;

namespace UnifiedTests;

#if MSTEST
[TestClass]
#elif NUNIT
[TestFixture]
#endif
public class AsyncTests
{
#if TUNIT
    [Test]
    public async Task SimpleAsyncTest()
#elif XUNIT || XUNIT3
    [Fact]
    public async Task SimpleAsyncTest()
#elif NUNIT
    [Test]
    public async Task SimpleAsyncTest()
#elif MSTEST
    [TestMethod]
    public async Task SimpleAsyncTest()
#endif
    {
        var result = await ComputeAsync(10);
#if TUNIT
        await Assert.That(result).IsEqualTo(100);
#elif XUNIT || XUNIT3
        Assert.Equal(100, result);
#elif NUNIT
        Assert.That(result, Is.EqualTo(100));
#elif MSTEST
        Assert.AreEqual(100, result);
#endif

        await ProcessTextAsync("hello");
    }

#if TUNIT
    [Test]
    public async Task ParallelAsyncOperationsTest()
#elif XUNIT || XUNIT3
    [Fact]
    public async Task ParallelAsyncOperationsTest()
#elif NUNIT
    [Test]
    public async Task ParallelAsyncOperationsTest()
#elif MSTEST
    [TestMethod]
    public async Task ParallelAsyncOperationsTest()
#endif
    {
        var tasks = Enumerable.Range(0, 10)
            .Select(i => ComputeAsync(i))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var sum = results.Sum();
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
