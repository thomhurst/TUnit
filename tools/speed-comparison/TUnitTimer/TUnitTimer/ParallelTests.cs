namespace TUnitTimer;

public class ParallelTests
{
    private static readonly object _lock = new();
    private static int _sharedCounter = 0;
    
    [Test]
    public void ParallelTest1()
    {
        var localData = ProcessData(Enumerable.Range(1, 1000).ToList());
        Assert.That(localData.Sum()).IsEqualTo(500500);
        
        lock (_lock)
        {
            _sharedCounter++;
        }
    }
    
    [Test]
    public void ParallelTest2()
    {
        var localData = ProcessData(Enumerable.Range(1001, 1000).ToList());
        Assert.That(localData.Sum()).IsEqualTo(1500500);
        
        lock (_lock)
        {
            _sharedCounter++;
        }
    }
    
    [Test]
    public void ParallelTest3()
    {
        var localData = ProcessData(Enumerable.Range(2001, 1000).ToList());
        Assert.That(localData.Sum()).IsEqualTo(2500500);
        
        lock (_lock)
        {
            _sharedCounter++;
        }
    }
    
    [Test]
    public void ParallelTest4()
    {
        var localData = ProcessData(Enumerable.Range(3001, 1000).ToList());
        Assert.That(localData.Sum()).IsEqualTo(3500500);
        
        lock (_lock)
        {
            _sharedCounter++;
        }
    }
    
    [Test]
    public void ParallelTest5()
    {
        var localData = ProcessData(Enumerable.Range(4001, 1000).ToList());
        Assert.That(localData.Sum()).IsEqualTo(4500500);
        
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

[NotInParallel("sequential-group")]
public class SequentialTests
{
    private static int _sequentialCounter = 0;
    private static readonly List<int> _executionOrder = new();
    
    [Test]
    [NotInParallel("sequential-group")]
    public void SequentialTest1()
    {
        _executionOrder.Add(1);
        _sequentialCounter++;
        
        Assert.That(_sequentialCounter).IsEqualTo(_executionOrder.Count);
        Thread.Sleep(5); // Simulate work
    }
    
    [Test]
    [NotInParallel("sequential-group")]
    public void SequentialTest2()
    {
        _executionOrder.Add(2);
        _sequentialCounter++;
        
        Assert.That(_sequentialCounter).IsEqualTo(_executionOrder.Count);
        Thread.Sleep(5); // Simulate work
    }
    
    [Test]
    [NotInParallel("sequential-group")]
    public void SequentialTest3()
    {
        _executionOrder.Add(3);
        _sequentialCounter++;
        
        Assert.That(_sequentialCounter).IsEqualTo(_executionOrder.Count);
        Thread.Sleep(5); // Simulate work
    }
}

public class ThreadSafeTests
{
    private static readonly ConcurrentDictionary<int, string> _concurrentData = new();
    private static readonly ConcurrentBag<int> _processedItems = new();
    
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    public void ConcurrentCollectionTest(int id)
    {
        var key = Thread.CurrentThread.ManagedThreadId * 1000 + id;
        _concurrentData[key] = $"Thread_{Thread.CurrentThread.ManagedThreadId}_Item_{id}";
        _processedItems.Add(key);
        
        Assert.That(_concurrentData.ContainsKey(key)).IsTrue();
        Assert.That(_concurrentData[key]).Contains($"Item_{id}");
        
        // Simulate some work
        var sum = Enumerable.Range(1, 100).Select(x => x * id).Sum();
        Assert.That(sum).IsEqualTo(5050 * id);
    }
    
    [Test]
    public async Task ParallelAsyncTest()
    {
        var tasks = Enumerable.Range(1, 5).Select(async i =>
        {
            await Task.Yield();
            var result = await ProcessAsync(i);
            return result;
        });
        
        var results = await Task.WhenAll(tasks);
        
        Assert.That(results.Sum()).IsEqualTo(55);
        Assert.That(results).HasCount(5);
        Assert.That(results.All(r => r > 0)).IsTrue();
    }
    
    private async Task<int> ProcessAsync(int value)
    {
        await Task.Delay(1);
        return value * value;
    }
}