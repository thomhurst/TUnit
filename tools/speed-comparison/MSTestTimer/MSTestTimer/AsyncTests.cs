namespace MSTestTimer;

[TestClass]
public class AsyncTests
{
    [TestMethod]
    public async Task SimpleAsyncTest()
    {
        var result = await ComputeAsync(10);
        Assert.AreEqual(100, result);
        
        var text = await ProcessTextAsync("hello");
        Assert.AreEqual("HELLO", text);
    }

    [TestMethod]
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
        
        Assert.AreEqual(3, lines.Count);
        Assert.AreEqual("Line 1", lines[0]);
        Assert.AreEqual("Line 3", lines[2]);
    }

    [TestMethod]
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
        
        Assert.AreEqual(4, results.Length);
        Assert.AreEqual(25, results[0]);
        Assert.AreEqual(100, results[1]);
        Assert.AreEqual(225, results[2]);
        Assert.AreEqual(400, results[3]);
        Assert.AreEqual(750, results.Sum());
    }

    [TestMethod]
    public async Task AsyncEnumerableTest()
    {
        var sum = 0;
        var count = 0;
        
        await foreach (var value in GenerateValuesAsync())
        {
            sum += value;
            count++;
        }
        
        Assert.AreEqual(10, count);
        Assert.AreEqual(55, sum);
    }

    [TestMethod]
    public async Task AsyncFileSimulationTest()
    {
        var data = await SimulateFileReadAsync("test.txt");
        var lines = data.Split('\n');
        
        Assert.AreEqual(5, lines.Length);
        Assert.AreEqual("Header: Test File", lines[0]);
        Assert.AreEqual("Footer: End of File", lines[4]);
        
        var processed = await SimulateFileProcessAsync(data);
        Assert.IsTrue(processed.Contains("PROCESSED"));
    }

    [DataTestMethod]
    [DataRow(10)]
    [DataRow(20)]
    [DataRow(30)]
    public async Task ParameterizedAsyncTest(int input)
    {
        var result = await ComputeAsync(input);
        Assert.AreEqual(input * input, result);
        
        var delayed = await DelayedComputeAsync(input, 10);
        Assert.AreEqual(input + 10, delayed);
    }

    [TestMethod]
    public async Task AsyncExceptionHandlingTest()
    {
        try
        {
            var result = await SafeComputeAsync(10);
            Assert.AreEqual(100, result);
            
            result = await SafeComputeAsync(-1);
            Assert.AreEqual(0, result);
        }
        catch
        {
            Assert.Fail("Should not throw");
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