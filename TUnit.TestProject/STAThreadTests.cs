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
        // Initial check
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        // After a simple delay
        await Task.Delay(10);

        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithTaskYield()
    {
        // Initial check
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        // After Task.Yield() which forces continuation on thread pool
        await Task.Yield();
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithConfigureAwaitTrue()
    {
        // Initial check
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        // ConfigureAwait(true) should maintain STA context
        await Task.Delay(10).ConfigureAwait(true);
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithNestedAsyncCalls()
    {
        // Initial check
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        // Call nested async method
        await NestedAsyncMethod();

        // Should still be STA after nested calls
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithTaskFromResult()
    {
        // Initial check
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        // Task.FromResult should complete synchronously
        var result = await Task.FromResult(42);
        await Assert.That(result).IsEqualTo(42);
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithCompletedTask()
    {
        // Initial check
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        // Awaiting already completed task
        await Task.CompletedTask;
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithTaskRun()
    {
        // Initial check
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        // Task.Run executes on thread pool but we should return to STA
        var result = await Task.Run(() =>
        {
            // This runs on thread pool (MTA)
            return Thread.CurrentThread.GetApartmentState();
        });

        // Task.Run should have executed on MTA thread pool
        await Assert.That(result).IsEqualTo(ApartmentState.MTA);

        // But we should be back on STA thread
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithMultipleAwaits()
    {
        // Initial check
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        // Multiple awaits in sequence
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
        // Initial check
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        // Test with async enumerable
        await foreach (var item in GetAsyncEnumerable())
        {
            await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
            await Assert.That(item).IsGreaterThan(0);
        }

        // Final check
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithTaskWhenAll()
    {
        // Initial check
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        // Create multiple tasks
        var tasks = new[]
        {
            Task.Delay(5),
            Task.Delay(10),
            Task.FromResult(42).ContinueWith(t => t.Result)
        };

        // Wait for all to complete
        await Task.WhenAll(tasks);

        // Should still be on STA thread
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
    }

    [Test, STAThreadExecutor]
    public async Task STA_WithExceptionHandling()
    {
        // Initial check
        await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);

        try
        {
            // This should throw
            await ThrowingAsyncMethod();
        }
        catch (InvalidOperationException)
        {
            // Exception caught, check thread state
            await Assert.That(Thread.CurrentThread.GetApartmentState()).IsEquatableOrEqualTo(ApartmentState.STA);
        }

        // Final check after exception handling
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
