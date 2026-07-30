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

// Numeric widening (#6518 review finding): the generic alias infers the async lambda's own
// numeric type (`async () => 1` infers int on a Task<long> member); the conversion helper must
// replay the implicit widening rather than fail unboxing.
public interface INumericAsyncService
{
    Task<long> GetCountAsync();

    ValueTask<double> GetRatioAsync();

    Task<long> AddAsync(int x);
}

// User-named type parameter (#6518 review finding): the alias's own type parameter must be
// uniquified, or it shadows this one (CS0693) and rebinds the conversion helper's result type.
public interface IUserNamedTypeParam
{
    Task<TAsyncFactoryResult> RoundtripAsync<TAsyncFactoryResult>(TAsyncFactoryResult value);
}

// Null-task pass-through (#6518 review finding): when the generic alias binds (the factory's
// task type differs from the declared one, so the synchronous factory is inapplicable) and the
// factory produces a null task at runtime, the null must reach the raw-return check instead of
// faulting inside the conversion helper.
public interface IOuterNullableObjectService
{
    Task<object?>? GetAsync();
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
    public async Task Async_Lambda_With_Narrower_Numeric_Type_Widens_To_The_Declared_Result()
    {
        // `async () => 1` infers Task<int> on a Task<long> member; a boxed int cannot be
        // unboxed as long, so the conversion helper must widen explicitly.
        var mock = INumericAsyncService.Mock();
        mock.GetCountAsync().Returns(async () =>
        {
            await Task.Yield();
            return 1;
        });

        await Assert.That(await mock.Object.GetCountAsync()).IsEqualTo(1L);
    }

    [Test]
    public async Task Async_Lambda_Numeric_Widening_On_ValueTask_Member()
    {
        var mock = INumericAsyncService.Mock();
        mock.GetRatioAsync().Returns(async () =>
        {
            await Task.Yield();
            return 2;
        });

        await Assert.That(await mock.Object.GetRatioAsync()).IsEqualTo(2d);
    }

    [Test]
    public async Task Async_Lambda_Numeric_Widening_With_Typed_Parameters()
    {
        var mock = INumericAsyncService.Mock();
        mock.AddAsync(Arg.Any<int>()).Returns(async x =>
        {
            await Task.Yield();
            return x + 1;
        });

        await Assert.That(await mock.Object.AddAsync(2)).IsEqualTo(3L);
    }

    [Test]
    public async Task Wrong_Typed_Async_Factory_Surfaces_An_Informative_InvalidCast()
    {
        // A string is IConvertible, but C# has no string-to-long conversion — the helper must
        // not silently parse it, and the failure must name both types.
        var mock = INumericAsyncService.Mock();
        mock.GetCountAsync().Returns(async () =>
        {
            await Task.Yield();
            return "nope";
        });

        await Assert.That(async () => await mock.Object.GetCountAsync())
            .Throws<InvalidCastException>();
    }

    [Test]
    public async Task Generic_Method_With_User_Named_TAsyncFactoryResult_Still_Converts()
    {
        // The interface's own type parameter is literally named TAsyncFactoryResult; the alias
        // must not shadow it (this test failing to COMPILE is the regression).
        var mock = IUserNamedTypeParam.Mock();
        mock.RoundtripAsync<string>(Arg.Any<string>()).Returns(async v =>
        {
            await Task.Yield();
            return v + "!";
        });

        await Assert.That(await mock.Object.RoundtripAsync("a")).IsEqualTo("a!");
    }

    [Test]
    public async Task Null_Task_From_A_Generic_Alias_Factory_Passes_Through_On_Outer_Nullable_Member()
    {
        // Task<string?>? does not convert to the declared Task<object?>?, so the synchronous
        // factory is inapplicable and the generic alias binds with the factory's own task type.
        // Its null result must round-trip as the member's (contractually valid) null task.
        Func<Task<string?>?> inner = () => null;

        var mock = IOuterNullableObjectService.Mock();
        // The '!' silences the annotation-level mismatch only; the runtime value is still null.
        mock.GetAsync().Returns(() => inner()!);

        // Boxed so the assertion targets the task reference itself, not its awaited result.
        await Assert.That((object?)mock.Object.GetAsync()).IsNull();
    }

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
