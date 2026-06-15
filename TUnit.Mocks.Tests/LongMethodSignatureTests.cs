using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

// Regression coverage for issue #6254: mocking a method whose parameter count exceeds the BCL
// System.Func<>/System.Action<> arity limit (16 inputs) failed to compile (CS0305) because the
// generator emitted typed Returns/ReturnsAsync/Callback/Throws overloads using Func<...>/Action<...>
// with more than 16 type arguments. Async methods always get a typed wrapper, so they hit this even
// though non-async methods with > MaxTypedParams params already fell back to the untyped surface.
//
// The fix omits the parameter-typed convenience overloads when the count exceeds 16; the wrapper is
// still generated (untyped Returns/ReturnsAsync/Throws/Callback/verification remain available). The
// fact that these test interfaces compile at all is itself the core regression assertion.

#region Mock targets

public interface ILongMethodSignatures
{
    // 20 parameters — exceeds the Func<>/Action<> limit. This is the exact shape from the issue.
    Task SomeMethod(string _1, string _2, string _3, string _4, string _5,
        string _6, string _7, string _8, string _9, string _10,
        string _11, string _12, string _13, string _14, string _15,
        string _16, string _17, string _18, string _19, string _20);
}

public interface ILongReturningSignature
{
    // 20 parameters with a Task<T> return — exercises the returning-method emit path.
    Task<int> Sum(int _1, int _2, int _3, int _4, int _5,
        int _6, int _7, int _8, int _9, int _10,
        int _11, int _12, int _13, int _14, int _15,
        int _16, int _17, int _18, int _19, int _20);
}

public interface IWithinArityLimit
{
    // 10 parameters — above the old MaxTypedParams (8) but within the Func<>/Action<> limit, so the
    // parameter-typed overloads must STILL be emitted (guards against over-gating the fix).
    Task<int> Add(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j);
}

#endregion

public class LongMethodSignatureTests
{
    [Test]
    public async Task Async_Void_Task_With_Twenty_Params_Compiles_And_Runs()
    {
        var mock = ILongMethodSignatures.Mock();

        // No setup: the call should still resolve to a completed task (default async behaviour).
        await mock.Object.SomeMethod("1", "2", "3", "4", "5", "6", "7", "8", "9", "10",
            "11", "12", "13", "14", "15", "16", "17", "18", "19", "20");

        mock.SomeMethod(AnyArgs()).WasCalled(Times.Once);
    }

    [Test]
    public async Task Async_Void_Task_With_Twenty_Params_Throws_Setup()
    {
        var mock = ILongMethodSignatures.Mock();
        mock.SomeMethod(AnyArgs()).Throws(new InvalidOperationException("boom"));

        await Assert.That(async () => await mock.Object.SomeMethod(
                "1", "2", "3", "4", "5", "6", "7", "8", "9", "10",
                "11", "12", "13", "14", "15", "16", "17", "18", "19", "20"))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Async_Returning_Task_With_Twenty_Params_ReturnsAsync_Value()
    {
        var mock = ILongReturningSignature.Mock();
        mock.Sum(AnyArgs()).ReturnsAsync(Task.FromResult(42));

        var result = await mock.Object.Sum(1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            11, 12, 13, 14, 15, 16, 17, 18, 19, 20);

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Within_Arity_Limit_Keeps_Typed_Parameter_Overloads()
    {
        var mock = IWithinArityLimit.Mock();

        // 10 params is above the old MaxTypedParams (8): the typed Returns(Func<...>) overload must
        // still exist. Returning the sum proves the actual arguments flow through the typed delegate.
        mock.Add(AnyArgs()).Returns((a, b, c, d, e, f, g, h, i, j) =>
            a + b + c + d + e + f + g + h + i + j);

        var result = await mock.Object.Add(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

        await Assert.That(result).IsEqualTo(55);
    }
}
