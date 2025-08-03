namespace TUnitTimer;

public class RepeatTests
{
    private static int _counter = 0;
    
    [Test]
    [Repeat(100)]
    public void RepeatedCalculationTest()
    {
        var localCounter = Interlocked.Increment(ref _counter);
        var result = PerformCalculation(localCounter);
        
        Assert.That(result).IsGreaterThan(0);
        Assert.That(result % localCounter).IsEqualTo(0);
    }
    
    [Test]
    [Repeat(50)]
    public async Task RepeatedAsyncTest()
    {
        var taskId = Guid.NewGuid();
        var result = await ProcessDataAsync(taskId);
        
        Assert.That(result).IsNotNull();
        Assert.That(result.Length).IsEqualTo(36); // GUID length
        Assert.That(result).IsEqualTo(taskId.ToString());
    }
    
    [Test]
    [Repeat(25)]
    public void RepeatedStringOperationTest()
    {
        var iteration = Interlocked.Increment(ref _counter);
        var text = $"Iteration_{iteration}";
        var processed = ProcessString(text);
        
        Assert.That(processed).Contains("PROCESSED");
        Assert.That(processed).Contains(iteration.ToString());
        Assert.That(processed.Length).IsGreaterThan(text.Length);
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