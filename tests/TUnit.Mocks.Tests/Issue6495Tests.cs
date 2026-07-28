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

// A reference-typed async result: null and throw-expression lambda bodies convert to both T and
// Task<T>, so the two Returns factory overloads must not become an ambiguity (CS0121).
public interface IReferenceResultService
{
    Task<string?> GetNameAsync();

    Task<string?> GetNameAsync(int id);
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
    public async Task Null_Returning_Lambda_Still_Binds_To_The_Synchronous_Factory()
    {
        // `() => null` converts to both Func<string?> and Func<Task<string?>>. The async overload
        // is deprioritised, so this keeps its pre-existing meaning: null is the *value*, and the
        // member still returns a completed task.
        var mock = IReferenceResultService.Mock();
        mock.GetNameAsync().Returns(() => null);

        var call = mock.Object.GetNameAsync();

        await Assert.That(call.IsCompleted).IsTrue();
        await Assert.That(await call).IsNull();
    }

    [Test]
    public async Task Throwing_Lambda_Still_Binds_To_The_Synchronous_Factory()
    {
        var mock = IReferenceResultService.Mock();
        mock.GetNameAsync().Returns(() => throw new InvalidOperationException("boom"));

        await Assert.That(async () => await mock.Object.GetNameAsync())
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Null_Returning_Typed_Lambda_Still_Binds_To_The_Synchronous_Factory()
    {
        var mock = IReferenceResultService.Mock();
        mock.GetNameAsync(Arg.Any<int>()).Returns(id => null);

        await Assert.That(await mock.Object.GetNameAsync(1)).IsNull();
    }

    [Test]
    public async Task Async_Lambda_Still_Binds_On_A_Reference_Typed_Result()
    {
        var mock = IReferenceResultService.Mock();
        mock.GetNameAsync().Returns(async () =>
        {
            await Task.Delay(NeverInTestTimeMs);
            return "late";
        });

        var call = mock.Object.GetNameAsync();

        await Assert.That(call.IsCompleted).IsFalse();
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
