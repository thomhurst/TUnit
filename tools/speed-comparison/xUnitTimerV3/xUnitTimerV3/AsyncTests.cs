namespace xUnitTimerV3;

public class AsyncTests
{
    [Fact]
    public async Task SimpleAsyncTest()
    {
        var result = await ComputeAsync(10);
        Assert.Equal(100, result);
        
        var text = await ProcessTextAsync("hello");
        Assert.Equal("HELLO", text);
    }

    [Fact]
    public async Task AsyncStreamOperationsTest()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        using var reader = new StreamReader(stream);
        
        await writer.WriteLineAsync("Line 1");
        await writer.WriteLineAsync("Line 2");
        await writer.WriteLineAsync("Line 3");
        await writer.FlushAsync();
        
        stream.Position = 0;
        
        var lines = new List<string>();
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            lines.Add(line);
        }
        
        Assert.Equal(3, lines.Count);
        Assert.Equal("Line 1", lines[0]);
        Assert.Equal("Line 3", lines[2]);
    }

    [Fact]
    public async Task ConcurrentTasksTest()
    {
        var tasks = new[]
        {
            ComputeAsync(5),
            ComputeAsync(10),
            ComputeAsync(15),
            ComputeAsync(20)
        };
        
        var results = await Task.WhenAll(tasks);
        
        Assert.Equal(4, results.Length);
        Assert.Equal(25, results[0]);
        Assert.Equal(100, results[1]);
        Assert.Equal(225, results[2]);
        Assert.Equal(400, results[3]);
        Assert.Equal(750, results.Sum());
    }

    [Fact]
    public async Task AsyncEnumerableTest()
    {
        var sum = 0;
        var count = 0;
        
        await foreach (var value in GenerateValuesAsync())
        {
            sum += value;
            count++;
        }
        
        Assert.Equal(10, count);
        Assert.Equal(55, sum);
    }

    [Fact]
    public async Task AsyncFileSimulationTest()
    {
        var data = await SimulateFileReadAsync("test.txt");
        var lines = data.Split('\n');
        
        Assert.Equal(5, lines.Length);
        Assert.Equal("Header: Test File", lines[0]);
        Assert.Equal("Footer: End of File", lines[4]);
        
        var processed = await SimulateFileProcessAsync(data);
        Assert.Contains("PROCESSED", processed);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(30)]
    public async Task ParameterizedAsyncTest(int input)
    {
        var result = await ComputeAsync(input);
        Assert.Equal(input * input, result);
        
        var delayed = await DelayedComputeAsync(input, 10);
        Assert.Equal(input + 10, delayed);
    }

    [Fact]
    public async Task AsyncExceptionHandlingTest()
    {
        try
        {
            var result = await SafeComputeAsync(10);
            Assert.Equal(100, result);
            
            result = await SafeComputeAsync(-1);
            Assert.Equal(0, result);
        }
        catch
        {
            Assert.True(false, "Should not throw");
        }
    }

    private async Task<int> ComputeAsync(int value)
    {
        await Task.Yield();
        return value * value;
    }

    private async Task<string> ProcessTextAsync(string text)
    {
        await Task.Yield();
        return text.ToUpper();
    }

    private async IAsyncEnumerable<int> GenerateValuesAsync()
    {
        for (int i = 1; i <= 10; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }

    private async Task<string> SimulateFileReadAsync(string filename)
    {
        await Task.Yield();
        return $"Header: Test File\nLine 1: Data\nLine 2: More Data\nLine 3: Even More Data\nFooter: End of File";
    }

    private async Task<string> SimulateFileProcessAsync(string content)
    {
        await Task.Yield();
        return $"PROCESSED: {content.Length} characters";
    }

    private async Task<int> DelayedComputeAsync(int value, int delay)
    {
        await Task.Delay(1);
        return value + delay;
    }

    private async Task<int> SafeComputeAsync(int value)
    {
        await Task.Yield();
        if (value < 0) return 0;
        return value * value;
    }
}