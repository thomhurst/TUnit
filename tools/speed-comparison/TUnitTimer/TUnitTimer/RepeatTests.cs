using System.Threading.Tasks;

namespace TUnitTimer;

public class RepeatTests
{
    private static int _counter = 0;

    [Test]
    [Repeat(100)]
    public async Task RepeatedCalculationTest()
    {
        var localCounter = Interlocked.Increment(ref _counter);
        var result = PerformCalculation(localCounter);

        await Assert.That(result).IsGreaterThan(0);
        await Assert.That(result % localCounter).IsEqualTo(0);
    }

    [Test]
    [Repeat(50)]
    public async Task RepeatedAsyncTest()
    {
        var taskId = Guid.NewGuid();
        var result = await ProcessDataAsync(taskId);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Length).IsEqualTo(36); // GUID length
        await Assert.That(result).IsEqualTo(taskId.ToString());
    }

    [Test]
    [Repeat(25)]
    public async Task RepeatedStringOperationTest()
    {
        var iteration = Interlocked.Increment(ref _counter);
        var text = $"Iteration_{iteration}";
        var processed = ProcessString(text);

        await Assert.That(processed).Contains("PROCESSED");
        await Assert.That(processed).Contains(iteration.ToString());
        await Assert.That(processed.Length).IsGreaterThan(text.Length);
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
