namespace NUnitTimer;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class RepeatTests
{
    private static int _counter = 0;
    
    [Test]
    [Repeat(100)]
    public void RepeatedCalculationTest()
    {
        var localCounter = Interlocked.Increment(ref _counter);
        var result = PerformCalculation(localCounter);
        
        Assert.That(result, Is.GreaterThan(0));
        Assert.That(result % localCounter, Is.EqualTo(0));
    }
    
    [Test]
    [Repeat(50)]
    public async Task RepeatedAsyncTest()
    {
        var taskId = Guid.NewGuid();
        var result = await ProcessDataAsync(taskId);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.EqualTo(36)); // GUID length
        Assert.That(result, Is.EqualTo(taskId.ToString()));
    }
    
    [Test]
    [Repeat(25)]
    public void RepeatedStringOperationTest()
    {
        var iteration = Interlocked.Increment(ref _counter);
        var text = $"Iteration_{iteration}";
        var processed = ProcessString(text);
        
        Assert.That(processed, Does.Contain("PROCESSED"));
        Assert.That(processed, Does.Contain(iteration.ToString()));
        Assert.That(processed.Length, Is.GreaterThan(text.Length));
    }
    
    private int PerformCalculation(int input)
    {
        var result = 0;
        for (int i = 1; i <= input; i++)
        {
            result += i;
        }
        return result;
    }
    
    private async Task<string> ProcessDataAsync(Guid id)
    {
        await Task.Yield();
        return id.ToString();
    }
    
    private string ProcessString(string input)
    {
        return $"PROCESSED_{input.ToUpper()}_{input.Length}";
    }
}