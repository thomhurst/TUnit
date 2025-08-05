using System.Threading.Tasks;

namespace TUnitTimer;

public class AsyncTests
{
    [Test]
    public async Task SimpleAsyncTest()
    {
        var result = await ComputeAsync(10);
        await Assert.That(result).IsEqualTo(100);

        var text = await ProcessTextAsync("hello");
        await Assert.That(text).IsEqualTo("HELLO");
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

        await Assert.That(lines).HasCount(3);
        await Assert.That(lines[0]).IsEqualTo("Line 1");
        await Assert.That(lines[2]).IsEqualTo("Line 3");
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

        await Assert.That(results).HasCount(4);
        await Assert.That(results[0]).IsEqualTo(25);
        await Assert.That(results[1]).IsEqualTo(100);
        await Assert.That(results[2]).IsEqualTo(225);
        await Assert.That(results[3]).IsEqualTo(400);
        await Assert.That(results.Sum()).IsEqualTo(750);
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

        await Assert.That(count).IsEqualTo(10);
        await Assert.That(sum).IsEqualTo(55);
    }

    [Test]
    public async Task AsyncFileSimulationTest()
    {
        var data = await SimulateFileReadAsync("test.txt");
        var lines = data.Split('\n');

        await Assert.That(lines).HasCount(5);
        await Assert.That(lines[0]).IsEqualTo("Header: Test File");
        await Assert.That(lines[4]).IsEqualTo("Footer: End of File");

        var processed = await SimulateFileProcessAsync(data);
        await Assert.That(processed).Contains("PROCESSED");
    }

    [Test]
    [Arguments(10)]
    [Arguments(20)]
    [Arguments(30)]
    public async Task ParameterizedAsyncTest(int input)
    {
        var result = await ComputeAsync(input);
        await Assert.That(result).IsEqualTo(input * input);

        var delayed = await DelayedComputeAsync(input, 10);
        await Assert.That(delayed).IsEqualTo(input + 10);
    }

    [Test]
    public async Task AsyncExceptionHandlingTest()
    {
        try
        {
            var result = await SafeComputeAsync(10);
            await Assert.That(result).IsEqualTo(100);

            result = await SafeComputeAsync(-1);
            await Assert.That(result).IsEqualTo(0);
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
