using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Enums;
using TUnit.Core.Executors;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[RunOn(OS.Windows)]
[Repeat(100)]
[UnconditionalSuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class STAThreadTests
{
    [Test, STAThreadExecutor]
    public async Task With_STA()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test]
    public async Task Without_STA()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.MTA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithSimpleAwait()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        await Task.Delay(10);

        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithTaskYield()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        await Task.Yield();
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithConfigureAwaitTrue()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        await Task.Delay(10).ConfigureAwait(true);
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithNestedAsyncCalls()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        await NestedAsyncMethod();

        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithTaskFromResult()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        var result = await Task.FromResult(42);
        await Assert.That(result).IsEqualTo(42);
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithCompletedTask()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        await Task.CompletedTask;
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithTaskRun()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        var result = await Task.Run(() =>
        {
            return Thread.CurrentThread.GetApartmentState();
        });

        await Assert.That(result).IsEqualTo(ApartmentState.MTA);

        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithMultipleAwaits()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        await Task.Delay(1);
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        await Task.Yield();
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        await Task.Delay(1);
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        await Task.FromResult(true);
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithAsyncEnumerable()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        await foreach (var item in GetAsyncEnumerable())
        {
            await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
            await Assert.That(item).IsGreaterThan(0);
        }

        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithTaskWhenAll()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        var tasks = new[]
        {
            Task.Delay(5),
            Task.Delay(10),
            Task.FromResult(42).ContinueWith(t => t.Result)
        };

        await Task.WhenAll(tasks);

        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithExceptionHandling()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        try
        {
            await ThrowingAsyncMethod();
        }
        catch (InvalidOperationException)
        {
            await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
        }

        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    private async Task NestedAsyncMethod()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
        await Task.Delay(5);
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
        await AnotherNestedMethod();
    }

    private async Task AnotherNestedMethod()
    {
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
        await Task.Yield();
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    private async IAsyncEnumerable<int> GetAsyncEnumerable()
    {
        for (var i = 1; i <= 3; i++)
        {
            await Task.Delay(1);
            yield return i;
        }
    }

    private async Task ThrowingAsyncMethod()
    {
        await Task.Delay(1);
        throw new InvalidOperationException("Test exception");
    }
}
