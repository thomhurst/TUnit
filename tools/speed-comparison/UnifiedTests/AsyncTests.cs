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
#elif XUNIT
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
#elif XUNIT
        Assert.Equal(100, result);
#elif NUNIT
        Assert.That(result, Is.EqualTo(100));
#elif MSTEST
        Assert.AreEqual(100, result);
#endif

        var text = await ProcessTextAsync("hello");
#if TUNIT
        await Assert.That(text).IsEqualTo("HELLO");
#elif XUNIT
        Assert.Equal("HELLO", text);
#elif NUNIT
        Assert.That(text, Is.EqualTo("HELLO"));
#elif MSTEST
        Assert.AreEqual("HELLO", text);
#endif
    }

#if TUNIT
    [Test]
    public async Task ParallelAsyncOperationsTest()
#elif XUNIT
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

#if TUNIT
        await Assert.That(results).HasCount(10);
        await Assert.That(sum).IsEqualTo(285);
#elif XUNIT
        Assert.Equal(10, results.Length);
        Assert.Equal(285, sum);
#elif NUNIT
        Assert.That(results.Length, Is.EqualTo(10));
        Assert.That(sum, Is.EqualTo(285));
#elif MSTEST
        Assert.AreEqual(10, results.Length);
        Assert.AreEqual(285, sum);
#endif
    }

    private async Task<int> ComputeAsync(int value)
    {
        await Task.Delay(1);
        return value * value;
    }

    private async Task<string> ProcessTextAsync(string text)
    {
        await Task.Delay(1);
        return text.ToUpper();
    }
}