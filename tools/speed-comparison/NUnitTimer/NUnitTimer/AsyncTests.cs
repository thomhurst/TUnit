namespace NUnitTimer;

[TestFixture]
public class AsyncTests
{
    [Test]
    public async Task SimpleAsyncTest()
    {
        var result = await ComputeAsync(10);
        Assert.That(result, Is.EqualTo(100));
        
        var text = await ProcessTextAsync("hello");
        Assert.That(text, Is.EqualTo("HELLO"));
    }

    [Test]
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
        
        Assert.That(lines.Count, Is.EqualTo(3));
        Assert.That(lines[0], Is.EqualTo("Line 1"));
        Assert.That(lines[2], Is.EqualTo("Line 3"));
    }

    [Test]
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
        
        Assert.That(results.Length, Is.EqualTo(4));
        Assert.That(results[0], Is.EqualTo(25));
        Assert.That(results[1], Is.EqualTo(100));
        Assert.That(results[2], Is.EqualTo(225));
        Assert.That(results[3], Is.EqualTo(400));
        Assert.That(results.Sum(), Is.EqualTo(750));
    }

    [Test]
    public async Task AsyncEnumerableTest()
    {
        var sum = 0;
        var count = 0;
        
        await foreach (var value in GenerateValuesAsync())
        {
            sum += value;
            count++;
        }
        
        Assert.That(count, Is.EqualTo(10));
        Assert.That(sum, Is.EqualTo(55));
    }

    [Test]
    public async Task AsyncFileSimulationTest()
    {
        var data = await SimulateFileReadAsync("test.txt");
        var lines = data.Split('\n');
        
        Assert.That(lines.Length, Is.EqualTo(5));
        Assert.That(lines[0], Is.EqualTo("Header: Test File"));
        Assert.That(lines[4], Is.EqualTo("Footer: End of File"));
        
        var processed = await SimulateFileProcessAsync(data);
        Assert.That(processed, Does.Contain("PROCESSED"));
    }

    [TestCase(10)]
    [TestCase(20)]
    [TestCase(30)]
    public async Task ParameterizedAsyncTest(int input)
    {
        var result = await ComputeAsync(input);
        Assert.That(result, Is.EqualTo(input * input));
        
        var delayed = await DelayedComputeAsync(input, 10);
        Assert.That(delayed, Is.EqualTo(input + 10));
    }

    [Test]
    public async Task AsyncExceptionHandlingTest()
    {
        try
        {
            var result = await SafeComputeAsync(10);
            Assert.That(result, Is.EqualTo(100));
            
            result = await SafeComputeAsync(-1);
            Assert.That(result, Is.EqualTo(0));
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