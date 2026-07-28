using System.Diagnostics;
using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/6495
// `.Returns()` on an async member only accepted a synchronous Func<T>, so an `async () => ...`
// lambda failed with CS4010 and the only workaround (Thread.Sleep) blocked the caller before the
// task was ever handed back — making it impossible to race a mocked call against a timeout.

public interface ISlowService
{
    Task<int> GetValueAsync();

    Task<int> GetValueAsync(int seed);

    ValueTask<int> GetValueValueTaskAsync();

    Task DoWorkAsync();
}

public class Issue6495Tests
{
    private const int NeverInTestTimeMs = 30_000;

    [Test]
    public async Task Returns_Async_Lambda_Stays_Pending_So_A_Timeout_Can_Win()
    {
        var mock = ISlowService.Mock();
        mock.GetValueAsync().Returns(async () =>
        {
            await Task.Delay(NeverInTestTimeMs);
            return 42;
        });

        var call = mock.Object.GetValueAsync();
        var timeout = Task.Delay(50);

        // The mocked call must come back incomplete, or the timeout branch could never win.
        await Assert.That(call.IsCompleted).IsFalse();
        await Assert.That(await Task.WhenAny(call, timeout)).IsSameReferenceAs(timeout);
    }

    [Test]
    public async Task Returns_Async_Lambda_Still_Produces_The_Value()
    {
        var mock = ISlowService.Mock();
        mock.GetValueAsync().Returns(async () =>
        {
            await Task.Yield();
            return 42;
        });

        await Assert.That(await mock.Object.GetValueAsync()).IsEqualTo(42);
    }

    [Test]
    public async Task Returns_Async_Lambda_Is_Invoked_Per_Call()
    {
        var calls = 0;
        var mock = ISlowService.Mock();
        mock.GetValueAsync().Returns(async () =>
        {
            await Task.Yield();
            return ++calls;
        });

        await Assert.That(await mock.Object.GetValueAsync()).IsEqualTo(1);
        await Assert.That(await mock.Object.GetValueAsync()).IsEqualTo(2);
    }

    [Test]
    public async Task Returns_Async_Lambda_With_Parameters()
    {
        var mock = ISlowService.Mock();
        mock.GetValueAsync(Arg.Any<int>()).Returns(async seed =>
        {
            await Task.Yield();
            return seed * 2;
        });

        await Assert.That(await mock.Object.GetValueAsync(21)).IsEqualTo(42);
    }

    [Test]
    public async Task Returns_Async_Lambda_On_ValueTask_Member()
    {
        var mock = ISlowService.Mock();
        mock.GetValueValueTaskAsync().Returns(async () =>
        {
            await Task.Yield();
            return 7;
        });

        await Assert.That(await mock.Object.GetValueValueTaskAsync()).IsEqualTo(7);
    }

    [Test]
    public async Task Returns_Async_Lambda_On_Task_Returning_Member_Stays_Pending()
    {
        var mock = ISlowService.Mock();
        mock.DoWorkAsync().Returns(async () => await Task.Delay(NeverInTestTimeMs));

        var call = mock.Object.DoWorkAsync();

        await Assert.That(call.IsCompleted).IsFalse();
    }

    [Test]
    public async Task Synchronous_Returns_Factory_Still_Works()
    {
        var mock = ISlowService.Mock();
        mock.GetValueAsync().Returns(() => 42);

        await Assert.That(await mock.Object.GetValueAsync()).IsEqualTo(42);
    }

    [Test]
    public async Task ReturnsAsync_Factory_Is_Unchanged()
    {
        var mock = ISlowService.Mock();
        mock.GetValueAsync().ReturnsAsync(async () =>
        {
            await Task.Yield();
            return 42;
        });

        await Assert.That(await mock.Object.GetValueAsync()).IsEqualTo(42);
    }
}
