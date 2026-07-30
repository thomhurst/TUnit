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

    Task<int> GetSmallAsync();

    Task<decimal> GetPriceAsync();

    Task<long?> GetCountOrNullAsync();
}

// Implicit constant expression conversions (#6518 Codex round 5): `async () => 1` on a
// Task<byte> member compiles against the declared delegate (in-range int constant → byte), but
// the generic alias infers int and the widening table has no int → byte entry. The helper
// replays these as range-guarded exact-value narrowings — constant-ness is erased at runtime.
public interface IConstantNumericAsyncService
{
    Task<byte> GetByteAsync();

    ValueTask<short> GetShortAsync();

    Task<ulong> GetUnsignedAsync();
}

// Native integers (#6518 Codex round 7): nint → long (and nuint → ulong) are ordinary
// implicit numeric conversions, as are the small integral types → nint and the non-negative
// int constant → nuint — the conversion tables must include native integers on both sides.
public interface INativeIntAsyncService
{
    Task<long> GetLongAsync();

    Task<nint> GetNativeAsync();

    ValueTask<nuint> GetUnsignedNativeAsync();
}

// Enum results (#6518 Codex round 6): `async () => 0` on a Task<MyEnum> member compiles via
// C#'s implicit constant-zero-to-enum conversion, but the generic alias infers int and the
// numeric tables have no enum destinations. The helper replays it value-guarded — integral
// sources, exactly zero, enum destinations only.
public interface IEnumAsyncService
{
    Task<AsyncColor> GetColorAsync();

    ValueTask<AsyncColor> GetColorValueAsync();

    Task<AsyncColor?> GetColorOrNullAsync();
}

public enum AsyncColor
{
    None = 0,
    Red = 1,
}

// Defaultable T? (#6518 Codex round 6): on an unconstrained type parameter the trailing '?' is
// the defaultable annotation, not Nullable<T> — Task<T?> with T = int is Task<int>, so a null
// factory result must surface as the informative InvalidCastException, not silently become 0.
public interface IDefaultableGenericAsyncService
{
    Task<T?> FindAsync<T>(int id);
}

// dynamic result (#6518 Codex round 2): `dynamic` is illegal as a pattern type (CS8208) and as
// a typeof operand (CS1962), so the conversion helper must spell it `object` — merely mocking
// this interface failing to COMPILE is the regression.
public interface IDynamicAsyncService
{
    Task<dynamic> GetAsync();

    ValueTask<dynamic> GetValueAsync();
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
    public async Task Async_Lambda_With_In_Range_Constant_Int_On_Byte_Member_Converts()
    {
        // `async () => 1` compiles against Func<Task<byte>> only because 1 is an in-range int
        // constant; the alias infers int, so the helper must replay the constant conversion.
        var mock = IConstantNumericAsyncService.Mock();
        mock.GetByteAsync().Returns(async () =>
        {
            await Task.Yield();
            return 1;
        });

        await Assert.That(await mock.Object.GetByteAsync()).IsEqualTo((byte)1);
    }

    [Test]
    public async Task Async_Lambda_Constant_Conversion_On_ValueTask_Short_Member()
    {
        var mock = IConstantNumericAsyncService.Mock();
        mock.GetShortAsync().Returns(async () =>
        {
            await Task.Yield();
            return -5;
        });

        await Assert.That(await mock.Object.GetShortAsync()).IsEqualTo((short)-5);
    }

    [Test]
    public async Task Async_Lambda_Non_Negative_Int_On_Ulong_Member_Converts()
    {
        var mock = IConstantNumericAsyncService.Mock();
        mock.GetUnsignedAsync().Returns(async () =>
        {
            await Task.Yield();
            return 7;
        });

        await Assert.That(await mock.Object.GetUnsignedAsync()).IsEqualTo(7UL);
    }

    [Test]
    public async Task Async_Lambda_Returning_Nint_On_Long_Member_Converts()
    {
        // nint → long is an ordinary implicit numeric conversion; the alias infers nint and
        // the widening table must include the native-integer source.
        var mock = INativeIntAsyncService.Mock();
        mock.GetLongAsync().Returns(async () =>
        {
            await Task.Yield();
            return (nint)7;
        });

        await Assert.That(await mock.Object.GetLongAsync()).IsEqualTo(7L);
    }

    [Test]
    public async Task Async_Lambda_Returning_Int_On_Nint_Member_Converts()
    {
        // int → nint is an ordinary implicit numeric conversion (native int is at least 32
        // bits); the boxed int must convert via the native-integer destination entry.
        var mock = INativeIntAsyncService.Mock();
        mock.GetNativeAsync().Returns(async () =>
        {
            await Task.Yield();
            return 7;
        });

        await Assert.That(await mock.Object.GetNativeAsync()).IsEqualTo((nint)7);
    }

    [Test]
    public async Task Async_Lambda_Non_Negative_Int_On_Nuint_Member_Converts()
    {
        // A non-negative int CONSTANT converts implicitly to nuint; replayed value-guarded.
        var mock = INativeIntAsyncService.Mock();
        mock.GetUnsignedNativeAsync().Returns(async () =>
        {
            await Task.Yield();
            return 7;
        });

        await Assert.That(await mock.Object.GetUnsignedNativeAsync()).IsEqualTo((nuint)7);
    }

    [Test]
    public async Task Negative_Int_On_Nuint_Member_Surfaces_An_Informative_InvalidCast()
    {
        var mock = INativeIntAsyncService.Mock();
        mock.GetUnsignedNativeAsync().Returns(async () =>
        {
            await Task.Yield();
            return -1;
        });

        await Assert.That(async () => await mock.Object.GetUnsignedNativeAsync())
            .Throws<InvalidCastException>();
    }

    [Test]
    public async Task Out_Of_Range_Int_On_Byte_Member_Surfaces_An_Informative_InvalidCast()
    {
        // 300 is not representable as byte — C# would reject the constant conversion too, so
        // the helper must not silently truncate; it takes the informative failure path.
        var mock = IConstantNumericAsyncService.Mock();
        mock.GetByteAsync().Returns(async () =>
        {
            await Task.Yield();
            return 300;
        });

        await Assert.That(async () => await mock.Object.GetByteAsync())
            .Throws<InvalidCastException>();
    }

    [Test]
    public async Task Negative_Int_On_Ulong_Member_Surfaces_An_Informative_InvalidCast()
    {
        var mock = IConstantNumericAsyncService.Mock();
        mock.GetUnsignedAsync().Returns(async () =>
        {
            await Task.Yield();
            return -1;
        });

        await Assert.That(async () => await mock.Object.GetUnsignedAsync())
            .Throws<InvalidCastException>();
    }

    [Test]
    public async Task Zero_On_Enum_Member_Converts()
    {
        // `async () => 0` compiles against the declared delegate only via the implicit
        // constant-zero-to-enum conversion; the alias infers int and the helper must replay it.
        var mock = IEnumAsyncService.Mock();
        mock.GetColorAsync().Returns(async () =>
        {
            await Task.Yield();
            return 0;
        });

        await Assert.That(await mock.Object.GetColorAsync()).IsEqualTo(AsyncColor.None);
    }

    [Test]
    public async Task Zero_On_ValueTask_Enum_Member_Converts()
    {
        var mock = IEnumAsyncService.Mock();
        mock.GetColorValueAsync().Returns(async () =>
        {
            await Task.Yield();
            return 0;
        });

        await Assert.That(await mock.Object.GetColorValueAsync()).IsEqualTo(AsyncColor.None);
    }

    [Test]
    public async Task Zero_On_Nullable_Enum_Member_Converts()
    {
        var mock = IEnumAsyncService.Mock();
        mock.GetColorOrNullAsync().Returns(async () =>
        {
            await Task.Yield();
            return 0;
        });

        await Assert.That(await mock.Object.GetColorOrNullAsync()).IsEqualTo(AsyncColor.None);
    }

    [Test]
    public async Task Non_Zero_Int_On_Enum_Member_Surfaces_An_Informative_InvalidCast()
    {
        // C# only converts the CONSTANT ZERO to an enum implicitly — a non-zero int would not
        // compile against the declared delegate either, so the helper must not silently cast.
        var mock = IEnumAsyncService.Mock();
        mock.GetColorAsync().Returns(async () =>
        {
            await Task.Yield();
            return 1;
        });

        await Assert.That(async () => await mock.Object.GetColorAsync())
            .Throws<InvalidCastException>();
    }

    [Test]
    public async Task Null_On_Defaultable_Generic_Value_Instantiation_Throws()
    {
        // Task<T?> with unconstrained T = int is Task<int> — '?' is the defaultable annotation,
        // not Nullable<int>. A null factory result silently becoming 0 is the regression.
        var mock = IDefaultableGenericAsyncService.Mock();
        mock.FindAsync<int>(Arg.Any<int>()).Returns(async _ =>
        {
            await Task.Yield();
            return (int?)null;
        });

        await Assert.That(async () => await mock.Object.FindAsync<int>(1))
            .Throws<InvalidCastException>();
    }

    [Test]
    public async Task Value_On_Defaultable_Generic_Value_Instantiation_Round_Trips()
    {
        var mock = IDefaultableGenericAsyncService.Mock();
        mock.FindAsync<int>(Arg.Any<int>()).Returns(async _ =>
        {
            await Task.Yield();
            return 5;
        });

        await Assert.That(await mock.Object.FindAsync<int>(1)).IsEqualTo(5);
    }

    [Test]
    public async Task Null_On_Defaultable_Generic_Reference_Instantiation_Round_Trips()
    {
        // For T = string the same runtime guard must NOT fire — null is a valid T? value.
        var mock = IDefaultableGenericAsyncService.Mock();
        mock.FindAsync<string>(Arg.Any<int>()).Returns(async _ =>
        {
            await Task.Yield();
            return (string?)null;
        });

        await Assert.That(await mock.Object.FindAsync<string>(1)).IsNull();
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
    public async Task Task_Of_Dynamic_Member_Converts_The_Factory_Result()
    {
        // `async () => "dyn"` infers Task<string>, which is invariant-incompatible with the
        // declared Task<dynamic>, so the generic alias binds and the conversion helper runs.
        var mock = IDynamicAsyncService.Mock();
        mock.GetAsync().Returns(async () =>
        {
            await Task.Yield();
            return "dyn";
        });

        string result = await mock.Object.GetAsync();

        await Assert.That(result).IsEqualTo("dyn");
    }

    [Test]
    public async Task ValueTask_Of_Dynamic_Member_Converts_The_Factory_Result()
    {
        var mock = IDynamicAsyncService.Mock();
        mock.GetValueAsync().Returns(async () =>
        {
            await Task.Yield();
            return 7;
        });

        int result = await mock.Object.GetValueAsync();

        await Assert.That(result).IsEqualTo(7);
    }

    [Test]
    public async Task Task_Of_Dynamic_Member_Accepts_A_Null_Factory_Result()
    {
        // dynamic accepts null — the non-nullable value-type null guard must not fire here.
        var mock = IDynamicAsyncService.Mock();
        mock.GetAsync().Returns(async () =>
        {
            await Task.Yield();
            return (string?)null;
        });

        object? result = await mock.Object.GetAsync();

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Null_From_A_Factory_Inferring_Nullable_On_A_NonNullable_Value_Member_Throws()
    {
        // `async () => (long?)null` infers Task<long?> on the Task<long> member; the null must
        // not silently become default(long) — zero would be corrupted data.
        var mock = INumericAsyncService.Mock();
        mock.GetCountAsync().Returns(async () =>
        {
            await Task.Yield();
            return (long?)null;
        });

        await Assert.That(async () => await mock.Object.GetCountAsync())
            .Throws<InvalidCastException>();
    }

    [Test]
    public async Task Null_From_A_Factory_On_A_Nullable_Value_Member_Round_Trips()
    {
        // `async () => (int?)null` infers Task<int?> on the Task<long?> member — a different
        // task type, so the conversion helper runs; null is valid for the nullable result.
        var mock = INumericAsyncService.Mock();
        mock.GetCountOrNullAsync().Returns(async () =>
        {
            await Task.Yield();
            return (int?)null;
        });

        await Assert.That(await mock.Object.GetCountOrNullAsync()).IsNull();
    }

    [Test]
    public async Task Long_To_Int_Narrowing_Attempt_Throws_Instead_Of_Truncating()
    {
        // long → int is not an implicit C# conversion; the helper must not replay it.
        var mock = INumericAsyncService.Mock();
        mock.GetSmallAsync().Returns(async () =>
        {
            await Task.Yield();
            return 5L;
        });

        await Assert.That(async () => await mock.Object.GetSmallAsync())
            .Throws<InvalidCastException>();
    }

    [Test]
    public async Task Double_To_Int_Rounding_Attempt_Throws_Instead_Of_Rounding()
    {
        // Convert.ChangeType would round 1.9 to 2; C# has no double → int implicit conversion.
        var mock = INumericAsyncService.Mock();
        mock.GetSmallAsync().Returns(async () =>
        {
            await Task.Yield();
            return 1.9;
        });

        await Assert.That(async () => await mock.Object.GetSmallAsync())
            .Throws<InvalidCastException>();
    }

    [Test]
    public async Task Int_To_Decimal_Widening_Works()
    {
        // int → decimal IS an implicit numeric conversion and must keep working.
        var mock = INumericAsyncService.Mock();
        mock.GetPriceAsync().Returns(async () =>
        {
            await Task.Yield();
            return 42;
        });

        await Assert.That(await mock.Object.GetPriceAsync()).IsEqualTo(42m);
    }

    [Test]
    public async Task Double_To_Decimal_Is_Not_Implicit_And_Throws()
    {
        // double → decimal is only an EXPLICIT conversion in C#.
        var mock = INumericAsyncService.Mock();
        mock.GetPriceAsync().Returns(async () =>
        {
            await Task.Yield();
            return 1.5;
        });

        await Assert.That(async () => await mock.Object.GetPriceAsync())
            .Throws<InvalidCastException>();
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
