using System.Collections.Concurrent;

namespace xUnitTimerV3;

public class ParallelTests
{
    private static readonly object _lock = new();
    private static int _sharedCounter = 0;
    
    [Fact]
    public void ParallelTest1()
    {
        var localData = ProcessData(Enumerable.Range(1, 1000).ToList());
        Assert.Equal(500500, localData.Sum());
        
        lock (_lock)
        {
            _sharedCounter++;
        }
    }
    
    [Fact]
    public void ParallelTest2()
    {
        var localData = ProcessData(Enumerable.Range(1001, 1000).ToList());
        Assert.Equal(1500500, localData.Sum());
        
        lock (_lock)
        {
            _sharedCounter++;
        }
    }
    
    [Fact]
    public void ParallelTest3()
    {
        var localData = ProcessData(Enumerable.Range(2001, 1000).ToList());
        Assert.Equal(2500500, localData.Sum());
        
        lock (_lock)
        {
            _sharedCounter++;
        }
    }
    
    [Fact]
    public void ParallelTest4()
    {
        var localData = ProcessData(Enumerable.Range(3001, 1000).ToList());
        Assert.Equal(3500500, localData.Sum());
        
        lock (_lock)
        {
            _sharedCounter++;
        }
    }
    
    [Fact]
    public void ParallelTest5()
    {
        var localData = ProcessData(Enumerable.Range(4001, 1000).ToList());
        Assert.Equal(4500500, localData.Sum());
        
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

[Collection("Sequential")]
public class SequentialTests
{
    private static int _sequentialCounter = 0;
    private static readonly List<int> _executionOrder = new();
    
    [Fact]
    public void SequentialTest1()
    {
        _executionOrder.Add(1);
        _sequentialCounter++;
        
        Assert.Equal(_executionOrder.Count, _sequentialCounter);
        Thread.Sleep(5); // Simulate work
    }
    
    [Fact]
    public void SequentialTest2()
    {
        _executionOrder.Add(2);
        _sequentialCounter++;
        
        Assert.Equal(_executionOrder.Count, _sequentialCounter);
        Thread.Sleep(5); // Simulate work
    }
    
    [Fact]
    public void SequentialTest3()
    {
        _executionOrder.Add(3);
        _sequentialCounter++;
        
        Assert.Equal(_executionOrder.Count, _sequentialCounter);
        Thread.Sleep(5); // Simulate work
    }
}

public class ThreadSafeTests
{
    private static readonly ConcurrentDictionary<int, string> _concurrentData = new();
    private static readonly ConcurrentBag<int> _processedItems = new();
    
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void ConcurrentCollectionTest(int id)
    {
        var key = Thread.CurrentThread.ManagedThreadId * 1000 + id;
        _concurrentData[key] = $"Thread_{Thread.CurrentThread.ManagedThreadId}_Item_{id}";
        _processedItems.Add(key);
        
        Assert.True(_concurrentData.ContainsKey(key));
        Assert.Contains($"Item_{id}", _concurrentData[key]);
        
        // Simulate some work
        var sum = Enumerable.Range(1, 100).Select(x => x * id).Sum();
        Assert.Equal(5050 * id, sum);
    }
    
    [Fact]
    public async Task ParallelAsyncTest()
    {
        var tasks = Enumerable.Range(1, 5).Select(async i =>
        {
            await Task.Yield();
            var result = await ProcessAsync(i);
            return result;
        });
        
        var results = await Task.WhenAll(tasks);
        
        Assert.Equal(55, results.Sum());
        Assert.Equal(5, results.Length);
        Assert.All(results, r => Assert.True(r > 0));
    }
    
    private async Task<int> ProcessAsync(int value)
    {
        await Task.Delay(1);
        return value * value;
    }
}