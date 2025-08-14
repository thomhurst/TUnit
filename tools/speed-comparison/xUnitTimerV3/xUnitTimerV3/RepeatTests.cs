namespace xUnitTimerV3;

public class RepeatTests
{
    private static int _counter = 0;
    
    public static IEnumerable<object[]> RepeatData100()
    {
        return Enumerable.Range(0, 100).Select(i => new object[] { i });
    }
    
    public static IEnumerable<object[]> RepeatData50()
    {
        return Enumerable.Range(0, 50).Select(i => new object[] { i });
    }
    
    public static IEnumerable<object[]> RepeatData25()
    {
        return Enumerable.Range(0, 25).Select(i => new object[] { i });
    }
    
    [Theory]
    [MemberData(nameof(RepeatData100))]
    public void RepeatedCalculationTest(int iteration)
    {
        var localCounter = Interlocked.Increment(ref _counter);
        var result = PerformCalculation(localCounter);
        
        Assert.True(result > 0);
        Assert.Equal(0, result % localCounter);
    }
    
    [Theory]
    [MemberData(nameof(RepeatData50))]
    public async Task RepeatedAsyncTest(int iteration)
    {
        var taskId = Guid.NewGuid();
        var result = await ProcessDataAsync(taskId);
        
        Assert.NotNull(result);
        Assert.Equal(36, result.Length); // GUID length
        Assert.Equal(taskId.ToString(), result);
    }
    
    [Theory]
    [MemberData(nameof(RepeatData25))]
    public void RepeatedStringOperationTest(int iteration)
    {
        var localIteration = Interlocked.Increment(ref _counter);
        var text = $"Iteration_{localIteration}";
        var processed = ProcessString(text);
        
        Assert.Contains("PROCESSED", processed);
        Assert.Contains(localIteration.ToString(), processed);
        Assert.True(processed.Length > text.Length);
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