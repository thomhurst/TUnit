using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Tests for the untyped <c>Any()</c> method and its implicit conversion to <c>Arg{T}</c>.
/// The non-generic <c>Any()</c> returns an <see cref="Arg"/> which implicitly converts to
/// <c>Arg{T}</c> via <c>public static implicit operator Arg{T}(Arg arg)</c>, creating
/// an <c>AnyMatcher{T}</c> under the hood.
/// </summary>
public class UntypedAnyTests
{
    // ──────────────────────────────────────────────
    // Implicit conversion to value types
    // ──────────────────────────────────────────────

    [Test]
    public async Task Any_Converts_To_Int_And_Matches_All()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Any()).Returns(42);

        await Assert.That(mock.Object.Add(0, 0)).IsEqualTo(42);
        await Assert.That(mock.Object.Add(1, 2)).IsEqualTo(42);
        await Assert.That(mock.Object.Add(-100, int.MaxValue)).IsEqualTo(42);
        await Assert.That(mock.Object.Add(int.MinValue, int.MaxValue)).IsEqualTo(42);
    }

    [Test]
    public async Task Any_Converts_To_Bool()
    {
        var mock = Mock.Of<IOverloadedService>();
        mock.Process(Any(), Any()).Callback(() => { });

        // Should not throw — Any() converts to Arg<bool>
        mock.Object.Process("data", true);
        mock.Object.Process("data", false);

        mock.Process(Any(), Any()).WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task Any_Converts_To_Double()
    {
        var mock = Mock.Of<IOverloadedService>();
        mock.Format(Any<double>(), Any()).Returns("ok");

        await Assert.That(mock.Object.Format(3.14, 2)).IsEqualTo("ok");
        await Assert.That(mock.Object.Format(0.0, 0)).IsEqualTo("ok");
        await Assert.That(mock.Object.Format(-1.5, 10)).IsEqualTo("ok");
    }

    [Test]
    public async Task Any_Converts_To_Enum()
    {
        var mock = Mock.Of<ITaskManager>();
        mock.CountByStatusAsync(Any()).Returns(99);

        await Assert.That(await mock.Object.CountByStatusAsync(Status.Active)).IsEqualTo(99);
        await Assert.That(await mock.Object.CountByStatusAsync(Status.Pending)).IsEqualTo(99);
        await Assert.That(await mock.Object.CountByStatusAsync(Status.Completed)).IsEqualTo(99);
        await Assert.That(await mock.Object.CountByStatusAsync(Status.Failed)).IsEqualTo(99);
    }

    // ──────────────────────────────────────────────
    // Implicit conversion to reference types
    // ──────────────────────────────────────────────

    [Test]
    public async Task Any_Converts_To_String()
    {
        var mock = Mock.Of<IGreeter>();
        mock.Greet(Any()).Returns("hello");

        await Assert.That(mock.Object.Greet("Alice")).IsEqualTo("hello");
        await Assert.That(mock.Object.Greet("")).IsEqualTo("hello");
    }

    [Test]
    public async Task Any_Converts_To_String_And_Matches_Null()
    {
        var mock = Mock.Of<IGreeter>();
        mock.Greet(Any()).Returns("matched");

        await Assert.That(mock.Object.Greet(null!)).IsEqualTo("matched");
    }

    [Test]
    public async Task Any_Converts_To_Array()
    {
        var mock = Mock.Of<IComplexOperations>();
        mock.BuildQuery(
            Any(), Any(), Any(), Any(), Any(), Any(), Any()
        ).Returns("query");

        var result = mock.Object.BuildQuery("users", ["id"], null, null, null, null, true);

        await Assert.That(result).IsEqualTo("query");
    }

    // ──────────────────────────────────────────────
    // Implicit conversion to nullable types
    // ──────────────────────────────────────────────

    [Test]
    public async Task Any_Converts_To_Nullable_String()
    {
        var mock = Mock.Of<INullableService>();
        mock.Process(Any(), Any()).Callback(() => { });

        // Both null and non-null should match
        mock.Object.Process(null, null);
        mock.Object.Process("text", 42);

        mock.Process(Any(), Any()).WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task Any_Converts_To_Nullable_Int()
    {
        var mock = Mock.Of<IComplexOperations>();
        mock.BuildQuery("t", Any(), Any(), Any(), Any(), Any(), Any()).Returns("ok");

        // Nullable int? params (limit, offset) accept both null and values
        await Assert.That(mock.Object.BuildQuery("t", [], null, null, null, null, true)).IsEqualTo("ok");
        await Assert.That(mock.Object.BuildQuery("t", [], null, 10, 0, null, true)).IsEqualTo("ok");
    }

    // ──────────────────────────────────────────────
    // Mixed with typed matchers
    // ──────────────────────────────────────────────

    [Test]
    public async Task Any_Mixed_With_Exact_Value()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), 5).Returns(50);

        await Assert.That(mock.Object.Add(0, 5)).IsEqualTo(50);
        await Assert.That(mock.Object.Add(999, 5)).IsEqualTo(50);
        await Assert.That(mock.Object.Add(0, 6)).IsEqualTo(0);
    }

    [Test]
    public async Task Any_Mixed_With_Typed_Any()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Any<int>()).Returns(77);

        await Assert.That(mock.Object.Add(1, 2)).IsEqualTo(77);
        await Assert.That(mock.Object.Add(-5, 0)).IsEqualTo(77);
    }

    [Test]
    public async Task Any_Mixed_With_Predicate_Matcher()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Is<int>(x => x > 0)).Returns(100);

        await Assert.That(mock.Object.Add(0, 1)).IsEqualTo(100);
        await Assert.That(mock.Object.Add(0, 0)).IsEqualTo(0);
        await Assert.That(mock.Object.Add(0, -1)).IsEqualTo(0);
    }

    [Test]
    public async Task Any_Mixed_With_IsNull_Matcher()
    {
        var mock = Mock.Of<IGreeter>();
        mock.Greet(IsNull<string>()).Returns("was null");
        mock.Greet(Any()).Returns("catch-all");

        // Last setup wins — Any() catches everything including null
        await Assert.That(mock.Object.Greet(null!)).IsEqualTo("catch-all");
        await Assert.That(mock.Object.Greet("hi")).IsEqualTo("catch-all");
    }

    // ──────────────────────────────────────────────
    // Verification context
    // ──────────────────────────────────────────────

    [Test]
    public async Task Any_Works_In_Verification()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Any()).Returns(1);

        mock.Object.Add(1, 2);
        mock.Object.Add(3, 4);
        mock.Object.Add(5, 6);

        mock.Add(Any(), Any()).WasCalled(Times.Exactly(3));
    }

    [Test]
    public async Task Any_WasNeverCalled_Verification()
    {
        var mock = Mock.Of<ICalculator>();

        mock.Add(Any(), Any()).WasNeverCalled();
    }

    [Test]
    public async Task Any_Verification_On_Void_Method()
    {
        var mock = Mock.Of<ICalculator>();

        mock.Object.Log("hello");
        mock.Object.Log("world");

        mock.Log(Any()).WasCalled(Times.Exactly(2));
    }

    // ──────────────────────────────────────────────
    // Setup behaviors (Returns, Throws, Callback)
    // ──────────────────────────────────────────────

    [Test]
    public async Task Any_With_Throws()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Any()).Throws<InvalidOperationException>();

        await Assert.That(() => mock.Object.Add(1, 2)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Any_With_Callback()
    {
        var callCount = 0;
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), Any())
            .Callback(() => callCount++);

        mock.Object.Add(1, 2);
        mock.Object.Add(3, 4);

        await Assert.That(callCount).IsEqualTo(2);
    }

    [Test]
    public async Task Any_With_Sequential_Returns()
    {
        var mock = Mock.Of<IGreeter>();
        mock.Greet(Any())
            .Returns("first")
            .Then()
            .Returns("second");

        await Assert.That(mock.Object.Greet("a")).IsEqualTo("first");
        await Assert.That(mock.Object.Greet("b")).IsEqualTo("second");
        await Assert.That(mock.Object.Greet("c")).IsEqualTo("second");
    }

    // ──────────────────────────────────────────────
    // All params using Any() (many-arg method)
    // ──────────────────────────────────────────────

    [Test]
    public async Task Any_All_Seven_Params()
    {
        var mock = Mock.Of<IComplexOperations>();
        mock.BuildQuery(Any(), Any(), Any(), Any(), Any(), Any(), Any())
            .Returns("wildcard");

        var result = mock.Object.BuildQuery("orders", ["*"], "active", 100, 0, "date", false);

        await Assert.That(result).IsEqualTo("wildcard");
    }

    // ──────────────────────────────────────────────
    // Async method support
    // ──────────────────────────────────────────────

    [Test]
    public async Task Any_With_Async_Method()
    {
        var mock = Mock.Of<IAsyncService>();
        mock.GetNameAsync(Any()).Returns("async-result");

        var result = await mock.Object.GetNameAsync("key");

        await Assert.That(result).IsEqualTo("async-result");
    }

    // ──────────────────────────────────────────────
    // Capture requires typed Any<T>() — not untyped
    // ──────────────────────────────────────────────

    [Test]
    public async Task Typed_Any_Captures_But_Untyped_Does_Not_Have_Values()
    {
        // Typed Any<T>() supports capture
        var typedArg = Any<int>();
        var mock = Mock.Of<ICalculator>();
        mock.Add(typedArg, Any()).Returns(1);

        mock.Object.Add(10, 0);
        mock.Object.Add(20, 0);

        await Assert.That(typedArg.Values).Count().IsEqualTo(2);
        await Assert.That(typedArg.Values[0]).IsEqualTo(10);
        await Assert.That(typedArg.Values[1]).IsEqualTo(20);
        await Assert.That(typedArg.Latest).IsEqualTo(20);

        // Untyped Any() is used for the second arg — still matches, no capture needed
        mock.Add(Any(), Any()).WasCalled(Times.Exactly(2));
    }

    // ──────────────────────────────────────────────
    // Overload disambiguation requires typed Any<T>()
    // ──────────────────────────────────────────────

    [Test]
    public async Task Typed_Any_Disambiguates_Overloads()
    {
        var mock = Mock.Of<IOverloadedService>();
        mock.Format(Any<int>()).Returns("int-match");
        mock.Format(Any<string>()).Returns("str-match");

        await Assert.That(mock.Object.Format(42)).IsEqualTo("int-match");
        await Assert.That(mock.Object.Format("hello")).IsEqualTo("str-match");
    }

    // ──────────────────────────────────────────────
    // Default values in loose mode with Any()
    // ──────────────────────────────────────────────

    [Test]
    public async Task Any_Without_Setup_Returns_Default_In_Loose_Mode()
    {
        var mock = Mock.Of<ICalculator>();
        // No setup — loose mode returns defaults

        await Assert.That(mock.Object.Add(1, 2)).IsEqualTo(0);
        await Assert.That(mock.Object.GetName()).IsNotNull();
    }

    [Test]
    public async Task Any_In_Strict_Mode_Allows_Configured_Calls()
    {
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);
        mock.Add(Any(), Any()).Returns(42);

        await Assert.That(mock.Object.Add(1, 2)).IsEqualTo(42);
        await Assert.That(mock.Object.Add(99, 0)).IsEqualTo(42);
    }
}
