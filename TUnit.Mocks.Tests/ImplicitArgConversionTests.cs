using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Tests for implicit conversions on <see cref="Arg{T}"/>:
/// - T value → Arg{T} (exact matching via ExactMatcher)
/// - Func{T?, bool} → Arg{T} (predicate matching via PredicateMatcher)
/// </summary>
public class ImplicitArgConversionTests
{
    // ──────────────────────────────────────────────
    // Implicit T → Arg<T> (exact value matching)
    // ──────────────────────────────────────────────

    [Test]
    public async Task Implicit_Value_Int_Matches_Exact()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(2, 3).Returns(5);

        await Assert.That(mock.Object.Add(2, 3)).IsEqualTo(5);
    }

    [Test]
    public async Task Implicit_Value_Int_Does_Not_Match_Different_Value()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(2, 3).Returns(5);

        await Assert.That(mock.Object.Add(2, 4)).IsEqualTo(0);
        await Assert.That(mock.Object.Add(3, 3)).IsEqualTo(0);
    }

    [Test]
    public async Task Implicit_Value_String_Matches_Exact()
    {
        var mock = Mock.Of<IGreeter>();
        mock.Greet("Alice").Returns("Hello, Alice!");

        await Assert.That(mock.Object.Greet("Alice")).IsEqualTo("Hello, Alice!");
    }

    [Test]
    public async Task Implicit_Value_String_Does_Not_Match_Different_Value()
    {
        var mock = Mock.Of<IGreeter>();
        mock.Greet("Alice").Returns("Hello, Alice!");

        await Assert.That(mock.Object.Greet("Bob")).IsNotEqualTo("Hello, Alice!");
        await Assert.That(mock.Object.Greet("alice")).IsNotEqualTo("Hello, Alice!");
        await Assert.That(mock.Object.Greet("")).IsNotEqualTo("Hello, Alice!");
    }

    [Test]
    public async Task Implicit_Value_Zero_Matches()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(0, 0).Returns(99);

        await Assert.That(mock.Object.Add(0, 0)).IsEqualTo(99);
    }

    [Test]
    public async Task Implicit_Value_Negative_Numbers_Match()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(-1, -2).Returns(-3);

        await Assert.That(mock.Object.Add(-1, -2)).IsEqualTo(-3);
        await Assert.That(mock.Object.Add(1, 2)).IsEqualTo(0);
    }

    [Test]
    public async Task Implicit_Value_Empty_String_Matches()
    {
        var mock = Mock.Of<IGreeter>();
        mock.Greet("").Returns("empty");

        await Assert.That(mock.Object.Greet("")).IsEqualTo("empty");
        await Assert.That(mock.Object.Greet(" ")).IsNotEqualTo("empty");
    }

    [Test]
    public async Task Implicit_Value_Mixed_With_Explicit_Matcher()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(Any(), 5).Returns(50);

        await Assert.That(mock.Object.Add(0, 5)).IsEqualTo(50);
        await Assert.That(mock.Object.Add(999, 5)).IsEqualTo(50);
        await Assert.That(mock.Object.Add(0, 6)).IsEqualTo(0);
    }

    [Test]
    public async Task Implicit_Value_Multiple_Setups_Last_Wins()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(1, 1).Returns(10);
        mock.Add(1, 1).Returns(20);

        await Assert.That(mock.Object.Add(1, 1)).IsEqualTo(20);
    }

    [Test]
    public async Task Implicit_Value_IntMaxValue_And_IntMinValue()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(int.MaxValue, int.MinValue).Returns(1);

        await Assert.That(mock.Object.Add(int.MaxValue, int.MinValue)).IsEqualTo(1);
        await Assert.That(mock.Object.Add(int.MinValue, int.MaxValue)).IsEqualTo(0);
    }

    // ──────────────────────────────────────────────
    // Implicit Func<T?, bool> → Arg<T> (predicate)
    // Using inline lambdas — the primary usage pattern
    // ──────────────────────────────────────────────

    [Test]
    public async Task Implicit_Predicate_Int_Greater_Than_Matches()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(Is<int>(x => x > 5), Any()).Returns(100);

        await Assert.That(mock.Object.Add(6, 0)).IsEqualTo(100);
        await Assert.That(mock.Object.Add(10, 0)).IsEqualTo(100);
        await Assert.That(mock.Object.Add(int.MaxValue, 0)).IsEqualTo(100);
    }

    [Test]
    public async Task Implicit_Predicate_Int_Greater_Than_Does_Not_Match()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(Is<int>(x => x > 5), Any()).Returns(100);

        await Assert.That(mock.Object.Add(5, 0)).IsEqualTo(0);
        await Assert.That(mock.Object.Add(0, 0)).IsEqualTo(0);
        await Assert.That(mock.Object.Add(-1, 0)).IsEqualTo(0);
    }

    [Test]
    public async Task Implicit_Predicate_String_StartsWith_Matches()
    {
        var mock = Mock.Of<IGreeter>();
        Func<string?, bool> startsWithHi = s => s != null && s.StartsWith("Hi");
        mock.Greet(startsWithHi).Returns("matched");

        await Assert.That(mock.Object.Greet("Hi there")).IsEqualTo("matched");
        await Assert.That(mock.Object.Greet("Hi")).IsEqualTo("matched");
    }

    [Test]
    public async Task Implicit_Predicate_String_StartsWith_Does_Not_Match()
    {
        var mock = Mock.Of<IGreeter>();
        Func<string?, bool> startsWithHi = s => s != null && s.StartsWith("Hi");
        mock.Greet(startsWithHi).Returns("matched");

        await Assert.That(mock.Object.Greet("Hello")).IsNotEqualTo("matched");
        await Assert.That(mock.Object.Greet("hi there")).IsNotEqualTo("matched");
        await Assert.That(mock.Object.Greet("")).IsNotEqualTo("matched");
    }

    [Test]
    public async Task Implicit_Predicate_Both_Args()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(Is<int>(x => x > 0), Is<int>(x => x % 2 == 0)).Returns(42);

        await Assert.That(mock.Object.Add(1, 2)).IsEqualTo(42);
        await Assert.That(mock.Object.Add(3, 4)).IsEqualTo(42);

        // First arg not positive
        await Assert.That(mock.Object.Add(0, 2)).IsEqualTo(0);
        await Assert.That(mock.Object.Add(-1, 2)).IsEqualTo(0);

        // Second arg not even
        await Assert.That(mock.Object.Add(1, 1)).IsEqualTo(0);
        await Assert.That(mock.Object.Add(1, 3)).IsEqualTo(0);
    }

    [Test]
    public async Task Implicit_Predicate_String_Contains()
    {
        var mock = Mock.Of<IGreeter>();
        Func<string?, bool> containsWorld = s => s != null && s.Contains("world");
        mock.Greet(containsWorld).Returns("has world");

        await Assert.That(mock.Object.Greet("hello world")).IsEqualTo("has world");
        await Assert.That(mock.Object.Greet("world")).IsEqualTo("has world");
        await Assert.That(mock.Object.Greet("hello")).IsNotEqualTo("has world");
    }

    [Test]
    public async Task Implicit_Predicate_String_Length_Check()
    {
        var mock = Mock.Of<IGreeter>();
        Func<string?, bool> shortString = s => s != null && s.Length <= 3;
        mock.Greet(shortString).Returns("short");

        await Assert.That(mock.Object.Greet("Hi")).IsEqualTo("short");
        await Assert.That(mock.Object.Greet("abc")).IsEqualTo("short");
        await Assert.That(mock.Object.Greet("abcd")).IsNotEqualTo("short");
    }

    [Test]
    public async Task Implicit_Predicate_Always_True_Matches_Everything()
    {
        var mock = Mock.Of<IGreeter>();
        Func<string?, bool> always = _ => true;
        mock.Greet(always).Returns("always");

        await Assert.That(mock.Object.Greet("anything")).IsEqualTo("always");
        await Assert.That(mock.Object.Greet("")).IsEqualTo("always");
    }

    [Test]
    public async Task Implicit_Predicate_Always_False_Matches_Nothing()
    {
        var mock = Mock.Of<IGreeter>();
        Func<string?, bool> never = _ => false;
        mock.Greet(never).Returns("never");

        await Assert.That(mock.Object.Greet("anything")).IsNotEqualTo("never");
        await Assert.That(mock.Object.Greet("")).IsNotEqualTo("never");
    }

    [Test]
    public async Task Implicit_Predicate_With_Closure()
    {
        var threshold = 10;
        var mock = Mock.Of<ICalculator>();
        mock.Add(Is<int>(x => x > threshold), Any()).Returns(1);

        await Assert.That(mock.Object.Add(11, 0)).IsEqualTo(1);
        await Assert.That(mock.Object.Add(10, 0)).IsEqualTo(0);
    }

    [Test]
    public async Task Implicit_Predicate_Int_Range_Check()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(Is<int>(x => x >= 1 && x <= 10), Is<int>(x => x >= 1 && x <= 10)).Returns(50);

        await Assert.That(mock.Object.Add(1, 10)).IsEqualTo(50);
        await Assert.That(mock.Object.Add(5, 5)).IsEqualTo(50);
        await Assert.That(mock.Object.Add(0, 5)).IsEqualTo(0);
        await Assert.That(mock.Object.Add(5, 11)).IsEqualTo(0);
    }

    [Test]
    public async Task Implicit_Predicate_Handles_Null_String()
    {
        var mock = Mock.Of<IGreeter>();
        Func<string?, bool> isNull = s => s is null;
        mock.Greet(isNull).Returns("was null");

        await Assert.That(mock.Object.Greet(null!)).IsEqualTo("was null");
        await Assert.That(mock.Object.Greet("not null")).IsNotEqualTo("was null");
    }

    [Test]
    public async Task Implicit_Predicate_Func_Variable_For_String()
    {
        // Verifies the implicit Func<T?, bool> → Arg<T> operator works via variable
        var mock = Mock.Of<IGreeter>();
        Func<string?, bool> predicate = s => s != null && s.Length > 3;
        mock.Greet(predicate).Returns("long name");

        await Assert.That(mock.Object.Greet("Alice")).IsEqualTo("long name");
        await Assert.That(mock.Object.Greet("Bob")).IsNotEqualTo("long name");
    }

    [Test]
    public async Task Implicit_Predicate_Func_Variable_Multiple_Setups()
    {
        var mock = Mock.Of<IGreeter>();
        Func<string?, bool> startsA = s => s != null && s.StartsWith("A");
        Func<string?, bool> startsB = s => s != null && s.StartsWith("B");
        mock.Greet(startsA).Returns("A-name");
        mock.Greet(startsB).Returns("B-name");

        // Last matching setup wins
        await Assert.That(mock.Object.Greet("Alice")).IsEqualTo("A-name");
        await Assert.That(mock.Object.Greet("Bob")).IsEqualTo("B-name");
    }

    // ──────────────────────────────────────────────
    // Mixing implicit value and implicit predicate
    // ──────────────────────────────────────────────

    [Test]
    public async Task Implicit_Value_And_Predicate_Mixed()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(10, Is<int>(x => x % 2 == 0)).Returns(77);

        // First arg must be exactly 10, second must be even
        await Assert.That(mock.Object.Add(10, 2)).IsEqualTo(77);
        await Assert.That(mock.Object.Add(10, 100)).IsEqualTo(77);

        // First arg not 10
        await Assert.That(mock.Object.Add(11, 2)).IsEqualTo(0);

        // Second arg not even
        await Assert.That(mock.Object.Add(10, 3)).IsEqualTo(0);
    }

    [Test]
    public async Task Implicit_Predicate_Overrides_Earlier_Value_Setup()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(5, 5).Returns(10);
        mock.Add(Is<int>(_ => true), Is<int>(_ => true)).Returns(99);

        // Last setup wins
        await Assert.That(mock.Object.Add(5, 5)).IsEqualTo(99);
        await Assert.That(mock.Object.Add(1, 2)).IsEqualTo(99);
    }

    [Test]
    public async Task Implicit_Value_Overrides_Earlier_Predicate_Setup()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Add(Is<int>(_ => true), Is<int>(_ => true)).Returns(99);
        mock.Add(5, 5).Returns(10);

        // Last setup wins for (5,5), predicate still catches others
        await Assert.That(mock.Object.Add(5, 5)).IsEqualTo(10);
        await Assert.That(mock.Object.Add(1, 2)).IsEqualTo(99);
    }

    [Test]
    public async Task Implicit_Predicate_With_Capture_On_Other_Arg()
    {
        var captured = Any<int>();
        var mock = Mock.Of<ICalculator>();
        mock.Add(captured, Is<int>(x => x > 0)).Returns(1);

        mock.Object.Add(42, 1);
        mock.Object.Add(99, -1); // won't match — second arg not positive

        await Assert.That(captured.Values).Count().IsEqualTo(1);
        await Assert.That(captured.Values[0]).IsEqualTo(42);
    }
}
