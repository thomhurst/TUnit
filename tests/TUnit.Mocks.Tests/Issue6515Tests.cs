using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/6515
// The async-factory Returns alias added for #6495 carried [OverloadResolutionPriority] and was
// therefore gated to net9.0+ consumers, so on net8.0 (and net472) `.Returns(async () => ...)`
// did not exist and a mocked async call could never be handed back still-pending. Below net9.0
// the alias is now generic — Returns<TAsyncFactoryResult>(Func<Task<TAsyncFactoryResult>>) — so
// typeless lambdas (null/throw) fail type inference and keep binding the synchronous factory,
// while genuine async lambdas bind the alias. This file runs on every target framework the test
// project builds for.

#region Test types

public interface ITimeoutClient
{
    Task<int> GetValueAsync(CancellationToken ct);
}

// Outer-nullable task member (#6518 review finding): the trailing '?' must not demote the alias
// to the ungated bare-task shape, or Returns(() => null) becomes CS0121.
public interface IOuterNullableTaskService
{
    Task<string?>? GetNameAsync();
}

// Polymorphic results (#6518 review finding): below net9.0 the generic alias infers the async
// lambda's own result type — a SUBTYPE of the declared result here — and Task<T>/ValueTask<T>
// are invariant, so the stored task must be converted to the declared task type.
public interface IPolymorphicResultService
{
    Task<object> GetAsync();

    ValueTask<object> GetValueAsync();

    Task<object> GetByIdAsync(int id);
}

public class TimeoutConsumer
{
    public async Task<int> GetWithTimeoutAsync(ITimeoutClient client, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var task = client.GetValueAsync(cts.Token);
        var completed = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token));
        if (completed != task)
        {
            throw new TimeoutException();
        }
        return await task;
    }
}

#endregion

public class Issue6515Tests
{
    [Test]
    public async Task Timeout_Genuinely_Races_A_Pending_Mocked_Call_And_Wins()
    {
        var client = ITimeoutClient.Mock();
        client.GetValueAsync(Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            await Task.Delay(30_000);
            return 42;
        });

        var consumer = new TimeoutConsumer();

        await Assert.That(async () => await consumer.GetWithTimeoutAsync(client.Object, TimeSpan.FromMilliseconds(50)))
            .Throws<TimeoutException>();
    }

    [Test]
    public async Task Fast_Async_Factory_Completes_Before_The_Timeout()
    {
        var client = ITimeoutClient.Mock();
        client.GetValueAsync(Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            await Task.Yield();
            return 42;
        });

        var consumer = new TimeoutConsumer();

        await Assert.That(await consumer.GetWithTimeoutAsync(client.Object, TimeSpan.FromSeconds(30))).IsEqualTo(42);
    }

    [Test]
    public async Task Outer_Nullable_Task_Member_Null_Lambda_Still_Binds_The_Synchronous_Factory()
    {
        var mock = IOuterNullableTaskService.Mock();
        mock.GetNameAsync().Returns(() => null);

        var call = mock.Object.GetNameAsync();

        await Assert.That(call!.IsCompleted).IsTrue();
        await Assert.That(await call).IsNull();
    }

    [Test]
    public async Task Outer_Nullable_Task_Member_Async_Lambda_Stays_Pending()
    {
        var mock = IOuterNullableTaskService.Mock();
        mock.GetNameAsync().Returns(async () =>
        {
            await Task.Delay(30_000);
            return "late";
        });

        var call = mock.Object.GetNameAsync();

        await Assert.That(call!.IsCompleted).IsFalse();
    }

    [Test]
    public async Task Outer_Nullable_Member_Configured_With_A_Null_Task_Returns_Null()
    {
        var mock = IOuterNullableTaskService.Mock();
        mock.GetNameAsync().ReturnsAsync((Task<string?>?)null);

        // Boxed so the assertion targets the task reference itself, not its awaited result.
        await Assert.That((object?)mock.Object.GetNameAsync()).IsNull();
    }

    [Test]
    public async Task Async_Lambda_Returning_A_Subtype_Produces_The_Value()
    {
        // `async () => "value"` infers Task<string> for a Task<object> member below net9.0 —
        // the setup must still serve the declared Task<object>.
        var mock = IPolymorphicResultService.Mock();
        mock.GetAsync().Returns(async () =>
        {
            await Task.Yield();
            return "value";
        });

        await Assert.That(await mock.Object.GetAsync()).IsEqualTo("value");
    }

    [Test]
    public async Task Async_Lambda_Returning_A_Subtype_Stays_Pending()
    {
        var mock = IPolymorphicResultService.Mock();
        mock.GetAsync().Returns(async () =>
        {
            await Task.Delay(30_000);
            return "late";
        });

        await Assert.That(mock.Object.GetAsync().IsCompleted).IsFalse();
    }

    [Test]
    public async Task Async_Lambda_Returning_A_Subtype_On_ValueTask_Member()
    {
        var mock = IPolymorphicResultService.Mock();
        mock.GetValueAsync().Returns(async () =>
        {
            await Task.Yield();
            return "vt-value";
        });

        await Assert.That(await mock.Object.GetValueAsync()).IsEqualTo("vt-value");
    }

    [Test]
    public async Task Async_Lambda_Returning_A_Subtype_With_Typed_Parameters()
    {
        var mock = IPolymorphicResultService.Mock();
        mock.GetByIdAsync(Arg.Any<int>()).Returns(async id =>
        {
            await Task.Yield();
            return $"id-{id}";
        });

        await Assert.That(await mock.Object.GetByIdAsync(7)).IsEqualTo("id-7");
    }

#if NET9_0_OR_GREATER
    [Test]
    public async Task Async_Lambda_With_Null_Body_Binds_The_NonGeneric_Alias()
    {
        // `async () => null` pins no type: it has no natural type, cannot infer the generic
        // alias's type parameter, and is not convertible to Func<T> — the ORP(-1) non-generic
        // alias is the sole applicable candidate. That alias only exists on net9.0+.
        var mock = IOuterNullableTaskService.Mock();
        mock.GetNameAsync().Returns(async () => null);

        var call = mock.Object.GetNameAsync();

        await Assert.That(call!.IsCompleted).IsTrue();
        await Assert.That(await call).IsNull();
    }
#endif

    [Test]
    public async Task Exactly_Typed_Factory_Task_Is_Handed_Back_As_Is()
    {
        // The conversion path must not wrap a factory task that already has the declared type —
        // reference identity is part of the "returned as-is" contract.
        var exact = Task.FromResult(42);
        Func<Task<int>> factory = () => exact;

        var mock = ISlowService.Mock();
        mock.GetValueAsync().Returns(factory);

        // Boxed so the assertion targets the task reference itself, not its awaited result.
        await Assert.That((object)mock.Object.GetValueAsync()).IsSameReferenceAs(exact);
    }
}
