namespace TUnit.TestProject.Library;

// Base class with async test methods in a different assembly
public abstract class AsyncBaseTests
{
    [Test]
    public async Task BaseAsyncTest()
    {
        await Task.Delay(1);
        Console.WriteLine("Base async test executed");
    }

    [Test]
    public async Task BaseAsyncTestWithReturn()
    {
        await Task.Delay(1);
        Console.WriteLine("Base async test with return executed");
    }

    [Test]
    [Arguments("test1")]
    [Arguments("test2")]
    public async Task BaseAsyncTestWithArguments(string value)
    {
        await Task.Delay(1);
        Console.WriteLine($"Base async test with argument: {value}");
    }

    [Test]
    public void BaseSyncTest()
    {
        Console.WriteLine("Base sync test executed");
    }

    [Test]
    public async Task BaseAsyncTestWithCancellation(CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        Console.WriteLine("Base async test with cancellation token executed");
    }
}