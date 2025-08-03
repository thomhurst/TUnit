namespace MSTestTimer;

[TestClass]
public class ParallelTests
{
    private static readonly object _lock = new();
    private static int _sharedCounter = 0;
    
    [TestMethod]
    public void ParallelTest1()
    {
        var localData = ProcessData(Enumerable.Range(1, 1000).ToList());
        Assert.AreEqual(500500, localData.Sum());
        
        lock (_lock)
        {
            _sharedCounter++;
        }
    }
    
    [TestMethod]
    public void ParallelTest2()
    {
        var localData = ProcessData(Enumerable.Range(1001, 1000).ToList());
        Assert.AreEqual(1500500, localData.Sum());
        
        lock (_lock)
        {
            _sharedCounter++;
        }
    }
    
    [TestMethod]
    public void ParallelTest3()
    {
        var localData = ProcessData(Enumerable.Range(2001, 1000).ToList());
        Assert.AreEqual(2500500, localData.Sum());
        
        lock (_lock)
        {
            _sharedCounter++;
        }
    }
    
    [TestMethod]
    public void ParallelTest4()
    {
        var localData = ProcessData(Enumerable.Range(3001, 1000).ToList());
        Assert.AreEqual(3500500, localData.Sum());
        
        lock (_lock)
        {
            _sharedCounter++;
        }
    }
    
    [TestMethod]
    public void ParallelTest5()
    {
        var localData = ProcessData(Enumerable.Range(4001, 1000).ToList());
        Assert.AreEqual(4500500, localData.Sum());
        
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

[TestClass]
[DoNotParallelize]
public class SequentialTests
{
    private static int _sequentialCounter = 0;
    private static readonly List<int> _executionOrder = new();
    
    [TestMethod]
    [DoNotParallelize]
    public void SequentialTest1()
    {
        _executionOrder.Add(1);
        _sequentialCounter++;
        
        Assert.AreEqual(_executionOrder.Count, _sequentialCounter);
        Thread.Sleep(5); // Simulate work
    }
    
    [TestMethod]
    [DoNotParallelize]
    public void SequentialTest2()
    {
        _executionOrder.Add(2);
        _sequentialCounter++;
        
        Assert.AreEqual(_executionOrder.Count, _sequentialCounter);
        Thread.Sleep(5); // Simulate work
    }
    
    [TestMethod]
    [DoNotParallelize]
    public void SequentialTest3()
    {
        _executionOrder.Add(3);
        _sequentialCounter++;
        
        Assert.AreEqual(_executionOrder.Count, _sequentialCounter);
        Thread.Sleep(5); // Simulate work
    }
}

[TestClass]
public class ThreadSafeTests
{
    private static readonly ConcurrentDictionary<int, string> _concurrentData = new();
    private static readonly ConcurrentBag<int> _processedItems = new();
    
    [DataTestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    public void ConcurrentCollectionTest(int id)
    {
        var key = Thread.CurrentThread.ManagedThreadId * 1000 + id;
        _concurrentData[key] = $"Thread_{Thread.CurrentThread.ManagedThreadId}_Item_{id}";
        _processedItems.Add(key);
        
        Assert.IsTrue(_concurrentData.ContainsKey(key));
        Assert.IsTrue(_concurrentData[key].Contains($"Item_{id}"));
        
        // Simulate some work
        var sum = Enumerable.Range(1, 100).Select(x => x * id).Sum();
        Assert.AreEqual(5050 * id, sum);
    }
    
    [TestMethod]
    public async Task ParallelAsyncTest()
    {
        var tasks = Enumerable.Range(1, 5).Select(async i =>
        {
            await Task.Yield();
            var result = await ProcessAsync(i);
            return result;
        });
        
        var results = await Task.WhenAll(tasks);
        
        Assert.AreEqual(55, results.Sum());
        Assert.AreEqual(5, results.Length);
        Assert.IsTrue(results.All(r => r > 0));
    }
    
    private async Task<int> ProcessAsync(int value)
    {
        await Task.Delay(1);
        return value * value;
    }
}