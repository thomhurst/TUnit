using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace UnifiedTests;

#if MSTEST
[TestClass]
#elif NUNIT
[TestFixture]
[Parallelizable(ParallelScope.All)]
#endif
public class ParallelTests
{
    private static readonly object _lock = new();
    private static int _sharedCounter = 0;

#if TUNIT
    [Test]
    public async Task ParallelTest1()
#elif XUNIT
    [Fact]
    public void ParallelTest1()
#elif NUNIT
    [Test]
    public void ParallelTest1()
#elif MSTEST
    [TestMethod]
    public void ParallelTest1()
#endif
    {
        var localData = ProcessData(Enumerable.Range(1, 1000).ToList());
#if TUNIT
        await Assert.That(localData.Sum()).IsEqualTo(500500);
#elif XUNIT
        Assert.Equal(500500, localData.Sum());
#elif NUNIT
        Assert.That(localData.Sum(), Is.EqualTo(500500));
#elif MSTEST
        Assert.AreEqual(500500, localData.Sum());
#endif

        lock (_lock)
        {
            _sharedCounter++;
        }
    }

#if TUNIT
    [Test]
    public async Task ParallelTest2()
#elif XUNIT
    [Fact]
    public void ParallelTest2()
#elif NUNIT
    [Test]
    public void ParallelTest2()
#elif MSTEST
    [TestMethod]
    public void ParallelTest2()
#endif
    {
        var localData = ProcessData(Enumerable.Range(1001, 1000).ToList());
#if TUNIT
        await Assert.That(localData.Sum()).IsEqualTo(1500500);
#elif XUNIT
        Assert.Equal(1500500, localData.Sum());
#elif NUNIT
        Assert.That(localData.Sum(), Is.EqualTo(1500500));
#elif MSTEST
        Assert.AreEqual(1500500, localData.Sum());
#endif

        lock (_lock)
        {
            _sharedCounter++;
        }
    }

#if TUNIT
    [Test]
    public async Task ParallelTest3()
#elif XUNIT
    [Fact]
    public void ParallelTest3()
#elif NUNIT
    [Test]
    public void ParallelTest3()
#elif MSTEST
    [TestMethod]
    public void ParallelTest3()
#endif
    {
        var localData = ProcessData(Enumerable.Range(2001, 1000).ToList());
#if TUNIT
        await Assert.That(localData.Sum()).IsEqualTo(2500500);
#elif XUNIT
        Assert.Equal(2500500, localData.Sum());
#elif NUNIT
        Assert.That(localData.Sum(), Is.EqualTo(2500500));
#elif MSTEST
        Assert.AreEqual(2500500, localData.Sum());
#endif

        lock (_lock)
        {
            _sharedCounter++;
        }
    }

#if TUNIT
    [Test]
    public async Task ParallelTest4()
#elif XUNIT
    [Fact]
    public void ParallelTest4()
#elif NUNIT
    [Test]
    public void ParallelTest4()
#elif MSTEST
    [TestMethod]
    public void ParallelTest4()
#endif
    {
        var localData = ProcessData(Enumerable.Range(3001, 1000).ToList());
#if TUNIT
        await Assert.That(localData.Sum()).IsEqualTo(3500500);
#elif XUNIT
        Assert.Equal(3500500, localData.Sum());
#elif NUNIT
        Assert.That(localData.Sum(), Is.EqualTo(3500500));
#elif MSTEST
        Assert.AreEqual(3500500, localData.Sum());
#endif

        lock (_lock)
        {
            _sharedCounter++;
        }
    }

#if TUNIT
    [Test]
    public async Task ParallelTest5()
#elif XUNIT
    [Fact]
    public void ParallelTest5()
#elif NUNIT
    [Test]
    public void ParallelTest5()
#elif MSTEST
    [TestMethod]
    public void ParallelTest5()
#endif
    {
        var localData = ProcessData(Enumerable.Range(4001, 1000).ToList());
#if TUNIT
        await Assert.That(localData.Sum()).IsEqualTo(4500500);
#elif XUNIT
        Assert.Equal(4500500, localData.Sum());
#elif NUNIT
        Assert.That(localData.Sum(), Is.EqualTo(4500500));
#elif MSTEST
        Assert.AreEqual(4500500, localData.Sum());
#endif

        lock (_lock)
        {
            _sharedCounter++;
        }
    }

    private List<int> ProcessData(List<int> input)
    {
        // Simulate some CPU work
        var result = new List<int>(input.Count);
        foreach (var item in input)
        {
            result.Add(item);
        }
        result.Sort();
        return result;
    }
}

#if TUNIT
[NotInParallel("sequential-group")]
public class SequentialTests
#elif NUNIT
[TestFixture]
[NonParallelizable]
public class SequentialTests
#elif MSTEST
[TestClass]
[DoNotParallelize]
public class SequentialTests
#elif XUNIT
[Collection("Sequential")]
public class SequentialTests
#endif
{
    private static int _sequentialCounter = 0;
    private static readonly List<int> _executionOrder = new();

#if TUNIT
    [Test]
    [NotInParallel("sequential-group")]
    public async Task SequentialTest1()
#elif XUNIT
    [Fact]
    public void SequentialTest1()
#elif NUNIT
    [Test]
    public void SequentialTest1()
#elif MSTEST
    [TestMethod]
    public void SequentialTest1()
#endif
    {
        _executionOrder.Add(1);
        _sequentialCounter++;

#if TUNIT
        await Assert.That(_sequentialCounter).IsEqualTo(_executionOrder.Count);
#elif XUNIT
        Assert.Equal(_executionOrder.Count, _sequentialCounter);
#elif NUNIT
        Assert.That(_sequentialCounter, Is.EqualTo(_executionOrder.Count));
#elif MSTEST
        Assert.AreEqual(_executionOrder.Count, _sequentialCounter);
#endif
        Thread.Sleep(5); // Simulate work
    }

#if TUNIT
    [Test]
    [NotInParallel("sequential-group")]
    public async Task SequentialTest2()
#elif XUNIT
    [Fact]
    public void SequentialTest2()
#elif NUNIT
    [Test]
    public void SequentialTest2()
#elif MSTEST
    [TestMethod]
    public void SequentialTest2()
#endif
    {
        _executionOrder.Add(2);
        _sequentialCounter++;

#if TUNIT
        await Assert.That(_sequentialCounter).IsEqualTo(_executionOrder.Count);
#elif XUNIT
        Assert.Equal(_executionOrder.Count, _sequentialCounter);
#elif NUNIT
        Assert.That(_sequentialCounter, Is.EqualTo(_executionOrder.Count));
#elif MSTEST
        Assert.AreEqual(_executionOrder.Count, _sequentialCounter);
#endif
        Thread.Sleep(5); // Simulate work
    }

#if TUNIT
    [Test]
    [NotInParallel("sequential-group")]
    public async Task SequentialTest3()
#elif XUNIT
    [Fact]
    public void SequentialTest3()
#elif NUNIT
    [Test]
    public void SequentialTest3()
#elif MSTEST
    [TestMethod]
    public void SequentialTest3()
#endif
    {
        _executionOrder.Add(3);
        _sequentialCounter++;

#if TUNIT
        await Assert.That(_sequentialCounter).IsEqualTo(_executionOrder.Count);
#elif XUNIT
        Assert.Equal(_executionOrder.Count, _sequentialCounter);
#elif NUNIT
        Assert.That(_sequentialCounter, Is.EqualTo(_executionOrder.Count));
#elif MSTEST
        Assert.AreEqual(_executionOrder.Count, _sequentialCounter);
#endif
        Thread.Sleep(5); // Simulate work
    }
}

#if MSTEST
[TestClass]
#elif NUNIT
[TestFixture]
#endif
public class ThreadSafeTests
{
    private static readonly ConcurrentDictionary<int, string> _concurrentData = new();
    private static readonly ConcurrentBag<int> _processedItems = new();

#if TUNIT
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    public async Task ConcurrentCollectionTest(int id)
#elif XUNIT
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void ConcurrentCollectionTest(int id)
#elif NUNIT
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void ConcurrentCollectionTest(int id)
#elif MSTEST
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    public void ConcurrentCollectionTest(int id)
#endif
    {
        var key = Thread.CurrentThread.ManagedThreadId * 1000 + id;
        _concurrentData[key] = $"Thread_{Thread.CurrentThread.ManagedThreadId}_Item_{id}";
        _processedItems.Add(key);

#if TUNIT
        await Assert.That(_concurrentData.ContainsKey(key)).IsTrue();
        await Assert.That(_concurrentData[key]).Contains($"Item_{id}");
#elif XUNIT
        Assert.True(_concurrentData.ContainsKey(key));
        Assert.Contains($"Item_{id}", _concurrentData[key]);
#elif NUNIT
        Assert.That(_concurrentData.ContainsKey(key), Is.True);
        Assert.That(_concurrentData[key], Does.Contain($"Item_{id}"));
#elif MSTEST
        Assert.IsTrue(_concurrentData.ContainsKey(key));
        Assert.IsTrue(_concurrentData[key].Contains($"Item_{id}"));
#endif

        // Simulate some work
        var sum = Enumerable.Range(1, 100).Select(x => x * id).Sum();
#if TUNIT
        await Assert.That(sum).IsEqualTo(5050 * id);
#elif XUNIT
        Assert.Equal(5050 * id, sum);
#elif NUNIT
        Assert.That(sum, Is.EqualTo(5050 * id));
#elif MSTEST
        Assert.AreEqual(5050 * id, sum);
#endif
    }

#if TUNIT
    [Test]
    public async Task ParallelAsyncTest()
#elif XUNIT
    [Fact]
    public async Task ParallelAsyncTest()
#elif NUNIT
    [Test]
    public async Task ParallelAsyncTest()
#elif MSTEST
    [TestMethod]
    public async Task ParallelAsyncTest()
#endif
    {
        var tasks = Enumerable.Range(1, 5).Select(async i =>
        {
            await Task.Yield();
            var result = await ProcessAsync(i);
            return result;
        });

        var results = await Task.WhenAll(tasks);

#if TUNIT
        await Assert.That(results.Sum()).IsEqualTo(55);
        await Assert.That(results).HasCount(5);
        await Assert.That(results.All(r => r > 0)).IsTrue();
#elif XUNIT
        Assert.Equal(55, results.Sum());
        Assert.Equal(5, results.Length);
        Assert.True(results.All(r => r > 0));
#elif NUNIT
        Assert.That(results.Sum(), Is.EqualTo(55));
        Assert.That(results.Length, Is.EqualTo(5));
        Assert.That(results.All(r => r > 0), Is.True);
#elif MSTEST
        Assert.AreEqual(55, results.Sum());
        Assert.AreEqual(5, results.Length);
        Assert.IsTrue(results.All(r => r > 0));
#endif
    }

    private async Task<int> ProcessAsync(int value)
    {
        await Task.Delay(1);
        return value * value;
    }
}